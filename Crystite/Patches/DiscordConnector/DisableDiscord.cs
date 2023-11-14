//
//  SPDX-FileName: DisableDiscord.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.DiscordConnector;

/// <summary>
/// Disables the Discord platform interface.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.Interfacing.DiscordConnector), nameof(FrooxEngine.Interfacing.DiscordConnector.Initialize))]
[UsedImplicitly]
public static class DisableDiscord
{
    /// <summary>
    /// Overrides the Discord connector's initialization routine, disabling it outright.
    /// </summary>
    /// <param name="__result">The result of the original method.</param>
    /// <returns>Always false.</returns>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Required")]
    public static bool Prefix(out Task<bool> __result)
    {
        __result = Task.FromResult(false);
        return false;
    }
}
