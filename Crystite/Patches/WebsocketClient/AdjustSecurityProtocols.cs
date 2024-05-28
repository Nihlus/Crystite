//
//  SPDX-FileName: AdjustSecurityProtocols.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net;
using FrooxEngine;
using HarmonyLib;
using JetBrains.Annotations;

namespace Crystite.Patches.WebsocketClient;

/// <summary>
/// Patches the <see cref="WebsocketClient"/> class.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.WebsocketClient), "ConnectTo", MethodType.Async)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class AdjustSecurityProtocols
{
    /// <summary>
    /// Patches out Sslv3 from the configured security protocols.
    /// </summary>
    /// <param name="instructions">The instructions of the method.</param>
    /// <returns>The patched code.</returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        #pragma warning disable CS0618
        var target = SecurityProtocolType.Ssl3
                     | SecurityProtocolType.Tls
                     | SecurityProtocolType.Tls11
                     | SecurityProtocolType.Tls12;
        #pragma warning restore CS0618

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
