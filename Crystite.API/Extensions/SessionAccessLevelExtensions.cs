//
//  SPDX-FileName: SessionAccessLevelExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using Crystite.API.Abstractions;
using SkyFrost.Base;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines various extension methods for the <see cref="SessionAccessLevel"/> enumeration.
/// </summary>
public static class SessionAccessLevelExtensions
{
    /// <summary>
    /// Converts a <see cref="SessionAccessLevel"/> to a <see cref="RestAccessLevel"/>.
    /// </summary>
    /// <param name="accessLevel">The level to convert.</param>
    /// <returns>The converted level.</returns>
    public static RestAccessLevel ToRestAccessLevel(this SessionAccessLevel accessLevel) => accessLevel switch
    {
        SessionAccessLevel.Private => RestAccessLevel.Private,
        SessionAccessLevel.LAN => RestAccessLevel.LAN,
        SessionAccessLevel.Contacts => RestAccessLevel.Contacts,
        SessionAccessLevel.ContactsPlus => RestAccessLevel.ContactsPlus,
        SessionAccessLevel.RegisteredUsers => RestAccessLevel.RegisteredUsers,
        SessionAccessLevel.Anyone => RestAccessLevel.Anyone,
        _ => throw new ArgumentOutOfRangeException(nameof(accessLevel), accessLevel, null)
    };
}
