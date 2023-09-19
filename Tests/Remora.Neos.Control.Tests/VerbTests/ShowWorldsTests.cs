//
//  SPDX-FileName: ShowWorldsTests.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Net.Http;
using System.Threading.Tasks;
using Remora.Neos.Control.API;
using Remora.Neos.Control.Tests.TestBases;
using Remora.Neos.Control.Verbs;
using RichardSzalay.MockHttp;
using Xunit;

namespace Remora.Neos.Control.Tests.VerbTests;

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
    /// Tests whether the verb correctly shows the resulting worlds.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CanShowWorlds()
    {
        ConfigureAPI<HeadlessWorldAPI>
        (
            api => api
                .Expect(HttpMethod.Get, "http://xunit:1/worlds")
                .Respond("application/json", "[ ]")
        );

        var args = new[] { "show-worlds" };
        var verb = Program.ParseVerb(args);

        Assert.NotNull(verb);

        var result = await verb.ExecuteAsync(this.Services);
        ResultAssert.Successful(result);
    }
}
