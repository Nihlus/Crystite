//
//  SPDX-FileName: ContactController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Microsoft.AspNetCore.Mvc;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.Extensions;

namespace Remora.Neos.Headless.Routes;

/// <summary>
/// Defines API routes for contact lists.
/// </summary>
[ApiController]
[Route("contacts")]
public class ContactController : ControllerBase
{
    private readonly INeosContactController _contactController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactController"/> class.
    /// </summary>
    /// <param name="contactController">The contact controller.</param>
    public ContactController(INeosContactController contactController)
    {
        _contactController = contactController;
    }

    /// <summary>
    /// Gets the contacts for the current account.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IRestContact>>> GetContactsAsync(CancellationToken ct = default)
    {
        return (await _contactController.GetContactsAsync(ct)).ToActionResult();
    }

    /// <summary>
    /// Modifies the given contact.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the contact.</param>
    /// <param name="status">The new status of the contact.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPatch]
    [Route("{userIdOrName}")]
    public async Task<ActionResult<IRestContact>> ModifyContactAsync
    (
        string userIdOrName,
        RestContactStatus status,
        CancellationToken ct = default
    )
    {
        return (await _contactController.ModifyContactAsync(userIdOrName, status, ct)).ToActionResult();
    }
}
