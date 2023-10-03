//
//  SPDX-FileName: RestUserRole.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using JetBrains.Annotations;

namespace Crystite.API.Abstractions;

/// <summary>
/// Enumerates valid roles for users.
/// </summary>
[PublicAPI]
public enum RestUserRole
{
    /// <summary>
    /// An administrator with full rights.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// A builder with world modification rights.
    /// </summary>
    Builder = 2,

    /// <summary>
    /// A moderator with user control rights.
    /// </summary>
    Moderator = 3,

    /// <summary>
    /// A guest with interaction rights.
    /// </summary>
    Guest = 4,

    /// <summary>
    /// A spectator without any rights.
    /// </summary>
    Spectator = 5
}
