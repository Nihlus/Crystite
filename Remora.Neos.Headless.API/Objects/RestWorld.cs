//
//  SPDX-FileName: RestWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using CloudX.Shared;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>
public record RestWorld
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("access_level")] SessionAccessLevel AccessLevel
);
