//
//  SPDX-FileName: RedirectCommandLineParsing.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.Generic;

/// <summary>
/// Wraps instantiation of <see cref="RedirectCommandLineParsingPatch{TTarget}"/> patches.
/// </summary>
public static class RedirectCommandLineParsing
{
    private static readonly List<Type> _instantiatedGenericPatches = new();

    /// <summary>
    /// Configures a command-line patch against the given type and method.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(string methodName, Action<List<string>> argumentBuilder)
    {
        if (_instantiatedGenericPatches.Contains(typeof(RedirectCommandLineParsingPatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        RedirectCommandLineParsingPatch<TTarget>.MethodNames = new[] { methodName };
        RedirectCommandLineParsingPatch<TTarget>.GetOverridingArguments = argumentBuilder;

        _instantiatedGenericPatches.Add(typeof(RedirectCommandLineParsingPatch<TTarget>));
    }

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="methodNames">The names of the methods.</param>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(string[] methodNames, Action<List<string>> argumentBuilder)
    {
        if (_instantiatedGenericPatches.Contains(typeof(RedirectCommandLineParsingPatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        RedirectCommandLineParsingPatch<TTarget>.MethodNames = methodNames;
        RedirectCommandLineParsingPatch<TTarget>.GetOverridingArguments = argumentBuilder;

        _instantiatedGenericPatches.Add(typeof(RedirectCommandLineParsingPatch<TTarget>));
    }

    /// <summary>
    /// Configures a command-line patch against the given type's constructor.
    /// </summary>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(Action<List<string>> argumentBuilder)
    {
        if (_instantiatedGenericPatches.Contains(typeof(RedirectCommandLineParsingPatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        RedirectCommandLineParsingPatch<TTarget>.GetOverridingArguments = argumentBuilder;

        _instantiatedGenericPatches.Add(typeof(RedirectCommandLineParsingPatch<TTarget>));
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
    /// Patches the given class, replacing calls to GetCommandLineArgs with the redirected factory function.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    [HarmonyPatch]
    [HarmonyPatchCategory("Generic")]
    private static class RedirectCommandLineParsingPatch<TTarget>
    {
        /// <summary>
        /// Gets or sets the name of the method to patch.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string[]? MethodNames { get; set; }

        /// <summary>
        /// Gets or sets the overriding argument factory function.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static Action<List<string>> GetOverridingArguments { get; set; } = null!;

        /// <summary>
        /// Gets the command line arguments visible to the type.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string[] RedirectedCommandLineArgs { get; private set; } = null!;

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
        /// Replaces access to the full command line arguments with a specific set.
        /// </summary>
        /// <param name="instructions">The instructions of the method.</param>
        /// <returns>The patched code.</returns>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var args = new List<string>();
            GetOverridingArguments(args);

            RedirectedCommandLineArgs = args.ToArray();

            var getArgsMethod = typeof(Environment).GetMethod(nameof(Environment.GetCommandLineArgs));
            var getRedirectedArgsMethod = typeof(RedirectCommandLineParsingPatch<TTarget>)
                .GetProperty(nameof(RedirectedCommandLineArgs))?
                .GetGetMethod();

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getArgsMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, getRedirectedArgsMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
