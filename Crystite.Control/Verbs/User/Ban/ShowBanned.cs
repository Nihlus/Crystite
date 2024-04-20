//
//  SPDX-FileName: ShowBanned.cs
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

namespace Crystite.Control.Verbs.User.Ban;

/// <summary>
/// Shows the running banned.
/// </summary>
[UsedImplicitly]
[Verb("show-banned", HelpText = "Shows the running banned")]
public sealed class ShowBanned : HeadlessVerb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowBanned"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb(ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowBanned(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var banAPI = services.GetRequiredService<HeadlessBanAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getBans = await banAPI.GetBansAsync(ct);
        if (!getBans.IsDefined(out var bans))
        {
            return (Result)getBans;
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(bans, outputOptions));
                break;
            }
            case OutputFormat.Simple:
            {
                foreach (var ban in bans)
                {
                    await outputWriter.WriteLineAsync(ban.Username);
                }

                break;
            }
            case OutputFormat.Verbose:
            {
                foreach (var ban in bans)
                {
                    await outputWriter.WriteLineAsync
                    (
                        $"{ban.Username}\t{ban.Id}\t{string.Join(", ", ban.MachineIds ?? Array.Empty<string>())}"
                    );
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
