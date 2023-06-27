//
//  SPDX-FileName: AssetCleanupLocation.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Remora.Neos.Headless.Configuration;

/// <summary>
/// Specifies one or more built-in locations to consider when cleaning up assets.
/// </summary>
public enum AssetCleanupLocation
{
    /// <summary>
    /// Clean up assets in the data folder.
    /// </summary>
    Data,

    /// <summary>
    /// Clean up assets in the cache folder.
    /// </summary>
    Cache
}
