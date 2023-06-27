//
//  SPDX-FileName: AssetHooverService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using Flettu.Lock;
using FrooxEngine;
using Humanizer;
using LiteDB;
using Microsoft.Extensions.Options;
using Remora.Neos.Headless.Configuration;

namespace Remora.Neos.Headless;

/// <summary>
/// Periodically cleans up old cached files.
/// </summary>
public class AssetHooverService : BackgroundService
{
    private readonly ILogger<AssetHooverService> _log;

    private readonly HeadlessApplicationConfiguration _config;

    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetHooverService"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="engine">The game engine.</param>
    public AssetHooverService
    (
        ILogger<AssetHooverService> log,
        IOptionsMonitor<HeadlessApplicationConfiguration> config,
        Engine engine
    )
    {
        _log = log;
        _config = config.CurrentValue;
        _engine = engine;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (
            _config.AssetCleanupInterval is null
            || _config.MaxAssetAge is null
            || _config.CleanupTypes is null
            || _config.CleanupLocations is null
        )
        {
            // no interval or age set, end early
            return;
        }

        var lockField = typeof(LocalDB).GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException();
        var assetsField = typeof(LocalDB).GetField("assets", BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException();

        var engineStartup = new TaskCompletionSource();
        _engine.OnReady += () => engineStartup.SetResult();

        await engineStartup.Task;

        var db = _engine.LocalDB;

        var dbLock = (AsyncLock)(lockField.GetValue(db) ?? throw new InvalidOperationException());
        var assets = (LiteCollection<AssetRecord>)(assetsField.GetValue(db) ?? throw new InvalidOperationException());

        var directories = GetTargetDirectories();

        var timer = new PeriodicTimer(_config.AssetCleanupInterval.Value);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (Userspace.IsExitingNeos || _engine.ShutdownRequested)
            {
                // don't mess with local files while we're shutting down
                continue;
            }

            var now = DateTimeOffset.UtcNow;

            var files = directories
                .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories))
                .Where(f => f.CreationTimeUtc < f.LastAccessTimeUtc) // filter out entries without an access time
                .ToArray();

            if (files.Length <= 0)
            {
                continue;
            }

            var paths = files.Select(f => f.FullName).ToArray();

            using (await dbLock.AcquireAsync(stoppingToken))
            {
                // data assets aren't stored in the database with their full path; check the Assets directory too
                var oldAssets = assets
                    .Find(a => paths.Contains(a.path) || paths.Contains(Path.Combine(_engine.DataPath, "Assets", a.path)))
                    .Select
                    (
                        a =>
                        (
                            Url: new Uri(a.url),
                            Path: a.path,
                            File: files.Single
                            (
                                f => f.FullName == (Path.IsPathRooted(a.path)
                                    ? a.path
                                    : Path.Combine(_engine.DataPath, "Assets", a.path))
                            )
                        )
                    )
                    .Where(a =>
                    {
                        var type = Enum.TryParse<AssetCleanupType>(a.Url.Scheme, true, out var val)
                            ? val
                            : AssetCleanupType.Other;

                        if (!_config.CleanupTypes.TryGetValue(type, out var maxAssetAge))
                        {
                            return false;
                        }

                        // age check
                        maxAssetAge ??= _config.MaxAssetAge;
                        return now - a.File.LastAccessTimeUtc > maxAssetAge;
                    })
                    .ToArray();

                if (oldAssets.Length <= 0)
                {
                    continue;
                }

                _log.LogInformation("Cleaning up cached assets");

                var totalSize = oldAssets.Sum(f => f.File.Length);
                _log.LogInformation
                (
                    "{Count} expired assets found (total {Size})",
                    oldAssets.Length,
                    totalSize.Bytes().Humanize()
                );

                foreach (var (url, _, _) in oldAssets)
                {
                    _log.LogInformation("Deleting {Url}", url);
                    await db.DeleteCacheRecordAsync(url);
                }
            }
        }
    }

    private IReadOnlyList<DirectoryInfo> GetTargetDirectories()
    {
        return _config.CleanupLocations!.Select(cleanupLocation => new DirectoryInfo
        (
            cleanupLocation switch
            {
                AssetCleanupLocation.Data => Path.Combine(_engine.DataPath, "Assets"),
                AssetCleanupLocation.Cache => Path.Combine(_engine.CachePath, "Cache"),
                _ => throw new ArgumentOutOfRangeException()
            }
        )).ToArray();
    }
}
