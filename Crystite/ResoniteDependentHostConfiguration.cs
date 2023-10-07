//
//  SPDX-FileName: ResoniteDependentHostConfiguration.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Extensions;
using Crystite.Configuration;
using Crystite.Implementations;
using Crystite.Patches.Generic;
using Crystite.Patches.RecordUploadTaskBase;
using Crystite.Patches.ResoniteAssemblyPostProcessor;
using Crystite.Patches.SteamConnector;
using Crystite.Patches.VideoTextureProvider;
using Crystite.Services;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Microsoft.Extensions.Options;
using Serilog;

namespace Crystite;

/// <summary>
/// Contains second-stage host configuration, usable only after assembly loading is set up.
/// </summary>
public static class ResoniteDependentHostConfiguration
{
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

        var harmony = new Harmony("nu.algiz.crystite");
        harmony.PatchAllUncategorized();

        // Generic patches
        RedirectCommandLineParsing.Configure<InputInterface>(_ => { });

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

        RedirectCommandLineParsing.Configure<EngineSkyFrostInterface>(_ => { });
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

        RedirectCommandLineParsing.Configure<CommonAvatarBuilder>(_ => { });
        RedirectCommandLineParsing.PatchAll(harmony);

        SuppressStackTrace.Configure<WorldConfiguration>("FieldChanged");
        SuppressStackTrace.Configure<UserRoot>("Slot_OnPrepareDestroy");
        SuppressStackTrace.PatchAll(harmony);

        UseThreadInterrupt.Configure(AccessTools.Inner(typeof(WorkProcessor), "ThreadWorker"), "Abort");
        UseThreadInterrupt.PatchAll(harmony);

        UniLog.OnLog += s => logger.LogInformation("{Message}", s);
        UniLog.OnError += s => logger.LogError("{Message}", s);
    }
}
