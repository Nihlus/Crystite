//
//  SPDX-FileName: ContactVerb.cs
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
/// Represents a base class for contact-related verbs.
/// </summary>
public abstract class ContactVerb : HeadlessVerb
{
     /// <summary>
     /// Gets the name of the contact.
     /// </summary>
     [Option('n', "name", Group = "CONTACT_IDENTIFIER", HelpText = "The name of the contact")]
     public string? Name { get; }

     /// <summary>
     /// Gets the ID of the contact.
     /// </summary>
     [Option('i', "id", Group = "CONTACT_IDENTIFIER", HelpText = "The ID of the contact")]
     public string? ID { get; }

     /// <summary>
     /// Initializes a new instance of the <see cref="ContactVerb"/> class.
     /// </summary>
     /// <param name="name">The name of the contact.</param>
     /// <param name="id">The ID of the contact.</param>
     /// <inheritdoc cref=".ctor(ushort, string, OutputFormat)" path="/param" />
     [SuppressMessage("Documentation", "CS1573", Justification = "Copied from base class")]
     protected ContactVerb
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
     /// Gets the contact targeted by the command.
     /// </summary>
     /// <param name="contactAPI">The contact API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The contact.</returns>
     protected async Task<Result<IRestContact>> GetTargetContactAsync
     (
          HeadlessContactAPI contactAPI,
          CancellationToken ct = default
     )
     {
          if (this.ID is not null)
          {
               return await contactAPI.GetContactAsync(this.ID, ct);
          }

          var getContacts = await contactAPI.GetContactsAsync(ct);
          if (!getContacts.IsDefined(out var contacts))
          {
               return Result<IRestContact>.FromError(getContacts);
          }

          var contact = contacts.FirstOrDefault
          (
               w => w.Username.Equals(this.Name, StringComparison.OrdinalIgnoreCase)
          );

          return contact is null
               ? new NotFoundError($"No contact named \"{this.Name}\" found")
               : Result<IRestContact>.FromSuccess(contact);
     }

     /// <summary>
     /// Gets the ID of the contact targeted by the command.
     /// </summary>
     /// <param name="contactAPI">The contact API.</param>
     /// <param name="ct">The cancellation token for this operation.</param>
     /// <returns>The contact.</returns>
     protected async ValueTask<Result<string>> GetTargetContactIDAsync
     (
          HeadlessContactAPI contactAPI,
          CancellationToken ct = default
     )
     {
          if (this.ID is not null)
          {
               return this.ID;
          }

          var getContacts = await contactAPI.GetContactsAsync(ct);
          if (!getContacts.IsDefined(out var contacts))
          {
               return Result<string>.FromError(getContacts);
          }

          var contact = contacts.FirstOrDefault
          (
               w => w.Username.Equals(this.Name, StringComparison.OrdinalIgnoreCase)
          );

          return contact is null
               ? new NotFoundError($"No contact named \"{this.Name}\" found")
               : contact.Id;
     }
}
