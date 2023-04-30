//
//  SPDX-FileName: ApplicationResource.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Grapevine;
using JetBrains.Annotations;
using NeosHeadless;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Defines API routes for application control.
/// </summary>
[RestResource]
[PublicAPI]
internal sealed class ApplicationResource
{
    private static bool _hasShutdownBeenRequested;

    private readonly JobService _jobService;
    private readonly Func<Task> _shutdownProcedure;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationResource"/> class.
    /// </summary>
    /// <param name="jobService">The job service.</param>
    public ApplicationResource(JobService jobService)
    {
        _jobService = jobService;
        _shutdownProcedure = (Func<Task>)(Assembly.GetAssembly(typeof(WorldHandler))
            .GetType("NeosHeadless.Program")?
            .GetMethod("Shutdown", BindingFlags.Static | BindingFlags.NonPublic)?
            .CreateDelegate(typeof(Func<Task>)) ?? throw new MissingMethodException());
    }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RestRoute("POST", "/application/shutdown")]
    public async Task ShutdownAsync(IHttpContext context)
    {
        if (_hasShutdownBeenRequested)
        {
            await context.Response.SendResponseAsync(HttpStatusCode.Forbidden);
            return;
        }

        var job = _jobService.CreateJob
        (
            "shutdown",
            _ => _shutdownProcedure()
        );

        _hasShutdownBeenRequested = true;

        var json = JsonSerializer.Serialize(job);
        await context.Response.SendResponseAsync(json);
    }
}
