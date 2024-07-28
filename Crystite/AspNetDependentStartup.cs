//
//  SPDX-FileName: AspNetDependentStartup.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crystite.API.Abstractions;
using Crystite.Configuration;
using Crystite.Extensions;
using Crystite.Helpers;
using Crystite.OptionConfigurators;
using Crystite.ResoniteInstallation;
using Hardware.Info;
using Microsoft.Extensions.Options;
using Remora.Extensions.Options.Immutable;
using Remora.Rest.Extensions;
using Remora.Rest.Json.Policies;
using SteamKit2;

namespace Crystite;

/// <summary>
/// Encapsulates ASP.NET-dependent code so we can preload assemblies from the Resonite installation directory before
/// starting up.
/// </summary>
public class AspNetDependentStartup
{
    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="assemblyResolver">The assembly resolver.</param>
    /// <param name="args">The command-line arguments to the application.</param>
    /// <returns>The return code of the application.</returns>
    public static async Task<int> RunAsync(ResoniteAssemblyResolver assemblyResolver, string[] args)
    {
        var hardwareInfo = new HardwareInfo();
        hardwareInfo.RefreshCPUList();
        hardwareInfo.RefreshVideoControllerList();
        hardwareInfo.RefreshMemoryList();

        var applicationBuilder = WebApplication.CreateBuilder(args);

        applicationBuilder.Configuration.ConfigureCrystiteConfigurationSources(args);

        applicationBuilder.Host
            .UseSystemd()
            .ConfigureServices
            (
                (config, services) =>
                {
                    services.ConfigureRestJsonConverters(ApiJsonMvcJsonOptionsConfigurator.Name);

                    services
                        .AddHttpClient()
                        .AddSingleton(assemblyResolver) // keep the resolver alive for the lifetime of the application
                        .AddSingleton<IHardwareInfo>(hardwareInfo)
                        .AddTransient
                        (
                            s => SteamConfiguration.Create
                                (c => c.WithHttpClientFactory(s.GetRequiredService<IHttpClientFactory>().CreateClient))
                        )
                        .AddTransient<SteamClient>()
                        .AddSingleton<ResoniteSteamClient>()
                        .AddSingleton<ResoniteInstallationManager>()
                        .Configure<CommandLineOptions>(o => applicationBuilder.Configuration.Bind(o))
                        .Configure<ResoniteHeadlessConfig>(config.Configuration.GetSection("Resonite"))
                        .Configure
                        (
                            () => config.Configuration
                                .GetSection("Headless")
                                .Get<HeadlessApplicationConfiguration>()
                                  ?? throw new InvalidOperationException("No default configuration available. Check if appsettings.json is available")
                        )
                        .PostConfigure<HeadlessApplicationConfiguration>(c =>
                            {
                                return c with
                                {
                                    YoutubeDLPaths = ResolveYoutubeDLPaths(c.YoutubeDLPaths),
                                    CleanupTypes = c.CleanupTypes ?? new Dictionary<AssetCleanupType, TimeSpan?>
                                    {
                                       { AssetCleanupType.Local, c.MaxAssetAge },
                                       { AssetCleanupType.ResoniteDB, c.MaxAssetAge },
                                       { AssetCleanupType.Other, c.MaxAssetAge }
                                    },
                                    CleanupLocations = c.CleanupLocations?.Distinct().ToArray() ?? new[]
                                    {
                                        AssetCleanupLocation.Data,
                                        AssetCleanupLocation.Cache
                                    }
                                };

                                IReadOnlyList<string> ResolveYoutubeDLPaths(IReadOnlyList<string>? paths)
                                {
                                    if (paths is not null)
                                    {
                                        return paths.Select(Path.GetFullPath).ToArray();
                                    }

                                    // defaults
                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                    {
                                        return new[]
                                        {
                                            Path.GetFullPath(Path.Combine(c.ResonitePath, "RuntimeData", "yt-dlp.exe")),
                                            Path.GetFullPath("yt-dlp.exe"),
                                            Path.GetFullPath(Path.Combine(c.ResonitePath, "RuntimeData", "youtube-dl.exe")),
                                            Path.GetFullPath("youtube-dl.exe")
                                        };
                                    }

                                    return new[]
                                    {
                                        Path.Combine("/", "usr", "bin", "yt-dlp"),
                                        Path.Combine("/", "usr", "local", "bin", "yt-dlp"),
                                        Path.GetFullPath("yt-dlp"),
                                        Path.Combine("/", "usr", "bin", "youtube-dl"),
                                        Path.Combine("/", "usr", "local", "bin", "youtube-dl"),
                                        Path.GetFullPath("youtube-dl"),
                                    };
                                }
                            }
                        );

                    services
                        .AddControllers()
                        .AddJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.WriteIndented = true;
                            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        })
                        .AddApiJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.WriteIndented = false;
                            options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();

                            options.JsonSerializerOptions.Converters.Add
                            (
                                new JsonStringEnumConverter(new SnakeCaseNamingPolicy())
                            );

                            options.JsonSerializerOptions.AddDataObjectConverter<IRestJob, RestJob>();
                            options.JsonSerializerOptions.AddDataObjectConverter<IRestBan, RestBan>();
                            options.JsonSerializerOptions.AddDataObjectConverter<IRestContact, RestContact>();
                            options.JsonSerializerOptions.AddDataObjectConverter<IRestWorld, RestWorld>();
                            options.JsonSerializerOptions.AddDataObjectConverter<IRestUser, RestUser>();
                        });
                }
            );

        // enclose this part in a block to ensure the temporary services used are disposed
        {
            await using var genericServiceProvider = applicationBuilder.Services.BuildServiceProvider();

            var installationManager = genericServiceProvider.GetRequiredService<ResoniteInstallationManager>();
            var genericLogger = genericServiceProvider.GetRequiredService<ILogger<Program>>();

            var headlessConfig = applicationBuilder.Configuration
                                     .GetSection("Headless")
                                     .Get<HeadlessApplicationConfiguration>() ?? new HeadlessApplicationConfiguration();

            if (headlessConfig.ManageResoniteInstallation)
            {
                var updateResonite = await installationManager.UpdateResoniteInstallationAsync();
                if (!updateResonite.IsSuccess)
                {
                    genericLogger.LogError("Failed to update Resonite: {Error}", updateResonite.Error);
                }
            }

            // check the version and notify the user if it doesn't match. We do this unconditionally to catch failed updates as
            // well as non-managed installations
            await CheckAndNotifyResoniteVersionMatch(installationManager, genericLogger);

            var options = genericServiceProvider.GetRequiredService<IOptions<CommandLineOptions>>().Value;
            if (options.InstallOnly)
            {
                genericLogger.LogInformation("Installation only requested; exiting");
                return 0;
            }
        }

        // at this point, the Resonite assemblies *must* be available in the configured directory
        applicationBuilder.Host.ConfigureResoniteDependentCode();

        var host = applicationBuilder.Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        var commandLineOptions = host.Services.GetRequiredService<IOptions<CommandLineOptions>>().Value;
        if (commandLineOptions is { DeleteUnsynced: true, ForceSync: true })
        {
            logger.LogError("--force-sync and --delete-unsynced are mutually exclusive");
            return 1;
        }

        host.MapControllers();

        host.PostConfigureHost();
        await host.RunAsync();

        return 0;
    }

    private static async Task CheckAndNotifyResoniteVersionMatch(ResoniteInstallationManager installationManager, ILogger log)
    {
        var getLocalVersion = await installationManager.GetLocalBuildVersionAsync();
        switch (getLocalVersion)
        {
            case { IsSuccess: false }:
            {
                log.LogWarning("Failed to get the local Resonite version");
                break;
            }
            case { Entity: null }:
            {
                log.LogWarning("No Resonite version information available. Is Resonite properly installed?");
                break;
            }
            case { Entity: not null }:
            {
                if (getLocalVersion.Entity == VersionHelpers.ResoniteVersion)
                {
                    break;
                }

                log.LogWarning
                (
                    "The installed Resonite version ({LocalVersion}) does not match the version Crystite was compiled "
                    + "with ({CompiledVersion}). The program may malfunction unexpectedly, and you should install a "
                    + "compatible version as soon as is convenient",
                    getLocalVersion.Entity,
                    VersionHelpers.ResoniteVersion
                );

                break;
            }
        }
    }
}
