//
//  SPDX-FileName: HeadlessApplicationConfiguration.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Remora.Neos.Headless.Configuration;

/// <summary>
/// Represents application-level configuration of the headless client outside of NeosVR-defined configuration.
/// </summary>
/// <param name="NeosPath">The path to the NeosVR installation directory.</param>
/// <param name="AssetCleanupInterval">The interval at which to clean up on-disk assets.</param>
/// <param name="MaxAssetAge">The maximum time an asset can be untouched for until it is deleted.</param>
public record HeadlessApplicationConfiguration
(
    string NeosPath,
    TimeSpan? AssetCleanupInterval = null,
    TimeSpan? MaxAssetAge = null
)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessApplicationConfiguration"/> class.
    /// </summary>
    public HeadlessApplicationConfiguration()
        : this(AppDomain.CurrentDomain.BaseDirectory)
    {
    }
}
