//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.API.Abstractions.Services;
using Crystite.Control;
using Crystite.Control.API;
using Crystite.Control.Extensions;
using Crystite.Control.Verbs.Bases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Remora.Rest;
using Remora.Rest.Extensions;
using Remora.Rest.Json;
using Remora.Rest.Json.Policies;
using Remora.Rest.Results;

var verb = ParseVerb(args);
if (verb is null)
{
    return 1;
}

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton(Console.Out);

ConfigureJsonSerializerOptions(serviceCollection);
ConfigureHeadlessAPIServices(verb.Port, verb.Server, serviceCollection);

var services = serviceCollection.BuildServiceProvider();
var result = await verb.ExecuteAsync(services);

if (result.IsSuccess)
{
    return 0;
}

if (verb.OutputFormat is OutputFormat.Json && result.Error is RestResultError<APIError> apiError)
{
    var outputOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite");
    Console.Error.WriteLine(JsonSerializer.Serialize(apiError.Error, outputOptions));
}
else
{
    Console.Error.WriteLine(result.Error.Explain());
}

return 1;

/// <summary>
/// Contains various testable helpers for the program entrypoint.
/// </summary>
public static partial class Program
{
    /// <summary>
    /// Parses the command verb from the provided arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The verb, or null if errors occurred.</returns>
    public static HeadlessVerb? ParseVerb(IEnumerable<string> args)
    {
        var verbs = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() is not null)
            .ToArray();

        var options = Parser.Default.ParseArguments(args, verbs);

        if (options.Errors.Any())
        {
            return null;
        }

        if (options.Value is not HeadlessVerb headlessVerb)
        {
            throw new InvalidOperationException();
        }

        return headlessVerb;
    }

    /// <summary>
    /// Configures the JSON serializer options in the service provider.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static void ConfigureJsonSerializerOptions(IServiceCollection serviceCollection)
    {
        // configure JSON options
        serviceCollection.Configure<JsonSerializerOptions>
        (
            "Crystite",
            o =>
            {
                o.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
                o.WriteIndented = true;

                o.Converters.Add(new StringEnumConverter<RestContactStatus>(o.PropertyNamingPolicy));
                o.Converters.Add(new StringEnumConverter<RestAccessLevel>(o.PropertyNamingPolicy));
                o.Converters.Add(new StringEnumConverter<RestUserRole>(o.PropertyNamingPolicy));
                o.Converters.Add(new StringEnumConverter<JobStatus>(o.PropertyNamingPolicy));

                o.AddDataObjectConverter<IRestBan, RestBan>();
                o.AddDataObjectConverter<IRestContact, RestContact>();
                o.AddDataObjectConverter<IRestUser, RestUser>();
                o.AddDataObjectConverter<IRestWorld, RestWorld>();
                o.AddDataObjectConverter<IRestJob, RestJob>();
            }
        );
    }

    /// <summary>
    /// Configures the headless API services.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    /// <param name="server">The URI of the server to connect to.</param>
    /// <param name="serviceCollection">The service collection to configure.</param>
    /// <param name="configureClient">Optional additional configuration for the HTTP client.</param>
    public static void ConfigureHeadlessAPIServices
    (
        ushort port,
        string server,
        IServiceCollection serviceCollection,
        Action<IHttpClientBuilder>? configureClient = null
    )
    {
        // add API services
        serviceCollection.TryAddTransient(s => new HeadlessBanAPI
        (
            s.GetRequiredService<IRestHttpClient>(),
            s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite")
        ));

        serviceCollection.TryAddTransient(s => new HeadlessContactAPI
        (
            s.GetRequiredService<IRestHttpClient>(),
            s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite")
        ));

        serviceCollection.TryAddTransient(s => new HeadlessJobAPI
        (
            s.GetRequiredService<IRestHttpClient>(),
            s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite")
        ));

        serviceCollection.TryAddTransient(s => new HeadlessUserAPI
        (
            s.GetRequiredService<IRestHttpClient>(),
            s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite")
        ));

        serviceCollection.TryAddTransient(s => new HeadlessWorldAPI
        (
            s.GetRequiredService<IRestHttpClient>(),
            s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Crystite")
        ));

        // configure the HTTP client
        var clientBuilder = serviceCollection
            .AddRestHttpClient<APIError>("Crystite")
            .ConfigureHttpClient((_, client) =>
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                var name = assemblyName.Name ?? "Crystite.Control";
                var version = assemblyName.Version ?? new Version(1, 0, 0);

                var builder = new UriBuilder(server)
                {
                    Port = port
                };

                client.BaseAddress = builder.Uri;
                client.DefaultRequestHeaders.UserAgent.Add
                (
                    new ProductInfoHeaderValue(name, version.ToString())
                );
            });

        configureClient?.Invoke(clientBuilder);
    }
}
