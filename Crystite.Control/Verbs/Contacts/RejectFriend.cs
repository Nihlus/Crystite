//
//  SPDX-FileName: RejectFriend.cs
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
/// Rejects a friend request.
/// </summary>
[UsedImplicitly]
[Verb("reject-friend", HelpText = "Rejects a friend request")]
public sealed class RejectFriend : ModifyContactBase
{
    /// <inheritdoc />
    protected override RestContactStatus Status => RestContactStatus.None;

    /// <inheritdoc />
    protected override string Message => "Friend request rejected";

    /// <summary>
    /// Initializes a new instance of the <see cref="RejectFriend"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public RejectFriend(string name, string id, ushort port, string server, OutputFormat outputFormat)
        : base(name, id, port, server, outputFormat)
    {
    }
}
