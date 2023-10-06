//
//  SPDX-FileName: RestAccessLevelExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using Crystite.API.Abstractions;
using SkyFrost.Base;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines various extension methods for the <see cref="RestAccessLevel"/> enumeration.
/// </summary>
public static class RestAccessLevelExtensions
{
    /// <summary>
    /// Converts a <see cref="RestAccessLevel"/> to a <see cref="SessionAccessLevel"/>.
    /// </summary>
    /// <param name="accessLevel">The level to convert.</param>
    /// <returns>The converted level.</returns>
    public static SessionAccessLevel ToSessionAccessLevel(this RestAccessLevel accessLevel) => accessLevel switch
    {
        RestAccessLevel.Private => SessionAccessLevel.Private,
        RestAccessLevel.LAN => SessionAccessLevel.LAN,
        RestAccessLevel.Contacts => SessionAccessLevel.Contacts,
        RestAccessLevel.ContactsPlus => SessionAccessLevel.ContactsPlus,
        RestAccessLevel.RegisteredUsers => SessionAccessLevel.RegisteredUsers,
        RestAccessLevel.Anyone => SessionAccessLevel.Anyone,
        _ => throw new ArgumentOutOfRangeException(nameof(accessLevel), accessLevel, null)
    };
}
