//
//  SPDX-FileName: DisposalExceptionHandling.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.World;

#pragma warning disable SA1313

// ReSharper disable once InconsistentNaming

/// <summary>
/// Catches and swallows instances of exceptions thrown when Dispose is called multiple times.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.World), nameof(FrooxEngine.World.Dispose))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class DisposalExceptionHandling
{
    /// <summary>
    /// Catches and swallows instances of exceptions thrown when Dispose is called multiple times.
    /// </summary>
    /// <param name="__exception">The thrown exception.</param>
    /// <returns>The exception to rethrow.</returns>
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception is null)
        {
            return null;
        }

        if (__exception.GetType() == typeof(Exception) && __exception.Message == "World is already disposed")
        {
            return null;
        }

        return __exception;
    }
}
