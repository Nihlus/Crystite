//
//  SPDX-FileName: ShowBlocked.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;

namespace Crystite.Control.Verbs;

/// <summary>
/// Shows contacts blocked by the server account.
/// </summary>
[UsedImplicitly]
[Verb("show-blocked", HelpText = "Shows contacts blocked by the server account")]
public sealed class ShowBlocked : ShowContacts
{
    /// <inheritdoc />
    protected override RestContactStatus? StatusFilter => RestContactStatus.Blocked;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowBlocked"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowBlocked(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
    {
    }
}
