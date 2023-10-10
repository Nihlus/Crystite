//
//  SPDX-FileName: BlockUser.cs
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
/// Blocks a user.
/// </summary>
[UsedImplicitly]
[Verb("block-user", HelpText = "Blocks a user")]
public sealed class BlockUser : ModifyContactBase
{
    /// <inheritdoc />
    protected override RestContactStatus Status => RestContactStatus.Blocked;

    /// <inheritdoc />
    protected override string Message => "User blocked";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockUser"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public BlockUser(string contactName, string id, ushort port, string server, OutputFormat outputFormat)
        : base(contactName, id, port, server, outputFormat)
    {
    }
}
