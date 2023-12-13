//
//  SPDX-FileName: ResoniteDependentHostConfiguration.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Extensions;
using Crystite.Configuration;
using Crystite.Implementations;
using Crystite.Patches.EngineSkyFrostInterface;
using Crystite.Patches.Generic;
using Crystite.Patches.RecordUploadTaskBase;
using Crystite.Patches.ResoniteAssemblyPostProcessor;
using Crystite.Patches.SteamConnector;
using Crystite.Patches.VideoTextureProvider;
using Crystite.Patches.WorkerManager;
using Crystite.Services;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.Store;
using HarmonyLib;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Crystite;

/// <summary>
/// Contains second-stage host configuration, usable only after assembly loading is set up.
/// </summary>
public static class ResoniteDependentHostConfiguration
{
    private static readonly IReadOnlyDictionary<string, LogLevel> _messagePatterns = new Dictionary<string, LogLevel>
    {
        { "Compatibility Hash:", LogLevel.Trace },
        { "KeyListenerAdded:", LogLevel.Trace },
        { "Sending info matching broadcast key", LogLevel.Trace },
        { "MachineID:", LogLevel.Trace },
        { "SIGNALR: ListenOnKey", LogLevel.Trace },
        { "BroadcastKey set to", LogLevel.Trace },
        { "BroadcastKey changed", LogLevel.Trace },
        { "Assembly:", LogLevel.Debug },
        { "Initializing App:", LogLevel.Debug },
        { "Configuring System.Net.ServicePointManager", LogLevel.Debug },
        { "Calling Coder<T>", LogLevel.Debug },
        { "Performing a dummy texture decode", LogLevel.Debug },
        { "Gathering all types", LogLevel.Debug },
        { "Unmapped element", LogLevel.Debug },
        { "SIGNALR: BroadcastSession SessionInfo", LogLevel.Debug },
        { "SIGNALR: BroadcastSessionEnded", LogLevel.Debug },
        { "Updated:", LogLevel.Debug },
        { "FreeImage Version:", LogLevel.Debug },
        { "BepuPhysics Version:", LogLevel.Debug },
        { "FreeType Version:", LogLevel.Debug },
        { "Opus Version:", LogLevel.Debug },
        { "Available locales:", LogLevel.Debug },
        { "Supported network protocols:", LogLevel.Debug },
        { "Supported texture formats:", LogLevel.Debug },
        { "Supported 3D model formats:", LogLevel.Debug },
        { "Supported point cloud formats:", LogLevel.Debug },
        { "Supported audio formats:", LogLevel.Debug },
        { "Supported image formats:", LogLevel.Debug },
        { "Supported video formats:", LogLevel.Debug },
        { "Supported font formats:", LogLevel.Debug },
        { "Supported subtitle formats:", LogLevel.Debug },
        { "HttpClient AutomaticDecompressionSupported:", LogLevel.Debug },
        { "Setting URL:", LogLevel.Debug },
        { "Rebuild:", LogLevel.Debug },
        { "Unused candidates:", LogLevel.Debug },
        { "PreserveWithAssets:", LogLevel.Debug },
        { "Associating self-reference with", LogLevel.Debug },
        { "Injected Start Async", LogLevel.Debug },
        { "ElementSource:", LogLevel.Debug },
        { "INJECTING source for", LogLevel.Debug },
        { "NetworkInitStart", LogLevel.Debug },
        { "Processing commandline arguments", LogLevel.Debug },
        { "Running default bootstrap", LogLevel.Debug },
        { "Session updated, forcing status update", LogLevel.Debug },
        { "SIGNALR: SendMessage", LogLevel.Debug },
        { "SIGNALR: BroadcastStatus", LogLevel.Debug },
        { "SIGNALR: InitializeStatus", LogLevel.Debug },
    };

    private static readonly IReadOnlyDictionary<string, LogLevel> _warningPatterns = new Dictionary<string, LogLevel>();

    private static readonly IReadOnlyDictionary<string, LogLevel> _errorPatterns = new Dictionary<string, LogLevel>
    {
        { "Restoring currently updating root", LogLevel.Debug },
        { "Exception when Updating object", LogLevel.Warning },
        { "Exception getting types from assembly", LogLevel.Warning },
    };

    /// <summary>
    /// Configures host components that require Resonite assemblies.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The host builder, with the configured components.</returns>
    public static IHostBuilder ConfigureResoniteDependentCode(this IHostBuilder hostBuilder) => hostBuilder
        .UseSerilog
        (
            (context, provider, log) =>
            {
                log.ReadFrom.Configuration(context.Configuration);

                // Backwards compatibility
                var config = provider.GetRequiredService<IOptions<ResoniteHeadlessConfig>>().Value;
                if (config.LogsFolder is not null)
                {
                    log.WriteTo.File(Path.Combine(config.LogsFolder, UniLog.GenerateLogName(Engine.VersionNumber)));
                }
            }
        )
        .ConfigureServices
        (
            s => s.AddResoniteControllerServices<ResoniteApplicationController, CustomHeadlessResoniteWorldController>()
                .AddSingleton<HeadlessSystemInfo>()
                .AddSingleton<ISystemInfo>(p => p.GetRequiredService<HeadlessSystemInfo>())
                .AddSingleton<Engine>()
                .AddSingleton<LocalDB>(p => p.GetRequiredService<Engine>().LocalDB)
                .AddSingleton<WorldManager>(p => p.GetRequiredService<Engine>().WorldManager)
                .AddSingleton<WorldService>()
                .AddHostedService<AssetHooverService>()
                .AddHostedService<StandaloneFrooxEngineService>()
        );

    /// <summary>
    /// Configures additional host-external components that require Resonite assemblies.
    /// </summary>
    /// <param name="host">The host.</param>
    public static void PostConfigureHost(this IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        OverrideCecilAssemblyResolver.OverridingAssemblyResolver = host.Services
            .GetRequiredService<ResoniteAssemblyResolver>();

        var flags = host.Services.GetRequiredService<IOptionsMonitor<CommandLineOptions>>().CurrentValue;
        var headlessConfig = host.Services.GetRequiredService<IOptionsMonitor<HeadlessApplicationConfiguration>>()
            .CurrentValue;

        var logFactory = host.Services.GetRequiredService<ILoggerFactory>();

        ForwardedTypeSerialization.Log = logFactory.CreateLogger(typeof(ForwardedTypeSerialization));

        CorrectErrorHandling.Log = logFactory.CreateLogger(typeof(CorrectErrorHandling));
        CorrectErrorHandling.MaxUploadRetries = headlessConfig.MaxUploadRetries ?? 3;
        CorrectErrorHandling.RetryDelay = headlessConfig.RetryDelay ?? TimeSpan.Zero;

        DisableSteamAPI.ShouldAttemptSteamInitialization = headlessConfig.EnableSteam;

        var firstValidPath = headlessConfig.YoutubeDLPaths!.FirstOrDefault(File.Exists);

        ConfigurableYoutubeDLPath.EnableYoutubeDL = headlessConfig.EnableYoutubeDL && firstValidPath is not null;
        if (firstValidPath is not null)
        {
            ConfigurableYoutubeDLPath.YoutubeDLPath = firstValidPath;
        }

        switch (headlessConfig.EnableYoutubeDL)
        {
            case true when firstValidPath is null:
            {
                logger.LogWarning
                (
                    "None of the configured youtube-dl paths appear to be valid. Please configure a valid path or "
                    + "disable youtube-dl integration"
                );

                break;
            }
            case true:
            {
                logger.LogInformation("Using {Path} for youtube-dl integration", firstValidPath);
                break;
            }
        }

        FixConfigInstallationPathHandling.ResoniteRoot = headlessConfig.ResonitePath;

        var harmony = new Harmony("nu.algiz.crystite");
        harmony.PatchAllUncategorized();

        // Generic patches

        // hide command-line args from various types
        RedirectCommandLineParsing.Configure<CommonAvatarBuilder>(_ => { });
        RedirectCommandLineParsing.Configure<DroneCamera>(_ => { });
        RedirectCommandLineParsing.Configure<FirstPersonTargettingController>(_ => { });
        RedirectCommandLineParsing.Configure<InputInterface>(_ => { });
        RedirectCommandLineParsing.Configure<LNL_Connection>(_ => { });

        // set args based on proper configuration keys
        RedirectCommandLineParsing.Configure<LocalDB>(nameof(LocalDB.Initialize), args =>
        {
            if (!flags.RepairDatabase)
            {
                return;
            }

            logger.LogWarning
            (
                "The local database will be repaired. Ensure you remove the --force-sync flag once your instance "
                + "is in a reliable state again"
            );

            args.Add("repairdatabase");
        });

        RedirectCommandLineParsing.Configure<Userspace>(new[] { "OnAttach", "Bootstrap" }, args =>
        {
            args.Add("noui");
            args.Add("skipintrotutorial");
            args.Add("dontautoopencloudhome");

            if (flags.DeleteUnsynced)
            {
                logger.LogWarning
                (
                    "Unsynchronized records will be deleted. Ensure you remove the --delete-unsynced flag once your "
                    + "instance is in a reliable state again"
                );

                args.Add("deleteunsyncedcloudrecords");
            }

            // ReSharper disable once InvertIf
            if (flags.ForceSync)
            {
                logger.LogWarning
                (
                    "Unsynchronized records will be forcibly synced. Ensure you remove the --force-sync flag once your "
                    + "instance is in a reliable state again"
                );

                args.Add("forcesyncconflictingcloudrecords");
            }
        });

        RedirectCommandLineParsing.Configure<UserspaceRadiantDash>(_ => { });

        RedirectCommandLineParsing.PatchAll(harmony);

        SuppressStackTrace.Configure<WorldConfiguration>("FieldChanged");
        SuppressStackTrace.Configure<UserRoot>("Slot_OnPrepareDestroy");
        SuppressStackTrace.PatchAll(harmony);

        UseThreadInterrupt.Configure(AccessTools.Inner(typeof(WorkProcessor), "ThreadWorker"), "Abort");
        UseThreadInterrupt.PatchAll(harmony);

        UniLog.OnLog += message => ClassifyAndLogMessage(logger, message);
        UniLog.OnWarning += warning => ClassifyAndLogWarning(logger, warning);
        UniLog.OnError += error => ClassifyAndLogError(logger, error);
    }

    /// <summary>
    /// Attempts to classify and assign an appropriate log level before actually logging the message. This is done to
    /// reduce unwanted log noise, since UniLog has no concept of a log level.
    ///
    /// Generally, a lot of the information logged by FrooxEngine is entirely irrelevant to an end user, and is
    /// therefore relegated to the lower, more detailed levels.
    /// </summary>
    /// <param name="logger">The logging instance.</param>
    /// <param name="message">The message.</param>
    private static void ClassifyAndLogMessage(ILogger logger, string message)
    {
        message = message.Trim();

        var logLevel = LogLevel.Information;
        foreach (var messagePattern in _messagePatterns.Keys)
        {
            if (message.StartsWith(messagePattern, StringComparison.OrdinalIgnoreCase))
            {
                logLevel = _messagePatterns[messagePattern];
            }
        }

        logger.Log(logLevel, "{Message}", message);
    }

    /// <summary>
    /// Attempts to classify and assign an appropriate log level before actually logging the warning. This is done to
    /// reduce unwanted log noise, since UniLog has no concept of a log level.
    /// </summary>
    /// <param name="logger">The logging instance.</param>
    /// <param name="warning">The warning.</param>
    private static void ClassifyAndLogWarning(ILogger logger, string warning)
    {
        warning = warning.Trim();

        var logLevel = LogLevel.Warning;
        foreach (var warningPattern in _warningPatterns.Keys)
        {
            if (warning.StartsWith(warningPattern, StringComparison.OrdinalIgnoreCase))
            {
                logLevel = _warningPatterns[warningPattern];
            }
        }

        logger.Log(logLevel, "{Message}", warning);
    }

    /// <summary>
    /// Attempts to classify and assign an appropriate log level before actually logging the error. This is done to
    /// reduce unwanted log noise, since UniLog has no concept of a log level.
    ///
    /// Some of the errors produced by the engine are not really errors - a typical example is when a user does
    /// something invalid or silly in-game, causing a node update to fail. Instances like this are, for example, moved
    /// down to warnings.
    /// </summary>
    /// <param name="logger">The logging instance.</param>
    /// <param name="error">The warning.</param>
    private static void ClassifyAndLogError(ILogger logger, string error)
    {
        error = error.Trim();

        var logLevel = LogLevel.Error;
        foreach (var errorPattern in _errorPatterns.Keys)
        {
            if (error.StartsWith(errorPattern, StringComparison.OrdinalIgnoreCase))
            {
                logLevel = _errorPatterns[errorPattern];
            }
        }

        logger.Log(logLevel, "{Message}", error);
    }
}
