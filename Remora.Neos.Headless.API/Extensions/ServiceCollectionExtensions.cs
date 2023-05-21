//
//  SPDX-FileName: ServiceCollectionExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Microsoft.Extensions.DependencyInjection;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Neos.Headless.API.Services;

namespace Remora.Neos.Headless.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NeosVR controller services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="TApplicationController">The concrete application controller type.</typeparam>
    /// <typeparam name="TWorldController">The concrete world controller type.</typeparam>
    /// <returns>The service collection, with the services added.</returns>
    public static IServiceCollection AddNeosControllerServices<TApplicationController, TWorldController>
    (
        this IServiceCollection services
    )
        where TApplicationController : class, INeosApplicationController
        where TWorldController : class, INeosWorldController
    {
        return services
            .AddSingleton<INeosApplicationController, TApplicationController>()
            .AddSingleton<INeosBanController, NeosBanController>()
            .AddSingleton<INeosContactController, NeosContactController>()
            .AddSingleton<INeosWorldController, TWorldController>()
            .AddSingleton<IJobService, JobService>();
    }
}
