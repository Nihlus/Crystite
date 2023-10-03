//
//  SPDX-FileName: UseForwardedTypeName.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.Patches.WorkerManager;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.Worker;

#pragma warning disable SA1313

/// <summary>
/// Replaces the type name of the worker with a serializable type name.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.Worker), nameof(FrooxEngine.Worker.WorkerTypeName), MethodType.Getter)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class UseForwardedTypeName
{
    /// <summary>
    /// Replaces the type name of the worker with a serializable type name.
    /// </summary>
    /// <param name="__instance">The instance.</param>
    /// <param name="__result">The new result.</param>
    /// <returns>Always false.</returns>
    // ReSharper disable InconsistentNaming
    // ReSharper disable RedundantAssignment
    public static bool Prefix(FrooxEngine.Worker __instance, ref string __result)
    {
        __result = __instance.WorkerType.GetSerializableTypeName();
        return false;
    }
}
