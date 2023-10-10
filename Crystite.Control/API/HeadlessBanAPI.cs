//
//  SPDX-FileName: HeadlessBanAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using Crystite.API.Abstractions;
using Remora.Rest;
using Remora.Results;

namespace Crystite.Control.API;

/// <summary>
/// Defines API endpoints for the headless ban API.
/// </summary>
public class HeadlessBanAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessBanAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessBanAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Gets the active bans.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The bans.</returns>
    public Task<Result<IReadOnlyList<IRestBan>>> GetBansAsync(CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IReadOnlyList<IRestBan>>("bans", ct: ct);

    /// <summary>
    /// Bans the given user.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The ban information.</returns>
    public Task<Result<IRestBan>> BanUserAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.PostAsync<IRestBan>($"bans/{id}", ct: ct);

    /// <summary>
    /// Unbans the given user.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    public Task<Result> UnbanUserAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.DeleteAsync($"bans/{id}", ct: ct);
}
