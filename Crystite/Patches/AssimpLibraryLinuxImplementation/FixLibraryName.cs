//
//  SPDX-FileName: FixLibraryName.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.AssimpLibraryLinuxImplementation;

/// <summary>
/// Fixes a bad library pathname.
/// </summary>
[HarmonyPatch("UnmanagedLibraryImplementation", "LoadLibrary")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class FixLibraryName
{
    /// <summary>
    /// Modifies the path name.
    /// </summary>
    /// <param name="path">The path name argument.</param>
    public static void Prefix(ref string path)
    {
        if (path is "Assimp64.so" or "Assimp32.so" or "libassimp.so")
        {
            path = "libassimp.so.5";
        }
    }
}
