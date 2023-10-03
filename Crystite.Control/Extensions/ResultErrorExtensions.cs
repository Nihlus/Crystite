//
//  SPDX-FileName: ResultErrorExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net.Http;
using System.Net.Sockets;
using Remora.Rest.Results;
using Remora.Results;

namespace Crystite.Control.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IResultError"/> interface.
/// </summary>
public static class ResultErrorExtensions
{
    /// <summary>
    /// Attempts to explain the error to the end user.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The explanation to display.</returns>
    public static string Explain(this IResultError? error) => error switch
    {
        RestResultError<APIError> ae => $"The server replied with an error: {ae.Error.Message}",
        HttpResultError ae => $"The server rejected the request: {ae.StatusCode}",
        NotFoundError ne => ne.Message,
        ExceptionError { Exception: HttpRequestException { InnerException: SocketException se } } => $"Connection failed ({se.Message}). Check port or server address?",
        ExceptionError ex => $"Unexpected fatal error: {ex.Exception}",
        null => "No error occurred",
        _ => $"Unknown error: {error.Message}"
    };
}
