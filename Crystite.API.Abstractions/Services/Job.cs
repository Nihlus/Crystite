//
//  SPDX-FileName: Job.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Crystite.API.Abstractions.Services;

#pragma warning disable SA1402

/// <inheritdoc />
[PublicAPI]
public sealed record Job
(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonIgnore] Task Action,
    [property: JsonIgnore] CancellationTokenSource TokenSource
) : IJob
{
    /// <summary>
    /// Gets the status of the job.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("status")]
    public JobStatus Status => this.Action.IsCanceled
        ? JobStatus.Canceled
        : this.Action.IsFaulted
            ? JobStatus.Faulted
            : this.Action.IsCompleted
                ? JobStatus.Completed
                : JobStatus.Running;
}

/// <summary>
/// Represents an ongoing job.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Gets the status of the job.
    /// </summary>
    JobStatus Status { get; }

    /// <summary>
    /// Gets the ID of the job.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the human-readable description of the job.
    /// </summary>
    string Description { get; }
}
