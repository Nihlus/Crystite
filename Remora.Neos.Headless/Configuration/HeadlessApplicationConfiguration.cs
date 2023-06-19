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
/// <param name="MaxUploadRetries">The maximum number of times a record upload will be retried before it is considered failed.</param>
/// <param name="RetryDelay">The delay between attempts to upload a record again.</param>
/// <param name="Invisible">Whether to set the logged-in user as invisible.</param>
/// <param name="EnableSteam">Whether to enable Steam API integration.</param>
public record HeadlessApplicationConfiguration
(
    string NeosPath,
    TimeSpan? AssetCleanupInterval = null,
    TimeSpan? MaxAssetAge = null,
    byte? MaxUploadRetries = 3,
    TimeSpan? RetryDelay = null,
    bool Invisible = false,
    bool EnableSteam = false
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
