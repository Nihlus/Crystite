//
//  SPDX-FileName: NormalizeGetNiceFullName.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace Crystite.Patches.ReflectionExtensions;

/// <summary>
/// Patches <see cref="Elements.Core.ReflectionExtensions.GetNiceFullName"/> to have consistent behaviour across Mono
/// and modern .NET.
/// </summary>
[HarmonyPatch(typeof(Elements.Core.ReflectionExtensions), nameof(Elements.Core.ReflectionExtensions.GetNiceFullName))]
public static class NormalizeGetNiceFullName
{
    /// <summary>
    /// Postfixes the method.
    /// </summary>
    /// <param name="__result">The result of the original method.</param>
    /// <param name="type">The type to get the nice name of.</param>
    /// <param name="open">The string to use for opening generic parameter lists.</param>
    /// <param name="close">The string to use for closing generic parameter lists.</param>
    /// <param name="nested">The string to use for appending nested types.</param>
    /// <param name="includeGenericParameters">Whether generic parameters should be included.</param>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by Harmony")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Required by Harmony")]
    public static void Postfix
    (
        ref string __result,
        Type type,
        string open = "<",
        string close = ">",
        string nested = "+",
        bool includeGenericParameters = true
    )
    {
        if (!type.IsArray)
        {
            return;
        }

        if (type.Namespace is null)
        {
            // on Mono or a mono-like runtime, no change required
            return;
        }

        // "break" the function when we have an encompassing type that has a namespace, as is the case with modern .NET
        // to match the behaviour of Mono and Mono-like runtimes.
        //
        // Mono doesn't place compiler-intrinsic encompassing types in any namespace while modern .NET does, so we need
        // to strip it out in these cases.
        __result = __result[(__result.IndexOf(type.Namespace, StringComparison.Ordinal) + type.Namespace.Length + 1)..];
    }
}
