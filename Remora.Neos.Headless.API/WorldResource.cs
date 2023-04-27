//
//  SPDX-FileName: WorldResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FrooxEngine;
using Grapevine;
using Remora.Neos.Headless.API.Extensions;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for NeosVR worlds.
/// </summary>
[RestResource]
public class WorldResource
{
    private readonly WorldManager _worldManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldResource"/> class.
    /// </summary>
    /// <param name="worldManager">The world manager.</param>
    public WorldResource(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    /// <summary>
    /// Gets the available worlds.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds")]
    public async Task GetWorldsAsync(IHttpContext context)
    {
        var worlds = _worldManager.Worlds.Select(w => w.ToRestWorld());

        var json = JsonSerializer.Serialize(worlds);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Gets a world by its ID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}")]
    public async Task GetWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var json = JsonSerializer.Serialize(world.ToRestWorld());
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Start a world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds")]
    public async Task StartWorldAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Save the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/save")]
    public async Task SaveWorldAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Close the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/close")]
    public async Task CloseWorldAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Restart the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/restart")]
    public async Task RestartWorldAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Set the name of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/name")]
    public async Task SetWorldNameAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Set the access level of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/access-level")]
    public async Task SetWorldAccessLevelAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Set the description of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/description")]
    public async Task SetWorldDescriptionAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Set the away kick interval of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/away-kick-interval")]
    public async Task SetWorldAwayKickIntervalAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Get the users in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/users")]
    public async Task GetWorldUsersAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Get the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/users/{user-id}")]
    public async Task GetWorldUserAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Get the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/focused")]
    public async Task GetFocusedWorldsAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Set the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PUT", "/worlds/focused")]
    public async Task FocusWorldAsync(IHttpContext context)
    {
        await context.Response.SendResponseAsync(HttpStatusCode.ServiceUnavailable);
    }
}
