//
//  SPDX-FileName: RestContactStatusExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using Crystite.API.Abstractions;
using SkyFrost.Base;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="RestContactStatus"/> enumeration.
/// </summary>
public static class RestContactStatusExtensions
{
    /// <summary>
    /// Converts a <see cref="RestContactStatus"/> to a <see cref="ContactStatus"/>.
    /// </summary>
    /// <param name="status">The status to convert.</param>
    /// <returns>The converted status.</returns>
    public static ContactStatus ToContactStatus(this RestContactStatus status) => status switch
    {
        RestContactStatus.None => ContactStatus.None,
        RestContactStatus.Ignored => ContactStatus.Ignored,
        RestContactStatus.Blocked => ContactStatus.Blocked,
        RestContactStatus.Friend => ContactStatus.Accepted,
        RestContactStatus.Requested => ContactStatus.Requested,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
