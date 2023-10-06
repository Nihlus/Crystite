//
//  SPDX-FileName: RestAccessLevel.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Crystite.API.Abstractions;

/// <summary>
/// Enumerates various access levels for worlds.
/// </summary>
public enum RestAccessLevel
{
    /// <summary>
    /// The world is private and only the session owner can join it.
    /// </summary>
    Private,

    /// <summary>
    /// People on the same local network can join the world.
    /// </summary>
    LAN,

    /// <summary>
    /// Contacts of the session owner can join the world.
    /// </summary>
    Contacts,

    /// <summary>
    /// Contacts and contacts of contacts of the session owner can join the world.
    /// </summary>
    ContactsPlus,

    /// <summary>
    /// Registered users can join the world.
    /// </summary>
    RegisteredUsers,

    /// <summary>
    /// Anyone can join the world.
    /// </summary>
    Anyone,
}
