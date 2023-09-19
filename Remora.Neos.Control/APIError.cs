//
//  SPDX-FileName: APIError.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

namespace Remora.Neos.Control;

/// <summary>
/// Represents a simple error returned by the API.
/// </summary>
/// <param name="Message">The message.</param>
public record APIError(string? Message = null);
