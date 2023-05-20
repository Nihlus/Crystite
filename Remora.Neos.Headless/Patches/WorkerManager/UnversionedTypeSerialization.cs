//
//  SPDX-FileName: UnversionedTypeSerialization.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.WorkerManager;

#pragma warning disable SA1313

/// <summary>
/// Patches the <see cref="WorkerManager"/> type.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.WorkerManager), nameof(FrooxEngine.WorkerManager.GetTypename))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class UnversionedTypeSerialization
{
    private const string _mscorlib = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

    private static readonly IReadOnlyDictionary<Type, TypeForwardedFromAttribute> _typeForwardedFromOverrides = new Dictionary<Type, TypeForwardedFromAttribute>();

    private static readonly ConcurrentDictionary<Type, string> _qualifiedForwardedTypeNameCache = new();
    private static readonly ConcurrentDictionary<Type, string> _typeNameCache = new();

    /// <summary>
    /// Overrides GetTypename handling for forwarded types, enabling cross-framework serialization.
    /// </summary>
    /// <param name="type">The input type.</param>
    /// <param name="__result">The resulting type name.</param>
    /// <returns>true if the original should run; otherwise, false.</returns>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once RedundantAssignment
    public static bool Prefix(Type type, ref string? __result)
    {
        if (_typeNameCache.TryGetValue(type, out var result))
        {
            __result = result;
            return false;
        }

        __result = type.GetSerializableTypeName();
        return false;
    }

    private static string GetSerializableTypeName(this Type type, bool qualifyAssembly = false)
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

        nameBuilder.Append(type.Name);

        if (type.IsGenericTypeDefinition)
        {
            return nameBuilder.ToString();
        }

        if (type.IsGenericType)
        {
            nameBuilder.Append('[');
            nameBuilder.AppendJoin(',', type.GetGenericArguments().Select(a => $"[{a.GetSerializableTypeName(true)}]"));
            nameBuilder.Append(']');
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
