//
//  SPDX-FileName: IResoniteUserController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Threading;
using System.Threading.Tasks;
using Remora.Results;

namespace Crystite.API.Abstractions;

/// <summary>
/// Represents the public API of user-related functionality of a headless Resonite client.
/// </summary>
public interface IResoniteUserController
{
    /// <summary>
    /// Gets a user by their ID or name.
    /// </summary>
    /// <param name="userIdOrName">The ID or name of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    Task<Result<IRestUser>> GetUserAsync(string userIdOrName, CancellationToken ct = default);
}
