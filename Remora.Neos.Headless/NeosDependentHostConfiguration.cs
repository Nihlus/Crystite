//
//  SPDX-FileName: NeosDependentHostConfiguration.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using BaseX;
using FrooxEngine;
using HarmonyLib;
using Microsoft.Extensions.Options;
using Remora.Neos.Headless.API.Extensions;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Implementations;
using Remora.Neos.Headless.Patches.Generic;
using Remora.Neos.Headless.Patches.NeosAssemblyPostProcessor;
using Remora.Neos.Headless.Patches.RecordUploadTaskBase;
using Remora.Neos.Headless.Patches.SteamConnector;
using Remora.Neos.Headless.Services;
using Serilog;

namespace Remora.Neos.Headless;

/// <summary>
/// Contains second-stage host configuration, usable only after assembly loading is set up.
/// </summary>
public static class NeosDependentHostConfiguration
{
    /// <summary>
    /// Configures host components that require NeosVR assemblies.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The host builder, with the configured components.</returns>
    public static IHostBuilder ConfigureNeosDependentCode(this IHostBuilder hostBuilder) => hostBuilder
        .UseSerilog
        (
            (context, provider, log) =>
            {
                log.ReadFrom.Configuration(context.Configuration);

                // Backwards compatibility
                var config = provider.GetRequiredService<IOptions<NeosHeadlessConfig>>().Value;
                if (config.LogsFolder is not null)
                {
                    log.WriteTo.File(Path.Combine(config.LogsFolder, UniLog.GenerateLogName(Engine.VersionNumber)));
                }
            }
        )
        .ConfigureServices
        (
            s => s.AddNeosControllerServices<NeosApplicationController, CustomHeadlessNeosWorldController>()
                .AddSingleton<ISystemInfo, HeadlessSystemInfo>()
                .AddSingleton<Engine>()
                .AddSingleton<LocalDB>(p => p.GetRequiredService<Engine>().LocalDB)
                .AddSingleton<WorldManager>(p => p.GetRequiredService<Engine>().WorldManager)
                .AddSingleton<WorldService>()
                .AddHostedService<StandaloneFrooxEngineService>()
                .AddHostedService<AssetHooverService>()
        );

    /// <summary>
    /// Configures additional host-external components that require NeosVR assemblies.
    /// </summary>
    /// <param name="host">The host.</param>
    public static void PostConfigureHost(this IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        OverrideCecilAssemblyResolver.OverridingAssemblyResolver = host.Services
            .GetRequiredService<NeosAssemblyResolver>();

        var flags = host.Services.GetRequiredService<IOptionsMonitor<CommandLineOptions>>().CurrentValue;
        var headlessConfig = host.Services.GetRequiredService<IOptionsMonitor<HeadlessApplicationConfiguration>>()
            .CurrentValue;
        var neosConfig = host.Services.GetRequiredService<IOptionsMonitor<NeosHeadlessConfig>>().CurrentValue;

        var logFactory = host.Services.GetRequiredService<ILoggerFactory>();

        CorrectErrorHandling.Log = logFactory.CreateLogger(typeof(CorrectErrorHandling));
        CorrectErrorHandling.MaxUploadRetries = headlessConfig.MaxUploadRetries ?? 3;
        CorrectErrorHandling.RetryDelay = headlessConfig.RetryDelay ?? TimeSpan.Zero;

        DisableSteamAPI.ShouldAttemptSteamInitialization = headlessConfig.EnableSteam;

        var harmony = new Harmony("nu.algiz.remora.neos.headless");
        harmony.PatchAllUncategorized();

        // Generic patches
        RedirectCommandLineParsing.Configure<InputInterface>(_ => { });

        RedirectCommandLineParsing.Configure<Engine>(nameof(Engine.Initialize), args =>
        {
            if (neosConfig.PluginAssemblies is not null)
            {
                foreach (var pluginAssembly in neosConfig.PluginAssemblies)
                {
                    args.Add("LoadAssembly");
                    args.Add(pluginAssembly);
                }
            }

            if (neosConfig.GeneratePreCache is true)
            {
                args.Add("GeneratePreCache");
            }

            if (neosConfig.BackgroundWorkers is { } backgroundWorkers)
            {
                args.Add("backgroundworkers");
                args.Add(backgroundWorkers.ToString());
            }

            // ReSharper disable once InvertIf
            if (neosConfig.PriorityWorkers is { } priorityWorkers)
            {
                args.Add("priorityworkers");
                args.Add(priorityWorkers.ToString());
            }
        });

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

        RedirectCommandLineParsing.Configure<EngineCloudXInterface>(_ => { });

        RedirectCommandLineParsing.Configure<StatusManager>(args =>
        {
            if (headlessConfig.Invisible)
            {
                args.Add("invisible");
            }
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

        RedirectCommandLineParsing.Configure<CommonAvatarBuilder>(_ => { });
        RedirectCommandLineParsing.PatchAll(harmony);

        SuppressStackTrace.Configure<WorldConfiguration>("FieldChanged");
        SuppressStackTrace.PatchAll(harmony);

        UseThreadInterrupt.Configure(AccessTools.Inner(typeof(WorkProcessor), "ThreadWorker"), "Abort");
        UseThreadInterrupt.Configure<WorldAnnouncer>(nameof(WorldAnnouncer.Dispose));
        UseThreadInterrupt.PatchAll(harmony);

        UniLog.OnLog += s => logger.LogInformation("{Message}", s);
        UniLog.OnError += s => logger.LogError("{Message}", s);
    }
}
