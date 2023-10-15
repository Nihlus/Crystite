//
//  SPDX-FileName: VersionHelpers.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;

namespace Crystite.Helpers;

/// <summary>
/// Contains various version-related helper functions.
/// </summary>
public static class VersionHelpers
{
    /// <summary>
    /// Gets the version of Resonite the program was built against.
    /// </summary>
    public static Version ResoniteVersion { get; }

    /// <summary>
    /// Initializes static members of the <see cref="VersionHelpers"/> class.
    /// </summary>
    static VersionHelpers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(am => am.Key is "ResoniteVersion");

        if (attribute is null || !Version.TryParse(attribute.Value, out var resoniteVersion))
        {
            throw new InvalidOperationException
            (
                "Failed to determine the version of Resonite the program was built against"
            );
        }

        ResoniteVersion = resoniteVersion;
    }
}
