//
//  SPDX-FileName: WorldExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using FrooxEngine;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="World"/> class.
/// </summary>
public static class WorldExtensions
{
    /// <summary>
    /// Converts a <see cref="World"/> to a <see cref="RestWorld"/>.
    /// </summary>
    /// <param name="world">The world to convert.</param>
    /// <returns>The <see cref="RestWorld"/>.</returns>
    public static RestWorld ToRestWorld(this World world)
    {
        return new RestWorld
        (
            world.SessionId,
            world.Name,
            world.Description,
            world.AccessLevel.ToRestAccessLevel(),
            world.AwayKickMinutes,
            world.HideFromListing,
            world.MaxUsers
        );
    }
}
