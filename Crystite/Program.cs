//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite;
using Crystite.Configuration;
using Crystite.Extensions;

#pragma warning disable ASP0000

var resolver = ConfigureAssemblyResolver(args);

return await AspNetDependentStartup.RunAsync(resolver, args);

static ResoniteAssemblyResolver ConfigureAssemblyResolver(string[] args)
{
    var configManager = new ConfigurationManager();
    configManager.ConfigureCrystiteConfigurationSources(args);

    var headlessConfig = configManager.GetSection("Headless").Get<HeadlessApplicationConfiguration>()
                         ?? new HeadlessApplicationConfiguration();

    var assemblyResolver = new ResoniteAssemblyResolver(new[] { headlessConfig.ResonitePath });

    return assemblyResolver;
}
