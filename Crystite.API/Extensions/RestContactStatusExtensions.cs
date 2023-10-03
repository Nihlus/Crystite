//
//  SPDX-FileName: RestContactStatusExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using CloudX.Shared;
using Crystite.API.Abstractions;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="RestContactStatus"/> enumeration.
/// </summary>
public static class RestContactStatusExtensions
{
    /// <summary>
    /// Converts a <see cref="RestContactStatus"/> to a <see cref="FriendStatus"/>.
    /// </summary>
    /// <param name="status">The status to convert.</param>
    /// <returns>The converted status.</returns>
    public static FriendStatus ToFriendStatus(this RestContactStatus status) => status switch
    {
        RestContactStatus.None => FriendStatus.None,
        RestContactStatus.Ignored => FriendStatus.Ignored,
        RestContactStatus.Blocked => FriendStatus.Blocked,
        RestContactStatus.Friend => FriendStatus.Accepted,
        RestContactStatus.Requested => FriendStatus.Requested,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
