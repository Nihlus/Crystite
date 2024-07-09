//
//  SPDX-FileName: UseSerializableFullName.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.Generic;

#pragma warning disable SA1313

/// <summary>
/// Wraps instantiation of <see cref="UseSerializableFullNamePatch{TTarget}"/> patches.
/// </summary>
public static class UseSerializableFullName
{
    private const string _mscorlib = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

    private static readonly IReadOnlyDictionary<Type, TypeForwardedFromAttribute> _typeForwardedFromOverrides = new Dictionary<Type, TypeForwardedFromAttribute>
    {
        { typeof(System.Net.HttpStatusCode), new TypeForwardedFromAttribute("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") }
    };

    private static readonly ConcurrentDictionary<Type, string> _qualifiedForwardedTypeNameCache = new();
    private static readonly ConcurrentDictionary<Type, string> _typeNameCache = new();

    private static readonly MethodInfo _getTypename = AccessTools.Method
    (
        typeof(UseSerializableFullName),
        nameof(GetTypename)
    );

    private static readonly List<Type> _instantiatedGenericPatches = new();

    /// <summary>
    /// Gets or sets the logging instance for this type.
    /// </summary>
    public static ILogger Log { get; set; } = null!;

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="methodNames">The names of the methods.</param>
    public static void Configure(Type type, params string[] methodNames)
    {
        var configure = AccessTools.MethodDelegate<Action<string[]>>
        (
            AccessTools.Method
            (
                typeof(UseSerializableFullName),
                "Configure",
                new[] { typeof(string[]) },
                new[] { type }
            )
        );

        configure(methodNames);
    }

    /// <summary>
    /// Configures a command-line patch against the given type and set of methods.
    /// </summary>
    /// <param name="methodNames">The names of the methods.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>(params string[] methodNames)
    {
        if (_instantiatedGenericPatches.Contains(typeof(UseSerializableFullNamePatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        UseSerializableFullNamePatch<TTarget>.MethodNames = methodNames;

        _instantiatedGenericPatches.Add(typeof(UseSerializableFullNamePatch<TTarget>));
    }

    /// <summary>
    /// Configures a command-line patch against the given type's constructor.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public static void Configure<TTarget>()
    {
        if (_instantiatedGenericPatches.Contains(typeof(UseSerializableFullNamePatch<TTarget>)))
        {
            throw new InvalidOperationException();
        }

        _instantiatedGenericPatches.Add(typeof(UseSerializableFullNamePatch<TTarget>));
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
    private static class UseSerializableFullNamePatch<TTarget>
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
        /// Replaces calls to FullName with calls to GetTypename.
        /// </summary>
        /// <param name="instructions">The instructions of the method.</param>
        /// <returns>The patched code.</returns>
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.Calls(AccessTools.PropertyGetter(typeof(Type), nameof(Type.FullName))))
                {
                    yield return new CodeInstruction(OpCodes.Call, _getTypename);
                }
                else
                {
                    yield return codeInstruction;
                }
            }
        }
    }

    /// <summary>
    /// Overrides GetTypename handling for forwarded types, enabling cross-framework serialization.
    /// </summary>
    /// <param name="type">The input type.</param>
    /// <returns>The resulting type name.</returns>
    private static string GetTypename(Type type)
    {
        return _typeNameCache.TryGetValue(type, out var result)
            ? result
            : type.GetSerializableFullName();
    }

    /// <summary>
    /// Gets a serializable type name for the given type, that is, a type name that uses a full name compatible with
    /// both modern .NET and the legacy .NET Framework.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="qualifyAssembly">Whether to fully qualify the assembly.</param>
    /// <returns>The serializable type name.</returns>
    public static string GetSerializableFullName(this Type type, bool qualifyAssembly = false)
    {
        switch (qualifyAssembly)
        {
            case false when _typeNameCache.TryGetValue(type, out var unqualifiedName):
            {
                return unqualifiedName;
            }
            case true when _qualifiedForwardedTypeNameCache.TryGetValue(type, out var qualifiedName):
            {
                return qualifiedName;
            }
        }

        var nameBuilder = new StringBuilder();
        if (type.Namespace is not null)
        {
            nameBuilder.Append(type.Namespace);
            nameBuilder.Append('.');
        }

        if (type.IsNested)
        {
            var nestedTypeNameBuilder = new StringBuilder();
            var declaringType = type.DeclaringType;
            while (declaringType is not null)
            {
                nestedTypeNameBuilder.Insert(0, $"{declaringType.Name}+");
                declaringType = declaringType.DeclaringType;
            }

            nameBuilder.Append(nestedTypeNameBuilder);
        }

        nameBuilder.Append(type.Name);

        if (type.IsGenericTypeDefinition)
        {
            return nameBuilder.ToString();
        }

        if (type.IsGenericType)
        {
            nameBuilder.Append('[');
            nameBuilder.AppendJoin(',', type.GetGenericArguments().Select(a => $"[{a.GetSerializableFullName(true)}]"));
            nameBuilder.Append(']');
        }

        if (!qualifyAssembly)
        {
            return nameBuilder.ToString();
        }

        // Respect TypeForwardedFromAttribute to enable cross-framework de/serialization
        var forwardedFrom = type.GetCustomAttribute<TypeForwardedFromAttribute>();

        if (forwardedFrom is null)
        {
            if (!_typeForwardedFromOverrides.TryGetValue(type, out forwardedFrom))
            {
                if (type.Assembly.FullName?.Contains("System.Private.CoreLib") is true)
                {
                    // assume mscorlib
                    forwardedFrom = new TypeForwardedFromAttribute(_mscorlib);
                }
            }
        }

        if (!qualifyAssembly && forwardedFrom is null)
        {
            _ = _typeNameCache.TryAdd(type, nameBuilder.ToString());
            return nameBuilder.ToString();
        }

        var assemblyName = forwardedFrom is not null
            ? forwardedFrom.AssemblyFullName
            : type.Assembly.FullName;

        if (forwardedFrom is null && type.Assembly.Location.Contains("Microsoft.NETCore.App.Ref"))
        {
            Log.LogWarning
            (
                "The framework-originating type {Type} was serialized with an assembly name of {AssemblyName} and no "
                + "forwarding information was available. This is likely an error, and should be reported on the "
                + "Crystite repository",
                type.Name,
                assemblyName
            );
        }

        if (assemblyName is null)
        {
            _ = _qualifiedForwardedTypeNameCache.TryAdd(type, nameBuilder.ToString());
            return nameBuilder.ToString();
        }

        nameBuilder.Append(", ");
        nameBuilder.Append(assemblyName);

        _ = _qualifiedForwardedTypeNameCache.TryAdd(type, nameBuilder.ToString());
        return nameBuilder.ToString();
    }
}
