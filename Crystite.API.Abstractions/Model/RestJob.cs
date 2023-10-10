//
//  SPDX-FileName: RestJob.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Text.Json.Serialization;
using Crystite.API.Abstractions.Services;

namespace Crystite.API.Abstractions;

/// <inheritdoc />
public sealed record RestJob
(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("status")] JobStatus Status
) : IRestJob;

/// <summary>
/// Represents an ongoing job.
/// </summary>
public interface IRestJob
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
