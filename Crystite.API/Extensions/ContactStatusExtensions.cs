//
//  SPDX-FileName: ContactStatusExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using Crystite.API.Abstractions;
using SkyFrost.Base;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="ContactStatus"/> enumeration.
/// </summary>
public static class ContactStatusExtensions
{
    /// <summary>
    /// Converts a <see cref="ContactStatus"/> to a <see cref="RestContactStatus"/>.
    /// </summary>
    /// <remarks>
    /// Note that this conversion is potentially lossy.
    /// </remarks>
    /// <param name="contactStatus">The status to convert.</param>
    /// <returns>The converted status.</returns>
    public static RestContactStatus ToRestContactStatus(this ContactStatus contactStatus) => contactStatus switch
    {
        ContactStatus.None => RestContactStatus.None,
        ContactStatus.SearchResult => RestContactStatus.None, // TODO: something else or throw?
        ContactStatus.Requested => RestContactStatus.Requested,
        ContactStatus.Ignored => RestContactStatus.Ignored,
        ContactStatus.Blocked => RestContactStatus.Blocked,
        ContactStatus.Accepted => RestContactStatus.Friend,
        _ => throw new ArgumentOutOfRangeException(nameof(contactStatus), contactStatus, null)
    };
}
