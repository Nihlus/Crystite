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
    private readonly ConcurrentDictionary<string, ActiveSession> _activeWorlds;

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
    public async Task<Result<ActiveSession>> StartWorld
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

        // Coroutines are kept track of by the engine
        _ = _engine.GlobalCoroutineManager.StartTask
        (
            args => SessionHandlerAsync(args.Session, args.Token),
            (Session: session, Token: ct)
        );

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

        if (!_activeWorlds.TryAdd(world.RawName, session))
        {
            throw new InvalidOperationException("Duplicate session ID?");
        }

        return session;
    }

    /// <summary>
    /// Asynchronously manages an active session, restarting it and updating its information as necessary.
    /// </summary>
    /// <param name="session">The session to manage.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    private async Task SessionHandlerAsync(ActiveSession session, CancellationToken ct = default)
    {
        var restart = false;
        var autoRecover = session.StartInfo.AutoRecover;
        session.World.WorldManager.WorldFailed += world =>
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

        while (!ct.IsCancellationRequested && !session.World.IsDestroyed)
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
            var startupParameters = session.StartInfo;
            var world = session.World;
            session.World.RunSynchronously
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

            var originalSession = session;
            session = session with
            {
                StartInfo = startupParameters
            };

            if (!_activeWorlds.TryUpdate(world.RawName, session, originalSession))
            {
                _log.LogError
                (
                    "Failed to update an active session's information ({World})! This is probably a concurrency bug",
                    world.RawName
                );
            }

            var timeSinceLastSave = DateTimeOffset.UtcNow - lastSaveTime;
            var autosaveInterval = TimeSpan.FromSeconds(session.StartInfo.AutosaveInterval);
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

                var idleRestartInterval = TimeSpan.FromSeconds(session.StartInfo.IdleRestartInterval);
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
                }
            }

            var forceRestartInterval = TimeSpan.FromSeconds(session.StartInfo.ForcedRestartInterval);
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
            }

            lastUserCount = world.UserCount;
        }

        if (!ct.IsCancellationRequested && restart)
        {
            // always remove us first
            _ = _activeWorlds.TryRemove(session.World.RawName, out _);

            var restartWorld = await StartWorld(session.StartInfo, ct);
            if (!restartWorld.IsSuccess)
            {
                _log.LogError("Failed to restart world {World}: {Reason}", session.World.RawName, restartWorld.Error);
            }
        }
    }
}
