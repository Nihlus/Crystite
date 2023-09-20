//
//  SPDX-FileName: ShowWorlds.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Neos.Control.API;
using Remora.Neos.Control.Verbs.Bases;
using Remora.Results;

namespace Remora.Neos.Control.Verbs;

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

        var getWorlds = await worldAPI.GetWorldsAsync(ct);
        if (!getWorlds.IsSuccess)
        {
            return (Result)getWorlds;
        }

        var table = new ConsoleTable("Name", "ID", "Description")
        {
            Options =
            {
                EnableCount = false
            }
        };

        foreach (var world in getWorlds.Entity)
        {
            table.AddRow(world.Name, world.Id, world.Description);
        }

        table.Write();
        return Result.FromSuccess();
    }
}
