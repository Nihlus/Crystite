//
//  SPDX-FileName: SkipWebSocketNegotiation.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Crystite.Patches.SkyFrostInterface;

/// <summary>
/// Modifies the SignalR connection to skip the initial protocol negotiation and move straight to a WebSocket
/// connection.
///
/// Generally, this improves error tolerance and connection speed.
///
/// See <see href="https://github.com/dotnet/aspnetcore/issues/23679"/> for more information regarding the rationale.
/// </summary>
[HarmonyPatch("SkyFrost.Base.SkyFrostInterface+<>c__DisplayClass153_0", "<ConnectToHub>b__0")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class SkipWebSocketNegotiation
{
    /// <summary>
    /// Applies additional modifications to the HTTP connection options.
    /// </summary>
    /// <param name="options">The connection options.</param>
    [HarmonyPostfix]
    public static void Postfix(HttpConnectionOptions options)
    {
        options.Transports = HttpTransportType.WebSockets;
        options.SkipNegotiation = true;
    }
}
