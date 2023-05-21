//
//  SPDX-FileName: CustomHeadlessNeosWorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;
using Remora.Neos.Headless.API;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Extensions;
using Remora.Neos.Headless.Configuration;
using Remora.Neos.Headless.Services;
using Remora.Results;

namespace Remora.Neos.Headless.Implementations;

/// <summary>
/// Implements world-related functionality specific to the custom headless client.
/// </summary>
public class CustomHeadlessNeosWorldController : NeosWorldController
{
    private readonly WorldService _worldService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomHeadlessNeosWorldController"/> class.
    /// </summary>
    /// <param name="worldManager">The world manager.</param>
    /// <param name="worldService">The world service.</param>
    public CustomHeadlessNeosWorldController(WorldManager worldManager, WorldService worldService)
        : base(worldManager)
    {
        _worldService = worldService;
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
                LoadWorldURL = worldUrl
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

        var startWorld = await _worldService.StartWorldAsync(startInfo, ct);

        return startWorld.IsDefined(out var session)
            ? session.World.ToRestWorld()
            : Result<RestWorld>.FromError(startWorld);
    }

    /// <inheritdoc />
    public override async Task<Result<RestWorld>> RestartWorldAsync(string worldId, CancellationToken ct = default)
    {
        var restartWorld = await _worldService.RestartWorldAsync(worldId, ct);

        return restartWorld.IsDefined(out var session)
            ? session.World.ToRestWorld()
            : Result<RestWorld>.FromError(restartWorld);
    }
}
