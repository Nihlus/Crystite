//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Text.Json.Serialization;
using Hardware.Info;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Neos.Headless;
using Remora.Neos.Headless.Configuration;

var hardwareInfo = new HardwareInfo();
hardwareInfo.RefreshAll();

NeosAssemblyResolver? assemblyResolver = null;

var hostBuilder = Host.CreateDefaultBuilder(args)
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

hostBuilder
    .ConfigureNeosDependentCode()
    .ConfigureServices
    (
        (c, s) => s
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
            )
    );

var host = hostBuilder.Build();

host.PostConfigureHost();

await host.RunAsync();
