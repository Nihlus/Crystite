//
//  SPDX-FileName: NeosApplicationController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Reflection;
using System.Threading.Tasks;
using NeosHeadless;
using Remora.Neos.Headless.API.Abstractions;

namespace Remora.Neos.Headless.API.Mod.Implementations;

/// <summary>
/// Implements application control logic for the stock headless client.
/// </summary>
internal class NeosApplicationController : INeosApplicationController
{
    private readonly Func<Task> _shutdownProcedure;

    /// <inheritdoc/>
    public bool HasShutdownBeenRequested { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosApplicationController"/> class.
    /// </summary>
    public NeosApplicationController()
    {
        _shutdownProcedure = (Func<Task>)(Assembly.GetAssembly(typeof(WorldHandler))
            .GetType("NeosHeadless.Program")?
            .GetMethod("Shutdown", BindingFlags.Static | BindingFlags.NonPublic)?
            .CreateDelegate(typeof(Func<Task>)) ?? throw new MissingMethodException());
    }

    /// <inheritdoc/>
    public Task ShutdownAsync()
    {
        if (this.HasShutdownBeenRequested)
        {
            return Task.CompletedTask;
        }

        this.HasShutdownBeenRequested = true;
        return _shutdownProcedure();
    }
}
