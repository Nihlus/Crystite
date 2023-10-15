//
//  SPDX-FileName: KeyValueExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using SteamKit2;

namespace Crystite.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="KeyValue"/> class.
/// </summary>
public static class KeyValueExtensions
{
    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out KeyValue? value)
    {
        value = default;

        var child = keyValue.Children.SingleOrDefault(c => c.Name == key);
        if (child is null)
        {
            return false;
        }

        value = child;
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out byte? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsUnsignedByte();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out ushort? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsUnsignedShort();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out uint? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsUnsignedInteger();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out int? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsInteger();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out ulong? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsUnsignedLong();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out long? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsLong();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out string? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        var maybeValue = child.AsString();
        if (maybeValue is null)
        {
            return false;
        }

        value = maybeValue;
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out float? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsFloat();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet(this KeyValue keyValue, string key, [NotNullWhen(true)] out bool? value)
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsBoolean();
        return true;
    }

    /// <summary>
    /// Attempts to get the value of the given key inside the given <see cref="KeyValue"/>.
    /// </summary>
    /// <param name="keyValue">The <see cref="KeyValue"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, if any.</param>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <returns>true if the key was present; otherwise, false.</returns>
    public static bool TryGet<T>(this KeyValue keyValue, string key, [NotNullWhen(true)] out T? value)
        where T : struct, Enum
    {
        value = default;

        if (!keyValue.TryGet(key, out KeyValue? child))
        {
            return false;
        }

        value = child.AsEnum<T>();
        return true;
    }
}
