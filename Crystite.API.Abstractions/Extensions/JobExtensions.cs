//
//  SPDX-FileName: JobExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions.Services;

namespace Crystite.API.Abstractions.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="Job"/> record.
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Converts a job to a REST-compatible representation.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <returns>The REST-compatible job.</returns>
    public static RestJob ToRestJob(this Job job) => new(job.Id, job.Description, job.Status);
}
