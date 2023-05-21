//
//  SPDX-FileName: BanController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Microsoft.AspNetCore.Mvc;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.Extensions;

namespace Remora.Neos.Headless.Routes;

/// <summary>
/// Handles API requests for ban information.
/// </summary>
[ApiController]
[Route("bans")]
public class BanController : ControllerBase
{
    private readonly INeosBanController _neosBanController;

    /// <summary>
    /// Initializes a new instance of the <see cref="BanController"/> class.
    /// </summary>
    /// <param name="neosBanController">The Neos ban controller.</param>
    public BanController(INeosBanController neosBanController)
    {
        _neosBanController = neosBanController;
    }

    /// <summary>
    /// Gets the active bans.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IEnumerable<RestBan>>> GetBansAsync(CancellationToken ct = default)
    {
        var bans = await _neosBanController.GetBansAsync(ct);
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
    public async Task<ActionResult<RestBan>> BanUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        return (await _neosBanController.BanUserAsync(userIdOrName, ct)).ToActionResult();
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
        return (await _neosBanController.UnbanUserAsync(userIdOrName, ct)).ToActionResult();
    }
}
