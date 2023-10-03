//
//  SPDX-FileName: VerbTestBase.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Crystite.Control.API;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;
using StreamWriter = System.IO.StreamWriter;

#pragma warning disable SA1402

namespace Crystite.Control.Tests.TestBases;

/// <summary>
/// Serves as a base class for REST API tests.
/// </summary>
[Collection("Verb tests")]
public abstract class VerbTestBase : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly MemoryStream _outputStream;

    /// <summary>
    /// Gets the services available to the test.
    /// </summary>
    protected IServiceProvider Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerbTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The test fixture.</param>
    protected VerbTestBase(VerbTestFixture fixture)
    {
        _mockHandler = new();
        _outputStream = new();

        var serviceCollection = new ServiceCollection()
            .AddSingleton<TextWriter>(new StreamWriter(_outputStream, Encoding.UTF8))
            .AddSingleton(fixture);

        Program.ConfigureHeadlessAPIServices(1, "xunit", serviceCollection, b => b.ConfigurePrimaryHttpMessageHandler
        (
            _ => _mockHandler
        ));

        serviceCollection.AddSingleton<IOptionsFactory<JsonSerializerOptions>, OptionsFactory<JsonSerializerOptions>>();
        Decorate<IOptionsFactory<JsonSerializerOptions>, CachedOptionsFactory>(serviceCollection);

        this.Services = serviceCollection.BuildServiceProvider(true);
    }

    /// <summary>
    /// Creates a configured, mocked API instance.
    /// </summary>
    /// <typeparam name="TAPI">The type of the API class to configure and create.</typeparam>
    /// <param name="builder">The mock builder.</param>
    /// <returns>The API instance.</returns>
    protected TAPI ConfigureAPI<TAPI>(Action<MockHttpMessageHandler> builder) where TAPI : AbstractHeadlessRestAPI
    {
        builder(_mockHandler);
        return this.Services.GetRequiredService<TAPI>();
    }

    /// <summary>
    /// Gets the current total output from the program.
    /// </summary>
    /// <returns>The output as a set of lines.</returns>
    protected IReadOnlyList<string> GetOutput()
    {
        var writer = this.Services.GetRequiredService<TextWriter>();
        writer.Flush();

        var outputPosition = _outputStream.Position;
        _outputStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(_outputStream, Encoding.UTF8, leaveOpen: true);
        var value = reader.ReadToEnd();

        _outputStream.Seek(outputPosition, SeekOrigin.Begin);
        var lines = value.Split(Environment.NewLine);

        // if the last line is empty, strip it out
        return string.IsNullOrWhiteSpace(lines[^1]) ? lines[..^1] : lines;
    }

    /// <summary>
    /// Gets the contents of an embedded response payload.
    /// </summary>
    /// <param name="filename">The filename of the payload.</param>
    /// <returns>The contents of the payload.</returns>
    // ReSharper disable once Html.PathError
    protected string GetResponsePayload([PathReference("~/Data")] string filename)
    {
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream
            (
                $"Crystite.Control.Tests.Data.{filename}".Replace(Path.DirectorySeparatorChar, '.')
            );

        if (stream is null)
        {
            throw new InvalidOperationException();
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Registers a decorator service, replacing the existing interface.
    /// </summary>
    /// <remarks>
    /// Implementation based off of
    /// https://greatrexpectations.com/2018/10/25/decorators-in-net-core-with-dependency-injection/.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="TInterface">The interface type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    public static void Decorate<TInterface, TDecorator>(IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.First(s => s.ServiceType == typeof(TInterface));

        var objectFactory = ActivatorUtilities.CreateFactory(typeof(TDecorator), new[] { typeof(TInterface) });
        services.Replace(ServiceDescriptor.Describe
        (
            typeof(TInterface),
            s => (TInterface)objectFactory(s, new[] { CreateInstance(s, wrappedDescriptor) }),
            wrappedDescriptor.Lifetime
        ));
    }

    private static object CreateInstance(IServiceProvider services, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        return descriptor.ImplementationFactory is not null
            ? descriptor.ImplementationFactory(services)
            : ActivatorUtilities.GetServiceOrCreateInstance
            (
                services,
                descriptor.ImplementationType ?? throw new InvalidOperationException()
            );
    }

    private class CachedOptionsFactory : IOptionsFactory<JsonSerializerOptions>
    {
        private readonly IOptionsFactory<JsonSerializerOptions> _actual;
        private readonly VerbTestFixture _fixture;

        public CachedOptionsFactory(IOptionsFactory<JsonSerializerOptions> actual, VerbTestFixture fixture)
        {
            _fixture = fixture;
            _actual = actual;
        }

        public JsonSerializerOptions Create(string name)
        {
            return name is "Crystite"
                ? _fixture.Options
                : _actual.Create(name);
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        _mockHandler.Dispose();
        _outputStream.Dispose();
    }
}

/// <summary>
/// Acts as a test fixture for REST API tests.
/// </summary>
public class VerbTestFixture
{
    /// <summary>
    /// Gets a set of JSON serializer options.
    /// </summary>
    public JsonSerializerOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerbTestFixture"/> class.
    /// </summary>
    public VerbTestFixture()
    {
        var serviceCollection = new ServiceCollection();
        Program.ConfigureJsonSerializerOptions(serviceCollection);

        var services = serviceCollection.BuildServiceProvider();

        this.Options = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>()
            .Get("Crystite");
    }
}

/// <summary>
/// Defines a test collection for JSON-backed type tests.
/// </summary>
[CollectionDefinition("Verb tests")]
public class VerbTestCollection : ICollectionFixture<VerbTestFixture>
{
}
