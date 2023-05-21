//
//  SPDX-FileName: CorrectAssemblyDirectoryArgument.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.Engine;

/// <summary>
/// Patches the <see cref="Engine"/> class.
/// </summary>
[HarmonyPatch("FrooxEngine.Engine+<Initialize>d__298", "MoveNext")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class CorrectAssemblyDirectoryArgument
{
    private static readonly FieldInfo _appPathField = AccessTools.Field
    (
        "FrooxEngine.Engine+<Initialize>d__298:appPath"
    );

    /// <summary>
    /// Replaces a hard-coded path with a reference to an input argument.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Is(OpCodes.Ldstr, "Neos_Data\\Managed"))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, _appPathField);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
