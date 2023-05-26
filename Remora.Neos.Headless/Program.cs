//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Text.Json.Serialization;
using Hardware.Info;
using Remora.Neos.Headless;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Extensions;
using Remora.Neos.Headless.OptionConfigurators;
using Remora.Rest.Extensions;
using Remora.Rest.Json.Policies;

#pragma warning disable ASP0013

var hardwareInfo = new HardwareInfo();
hardwareInfo.RefreshAll();

NeosAssemblyResolver? assemblyResolver = null;

var applicationBuilder = WebApplication.CreateBuilder(args);

applicationBuilder.Host
    .ConfigureAppConfiguration
    (
        builder =>
        {
            var config = builder.Build();
            var headlessConfig = config.GetSection("Headless").Get<HeadlessApplicationConfiguration>()
                                 ?? new HeadlessApplicationConfiguration();

            assemblyResolver = new NeosAssemblyResolver(new[] { headlessConfig.NeosPath });
        }
    );

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

host.MapControllers();

host.PostConfigureHost();

await host.RunAsync();
