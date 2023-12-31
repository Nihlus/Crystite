//
//  SPDX-FileName: ResoniteInstallationManager.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Crystite.Configuration;
using Crystite.Helpers;
using Humanizer;
using Microsoft.Extensions.Options;
using Remora.Results;

namespace Crystite.ResoniteInstallation;

/// <summary>
/// Handles installation and updating of Resonite from Steam depots.
/// </summary>
public sealed class ResoniteInstallationManager
{
    private readonly ILogger<ResoniteInstallationManager> _log;
    private readonly ResoniteSteamClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HeadlessApplicationConfiguration _config;

    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteInstallationManager"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="client">The Resonite steam client.</param>
    /// <param name="jsonOptions">The JSON serialization config.</param>
    /// <param name="config">The application configuration.</param>
    public ResoniteInstallationManager(ILogger<ResoniteInstallationManager> log, ResoniteSteamClient client, IOptions<JsonSerializerOptions> jsonOptions, IOptions<HeadlessApplicationConfiguration> config)
    {
        _log = log;
        _client = client;
        _jsonOptions = jsonOptions.Value;
        _config = config.Value;
    }

    /// <summary>
    /// Updates the Resonite installation, either installing it from scratch or applying required updates from Steam.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    public async Task<Result> UpdateResoniteInstallationAsync(CancellationToken ct = default)
    {
        if (!_isInitialized)
        {
            var initialize = await _client.InitializeAsync(ct);
            if (!initialize.IsSuccess)
            {
                return initialize;
            }

            _isInitialized = true;
        }

        _log.LogInformation("Checking for Resonite updates");

        var getDepots = await _client.GetDepotsAsync();
        if (!getDepots.IsDefined(out var depots))
        {
            return (Result)getDepots;
        }

        var getManifests = await Task.WhenAll(depots.Select(d => _client.GetManifestAsync(d, ct)));
        var manifestBuilder = new ResoniteManifestBuilder();
        foreach (var getManifest in getManifests)
        {
            if (!getManifest.IsDefined(out var manifest))
            {
                return (Result)getManifest;
            }

            manifestBuilder.PushManifest(manifest);
        }

        var newAppManifest = manifestBuilder.Build();
        var depotKeys = depots.ToDictionary(d => d.ID, d => d.Key);

        var getLocalVersion = await GetLocalBuildVersionAsync();
        if (!getLocalVersion.IsSuccess)
        {
            return (Result)getLocalVersion;
        }

        var getLatestVersion = await GetLatestBuildVersion(newAppManifest, depotKeys, ct);
        if (!getLatestVersion.IsDefined(out var latestVersion))
        {
            return (Result)getLatestVersion;
        }

        var isLatestNewer = latestVersion > VersionHelpers.ResoniteVersion;
        if (isLatestNewer)
        {
            _log.LogWarning("The latest Resonite version is newer than the one Crystite is built against");
            _log.LogWarning("Cowardly refusing to install a version we might not be compatible with");

            return Result.FromSuccess();
        }

        var localVersion = getLocalVersion.Entity;
        if (localVersion == latestVersion)
        {
            _log.LogInformation("Resonite is up to date");
            return Result.FromSuccess();
        }

        if (localVersion is null)
        {
            _log.LogInformation
            (
                "Installing Resonite {Version} into {ResoniteRoot}",
                latestVersion,
                _config.ResonitePath
            );
        }
        else
        {
            _log.LogInformation("Updating Resonite from {OldVersion} to {NewVersion}", localVersion, latestVersion);
        }

        var updateResonite = await UpdateResoniteAsync(depotKeys, newAppManifest, ct);
        if (!updateResonite.IsSuccess)
        {
            return updateResonite;
        }

        _log.LogInformation("Installed Resonite {Version}", latestVersion);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets the version of the local Resonite installation.
    /// </summary>
    /// <returns>
    /// The version, or <value>null</value> if no version information is available (typically due to Resonite not being
    /// installed).
    /// </returns>
    public async Task<Result<Version?>> GetLocalBuildVersionAsync()
    {
        var localVersionPath = Path.Combine(_config.ResonitePath, "Build.version");
        if (!File.Exists(localVersionPath))
        {
            return (Version?)null;
        }

        var contents = await File.ReadAllTextAsync(localVersionPath);
        if (!Version.TryParse(contents, out var version))
        {
            return new InvalidOperationError("Failed to parse the contents of the local build version file");
        }

        return version;
    }

    private async Task<Result<Version>> GetLatestBuildVersion
    (
        ResoniteManifest newAppManifest,
        IReadOnlyDictionary<uint, byte[]> depotKeys,
        CancellationToken ct = default
    )
    {
        var versionFile = newAppManifest.Files.FirstOrDefault
        (
            f => Path.GetFileName(f.Path).Equals("Build.version", StringComparison.OrdinalIgnoreCase)
        );

        if (versionFile is null)
        {
            return new NotFoundError("Could not find the build version file in the remote manifest");
        }

        var getContents = await _client.GetFileAsync
        (
            versionFile.DepotID,
            depotKeys[versionFile.DepotID],
            versionFile,
            ct
        );

        if (!getContents.IsDefined(out var contents))
        {
            return Result<Version>.FromError(getContents);
        }

        var contentsAsString = Encoding.UTF8.GetString(contents).Trim();
        if (!Version.TryParse(contentsAsString, out var version))
        {
            return new InvalidOperationError("Failed to parse the contents of the remote build version file");
        }

        return version;
    }

    private async Task<Result> UpdateResoniteAsync
    (
        IReadOnlyDictionary<uint, byte[]> depotKeys,
        ResoniteManifest newAppManifest,
        CancellationToken ct = default
    )
    {
        var getChanges = await GetChangesAsync(newAppManifest);
        if (!getChanges.IsDefined(out var changes))
        {
            return (Result)getChanges;
        }

        var deleteOldFiles = DeleteOldData(changes);
        if (!deleteOldFiles.IsSuccess)
        {
            return deleteOldFiles;
        }

        var createNewDirectories = CreateNewDirectories(changes);
        if (!createNewDirectories.IsSuccess)
        {
            return createNewDirectories;
        }

        foreach (var (oldFile, newFile) in changes.ChangedFiles)
        {
            var filePath = Path.Combine(_config.ResonitePath, newFile.Path);

            try
            {
                if (File.Exists(filePath))
                {
                    using var sha1 = SHA1.Create();
                    await using var readonlyFile = File.OpenRead(filePath);
                    var hash = await sha1.ComputeHashAsync(readonlyFile, ct);

                    if (newFile.Hash.SequenceEqual(hash))
                    {
                        // file is OK, no need to download it
                        continue;
                    }
                }

                var fileDirectory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException();

                Directory.CreateDirectory(fileDirectory);

                await using var file = oldFile is null
                    ? File.Open(filePath, FileMode.Create)
                    : File.Open(filePath, FileMode.Open);

                // set the file to the correct size
                file.SetLength((long)newFile.Size);

                var changedChunks = oldFile is null
                    ? newFile.Chunks.ToImmutableArray()
                    : oldFile.GetChangedChunks(newFile).ToImmutableArray();

                var totalChunkSize = changedChunks.Sum(c => c.CompressedLength);

                if (oldFile is null)
                {
                    _log.LogDebug("New file: {Path}", newFile.Path);
                }
                else
                {
                    _log.LogDebug("Patching file: {Path}", newFile.Path);
                }

                _log.LogDebug
                (
                    "Chunks: {ChunkCount}, Total Size: {TotalChunkSize}",
                    changedChunks.Length,
                    totalChunkSize.Bytes()
                );

                var downloadedChunkCount = 0;
                foreach (var chunk in changedChunks)
                {
                    var getChunk = await _client.GetDepotChunkAsync
                    (
                        newFile.DepotID,
                        depotKeys[newFile.DepotID],
                        chunk,
                        ct
                    );

                    if (!getChunk.IsDefined(out var chunkData))
                    {
                        // warn and stop
                        _log.LogError("Failed to get depot chunk for file {File}", filePath);
                        return (Result)getChunk;
                    }

                    file.Seek((long)chunk.Offset, SeekOrigin.Begin);
                    await file.WriteAsync(chunkData.Data, ct);

                    ++downloadedChunkCount;
                    _log.LogDebug
                    (
                        "Fetched chunk {CurrentChunk}/{ChunkCount} ({ChunkSize})",
                        downloadedChunkCount,
                        changedChunks.Length,
                        chunk.CompressedLength.Bytes()
                    );
                }

                if (newFile.IsExecutable && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    File.SetUnixFileMode
                    (
                        filePath,
                        UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute
                    );
                }
            }
            catch (Exception e)
            {
                // warn and stop
                _log.LogError(e, "Failed to download file {File}", filePath);
                return e;
            }
        }

        try
        {
            var localManifestPath = Path.Combine(_config.ResonitePath, ".crystite", "appmanifest.json");
            var localManifestDirectory = Path.GetDirectoryName(localManifestPath) ?? throw new InvalidOperationException();
            Directory.CreateDirectory(localManifestDirectory);

            await using var manifestFile = File.Open(localManifestPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(manifestFile, changes.NewManifest, _jsonOptions, ct);
        }
        catch (Exception e)
        {
            _log.LogError(e, "Failed to save the updated manifest");
            return e;
        }

        return Result.FromSuccess();
    }

    private Result DeleteOldData(ResoniteInstallationChangeInformation changes)
    {
        foreach (var deletedFile in changes.DeletedFiles)
        {
            var deletedPath = Path.Combine(_config.ResonitePath, deletedFile.Path);
            if (!File.Exists(deletedPath))
            {
                continue;
            }

            try
            {
                File.Delete(deletedPath);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to delete file {File}", deletedPath);
                return new InvalidOperationError($"Failed to delete file {deletedPath}");
            }
        }

        foreach (var deletedDirectory in changes.DeletedDirectories)
        {
            var deletedPath = Path.Combine(_config.ResonitePath, deletedDirectory);
            if (!Directory.Exists(deletedPath))
            {
                continue;
            }

            try
            {
                Directory.Delete(deletedPath, false);
            }
            catch (IOException)
            {
                _log.LogWarning("Directory {Directory} not empty; skipping deletion", deletedPath);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to delete directory {Directory}", deletedPath);
                return new InvalidOperationError($"Failed to delete directory {deletedPath}");
            }
        }

        return Result.FromSuccess();
    }

    private Result CreateNewDirectories(ResoniteInstallationChangeInformation changes)
    {
        foreach (var addedDirectory in changes.AddedDirectories)
        {
            var addedPath = Path.Combine(_config.ResonitePath, addedDirectory);
            try
            {
                Directory.CreateDirectory(addedPath);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to create directory {Directory}", addedPath);
                return new InvalidOperationError($"Failed to create directory {addedPath}");
            }
        }

        return Result.FromSuccess();
    }

    private async Task<Result<ResoniteInstallationChangeInformation>> GetChangesAsync(ResoniteManifest newAppManifest)
    {
        var localManifestPath = Path.Combine(_config.ResonitePath, ".crystite", "appmanifest.json");
        ResoniteManifest? oldAppManifest = null;
        if (File.Exists(localManifestPath))
        {
            try
            {
                await using var file = File.OpenRead(localManifestPath);
                oldAppManifest = JsonSerializer.Deserialize<ResoniteManifest>(file, _jsonOptions);
            }
            catch (Exception)
            {
                // pass; we can handle not having an old manifest
            }
        }

        if (oldAppManifest is not null)
        {
            return new ResoniteInstallationChangeInformation
            (
                newAppManifest,
                oldAppManifest.GetDeletedDirectories(newAppManifest).ToImmutableArray(),
                oldAppManifest.GetAddedDirectories(newAppManifest).ToImmutableArray(),
                oldAppManifest.GetDeletedFiles(newAppManifest).ToImmutableArray(),
                oldAppManifest.GetChangedFiles(newAppManifest).ToImmutableArray()
            );
        }

        return new ResoniteInstallationChangeInformation
        (
            newAppManifest,
            Array.Empty<string>(),
            newAppManifest.Directories.ToImmutableArray(),
            Array.Empty<ResoniteManifestFile>(),
            newAppManifest.Files.Select(f => ((ResoniteManifestFile?)null, f)).ToImmutableArray()
        );
    }

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "Bug in analyzer")]
    private record ResoniteInstallationChangeInformation
    (
        ResoniteManifest NewManifest,
        IReadOnlyList<string> DeletedDirectories,
        IReadOnlyList<string> AddedDirectories,
        IReadOnlyList<ResoniteManifestFile> DeletedFiles,
        IReadOnlyList<(ResoniteManifestFile? Old, ResoniteManifestFile New)> ChangedFiles
    );
}
