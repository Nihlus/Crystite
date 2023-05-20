//
//  SPDX-FileName: RestBan.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents information about a banned user.
/// </summary>
[PublicAPI]
public sealed record RestBan
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("machine_id")] string? MachineId = null
);
