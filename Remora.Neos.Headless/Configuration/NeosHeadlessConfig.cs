//
//  SPDX-FileName: NeosHeadlessConfig.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;

namespace Remora.Neos.Headless.Configuration;

/// <summary>
/// Options used to configure a Neos Headless server. Read in from a JSON file on Headless start.
/// </summary>
/// <a href="https://raw.githubusercontent.com/Neos-Metaverse/JSONSchemas/main/schemas/NeosHeadlessConfig.schema.json"/>
/// <param name="Comment">An optional free form comment for this file. Used for identification for your configuration.</param>
/// <param name="UniverseID">Optionally, specifies which universe this Headless Server will be in. See our wiki article on Universes for more info.</param>
/// <param name="TickRate">Configures how many ticks(updates), should occur per second. Default is 60.</param>
/// <param name="MaxConcurrentAssetTransfers">Specifies the maximum concurrent asset transfers for this headless server. Default is 4.</param>
/// <param name="UsernameOverride">Configures a username which will override LoginCredential, within the world browser etc.</param>
/// <param name="LoginCredential">Neos username for this Headless Server to use.</param>
/// <param name="LoginPassword">Neos user password for the Headless Server to use.</param>
/// <param name="StartWorlds">A list of worlds/sessions to start/create when this Headless Server starts.</param>
/// <param name="DataFolder">Optionally, override the folder which Neos will use to store data for this Headless Server.</param>
/// <param name="CacheFolder">Optionally, override the folder which Neos will use to store cache for this Headless Server.</param>
/// <param name="LogsFolder">Optionally, override the folder which Neos will use to store logs for this Headless Server.</param>
/// <param name="AllowedUrlHosts">A list of hosts which will automatically be allowed for in-Neos HTTP/WebSocket operations.</param>
/// <param name="AutoSpawnItems">A list of item URIs to spawn in when a world starts.</param>
public record NeosHeadlessConfig
(
    string? Comment = null,
    string? UniverseID = null,
    float TickRate = 60,
    int MaxConcurrentAssetTransfers = 4,
    string? UsernameOverride = null,
    string? LoginCredential = null,
    string? LoginPassword = null,
    IReadOnlyList<WorldStartupParameters>? StartWorlds = null,
    string? DataFolder = null,
    string? CacheFolder = null,
    string? LogsFolder = null,
    IReadOnlyList<Uri>? AllowedUrlHosts = null,
    IReadOnlyList<Uri>? AutoSpawnItems = null
)
{
    /// <summary>
    /// Gets the JSON schema for this file.
    /// </summary>
    [JsonInclude]
    public static Uri Schema => new
    (
        "https://raw.githubusercontent.com/Neos-Metaverse/JSONSchemas/main/schemas/NeosHeadlessConfig.schema.json"
    );

    /// <summary>
    /// Gets the default data folder.
    /// </summary>
    [JsonIgnore]
    public static string DefaultDataFolder => Path.Combine(Directory.GetCurrentDirectory(), "Data");

    /// <summary>
    /// Gets the default cache folder.
    /// </summary>
    [JsonIgnore]
    public static string DefaultCacheFolder => Path.Combine(Directory.GetCurrentDirectory(), "Cache");

    /// <summary>
    /// Gets the default log folder.
    /// </summary>
    [JsonIgnore]
    public static string DefaultLogsFolder => Path.Combine(Directory.GetCurrentDirectory(), "Cache");

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosHeadlessConfig"/> class.
    /// </summary>
    public NeosHeadlessConfig()
        : this(Comment: null) // force overload resolution
    {
    }
}
