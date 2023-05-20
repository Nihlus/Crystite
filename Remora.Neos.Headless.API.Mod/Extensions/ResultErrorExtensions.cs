//
//  SPDX-FileName: ResultErrorExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Grapevine;
using Remora.Results;

namespace Remora.Neos.Headless.API.Mod.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IResultError"/> interface.
/// </summary>
public static class ResultErrorExtensions
{
    /// <summary>
    /// Maps well-known errors to well-known HTTP status codes.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The status code.</returns>
    public static HttpStatusCode ToStatusCode(this IResultError? error) => error switch
    {
        ArgumentInvalidError => HttpStatusCode.BadRequest,
        ArgumentNullError => HttpStatusCode.BadRequest,
        ArgumentOutOfRangeError => HttpStatusCode.BadRequest,
        ArgumentError => HttpStatusCode.BadRequest,
        ExceptionError => HttpStatusCode.InternalServerError,
        InvalidOperationError => HttpStatusCode.BadRequest,
        NotFoundError => HttpStatusCode.NotFound,
        NotSupportedError => HttpStatusCode.NotImplemented,
        _ => HttpStatusCode.InternalServerError
    };
}
