//
//  SPDX-FileName: ResultExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Remora.Results;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Result"/> struct and its overloads.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a Remora result to an ASP.NET action result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <typeparam name="T">The contained entity type.</typeparam>
    /// <returns>The mapped result.</returns>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? result.Entity
            : result.Error.ToActionResult();
    }

    /// <summary>
    /// Maps a Remora result to an ASP.NET action result.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The mapped result.</returns>
    public static ActionResult ToActionResult(this Result result)
    {
        return result.IsSuccess
            ? new OkResult()
            : result.Error.ToActionResult();
    }

    /// <summary>
    /// Converts a Remora error to an ASP.NET action result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The action result.</returns>
    private static ActionResult ToActionResult(this IResultError error) => error switch
    {
        ArgumentInvalidError => new BadRequestObjectResult(error),
        ArgumentNullError => new BadRequestObjectResult(error),
        ArgumentOutOfRangeError => new BadRequestObjectResult(error),
        ArgumentError => new BadRequestObjectResult(error),
        ExceptionError => new BadRequestObjectResult(error),
        InvalidOperationError => new BadRequestObjectResult(error),
        NotFoundError => new NotFoundObjectResult(error),
        NotSupportedError => new StatusCodeResult((int)HttpStatusCode.NotImplemented),
        _ => new StatusCodeResult((int)HttpStatusCode.InternalServerError)
    };
}
