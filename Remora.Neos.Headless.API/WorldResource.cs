//
//  SPDX-FileName: WorldResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CloudX.Shared;
using FrooxEngine;
using Grapevine;
using NeosHeadless;
using Remora.Neos.Headless.API.Extensions;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for NeosVR worlds.
/// </summary>
[RestResource]
public class WorldResource
{
    private readonly NeosHeadlessConfig _config;
    private readonly Engine _engine;
    private readonly WorldManager _worldManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldResource"/> class.
    /// </summary>
    /// <param name="config">The client configuration.</param>
    /// <param name="engine">The game engine.</param>
    /// <param name="worldManager">The world manager.</param>
    public WorldResource(NeosHeadlessConfig config, Engine engine, WorldManager worldManager)
    {
        _worldManager = worldManager;
        _config = config;
        _engine = engine;
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
        WorldStartupParameters startInfo;

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.TryGetValue("url", out var url))
        {
            startInfo = new WorldStartupParameters
            {
                LoadWorldURL = url
            };
        }
        else if (data.TryGetValue("template", out var template))
        {
            startInfo = new WorldStartupParameters
            {
                LoadWorldPresetName = template
            };

            var preset = (await WorldPresets.GetPresets()).FirstOrDefault<WorldPreset>
            (
                p =>
                {
                    var name = p.Name;
                    return name != null && name.Equals
                    (
                        startInfo.LoadWorldPresetName,
                        StringComparison.InvariantCultureIgnoreCase
                    );
                }
            );

            if (preset == null)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }
        }
        else
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        lock (_config)
        {
            _config.StartWorlds ??= new List<WorldStartupParameters>();
            _config.StartWorlds.Add(startInfo);
        }

        // that's the way it is, chief
        // ReSharper disable once InconsistentlySynchronizedField
        await new WorldHandler(_engine, _config, startInfo).Start();

        await context.Response.SendResponseAsync(HttpStatusCode.Created);
    }

    /// <summary>
    /// Save the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/save")]
    public async Task SaveWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        if (!Userspace.CanSave(world))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        // TODO: This can take a very long time - implement remote task system
        await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Close the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/close")]
    public async Task CloseWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        world.Destroy();

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Restart the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/restart")]
    public async Task RestartWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var handler = WorldHandler.GetHandler(world);
        if (handler == null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        _ = await handler.Restart();
        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Set the name of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/name")]
    public async Task SetWorldNameAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("name", out var name))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        world.Name = name;

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Set the access level of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/access-level")]
    public async Task SetWorldAccessLevelAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("access-level", out var rawAccessLevel) || !Enum.TryParse<SessionAccessLevel>(rawAccessLevel, out var accessLevel))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        world.AccessLevel = accessLevel;

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Set the description of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/worlds/{id}/description")]
    public async Task SetWorldDescriptionAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("description", out var description))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        world.Description = description;

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }

    /// <summary>
    /// Set the away kick interval of the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/{id}/away-kick-interval")]
    public async Task SetWorldAwayKickIntervalAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("away-kick-interval", out var rawAwayKickInterval))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!float.TryParse(rawAwayKickInterval, out var awayKickIntervalValue))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        world.AwayKickEnabled = awayKickIntervalValue > 0.0;
        world.AwayKickMinutes = awayKickIntervalValue;

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
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
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var users = world.AllUsers.Select(u => u.ToRestUser());
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
        var world = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var userId = context.Request.PathParameters["user-id"];
        var user = world.AllUsers.FirstOrDefault(u => u.UserID == userId);
        if (user is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var json = JsonSerializer.Serialize(user.ToRestUser());
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Get the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/focused")]
    public async Task GetFocusedWorldsAsync(IHttpContext context)
    {
        var world = _worldManager.FocusedWorld;
        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var json = JsonSerializer.Serialize(world.ToRestWorld());
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Set the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PUT", "/worlds/focused")]
    public async Task FocusWorldAsync(IHttpContext context)
    {
        World world;

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.TryGetValue("id", out var worldId))
        {
            var worldById = _worldManager.Worlds.FirstOrDefault(w => w.CorrespondingWorldId == worldId);
            if (worldById is null)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }

            world = worldById;
        }
        else if (data.TryGetValue("name", out var worldName))
        {
            worldName = worldName.Trim();
            var worldByName = _worldManager.GetWorld(w => w.RawName == worldName || w.SessionId == worldName);
            if (worldByName is null)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }

            world = worldByName;
        }
        else if (data.TryGetValue("index", out var rawWorldIndex))
        {
            if (!int.TryParse(rawWorldIndex, out var worldIndex) || worldIndex < 0)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            var nonUserspaceWorlds = _worldManager.Worlds.Where(w => w != Userspace.UserspaceWorld).ToArray();
            if (worldIndex >= nonUserspaceWorlds.Length)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            world = nonUserspaceWorlds[worldIndex];
        }
        else
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        _worldManager.FocusWorld(world);

        await context.Response.SendResponseAsync(HttpStatusCode.Ok);
    }
}
