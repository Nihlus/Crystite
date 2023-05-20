//
//  SPDX-FileName: IJobService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Remora.Neos.Headless.API.Abstractions.Services;

/// <summary>
/// Represents the public API of a job management service.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Creates a job with the given description and associated action.
    /// </summary>
    /// <param name="description">The job description.</param>
    /// <param name="action">The action.</param>
    /// <returns>The job.</returns>
    Job CreateJob(string description, Func<CancellationToken, Task> action);

    /// <summary>
    /// Attempts to retrieve the job with the given ID.
    /// </summary>
    /// <remarks>
    /// If the job is completed, it will also be removed from the service's internal container.
    /// </remarks>
    /// <param name="id">The ID of the job.</param>
    /// <param name="job">The job.</param>
    /// <returns>true if a matching job was found; otherwise, false.</returns>
    bool TryGetJob(Guid id, [NotNullWhen(true)] out Job? job);
}
