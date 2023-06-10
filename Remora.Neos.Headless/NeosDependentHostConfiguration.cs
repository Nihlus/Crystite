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
using Remora.Neos.Headless.Patches.Engine;
using Remora.Neos.Headless.Patches.NeosAssemblyPostProcessor;
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
                .AddSingleton<WorldManager>(p => p.GetRequiredService<Engine>().WorldManager)
                .AddSingleton<WorldService>()
                .AddHostedService<StandaloneFrooxEngineService>()
        );

    /// <summary>
    /// Configures additional host-external components that require NeosVR assemblies.
    /// </summary>
    /// <param name="host">The host.</param>
    public static void PostConfigureHost(this IHost host)
    {
        var config = host.Services.GetRequiredService<IOptionsMonitor<NeosHeadlessConfig>>();
        RedirectCommandLineParsing.SetRedirectedCommandLine(config.CurrentValue);

        OverrideCecilAssemblyResolver.OverridingAssemblyResolver = host.Services
            .GetRequiredService<NeosAssemblyResolver>();

        var harmony = new Harmony("nu.algiz.remora.neos.headless");
        harmony.PatchAll();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        UniLog.OnLog += s => logger.LogInformation("{Message}", s);
        UniLog.OnError += s => logger.LogError("{Message}", s);
    }
}
