//
//  SPDX-FileName: ResoniteUserController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Threading;
using System.Threading.Tasks;
using Crystite.API.Abstractions;
using Crystite.API.Extensions;
using FrooxEngine;
using Remora.Results;

namespace Crystite.API;

/// <summary>
/// Implements user control logic for the stock headless client.
/// </summary>
public class ResoniteUserController : IResoniteUserController
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteUserController"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public ResoniteUserController(Engine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public async Task<Result<IRestUser>> GetUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        var getUser = await _engine.Cloud.Users.GetUser(userIdOrName);
        if (getUser.IsOK)
        {
            return getUser.Entity.ToRestUser();
        }

        getUser = await _engine.Cloud.Users.GetUserByName(userIdOrName);
        if (!getUser.IsOK)
        {
            return new NotFoundError("No user with that ID or name could be found.");
        }

        return getUser.Entity.ToRestUser();
    }
}
