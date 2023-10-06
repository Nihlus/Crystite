//
//  SPDX-FileName: ContactExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using SkyFrost.Base;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Contact"/> class.
/// </summary>
public static class ContactExtensions
{
    /// <summary>
    /// Converts a <see cref="Contact"/> to a <see cref="RestContact"/>.
    /// </summary>
    /// <param name="friend">The <see cref="Contact"/>.</param>
    /// <returns>The <see cref="RestContact"/>.</returns>
    public static RestContact ToRestContact(this Contact friend)
    {
        return new RestContact(friend.ContactUserId, friend.ContactUsername, friend.ContactStatus.ToRestContactStatus(), friend.IsAccepted);
    }
}
