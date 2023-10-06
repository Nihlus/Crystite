//
//  SPDX-FileName: CustomHeadlessResoniteWorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API;
using Crystite.API.Abstractions;
using Crystite.API.Extensions;
using Crystite.Configuration;
using Crystite.Services;
using FrooxEngine;
using Remora.Results;

namespace Crystite.Implementations;

/// <summary>
/// Implements world-related functionality specific to the custom headless client.
/// </summary>
public class CustomHeadlessResoniteWorldController : ResoniteWorldController
{
    private readonly WorldService _worldService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomHeadlessResoniteWorldController"/> class.
    /// </summary>
    /// <param name="worldManager">The world manager.</param>
    /// <param name="worldService">The world service.</param>
    public CustomHeadlessResoniteWorldController(WorldManager worldManager, WorldService worldService)
        : base(worldManager)
    {
        _worldService = worldService;
    }

    /// <inheritdoc />
    public override async Task<Result<IRestWorld>> StartWorldAsync
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
                LoadWorldURL = worldUrl
            };
        }
        else if (templateName is not null)
        {
            startInfo = new WorldStartupParameters
            {
                LoadWorldPresetName = templateName
            };

            var preset = WorldPresets.Presets.FirstOrDefault<WorldPreset>
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

        var startWorld = await _worldService.StartWorldAsync(startInfo, ct);

        return startWorld.IsDefined(out var session)
            ? session.World.ToRestWorld()
            : Result<IRestWorld>.FromError(startWorld);
    }

    /// <inheritdoc />
    public override Task<Result> CloseWorldAsync(string worldId, CancellationToken ct = default)
        => _worldService.StopWorldAsync(worldId);

    /// <inheritdoc />
    public override async Task<Result<IRestWorld>> RestartWorldAsync(string worldId, CancellationToken ct = default)
    {
        var findWorld = FindWorld(worldId);
        if (!findWorld.IsDefined(out var world))
        {
            return Result<IRestWorld>.FromError(findWorld);
        }

        var restartWorld = await _worldService.RestartWorldAsync(world.SessionId, ct);

        return restartWorld.IsDefined(out var session)
            ? session.World.ToRestWorld()
            : Result<IRestWorld>.FromError(restartWorld);
    }
}
