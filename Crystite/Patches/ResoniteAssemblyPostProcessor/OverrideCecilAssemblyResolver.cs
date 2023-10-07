//
//  SPDX-FileName: OverrideCecilAssemblyResolver.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Crystite.Patches.ResoniteAssemblyPostProcessor;

/// <summary>
/// Replaces the assembly resolver used by FrooxEngine.Weaver with our own.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.Weaver.AssemblyPostProcessor))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OverrideCecilAssemblyResolver
{
    /// <summary>
    /// Gets the overriding assembly resolver in use.
    /// </summary>
    public static IAssemblyResolver? OverridingAssemblyResolver { get; internal set; }

    /// <summary>
    /// Gets the target method, unwrapping the async wrapper if required.
    /// </summary>
    /// <returns>The target method.</returns>
    [HarmonyTargetMethod]
    public static MethodInfo GetTargetMethod()
    {
        var method = AccessTools.Method
        (
            typeof(FrooxEngine.Weaver.AssemblyPostProcessor),
            nameof(FrooxEngine.Weaver.AssemblyPostProcessor.Process),
            new[]
            {
                typeof(string),
                typeof(string).MakeByRefType(),
                typeof(string)
            }
        );

        if (method is null)
        {
            throw new InvalidOperationException();
        }

        var asyncAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (asyncAttribute is null)
        {
            return method;
        }

        var asyncStateMachineType = asyncAttribute.StateMachineType;
        var asyncMethodBody = AccessTools.DeclaredMethod(asyncStateMachineType, nameof(IAsyncStateMachine.MoveNext))
                              ?? throw new InvalidOperationException();

        return asyncMethodBody;
    }

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
