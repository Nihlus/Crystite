//
//  SPDX-FileName: NameValueListExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Specialized;
using System.Linq.Expressions;
using Remora.Rest.Core;

#pragma warning disable SA1402

namespace Crystite.Control.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="NameValueCollection"/> class.
/// </summary>
public static class NameValueListExtensions
{
    /// <summary>
    /// Adds an optional value to the collection. If the optional does not contain a value, the collection is not
    /// modified.
    /// </summary>
    /// <remarks>If the contained value is null, the literal string "null" is added to the collection.</remarks>
    /// <param name="collection">The collection.</param>
    /// <param name="name">The name of the value to add.</param>
    /// <param name="value">The value.</param>
    /// <typeparam name="T">The type of the contained value.</typeparam>
    public static void Add<T>(this List<KeyValuePair<string, string>> collection, string name, Optional<T> value) where T : class
    {
        if (!value.HasValue)
        {
            return;
        }

        collection.Add(new KeyValuePair<string, string>(name, value.Value.ToString() ?? "null"));
    }
}

/// <summary>
/// Defines struct type-overloaded extension methods for the <see cref="NameValueCollection"/> class.
/// </summary>
public static class NameValueCollectionStructExtensions
{
    private static Func<TEnum, int> GenerateFunc<TEnum>()
        where TEnum : struct
    {
        var inputParameter = Expression.Parameter(typeof(TEnum));

        var body = Expression.Convert(inputParameter, typeof(int)); // means: (int)input;

        var lambda = Expression.Lambda<Func<TEnum, int>>(body, inputParameter);

        var func = lambda.Compile();
        return func;
    }

    /// <summary>
    /// Adds an optional value to the collection. If the optional does not contain a value, the collection is not
    /// modified.
    /// </summary>
    /// <remarks>If the contained value is null, the literal string "null" is added to the collection.</remarks>
    /// <param name="collection">The collection.</param>
    /// <param name="name">The name of the value to add.</param>
    /// <param name="value">The value.</param>
    /// <typeparam name="T">The type of the contained value.</typeparam>
    public static void Add<T>(this List<KeyValuePair<string, string>> collection, string name, Optional<T> value)
        where T : struct
    {
        if (!value.HasValue)
        {
            return;
        }

        var stringified = typeof(T).IsEnum
            ? GenerateFunc<T>()(value.Value).ToString()
            : value.Value.ToString();

        collection.Add(new(name, stringified ?? "null"));
    }
}
