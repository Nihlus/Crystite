//
//  SPDX-FileName: RestContactStatus.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Enumerates various states a contact can be in.
/// </summary>
[PublicAPI]
public enum RestContactStatus
{
    /// <summary>
    /// The contact is not on the current account's contact list.
    /// </summary>
    None,

    /// <summary>
    /// The contact is ignored by the current account.
    /// </summary>
    Ignored,

    /// <summary>
    /// The contact is blocked by the current account.
    /// </summary>
    Blocked,

    /// <summary>
    /// The contact is a friend of the current account.
    /// </summary>
    Friend,

    /// <summary>
    /// The contact has been sent a friend request, but they haven't responded yet.
    /// </summary>
    Requested
}
