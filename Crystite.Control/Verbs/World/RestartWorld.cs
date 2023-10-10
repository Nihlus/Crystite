//
//  SPDX-FileName: RestartWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Restarts a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("restart-world", HelpText = "Restarts a specific running world")]
public sealed class RestartWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestartWorld"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public RestartWorld(string? worldName, string? worldID, ushort port, string server, OutputFormat outputFormat)
        : base(worldName, worldID, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var jobAPI = services.GetRequiredService<HeadlessJobAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

        var getWorld = await GetTargetWorldIDAsync(worldAPI, ct);
        if (!getWorld.IsDefined(out var world))
        {
            return (Result)getWorld;
        }

        var restartWorld = await worldAPI.RestartWorldAsync(world, ct);
        if (!restartWorld.IsDefined(out var job))
        {
            return (Result)restartWorld;
        }

        var waitForRestart = await jobAPI.WaitForJobAsync(job, ct: ct);
        if (!waitForRestart.IsSuccess)
        {
            return (Result)waitForRestart;
        }

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("World restarted");
        }

        return Result.FromSuccess();
    }
}
