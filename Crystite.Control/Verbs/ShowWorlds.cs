//
//  SPDX-FileName: ShowWorlds.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CommandLine;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Shows the running worlds.
/// </summary>
[Verb("show-worlds", HelpText = "Shows the running worlds")]
public class ShowWorlds : HeadlessVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorlds"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb(ushort, string, bool)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowWorlds(ushort port, string server, bool verbose)
        : base(port, server, verbose)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getWorlds = await worldAPI.GetWorldsAsync(ct);
        if (!getWorlds.IsDefined(out var worlds))
        {
            return (Result)getWorlds;
        }

        if (this.Verbose)
        {
            await outputWriter.WriteLineAsync(JsonSerializer.Serialize(worlds, outputOptions));
        }
        else
        {
            foreach (var world in worlds)
            {
                var description = world.Description is null
                    ? string.Empty
                    : $"\t{world.Description}";

                await outputWriter.WriteLineAsync($"{world.Name}\t{world.Id}{description}");
            }
        }

        return Result.FromSuccess();
    }
}
