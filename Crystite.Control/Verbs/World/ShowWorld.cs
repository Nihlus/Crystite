//
//  SPDX-FileName: ShowWorld.cs
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
/// Shows a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("show-world", HelpText = "Shows a specific running world")]
public sealed class ShowWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorld"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowWorld(string? worldName, string? worldID, ushort port, string server, OutputFormat outputFormat)
        : base(worldName, worldID, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getWorld = await GetTargetWorldAsync(worldAPI, ct);
        if (!getWorld.IsDefined(out var world))
        {
            return (Result)getWorld;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Simple:
            {
                await outputWriter.WriteLineAsync(world.Name);
                break;
            }
            case OutputFormat.Verbose:
            {
                var description = world.Description is null
                    ? string.Empty
                    : $"\t{world.Description}";

                await outputWriter.WriteLineAsync($"{world.Name}\t{world.Id}{description}");
                break;
            }
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(world, outputOptions));
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
