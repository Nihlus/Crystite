//
//  SPDX-FileName: HeadlessWorldAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Rest;
using Remora.Results;

namespace Remora.Neos.Control.API;

/// <summary>
/// Defines API endpoints for the headless world API.
/// </summary>
public class HeadlessWorldAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessWorldAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessWorldAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Gets the currently running worlds.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The worlds.</returns>
    public Task<Result<IReadOnlyList<IRestWorld>>> GetWorldsAsync(CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IReadOnlyList<IRestWorld>>("worlds", ct: ct);

    /// <summary>
    /// Gets the currently running world identified by the given ID.
    /// </summary>
    /// <param name="id">The ID of the world.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The world.</returns>
    public Task<Result<IRestWorld>> GetWorldAsync(string id, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IRestWorld>($"world/{id}", ct: ct);

    /// <summary>
    /// Starts a world using the given start argument.
    /// </summary>
    /// <param name="startArgument">The startup argument.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous job description.</returns>
    public Task<Result<IJob>> StartWorldAsync(OneOf<string, Uri> startArgument, CancellationToken ct = default)
    {
        var form = new FormUrlEncodedContent
        (
            new[]
            {
                startArgument.Match
                (
                    template => new KeyValuePair<string, string>("template", template),
                    uri => new KeyValuePair<string, string>("uri", uri.ToString())
                )
            }
        );

        return this.RestHttpClient.PostAsync<IJob>
        (
            "worlds",
            r => r.With(m => m.Content = form),
            ct: ct
        );
    }
}
