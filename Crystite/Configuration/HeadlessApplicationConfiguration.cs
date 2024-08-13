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
/// <param name="EnableYoutubeDL">Whether to enable YoutubeDL integration.</param>
/// <param name="YoutubeDLPaths">The paths to consider when looking for valid YoutubeDL executables.</param>
/// <param name="ManageResoniteInstallation">Whether to automatically install and update the Resonite installation used by the program.</param>
/// <param name="SteamCredential">The username to use when logging into Steam.</param>
/// <param name="SteamPassword">The password to use when logging into Steam.</param>
/// <param name="AllowUnsafeHosts">
/// Whether to allow preemptive whitelisting of unsafe API hosts. Enabling this option is functionally the same as
/// giving everyone that joins any session hosted by the headless full control over the headless. You should not enable
/// this option unless you have complete trust in everyone who joins your sessions and any and all items they bring with
/// them.
/// </param>
/// <param name="SteamBranch">The name of the Steam branch to download the game from.</param>
/// <param name="SteamBranchCode">The access code to use when downloading the game.</param>
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
    IReadOnlyList<string>? YoutubeDLPaths = null,
    bool ManageResoniteInstallation = false,
    string? SteamCredential = null,
    string? SteamPassword = null,
    bool AllowUnsafeHosts = false,
    string SteamBranch = "public",
    string? SteamBranchCode = null
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
