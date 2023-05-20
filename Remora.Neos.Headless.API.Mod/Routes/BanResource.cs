//
//  SPDX-FileName: BanResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Mod.Extensions;
using Remora.Results;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines API routes for handling bans.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class BanResource
{
    private readonly INeosBanController _banController;

    /// <summary>
    /// Initializes a new instance of the <see cref="BanResource"/> class.
    /// </summary>
    /// <param name="banController">The ban controller.</param>
    public BanResource(INeosBanController banController)
    {
        _banController = banController;
    }

    /// <summary>
    /// Gets the active bans.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/bans")]
    public async Task GetBansAsync(IHttpContext context)
    {
        var bans = await _banController.GetBansAsync(context.CancellationToken);

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
        var userIdOrName = context.Request.PathParameters["id"];

        var banUser = await _banController.BanUserAsync(userIdOrName, context.CancellationToken);
        if (!banUser.IsDefined(out var ban))
        {
            await context.Response.SendResponseAsync(banUser.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(ban);
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
        var userIdOrName = context.Request.PathParameters["id"];

        var unbanUser = await _banController.UnbanUserAsync(userIdOrName, context.CancellationToken);
        if (!unbanUser.IsSuccess)
        {
            await context.Response.SendResponseAsync(unbanUser.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }
}
