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

namespace Remora.Neos.Control.Verbs.Bases;

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
    /// Initializes a new instance of the <see cref="HeadlessVerb"/> class.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="server">The server.</param>
    protected HeadlessVerb(ushort port, string server)
    {
        this.Port = port;
        this.Server = server;
    }

    /// <inheritdoc />
    public abstract ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default);
}
