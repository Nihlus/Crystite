//
//  SPDX-FileName: WorldVerb.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace Crystite.Control.Verbs.Bases;

/// <summary>
/// Represents a base class for world-related verbs.
/// </summary>
public abstract class WorldVerb : HeadlessVerb
{
     /// <summary>
     /// Gets the name of the world.
     /// </summary>
     [Option('n', "name", Required = true, SetName = "WORLD_NAME", HelpText = "The name of the world")]
     public string? Name { get; }

     /// <summary>
     /// Gets the ID of the world.
     /// </summary>
     [Option('i', "id", Required = true, SetName = "WORLD_IDENTIFIER", HelpText = "The ID of the world")]
     public string? ID { get; }

     /// <summary>
     /// Initializes a new instance of the <see cref="WorldVerb"/> class.
     /// </summary>
     /// <param name="name">The name of the world. Mutually exclusive with <paramref name="id"/>.</param>
     /// <param name="id">The ID of the world. Mutually exclusive with <paramref name="name"/>.</param>
     /// <inheritdoc cref="HeadlessVerb(ushort, string, bool)" path="/param" />
     [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
     protected WorldVerb(string? name, string? id, ushort port, string server, bool verbose)
          : base(port, server, verbose)
     {
          this.Name = name;
          this.ID = id;
     }
}
