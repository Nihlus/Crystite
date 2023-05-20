//
//  SPDX-FileName: FriendStatusExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using CloudX.Shared;
using Remora.Neos.Headless.API.Abstractions;

namespace Remora.Neos.Headless.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="FriendStatus"/> enumeration.
/// </summary>
public static class FriendStatusExtensions
{
    /// <summary>
    /// Converts a <see cref="FriendStatus"/> to a <see cref="RestContactStatus"/>.
    /// </summary>
    /// <remarks>
    /// Note that this conversion is potentially lossy.
    /// </remarks>
    /// <param name="friendStatus">The status to convert.</param>
    /// <returns>The converted status.</returns>
    public static RestContactStatus ToRestContactStatus(this FriendStatus friendStatus) => friendStatus switch
    {
        FriendStatus.None => RestContactStatus.None,
        FriendStatus.SearchResult => RestContactStatus.None, // TODO: something else or throw?
        FriendStatus.Requested => RestContactStatus.Requested,
        FriendStatus.Ignored => RestContactStatus.Ignored,
        FriendStatus.Blocked => RestContactStatus.Blocked,
        FriendStatus.Accepted => RestContactStatus.Friend,
        _ => throw new ArgumentOutOfRangeException(nameof(friendStatus), friendStatus, null)
    };
}
