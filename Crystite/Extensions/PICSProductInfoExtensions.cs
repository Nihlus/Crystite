//
//  SPDX-FileName: PICSProductInfoExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using SteamKit2;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="PICSProductInfo"/> class.
/// </summary>
public static class PICSProductInfoExtensions
{
    /// <summary>
    /// Gets the available depots for the given operating system and architecture.
    /// </summary>
    /// <param name="productInfo">The product information.</param>
    /// <param name="os">The operating system.</param>
    /// <param name="arch">The architecture.</param>
    /// <returns>The depots.</returns>
    public static IEnumerable<KeyValue> GetDepots(this PICSProductInfo productInfo, string os, string arch)
    {
        if (!productInfo.KeyValues.TryGet("depots", out KeyValue? depots))
        {
            throw new ArgumentException("The product information did not contain any depots", nameof(productInfo));
        }

        foreach (var depot in depots.Children)
        {
            if (!depot.TryGet("config", out KeyValue? config))
            {
                continue;
            }

            // filter out depots which are for specific operating systems
            if (config.TryGet("oslist", out string? oslist) && !oslist.Split(',').Contains(os))
            {
                continue;
            }

            // filter out depots which are for specific architectures
            if (config.TryGet("osarch", out string? osarch) && osarch != arch)
            {
                continue;
            }

            if (!depot.TryGet("manifests", out KeyValue? manifests))
            {
                // no use downloading manifestless depots
                continue;
            }

            if (!manifests.TryGet("public", out KeyValue? _))
            {
                // only include public depots
                continue;
            }

            yield return depot;
        }
    }
}
