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
    private readonly NeosHeadlessConfig _neosConfig;

    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetHooverService"/> class.
    /// </summary>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="neosConfig">The headless configuration.</param>
    /// <param name="engine">The game engine.</param>
    public AssetHooverService
    (
        ILogger<AssetHooverService> log,
        IOptionsMonitor<HeadlessApplicationConfiguration> config,
        IOptionsMonitor<NeosHeadlessConfig> neosConfig,
        Engine engine
    )
    {
        _log = log;
        _config = config.CurrentValue;
        _neosConfig = neosConfig.CurrentValue;
        _engine = engine;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_config.AssetCleanupInterval is null || _config.MaxAssetAge is null)
        {
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

        var cacheFolder = string.IsNullOrWhiteSpace(_neosConfig.CacheFolder)
            ? NeosHeadlessConfig.DefaultCacheFolder
            : _neosConfig.CacheFolder;

        var cacheDirectory = new DirectoryInfo(cacheFolder);

        var timer = new PeriodicTimer(_config.AssetCleanupInterval.Value);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTimeOffset.UtcNow;

            var files = cacheDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
            var oldFiles = files
                .Where(f => f.CreationTimeUtc > f.LastAccessTimeUtc) // filter out entries without an access time
                .Where(f => now - f.LastAccessTimeUtc > _config.MaxAssetAge).ToArray();

            var oldPaths = oldFiles.Select(f => f.FullName).ToArray();

            using (await dbLock.AcquireAsync(stoppingToken))
            {
                var oldAssets = assets.Find(a => oldPaths.Contains(a.path)).Select(a => (Url: a.url, Path: a.path)).ToArray();
                if (oldAssets.Length <= 0)
                {
                    continue;
                }

                _log.LogInformation("Cleaning up cached assets");

                var totalSize = oldFiles.Sum(f => f.Length);
                _log.LogInformation
                (
                    "{Count} expired assets found (total {Size})",
                    oldAssets.Length,
                    totalSize.Bytes().Humanize()
                );

                foreach (var (url, path) in oldAssets)
                {
                    _log.LogInformation("Deleting {Url}", url);
                    await db.DeleteCacheRecordAsync(new Uri(url));

                    // clean up files that the DB doesn't know about
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
        }
    }
}
