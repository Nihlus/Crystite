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

namespace Remora.Neos.Headless.API;

/// <summary>
/// Represents an ongoing job.
/// </summary>
/// <param name="Id">The ID of the job.</param>
/// <param name="Description">The human-readable description of the job.</param>
/// <param name="Action">The associated programmatic action.</param>
/// <param name="TokenSource">The cancellation token source associated with the job.</param>
[PublicAPI]
public sealed record Job
(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonIgnore] Task Action,
    [property: JsonIgnore] CancellationTokenSource TokenSource
)
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
