//
//  SPDX-FileName: ShowWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using Remora.Neos.Control.API;
using Remora.Neos.Control.Verbs.Bases;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Results;

namespace Remora.Neos.Control.Verbs;

/// <summary>
/// Shows a specific running world.
/// </summary>
[Verb("show-world", HelpText = "Shows a specific running world")]
public sealed class ShowWorld : WorldVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorld"/> class.
    /// </summary>
    /// <param name="name">The name of the world. Mutually exclusive with <paramref name="id"/>.</param>
    /// <param name="id">The ID of the world. Mutually exclusive with <paramref name="name"/>.</param>
    /// <param name="port">The port.</param>
    /// <param name="server">The server.</param>
    public ShowWorld(string? name, string? id, ushort port, string server)
        : base(name, id, port, server)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();

        IRestWorld? world;
        if (this.ID is null)
        {
            var getWorlds = await worldAPI.GetWorldsAsync(ct);
            if (!getWorlds.IsDefined(out var worlds))
            {
                return (Result)getWorlds;
            }

            world = worlds.FirstOrDefault(w => w.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));
            if (world is null)
            {
                return new NotFoundError($"No world named \"{this.Name}\" found");
            }
        }
        else
        {
            var getWorld = await worldAPI.GetWorldAsync(this.ID, ct);
            if (!getWorld.IsDefined(out world))
            {
                return (Result)getWorld;
            }
        }

        var table = new ConsoleTable("Name", "ID", "Description")
        {
            Options =
            {
                EnableCount = false
            }
        };

        table.AddRow(world.Name, world.Id, world.Description);
        table.Write();

        return Result.FromSuccess();
    }
}
