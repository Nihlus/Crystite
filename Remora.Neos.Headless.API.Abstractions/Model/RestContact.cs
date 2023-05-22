//
//  SPDX-FileName: RestContact.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <inheritdoc />
[PublicAPI]
public sealed record RestContact
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("status")] RestContactStatus Status,
    [property: JsonPropertyName("is_accepted")] bool IsAccepted
) : IRestContact;

/// <summary>
/// Represents information about a friend passed over the REST API.
/// </summary>
[PublicAPI]
public interface IRestContact
{
    /// <summary>
    /// Gets the ID of the contact.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the username of the contact.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the status of the contact.
    /// </summary>
    RestContactStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether the contact's friend request has been accepted.
    /// </summary>
    bool IsAccepted { get; }
}
