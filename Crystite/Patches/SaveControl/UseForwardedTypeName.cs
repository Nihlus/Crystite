//
//  SPDX-FileName: UseForwardedTypeName.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection.Emit;
using Crystite.Patches.WorkerManager;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.SaveControl;

/// <summary>
/// Replaces use of FullName with a serializable type name.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.SaveControl), nameof(FrooxEngine.SaveControl.StoreTypeVersions))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class UseForwardedTypeName
{
    /// <summary>
    /// Transpiles the original IL into something new.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var fullNameGetter = typeof(Type).GetProperty(nameof(Type.FullName))?.GetMethod
                             ?? throw new InvalidOperationException();

        var getSerializableNameMethod = typeof(ForwardedTypeSerialization)
            .GetMethod(nameof(ForwardedTypeSerialization.GetSerializableTypeName))
                ?? throw new InvalidOperationException();

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(fullNameGetter))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Call, getSerializableNameMethod);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
