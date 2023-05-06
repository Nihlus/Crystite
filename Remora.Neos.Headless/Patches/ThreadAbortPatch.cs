//
//  SPDX-FileName: ThreadAbortPatch.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using BaseX;
using HarmonyLib;

namespace Remora.Neos.Headless.Patches;

/// <summary>
/// Patches instances of hard thread aborts with a soft wait.
/// </summary>
[HarmonyPatch]
public class ThreadAbortPatch
{
    /// <summary>
    /// Gets the target method for patching.
    /// </summary>
    /// <returns>The method.</returns>
    [HarmonyTargetMethod]
    public static MethodBase GetTarget()
    {
        var workerType = typeof(WorkProcessor).GetNestedType("ThreadWorker", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException();

        return workerType.GetMethod("Abort") ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Patches out Sslv3 from the configured security protocols.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var abortMethod = typeof(Thread).GetMethod(nameof(Thread.Abort), Array.Empty<Type>())
                          ?? throw new InvalidOperationException();

        var interruptMethod = typeof(Thread).GetMethod(nameof(Thread.Interrupt))
                         ?? throw new InvalidOperationException();

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(abortMethod))
            {
                var newInstruction = instruction.Clone(interruptMethod);
                yield return newInstruction;
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
