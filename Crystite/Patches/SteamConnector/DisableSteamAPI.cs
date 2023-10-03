//
//  SPDX-FileName: DisableSteamAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.SteamConnector;

/// <summary>
/// Bails out early of the Steam connection setup, depending on the configuration.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.SteamConnector), nameof(FrooxEngine.SteamConnector.InitializeSteamAPI))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class DisableSteamAPI
{
    /// <summary>
    /// Gets or sets a value indicating whether Steam API initialization should be attempted.
    /// </summary>
    public static bool ShouldAttemptSteamInitialization { get; set; }

    /// <summary>
    /// Bails out early of the Steam connection setup, depending on the configuration.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var initAttemptedSetter = AccessTools.PropertySetter
        (
            typeof(FrooxEngine.SteamConnector),
            nameof(FrooxEngine.SteamConnector.InitializationAttempted)
        );

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(initAttemptedSetter))
            {
                yield return instruction;

                if (ShouldAttemptSteamInitialization)
                {
                    continue;
                }

                yield return new CodeInstruction(OpCodes.Ret);
                yield break;
            }

            yield return instruction;
        }
    }
}
