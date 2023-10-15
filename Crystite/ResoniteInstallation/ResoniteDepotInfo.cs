//
//  SPDX-FileName: ResoniteDepotInfo.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Crystite.ResoniteInstallation;

/// <summary>
/// Represents information about a downloadable depot.
/// </summary>
public record ResoniteDepotInfo(uint ID, ulong ManifestID, byte[] Key);
