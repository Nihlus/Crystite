//
//  SPDX-FileName: LibraryInitializerPatch.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net;
using System.Reflection.Emit;
using FrooxEngine;
using HarmonyLib;

namespace Remora.Neos.Headless.Patches;

/// <summary>
/// Patches the <see cref="LibraryInitializer"/> class.
/// </summary>
[HarmonyPatch(typeof(LibraryInitializer), nameof(LibraryInitializer.Initialize))]
public static class LibraryInitializerPatch
{
    /// <summary>
    /// Patches out Sslv3 from the configured security protocols.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var target = SecurityProtocolType.Ssl3
                     | SecurityProtocolType.Tls
                     | SecurityProtocolType.Tls11
                     | SecurityProtocolType.Tls12;

        var replacement = SecurityProtocolType.Tls
                     | SecurityProtocolType.Tls11
                     | SecurityProtocolType.Tls12;

        foreach (var instruction in instructions)
        {
            if (instruction.LoadsConstant(target))
            {
                var newInstruction = instruction.Clone((int)replacement);
                yield return newInstruction;
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
