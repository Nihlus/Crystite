//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaseX;
using FrooxEngine;
using Hardware.Info;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Neos.Headless;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Services;
using Serilog;

var harmony = new Harmony("nu.algiz.remora.neos.headless");
harmony.PatchAll();

var knownNativeLibraryMappings = new Dictionary<string, string[]>
{
    { "assimp", new[] { "libassimp.so.5" } },
    { "freeimage", new[] { "libfreeimage.so.3" } },
    { "freetype6", new[] { "libfreetype.so.6" } },
    { "opus", new[] { "libopus.so.0" } },
    { "dl", new[] { "libdl.so.2" } },
    { "libdl.so", new[] { "libdl.so.2" } },
    { "zlib", new[] { "libzlib.so.1" } },
};

AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
{
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        name = name.ToLowerInvariant();
    }

    // naive approach
    if (NativeLibrary.TryLoad(name, out var handle))
    {
        return handle;
    }

    // check for a versioned library on Linux
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return IntPtr.Zero;
    }

    if (!knownNativeLibraryMappings.TryGetValue(name, out var candidates))
    {
        return IntPtr.Zero;
    }

    foreach (var candidate in candidates)
    {
        if (NativeLibrary.TryLoad(candidate, out handle))
        {
            return handle;
        }
    }

    return IntPtr.Zero;
};

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, provider, log) =>
    {
        log.ReadFrom.Configuration(context.Configuration);

        // Backwards compatibility
        var config = provider.GetRequiredService<IOptions<NeosHeadlessConfig>>().Value;
        if (config.LogsFolder is not null)
        {
            log.WriteTo.File(Path.Combine(config.LogsFolder, UniLog.GenerateLogName(Engine.VersionNumber)));
        }
    })
    .ConfigureServices
    (
        (c, s) => s
            .AddSingleton<IHardwareInfo, HardwareInfo>()
            .AddSingleton<ISystemInfo, HeadlessSystemInfo>()
            .AddSingleton<Engine>()
            .AddHostedService<StandaloneFrooxEngineService>()
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
    )
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
UniLog.OnLog += s => logger.LogInformation("{Message}", s);
UniLog.OnError += s => logger.LogError("{Message}", s);

await host.RunAsync();
