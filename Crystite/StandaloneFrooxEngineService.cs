//
//  SPDX-FileName: StandaloneFrooxEngineService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.Configuration;
using Crystite.Extensions;
using Crystite.Services;
using FrooxEngine;
using HarmonyLib;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Options;
using SkyFrost.Base;

namespace Crystite;

/// <summary>
/// Hosts a standalone FrooxEngine instance.
/// </summary>
public class StandaloneFrooxEngineService : BackgroundService
{
    private readonly ILogger<StandaloneFrooxEngineService> _log;
    private readonly HeadlessApplicationConfiguration _applicationConfig;
    private readonly ResoniteHeadlessConfig _config;
    private readonly Engine _engine;
    private readonly HeadlessSystemInfo _systemInfo;
    private readonly WorldService _worldService;
    private readonly ISystemdNotifier? _systemdNotifier;

    private bool _engineShutdownComplete;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandaloneFrooxEngineService"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="applicationConfig">The application configuration.</param>
    /// <param name="config">The headless configuration.</param>
    /// <param name="engine">The engine.</param>
    /// <param name="systemInfo">Information about the system.</param>
    /// <param name="worldService">The world service.</param>
    /// <param name="systemdNotifier">The systemd notifier.</param>
    public StandaloneFrooxEngineService
    (
        ILogger<StandaloneFrooxEngineService> log,
        IOptions<HeadlessApplicationConfiguration> applicationConfig,
        IOptions<ResoniteHeadlessConfig> config,
        Engine engine,
        HeadlessSystemInfo systemInfo,
        WorldService worldService,
        ISystemdNotifier? systemdNotifier = null
    )
    {
        _log = log;
        _applicationConfig = applicationConfig.Value;
        _config = config.Value;
        _engine = engine;
        _systemInfo = systemInfo;
        _worldService = worldService;
        _systemdNotifier = systemdNotifier;
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.ExecuteTask is null)
        {
            return;
        }

        try
        {
            var fieldRef = AccessTools.FieldRefAccess<CancellationTokenSource>(typeof(BackgroundService), "_stoppingCts");
            var tokenSource = fieldRef.Invoke(this);
            tokenSource.Cancel();
        }
        finally
        {
            // ignore the stop cancellation token
            await this.ExecuteTask.ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _engine.UsernameOverride = _config.UsernameOverride;
        _engine.EnvironmentShutdownCallback = () => _engineShutdownComplete = true;

        var dataFolder = string.IsNullOrWhiteSpace(_config.DataFolder)
            ? ResoniteHeadlessConfig.DefaultDataFolder
            : _config.DataFolder;

        var cacheFolder = string.IsNullOrWhiteSpace(_config.CacheFolder)
            ? ResoniteHeadlessConfig.DefaultCacheFolder
            : _config.CacheFolder;

        var launchOptions = new LaunchOptions
        {
            AdditionalAssemblies = new List<string>(_config.PluginAssemblies ?? Array.Empty<string>()),
            CacheDirectory = cacheFolder,
            DataDirectory = dataFolder,
            GeneratePrecache = _config.GeneratePreCache.GetValueOrDefault(false),
            LogsDirectory = null,
            BackgroundWorkerCount = _config.BackgroundWorkers,
            PriorityWorkerCount = _config.PriorityWorkers,
            StartInvisible = _applicationConfig.Invisible,
            VerboseInit = true
        };

        await _engine.Initialize
        (
            _applicationConfig.ResonitePath,
            launchOptions,
            _systemInfo,
            null,
            null // TODO: pass an init progress implementation that forwards to ILogger<Engine> here
        );

        var userspaceWorld = Userspace.SetupUserspace(_engine);

        // start the engine update loop so coroutines can proceed
        var engineLoop = EngineLoopAsync(ct);

        await userspaceWorld.Coroutines.StartTask(static async () => await default(ToWorld));

        if (_config.UniverseID is not null)
        {
            _engine.WorldAnnouncer.UniverseId = _config.UniverseID;
            Engine.Config.UniverseId = _config.UniverseID;
        }

        await LoginAsync();
        OverrideSignalRReconnectRoutine();

        await _engine.GlobalCoroutineManager.StartTask
        (
            static async args =>
            {
                await default(NextUpdate);

                foreach (var allowedUrlHost in args.Config.AllowedUrlHosts ?? Array.Empty<Uri>())
                {
                    args.Log.LogInformation
                    (
                        "Allowing host: {Host}, Port: {Port}",
                        allowedUrlHost.Host,
                        allowedUrlHost.Port
                    );

                    args.Engine.Security.AllowHost(allowedUrlHost.Host, allowedUrlHost.Port);
                }
            },
            (Config: _config, Log: _log, Engine: _engine)
        );

        await _engine.LocalDB.WriteVariableAsync
        (
            "Session.MaxConcurrentTransmitJobs",
            _config.MaxConcurrentAssetTransfers
        );

        await _engine.RecordManager.WaitForPendingUploadsAsync(ct: ct);

        foreach (var startWorld in _config.StartWorlds ?? Array.Empty<Configuration.WorldStartupParameters>())
        {
            if (!startWorld.IsEnabled)
            {
                continue;
            }

            var worldStart = await _worldService.StartWorldAsync(startWorld, ct);
            if (!worldStart.IsSuccess)
            {
                _log.LogWarning("Failed to start world: {Reason}", worldStart.Error);
            }
        }

        await engineLoop;
    }

    private Task LoginAsync() => _engine.GlobalCoroutineManager.StartTask
    (
        static async args =>
        {
            await default(NextUpdate);

            if (!string.IsNullOrWhiteSpace(args.Config.LoginCredential) && !string.IsNullOrWhiteSpace(args.Config.LoginPassword))
            {
                args.Log.LogInformation("Logging in as {Credential}", args.Config.LoginCredential);

                var login = await args.Engine.Cloud.Session.Login
                (
                    args.Config.LoginCredential,
                    new PasswordLogin(args.Config.LoginPassword),
                    args.Engine.LocalDB.SecretMachineID,
                    false,
                    null
                );

                if (!login.IsOK)
                {
                    args.Log.LogWarning("Failed to log in: {Error}", login.Content);
                }
                else
                {
                    args.Log.LogInformation("Logged in successfully");
                }
            }
        },
        (Config: _config, Log: _log, Engine: _engine)
    );

    private void OverrideSignalRReconnectRoutine()
    {
        if (_engine.Cloud.HubClient is null)
        {
            _log.LogDebug("Not logged in; skipping SignalR reconnection override");
            return;
        }

        var connection = _engine.Cloud.HubClient.Hub;

        // clear existing events
        var field = AccessTools.Field(typeof(HubConnection), "Closed");
        field.SetValue(connection, null);

        var cancellationTokenField = AccessTools.Field(typeof(SkyFrostInterface), "_hubConnectionToken");

        var connectDelegate = AccessTools.MethodDelegate<Func<Task>>
        (
            AccessTools.DeclaredMethod(typeof(SkyFrostInterface), "ConnectToHub"), _engine.Cloud
        );

        connection.Closed += async error =>
        {
            var tokenSource = AccessTools.FieldRefAccess<SkyFrostInterface, CancellationTokenSource>
            (
                _engine.Cloud,
                cancellationTokenField
            );

            var cancellationToken = tokenSource.Token;

            _log.LogInformation("SignalR connection closed: {Error}", error);
            if (cancellationToken.IsCancellationRequested || error is not HubException)
            {
                return;
            }

            _log.LogInformation("Running manual reconnect");
            try
            {
                await connectDelegate();
            }
            catch (Exception connectException)
            {
                _log.LogWarning(connectException, "Failed to reconnect; attempting to relog");

                try
                {
                    await LoginAsync();
                }
                catch (Exception loginException)
                {
                    _log.LogWarning(loginException, "Failed to relog");
                    throw;
                }
            }
        };
    }

    private async Task EngineLoopAsync(CancellationToken ct = default)
    {
        var audioStartTime = DateTimeOffset.UtcNow;
        var audioTime = 0.0;
        var audioTickRate = 1.0 / _config.TickRate;

        using var tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / _config.TickRate));

        var isShuttingDown = false;
        while (!ct.IsCancellationRequested || !_engineShutdownComplete)
        {
            // Intentional. Ticks should continue until _engineShutdownComplete, so cancelling early here is
            // undesired.
            _ = await tickTimer.WaitForNextTickAsync(CancellationToken.None);

            try
            {
                _engine.RunUpdateLoop();
                _systemInfo.FrameFinished();

                if (ct.IsCancellationRequested && _systemdNotifier is not null)
                {
                    _systemdNotifier.Notify
                    (
                        new ServiceState($"EXTEND_TIMEOUT_USEC={(int)TimeSpan.FromSeconds(15).TotalMicroseconds}")
                    );
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unexpected error during engine update loop");
            }

            audioTime += audioTickRate * 48000f;
            if (audioTime >= 1024.0)
            {
                audioTime = (audioTime - 1024.0) % 1024.0;
                DummyAudioConnector.UpdateCallback((DateTimeOffset.UtcNow - audioStartTime).TotalMilliseconds * 1000);
            }

            if (!ct.IsCancellationRequested || isShuttingDown)
            {
                continue;
            }

            isShuttingDown = true;
            Userspace.ExitApp(false);
        }
    }
}
