//
//  SPDX-FileName: SuppressStackTrace.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Elements.Core;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.Generic;

/// <summary>
/// Wraps instantiation of <see cref="SuppressStackTracePatch{TTarget}"/> patches.
/// </summary>
public static class SuppressStackTrace
{
    private static readonly List<Type> _instantiatedGenericPatches = new();

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="methodNames">The names of the methods.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(params string[] methodNames)
    {
        if (_instantiatedGenericPatches.Contains(typeof(SuppressStackTracePatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        SuppressStackTracePatch<TTarget>.MethodNames = methodNames.Length > 0 ? methodNames : null;

        _instantiatedGenericPatches.Add(typeof(SuppressStackTracePatch<TTarget>));
    }

    /// <summary>
    /// Applies all the configured patches.
    /// </summary>
    /// <param name="harmony">The harmony instance.</param>
    public static void PatchAll(Harmony harmony)
    {
        foreach (var instantiatedGenericPatch in _instantiatedGenericPatches)
        {
            harmony.CreateClassProcessor(instantiatedGenericPatch).Patch();
        }
    }

    /// <summary>
    /// Patches a logging call to suppress stack trace generation.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    [HarmonyPatch]
    [HarmonyPatchCategory("Generic")]
    private static class SuppressStackTracePatch<TTarget>
    {
        /// <summary>
        /// Gets or sets the name of the method to patch.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string[]? MethodNames { get; set; }

        /// <summary>
        /// Gets the target method to patch.
        /// </summary>
        /// <returns>The target method.</returns>
        [HarmonyTargetMethods]
        [UsedImplicitly]
        public static IEnumerable<MethodBase?> GetTargetMethods()
        {
            if (MethodNames is null)
            {
                // get constructor
                var constructors = AccessTools.GetDeclaredConstructors(typeof(TTarget));
                foreach (var constructor in constructors)
                {
                    yield return constructor;
                }

                var staticConstructors = AccessTools.GetDeclaredConstructors(typeof(TTarget), true);
                foreach (var staticConstructor in staticConstructors)
                {
                    yield return staticConstructor;
                }

                yield break;
            }

            foreach (var methodName in MethodNames)
            {
                var method = AccessTools.Method(typeof(TTarget), methodName);
                if (method is null)
                {
                    continue;
                }

                var asyncAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
                if (asyncAttribute is null)
                {
                    yield return method;
                    continue;
                }

                var asyncStateMachineType = asyncAttribute.StateMachineType;
                var asyncMethodBody = AccessTools.DeclaredMethod(asyncStateMachineType, nameof(IAsyncStateMachine.MoveNext))
                                      ?? throw new InvalidOperationException();

                yield return asyncMethodBody;
            }
        }

        /// <summary>
        /// Transpiles the original IL into something new.
        /// </summary>
        /// <param name="instructions">The instructions of the method.</param>
        /// <returns>The patched code.</returns>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var enumeratedInstructions = instructions.ToArray();

            var logMethod = AccessTools.Method
                (typeof(UniLog), nameof(UniLog.Log), new[] { typeof(string), typeof(bool) });

            for (var i = 0; i < enumeratedInstructions.Length; i++)
            {
                var instruction = enumeratedInstructions[i];

                var nextInstruction = i < enumeratedInstructions.Length - 1
                    ? enumeratedInstructions[i + 1]
                    : null;

                if (nextInstruction is not null && nextInstruction.Calls(logMethod))
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
}
