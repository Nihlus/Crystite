//
//  SPDX-FileName: SuppressStackTrace.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.WorldConfiguration;

/// <summary>
/// Patches a logging call to suppress stack trace generation.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.WorldConfiguration), "FieldChanged")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SuppressStackTrace
{
    /// <summary>
    /// Patches out Sslv3 from the configured security protocols.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_1)
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
