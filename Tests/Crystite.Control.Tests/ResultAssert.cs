//
//  SPDX-FileName: ResultAssert.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Remora.Results;
using Xunit;

namespace Crystite.Control.Tests;

/// <summary>
/// Contains helper assertions for results.
/// </summary>
public static class ResultAssert
{
    /// <summary>
    /// Asserts that the given result is successful.
    /// </summary>
    /// <typeparam name="TResult">The result type to inspect.</typeparam>
    /// <param name="result">The result.</param>
    public static void Successful<TResult>(TResult result) where TResult : struct, IResult
    {
        Assert.True
        (
            result.IsSuccess,
            result.IsSuccess ? string.Empty : result.Error?.Message ?? "Unknown error."
        );
    }

    /// <summary>
    /// Asserts that a given result is unsuccessful.
    /// </summary>
    /// <typeparam name="TResult">The result type to inspect.</typeparam>
    /// <param name="result">The result.</param>
    public static void Unsuccessful<TResult>(TResult result) where TResult : struct, IResult
    {
        Assert.False(result.IsSuccess, "The result was successful.");
    }
}
