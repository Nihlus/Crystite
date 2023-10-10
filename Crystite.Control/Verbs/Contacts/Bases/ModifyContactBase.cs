//
//  SPDX-FileName: ModifyContactBase.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace Crystite.Control.Verbs.Bases;

/// <summary>
/// Shows contact requests.
/// </summary>
public abstract class ModifyContactBase : ContactVerb
{
    /// <summary>
    /// Gets the status to modify the contact to.
    /// </summary>
    protected abstract RestContactStatus Status { get; }

    /// <summary>
    /// Gets the message to display to the end user on success.
    /// </summary>
    protected virtual string Message => $"Contact status set to {this.Status}";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifyContactBase"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ModifyContactBase(string name, string id, ushort port, string server, OutputFormat outputFormat)
        : base(name, id, port, server, outputFormat)
    {
    }

    /// <inheritdoc/>
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var contactAPI = services.GetRequiredService<HeadlessContactAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

        var getContactID = await GetTargetContactIDAsync(contactAPI, ct);
        if (!getContactID.IsDefined(out var contactID))
        {
            return (Result)getContactID;
        }

        var acceptRequest = await contactAPI.ModifyContactAsync(contactID, this.Status, ct);
        if (!acceptRequest.IsSuccess)
        {
            return acceptRequest;
        }

        await outputWriter.WriteLineAsync(this.Message);
        return Result.FromSuccess();
    }
}
