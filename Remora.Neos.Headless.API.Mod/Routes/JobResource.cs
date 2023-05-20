//
//  SPDX-FileName: JobResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Abstractions.Services;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines API routes for long-running jobs.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class JobResource
{
    private readonly IJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobResource"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    public JobResource(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// Get a job identified by its ID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("GET", "/jobs/{id}")]
    public async Task GetJobAsync(IHttpContext context)
    {
        if (!Guid.TryParse(context.Request.PathParameters["id"], out var jobId))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!_jobService.TryGetJob(jobId, out var job))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }

    /// <summary>
    /// Cancels a job identified by its ID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("DELETE", "/jobs/{id}")]
    public async Task CancelJobAsync(IHttpContext context)
    {
        if (!Guid.TryParse(context.Request.PathParameters["id"], out var jobId))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.BadRequest);
            return;
        }

        if (!_jobService.TryGetJob(jobId, out var job))
        {
            await context.Response.SendResponseAsync(HttpStatusCode.NotFound);
            return;
        }

        job.TokenSource.Cancel();

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }
}
