//
//  SPDX-FileName: WorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Microsoft.AspNetCore.Mvc;
using OneOf;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.Extensions;

namespace Remora.Neos.Headless.Routes;

/// <summary>
/// Defines API routes for world management.
/// </summary>
[ApiController]
[Route("worlds")]
public class WorldController : ControllerBase
{
    private readonly INeosWorldController _worldController;
    private readonly IJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldController"/> class.
    /// </summary>
    /// <param name="worldController">The world controller.</param>
    /// <param name="jobService">The job service.</param>
    public WorldController(INeosWorldController worldController, IJobService jobService)
    {
        _worldController = worldController;
        _jobService = jobService;
    }

    /// <summary>
    /// Gets the available worlds.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IRestWorld>>> GetWorldsAsync(CancellationToken ct = default)
        => (await _worldController.GetWorldsAsync(ct)).ToActionResult();

    /// <summary>
    /// Gets the available worlds.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<IRestWorld>> GetWorldAsync(string id, CancellationToken ct = default)
        => (await _worldController.GetWorldAsync(id, ct)).ToActionResult();

    /// <summary>
    /// Start a new world.
    /// </summary>
    /// <param name="worldUrl">The record URL of the world.</param>
    /// <param name="templateName">The template name of the world.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    public ActionResult<IJob> StartWorld
    (
        [FromForm(Name = "url")] Uri? worldUrl = null,
        [FromForm(Name = "template")] string? templateName = null
    )
    {
        if (worldUrl is null && templateName is null)
        {
            return BadRequest();
        }

        return _jobService.CreateJob
        (
            $"start world {worldUrl?.ToString() ?? templateName}",
            jct => _worldController.StartWorldAsync(worldUrl, templateName, jct)
        );
    }

    /// <summary>
    /// Saves the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/save")]
    public ActionResult<IJob> SaveWorld(string id) => _jobService.CreateJob
    (
        $"save world {id}",
        jct => _worldController.SaveWorldAsync(id, jct)
    );

    /// <summary>
    /// Closes the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpDelete]
    [Route("{id}")]
    public ActionResult<IJob> CloseWorld(string id) => _jobService.CreateJob
    (
        $"close world {id}",
        jct => _worldController.CloseWorldAsync(id, jct)
    );

    /// <summary>
    /// Restarts the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}")]
    public ActionResult<IJob> RestartWorld(string id) => _jobService.CreateJob
    (
        $"restart world {id}",
        jct => _worldController.RestartWorldAsync(id, jct)
    );

    /// <summary>
    /// Modifies the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="accessLevel">The new access level.</param>
    /// <param name="awayKickInterval">The new away kick interval.</param>
    /// <param name="hideFromListing">Whether the world should be hidden from public listings.</param>
    /// <param name="maxUsers">The new maximum user count.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPatch]
    [Route("{id}")]
    public async Task<ActionResult<IRestWorld>> ModifyWorldAsync
    (
        string id,
        [FromForm] string? name = null,
        [FromForm] string? description = null,
        [FromForm(Name = "access_level")] RestAccessLevel? accessLevel = null,
        [FromForm(Name = "away_kick_interval")] float? awayKickInterval = null,
        [FromForm(Name = "hide_from_listing")] bool? hideFromListing = null,
        [FromForm(Name = "max_users")] int? maxUsers = null,
        CancellationToken ct = default
    )
    {
        var modify = await _worldController.ModifyWorldAsync
        (
            id,
            name,
            description,
            accessLevel,
            awayKickInterval,
            hideFromListing,
            maxUsers,
            ct
        );

        return modify.ToActionResult();
    }

    /// <summary>
    /// Gets the users in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpGet]
    [Route("{id}/users")]
    public async Task<ActionResult<IReadOnlyList<IRestUser>>> GetWorldUsersAsync
    (
        string id,
        CancellationToken ct = default
    )
        => (await _worldController.GetWorldUsersAsync(id, ct)).ToActionResult();

    /// <summary>
    /// Gets the given user in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpGet]
    [Route("{id}/users/{userIdOrName}")]
    public async Task<ActionResult<IRestUser>> GetWorldUserAsync
    (
        string id,
        string userIdOrName,
        CancellationToken ct = default
    )
        => (await _worldController.GetWorldUserAsync(id, userIdOrName, ct)).ToActionResult();

    /// <summary>
    /// Kicks the given user from the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/users/{userIdOrName}/kick")]
    public async Task<ActionResult> KickWorldUserAsync
    (
        string id,
        string userIdOrName,
        CancellationToken ct = default
    ) => (await _worldController.KickWorldUserAsync(id, userIdOrName, ct)).ToActionResult();

    /// <summary>
    /// Bans the given user from the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/users/{userIdOrName}/ban")]
    public async Task<ActionResult<IRestBan>> BanWorldUserAsync
    (
        string id,
        string userIdOrName,
        CancellationToken ct = default
    ) => (await _worldController.BanWorldUserAsync(id, userIdOrName, ct)).ToActionResult();

    /// <summary>
    /// Silences or unsilences the given user in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="isSilenced">Whether the user is silenced.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/users/{userIdOrName}/silence")]
    public async Task<ActionResult> BanWorldUserAsync
    (
        string id,
        string userIdOrName,
        [FromForm(Name = "silenced")] bool isSilenced,
        CancellationToken ct = default
    ) => (await _worldController.SilenceUnsilenceWorldUserAsync(id, userIdOrName, isSilenced, ct)).ToActionResult();

    /// <summary>
    /// Respawns the given user in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/users/{userIdOrName}/respawn")]
    public async Task<ActionResult> RespawnWorldUserAsync
    (
        string id,
        string userIdOrName,
        CancellationToken ct = default
    ) => (await _worldController.RespawnWorldUserAsync(id, userIdOrName, ct)).ToActionResult();

    /// <summary>
    /// Sets the given user's role in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="role">The new role.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPut]
    [Route("{id}/users/{userIdOrName}/role")]
    public async Task<ActionResult> SetWorldUserRoleAsync
    (
        string id,
        string userIdOrName,
        [FromForm] RestUserRole role,
        CancellationToken ct = default
    ) => (await _worldController.SetWorldUserRoleAsync(id, userIdOrName, role, ct)).ToActionResult();

    /// <summary>
    /// Focuses the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPut]
    [Route("focused")]
    public async Task<ActionResult> FocusWorldAsync([FromForm] string id, CancellationToken ct = default)
        => (await _worldController.FocusWorldAsync(id, ct)).ToActionResult();

    /// <summary>
    /// Sends a dynamic impulse to the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="tag">The impulse tag.</param>
    /// <param name="value">The value to send to the impulse.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpPost]
    [Route("{id}/impulses/{tag}")]
    public async Task<ActionResult> SendImpulseAsync
    (
        string id,
        string tag,
        [FromForm] OneOf<int, float, string>? value = null,
        CancellationToken ct = default
    )
        => (await _worldController.SendImpulseAsync(id, tag, value, ct)).ToActionResult();
}
