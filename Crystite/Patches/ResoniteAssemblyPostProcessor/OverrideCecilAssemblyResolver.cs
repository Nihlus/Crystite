//
//  SPDX-FileName: OverrideCecilAssemblyResolver.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Crystite.Patches.ResoniteAssemblyPostProcessor;

/// <summary>
/// Replaces the assembly resolver used by PostX with our own.
/// </summary>
[HarmonyPatch(typeof(PostX.NeosAssemblyPostProcessor), nameof(PostX.NeosAssemblyPostProcessor.Process))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OverrideCecilAssemblyResolver
{
    /// <summary>
    /// Gets the overriding assembly resolver in use.
    /// </summary>
    public static IAssemblyResolver? OverridingAssemblyResolver { get; internal set; }

    /// <summary>
    /// Replaces the assembly resolver used by PostX with our own.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var defaultConstructor = typeof(DefaultAssemblyResolver).GetConstructors().Single();
        var getter = typeof(OverrideCecilAssemblyResolver).GetProperty(nameof(OverridingAssemblyResolver))?.GetMethod
            ?? throw new InvalidOperationException();

        foreach (var instruction in instructions)
        {
            if (instruction.Is(OpCodes.Newobj, defaultConstructor))
            {
                yield return new CodeInstruction(OpCodes.Call, getter);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
