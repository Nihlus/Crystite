//
//  SPDX-FileName: BanController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using Crystite.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Crystite.Routes;

/// <summary>
/// Handles API requests for ban information.
/// </summary>
[ApiController]
[Route("bans")]
public class BanController : ControllerBase
{
    private readonly IResoniteBanController _resoniteBanController;

    /// <summary>
    /// Initializes a new instance of the <see cref="BanController"/> class.
    /// </summary>
    /// <param name="resoniteBanController">The Resonite ban controller.</param>
    public BanController(IResoniteBanController resoniteBanController)
    {
        _resoniteBanController = resoniteBanController;
    }

    /// <summary>
    /// Gets the active bans.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IEnumerable<IRestBan>>> GetBansAsync(CancellationToken ct = default)
    {
        var bans = await _resoniteBanController.GetBansAsync(ct);
        return new(bans);
    }

    /// <summary>
    /// Bans the identified user from all sessions.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the user to ban.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost]
    [Route("{userIdOrName}")]
    public async Task<ActionResult<IRestBan>> BanUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        return (await _resoniteBanController.BanUserAsync(userIdOrName, ct)).ToActionResult();
    }

    /// <summary>
    /// Unbans the identified user from all sessions.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the user to ban.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpDelete]
    [Route("{userIdOrName}")]
    public async Task<ActionResult> UnbanUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        return (await _resoniteBanController.UnbanUserAsync(userIdOrName, ct)).ToActionResult();
    }
}
