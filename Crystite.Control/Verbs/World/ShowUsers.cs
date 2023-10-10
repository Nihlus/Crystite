//
//  SPDX-FileName: ShowUsers.cs
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
/// Shows the users in a specific world.
/// </summary>
[UsedImplicitly]
[Verb("show-users", HelpText = "Shows the users in a specific world")]
public sealed class ShowUsers : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowUsers"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowUsers(string? worldName, string? worldID, ushort port, string server, OutputFormat outputFormat)
        : base(worldName, worldID, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getWorld = await GetTargetWorldIDAsync(worldAPI, ct);
        if (!getWorld.IsDefined(out var world))
        {
            return (Result)getWorld;
        }

        var getUsers = await worldAPI.GetUsersInWorldAsync(world, ct);
        if (!getUsers.IsDefined(out var users))
        {
            return (Result)getUsers;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(users, outputOptions));
                break;
            }
            case OutputFormat.Simple:
            {
                foreach (var user in users)
                {
                    await outputWriter.WriteLineAsync($"{user.Name}");
                }
                break;
            }
            case OutputFormat.Verbose:
            {
                foreach (var user in users)
                {
                    await outputWriter.WriteLineAsync($"{user.Name}\t{user.Id}");
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
