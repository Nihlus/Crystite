//
//  SPDX-FileName: RestUser.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <inheritdoc />
[PublicAPI]
public sealed record RestUser
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("is_present")] bool IsPresent,
    [property: JsonPropertyName("ping")] int Ping
) : IRestUser;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>>
[PublicAPI]
public interface IRestUser
{
    /// <summary>
    /// Gets the ID of the user.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the role of the user, if any.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets a value indicating whether the user is currently present in their headset.
    /// </summary>
    bool IsPresent { get; }

    /// <summary>
    /// Gets the ping of the user.
    /// </summary>
    int Ping { get; }
}
