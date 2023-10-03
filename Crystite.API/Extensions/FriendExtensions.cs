//
//  SPDX-FileName: FriendExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using CloudX.Shared;
using Crystite.API.Abstractions;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Friend"/> class.
/// </summary>
public static class FriendExtensions
{
    /// <summary>
    /// Converts a <see cref="Friend"/> to a <see cref="RestContact"/>.
    /// </summary>
    /// <param name="friend">The <see cref="Friend"/>.</param>
    /// <returns>The <see cref="RestContact"/>.</returns>
    public static RestContact ToRestContact(this Friend friend)
    {
        return new RestContact(friend.FriendUserId, friend.FriendUsername, friend.FriendStatus.ToRestContactStatus(), friend.IsAccepted);
    }
}
