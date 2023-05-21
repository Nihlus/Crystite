//
//  SPDX-FileName: WorldService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using CloudX.Shared;
using FrooxEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Extensions;
using Remora.Results;
using WorldStartupParameters = Remora.Neos.Headless.Configuration.WorldStartupParameters;

namespace Remora.Neos.Headless.Services;

/// <summary>
/// Handles access to and control of worlds.
/// </summary>
public class WorldService
{
    private readonly NeosHeadlessConfig _config;
    private readonly ILogger<WorldService> _log;
    private readonly Engine _engine;
    private readonly ConcurrentDictionary<string, SessionWrapper> _activeWorlds;

    private record SessionWrapper(ActiveSession Session, CancellationTokenSource CancellationSource)
    {
        /// <summary>
        /// Gets or sets the session handler loop.
        /// </summary>
        public Task? Handler { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldService"/> class.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="engine">The engine.</param>
    public WorldService(IOptions<NeosHeadlessConfig> config, ILogger<WorldService> log, Engine engine)
    {
        _config = config.Value;
        _log = log;
        _engine = engine;
        _activeWorlds = new();
    }

    /// <summary>
    /// Starts a world based on the given startup parameters.
    /// </summary>
    /// <param name="startupParameters">The startup parameters.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The started session.</returns>
    public async Task<Result<ActiveSession>> StartWorldAsync
    (
        WorldStartupParameters startupParameters,
        CancellationToken ct = default
    )
    {
        var sessionID = startupParameters.CustomSessionID;
        if (sessionID is not null)
        {
            if (!SessionInfo.IsValidSessionId(sessionID))
            {
                _log.LogWarning("Invalid custom session ID: {ID}", sessionID);
                sessionID = null;
            }

            var sessionIDOwner = SessionInfo.GetCustomSessionIdOwner(sessionID);
            if (sessionIDOwner != _engine.Cloud.CurrentUser?.Id)
            {
                _log.LogWarning
                (
                    "Cannot use session ID that's owned by another user. Trying to use {ID}, currently logged in as {User}",
                    sessionID,
                    _engine.Cloud.CurrentUser?.Id ?? "anonymous"
                );

                sessionID = null;
            }
        }

        var startSettings = new WorldStartSettings();
        if (startupParameters.LoadWorldURL is not null)
        {
            startSettings.URIs = new[] { startupParameters.LoadWorldURL };
        }
        else if (startupParameters.LoadWorldPresetName is not null)
        {
            var worldPreset = (await WorldPresets.GetPresets()).FirstOrDefault
            (
                p => startupParameters.LoadWorldPresetName.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase)
            );

            if (worldPreset is null)
            {
                return new NotFoundError($"Unknown world preset: {startupParameters.LoadWorldPresetName}");
            }

            startSettings.InitWorld = worldPreset.Method;
        }
        else
        {
            return new InvalidOperationError
            (
                "No world startup information. At least one of loadWorldUrl or loadWorldPresetName is required."
            );
        }

        startSettings.ForcePort = startupParameters.ForcePort.GetValueOrDefault();
        startSettings.ForceSessionId = sessionID;
        startSettings.DefaultAccessLevel = startupParameters.AccessLevel;
        startSettings.HideFromListing = startupParameters.HideFromPublicListing;
        startSettings.GetExisting = false;
        startSettings.CreateLoadIndicator = false;

        var world = await Userspace.OpenWorld(startSettings);

        // wait for the world to initialize
        while (world.State is World.WorldState.Initializing)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
        }

        if (world.State is World.WorldState.Failed)
        {
            return new InvalidOperationError($"Internal startup failure: {world.FailState.ToString()}");
        }

        startupParameters = await world.SetParametersAsync(startupParameters, _log);

        var session = new ActiveSession(startupParameters, world);
        var sessionCancellation = new CancellationTokenSource();
        var wrapper = new SessionWrapper(session, sessionCancellation);

        if (!_activeWorlds.TryAdd(world.SessionId, wrapper))
        {
            throw new InvalidOperationException("Duplicate session ID?");
        }

        var handler = _engine.GlobalCoroutineManager.StartTask
        (
            args => SessionHandlerAsync(args.Wrapper, args.Token),
            (Wrapper: wrapper, sessionCancellation.Token)
        );

        wrapper.Handler = handler;

        _log.LogInformation("World running: {Name}", startupParameters.SessionName ?? world.SessionId);

        // Coroutines are kept track of by the engine
        _ = world.Coroutines.StartTask
        (
            async config =>
            {
                if (config.AutoSpawnItems is null)
                {
                    return;
                }

                foreach (var autoSpawnItem in config.AutoSpawnItems)
                {
                    await world.RootSlot.AddSlot("Headless Auto-Spawn").LoadObjectAsync(autoSpawnItem);
                }
            },
            _config
        );

        return wrapper.Session;
    }

    /// <summary>
    /// Restarts the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The restarted world.</returns>
    public async Task<Result<ActiveSession>> RestartWorldAsync(string worldId, CancellationToken ct = default)
    {
        if (!_activeWorlds.TryRemove(worldId, out var wrapper))
        {
            return new NotFoundError("No matching world found.");
        }

        wrapper.CancellationSource.Cancel();
        await (wrapper.Handler ?? Task.CompletedTask);

        return await StartWorldAsync(wrapper.Session.StartInfo, ct);
    }

    /// <summary>
    /// Stops the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> StopWorldAsync(string worldId)
    {
        if (!_activeWorlds.TryRemove(worldId, out var wrapper))
        {
            return new NotFoundError("No matching world found.");
        }

        wrapper.CancellationSource.Cancel();
        await (wrapper.Handler ?? Task.CompletedTask);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Asynchronously manages an active session, restarting it and updating its information as necessary.
    /// </summary>
    /// <param name="wrapper">The session wrapper to manage.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    private async Task SessionHandlerAsync(SessionWrapper wrapper, CancellationToken ct = default)
    {
        var restart = false;
        var autoRecover = wrapper.Session.StartInfo.AutoRecover;
        wrapper.Session.World.WorldManager.WorldFailed += world =>
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (autoRecover)
            {
                restart = true;
                _log.LogWarning("World {World} has crashed! Restarting...", world.RawName);
            }
            else
            {
                _log.LogWarning("World {World} has crashed!", world.RawName);
            }
        };

        var lastUserCount = 1;
        var lastIdleBeginTime = DateTimeOffset.UtcNow;
        var lastSaveTime = DateTimeOffset.UtcNow;

        while (!ct.IsCancellationRequested && !wrapper.Session.World.IsDestroyed)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            // Update any changes to our startup information
            var startupParameters = wrapper.Session.StartInfo;
            var world = wrapper.Session.World;
            wrapper.Session.World.RunSynchronously
            (
                () =>
                {
                    startupParameters = startupParameters with
                    {
                        SessionName = world.Name,
                        Description = world.Description,
                        MaxUsers = world.MaxUsers,
                        AccessLevel = world.AccessLevel,
                        HideFromPublicListing = world.HideFromListing,
                        Tags = world.Tags.ToList(),
                        MobileFriendly = world.MobileFriendly,
                        ParentSessionIDs = world.ParentSessionIds.ToList(),
                        AwayKickMinutes = !world.AwayKickEnabled ? -1.0 : world.AwayKickMinutes
                    };
                }
            );

            var originalWrapper = wrapper;
            wrapper = wrapper with
            {
                Session = wrapper.Session with
                {
                    StartInfo = startupParameters
                }
            };

            if (!_activeWorlds.TryUpdate(world.SessionId, wrapper, originalWrapper))
            {
                _log.LogError
                (
                    "Failed to update an active session's information ({World})! This is probably a concurrency bug",
                    world.RawName
                );
            }

            var timeSinceLastSave = DateTimeOffset.UtcNow - lastSaveTime;
            var autosaveInterval = TimeSpan.FromSeconds(wrapper.Session.StartInfo.AutosaveInterval);
            if (autosaveInterval > TimeSpan.Zero && timeSinceLastSave > autosaveInterval && Userspace.CanSave(world))
            {
                _log.LogInformation("Autosaving {World}", world.RawName);
                await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);
            }

            // world is idle
            if (world.UserCount == 1)
            {
                if (lastUserCount < 1)
                {
                    lastIdleBeginTime = DateTimeOffset.UtcNow;
                }

                var idleRestartInterval = TimeSpan.FromSeconds(wrapper.Session.StartInfo.IdleRestartInterval);
                var timeSpentIdle = DateTimeOffset.UtcNow - lastIdleBeginTime;

                if (idleRestartInterval > TimeSpan.Zero && timeSpentIdle > idleRestartInterval)
                {
                    _log.LogInformation
                    (
                        "World {World} has been idle for {Time:1:F0} seconds, restarting",
                        world.RawName,
                        timeSpentIdle.TotalSeconds
                    );

                    restart = true;
                    world.Destroy();

                    break;
                }
            }

            var forceRestartInterval = TimeSpan.FromSeconds(wrapper.Session.StartInfo.ForcedRestartInterval);
            var timeRunning = DateTimeOffset.UtcNow - world.Time.LocalSessionBeginTime;
            if (forceRestartInterval > TimeSpan.Zero && timeRunning > forceRestartInterval)
            {
                _log.LogInformation
                (
                    "World {World} has been running for {Time:1:F0} seconds, forcing a restart",
                    world.RawName,
                    forceRestartInterval.TotalSeconds
                );

                restart = true;
                world.Destroy();

                break;
            }

            lastUserCount = world.UserCount;
        }

        // always remove us first
        _ = _activeWorlds.TryRemove(wrapper.Session.World.SessionId, out _);

        if (!ct.IsCancellationRequested && restart)
        {
            var restartWorld = await StartWorldAsync(wrapper.Session.StartInfo, ct);
            if (!restartWorld.IsSuccess)
            {
                _log.LogError
                (
                    "Failed to restart world {World}: {Reason}",
                    wrapper.Session.World.RawName,
                    restartWorld.Error
                );
            }
        }
    }
}
