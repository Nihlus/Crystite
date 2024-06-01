//
//  SPDX-FileName: WorldService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using Crystite.Configuration;
using Crystite.Extensions;
using Crystite.Patches.EngineRecordUploadTask;
using FrooxEngine;
using Microsoft.Extensions.Options;
using Remora.Results;
using SkyFrost.Base;
using Record = FrooxEngine.Store.Record;

namespace Crystite.Services;

/// <summary>
/// Handles access to and control of worlds.
/// </summary>
public class WorldService
{
    private readonly ResoniteHeadlessConfig _config;
    private readonly ILogger<WorldService> _log;
    private readonly Engine _engine;
    private readonly ConcurrentDictionary<string, SessionWrapper> _activeWorlds;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldService"/> class.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="engine">The engine.</param>
    public WorldService(IOptions<ResoniteHeadlessConfig> config, ILogger<WorldService> log, Engine engine)
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
        Configuration.WorldStartupParameters startupParameters,
        CancellationToken ct = default
    )
    {
        var sessionID = startupParameters.CustomSessionID;
        if (sessionID is not null)
        {
            // automatically add the session ID prefix if it's not already been provided.
            // custom session IDs must be in the form "S-U-<username>:<arbitrary>
            if (!sessionID.StartsWith("S-"))
            {
                sessionID = $"S-{sessionID}";
            }

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

        var createStartSettings = startupParameters.CreateWorldStartSettings(sessionID);
        if (!createStartSettings.IsDefined(out var startSettings))
        {
            return Result<ActiveSession>.FromError(createStartSettings);
        }

        var world = await Userspace.OpenWorld(startSettings);
        if (world is null)
        {
            return new InvalidOperationError("Could not open world. Is the record or template valid and accessible to the logged-in account?");
        }

        // wait for the world to initialize
        while (world.State is World.WorldState.Initializing)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
        }

        if (world.State is World.WorldState.Failed)
        {
            return new InvalidOperationError($"Internal startup failure: {world.FailState.ToString()}");
        }

        // wait for at least one update so that the engine has time to process the newly started world
        startupParameters = await _engine.GlobalCoroutineManager.StartBackgroundTask
        (
            async () =>
            {
                await default(NextUpdate);
                return await world.SetParametersAsync(startupParameters, _log);
            }
        );

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

        await wrapper.CancellationSource.CancelAsync();
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

        await wrapper.CancellationSource.CancelAsync();
        await (wrapper.Handler ?? Task.CompletedTask);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Stops all currently running worlds.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAllWorldsAsync(CancellationToken ct = default)
    {
        var valueSnapshot = _activeWorlds.Values;
        foreach (var wrapper in valueSnapshot)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            await wrapper.CancellationSource.CancelAsync();
            await (wrapper.Handler ?? Task.CompletedTask);

            _ = _activeWorlds.TryRemove(wrapper.Session.World.SessionId, out _);
        }
    }

    /// <summary>
    /// Asynchronously manages an active session, restarting it and updating its information as necessary.
    /// </summary>
    /// <param name="wrapper">The session wrapper to manage.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    private async Task SessionHandlerAsync(SessionWrapper wrapper, CancellationToken ct = default)
    {
        async Task RestartSessionAsync(SessionWrapper sessionWrapper, CancellationToken cancellationToken)
        {
            var world = sessionWrapper.Session.World;

            world.WorldManager.WorldFailed -= MarkAutoRecoverRestart;
            RecordStoreNotifications.RecordStored -= UpdateCorrespondingRecord;

            if (!world.IsDestroyed)
            {
                world.Destroy();
            }

            await default(NextUpdate);

            // start a new instance of this world
            var restartWorld = await StartWorldAsync(sessionWrapper.Session.StartInfo, cancellationToken);
            if (!restartWorld.IsSuccess)
            {
                _log.LogError
                (
                    "Failed to restart world {World}: {Reason}",
                    world.RawName,
                    restartWorld.Error
                );
            }
        }

        async Task StopSessionAsync(SessionWrapper sessionWrapper)
        {
            var world = sessionWrapper.Session.World;
            if (world.SaveOnExit && Userspace.CanSave(world))
            {
                // wait for any pending syncs of this world
                while (!world.CorrespondingRecord.IsSynced)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
                }

                _log.LogInformation("Saving {World}", world.RawName);
                await Userspace.SaveWorldAuto(sessionWrapper.Session.World, SaveType.Overwrite, true);
            }

            world.WorldManager.WorldFailed -= MarkAutoRecoverRestart;
            RecordStoreNotifications.RecordStored -= UpdateCorrespondingRecord;

            if (!world.IsDestroyed)
            {
                world.Destroy();
            }
        }

        var restart = false;
        var world = wrapper.Session.World;
        var autoRecover = wrapper.Session.StartInfo.AutoRecover;

        void MarkAutoRecoverRestart(World failedWorld)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (world.SessionId != failedWorld.SessionId)
            {
                return;
            }

            if (autoRecover)
            {
                restart = true;
                _log.LogWarning("World {World} has crashed! Restarting...", failedWorld.RawName);
            }
            else
            {
                _log.LogWarning("World {World} has crashed!", failedWorld.RawName);
            }
        }

        void UpdateCorrespondingRecord(Record record)
        {
            if (world.CorrespondingRecord is null)
            {
                return;
            }

            if (world.CorrespondingRecord.IsSameRecord(record))
            {
                world.CorrespondingRecord = record;
            }
        }

        wrapper.Session.World.WorldManager.WorldFailed += MarkAutoRecoverRestart;
        RecordStoreNotifications.RecordStored += UpdateCorrespondingRecord;

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
                        UseCustomJoinVerifier = world.UseCustomJoinVerifier,
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

            if (wrapper.HasAutosaveIntervalElapsed && Userspace.CanSave(world))
            {
                // only attempt a save if the last save has been synchronized and we're not shutting down
                if (world.CorrespondingRecord.IsSynced && !(Userspace.IsExitingApp || _engine.ShutdownRequested))
                {
                    _log.LogInformation("Autosaving {World}", world.RawName);
                    await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);
                }

                wrapper = wrapper with
                {
                    LastSaveTime = DateTimeOffset.UtcNow
                };
            }

            wrapper = wrapper with
            {
                IdleBeginTime = world.UserCount switch
                {
                    1 when wrapper.LastUserCount > 1 => DateTimeOffset.UtcNow,
                    > 1 => null,
                    _ => wrapper.IdleBeginTime
                }
            };

            if (wrapper.HasIdleTimeElapsed)
            {
                _log.LogInformation
                (
                    "World {World} has been idle for {Time} seconds, restarting",
                    world.RawName,
                    (long)wrapper.TimeSpentIdle.TotalSeconds
                );

                restart = true;
                world.Destroy();

                break;
            }

            if (wrapper.HasForcedRestartIntervalElapsed)
            {
                _log.LogInformation
                (
                    "World {World} has been running for {Time:1:F0} seconds, forcing a restart",
                    world.RawName,
                    wrapper.ForceRestartInterval.TotalSeconds
                );

                restart = true;
                world.Destroy();

                break;
            }

            wrapper = wrapper with
            {
                LastUserCount = world.UserCount
            };

            if (!_activeWorlds.TryUpdate(world.SessionId, wrapper, originalWrapper))
            {
                _log.LogError
                (
                    "Failed to update an active session's information ({World})! This is probably a concurrency bug",
                    world.RawName
                );
            }
        }

        _log.LogInformation("World {World} has stopped", world.Name);

        // always remove us first
        _ = _activeWorlds.TryRemove(wrapper.Session.World.SessionId, out _);

        if (!ct.IsCancellationRequested && restart)
        {
            await RestartSessionAsync(wrapper, ct);
            return;
        }

        await StopSessionAsync(wrapper);
    }

    /// <summary>
    /// Wraps an active session with additional varying information used by the world service to manage the session.
    /// </summary>
    /// <param name="Session">The managed session.</param>
    /// <param name="CancellationSource">The cancellation token source for the session.</param>
    /// <param name="LastSaveTime">The last time at which the session was saved and synchronized.</param>
    /// <param name="IdleBeginTime">The time at which the current idle period began.</param>
    /// <param name="LastUserCount">The last known user count.</param>
    private record SessionWrapper
    (
        ActiveSession Session,
        CancellationTokenSource CancellationSource,
        DateTimeOffset LastSaveTime = default,
        DateTimeOffset? IdleBeginTime = default,
        int LastUserCount = 0
    )
    {
        /// <summary>
        /// Gets or sets the session handler loop.
        /// </summary>
        public Task? Handler { get; set; }

        /// <summary>
        /// Gets the interval at which the world should automatically save.
        /// </summary>
        public TimeSpan AutosaveInterval { get; } = TimeSpan.FromSeconds(Session.StartInfo.AutosaveInterval);

        /// <summary>
        /// Gets the time after which an idle world should restart.
        /// </summary>
        public TimeSpan IdleRestartInterval { get; } = TimeSpan.FromSeconds(Session.StartInfo.IdleRestartInterval);

        /// <summary>
        /// Gets the absolute time after which a world should unconditionally restart.
        /// </summary>
        public TimeSpan ForceRestartInterval { get; } = TimeSpan.FromSeconds(Session.StartInfo.ForcedRestartInterval);

        /// <summary>
        /// Gets the uptime of the session.
        /// </summary>
        public TimeSpan TimeRunning => DateTimeOffset.UtcNow - this.Session.World.Time.LocalSessionBeginTime;

        /// <summary>
        /// Gets the time elapsed since the last time the session saved.
        /// </summary>
        public TimeSpan TimeSinceLastSave => DateTimeOffset.UtcNow - this.LastSaveTime;

        /// <summary>
        /// Gets the time spent by the world in the current idle state.
        /// </summary>
        public TimeSpan TimeSpentIdle => this.IdleBeginTime is null
            ? TimeSpan.Zero
            : DateTimeOffset.UtcNow - this.IdleBeginTime.Value;

        /// <summary>
        /// Gets a value indicating whether the forced restart interval has elapsed.
        /// </summary>
        public bool HasForcedRestartIntervalElapsed => this.ForceRestartInterval > TimeSpan.Zero &&
                                                       this.TimeRunning > this.ForceRestartInterval;

        /// <summary>
        /// Gets a value indicating whether the autosave interval has elapsed.
        /// </summary>
        public bool HasAutosaveIntervalElapsed => this.AutosaveInterval > TimeSpan.Zero &&
                                                  this.TimeSinceLastSave > this.AutosaveInterval;

        /// <summary>
        /// Gets a value indicating whether the idle restart interval has elapsed.
        /// </summary>
        public bool HasIdleTimeElapsed => this.IdleRestartInterval > TimeSpan.Zero &&
                                          this.TimeSpentIdle > this.IdleRestartInterval;

        /// <summary>
        /// Gets the last time at which the world was successfully saved and synchronized.
        /// </summary>
        public DateTimeOffset LastSaveTime { get; init; } = LastSaveTime == default
            ? DateTimeOffset.UtcNow
            : LastSaveTime;
    }
}
