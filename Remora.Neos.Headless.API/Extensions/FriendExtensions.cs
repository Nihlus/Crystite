//
//  SPDX-FileName: FriendExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using CloudX.Shared;

namespace Remora.Neos.Headless.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Friend"/> class.
/// </summary>
internal static class FriendExtensions
{
    /// <summary>
    /// Converts a <see cref="Friend"/> to a <see cref="RestFriend"/>.
    /// </summary>
    /// <param name="friend">The <see cref="Friend"/>.</param>
    /// <returns>The <see cref="RestFriend"/>.</returns>
    public static RestFriend ToRestFriend(this Friend friend)
    {
        return new RestFriend(friend.FriendUserId, friend.FriendUsername, friend.FriendStatus, friend.IsAccepted);
    }
}
