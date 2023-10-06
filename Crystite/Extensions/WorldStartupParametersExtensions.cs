//
//  SPDX-FileName: WorldStartupParametersExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.Configuration;
using FrooxEngine;
using Remora.Results;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods to the <see cref="WorldStartupParameters"/> class.
/// </summary>
internal static class WorldStartupParametersExtensions
{
    /// <summary>
    /// Creates world start settings from the given startup parameters.
    /// </summary>
    /// <param name="startupParameters">The startup parameters.</param>
    /// <param name="sessionID">The ID of the new session.</param>
    /// <returns>The start settings.</returns>
    public static Result<WorldStartSettings> CreateWorldStartSettings
    (
        this WorldStartupParameters startupParameters,
        string? sessionID
    )
    {
        var startSettings = new WorldStartSettings();
        if (startupParameters.LoadWorldURL is not null)
        {
            startSettings.URIs = new[] { startupParameters.LoadWorldURL };
        }
        else if (startupParameters.LoadWorldPresetName is not null)
        {
            var worldPreset = WorldPresets.Presets.FirstOrDefault
            (
                p => startupParameters.LoadWorldPresetName.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase)
            );

            if (worldPreset is null)
            {
                {
                    return new NotFoundError($"Unknown world preset: {startupParameters.LoadWorldPresetName}");
                }
            }

            startSettings.InitWorld = worldPreset.Method;
        }
        else
        {
            {
                return new InvalidOperationError
                (
                    "No world startup information. At least one of loadWorldUrl or loadWorldPresetName is required."
                );
            }
        }

        startSettings.ForcePort = startupParameters.ForcePort.GetValueOrDefault();
        startSettings.ForceSessionId = sessionID;
        startSettings.DefaultAccessLevel = startupParameters.AccessLevel;
        startSettings.HideFromListing = startupParameters.HideFromPublicListing;
        startSettings.GetExisting = false;
        startSettings.CreateLoadIndicator = false;
        return startSettings;
    }
}
