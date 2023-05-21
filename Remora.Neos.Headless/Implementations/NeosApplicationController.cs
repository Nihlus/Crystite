//
//  SPDX-FileName: NeosApplicationController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Remora.Neos.Headless.API.Abstractions;

namespace Remora.Neos.Headless.Implementations;

/// <summary>
/// Implements application-level control logic for the headless client.
/// </summary>
public class NeosApplicationController : INeosApplicationController
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosApplicationController"/> class.
    /// </summary>
    /// <param name="applicationLifetime">The application lifetime controller.</param>
    public NeosApplicationController(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    /// <inheritdoc />
    public bool HasShutdownBeenRequested => _applicationLifetime.ApplicationStopping.IsCancellationRequested;

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        if (this.HasShutdownBeenRequested)
        {
            return Task.CompletedTask;
        }

        _applicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}
