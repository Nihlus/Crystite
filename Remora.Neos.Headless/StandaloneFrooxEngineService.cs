//
//  SPDX-FileName: StandaloneFrooxEngineService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using FrooxEngine;
using Microsoft.Extensions.Hosting;

namespace Remora.Neos.Headless;

/// <summary>
/// Hosts a standalone FrooxEngine instance.
/// </summary>
public class StandaloneFrooxEngineService : BackgroundService
{
    private readonly Engine _engine;
    private readonly ISystemInfo _systemInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandaloneFrooxEngineService"/> class.
    /// </summary>
    /// <param name="engine">The engine.</param>
    /// <param name="systemInfo">Information about the system.</param>
    public StandaloneFrooxEngineService(Engine engine, ISystemInfo systemInfo)
    {
        _engine = engine;
        _systemInfo = systemInfo;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var assemblyDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location)
                                ?? throw new InvalidOperationException();

        await _engine.Initialize
        (
            assemblyDirectory.FullName,
            Path.Combine(assemblyDirectory.FullName, "Data"),
            Path.Combine(assemblyDirectory.FullName, "Cache"),
            _systemInfo,
            null,
            true
        );

        var userspaceWorld = Userspace.SetupUserspace(_engine);
        using var tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / 60.0));

        _ = userspaceWorld.Coroutines.StartTask(async () => await default(ToWorld));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await tickTimer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _engine.RunUpdateLoop();
        }

        Userspace.ExitNeos(false);
    }
}
