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
using BaseX;
using CloudX.Shared;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using Grapevine;
using JetBrains.Annotations;
using NeosHeadless;
using Remora.Neos.Headless.API.Extensions;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for NeosVR worlds.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class WorldResource
{
    private readonly JobService _jobService;
    private readonly NeosHeadlessConfig _config;
    private readonly Engine _engine;
    private readonly WorldManager _worldManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldResource"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    /// <param name="config">The client configuration.</param>
    /// <param name="engine">The game engine.</param>
    /// <param name="worldManager">The world manager.</param>
    public WorldResource(JobService jobService, NeosHeadlessConfig config, Engine engine, WorldManager worldManager)
    {
        _jobService = jobService;
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
        var worlds = _worldManager.Worlds.Where(w => !w.IsUserspace()).Select(w => w.ToRestWorld());

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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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
    /// <remarks>
    /// This is an asynchronous route; it returns a job object that needs to be polled in order to determine when the
    /// action has completed.
    /// </remarks>
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
        var job = _jobService.CreateJob
        (
            $"start world {startInfo.LoadWorldURL ?? startInfo.LoadWorldPresetName}",
            _ => new WorldHandler(_engine, _config, startInfo).Start()
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        var job = _jobService.CreateJob
        (
            $"save world {world.Name}",
            _ => Userspace.SaveWorldAuto(world, SaveType.Overwrite, false)
        );

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Close the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("DELETE", "/worlds/{id}")]
    public async Task CloseWorldAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        world.Destroy();

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        var job = _jobService.CreateJob
        (
            $"restart world {world.Name}",
            _ => handler.Restart()
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.Count <= 0)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (data.TryGetValue("name", out var name))
        {
            world.Name = name;
        }

        if (data.TryGetValue("description", out var description))
        {
            world.Description = description;
        }

        if (data.TryGetValue("access_level", out var rawAccessLevel))
        {
            if (!Enum.TryParse<SessionAccessLevel>(rawAccessLevel, out var accessLevel))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            world.AccessLevel = accessLevel;
        }

        if (data.TryGetValue("away_kick_interval", out var rawAwayKickInterval))
        {
            if (!float.TryParse(rawAwayKickInterval, out var awayKickIntervalValue))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            if (awayKickIntervalValue < 0.0)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            world.AwayKickEnabled = awayKickIntervalValue >= 0.0;
            world.AwayKickMinutes = awayKickIntervalValue;
        }

        if (data.TryGetValue("hide_from_listing", out var rawHideFromListing))
        {
            if (!bool.TryParse(rawHideFromListing, out var hideFromListing))
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            world.HideFromListing = hideFromListing;
        }

        if (data.TryGetValue("max_users", out var rawMaxUsers))
        {
            if (!int.TryParse(rawMaxUsers, out var maxUsers) || maxUsers is < 1 or > 256)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }

            world.MaxUsers = maxUsers;
        }

        var json = JsonSerializer.Serialize(world.ToRestWorld());
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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
    /// Kicks the user identified by "user-id" in the world identified by "id".
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/worlds/{id}/users/{user-id}/kick")]
    public async Task KickWorldUserAsync(IHttpContext context)
    {
        var worldId = context.Request.PathParameters["id"];
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        if (user.IsHost || !user.LocalUser.CanKick())
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        user.Kick();

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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        if (user.IsHost || !user.LocalUser.CanBan())
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        user.Ban();

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        var data = await context.Request.ParseFormUrlEncodedData();
        if (data.TryGetValue("silenced", out var rawSilenced) || !bool.TryParse(rawSilenced, out var silenced))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        user.IsSilenced = silenced;

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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        user.Root?.Slot.Destroy();

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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

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

        var role = user.World.Permissions.Roles.FirstOrDefault<PermissionSet>
        (
            r => r.RoleName.Value.Equals(restUserRole.ToString(), StringComparison.InvariantCultureIgnoreCase)
        );

        if (role is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (role > user.World.HostUser.Role)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        user.Role = role;
        user.World.Permissions.AssignDefaultRole(user, role);

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Get the currently focused world.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/worlds/focused")]
    public async Task GetFocusedWorldAsync(IHttpContext context)
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
            var worldById = _worldManager.Worlds.FirstOrDefault(w => w.SessionId == worldId);
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

        var json = JsonSerializer.Serialize(world.ToRestWorld());
        await context.Response.SendResponseAsync(json);
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
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var tag = context.Request.PathParameters["tag"];

        var data = await context.Request.ParseFormUrlEncodedData();

        if (!data.TryGetValue("value", out var value))
        {
            var list = Pool.BorrowList<DynamicImpulseReceiver>();
            {
                world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                foreach (var dynamicImpulseReceiver in list)
                {
                    dynamicImpulseReceiver.Impulse.Trigger();
                }
            }
            Pool.Return(ref list);
        }
        else if (float.TryParse(value, out var floatValue))
        {
            var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<float>>();
            {
                world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                foreach (var dynamicImpulseReceiver in list)
                {
                    dynamicImpulseReceiver.Trigger(floatValue);
                }
            }
            Pool.Return(ref list);
        }
        else if (!int.TryParse(value, out var intValue))
        {
            var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<int>>();
            {
                world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                foreach (var dynamicImpulseReceiver in list)
                {
                    dynamicImpulseReceiver.Trigger(intValue);
                }
            }
            Pool.Return(ref list);
        }
        else
        {
            var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<string>>();
            {
                world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                foreach (var dynamicImpulseReceiver in list)
                {
                    dynamicImpulseReceiver.Trigger(value);
                }
            }
            Pool.Return(ref list);
        }
    }
}
