//
//  SPDX-FileName: JobController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Crystite.Routes;

/// <summary>
/// Defines API routes for job management.
/// </summary>
[ApiController]
[Route("jobs")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobController"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    public JobController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// Gets all running jobs.
    /// </summary>
    /// <returns>The jobs.</returns>
    [HttpGet]
    public ActionResult<IEnumerable<IJob>> GetJobsAsync() => new(_jobService.GetJobs());

    /// <summary>
    /// Gets the specified job.
    /// </summary>
    /// <param name="id">The ID of the job.</param>
    /// <param name="peek">Whether to just peek at the job instead of removing it.</param>
    /// <returns>The job.</returns>
    [HttpGet]
    [Route("{id}")]
    public ActionResult<IJob> GetJob(Guid id, [FromQuery] bool peek = false)
    {
        if (peek)
        {
            return _jobService.TryPeekJob(id, out var peekedJob)
                ? new ActionResult<IJob>(peekedJob)
                : NotFound();
        }

        return _jobService.TryGetJob(id, out var job)
            ? new ActionResult<IJob>(job)
            : NotFound();
    }

    /// <summary>
    /// Cancels the specified job.
    /// </summary>
    /// <remarks>
    /// Cancellation of a job is not a guarantee that the operation it is performing will be canceled or rolled back.
    /// </remarks>
    /// <param name="id">The ID of the job.</param>
    /// <returns>An action result.</returns>
    [HttpDelete]
    [Route("{id}")]
    public ActionResult CancelJobAsync(Guid id)
    {
        if (!_jobService.TryGetJob(id, out var job))
        {
            return NotFound();
        }

        job.TokenSource.Cancel();
        return NoContent();
    }
}
