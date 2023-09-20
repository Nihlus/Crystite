//
//  SPDX-FileName: StartWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using Remora.Neos.Control.API;
using Remora.Neos.Control.Verbs.Bases;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Rest.Results;
using Remora.Results;

namespace Remora.Neos.Control.Verbs;

/// <summary>
/// Starts a world.
/// </summary>
[Verb("show-world", HelpText = "Shows a specific running world")]
public sealed class StartWorld : WorldVerb
{
    /// <summary>
    /// Gets the name of the builtin template to start.
    /// </summary>
    [Option('t', "template", Required = true, SetName = "WORLD_TEMPLATE")]
    public string? Template { get; }

    /// <summary>
    /// Gets the record URL of the world to start.
    /// </summary>
    [Option('u', "url", Required = true, SetName = "WORLD_URL")]
    public Uri? Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartWorld"/> class.
    /// </summary>
    /// <param name="template">The name of the builtin template to start.</param>
    /// <param name="url">The record URL of the world to start.</param>
    /// <inheritdoc cref="WorldVerb(string, string, ushort, string, bool)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public StartWorld
    (
        string? template,
        Uri? url,
        string? name,
        string? id,
        ushort port,
        string server,
        bool verbose
    )
        : base(name, id, port, server, verbose)
    {
        this.Template = template;
        this.Url = url;
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var jobAPI = services.GetRequiredService<HeadlessJobAPI>();

        OneOf<string, Uri> startArgument;
        if (this.Template is not null)
        {
            startArgument = this.Template;
        }
        else if (this.Url is not null)
        {
            startArgument = this.Url;
        }
        else
        {
            throw new InvalidOperationException();
        }

        var startJob = await worldAPI.StartWorldAsync(startArgument, ct);
        if (!startJob.IsDefined(out var job))
        {
            return (Result)startJob;
        }

        var awaitJob = await jobAPI.WaitForJobAsync(job, ct: ct);
        if (!awaitJob.IsDefined(out job))
        {
            return (Result)awaitJob;
        }

        Console.WriteLine("World started");
        return Result.FromSuccess();
    }
}
