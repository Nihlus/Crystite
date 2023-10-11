//
//  SPDX-FileName: WorldVerb.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Remora.Results;

namespace Crystite.Control.Verbs.Bases;

/// <summary>
/// Represents a base class for world-related verbs.
/// </summary>
public abstract class WorldVerb : HeadlessVerb
{
     /// <summary>
     /// Gets the name of the world.
     /// </summary>
     [Option('n', "name", Group = "WORLD_IDENTIFIER", HelpText = "The name of the world")]
     public string? Name { get; }

     /// <summary>
     /// Gets the ID of the world.
     /// </summary>
     [Option('i', "id", Group = "WORLD_IDENTIFIER", HelpText = "The ID of the world")]
     public string? ID { get; }

     /// <summary>
     /// Initializes a new instance of the <see cref="WorldVerb"/> class.
     /// </summary>
     /// <param name="name">The name of the world.</param>
     /// <param name="id">The ID of the world.</param>
     /// <inheritdoc cref="HeadlessVerb(ushort, string, OutputFormat)" path="/param" />
     [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
     protected WorldVerb(string? name, string? id, ushort port, string server, OutputFormat outputFormat)
          : base(port, server, outputFormat)
     {
          this.Name = name;
          this.ID = id;
     }

     /// <summary>
     /// Gets the world targeted by the command.
     /// </summary>
     /// <param name="worldAPI">The world API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The world.</returns>
     protected async Task<Result<IRestWorld>> GetTargetWorldAsync
     (
          HeadlessWorldAPI worldAPI,
          CancellationToken ct = default
     )
     {
          if (this.ID is not null)
          {
               return await worldAPI.GetWorldAsync(this.ID, ct);
          }

          var getWorlds = await worldAPI.GetWorldsAsync(ct);
          if (!getWorlds.IsDefined(out var worlds))
          {
               return Result<IRestWorld>.FromError(getWorlds);
          }

          var world = worlds.FirstOrDefault(w => w.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));
          return world is null
               ? new NotFoundError($"No world named \"{this.Name}\" found")
               : Result<IRestWorld>.FromSuccess(world);
     }

     /// <summary>
     /// Gets the ID of the world targeted by the command.
     /// </summary>
     /// <param name="worldAPI">The world API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The world.</returns>
     protected async ValueTask<Result<string>> GetTargetWorldIDAsync
     (
          HeadlessWorldAPI worldAPI,
          CancellationToken ct = default
     )
     {
          if (this.ID is not null)
          {
               return this.ID;
          }

          var getWorlds = await worldAPI.GetWorldsAsync(ct);
          if (!getWorlds.IsDefined(out var worlds))
          {
               return Result<string>.FromError(getWorlds);
          }

          var world = worlds.FirstOrDefault(w => w.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));
          return world is null
               ? new NotFoundError($"No world named \"{this.Name}\" found")
               : world.Id;
     }
}
