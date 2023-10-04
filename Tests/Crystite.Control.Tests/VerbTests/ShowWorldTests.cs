//
//  SPDX-FileName: ShowWorldTests.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Crystite.Control.Tests.TestBases;
using Crystite.Control.Verbs;
using Crystite.Control.Verbs.Bases;
using Remora.Rest.Xunit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Crystite.Control.Tests.VerbTests;

/// <summary>
/// Tests the <see cref="ShowWorld"/> verb.
/// </summary>
public class ShowWorldTests : VerbTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorldTests"/> class.
    /// </summary>
    /// <param name="fixture">The text fixture.</param>
    public ShowWorldTests(VerbTestFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Tests whether the verb displays the correct output.
    /// </summary>
    /// <param name="arguments">The arguments passed to the verb.</param>
    /// <param name="expected">The expected output, or null if it should match the payload.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(new[] { "-n", "SpaceWorld" }, new[] { "SpaceWorld\tS-9801807d-fd97-40de-a46a-aed4b5ab4a62\twith description!" })]
    [InlineData(new[] { "-v", "-n", "SpaceWorld" }, null)]
    [InlineData(new[] { "-i", "S-9801807d-fd97-40de-a46a-aed4b5ab4a62" }, new[] { "SpaceWorld\tS-9801807d-fd97-40de-a46a-aed4b5ab4a62\twith description!" })]
    [InlineData(new[] { "-v", "-i", "S-9801807d-fd97-40de-a46a-aed4b5ab4a62" }, null)]
    public async Task DisplaysCorrectOutput(string[] arguments, string[]? expected)
    {
        var args = new[] { "show-world" }.Concat(GetFixtureConnectionArguments()).Concat(arguments);
        var verb = (WorldVerb?)Program.ParseVerb(args);

        Assert.NotNull(verb);

        var allWorldsPayload = GetResponsePayload(Path.Combine("show_world", "running-worlds.json"));
        var worldPayload = GetResponsePayload(Path.Combine("show_world", "running-world.json"));

        ConfigureServer
        (
            server =>
            {
                if (verb.ID is null)
                {
                    // full lookup
                    server
                        .Expect(HttpMethod.Get, "http://xunit:1/worlds")
                        .Respond("application/json", allWorldsPayload);
                }
                else
                {
                    server
                        .Expect(HttpMethod.Get, $"http://xunit:1/world/{verb.ID}")
                        .Respond("application/json", worldPayload);
                }
            }
        );

        var result = await verb.ExecuteAsync(this.Services);
        ResultAssert.Successful(result);

        var output = GetOutput();
        if (expected is null)
        {
            var payloadDocument = JsonDocument.Parse(worldPayload);
            var outputDocument = JsonDocument.Parse(string.Join(Environment.NewLine, output));
            JsonAssert.Equivalent(payloadDocument, outputDocument);
        }
        else
        {
            Assert.Equal(expected, output);
        }
    }
}
