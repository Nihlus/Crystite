//
//  SPDX-FileName: StockHeadlessNeosWorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudX.Shared;
using FrooxEngine;
using NeosHeadless;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Extensions;
using Remora.Results;

namespace Remora.Neos.Headless.API.Mod.Implementations;

/// <summary>
/// Implements world-related functionality specific to the stock headless client.
/// </summary>
public class StockHeadlessNeosWorldController : NeosWorldController
{
    private readonly NeosHeadlessConfig _config;
    private readonly Engine _engine;
    private readonly WorldManager _worldManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StockHeadlessNeosWorldController"/> class.
    /// </summary>
    /// <param name="worldManager">The world manager.</param>
    /// <param name="config">The headless config.</param>
    /// <param name="engine">The game engine.</param>
    public StockHeadlessNeosWorldController(WorldManager worldManager, NeosHeadlessConfig config, Engine engine)
        : base(worldManager)
    {
        _config = config;
        _engine = engine;
        _worldManager = worldManager;
    }

    /// <inheritdoc />
    public override async Task<Result<RestWorld>> StartWorldAsync
    (
        Uri? worldUrl = null,
        string? templateName = null,
        CancellationToken ct = default
    )
    {
        WorldStartupParameters startInfo;

        if (worldUrl is not null)
        {
            startInfo = new WorldStartupParameters
            {
                LoadWorldURL = worldUrl.ToString()
            };
        }
        else if (templateName is not null)
        {
            startInfo = new WorldStartupParameters
            {
                LoadWorldPresetName = templateName
            };

            var preset = (await WorldPresets.GetPresets()).FirstOrDefault<WorldPreset>
            (
                p =>
                {
                    var name = p.Name;
                    return name != null && name.Equals
                    (
                        startInfo.LoadWorldPresetName,
                        StringComparison.InvariantCultureIgnoreCase
                    );
                }
            );

            if (preset == null)
            {
                return new NotFoundError("No matching world preset found.");
            }
        }
        else
        {
            return new InvalidOperationException
            (
                $"Either {nameof(worldUrl)} or {nameof(templateName)} must be provided."
            );
        }

        lock (_config)
        {
            _config.StartWorlds ??= new List<WorldStartupParameters>();
            _config.StartWorlds.Add(startInfo);
        }

        // that's the way it is, chief
        // ReSharper disable once InconsistentlySynchronizedField
        var handler = new WorldHandler(_engine, _config, startInfo);
        await handler.Start();

        return handler.CurrentInstance.ToRestWorld();
    }

    /// <inheritdoc />
    public override async Task<Result<RestWorld>> RestartWorldAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return new NotFoundError("No matching world found.");
        }

        var handler = WorldHandler.GetHandler(world);
        if (handler == null)
        {
            return new NotFoundError("No matching world found.");
        }

        var restartedWorld = await handler.Restart();
        return restartedWorld.ToRestWorld();
    }
}
