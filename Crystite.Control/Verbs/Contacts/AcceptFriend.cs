//
//  SPDX-FileName: AcceptFriend.cs
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
/// Accepts a friend request.
/// </summary>
[UsedImplicitly]
[Verb("accept-friend", HelpText = "Accepts a friend request")]
public sealed class AcceptFriend : ModifyContactBase
{
    /// <inheritdoc />
    protected override RestContactStatus Status => RestContactStatus.Friend;

    /// <inheritdoc />
    protected override string Message => "Friend request accepted";

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptFriend"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public AcceptFriend(string name, string contactID, ushort port, string server, OutputFormat outputFormat)
        : base(name, contactID, port, server, outputFormat)
    {
    }
}
