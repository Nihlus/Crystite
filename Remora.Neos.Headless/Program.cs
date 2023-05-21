//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Text.Json.Serialization;
using Hardware.Info;
using Remora.Neos.Headless;
using Remora.Neos.Headless.Configuration;

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
    .ConfigureNeosDependentCode()
    .ConfigureServices
    (
        (c, s) =>
        {
            s
                .AddSingleton(assemblyResolver ?? throw new InvalidOperationException())
                .AddSingleton<IHardwareInfo>(hardwareInfo)
                .Configure<HeadlessApplicationConfiguration>(c.Configuration.GetSection("Headless"))
                .Configure<NeosHeadlessConfig>(c.Configuration.GetSection("Neos"))
                .Configure<JsonSerializerOptions>
                (
                    o =>
                    {
                        o.WriteIndented = true;
                        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    }
                );

            s.AddControllers();
        }
    );

var host = applicationBuilder.Build();

host.MapControllers();

host.PostConfigureHost();

await host.RunAsync();
