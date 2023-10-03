//
//  SPDX-FileName: UseThreadInterrupt.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.Generic;

/// <summary>
/// Wraps instantiation of <see cref="UseThreadInterruptPatch{TTarget}"/> patches.
/// </summary>
public static class UseThreadInterrupt
{
    private static readonly List<Type> _instantiatedGenericPatches = new();

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="methodNames">The names of the methods.</param>
    public static void Configure(Type type, params string[] methodNames)
    {
        var configureMethod = AccessTools.Method
        (
            typeof(UseThreadInterrupt),
            nameof(Configure),
            new[] { typeof(string[]) },
            new[] { type }
        );

        var configureDelegate = AccessTools.MethodDelegate<Action<string[]>>(configureMethod);
        configureDelegate(methodNames);
    }

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="methodNames">The names of the methods.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(params string[] methodNames)
    {
        if (_instantiatedGenericPatches.Contains(typeof(UseThreadInterruptPatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        UseThreadInterruptPatch<TTarget>.MethodNames = methodNames.Length > 0 ? methodNames : null;

        _instantiatedGenericPatches.Add(typeof(UseThreadInterruptPatch<TTarget>));
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
    /// Patches instances of hard thread aborts with a soft interrupt.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    [HarmonyPatch]
    [HarmonyPatchCategory("Generic")]
    private static class UseThreadInterruptPatch<TTarget>
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
        /// Patches out Sslv3 from the configured security protocols.
        /// </summary>
        /// <param name="instructions">The instructions of the method.</param>
        /// <returns>The patched code.</returns>
        [HarmonyTranspiler]
        [UsedImplicitly]
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
}
