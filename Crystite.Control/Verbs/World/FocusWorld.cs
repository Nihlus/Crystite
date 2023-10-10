//
//  SPDX-FileName: FocusWorld.cs
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
/// Focuses a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("focus-world", HelpText = "Focuses a specific running world")]
public sealed class FocusWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FocusWorld"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public FocusWorld(string? worldName, string? worldID, ushort port, string server, OutputFormat outputFormat)
        : base(worldName, worldID, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

        var getWorld = await GetTargetWorldIDAsync(worldAPI, ct);
        if (!getWorld.IsDefined(out var world))
        {
            return (Result)getWorld;
        }

        var focusWorld = await worldAPI.SetFocusedWorldAsync(world, ct: ct);
        if (!focusWorld.IsSuccess)
        {
            return focusWorld;
        }

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("World focused");
        }

        return Result.FromSuccess();
    }
}
