//
//  SPDX-FileName: FriendResource.cs
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
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Extensions;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for handling friend requests.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class FriendResource
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendResource"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public FriendResource(Engine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Gets all friends for the current account.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/friends")]
    public async Task GetFriendsAsync(IHttpContext context)
    {
        var friends = new List<Friend>();
        _engine.Cloud.Friends.GetFriends(friends);

        var json = JsonSerializer.Serialize
        (
            friends
                .Where(f => f.FriendStatus is FriendStatus.Accepted)
                .Select(f => f.ToRestFriend()).ToArray()
        );

        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Modifies the status of the given friend request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("PATCH", "/friends/{id}")]
    public async Task ModifyFriendStatusAsync(IHttpContext context)
    {
        var friends = new List<Friend>();
        _engine.Cloud.Friends.GetFriends(friends);

        var idOrUsername = context.Request.PathParameters["id"];

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("status", out var rawStatus) || !Enum.TryParse<FriendStatus>(rawStatus, out var friendStatus))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var friendRequest = friends.FirstOrDefault(f => f.FriendUserId == idOrUsername);
        if (friendRequest is null)
        {
            friendRequest = friends.FirstOrDefault(f => string.Equals(idOrUsername, f.FriendUsername, StringComparison.InvariantCultureIgnoreCase));
            if (friendRequest is null)
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
                return;
            }
        }

        if (friendRequest.FriendStatus == friendStatus)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
            return;
        }

        switch (friendStatus)
        {
            case FriendStatus.None:
            {
                // Remove friend
                _engine.Cloud.Friends.RemoveFriend(friendRequest);
                break;
            }
            case FriendStatus.Ignored:
            {
                // ignore
                _engine.Cloud.Friends.IgnoreRequest(friendRequest);
                break;
            }
            case FriendStatus.Blocked:
            {
                await context.Response.SendResponseAsync(HttpStatusCode.NotImplemented);
                break;
            }
            case FriendStatus.Accepted:
            {
                // accept
                _engine.Cloud.Friends.AddFriend(friendRequest);
                break;
            }
            case FriendStatus.Requested:
            case FriendStatus.SearchResult:
            default:
            {
                await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
                return;
            }
        }

        await context.Response.SendResponseAsync(HttpStatusCode.NoContent);
    }
}
