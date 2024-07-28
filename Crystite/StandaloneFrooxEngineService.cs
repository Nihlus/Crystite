//
//  SPDX-FileName: StandaloneFrooxEngineService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Crystite.Configuration;
using Crystite.Extensions;
using Crystite.Services;
using FrooxEngine;
using HarmonyLib;
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
    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly ISystemdNotifier? _systemdNotifier;

    private bool _applicationStartupComplete;
    private bool _engineShutdownComplete;

    /// <summary>
    /// Forwards logged initialization phases of the engine to the logging subsystem.
    /// </summary>
    private class LoggerEngineInitProgress : IEngineInitProgress
    {
        private readonly ILogger _log;

        /// <inheritdoc />
        public int FixedPhaseIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerEngineInitProgress"/> class.
        /// </summary>
        /// <param name="log">The logging instance to forward to.</param>
        public LoggerEngineInitProgress(ILogger log)
        {
            _log = log;
        }

        /// <inheritdoc />
        public void SetFixedPhase(string phase)
        {
            ++this.FixedPhaseIndex;
            _log.LogDebug("{Phase}", phase);
        }

        /// <inheritdoc />
        public void SetSubphase(string? subphase, bool alwaysShow = false)
        {
            if (subphase is null)
            {
                return;
            }

            _log.LogDebug("\t{Subphase}", subphase);
        }

        /// <inheritdoc />
        public void EngineReady()
        {
            _log.LogInformation("Engine ready");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandaloneFrooxEngineService"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="applicationConfig">The application configuration.</param>
    /// <param name="config">The headless configuration.</param>
    /// <param name="engine">The engine.</param>
    /// <param name="systemInfo">Information about the system.</param>
    /// <param name="worldService">The world service.</param>
    /// <param name="applicationLifetime">The application lifetime.</param>
    /// <param name="systemdNotifier">The systemd notifier.</param>
    public StandaloneFrooxEngineService
    (
        ILogger<StandaloneFrooxEngineService> log,
        IOptions<HeadlessApplicationConfiguration> applicationConfig,
        IOptions<ResoniteHeadlessConfig> config,
        Engine engine,
        HeadlessSystemInfo systemInfo,
        WorldService worldService,
        IHostApplicationLifetime applicationLifetime,
        ISystemdNotifier? systemdNotifier = null
    )
    {
        _log = log;
        _applicationConfig = applicationConfig.Value;
        _config = config.Value;
        _engine = engine;
        _systemInfo = systemInfo;
        _worldService = worldService;
        _applicationLifetime = applicationLifetime;
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
            await tokenSource.CancelAsync();
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

        // These assemblies need to be loaded into the AppDomain for the engine to initialize correctly. Depending on
        // what we do, these may not actually be present at the point the engine needs them, so we force their loading
        // here
        EnsureAssemblyIsLoaded("ProtoFlux.Nodes.FrooxEngine");
        EnsureAssemblyIsLoaded("ProtoFluxBindings");

        await _engine.Initialize
        (
            _applicationConfig.ResonitePath,
            launchOptions,
            _systemInfo,
            null,
            new LoggerEngineInitProgress(_log)
        );

        var userspaceWorld = Userspace.SetupUserspace(_engine);

        // start the engine update loop so coroutines can proceed
        var engineLoop = EngineLoopAsync(ct);

        await userspaceWorld.Coroutines.StartTask(static async () => await default(ToWorld));

        if (_config.UniverseID is not null)
        {
            Engine.Config.UniverseId = _config.UniverseID;
        }

        await LoginAsync();

        await ConfigureAllowedHostsAsync();

        await _engine.LocalDB.WriteVariableAsync
        (
            "Session.MaxConcurrentTransmitJobs",
            _config.MaxConcurrentAssetTransfers
        );

        await _engine.RecordManager.WaitForPendingUploadsAsync(ct: ct);

        var startWorlds = _config.StartWorlds ?? Array.Empty<Configuration.WorldStartupParameters>();
        if (startWorlds.Count is 0)
        {
            _log.LogWarning
            (
                "No startup worlds have been configured. Check your configuration for errors if you expect at least"
                + " one world to start automatically"
            );
        }

        foreach (var startWorld in startWorlds)
        {
            if (!startWorld.IsEnabled)
            {
                continue;
            }

            _log.LogInformation("Starting {World}", startWorld.SessionName ?? "a world without a name");
            var worldStart = await _worldService.StartWorldAsync(startWorld, ct);
            if (!worldStart.IsSuccess)
            {
                _log.LogWarning("Failed to start world: {Reason}", worldStart.Error);
            }
        }

        _applicationStartupComplete = true;

        await engineLoop;
        _applicationLifetime.StopApplication();
    }

    private async Task ConfigureAllowedHostsAsync()
    {
        await _engine.GlobalCoroutineManager.StartTask
        (
            static async args =>
            {
                var (applicationConfiguration, configuration, log, engine) = args;

                await default(NextUpdate);

                foreach (var allowedUrlHost in configuration.AllowedUrlHosts ?? Array.Empty<string>())
                {
                    var extractedHost = string.Empty;
                    var extractedPort = 443;

                    if (Uri.TryCreate(allowedUrlHost, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
                    {
                        extractedHost = uri.Host;
                        extractedPort = uri.Port;
                    }
                    else
                    {
                        var urlSegments = allowedUrlHost.Split(':');
                        switch (urlSegments.Length)
                        {
                            case 1:
                            {
                                extractedHost = urlSegments[0];
                                log.LogWarning
                                (
                                    "Could not determine port for allowed host entry \"{Host}\". Defaulting to port {Port}",
                                    allowedUrlHost,
                                    extractedPort
                                );

                                break;
                            }
                            case 2:
                            {
                                extractedHost = urlSegments[0];
                                extractedPort = int.Parse(urlSegments[1]);

                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(extractedHost))
                    {
                        log.LogWarning
                        (
                            "Unable to parse allowed host entry: \"{Host}\"",
                            allowedUrlHost
                        );

                        return;
                    }

                    // Non-exhaustive check so people don't unintentionally expose Crystite's API - the
                    string[] dangerousHostPatterns =
                    {
                        "^localhost$",
                        @"^127\.\d{1,3}\.\d{1,3}\.\d{1,3}$",
                        "^ip6-localhost$",
                        "^ip6-loopback$",
                        @"^\[::1\]$",
                        Environment.MachineName
                    };

                    if (dangerousHostPatterns.Any(p => Regex.IsMatch(allowedUrlHost, p)))
                    {
                        log.LogError("WARNING - you may be putting this machine at risk");
                        log.LogError("Allowed host entry \"{Entry}\" allows access to localhost", allowedUrlHost);
                        log.LogError
                        (
                            "HTTP requests ignore the specified port, meaning Crystite's API and other sensitive "
                            + "services can be accessed even if you allowed a different port"
                        );
                        log.LogError
                        (
                            "Having this entry whitelisted could allow arbitrary users to give themselves "
                            + "administrator privileges or cause other serious harm. Proceed with extreme caution"
                        );

                        if (!applicationConfiguration.AllowUnsafeHosts)
                        {
                            log.LogInformation("Skipping whitelisting of \"{Entry}\"", allowedUrlHost);
                            continue;
                        }
                    }

                    log.LogInformation
                    (
                        "Allowing host: {Host}, Port: {Port}",
                        extractedHost,
                        extractedPort
                    );

                    engine.Security.TemporarilyAllowHTTP(extractedHost);
                    engine.Security.TemporarilyAllowWebsocket(extractedHost, extractedPort);
                }
            },
            (ApplicationConfiguration: _applicationConfig, Configuration: _config, Log: _log, Engine: _engine)
        );
    }

    private Task LoginAsync() => _engine.GlobalCoroutineManager.StartTask
    (
        static async args =>
        {
            await default(NextUpdate);

            if (!string.IsNullOrWhiteSpace(args.Configuration.LoginCredential) && !string.IsNullOrWhiteSpace(args.Configuration.LoginPassword))
            {
                args.Log.LogInformation("Logging in as {Credential}", args.Configuration.LoginCredential);

                var login = await args.Engine.Cloud.Session.Login
                (
                    args.Configuration.LoginCredential,
                    new PasswordLogin(args.Configuration.LoginPassword),
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
        (Configuration: _config, Log: _log, Engine: _engine)
    );

    private async Task EngineLoopAsync(CancellationToken ct = default)
    {
        var audioStartTime = DateTimeOffset.UtcNow;
        var audioTime = 0.0;
        var audioTickRate = 1.0 / _config.TickRate;

        using var tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / _config.TickRate));

        Task? exitEngineTask = null;
        var isShuttingDown = false;
        while (!ct.IsCancellationRequested || !_engineShutdownComplete || !_applicationStartupComplete)
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

            if (!ct.IsCancellationRequested || isShuttingDown || !_applicationStartupComplete)
            {
                continue;
            }

            isShuttingDown = true;
            exitEngineTask = ExitEngineAsync();
        }

        // observe any results from shutting down the engine.
        await (exitEngineTask ?? Task.CompletedTask);
    }

    /// <summary>
    /// Exits the engine, saving relevant worlds and settings, synchronizing with the cloud, and then requesting an
    /// environment shutdown.
    /// </summary>
    /// <remarks>
    /// This method is loosely based on what Userspace.ExitApp(false) would do with major simplifications. Primarily,
    /// the order of some operations have been switched around and the exit will actually wait for all pending uploads
    /// before terminating. Additionally, all worlds are stopped in a controlled fashion instead of just letting them
    /// get destroyed on their own.
    ///
    /// Once the engine shutdown is complete, <see cref="_engineShutdownComplete"/> will be set to <value>true</value>.
    /// </remarks>
    private async Task ExitEngineAsync()
    {
        await _worldService.StopAllWorldsAsync();

        await _engine.RecordManager.WaitForPendingUploadsAsync();

        await _engine.Cloud.FinalizeSession();

        _engine.RequestShutdown();
    }

    /// <summary>
    /// Ensures an assembly with the given name is loaded into the application domain, guarding against duplicates
    /// before performing the load.
    /// </summary>
    /// <remarks>The name is not exactly matched against; rather, a Contains() call is used.</remarks>
    /// <param name="assemblyName">The name of the assembly.</param>
    private static void EnsureAssemblyIsLoaded(string assemblyName)
    {
        _ = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
    }
}
