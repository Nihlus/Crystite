//
//  SPDX-FileName: ApplicationResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.API.Services;

namespace Remora.Neos.Headless.API.Mod;

/// <summary>
/// Defines API routes for application control.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class ApplicationResource
{
    private readonly IJobService _jobService;
    private readonly INeosApplicationController _applicationController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationResource"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    /// <param name="applicationController">The application controller.</param>
    public ApplicationResource(JobService jobService, INeosApplicationController applicationController)
    {
        _jobService = jobService;
        _applicationController = applicationController;
    }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/application/shutdown")]
    public async Task ShutdownAsync(IHttpContext context)
    {
        if (_applicationController.HasShutdownBeenRequested)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        var job = _jobService.CreateJob
        (
            "shutdown",
            _ => _applicationController.ShutdownAsync()
        );

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }
}
