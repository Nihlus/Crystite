//
//  SPDX-FileName: UserController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using Crystite.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Crystite.Routes;

/// <summary>
/// Handles API requests for user information.
/// </summary>
[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IResoniteUserController _resoniteUserController;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="resoniteUserController">The Resonite user controller.</param>
    public UserController(IResoniteUserController resoniteUserController)
    {
        _resoniteUserController = resoniteUserController;
    }

    /// <summary>
    /// Gets information about a user.
    /// </summary>
    /// <param name="userIdOrName">The ID or name of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost]
    [Route("{userIdOrName}")]
    public async Task<ActionResult<IRestUser>> GetUserAsync(string userIdOrName, CancellationToken ct = default)
    {
        return (await _resoniteUserController.GetUserAsync(userIdOrName, ct)).ToActionResult();
    }
}
