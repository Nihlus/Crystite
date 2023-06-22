//
//  SPDX-FileName: RecordStoreNotifications.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;
using HarmonyLib;
using JetBrains.Annotations;

namespace Remora.Neos.Headless.Patches.EngineRecordUploadTask;

/// <summary>
/// Provides event-driven access to records that are stored after finishing synchronization.
/// </summary>
[HarmonyPatch(typeof(FrooxEngine.EngineRecordUploadTask), "StoreSyncedRecord")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class RecordStoreNotifications
{
    /// <summary>
    /// Raised when a record is successfully stored locally. This happens after completed remote synchronization.
    /// </summary>
    public static event Action<Record>? RecordStored;

    /// <summary>
    /// Raises the <see cref="RecordStored"/> event after calls to
    /// <see cref="FrooxEngine.EngineRecordUploadTask.StoreSyncedRecord"/>.
    /// </summary>
    /// <param name="record">The stored record.</param>
    [HarmonyPostfix]
    public static void Postfix(Record record)
    {
        RecordStored?.Invoke(record);
    }
}
