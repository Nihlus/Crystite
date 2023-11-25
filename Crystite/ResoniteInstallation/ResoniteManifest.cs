//
//  SPDX-FileName: ResoniteManifest.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

#pragma warning disable SA1402

namespace Crystite.ResoniteInstallation;

/// <summary>
/// Represents serialized information about the contents of a Resonite installation.
/// </summary>
/// <param name="Directories">The relative paths to directories included in the Resonite install directory.</param>
/// <param name="Files">The files included in the installation.</param>
public record ResoniteManifest
(
    HashSet<string> Directories,
    HashSet<ResoniteManifestFile> Files
)
{
    /// <summary>
    /// Compares the manifest to another manifest, determining which directories have been deleted.
    /// </summary>
    /// <param name="newManifest">The new manifest.</param>
    /// <returns>The deleted directories.</returns>
    public IEnumerable<string> GetDeletedDirectories(ResoniteManifest newManifest)
    {
        return this.Directories.Where
        (
            directory =>
                !newManifest.Directories.Contains(directory)
        );
    }

    /// <summary>
    /// Compares the manifest to another manifest, determining which directories have been added.
    /// </summary>
    /// <param name="newManifest">The new manifest.</param>
    /// <returns>The added directories.</returns>
    public IEnumerable<string> GetAddedDirectories(ResoniteManifest newManifest)
    {
        return newManifest.Directories.Where
        (
            directory =>
                !this.Directories.Contains(directory)
        );
    }

    /// <summary>
    /// Compares the manifest to another manifest, determining which files have been deleted.
    /// </summary>
    /// <param name="newManifest">The new manifest.</param>
    /// <returns>The deleted files.</returns>
    public IEnumerable<ResoniteManifestFile> GetDeletedFiles(ResoniteManifest newManifest)
    {
        return this.Files.Where
        (
            file =>
                newManifest.Files.All(f => f.Path != file.Path)
        );
    }

    /// <summary>
    /// Compares the manifest to another manifest, determining which files have changed.
    /// </summary>
    /// <param name="newManifest">The new manifest.</param>
    /// <returns>The changed files.</returns>
    public IEnumerable<(ResoniteManifestFile? Old, ResoniteManifestFile New)> GetChangedFiles
    (
        ResoniteManifest newManifest
    )
    {
        foreach (var newFile in newManifest.Files)
        {
            var oldFile = this.Files.FirstOrDefault(f => f.Path == newFile.Path);
            if (oldFile is null)
            {
                // completely new file
                yield return (null, newFile);
                continue;
            }

            if (oldFile != newFile)
            {
                // changed file
                yield return (oldFile, newFile);
            }
        }
    }
}

/// <summary>
/// Represents serialized information about a downloaded file.
/// </summary>
/// <param name="DepotID">The ID of the depot the file comes from.</param>
/// <param name="Path">The relative path to the file within the Resonite install directory.</param>
/// <param name="Size">The total size in bytes of the file.</param>
/// <param name="Hash">The hash of the file.</param>
/// <param name="Chunks">The data chunks associated with the file.</param>
/// <param name="IsExecutable">Whether the file should have the execute bit set.</param>
public record ResoniteManifestFile
(
    uint DepotID,
    string Path,
    ulong Size,
    byte[] Hash,
    HashSet<ResoniteManifestFileChunk> Chunks,
    bool IsExecutable
)
{
    /// <summary>
    /// Compares the file to another file, determining which chunks have changed.
    /// </summary>
    /// <param name="newFile">The new file.</param>
    /// <returns>The changed chunks.</returns>
    public IEnumerable<ResoniteManifestFileChunk> GetChangedChunks(ResoniteManifestFile newFile)
    {
        foreach (var newChunk in newFile.Chunks)
        {
            var oldChunk = this.Chunks.FirstOrDefault(c => c.ChunkID.SequenceEqual(newChunk.ChunkID));
            if (oldChunk is null)
            {
                // completely new chunk
                yield return newChunk;
                continue;
            }

            if (oldChunk != newChunk)
            {
                // changed chunk
                yield return newChunk;
            }
        }
    }
}

/// <summary>
/// Represents serialized information about a chunk within a file.
/// </summary>
/// <param name="ChunkID">The SHA-1 hash ID of the chunk.</param>
/// <param name="Checksum">The Adler32 checksum of the chunk.</param>
/// <param name="Offset">The offset within the file of the chunk.</param>
/// <param name="CompressedLength">The compressed, on-wire length of the chunk.</param>
/// <param name="UncompressedLength">The uncompressed, on-disk length of the chunk.</param>
public record ResoniteManifestFileChunk
(
    byte[] ChunkID,
    byte[] Checksum,
    ulong Offset,
    uint CompressedLength,
    uint UncompressedLength
);
