//
//  SPDX-FileName: RestWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>
[PublicAPI]
public sealed record RestWorld
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("access_level")] RestAccessLevel AccessLevel,
    [property: JsonPropertyName("away_kick_interval")] float AwayKickInterval,
    [property: JsonPropertyName("hide_from_listing")] bool HideFromListing,
    [property: JsonPropertyName("max_users")] int MaxUsers
);
