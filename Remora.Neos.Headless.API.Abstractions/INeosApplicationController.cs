//
//  SPDX-FileName: INeosApplicationController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.API.Abstractions;

/// <summary>
/// Represents the public API of application-level control logic.
/// </summary>
[PublicAPI]
public interface INeosApplicationController
{
    /// <summary>
    /// Gets a value indicating whether shutdown has been requested.
    /// </summary>
    bool HasShutdownBeenRequested { get; }

    /// <summary>
    /// Shuts down the entire application, gracefully terminating any ongoing operations.
    /// </summary>
    /// <remarks>This method does not support cancellation; once requested, shutdown cannot be stopped.</remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ShutdownAsync();
}
