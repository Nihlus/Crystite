//
//  SPDX-FileName: DisableStackTraceGathering.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.Engine;

#pragma warning disable SA1313

/// <summary>
/// Disables stack trace gathering for threads other than the current one.
/// </summary>
[HarmonyPatch("FrooxEngine.Engine+<<RunUpdateLoop>b__328_0>d", "MoveNext")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class DisableStackTraceGathering
{
    /// <summary>
    /// Gets an error-reporting stack trace.
    /// </summary>
    /// <param name="thread">The thread that the caller wants to gather the stack trace from.</param>
    /// <returns>The stack trace of the current thread, or a fake stack trace with an error message.</returns>
    public static StackTrace GetReportingStackTrace(Thread thread)
    {
        if (thread == Thread.CurrentThread)
        {
            return new StackTrace();
        }

        return new StackTrace
        (
            new NotSupportedException("Stack traces from other threads cannot be gathered on this runtime.")
        );
    }

    /// <summary>
    /// Replaces a hard-coded path with a reference to an input argument.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getStackTraceMethod = typeof(FrooxEngine.Engine).GetMethod("GetStackTrace", BindingFlags.Static | BindingFlags.NonPublic);
        var getErrorReportingStackTrace = typeof(DisableStackTraceGathering).GetMethod(nameof(GetReportingStackTrace));

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(getStackTraceMethod))
            {
                yield return instruction.Clone(getErrorReportingStackTrace);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
