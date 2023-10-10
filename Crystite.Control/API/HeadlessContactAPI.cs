//
//  SPDX-FileName: HeadlessContactAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using Crystite.API.Abstractions;
using Remora.Rest;
using Remora.Results;

namespace Crystite.Control.API;

/// <summary>
/// Defines API endpoints for the headless contact API.
/// </summary>
public class HeadlessContactAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessContactAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessContactAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Gets all known contacts, regardless of status.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The contacts.</returns>
    public Task<Result<IReadOnlyList<IRestContact>>> GetContactsAsync(CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IReadOnlyList<IRestContact>>("contacts", ct: ct);

    /// <summary>
    /// Gets a specific contact.
    /// </summary>
    /// <param name="id">The ID of the contact.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The contacts.</returns>
    public Task<Result<IRestContact>> GetContactAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IRestContact>($"contacts/{id}", ct: ct);

    /// <summary>
    /// Modifies the status of the given contact.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="status">The new contact status.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    public Task<Result> ModifyContactAsync(string id, RestContactStatus status, CancellationToken ct = default)
    {
        var parameters = new List<KeyValuePair<string, string>>()
        {
            new("status", ((int)status).ToString())
        };

        return this.RestHttpClient.PatchAsync
        (
            $"contacts/{id}",
            r => r.With(m => m.Content = new FormUrlEncodedContent(parameters)),
            ct: ct
        );
    }
}
