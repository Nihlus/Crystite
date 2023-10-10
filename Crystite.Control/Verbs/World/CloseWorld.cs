//
//  SPDX-FileName: CloseWorld.cs
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
/// Closes a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("close-world", HelpText = "Closes a specific running world")]
public sealed class CloseWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloseWorld"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public CloseWorld(string? name, string? worldID, ushort port, string server, OutputFormat outputFormat)
        : base(name, worldID, port, server, outputFormat)
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

        var closeWorld = await worldAPI.CloseWorldAsync(world, ct);
        if (!closeWorld.IsDefined(out var job))
        {
            return (Result)closeWorld;
        }

        var waitForClose = await jobAPI.WaitForJobAsync(job, ct: ct);
        if (!waitForClose.IsSuccess)
        {
            return (Result)waitForClose;
        }

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("World closed");
        }

        return Result.FromSuccess();
    }
}
