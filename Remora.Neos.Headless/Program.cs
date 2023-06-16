//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLiners;
using Hardware.Info;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Options;
using Remora.Neos.Headless;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Extensions;
using Remora.Neos.Headless.OptionConfigurators;
using Remora.Rest.Extensions;
using Remora.Rest.Json.Policies;

var hardwareInfo = new HardwareInfo();
hardwareInfo.RefreshCPUList();
hardwareInfo.RefreshVideoControllerList();
hardwareInfo.RefreshMemoryList();

var applicationBuilder = WebApplication.CreateBuilder(args);

var systemConfig = OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
    ? Path.Combine("/", "etc", "remora-neos-headless", "appsettings.json")
    : Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

applicationBuilder.Configuration.AddJsonFile(systemConfig, true);

var headlessConfig = applicationBuilder.Configuration.GetSection("Headless").Get<HeadlessApplicationConfiguration>()
                     ?? new HeadlessApplicationConfiguration();

var assemblyResolver = new NeosAssemblyResolver(new[] { headlessConfig.NeosPath });

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

applicationBuilder.Services.Configure<CommandLineOptions>(o => applicationBuilder.Configuration.Bind(o));

applicationBuilder.Host
    .UseSystemd()
    .ConfigureNeosDependentCode()
    .ConfigureServices
    (
        (config, services) =>
        {
            services.ConfigureRestJsonConverters(ApiJsonMvcJsonOptionsConfigurator.Name);

            services
                .AddSingleton(assemblyResolver ?? throw new InvalidOperationException())
                .AddSingleton<IHardwareInfo>(hardwareInfo)
                .Configure<HeadlessApplicationConfiguration>(config.Configuration.GetSection("Headless"))
                .Configure<NeosHeadlessConfig>(config.Configuration.GetSection("Neos"));

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

                    options.JsonSerializerOptions.AddDataObjectConverter<IJob, Job>()
                        .IncludeWhenSerializing(j => j.Status)
                        .ExcludeWhenSerializing(j => j.Action)
                        .ExcludeWhenSerializing(j => j.TokenSource);

                    options.JsonSerializerOptions.AddDataObjectConverter<IRestBan, RestBan>();
                    options.JsonSerializerOptions.AddDataObjectConverter<IRestContact, RestContact>();
                    options.JsonSerializerOptions.AddDataObjectConverter<IRestWorld, RestWorld>();
                    options.JsonSerializerOptions.AddDataObjectConverter<IRestUser, RestUser>();
                });
        }
    );

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
