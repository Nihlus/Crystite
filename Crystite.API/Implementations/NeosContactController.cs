//
//  SPDX-FileName: NeosContactController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudX.Shared;
using Crystite.API.Abstractions;
using Crystite.API.Extensions;
using FrooxEngine;
using Remora.Results;

namespace Crystite.API;

/// <summary>
/// Implements contact control logic for the stock headless client.
/// </summary>
public class NeosContactController : INeosContactController
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosContactController"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public NeosContactController(Engine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IRestContact>>> GetContactsAsync(CancellationToken ct = default)
    {
        var friends = new List<Friend>();
        _engine.Cloud.Friends.GetFriends(friends);

        return Task.FromResult<Result<IReadOnlyList<IRestContact>>>(friends.Select(f => f.ToRestContact()).ToArray());
    }

    /// <inheritdoc />
    public Task<Result<IRestContact>> ModifyContactAsync(string userIdOrName, RestContactStatus status, CancellationToken ct = default)
    {
        var friends = new List<Friend>();
        _engine.Cloud.Friends.GetFriends(friends);

        var friendRequest = friends.FirstOrDefault(f => f.FriendUserId == userIdOrName);
        if (friendRequest is null)
        {
            friendRequest = friends.FirstOrDefault(f => string.Equals(userIdOrName, f.FriendUsername, StringComparison.InvariantCultureIgnoreCase));
            if (friendRequest is null)
            {
                return Task.FromResult<Result<IRestContact>>(new NotFoundError());
            }
        }

        if (friendRequest.FriendStatus == status.ToFriendStatus())
        {
            return Task.FromResult<Result<IRestContact>>
            (
                new InvalidOperationException($"The contact is already {status.ToString().ToLowerInvariant()}.")
            );
        }

        switch (status)
        {
            case RestContactStatus.None:
            {
                // Remove friend
                _engine.Cloud.Friends.RemoveFriend(friendRequest);
                break;
            }
            case RestContactStatus.Ignored:
            {
                // ignore
                _engine.Cloud.Friends.IgnoreRequest(friendRequest);
                break;
            }
            case RestContactStatus.Blocked:
            {
                return Task.FromResult<Result<IRestContact>>(new NotSupportedError("Blocking is currently unimplemented."));
            }
            case RestContactStatus.Requested:
            case RestContactStatus.Friend:
            {
                // accept or request
                // TODO: ensure friend requests can actually be sent this way
                _engine.Cloud.Friends.AddFriend(friendRequest);
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        return Task.FromResult<Result<IRestContact>>(friendRequest.ToRestContact() with { Status = status });
    }
}
