//
//  SPDX-FileName: Unban.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using CommandLine;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using Crystite.Control.Verbs.Users.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace Crystite.Control.Verbs.Users.Bans;

/// <summary>
/// Unbans a user from all sessions on the server.
/// </summary>
[UsedImplicitly]
[Verb("unban", HelpText = "Unbans a user from all sessions on the server")]
public sealed class Unban : UserVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Unban"/> class.
    /// </summary>
    /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
    public Unban(string? name, string? id, ushort port, string server, OutputFormat outputFormat)
        : base(name, id, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var userAPI = services.GetRequiredService<HeadlessUserAPI>();
        var unbanAPI = services.GetRequiredService<HeadlessBanAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

        var getUserID = await GetTargetUserIDAsync(userAPI, ct);
        if (!getUserID.IsDefined(out var userID))
        {
            return (Result)getUserID;
        }

        var unbanUser = await unbanAPI.UnbanUserAsync(userID, ct);
        if (!unbanUser.IsSuccess)
        {
            return unbanUser;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Verbose:
            {
                await outputWriter.WriteLineAsync("User unbanned");
                break;
            }
            case OutputFormat.Simple:
            case OutputFormat.Json:
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
