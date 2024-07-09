//
//  SPDX-FileName: TypeNameSerializationTests.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using Crystite.Patches.Generic;
using FrooxEngine;
using Xunit;

namespace Crystite.Tests;

/// <summary>
/// Tests the <see cref="UseSerializableFullName"/> patch's underlying functionality.
/// </summary>
public class TypeNameSerializationTests
{
    /// <summary>
    /// Tests whether the given type's name is correctly serialized.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="qualifyAssembly">Whether to fully quality the assembly.</param>
    /// <param name="expected">The expected output.</param>
    [Theory]
    [InlineData(typeof(string), false, "System.String")]
    [InlineData(typeof(string), true, "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [InlineData(typeof(ValueTextFormatDriver<float>), false, "FrooxEngine.ValueTextFormatDriver`1[[System.Single, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")]
    [InlineData(typeof(DynamicField<string>), false, "FrooxEngine.DynamicField`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")]
    public void SerializesNameCorrectly(Type type, bool qualifyAssembly, string expected)
    {
        var actual = type.GetSerializableFullName(qualifyAssembly);
        Assert.Equal(expected, actual);
    }
}
