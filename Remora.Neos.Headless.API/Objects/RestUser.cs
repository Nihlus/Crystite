//
//  SPDX-FileName: RestUser.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;

namespace Remora.Neos.Headless.API.Objects;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>>
public record RestUser
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("is_present")] bool IsPresent,
    [property: JsonPropertyName("ping")] int Ping
);
