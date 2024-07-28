//
//  SPDX-FileName: AssemblyNameExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="AssemblyName"/> class.
/// </summary>
public static class AssemblyNameExtensions
{
    /// <summary>
    /// Determines if the given assembly is version-compatible with the invoked-on assembly.
    /// </summary>
    /// <param name="name">The name of the target assembly.</param>
    /// <param name="other">The name of the requested assembly.</param>
    /// <returns>true if the assemblies are compatible; otherwise, false.</returns>
    public static bool IsCompatible(this AssemblyName name, AssemblyName other)
    {
        return name.Name == other.Name
               && name.Version?.Major == other.Version?.Major
               && name.Version?.Minor >= other.Version?.Minor;
    }
}
