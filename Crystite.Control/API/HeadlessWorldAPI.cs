//
//  SPDX-FileName: HeadlessWorldAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Specialized;
using System.Text.Json;
using Crystite.API.Abstractions;
using Crystite.API.Abstractions.Services;
using Crystite.Control.Extensions;
using OneOf;
using Remora.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace Crystite.Control.API;

/// <summary>
/// Defines API endpoints for the headless world API.
/// </summary>
public class HeadlessWorldAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessWorldAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessWorldAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Gets the currently running worlds.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The worlds.</returns>
    public Task<Result<IReadOnlyList<IRestWorld>>> GetWorldsAsync(CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IReadOnlyList<IRestWorld>>("worlds", ct: ct);

    /// <summary>
    /// Gets the currently running world identified by the given ID.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The world.</returns>
    public Task<Result<IRestWorld>> GetWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IRestWorld>($"world/{id}", ct: ct);

    /// <summary>
    /// Starts a world using the given start argument.
    /// </summary>
    /// <param name="startArgument">The startup argument.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous job description.</returns>
    public Task<Result<IRestJob>> StartWorldAsync(OneOf<string, Uri> startArgument, CancellationToken ct = default)
    {
        var form = new FormUrlEncodedContent
        (
            new[]
            {
                startArgument.Match
                (
                    template => new KeyValuePair<string, string>("template", template),
                    uri => new KeyValuePair<string, string>("uri", uri.ToString())
                )
            }
        );

        return this.RestHttpClient.PostAsync<IRestJob>
        (
            "worlds",
            r => r.With(m => m.Content = form),
            ct: ct
        );
    }

    /// <summary>
    /// Saves the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous job description.</returns>
    public Task<Result<IRestJob>> SaveWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.PostAsync<IRestJob>($"worlds/{id}/save", ct: ct);

    /// <summary>
    /// Closes the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous job description.</returns>
    public Task<Result<IRestJob>> CloseWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.DeleteAsync<IRestJob>($"worlds/{id}", ct: ct);

    /// <summary>
    /// Restarts the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous job description.</returns>
    public Task<Result<IRestJob>> RestartWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.PostAsync<IRestJob>($"worlds/{id}/restart", ct: ct);

    /// <summary>
    /// Modifies the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="name">The new name of the world.</param>
    /// <param name="description">The new description of the world.</param>
    /// <param name="accessLevel">The new access level of the world.</param>
    /// <param name="awayKickInterval">The new away kick interval for users.</param>
    /// <param name="hideFromListing">Whether the world should be hidden from public listings.</param>
    /// <param name="maxUsers">The new maximum number of users in the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>Thw updated world.</returns>
    public Task<Result<IRestWorld>> ModifyWorldAsync
    (
        string id,
        Optional<string> name = default,
        Optional<string> description = default,
        Optional<RestAccessLevel> accessLevel = default,
        Optional<TimeSpan> awayKickInterval = default,
        Optional<bool> hideFromListing = default,
        Optional<int> maxUsers = default,
        CancellationToken ct = default
    )
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            { "name", name },
            { "description", description },
            { "access_level", accessLevel },
            { "away_kick_interval", awayKickInterval.Map(i => i.TotalMinutes) },
            { "hide_from_listing", hideFromListing },
            { "max_users", maxUsers }
        };

        return this.RestHttpClient.PatchAsync<IRestWorld>
        (
            $"worlds/{id}",
            r => r.With(m => m.Content = new FormUrlEncodedContent(parameters)),
            ct: ct
        );
    }

    /// <summary>
    /// Gets the users currently in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The users.</returns>
    public Task<Result<IReadOnlyList<IRestUser>>> GetUsersInWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IReadOnlyList<IRestUser>>($"worlds/{id}/users", ct: ct);

    /// <summary>
    /// Gets a specific user currently in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result<IRestUser>> GetUserInWorldAsync
    (
        string id,
        string userId,
        CancellationToken ct = default
    )
        => this.RestHttpClient.GetAsync<IRestUser>($"worlds/{id}/users/{userId}", ct: ct);

    /// <summary>
    /// Kicks a given user from the world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result> KickUserAsync
    (
        string id,
        string userId,
        CancellationToken ct = default
    )
        => this.RestHttpClient.PostAsync($"worlds/{id}/users/{userId}/kick", ct: ct);

    /// <summary>
    /// Bans a given user from the world.
    /// TODO: check if this bans the user from all sessions or just the world. Not entirely clear.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result> BanUserAsync
    (
        string id,
        string userId,
        CancellationToken ct = default
    )
        => this.RestHttpClient.PostAsync($"worlds/{id}/users/{userId}/ban", ct: ct);

    /// <summary>
    /// Silences or unsilences a given user in a given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="isSilenced">Whether the user should be silenced.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result> SilenceUnsilenceUserAsync
    (
        string id,
        string userId,
        bool isSilenced,
        CancellationToken ct = default
    )
        => this.RestHttpClient.PostAsync
        (
            $"worlds/{id}/users/{userId}/ban",
            r => r.With(m => m.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("silenced", isSilenced.ToString())
            })),
            ct: ct
        );

    /// <summary>
    /// Respawns a given user from the world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result> RespawnUserAsync
    (
        string id,
        string userId,
        CancellationToken ct = default
    )
        => this.RestHttpClient.PostAsync($"worlds/{id}/users/{userId}/respawn", ct: ct);

    /// <summary>
    /// Sets the rule of the given user in the given world.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="role">The new role.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result> SetWorldUserRoleAsync
    (
        string id,
        string userId,
        RestUserRole role,
        CancellationToken ct = default
    )
        => this.RestHttpClient.PutAsync
        (
            $"worlds/{id}/users/{userId}/role",
            r => r.With(m => m.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("role", ((int)role).ToString())
            })),
            ct: ct
        );

    /// <summary>
    /// Gets the currently focused world.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The focused world.</returns>
    public Task<Result<IRestWorld>> GetFocusedWorldAsync(CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IRestWorld>("worlds/focused", ct: ct);

    /// <summary>
    /// Sets the currently focused world.
    /// TODO: remove world identification by index; that's just silly.
    /// TODO: remove return value from rest route.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="name">The name of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The focused world.</returns>
    public Task<Result> SetFocusedWorldAsync
    (
        Optional<string> id = default,
        Optional<string> name = default,
        CancellationToken ct = default
    )
    {
        if (id.HasValue ^ name.HasValue)
        {
            throw new InvalidOperationException("Either id or name must be provided.");
        }

        var parameters = new List<KeyValuePair<string, string>>();
        if (id.HasValue)
        {
            parameters.Add(new("id", id.Value));
        }

        if (name.HasValue)
        {
            parameters.Add(new("name", name.Value));
        }

        return this.RestHttpClient.PutAsync
        (
            "worlds/focused",
            r => r.With(m => m.Content = new FormUrlEncodedContent(parameters)),
            ct: ct
        );
    }
}
