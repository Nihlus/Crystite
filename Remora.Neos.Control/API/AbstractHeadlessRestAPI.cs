//
//  SPDX-FileName: AbstractHeadlessRestAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using JetBrains.Annotations;
using Remora.Rest;

namespace Remora.Neos.Control.API;

/// <summary>
/// Acts as an abstract base for REST API instances.
/// </summary>
[PublicAPI]
public abstract class AbstractHeadlessRestAPI
{
    /// <summary>
    /// Gets the <see cref="RestHttpClient{TError}"/> available to the API instance.
    /// </summary>
    protected IRestHttpClient RestHttpClient { get; }

    /// <summary>
    /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> available to the API instance.
    /// </summary>
    protected JsonSerializerOptions JsonOptions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractHeadlessRestAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The Discord-specialized Http client.</param>
    /// <param name="jsonOptions">The Remora-specialized JSON options.</param>
    protected AbstractHeadlessRestAPI
    (
        IRestHttpClient restHttpClient,
        JsonSerializerOptions jsonOptions
    )
    {
        this.RestHttpClient = restHttpClient;
        this.JsonOptions = jsonOptions;
    }
}
