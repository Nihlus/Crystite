//
//  SPDX-FileName: GuardDisposal.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.Engine;

/// <summary>
/// Guards each nested disposal call with a null check.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.Engine), nameof(FrooxEngine.Engine.Dispose))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GuardDisposal
{
    /// <summary>
    /// Guards each nested disposal call with a null check.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <param name="generator">The IL generator.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler
    (
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        var enumeratedInstructions = instructions.ToArray();

        Label? nextInstructionLabel = null;
        for (var i = 0; i < enumeratedInstructions.Length; i++)
        {
            var instruction = enumeratedInstructions[i];

            var previousInstruction = i > 0
                ? enumeratedInstructions[i - 1]
                : null;

            var callLabel = generator.DefineLabel();

            if (instruction.operand is MethodInfo { Name: nameof(IDisposable.Dispose) } && previousInstruction?.opcode != OpCodes.Br_S)
            {
                nextInstructionLabel = generator.DefineLabel();

                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Brtrue, callLabel);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Br_S, nextInstructionLabel);
                yield return instruction.WithLabels(callLabel);
            }
            else
            {
                if (nextInstructionLabel is not null)
                {
                    yield return instruction.WithLabels(nextInstructionLabel.Value);
                    nextInstructionLabel = null;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
