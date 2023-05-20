//
//  SPDX-FileName: HeadlessSystemInfo.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;
using Hardware.Info;

namespace Remora.Neos.Headless;

/// <summary>
/// Represents platform-independent information about the system.
/// </summary>
public class HeadlessSystemInfo : StandaloneSystemInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessSystemInfo"/> class.
    /// </summary>
    /// <param name="hardwareInfo">Information about the current system hardware.</param>
    public HeadlessSystemInfo(IHardwareInfo hardwareInfo)
    {
        var cpu = hardwareInfo.CpuList.FirstOrDefault();
        var gpu = hardwareInfo.VideoControllerList.FirstOrDefault();

        this.CPU = cpu?.Name.Trim() ?? "UNKNOWN";
        this.GPU = gpu?.Name.Trim() ?? "UNKNOWN";
        this.PhysicalCores = (int)hardwareInfo.CpuList.Sum(c => c.NumberOfCores);
        this.MemoryBytes = hardwareInfo.MemoryList.Sum(r => (long)r.Capacity);
        this.VRAMBytes = (long?)gpu?.AdapterRAM ?? -1;
    }
}
