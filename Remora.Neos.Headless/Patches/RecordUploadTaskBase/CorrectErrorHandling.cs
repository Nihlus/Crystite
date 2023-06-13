//
//  SPDX-FileName: CorrectErrorHandling.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

using UploadTask = CloudX.Shared.RecordUploadTaskBase<FrooxEngine.Record>;

namespace Remora.Neos.Headless.Patches.RecordUploadTaskBase;

// ReSharper disable InconsistentNaming
#pragma warning disable SA1313

/// <summary>
/// Corrects the error handling of record uploads, fixing stuck syncs.
/// </summary>
/// <remarks>Based on code from https://github.com/stiefeljackal/JworkzNeosFixFrickenSync.</remarks>
[HarmonyPatch(typeof(UploadTask))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class CorrectErrorHandling
{
    private static MethodInfo _runUploadInternalMethod = AccessTools.Method(typeof(UploadTask), "RunUploadInternal");
    private static FieldInfo _completionSourceField = AccessTools.Field(typeof(UploadTask), "_completionSource");
    private static MethodInfo _failMethod = AccessTools.Method(typeof(UploadTask), "Fail");

    /// <summary>
    /// Sets the logging instance for this type.
    /// </summary>
    internal static ILogger Log { private get; set; } = null!; // shush, compiler, this is set at initialization time

    /// <summary>
    /// Gets or sets the maximum number of times to retry uploading.
    /// </summary>
    public static byte MaxUploadRetries { get; set; }

    /// <summary>
    /// Gets or sets the delay between upload retries.
    /// </summary>
    public static TimeSpan RetryDelay { get; set; }

    /// <summary>
    /// Prefixes the targeted method.
    /// </summary>
    /// <param name="__instance">The instance that is targeted.</param>
    /// <param name="__result">The result to return.</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>Always false.</returns>
    [HarmonyPrefix]
    [HarmonyPatch("RunUpload")]
    public static bool RunUploadPrefix(UploadTask __instance, out Task __result, CancellationToken cancellationToken)
    {
        var uploadTask = _runUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException();

        var completionSource = _completionSourceField.GetValue(__instance) as TaskCompletionSource<bool>
            ?? throw new InvalidOperationException();

        var failMethod = _failMethod.CreateDelegate<Action<string>>(__instance);

        // We don't want to let the task terminate early, so the cancellation token is not passed on
        __result = Task.Run
        (
            async () =>
            {
                var retryCount = 0;
                var maxRetryCount = MaxUploadRetries <= 0 ? 1 : MaxUploadRetries;
                var delay = RetryDelay;

                while (retryCount < maxRetryCount && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await uploadTask.ConfigureAwait(false);
                        _ = completionSource.TrySetResult(__instance.IsFinished);

                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.LogError
                        (
                            ex,
                            "Exception during record upload task (attempt {Attempt} out of {Count})",
                            retryCount + 1,
                            maxRetryCount
                        );
                    }

                    ++retryCount;
                    await Task.Delay(delay, CancellationToken.None);
                }

                // we may not have signalled completion at this point, so if we haven't, notify the system of failure
                if (!completionSource.Task.IsCompleted)
                {
                    failMethod
                    (
                        cancellationToken.IsCancellationRequested
                            ? "Record upload task cancelled"
                            : "Exception during sync."
                    );
                }
            },
            CancellationToken.None
        );

        return false;
    }
}
