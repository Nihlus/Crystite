//
//  SPDX-FileName: HeadlessApiMod.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Reflection;
using FrooxEngine;
using Grapevine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeosHeadless;
using NeosModLoader;
using Remora.Neos.Headless.API.Extensions;
using Remora.Neos.Headless.API.Mod.Implementations;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines mod-related information, such as the name and its entrypoint.
/// </summary>
public class HeadlessApiMod : NeosMod
{
    [AutoRegisterConfigKey]
    private readonly ModConfigurationKey<string> _listenAddressKey = new
    (
        "listen_address",
        "The address to listen for REST requests on.",
        () => "127.0.0.1"
    );

    [AutoRegisterConfigKey]
    private readonly ModConfigurationKey<ushort> _listenPortKey = new
    (
        "listen_port",
        "The port to listen for REST requests on.",
        () => 12594
    );

    private IRestServer _server = null!;

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

        Engine.Current.OnShutdown += OnEngineShutdown;

        var modConfig = GetConfiguration() ?? throw new InvalidOperationException();

        if (!modConfig.TryGetValue(_listenAddressKey, out var listenAddress))
        {
            Error("No listen address defined.");
            return;
        }

        if (!modConfig.TryGetValue(_listenPortKey, out var listenPort))
        {
            Error("No listen port defined.");
            return;
        }

        var configField = Assembly.GetAssembly(typeof(WorldHandler))
            .GetType("NeosHeadless.Program")
            .GetField("config", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException();

        var serverBuilder = RestServerBuilder.UseDefaults();

        serverBuilder.ConfigureServer = s =>
        {
            s.Prefixes.Add($"http://{listenAddress}:{listenPort}/");
            s.Router.Options.SendExceptionMessages = true;
        };

        serverBuilder.ConfigureServices = s => s
            .AddLogging(l => l.ClearProviders())
            .AddNeosControllerServices<NeosApplicationController, StockHeadlessNeosWorldController>()
            .AddSingleton(Engine.Current)
            .AddSingleton(serv => serv.GetRequiredService<Engine>().WorldManager)
            .AddSingleton((NeosHeadlessConfig)configField.GetValue(null));

        _server = serverBuilder.Build();
        _server.Start();
    }

    private void OnEngineShutdown()
    {
        _server.Stop();
        _server.Dispose();
    }
}
