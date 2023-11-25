//
//  SPDX-FileName: RestWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

#pragma warning disable SA1402

namespace Crystite.API.Abstractions;

/// <inheritdoc />
[PublicAPI]
public sealed record RestWorld
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("access_level")] RestAccessLevel AccessLevel,
    [property: JsonPropertyName("away_kick_interval")] float AwayKickInterval,
    [property: JsonPropertyName("hide_from_listing")] bool HideFromListing,
    [property: JsonPropertyName("max_users")] int MaxUsers
) : IRestWorld;

/// <summary>
/// Represents information about a world passed over the REST API.
/// </summary>
[PublicAPI]
public interface IRestWorld
{
    /// <summary>
    /// Gets the ID of the world.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the world.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the world, if any.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the access level of the world.
    /// </summary>
    RestAccessLevel AccessLevel { get; }

    /// <summary>
    /// Gets the time after which away users will be kicked.
    /// </summary>
    float AwayKickInterval { get; }

    /// <summary>
    /// Gets a value indicating whether the world should be hidden from public listing.
    /// </summary>
    bool HideFromListing { get; }

    /// <summary>
    /// Gets the maximum number of users allowed in the world.
    /// </summary>
    int MaxUsers { get; }
}
