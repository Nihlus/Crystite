//
//  SPDX-FileName: ShowContacts.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Shows all contacts of the server account.
/// </summary>
[UsedImplicitly]
[Verb("show-contacts", HelpText = "Shows all contacts of the server account")]
public class ShowContacts : HeadlessVerb
{
    /// <summary>
    /// Gets the status to filter on.
    /// </summary>
    protected virtual RestContactStatus? StatusFilter => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowContacts"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb(ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowContacts(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var contactAPI = services.GetRequiredService<HeadlessContactAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();
        var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");

        var getContacts = await contactAPI.GetContactsAsync(ct);
        if (!getContacts.IsDefined(out var contacts))
        {
            return (Result)getContacts;
        }

        if (this.StatusFilter is not null)
        {
            contacts = contacts.Where(c => c.Status == this.StatusFilter).ToList();
        }

        switch (this.OutputFormat)
        {
            case OutputFormat.Json:
            {
                await outputWriter.WriteLineAsync(JsonSerializer.Serialize(contacts, outputOptions));
                break;
            }
            case OutputFormat.Simple:
            {
                foreach (var contact in contacts)
                {
                    await outputWriter.WriteLineAsync($"{contact.Username}\t{contact.Status}");
                }
                break;
            }
            case OutputFormat.Verbose:
            {
                foreach (var contact in contacts)
                {
                    await outputWriter.WriteLineAsync($"{contact.Username}\t{contact.Id}\t{contact.Status}");
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
