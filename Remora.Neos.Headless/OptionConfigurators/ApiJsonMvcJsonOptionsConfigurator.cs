//
//  SPDX-FileName: ApiJsonMvcJsonOptionsConfigurator.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Remora.Neos.Headless.OptionConfigurators;

/// <summary>
/// Configures API-specific JSON options for MVC controllers.
/// </summary>
public class ApiJsonMvcJsonOptionsConfigurator : IConfigureOptions<MvcOptions>
{
    /// <summary>
    /// Gets the name of the JSON options.
    /// </summary>
    public static string Name => "api";

    private readonly IOptionsMonitor<JsonOptions> _jsonOptions;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiJsonMvcJsonOptionsConfigurator"/> class.
    /// </summary>
    /// <param name="jsonOptions">The JSON options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ApiJsonMvcJsonOptionsConfigurator(IOptionsMonitor<JsonOptions> jsonOptions, ILoggerFactory loggerFactory)
    {
        _jsonOptions = jsonOptions;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public void Configure(MvcOptions options)
    {
        var jsonOptions = _jsonOptions.Get(Name);

        options.InputFormatters.Insert
        (
            0,
            new SystemTextJsonInputFormatter(jsonOptions, _loggerFactory.CreateLogger<SystemTextJsonInputFormatter>())
        );

        options.OutputFormatters.Insert(0, new SystemTextJsonOutputFormatter(jsonOptions.JsonSerializerOptions));
    }
}
