//
//  SPDX-FileName: ServiceCollectionExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using Crystite.API.Abstractions;
using Crystite.API.Abstractions.Services;
using Crystite.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Crystite.API.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Resonite controller services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="TApplicationController">The concrete application controller type.</typeparam>
    /// <typeparam name="TWorldController">The concrete world controller type.</typeparam>
    /// <returns>The service collection, with the services added.</returns>
    public static IServiceCollection AddResoniteControllerServices<TApplicationController, TWorldController>
    (
        this IServiceCollection services
    )
        where TApplicationController : class, IResoniteApplicationController
        where TWorldController : class, IResoniteWorldController
    {
        return services
            .AddSingleton<IResoniteApplicationController, TApplicationController>()
            .AddSingleton<IResoniteBanController, ResoniteBanController>()
            .AddSingleton<IResoniteContactController, ResoniteContactController>()
            .AddSingleton<IResoniteUserController, ResoniteUserController>()
            .AddSingleton<IResoniteWorldController, TWorldController>()
            .AddSingleton<IJobService, JobService>();
    }
}
