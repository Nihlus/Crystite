//
//  SPDX-FileName: ShowRequested.cs
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
/// Shows contact requests.
/// </summary>
[UsedImplicitly]
[Verb("show-requested", HelpText = "Shows contact requests")]
public sealed class ShowRequested : ShowContacts
{
    /// <inheritdoc />
    protected override RestContactStatus? StatusFilter => RestContactStatus.Requested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowRequested"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowRequested(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
    {
    }
}
