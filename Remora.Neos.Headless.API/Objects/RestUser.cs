//
//  SPDX-FileName: RestUser.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>>
[PublicAPI]
public sealed record RestUser
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("is_present")] bool IsPresent,
    [property: JsonPropertyName("ping")] int Ping
);
