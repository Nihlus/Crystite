//
//  SPDX-FileName: RestContact.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents information about a friend passed over the REST API.
/// </summary>
[PublicAPI]
public sealed record RestContact
(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("status")] RestContactStatus Status,
    [property: JsonPropertyName("is_accepted")] bool IsAccepted
);
