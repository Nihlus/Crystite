//
//  SPDX-FileName: WorldUserVerb.cs
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
public abstract class WorldUserVerb : WorldVerb
{
     /// <summary>
     /// Gets the name of the user.
     /// </summary>
     [Option('u', "user-name", Group = "USER_IDENTIFIER", HelpText = "The name of the user")]
     public string? UserName { get; }

     /// <summary>
     /// Gets the ID of the user.
     /// </summary>
     [Option("user-id", Group = "USER_IDENTIFIER", HelpText = "The ID of the user")]
     public string? UserID { get; }

     /// <summary>
     /// Initializes a new instance of the <see cref="WorldUserVerb"/> class.
     /// </summary>
     /// <param name="userName">The name of the user.</param>
     /// <param name="userID">The ID of the user.</param>
     /// <inheritdoc cref=".ctor(string, string, ushort, string, OutputFormat)" path="/param" />
     [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
     protected WorldUserVerb
     (
          string? userName,
          string? userID,
          string? worldName,
          string? worldID,
          ushort port,
          string server,
          OutputFormat outputFormat
     )
          : base(worldName, worldID, port, server, outputFormat)
     {
          this.UserName = userName;
          this.UserID = userID;
     }

     /// <summary>
     /// Gets the user targeted by the command.
     /// </summary>
     /// <param name="worldAPI">The world API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The world.</returns>
     protected async Task<Result<IRestUser>> GetTargetUserAsync
     (
          HeadlessWorldAPI worldAPI,
          CancellationToken ct = default
     )
     {
          var getWorldID = await GetTargetWorldIDAsync(worldAPI, ct);
          if (!getWorldID.IsDefined(out var worldID))
          {
               return Result<IRestUser>.FromError(getWorldID);
          }

          if (this.UserID is not null)
          {
               return await worldAPI.GetUserInWorldAsync(worldID, this.UserID, ct);
          }

          var getUsers = await worldAPI.GetUsersInWorldAsync(worldID, ct);
          if (!getUsers.IsDefined(out var users))
          {
               return Result<IRestUser>.FromError(getUsers);
          }

          var user = users.FirstOrDefault(w => w.Name.Equals(this.UserName, StringComparison.OrdinalIgnoreCase));
          return user is null
               ? new NotFoundError($"No user named \"{this.UserName}\" found")
               : Result<IRestUser>.FromSuccess(user);
     }

     /// <summary>
     /// Gets the ID of the user targeted by the command.
     /// </summary>
     /// <param name="worldAPI">The world API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The world.</returns>
     protected async ValueTask<Result<string>> GetTargetUserIDAsync
     (
          HeadlessWorldAPI worldAPI,
          CancellationToken ct = default
     )
     {
          if (this.UserID is not null)
          {
               return this.UserID;
          }

          var getWorldID = await GetTargetWorldIDAsync(worldAPI, ct);
          if (!getWorldID.IsDefined(out var worldID))
          {
               return Result<string>.FromError(getWorldID);
          }

          var getUsers = await worldAPI.GetUsersInWorldAsync(worldID, ct);
          if (!getUsers.IsDefined(out var users))
          {
               return Result<string>.FromError(getUsers);
          }

          var user = users.FirstOrDefault(w => w.Name.Equals(this.UserName, StringComparison.OrdinalIgnoreCase));
          return user is null
               ? new NotFoundError($"No user named \"{this.UserName}\" found")
               : Result<string>.FromSuccess(user.Id);
     }
}
