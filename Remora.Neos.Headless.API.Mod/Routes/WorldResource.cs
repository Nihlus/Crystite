//
//  SPDX-FileName: WorldResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using OneOf;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.API.Mod.Extensions;
using Remora.Neos.Headless.API.Services;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines API routes for NeosVR worlds.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class WorldResource
{
    private readonly IJobService _jobService;
    private readonly INeosWorldController _worldController;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldResource"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    /// <param name="worldController">The world controller.</param>
    public WorldResource(JobService jobService, INeosWorldController worldController)
    {
        _jobService = jobService;
        _worldController = worldController;
    }

    /// <summary>
    /// Gets the available worlds.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds")]
    public async Task GetWorldsAsync(IHttpContext context)
    {
        var getWorlds = await _worldController.GetWorldsAsync(context.CancellationToken);
        if (!getWorlds.IsDefined(out var worlds))
        {
            await context.Response.SendResponseAsync(getWorlds.Error.ToStatusCode());
            return;
        }

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

        var getWorld = await _worldController.GetWorldAsync(worldId, context.CancellationToken);
        if (!getWorld.IsDefined(out var world))
        {
            await context.Response.SendResponseAsync(getWorld.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(world);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Start a world.
    /// </summary>
    /// <remarks>
    /// This is an asynchronous route; it returns a job object that needs to be polled in order to determine when the
    /// action has completed.
    /// </remarks>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds")]
    public async Task StartWorldAsync(IHttpContext context)
    {
        Uri? worldUrl = null;
        string? templateName = null;

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.TryGetValue("url", out var rawUrl))
        {
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out worldUrl))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }
        }
        else if (data.TryGetValue("template", out templateName))
        {
            // okay
        }
        else
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var job = _jobService.CreateJob
        (
            $"start world {worldUrl?.ToString() ?? templateName}",
            _ => _worldController.StartWorldAsync(worldUrl, templateName, context.CancellationToken)
        );

        var json = JsonSerializer.Serialize(job);

        context.Response.StatusCode = HttpStatusCode.Created;
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Save the world identified by "id".
    /// </summary>
    /// <remarks>
    /// This is an asynchronous route; it returns a job object that needs to be polled in order to determine when the
    /// action has completed.
    /// </remarks>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/save")]
    public async Task SaveWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];

        var job = _jobService.CreateJob
        (
            $"save world {worldId}",
            jct => _worldController.SaveWorldAsync(worldId, jct)
        );

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Close the world identified by "id".
    /// </summary>
    /// <remarks>
    /// This is an asynchronous route; it returns a job object that needs to be polled in order to determine when the
    /// action has completed.
    /// </remarks>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("DELETE", "/worlds/{id}")]
    public async Task CloseWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];

        var job = _jobService.CreateJob
        (
            $"close world {worldId}",
            _ => _worldController.CloseWorldAsync(worldId, context.CancellationToken)
        );

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Restart the world identified by "id".
    /// </summary>
    /// <remarks>
    /// This is an asynchronous route; it returns a job object that needs to be polled in order to determine when the
    /// action has completed.
    /// </remarks>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/restart")]
    public async Task RestartWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];

        var job = _jobService.CreateJob
        (
            $"restart world {worldId}",
            _ => _worldController.RestartWorldAsync(worldId, context.CancellationToken)
        );

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Set the name of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}")]
    public async Task ModifyWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.Count <= 0)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        _ = data.TryGetValue("name", out var name);
        _ = data.TryGetValue("description", out var description);

        RestAccessLevel? accessLevel = null;
        if (data.TryGetValue("access_level", out var rawAccessLevel))
        {
            if (!Enum.TryParse<RestAccessLevel>(rawAccessLevel, out var accessLevelValue))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            accessLevel = accessLevelValue;
        }

        float? awayKickInterval = null;
        if (data.TryGetValue("away_kick_interval", out var rawAwayKickInterval))
        {
            if (!float.TryParse(rawAwayKickInterval, out var awayKickIntervalValue))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            awayKickInterval = awayKickIntervalValue;
        }

        bool? hideFromListing = null;
        if (data.TryGetValue("hide_from_listing", out var rawHideFromListing))
        {
            if (!bool.TryParse(rawHideFromListing, out var hideFromListingValue))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            hideFromListing = hideFromListingValue;
        }

        int? maxUsers = null;
        if (data.TryGetValue("max_users", out var rawMaxUsers))
        {
            if (!int.TryParse(rawMaxUsers, out var maxUsersValue))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            maxUsers = maxUsersValue;
        }

        var modifyWorld = await _worldController.ModifyWorldAsync
        (
            worldId,
            name,
            description,
            accessLevel,
            awayKickInterval,
            hideFromListing,
            maxUsers,
            context.CancellationToken
        );

        if (!modifyWorld.IsDefined(out var world))
        {
            await context.Response.SendResponseAsync(modifyWorld.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(world);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Get the users in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/users")]
    public async Task GetWorldUsersAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];

        var getUsers = await _worldController.GetWorldUsersAsync(worldId, context.CancellationToken);
        if (!getUsers.IsDefined(out var users))
        {
            await context.Response.SendResponseAsync(getUsers.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(users);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Get the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/users/{user-id}")]
    public async Task GetWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var getUser = await _worldController.GetWorldUserAsync(worldId, userId, context.CancellationToken);
        if (!getUser.IsDefined(out var user))
        {
            await context.Response.SendResponseAsync(getUser.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(user);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Kicks the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/users/{user-id}/kick")]
    public async Task KickWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var kickUser = await _worldController.KickWorldUserAsync(worldId, userId, context.CancellationToken);
        if (!kickUser.IsSuccess)
        {
            await context.Response.SendResponseAsync(kickUser.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Bans the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/users/{user-id}/ban")]
    public async Task BanWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var banUser = await _worldController.BanWorldUserAsync(worldId, userId, context.CancellationToken);
        if (!banUser.IsDefined(out var ban))
        {
            await context.Response.SendResponseAsync(banUser.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(ban);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Silences or unsilences the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/users/{user-id}/silence")]
    public async Task SilenceUnsilenceWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.TryGetValue("silenced", out var rawSilenced) || !bool.TryParse(rawSilenced, out var isSilenced))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var silenceUnsilenceUser = await _worldController.SilenceUnsilenceWorldUserAsync
        (
            worldId,
            userId,
            isSilenced,
            context.CancellationToken
        );

        if (!silenceUnsilenceUser.IsSuccess)
        {
            await context.Response.SendResponseAsync(silenceUnsilenceUser.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Respawns the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/users/{user-id}/respawn")]
    public async Task RespawnWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var banUser = await _worldController.RespawnWorldUserAsync(worldId, userId, context.CancellationToken);
        if (!banUser.IsSuccess)
        {
            await context.Response.SendResponseAsync(banUser.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Sets the role of the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PUT", "/worlds/{id}/users/{user-id}/role")]
    public async Task SetWorldUserRoleAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var userId = context.Request.PathParameters["user-id"];

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("role", out var rawRestUserRole))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!Enum.TryParse<RestUserRole>(rawRestUserRole, out var restUserRole))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var setRole = await _worldController.SetWorldUserRoleAsync
        (
            worldId,
            userId,
            restUserRole,
            context.CancellationToken
        );

        if (!setRole.IsSuccess)
        {
            await context.Response.SendResponseAsync(setRole.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Set the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PUT", "/worlds/focused")]
    public async Task FocusWorldAsync(IHttpContext context)
    {
        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("id", out var worldId))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var focusWorld = await _worldController.FocusWorldAsync(worldId, context.CancellationToken);
        if (!focusWorld.IsSuccess)
        {
            await context.Response.SendResponseAsync(focusWorld.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Sends a dynamic impulse to the given world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/impulses/{tag}")]
    public async Task SendImpulseAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var tag = context.Request.PathParameters["tag"];

        var data = await context.Request.ParseFormUrlEncodedData();

        OneOf<int, float, string>? value;
        if (!data.TryGetValue("value", out var raw))
        {
            value = null;
        }
        else if (int.TryParse(raw, out var intValue))
        {
            value = intValue;
        }
        else if (float.TryParse(raw, out var floatValue))
        {
            value = floatValue;
        }
        else
        {
            value = raw;
        }

        var sendImpulse = await _worldController.SendImpulseAsync(worldId, tag, value, context.CancellationToken);
        if (!sendImpulse.IsSuccess)
        {
            await context.Response.SendResponseAsync(sendImpulse.Error.ToStatusCode());
            return;
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }
}
