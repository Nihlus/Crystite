//
//  SPDX-FileName: ThreadInterruptHandling.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.WorkProcessor;

/// <summary>
/// Wraps job workers in order to catch <see cref="ThreadInterruptedException"/>s.
/// </summary>
[HarmonyPatch(typeof(BaseX.WorkProcessor), "JobWorker")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ThreadInterruptHandling
{
    /// <summary>
    /// Suppresses instances of <see cref="ThreadInterruptedException"/>.
    /// </summary>
    /// <param name="__exception">The thrown exception.</param>
    /// <returns>The exception to rethrow.</returns>
    #pragma warning disable SA1313
    // ReSharper disable once InconsistentNaming
    public static Exception? Finalizer(Exception __exception) => __exception is not ThreadInterruptedException
        #pragma warning restore SA1313
        ? __exception
        : null;
}
