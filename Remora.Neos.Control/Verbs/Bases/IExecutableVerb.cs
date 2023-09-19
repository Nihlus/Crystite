//
//  SPDX-FileName: IExecutableVerb.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Results;

namespace Remora.Neos.Control.Verbs.Bases;

/// <summary>
/// Represents an executable verb.
/// </summary>
public interface IExecutableVerb
{
    /// <summary>
    /// Executes the verb.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A task representing the potentially asynchronous operation.</returns>
    ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default);
}
