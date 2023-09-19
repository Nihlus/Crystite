//
//  SPDX-FileName: Program.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Remora.Neos.Control;
using Remora.Neos.Control.API;
using Remora.Neos.Control.Extensions;
using Remora.Neos.Control.Verbs;
using Remora.Neos.Control.Verbs.Bases;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Abstractions.Services;
using Remora.Rest;
using Remora.Rest.Extensions;
using Remora.Rest.Json;
using Remora.Rest.Json.Policies;
using Remora.Rest.Results;
using Remora.Results;

var options = Parser.Default.ParseArguments<ShowWorlds, ShowWorld, StartWorld>(args);
if (options.Errors.Any())
{
    return 1;
}

if (options.Value is not HeadlessVerb headlessVerb)
{
    throw new InvalidOperationException();
}

var serviceCollection = new ServiceCollection();

// configure JSON options
serviceCollection.Configure<JsonSerializerOptions>
(
    "Remora.Neos.Headless",
    o =>
    {
        o.PropertyNamingPolicy = new SnakeCaseNamingPolicy();

        o.Converters.Add(new StringEnumConverter<RestContactStatus>(o.PropertyNamingPolicy));
        o.Converters.Add(new StringEnumConverter<RestAccessLevel>(o.PropertyNamingPolicy));
        o.Converters.Add(new StringEnumConverter<RestUserRole>(o.PropertyNamingPolicy));
        o.Converters.Add(new StringEnumConverter<JobStatus>(o.PropertyNamingPolicy));

        o.AddDataObjectConverter<IRestBan, RestBan>();
        o.AddDataObjectConverter<IRestContact, RestContact>();
        o.AddDataObjectConverter<IRestUser, RestUser>();
        o.AddDataObjectConverter<IRestWorld, RestWorld>();
        o.AddDataObjectConverter<IJob, Job>();
    }
);

// add API services
serviceCollection.TryAddTransient(s => new HeadlessWorldAPI
(
    s.GetRequiredService<IRestHttpClient>(),
    s.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Remora.Neos.Headless")
));

// configure the HTTP client
_ = serviceCollection
    .AddRestHttpClient<APIError>("Remora.Neos.Headless")
    .ConfigureHttpClient((_, client) =>
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        var name = assemblyName.Name ?? "Remora.Neos.Control";
        var version = assemblyName.Version ?? new Version(1, 0, 0);

        var builder = new UriBuilder(headlessVerb.Server)
        {
            Port = headlessVerb.Port
        };

        client.BaseAddress = builder.Uri;
        client.DefaultRequestHeaders.UserAgent.Add
        (
            new ProductInfoHeaderValue(name, version.ToString())
        );
    });

var services = serviceCollection.BuildServiceProvider();

if (options.Value is not IExecutableVerb verb)
{
    throw new InvalidOperationException();
}

var result = await verb.ExecuteAsync(services);

if (result.IsSuccess)
{
    return 0;
}

Console.WriteLine(result.Error.Explain());
return 1;
