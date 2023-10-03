//
//  SPDX-FileName: ApplicationController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using Crystite.API.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Crystite.Routes;

/// <summary>
/// Defines API rotes for application control.
/// </summary>
[ApiController]
[Route("application")]
public class ApplicationController : ControllerBase
{
    private readonly INeosApplicationController _applicationController;
    private readonly IJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationController"/> class.
    /// </summary>
    /// <param name="applicationController">The Neos application controller.</param>
    /// <param name="jobService">The job service.</param>
    public ApplicationController(INeosApplicationController applicationController, IJobService jobService)
    {
        _applicationController = applicationController;
        _jobService = jobService;
    }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost]
    [Route("shutdown")]
    public ActionResult<IJob> ShutdownAsync(CancellationToken ct = default)
    {
        if (_applicationController.HasShutdownBeenRequested)
        {
            return Forbid();
        }

        return _jobService.CreateJob
        (
            "shutdown",
            _ => _applicationController.ShutdownAsync()
        );
    }
}
