//
//  SPDX-FileName: RecordManagerExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="RecordManager"/> class.
/// </summary>
public static class RecordManagerExtensions
{
    /// <summary>
    /// Waits for any pending uploads in the record manager to complete.
    /// </summary>
    /// <param name="recordManager">The record manager.</param>
    /// <param name="delay">The delay between polling operations to use. Defaults to 100 milliseconds.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WaitForPendingUploadsAsync
    (
        this RecordManager recordManager,
        TimeSpan? delay = null,
        CancellationToken ct = default
    )
    {
        delay ??= TimeSpan.FromMilliseconds(100);

        while (recordManager.SyncingRecordsCount > 0 || recordManager.UploadingVariantsCount > 0)
        {
            ct.ThrowIfCancellationRequested();

            await Task.Delay(delay.Value, ct);
        }
    }
}
