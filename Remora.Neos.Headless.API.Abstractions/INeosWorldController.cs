//
//  SPDX-FileName: INeosWorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OneOf;
using Remora.Results;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents the public API of world-related functionality of a headless NeosVR client.
/// </summary>
[PublicAPI]
public interface INeosWorldController
{
    /// <summary>
    /// Gets the available worlds.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IReadOnlyList<IRestWorld>>> GetWorldsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the specified world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestWorld>> GetWorldAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Starts a new world based on the given URL or template name.
    /// </summary>
    /// <param name="worldUrl">The world resource URL.</param>
    /// <param name="templateName">The name of the preset template.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestWorld>> StartWorldAsync
    (
        Uri? worldUrl = null,
        string? templateName = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Saves the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> SaveWorldAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Closes the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> CloseWorldAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Restarts the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestWorld>> RestartWorldAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Modifies the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="accessLevel">The new access level.</param>
    /// <param name="awayKickInterval">The new away kick interval.</param>
    /// <param name="hideFromListing">Whether the world should be hidden from public listings.</param>
    /// <param name="maxUsers">The new maximum user count.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestWorld>> ModifyWorldAsync
    (
        string worldId,
        string? name = null,
        string? description = null,
        RestAccessLevel? accessLevel = null,
        float? awayKickInterval = null,
        bool? hideFromListing = null,
        int? maxUsers = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets the users in the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IReadOnlyList<IRestUser>>> GetWorldUsersAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Gets the given user in the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestUser>> GetWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default);

    /// <summary>
    /// Kicks the given user from the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> KickWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default);

    /// <summary>
    /// Bans the given user from the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestBan>> BanWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default);

    /// <summary>
    /// Silences or unsilences the given user in the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="isSilenced">Whether the user should be silenced.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> SilenceUnsilenceWorldUserAsync
    (
        string worldId,
        string userIdOrName,
        bool isSilenced,
        CancellationToken ct = default
    );

    /// <summary>
    /// Respawns the given user in the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> RespawnWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default);

    /// <summary>
    /// Sets the role of the given user in the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="userIdOrName">The ID or username of the user.</param>
    /// <param name="userRole">The new role of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> SetWorldUserRoleAsync
    (
        string worldId,
        string userIdOrName,
        RestUserRole userRole,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets the focused world.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestWorld>> GetFocusedWorldAsync(CancellationToken ct = default);

    /// <summary>
    /// Focuses the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> FocusWorldAsync(string worldId, CancellationToken ct = default);

    /// <summary>
    /// Sends a dynamic impulse to the given world.
    /// </summary>
    /// <param name="worldId">The ID of the world.</param>
    /// <param name="tag">The tag name of the dynamic impulse.</param>
    /// <param name="value">The value to send to the impulse.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> SendImpulseAsync
    (
        string worldId,
        string tag,
        OneOf<int, float, string>? value = null,
        CancellationToken ct = default
    );
}
