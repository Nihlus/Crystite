//
//  SPDX-FileName: HeadlessApiMod.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Reflection;
using NeosModLoader;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines mod-related information, such as the name and its entrypoint.
/// </summary>
public class HeadlessApiMod : NeosMod
{
    private readonly ModConfigurationKey<string> _listenAddressKey = new
    (
        "listen_address",
        "The address to listen for REST requests on.",
        () => "127.0.0.1"
    );

    private readonly ModConfigurationKey<ushort> _listenPortKey = new
    (
        "listen_port",
        "The port to listen for REST requests on.",
        () => 12594
    );

    /// <inheritdoc />
    public override string Name => "Headless REST API";

    /// <inheritdoc />
    public override string Author => "Jarl Gullberg";

    /// <inheritdoc />
    public override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    /// <inheritdoc />
    public override string Link => "https://github.com/Remora/Remora.Neos.Headless.API";

    /// <inheritdoc />
    public override void OnEngineInit()
    {
        base.OnEngineInit();
    }

    /// <inheritdoc />
    public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        base.DefineConfiguration(builder);
        builder
            .Version(this.Version)
            .Key(_listenAddressKey)
            .Key(_listenPortKey);
    }
}
