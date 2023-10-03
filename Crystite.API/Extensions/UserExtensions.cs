//
//  SPDX-FileName: UserExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using FrooxEngine;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="User"/> class.
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Converts a <see cref="User"/> to a <see cref="RestUser"/>.
    /// </summary>
    /// <param name="user">The user to convert.</param>
    /// <returns>The <see cref="RestUser"/>.</returns>
    public static RestUser ToRestUser(this User user)
    {
        return new RestUser
        (
            user.UserID,
            user.UserName,
            user.Role?.RoleName.Value,
            user.IsPresentInWorld,
            user.Ping
        );
    }
}
