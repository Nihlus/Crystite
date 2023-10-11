//
//  SPDX-FileName: SetRole.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace Crystite.Control.Verbs.User;

/// <summary>
/// Sets a new role for the given user.
/// </summary>
[UsedImplicitly]
[Verb("set-role", HelpText = "Sets a new role for the given user")]
public sealed class SetRole : WorldUserVerb
{
    /// <summary>
    /// Gets the new role of the user.
    /// </summary>
    [Option('r', "role", Required = true, HelpText = "The new role of the user")]
    public RestUserRole Role { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetRole"/> class.
    /// </summary>
    /// <param name="role">The new role of the user.</param>
    /// <inheritdoc cref="WorldUserVerb(string, string, string, string, ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public SetRole
    (
        RestUserRole role,
        string? userName,
        string? userID,
        string? worldName,
        string? worldID,
        ushort port,
        string server,
        OutputFormat outputFormat
    )
        : base(userName, userID, worldName, worldID, port, server, outputFormat)
    {
        this.Role = role;
    }

    /// <inheritdoc/>
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

        var getWorldID = await GetTargetWorldIDAsync(worldAPI, ct);
        if (!getWorldID.IsDefined(out var worldID))
        {
            return (Result)getWorldID;
        }

        var getUserID = await GetTargetUserIDAsync(worldAPI, ct);
        if (!getUserID.IsDefined(out var userID))
        {
            return (Result)getUserID;
        }

        var roleUser = await worldAPI.SetWorldUserRoleAsync(worldID, userID, this.Role, ct);
        if (!roleUser.IsSuccess)
        {
            return roleUser;
        }

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("User role set");
        }

        return Result.FromSuccess();
    }
}
