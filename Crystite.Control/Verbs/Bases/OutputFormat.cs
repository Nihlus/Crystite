//
//  SPDX-FileName: OutputFormat.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Crystite.Control.Verbs.Bases;

/// <summary>
/// Enumerates various output formats for the program.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Simple, minimal output.
    /// </summary>
    Simple,

    /// <summary>
    /// Verbose, more detailed output.
    /// </summary>
    Verbose,

    /// <summary>
    /// Raw JSON output of whatever the response was.
    /// </summary>
    Json
}
