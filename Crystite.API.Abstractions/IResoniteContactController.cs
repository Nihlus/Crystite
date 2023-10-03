//
//  SPDX-FileName: IResoniteContactController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Results;

namespace Crystite.API.Abstractions;

/// <summary>
/// Represents the public API of friend-related functionality of a headless Resonite client.
/// </summary>
[PublicAPI]
public interface IResoniteContactController
{
    /// <summary>
    /// Gets the contacts of the current account.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IReadOnlyList<IRestContact>>> GetContactsAsync(CancellationToken ct = default);

    /// <summary>
    /// Modifies the given contact.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the contact to modify.</param>
    /// <param name="status">The new contact status.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestContact>> ModifyContactAsync
    (
        string userIdOrName,
        RestContactStatus status,
        CancellationToken ct = default
    );
}
