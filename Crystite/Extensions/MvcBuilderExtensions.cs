//
//  SPDX-FileName: MvcBuilderExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.OptionConfigurators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IMvcBuilder"/> interface.
/// </summary>
public static class MvcBuilderExtensions
{
    /// <summary>
    /// Adds a API-specific JSON option set to the builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The builder, with the options added.</returns>
    public static IMvcBuilder AddApiJsonOptions(this IMvcBuilder builder, Action<JsonOptions> configure)
    {
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, ApiJsonMvcJsonOptionsConfigurator>();
        builder.Services.Configure(ApiJsonMvcJsonOptionsConfigurator.Name, configure);

        return builder;
    }
}
