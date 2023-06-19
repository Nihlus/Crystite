//
//  SPDX-FileName: GracefulInterruptTermination.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net.Sockets;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.WorldAnnouncer;

/// <summary>
/// Ensures the local announce listener terminates gracefully if the thread is interrupted.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.WorldAnnouncer), "LocalAnnounceListener")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GracefulInterruptTermination
{
    /// <summary>
    /// Suppresses instances of <see cref="ThreadInterruptedException"/>.
    /// </summary>
    /// <param name="__exception">The thrown exception.</param>
    /// <returns>The exception to rethrow.</returns>
    #pragma warning disable SA1313
    // ReSharper disable once InconsistentNaming
    public static Exception? Finalizer(Exception __exception)
    {
        switch (__exception)
        {
            case ThreadInterruptedException:
            case SocketException { SocketErrorCode: SocketError.Interrupted }:
            case ObjectDisposedException:
            {
                return null;
            }
            default:
            {
                return __exception;
            }
        }
    }
    #pragma warning restore SA1313
}
