//
//  SPDX-FileName: HeadlessUserAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using Crystite.API.Abstractions;
using Remora.Rest;
using Remora.Results;

namespace Crystite.Control.API;

/// <summary>
/// Defines API endpoints for the headless user API.
/// </summary>
public class HeadlessUserAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessUserAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessUserAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Gets information about a user.
    /// </summary>
    /// <param name="userIdOrName">The ID or name of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The user.</returns>
    public Task<Result<IRestUser>> GetUserAsync(string userIdOrName, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IRestUser>($"users/{userIdOrName}", ct: ct);
}
