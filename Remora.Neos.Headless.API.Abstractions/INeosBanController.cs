//
//  SPDX-FileName: INeosBanController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Results;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents the public API of ban-related functionality of a headless NeosVR client.
/// </summary>
[PublicAPI]
public interface INeosBanController
{
    /// <summary>
    /// Gets active bans.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IReadOnlyList<IRestBan>> GetBansAsync(CancellationToken ct = default);

    /// <summary>
    /// Bans the given user from all sessions.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the user to ban.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<IRestBan>> BanUserAsync(string userIdOrName, CancellationToken ct = default);

    /// <summary>
    /// Unbans the given user from all sessions.
    /// </summary>
    /// <param name="userIdOrName">The ID or username of the user to ban.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> UnbanUserAsync(string userIdOrName, CancellationToken ct = default);
}
