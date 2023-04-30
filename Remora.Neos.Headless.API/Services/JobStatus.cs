//
//  SPDX-FileName: JobStatus.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Remora.Neos.Headless.API;

/// <summary>
/// Enumerates valid states for a job.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// The job is currently running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The job completed without any reported errors.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The job was canceled.
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// The job faulted due to some type of internal error.
    /// </summary>
    Faulted = 4
}
