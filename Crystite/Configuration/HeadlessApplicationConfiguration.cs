//
//  SPDX-FileName: HeadlessApplicationConfiguration.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Crystite.Configuration;

/// <summary>
/// Represents application-level configuration of the headless client outside of Resonite-defined configuration.
/// </summary>
/// <param name="ResonitePath">The path to the Resonite installation directory.</param>
/// <param name="AssetCleanupInterval">The interval at which to clean up on-disk assets.</param>
/// <param name="MaxAssetAge">The maximum time an asset can be untouched for until it is deleted.</param>
/// <param name="CleanupTypes">The asset types to clean and their associated max ages.</param>
/// <param name="CleanupLocations">The asset locations to clean.</param>
/// <param name="MaxUploadRetries">The maximum number of times a record upload will be retried before it is considered failed.</param>
/// <param name="RetryDelay">The delay between attempts to upload a record again.</param>
/// <param name="Invisible">Whether to set the logged-in user as invisible.</param>
/// <param name="EnableSteam">Whether to enable Steam API integration.</param>
public record HeadlessApplicationConfiguration
(
    string ResonitePath,
    TimeSpan? AssetCleanupInterval = null,
    TimeSpan? MaxAssetAge = null,
    IReadOnlyDictionary<AssetCleanupType, TimeSpan?>? CleanupTypes = null,
    IReadOnlyList<AssetCleanupLocation>? CleanupLocations = null,
    byte? MaxUploadRetries = 3,
    TimeSpan? RetryDelay = null,
    bool Invisible = false,
    bool EnableSteam = false,
    bool EnableYoutubeDL = true,
    IReadOnlyList<string>? YoutubeDLPaths = null
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
