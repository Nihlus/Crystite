//
//  SPDX-FileName: JobService.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Abstractions.Services;

namespace Remora.Neos.Headless.API.Services;

/// <summary>
/// Acts as a container and manager for long-running jobs.
/// </summary>
[PublicAPI]
public sealed class JobService : IJobService
{
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    /// <summary>
    /// Creates a job with the given description and associated action.
    /// </summary>
    /// <param name="description">The job description.</param>
    /// <param name="action">The action.</param>
    /// <returns>The job.</returns>
    public Job CreateJob(string description, Func<CancellationToken, Task> action)
    {
        var tokenSource = new CancellationTokenSource();

        async Task YieldingWrapper()
        {
            await Task.Yield();
            await action(tokenSource.Token);
        }

        var job = new Job
        (
            Guid.NewGuid(),
            description,
            YieldingWrapper(),
            tokenSource
        );

        // Guard against (highly unlikely) duplicate keys
        while (!_jobs.TryAdd(job.Id, job))
        {
            job = job with { Id = Guid.NewGuid() };
        }

        return job;
    }

    /// <summary>
    /// Attempts to retrieve the job with the given ID.
    /// </summary>
    /// <remarks>
    /// If the job is completed, it will also be removed from the service's internal container.
    /// </remarks>
    /// <param name="id">The ID of the job.</param>
    /// <param name="job">The job.</param>
    /// <returns>true if a matching job was found; otherwise, false.</returns>
    public bool TryGetJob(Guid id, [NotNullWhen(true)] out Job? job)
    {
        if (!_jobs.TryGetValue(id, out job))
        {
            return false;
        }

        if (job.Action.IsCompleted)
        {
            // Remove the job of it's completed.
            _ = _jobs.TryRemove(id, out _);
        }

        return true;
    }
}
