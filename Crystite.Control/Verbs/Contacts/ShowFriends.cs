//
//  SPDX-FileName: ShowFriends.cs
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
/// Shows contacts friends by the server account.
/// </summary>
[UsedImplicitly]
[Verb("show-friends", HelpText = "Shows contacts friends by the server account")]
public sealed class ShowFriends : ShowContacts
{
    /// <inheritdoc />
    protected override RestContactStatus? StatusFilter => RestContactStatus.Friend;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowFriends"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public ShowFriends(ushort port, string server, OutputFormat outputFormat)
        : base(port, server, outputFormat)
    {
    }
}
