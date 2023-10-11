//
//  SPDX-FileName: SaveWorld.cs
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
/// Saves a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("save-world", HelpText = "Saves a specific running world")]
public sealed class SaveWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveWorld"/> class.
    /// </summary>
    /// <inheritdoc cref="WorldVerb(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public SaveWorld(string? name, string? id, ushort port, string server, OutputFormat outputFormat)
        : base(name, id, port, server, outputFormat)
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

        var saveWorld = await worldAPI.SaveWorldAsync(world, ct);
        if (!saveWorld.IsDefined(out var job))
        {
            return (Result)saveWorld;
        }

        var waitForSave = await jobAPI.WaitForJobAsync(job, ct: ct);
        if (!waitForSave.IsSuccess)
        {
            return (Result)waitForSave;
        }

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("World saved");
        }

        return Result.FromSuccess();
    }
}
