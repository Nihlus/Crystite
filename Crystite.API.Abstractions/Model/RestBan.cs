//
//  SPDX-FileName: RestBan.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

#pragma warning disable SA1402

namespace Crystite.API.Abstractions;

/// <inheritdoc />
[PublicAPI]
public sealed record RestBan
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("machine_ids")] IReadOnlyList<string>? MachineIds = null
) : IRestBan;

/// <summary>
/// Represents information about a banned user.
/// </summary>
[PublicAPI]
public interface IRestBan
{
    /// <summary>
    /// Gets the ID of the banned user.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the username of the banned user.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the machine IDs of the banned user, if available.
    /// </summary>
    IReadOnlyList<string>? MachineIds { get; }
}
