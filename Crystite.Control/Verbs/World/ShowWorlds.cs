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
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Shows the running worlds.
/// </summary>
[UsedImplicitly]
[Verb("show-worlds", HelpText = "Shows the running worlds")]
public sealed class ShowWorlds : HeadlessVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorlds"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowWorlds(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
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

        switch (this.OutputFormat)
        {
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(worlds, outputOptions));
                break;
            }
            case OutputFormat.Simple:
            {
                foreach (var world in worlds)
                {
                    await outputWriter.WriteLineAsync(world.Name);
                }

                break;
            }
            case OutputFormat.Verbose:
            {
                foreach (var world in worlds)
                {
                    var description = world.Description is null
                        ? string.Empty
                        : $"\t{world.Description}";

                    await outputWriter.WriteLineAsync($"{world.Name}\t{world.Id}\t{description}");
                }

                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        return Result.FromSuccess();
    }
}
