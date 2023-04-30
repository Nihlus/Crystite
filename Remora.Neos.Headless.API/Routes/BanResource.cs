//
//  SPDX-FileName: BanResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FrooxEngine;
using Grapevine;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for handling bans.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class BanResource
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="BanResource"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public BanResource(Engine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Gets the active bans.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/bans")]
    public async Task GetBansAsync(IHttpContext context)
    {
        var bans = new List<RestBan>();
        foreach (var listSetting in Settings.ListSettings("Security.Ban.Blacklist"))
        {
            var banPath = "Security.Ban.Blacklist." + listSetting + ".";

            var username = Settings.ReadValue<string>(banPath + "Username", "N/A");
            var id = Settings.ReadValue<string>(banPath + "UserId", "N/A");
            var machineId = Settings.ReadValue<string?>(banPath + "MachineId", null);

            bans.Add(new RestBan(id, username, machineId));
        }

        var json = JsonSerializer.Serialize(bans);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Bans the identified user from all sessions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/bans/{id}")]
    public async Task BanUserAsync(IHttpContext context)
    {
        var idOrUsername = context.Request.PathParameters["id"];

        var getUser = await _engine.Cloud.GetUser(idOrUsername);
        if (!getUser.IsOK)
        {
            getUser = await _engine.Cloud.GetUserByName(idOrUsername);
            if (!getUser.IsOK)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }
        }

        var user = getUser.Entity;

        if (!BanManager.IsBanned(user.Id, null, null))
        {
            BanManager.AddToBanList(user.Username, user.Id, null, null);
        }

        var json = JsonSerializer.Serialize(new RestBan(user.Id, user.Username));
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Unbans the identified user.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("DELETE", "/bans/{id}")]
    public async Task UnbanUserAsync(IHttpContext context)
    {
        var idOrUsername = context.Request.PathParameters["id"];

        var getUser = await _engine.Cloud.GetUser(idOrUsername);
        if (!getUser.IsOK)
        {
            getUser = await _engine.Cloud.GetUserByName(idOrUsername);
            if (!getUser.IsOK)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }
        }

        var user = getUser.Entity;

        if (!BanManager.IsBanned(user.Id, null, null))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
            return;
        }

        BanManager.RemoveBanByUserId(user.Id);

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }
}
