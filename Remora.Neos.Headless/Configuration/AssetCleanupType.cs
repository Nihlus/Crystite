//
//  SPDX-FileName: AssetCleanupType.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Remora.Neos.Headless.Configuration;

/// <summary>
/// Specifies one or more built-in asset types to consider when cleaning up assets.
/// </summary>
public enum AssetCleanupType
{
    /// <summary>
    /// Clean up assets with a local URI.
    /// </summary>
    Local,

    /// <summary>
    /// Clean up assets with a NeosDB URI.
    /// </summary>
    NeosDB,

    /// <summary>
    /// Clean up assets with a URI not matching any of the other categories.
    /// </summary>
    Other
}
