//
//  SPDX-FileName: HeadlessVerb.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Remora.Results;

namespace Crystite.Control.Verbs.Bases;

/// <summary>
/// Represents common options for all verbs.
/// </summary>
public abstract class HeadlessVerb : IExecutableVerb
{
    /// <summary>
    /// Gets the port the server API listens on.
    /// </summary>
    [Option('p', "port", Default = (ushort)5000, HelpText = "The port to communicate with")]
    public ushort Port { get; }

    /// <summary>
    /// Gets the URL of the server.
    /// </summary>
    [Option('s', "server", Default = "http://localhost", HelpText = "The server to control")]
    public string Server { get; }

    /// <summary>
    /// Gets a value indicating whether more detailed information should be displayed.
    /// </summary>
    [Option('v', "verbose", Default = false, HelpText = "Whether to output detailed information")]
    public bool Verbose { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessVerb"/> class.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="server">The server.</param>
    /// <param name="verbose">Whether to output detailed information.</param>
    protected HeadlessVerb(ushort port, string server, bool verbose)
    {
        this.Port = port;
        this.Server = server;
        this.Verbose = verbose;
    }

    /// <inheritdoc />
    public abstract ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default);
}
