//
//  SPDX-FileName: StartWorld.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using Remora.Results;

namespace Crystite.Control.Verbs;

/// <summary>
/// Starts a world.
/// </summary>
[UsedImplicitly]
[Verb("start-world", HelpText = "Starts a new world")]
public sealed class StartWorld : HeadlessVerb
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
    /// <inheritdoc cref=".ctor(ushort, string, OutputFormat)" path="/param" />
    [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
    public StartWorld
    (
        string? template,
        Uri? url,
        ushort port,
        string server,
        OutputFormat outputFormat
    )
        : base(port, server, outputFormat)
    {
        this.Template = template;
        this.Url = url;
    }

    /// <inheritdoc />
    public override async ValueTask<Result> ExecuteAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var worldAPI = services.GetRequiredService<HeadlessWorldAPI>();
        var jobAPI = services.GetRequiredService<HeadlessJobAPI>();
        var outputWriter = services.GetRequiredService<TextWriter>();

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

        if (this.OutputFormat is OutputFormat.Verbose)
        {
            await outputWriter.WriteLineAsync("World started");
        }

        return Result.FromSuccess();
    }
}
