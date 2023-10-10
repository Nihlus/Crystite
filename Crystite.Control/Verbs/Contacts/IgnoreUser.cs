//
//  SPDX-FileName: IgnoreUser.cs
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
/// Ignores a user.
/// </summary>
[UsedImplicitly]
[Verb("ignore-user", HelpText = "Ignores a user")]
public sealed class IgnoreUser : ModifyContactBase
{
    /// <inheritdoc />
    protected override RestContactStatus Status => RestContactStatus.Ignored;

    /// <inheritdoc />
    protected override string Message => "User ignored";

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreUser"/> class.
    /// </summary>
    /// <inheritdoc cref="HeadlessVerb" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public IgnoreUser(string contactName, string contactID, ushort port, string server, OutputFormat outputFormat)
        : base(contactName, contactID, port, server, outputFormat)
    {
    }
}
