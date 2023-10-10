//
//  SPDX-FileName: UserVerb.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Crystite.API.Abstractions;
using Crystite.Control.API;
using Crystite.Control.Verbs.Bases;
using Remora.Results;

namespace Crystite.Control.Verbs.Users.Bases;

/// <summary>
/// Represents a base class for user-related verbs.
/// </summary>
public abstract class UserVerb : HeadlessVerb
{
     /// <summary>
     /// Gets the name of the user.
     /// </summary>
     [Option('n', "name", Group = "USER_IDENTIFIER", HelpText = "The name of the user")]
     public string? Name { get; }

     /// <summary>
     /// Gets the ID of the user.
     /// </summary>
     [Option('i', "id", Group = "USER_IDENTIFIER", HelpText = "The ID of the user")]
     public string? ID { get; }

     /// <summary>
     /// Initializes a new instance of the <see cref="UserVerb"/> class.
     /// </summary>
     /// <param name="name">The name of the user.</param>
     /// <param name="id">The ID of the user.</param>
     /// <inheritdoc cref=".ctor(ushort, string, OutputFormat)" path="/param" />
     [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
     protected UserVerb
     (
          string? name,
          string? id,
          ushort port,
          string server,
          OutputFormat outputFormat
     )
          : base(port, server, outputFormat)
     {
          this.Name = name;
          this.ID = id;
     }

     /// <summary>
     /// Gets the user targeted by the command.
     /// </summary>
     /// <param name="userAPI">The user API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The user.</returns>
     protected async Task<Result<IRestUser>> GetTargetUserAsync
     (
          HeadlessUserAPI userAPI,
          CancellationToken ct = default
     )
     {
          var identifier = this.ID ?? this.Name ?? throw new InvalidOperationException();
          var getUser = await userAPI.GetUserAsync(identifier, ct);

          return getUser.IsDefined(out var user)
               ? Result<IRestUser>.FromSuccess(user)
               : new NotFoundError($"No user identified by \"{identifier}\" found");
     }

     /// <summary>
     /// Gets the ID of the user targeted by the command.
     /// </summary>
     /// <param name="userAPI">The user API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The user.</returns>
     protected async ValueTask<Result<string>> GetTargetUserIDAsync
     (
          HeadlessUserAPI userAPI,
          CancellationToken ct = default
     )
     {
          var identifier = this.ID ?? this.Name ?? throw new InvalidOperationException();
          var getUser = await userAPI.GetUserAsync(identifier, ct);

          return getUser.IsDefined(out var user)
               ? Result<string>.FromSuccess(user.Id)
               : new NotFoundError($"No user identified by \"{identifier}\" found");
     }
}
