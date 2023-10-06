//
//  SPDX-FileName: ResoniteContactController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crystite.API.Abstractions;
using Crystite.API.Extensions;
using FrooxEngine;
using Remora.Results;
using SkyFrost.Base;

namespace Crystite.API;

/// <summary>
/// Implements contact control logic for the stock headless client.
/// </summary>
public class ResoniteContactController : IResoniteContactController
{
    private readonly Engine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteContactController"/> class.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    public ResoniteContactController(Engine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IRestContact>>> GetContactsAsync(CancellationToken ct = default)
    {
        var contacts = new List<Contact>();
        _engine.Cloud.Contacts.GetContacts(contacts);

        return Task.FromResult<Result<IReadOnlyList<IRestContact>>>(contacts.Select(f => f.ToRestContact()).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result<IRestContact>> ModifyContactAsync(string userIdOrName, RestContactStatus status, CancellationToken ct = default)
    {
        var contacts = new List<Contact>();
        _engine.Cloud.Contacts.GetContacts(contacts);

        var contact = contacts.FirstOrDefault(f => f.ContactUserId == userIdOrName);
        if (contact is null)
        {
            contact = contacts.FirstOrDefault(f => string.Equals(userIdOrName, f.ContactUsername, StringComparison.InvariantCultureIgnoreCase));
            if (contact is null)
            {
                return new NotFoundError();
            }
        }

        if (contact.ContactStatus == status.ToContactStatus())
        {
            return new InvalidOperationException($"The contact is already {status.ToString().ToLowerInvariant()}.");
        }

        switch (status)
        {
            case RestContactStatus.None:
            {
                // Remove friend
                await _engine.Cloud.Contacts.RemoveContact(contact);
                break;
            }
            case RestContactStatus.Ignored:
            {
                // ignore
                await _engine.Cloud.Contacts.IgnoreRequest(contact);
                break;
            }
            case RestContactStatus.Blocked:
            {
                return new NotSupportedError("Blocking is currently unimplemented.");
            }
            case RestContactStatus.Requested:
            case RestContactStatus.Friend:
            {
                // accept or request
                // TODO: ensure friend requests can actually be sent this way
                await _engine.Cloud.Contacts.AddContact(contact);
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        return contact.ToRestContact() with { Status = status };
    }
}
