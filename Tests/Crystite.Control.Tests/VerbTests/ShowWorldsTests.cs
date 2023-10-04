//
//  SPDX-FileName: ShowWorldsTests.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Crystite.Control.API;
using Crystite.Control.Tests.TestBases;
using Crystite.Control.Verbs;
using Remora.Rest.Xunit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Crystite.Control.Tests.VerbTests;

/// <summary>
/// Tests the <see cref="ShowWorlds"/> verb.
/// </summary>
public class ShowWorldsTests : VerbTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWorldsTests"/> class.
    /// </summary>
    /// <param name="fixture">The text fixture.</param>
    public ShowWorldsTests(VerbTestFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Tests whether the verb displays the correct output.
    /// </summary>
    /// <param name="payloadFile">The name of the payload file to use.</param>
    /// <param name="arguments">The arguments passed to the verb.</param>
    /// <param name="expected">The expected output, or null if it should match the payload.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("no-running-worlds.json", new string[] { }, new string[] { })]
    [InlineData("no-running-worlds.json", new[] { "-v" }, null)]
    [InlineData("single-running-world.json", new string[] { }, new[] { "SpaceWorld\tS-9801807d-fd97-40de-a46a-aed4b5ab4a62" })]
    [InlineData("multiple-running-worlds.json", new string[] { }, new[] { "SpaceWorld\tS-9801807d-fd97-40de-a46a-aed4b5ab4a62", "OtherWorld\tS-84b4a57b-af87-4638-bcbb-4ec079c1bb4b\twith description!" })]
    [InlineData("single-running-world.json", new[] { "-v" }, null)]
    [InlineData("multiple-running-worlds.json", new[] { "-v" }, null)]
    public async Task DisplaysCorrectOutput(string payloadFile, string[] arguments, string[]? expected)
    {
        var payload = GetResponsePayload(Path.Combine("show_worlds", payloadFile));
        ConfigureServer
        (
            server => server
                .Expect(HttpMethod.Get, "http://xunit:1/worlds")
                .Respond("application/json", payload)
        );

        var args = new[] { "show-worlds" }.Concat(GetFixtureConnectionArguments()).Concat(arguments);
        var verb = Program.ParseVerb(args);

        Assert.NotNull(verb);

        var result = await verb.ExecuteAsync(this.Services);
        ResultAssert.Successful(result);

        var output = GetOutput();
        if (expected is null)
        {
            var payloadDocument = JsonDocument.Parse(payload);
            var outputDocument = JsonDocument.Parse(string.Join(Environment.NewLine, output));
            JsonAssert.Equivalent(payloadDocument, outputDocument);
        }
        else
        {
            Assert.Equal(expected, output);
        }
    }
}
