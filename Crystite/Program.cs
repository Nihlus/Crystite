//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLiners;
using Crystite;
using Crystite.API.Abstractions;
using Crystite.Configuration;
using Crystite.Extensions;
using Crystite.OptionConfigurators;
using Crystite.ResoniteInstallation;
using Hardware.Info;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Options;
using Remora.Extensions.Options.Immutable;
using Remora.Rest.Extensions;
using Remora.Rest.Json.Policies;
using Serilog;
using SteamKit2;

#pragma warning disable ASP0000

var hardwareInfo = new HardwareInfo();
hardwareInfo.RefreshCPUList();
hardwareInfo.RefreshVideoControllerList();
hardwareInfo.RefreshMemoryList();

var applicationBuilder = WebApplication.CreateBuilder(args);

var systemConfigBase = OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
    ? Path.Combine("/", "etc", "crystite")
    : Directory.GetCurrentDirectory();

var systemConfig = Path.Combine(systemConfigBase, "appsettings.json");
applicationBuilder.Configuration.AddJsonFile(systemConfig, true);

var systemConfigDropInDirectory = Path.Combine(systemConfigBase, "conf.d");
if (Directory.Exists(systemConfigDropInDirectory))
{
    var dropInFiles = Directory.EnumerateFiles(systemConfigDropInDirectory, "*.json", SearchOption.TopDirectoryOnly);
    foreach (var dropInFile in dropInFiles.OrderBy(Path.GetFileNameWithoutExtension))
    {
        applicationBuilder.Configuration.AddJsonFile(dropInFile, true);
    }
}

var headlessConfig = applicationBuilder.Configuration.GetSection("Headless").Get<HeadlessApplicationConfiguration>()
                     ?? new HeadlessApplicationConfiguration();

var assemblyResolver = new ResoniteAssemblyResolver(new[] { headlessConfig.ResonitePath });

var oldProvider = applicationBuilder.Configuration.Sources.FirstOrDefault(s => s is CommandLineConfigurationSource);
if (oldProvider is not null)
{
    applicationBuilder.Configuration.Sources.Remove(oldProvider);
}

applicationBuilder.Configuration.AddCommandLineOptions
(
    args.ToPosix<CommandLineOptions>
    (
        map => map
            .Add("force-sync", o => o.ForceSync)
            .Add("delete-unsynced", o => o.DeleteUnsynced)
            .Add("repair-database", o => o.RepairDatabase)
    )
);

applicationBuilder.Host
    .UseSystemd()
    .ConfigureServices
    (
        (config, services) =>
        {
            services.ConfigureRestJsonConverters(ApiJsonMvcJsonOptionsConfigurator.Name);

            services
                .AddHttpClient()
                .AddSingleton(assemblyResolver)
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
                          ?? throw new InvalidOperationException()
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

    if (headlessConfig.ManageResoniteInstallation)
    {
        var installationManager = genericServiceProvider.GetRequiredService<ResoniteInstallationManager>();
        var genericLogger = genericServiceProvider.GetRequiredService<ILogger<Program>>();

        var updateResonite = await installationManager.UpdateResoniteInstallationAsync();
        if (!updateResonite.IsSuccess)
        {
            genericLogger.LogError("Failed to update Resonite: {Error}", updateResonite.Error);
        }
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
