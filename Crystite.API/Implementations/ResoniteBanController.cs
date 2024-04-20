//
//  SPDX-FileName: ResoniteBanController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crystite.API.Abstractions;
using FrooxEngine;
using Remora.Results;

namespace Crystite.API;

/// <summary>
/// Implements ban control logic for the stock headless client.
/// </summary>
public class ResoniteBanController : IResoniteBanController
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteBanController"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public ResoniteBanController(Engine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IRestBan>> GetBansAsync(CancellationToken ct = default)
    {
        var bans = new List<IRestBan>();
        foreach (var entry in BanManager.CurrentRestrictions.Entries)
        {
            bans.Add(new RestBan(entry.UserId, entry.Username, entry.MachineIDs));
        }

        return Task.FromResult<IReadOnlyList<IRestBan>>(bans);
    }

    /// <inheritdoc />
    public async Task<Result<IRestBan>> BanUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        var getUser = await _engine.Cloud.Users.GetUser(userIdOrName);
        if (!getUser.IsOK)
        {
            getUser = await _engine.Cloud.Users.GetUserByName(userIdOrName);
            if (!getUser.IsOK)
            {
                return new NotFoundError("No user with that ID or name could be found.");
            }
        }

        var user = getUser.Entity;
        var fingerprint = new UserFingerprint(user);

        if (BanManager.IsBanned(fingerprint))
        {
            return new InvalidOperationError("The user is already banned.");
        }

        BanManager.Ban(fingerprint);
        return new RestBan(user.Id, user.Username);
    }

    /// <inheritdoc />
    public async Task<Result> UnbanUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        var getUser = await _engine.Cloud.Users.GetUser(userIdOrName);
        if (!getUser.IsOK)
        {
            getUser = await _engine.Cloud.Users.GetUserByName(userIdOrName);
            if (!getUser.IsOK)
            {
                return new NotFoundError();
            }
        }

        var user = getUser.Entity;
        var fingerprint = new UserFingerprint(user);

        if (!BanManager.IsBanned(fingerprint))
        {
            return new InvalidOperationError("The user is not banned.");
        }

        BanManager.RemoveBan(fingerprint);
        return Result.FromSuccess();
    }
}
