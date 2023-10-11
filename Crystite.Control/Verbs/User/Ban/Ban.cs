//
//  SPDX-FileName: Ban.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using CommandLine;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using Crystite.Control.Verbs.User.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Results;

namespace Crystite.Control.Verbs.User.Ban;

/// <summary>
/// Bans a user from all sessions on the server.
/// </summary>
[UsedImplicitly]
[Verb("ban", HelpText = "Bans a user from all sessions on the server")]
public sealed class Ban : UserVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Ban"/> class.
    /// </summary>
    /// <inheritdoc cref="UserVerb(string, string, ushort, string, OutputFormat)" path="/param" />
    public Ban(string? userName, string? userID, ushort port, string server, OutputFormat outputFormat)
        : base(userName, userID, port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var userAPI = services.GetRequiredService<HeadlessUserAPI>();
        var banAPI = services.GetRequiredService<HeadlessBanAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getUserID = await GetTargetUserIDAsync(userAPI, ct);
        if (!getUserID.IsDefined(out var userID))
        {
            return (Result)getUserID;
        }

        var banUser = await banAPI.BanUserAsync(userID, ct);
        if (!banUser.IsDefined(out var ban))
        {
            return (Result)banUser;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Simple:
            {
                break;
            }
            case OutputFormat.Verbose:
            {
                await outputWriter.WriteLineAsync("User banned");
                break;
            }
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(ban, outputOptions));
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
