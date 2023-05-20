//
//  SPDX-FileName: FriendResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Mod.Extensions;
using Remora.Results;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines API routes for handling friend requests.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class FriendResource
{
    private readonly INeosContactController _contactController;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendResource"/> class.
    /// </summary>
    /// <param name="contactController">The contact controller.</param>
    public FriendResource(INeosContactController contactController)
    {
        _contactController = contactController;
    }

    /// <summary>
    /// Gets all friends for the current account.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/friends")]
    public async Task GetFriendsAsync(IHttpContext context)
    {
        var getContacts = await _contactController.GetContactsAsync(context.CancellationToken);
        if (!getContacts.IsDefined(out var contacts))
        {
            await context.Response.SendResponseAsync(getContacts.Error.ToStatusCode());
            return;
        }

        var friends = contacts.Select(c => c.Status is RestContactStatus.Friend);

        var json = JsonSerializer.Serialize(friends);
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
        var userIdOrName = context.Request.PathParameters["id"];

        var data = await context.Request.ParseFormUrlEncodedData();
        if (!data.TryGetValue("status", out var rawStatus) || !Enum.TryParse<RestContactStatus>(rawStatus, out var contactStatus))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        var modifyContact = await _contactController.ModifyContactAsync(userIdOrName, contactStatus);
        if (!modifyContact.IsDefined(out var contact))
        {
            await context.Response.SendResponseAsync(modifyContact.Error.ToStatusCode());
            return;
        }

        var json = JsonSerializer.Serialize(contact);
        await context.Response.SendResponseAsync(json);
    }
}
