//
//  SPDX-FileName: CommandLineOptions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using JetBrains.Annotations;

namespace Crystite.Configuration;

/// <summary>
/// Represents command-line flags passed to the program. These options should not be persistent, and are only provided
/// as a way to force certain one-time operations.
/// </summary>
/// <param name="ForceSync">Force synchronization of local records to the cloud, regardless of the remote version.</param>
/// <param name="DeleteUnsynced">Delete any local records that have not yet been synchronized to the cloud.</param>
/// <param name="RepairDatabase">Repair the local database.</param>
public record CommandLineOptions(bool ForceSync = false, bool DeleteUnsynced = false, bool RepairDatabase = false)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
    /// </summary>
    [UsedImplicitly]
    public CommandLineOptions()
        : this(ForceSync: false) // force overload resolution
    {
    }
}
