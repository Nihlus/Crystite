//
//  SPDX-FileName: ConfigurationManagerExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using CommandLiners;
using Crystite.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Crystite.Extensions;

/// <summary>
/// Defines various extension methods for the <see cref="ConfigurationManager"/> class.
/// </summary>
public static class ConfigurationManagerExtensions
{
    /// <summary>
    /// Configures Crystite-specific configuration sources.
    /// </summary>
    /// <param name="config">The configuration manager.</param>
    /// <param name="args">The command-line arguments passed to the application.</param>
    public static void ConfigureCrystiteConfigurationSources(this ConfigurationManager config, string[] args)
    {
        config.AddEnvironmentVariables();

        var systemConfigBase = OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
            ? Path.Combine("/", "etc", "crystite")
            : Directory.GetCurrentDirectory();

        var systemConfig = Path.Combine(systemConfigBase, "appsettings.json");
        config.AddJsonFile(systemConfig, true);

        var systemConfigDropInDirectory = Path.Combine(systemConfigBase, "conf.d");
        if (Directory.Exists(systemConfigDropInDirectory))
        {
            var dropInFiles = Directory.EnumerateFiles(systemConfigDropInDirectory, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var dropInFile in dropInFiles.OrderBy(Path.GetFileNameWithoutExtension))
            {
                config.AddJsonFile(dropInFile, true);
            }
        }

        var oldProvider = config.Sources.FirstOrDefault(s => s is CommandLineConfigurationSource);
        if (oldProvider is not null)
        {
            config.Sources.Remove(oldProvider);
        }

        config.AddCommandLineOptions
        (
            args.ToPosix<CommandLineOptions>
            (
                map => map
                    .Add("force-sync", o => o.ForceSync)
                    .Add("delete-unsynced", o => o.DeleteUnsynced)
                    .Add("repair-database", o => o.RepairDatabase)
                    .Add("install-only", o => o.InstallOnly)
                    .Add("allow-unsupported-resonite-version", o => o.AllowUnsupportedResoniteVersion)
            )
        );
    }
}
