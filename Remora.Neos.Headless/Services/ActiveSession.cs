//
//  SPDX-FileName: ActiveSession.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;
using Remora.Neos.Headless.Configuration;

namespace Remora.Neos.Headless.Services;

/// <summary>
/// Represents an active world.
/// </summary>
/// <param name="StartInfo">The startup parameters for the session.</param>
/// <param name="World">The world associated with the session.</param>
public record ActiveSession
(
    WorldStartupParameters StartInfo,
    World World
);
