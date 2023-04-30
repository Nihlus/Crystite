//
//  SPDX-FileName: RestFriend.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using CloudX.Shared;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Represents information about a friend passed over the REST API.
/// </summary>
[PublicAPI]
public sealed record RestFriend
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("friend_status")] FriendStatus FriendStatus,
    [property: JsonPropertyName("is_accepted")] bool IsAccepted
);
