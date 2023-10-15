//
//  SPDX-FileName: ResoniteManifestBuilder.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using SteamKit2;

namespace Crystite.ResoniteInstallation;

/// <summary>
/// Handles ordered building of an app manifest from one or more depot manifests.
/// </summary>
public class ResoniteManifestBuilder
{
    private readonly Stack<DepotManifest> _manifests = new();

    /// <summary>
    /// Pushes a manifest onto the builder's stack.
    /// </summary>
    /// <param name="manifest">The manifest to push.</param>
    public void PushManifest(DepotManifest manifest)
    {
        _manifests.Push(manifest);
    }

    /// <summary>
    /// Builds the app manifest.
    /// </summary>
    /// <returns>The built manifest.</returns>
    public ResoniteManifest Build()
    {
        // copy the stack so we can build multiple manifests if need be
        var manifests = new Stack<DepotManifest>(_manifests);

        var directories = new HashSet<string>();
        var files = new HashSet<ResoniteManifestFile>();

        while (manifests.TryPop(out var depotManifest))
        {
            if (depotManifest.Files is null)
            {
                continue;
            }

            foreach (var file in depotManifest.Files)
            {
                if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                {
                    // always add directories, since they're directly comparable
                    directories.Add(file.FileName);
                    continue;
                }

                var existingFile = files.FirstOrDefault(f => f.Path == file.FileName);
                if (existingFile is not null)
                {
                    // overridden by a more recent depot
                    continue;
                }

                var chunks = file.Chunks.Select
                (
                    c => new ResoniteManifestFileChunk
                    (
                        c.ChunkID ?? throw new InvalidOperationException(),
                        c.Checksum ?? throw new InvalidOperationException(),
                        c.Offset,
                        c.CompressedLength,
                        c.UncompressedLength
                    )
                )
                .Distinct()
                .ToHashSet();

                files.Add
                (
                    new ResoniteManifestFile
                    (
                        depotManifest.DepotID,
                        file.FileName,
                        file.TotalSize,
                        file.FileHash,
                        chunks,
                        file.Flags.HasFlag(EDepotFileFlag.Executable)
                    )
                );
            }
        }

        return new ResoniteManifest(directories, files);
    }
}
