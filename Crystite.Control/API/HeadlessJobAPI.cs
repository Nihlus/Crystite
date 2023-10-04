//
//  SPDX-FileName: HeadlessJobAPI.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json;
using Crystite.API.Abstractions.Services;
using Remora.Rest;
using Remora.Rest.Results;
using Remora.Results;

namespace Crystite.Control.API;

/// <summary>
/// Defines API endpoints for the headless job API.
/// </summary>
public class HeadlessJobAPI : AbstractHeadlessRestAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessJobAPI"/> class.
    /// </summary>
    /// <param name="restHttpClient">The headless HTTP client.</param>
    /// <param name="jsonOptions">The JSON options.</param>
    public HeadlessJobAPI(IRestHttpClient restHttpClient, JsonSerializerOptions jsonOptions)
        : base(restHttpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Retrieves information about a job.
    /// </summary>
    /// <remarks>
    /// Note that retrieving a completed job counts as taking receipt of its completion and will result in its removal
    /// from the server's job tracker.
    /// </remarks>
    /// <param name="id">The ID of the job.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The job.</returns>
    public Task<Result<IJob>> GetJobAsync(Guid id, CancellationToken ct = default)
        => this.RestHttpClient.GetAsync<IJob>($"jobs/{id}", ct: ct);

    /// <summary>
    /// Cancels the given job.
    /// </summary>
    /// <remarks>
    /// Cancelling a job is not an instant operation; it is merely a request for the ongoing task. As such, requesting a
    /// cancellation may not stop the job from completing.
    /// </remarks>
    /// <param name="id">The ID of the job.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The job.</returns>
    public Task<Result<IJob>> CancelJobAsync(Guid id, CancellationToken ct = default)
        => this.RestHttpClient.DeleteAsync<IJob>($"jobs/{id}", ct: ct);

    /// <summary>
    /// Asynchronously waits for completion of the given job, continuously polling the server about its status until
    /// completion.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="pollTime">The time between polling operations.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The completed job.</returns>
    public async Task<Result<IJob>> WaitForJobAsync(IJob job, TimeSpan? pollTime = null, CancellationToken ct = default)
    {
        pollTime ??= TimeSpan.FromSeconds(1);

        if (job.Status is not JobStatus.Running)
        {
            return Result<IJob>.FromSuccess(job);
        }

        var polledJob = job;
        while (polledJob.Status is JobStatus.Running && !ct.IsCancellationRequested)
        {
            var getJob = await GetJobAsync(job.Id, ct);
            if (!getJob.IsDefined(out polledJob))
            {
                // oops!
                return Result<IJob>.FromError(getJob);
            }

            await Task.Delay(pollTime.Value, CancellationToken.None);
        }

        return polledJob.Status switch
        {
            JobStatus.Canceled => new RestResultError<APIError>(new APIError("The world startup was canceled")),
            JobStatus.Faulted => new RestResultError<APIError>(new APIError($"Job {polledJob.Id} ({job.Description}) failed")),
            _ => Result<IJob>.FromSuccess(polledJob)
        };
    }
}
