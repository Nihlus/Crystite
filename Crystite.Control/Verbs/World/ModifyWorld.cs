//
//  SPDX-FileName: ModifyWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Modifies a specific running world.
/// </summary>
[UsedImplicitly]
[Verb("modify-world", HelpText = "Modifies a specific running world")]
public sealed class ModifyWorld : WorldVerb
{
    /// <summary>
    /// Gets the new name of the world.
    /// </summary>
    [Option("new-name", Required = false, HelpText = "The new name of the world")]
    public Optional<string> NewName { get; }

    /// <summary>
    /// Gets the new description of the world.
    /// </summary>
    [Option('d', "description", Required = false, HelpText = "The new description of the world")]
    public Optional<string> Description { get; }

    /// <summary>
    /// Gets the new access level of the world.
    /// </summary>
    [Option('a', "access-level", Required = false, HelpText = "The new access level of the world")]
    public Optional<RestAccessLevel> AccessLevel { get; }

    /// <summary>
    /// Gets the new kick interval for away users (in minutes).
    /// </summary>
    [Option('k', "away-kick-interval", Required = false, HelpText = "The new kick interval for away users (in minutes)")]
    public Optional<float> AwayKickInterval { get; }

    /// <summary>
    /// Gets a value indicating whether to hide the world from public listing.
    /// </summary>
    [Option('h', "hide-from-listing", Required = false, HelpText = "Whether to hide the world from public listing")]
    public Optional<bool> HideFromListing { get; }

    /// <summary>
    /// Gets the new maximum number of users allowed in the world.
    /// </summary>
    [Option('m', "max-users", Required = false, HelpText = "The new maximum number of users allowed in the world")]
    public Optional<int> MaxUsers { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifyWorld"/> class.
    /// </summary>
    /// <param name="newName">The new name of the world.</param>
    /// <param name="description">The new description of the world.</param>
    /// <param name="accessLevel">The new access level of the world.</param>
    /// <param name="awayKickInterval">The new kick interval for away users.</param>
    /// <param name="hideFromListing">Whether to hide the world from public listing.</param>
    /// <param name="maxUsers">The new maximum number of users allowed in the world.</param>
    /// <inheritdoc cref="WorldVerb(string, string, ushort, string, OutputFormat)" path="/param" />
    #pragma warning disable CS1573
    public ModifyWorld
    (
        Optional<string> newName,
        Optional<string> description,
        Optional<RestAccessLevel> accessLevel,
        Optional<float> awayKickInterval,
        Optional<bool> hideFromListing,
        Optional<int> maxUsers,
        string? name,
        string? id,
        ushort port,
        string server,
        OutputFormat outputFormat
    )
        : base(name, id, port, server, outputFormat)
    {
        this.NewName = newName;
        this.Description = description;
        this.AccessLevel = accessLevel;
        this.AwayKickInterval = awayKickInterval;
        this.HideFromListing = hideFromListing;
        this.MaxUsers = maxUsers;
    }
    #pragma warning restore CS1573

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

        var modifyWorld = await worldAPI.ModifyWorldAsync
        (
            world,
            this.NewName,
            this.Description,
            this.AccessLevel,
            this.AwayKickInterval.Map(minutes => TimeSpan.FromMinutes(minutes)),
            this.HideFromListing,
            this.MaxUsers,
            ct
        );

        if (!modifyWorld.IsSuccess)
        {
            return (Result)modifyWorld;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(world, outputOptions));
                break;
            }
            case OutputFormat.Verbose:
            {
                await outputWriter.WriteLineAsync("World modified");
                break;
            }
            case OutputFormat.Simple:
            {
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
