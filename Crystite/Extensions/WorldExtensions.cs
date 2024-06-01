//
//  SPDX-FileName: WorldExtensions.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using FrooxEngine;
using SkyFrost.Base;

namespace Crystite.Extensions;

/// <summary>
/// Defines extensions for the <see cref="World"/> class.
/// </summary>
public static class WorldExtensions
{
    /// <summary>
    /// Sets various startup parameters for the given world, potentially updating the startup parameter set.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="startupParameters">The startup parameters.</param>
    /// <param name="log">The logging instance to use.</param>
    /// <returns>The updated startup parameters.</returns>
    public static async Task<Configuration.WorldStartupParameters> SetParametersAsync
    (
        this World world,
        Configuration.WorldStartupParameters startupParameters,
        ILogger log
    )
    {
        if (startupParameters.SessionName is not null)
        {
            world.Name = startupParameters.SessionName;
        }

        if (startupParameters.Tags is not null)
        {
            world.Tags = startupParameters.Tags;
        }

        world.AccessLevel = startupParameters.AccessLevel;
        world.UseCustomJoinVerifier = startupParameters.UseCustomJoinVerifier;
        world.HideFromListing = startupParameters.HideFromPublicListing is true;
        world.MaxUsers = startupParameters.MaxUsers;
        world.MobileFriendly = startupParameters.MobileFriendly;
        world.Description = startupParameters.Description;
        world.ForceFullUpdateCycle = !startupParameters.AutoSleep;
        world.SaveOnExit = startupParameters.SaveOnExit;

        var correspondingWorldId = startupParameters.OverrideCorrespondingWorldID;
        if (correspondingWorldId is not null && correspondingWorldId.IsValid)
        {
            world.CorrespondingWorldId = correspondingWorldId.ToString();
        }

        if (startupParameters.AwayKickMinutes > 0.0)
        {
            world.AwayKickEnabled = true;
            world.AwayKickMinutes = (float)startupParameters.AwayKickMinutes;
        }

        world.SetCloudVariableParameters(startupParameters, log);
        world.ConfigureParentSessions(startupParameters, log);

        await world.ConfigurePermissionsAsync(startupParameters, log);
        await world.SendAutomaticInvitesAsync(startupParameters, log);

        startupParameters = await world.ConfigureSaveAsAsync(startupParameters, log);

        return startupParameters;
    }

    private static void ConfigureParentSessions(this World world, Configuration.WorldStartupParameters startupParameters, ILogger log)
    {
        if (startupParameters.ParentSessionIDs is null)
        {
            return;
        }

        var sessionIDs = new List<string>();
        foreach (var parentSessionID in startupParameters.ParentSessionIDs)
        {
            if (!SessionInfo.IsValidSessionId(parentSessionID))
            {
                log.LogWarning("Parent session ID {ID} is invalid", parentSessionID);
                continue;
            }

            log.LogInformation("Parent session ID: {ID}", parentSessionID);
            sessionIDs.Add(parentSessionID);
        }

        world.ParentSessionIds = sessionIDs;
    }

    private static async Task SendAutomaticInvitesAsync
    (
        this World world,
        Configuration.WorldStartupParameters startupParameters,
        ILogger log
    )
    {
        if (startupParameters.AutoInviteUsernames is null)
        {
            return;
        }

        if (world.Engine.Cloud.CurrentUser is null)
        {
            log.LogWarning("Not logged in, cannot send auto-invites");
            return;
        }

        foreach (var username in startupParameters.AutoInviteUsernames)
        {
            var contact = world.Engine.Cloud.Contacts.FindContact
            (
                f => f.ContactUsername.Equals(username, StringComparison.InvariantCultureIgnoreCase)
            );

            if (contact is null)
            {
                log.LogWarning("{Username} is not in the friends list, cannot auto-invite", username);
                continue;
            }

            var messages = world.Engine.Cloud.Messages.GetUserMessages(contact.ContactUserId);
            if (startupParameters.AutoInviteMessage is not null)
            {
                if (!await messages.SendTextMessage(startupParameters.AutoInviteMessage))
                {
                    log.LogWarning("Failed to send custom auto-invite message");
                }
            }

            world.AllowUserToJoin(contact.ContactUserId);
            var inviteMessage = await messages.CreateInviteMessage(world);
            if (!await messages.SendMessage(inviteMessage))
            {
                log.LogWarning("Failed to send auto-invite");
            }
            else
            {
                log.LogInformation("{Username} invited", username);
            }
        }
    }

    private static Task ConfigurePermissionsAsync
    (
        this World world,
        Configuration.WorldStartupParameters startupParameters,
        ILogger log
    )
    {
        return world.Coroutines.StartTask
        (
            static async args =>
            {
                await default(NextUpdate);
                if (!args.Startup.KeepOriginalRoles)
                {
                    args.World.Permissions.DefaultUserPermissions.Clear();
                }

                if (args.Startup.DefaultFriendRole is not null)
                {
                    var role = args.Startup.DefaultFriendRole;
                    var roleByName = args.World.Permissions.FindRoleByName(role);
                    if (roleByName is null)
                    {
                        args.Log.LogWarning("Role {Role} not available for world {World}", role, args.World.RawName);
                    }
                    else
                    {
                        var permissionSet = args.World.Permissions.FilterRole(roleByName);
                        if (permissionSet != roleByName)
                        {
                            args.Log.LogWarning
                            (
                                "Cannot use default role {DefaultRole} for {Role} because it's higher than the host role {HostRole} in world {World}",
                                roleByName.RoleName.Value,
                                role,
                                permissionSet.RoleName.Value,
                                args.World.RawName
                            );
                        }

                        args.World.Permissions.DefaultContactRole.ForceWrite(permissionSet);
                    }
                }

                if (args.Startup.DefaultAnonymousRole is not null)
                {
                    var role = args.Startup.DefaultAnonymousRole;
                    var roleByName = args.World.Permissions.FindRoleByName(role);
                    if (roleByName is null)
                    {
                        args.Log.LogWarning("Role {Role} not available for world {World}", role, args.World.RawName);
                    }
                    else
                    {
                        var permissionSet = args.World.Permissions.FilterRole(roleByName);
                        if (permissionSet != roleByName)
                        {
                            args.Log.LogWarning
                            (
                                "Cannot use default role {DefaultRole} for {Role} because it's higher than the host role {HostRole} in world {World}",
                                roleByName.RoleName.Value,
                                role,
                                permissionSet.RoleName.Value,
                                args.World.RawName
                            );
                        }

                        args.World.Permissions.DefaultAnonymousRole.ForceWrite(permissionSet);
                    }
                }

                if (args.Startup.DefaultVisitorRole is not null)
                {
                    var role = args.Startup.DefaultVisitorRole;
                    var roleByName = args.World.Permissions.FindRoleByName(role);
                    if (roleByName is null)
                    {
                        args.Log.LogWarning("Role {Role} not available for world {World}", role, args.World.RawName);
                    }
                    else
                    {
                        var permissionSet = args.World.Permissions.FilterRole(roleByName);
                        if (permissionSet != roleByName)
                        {
                            args.Log.LogWarning
                            (
                                "Cannot use default role {DefaultRole} for {Role} because it's higher than the host role {HostRole} in world {World}",
                                roleByName.RoleName.Value,
                                role,
                                permissionSet.RoleName.Value,
                                args.World.RawName
                            );
                        }

                        args.World.Permissions.DefaultVisitorRole.ForceWrite(permissionSet);
                    }
                }

                if (args.Startup.DefaultUserRoles is null)
                {
                    return;
                }

                foreach (var (user, role) in args.Startup.DefaultUserRoles)
                {
                    var userByName = await args.World.Engine.Cloud.Users.GetUserByName(user);
                    if (userByName.IsError)
                    {
                        args.Log.LogWarning("User {User} not found: {Reason}", user, userByName.State);
                        continue;
                    }

                    var roleByName = args.World.Permissions.FindRoleByName(role);
                    if (roleByName is null)
                    {
                        args.Log.LogWarning("Role {Role} not available for world {World}", role, args.World.RawName);
                        continue;
                    }

                    var permissionSet = args.World.Permissions.FilterRole(roleByName);
                    if (permissionSet != roleByName)
                    {
                        args.Log.LogWarning
                        (
                            "Cannot use default role {DefaultRole} for {Role} because it's higher than the host role {HostRole} in world {World}",
                            roleByName.RoleName.Value,
                            role,
                            permissionSet.RoleName.Value,
                            args.World.RawName
                        );
                    }

                    args.World.Permissions.DefaultUserPermissions.Remove(userByName.Entity.Id);
                    args.World.Permissions.DefaultUserPermissions.Add(userByName.Entity.Id, permissionSet);
                }
            },
            (World: world, Startup: startupParameters, Log: log)
        );
    }

    private static void SetCloudVariableParameters
    (
        this World world,
        Configuration.WorldStartupParameters startupParameters,
        ILogger log
    )
    {
        if (startupParameters.RoleCloudVariable is not null)
        {
            if (!CloudVariableHelper.IsValidPath(startupParameters.RoleCloudVariable))
            {
                log.LogWarning("Invalid RoleCloudVariable: {Variable}", startupParameters.RoleCloudVariable);
            }
            else
            {
                world.Permissions.DefaultRoleCloudVariable = startupParameters.RoleCloudVariable;
            }
        }

        if (startupParameters.AllowUserCloudVariable is not null)
        {
            if (!CloudVariableHelper.IsValidPath(startupParameters.AllowUserCloudVariable))
            {
                log.LogWarning("Invalid AllowUserCloudVariable: {Variable}", startupParameters.AllowUserCloudVariable);
            }
            else
            {
                world.AllowUserCloudVariable = startupParameters.AllowUserCloudVariable;
            }
        }

        if (startupParameters.DenyUserCloudVariable is not null)
        {
            if (!CloudVariableHelper.IsValidPath(startupParameters.DenyUserCloudVariable))
            {
                log.LogWarning("Invalid DenyUserCloudVariable: {Variable}", startupParameters.DenyUserCloudVariable);
            }
            else
            {
                world.DenyUserCloudVariable = startupParameters.DenyUserCloudVariable;
            }
        }

        if (startupParameters.RequiredUserJoinCloudVariable is not null)
        {
            if (!CloudVariableHelper.IsValidPath(startupParameters.RequiredUserJoinCloudVariable))
            {
                log.LogWarning
                    ("Invalid RequiredUserJoinCloudVariable: {Variable}", startupParameters.RequiredUserJoinCloudVariable);
            }
            else
            {
                world.RequiredUserJoinCloudVariable = startupParameters.RequiredUserJoinCloudVariable;
            }
        }

        if (startupParameters.RequiredUserJoinCloudVariableDenyMessage is not null)
        {
            world.RequiredUserJoinCloudVariableDenyMessage = startupParameters.RequiredUserJoinCloudVariableDenyMessage;
        }
    }

    private static async Task<Configuration.WorldStartupParameters> ConfigureSaveAsAsync
    (
        this World world,
        Configuration.WorldStartupParameters startupParameters,
        ILogger log
    )
    {
        string ownerID;
        switch (startupParameters.SaveAsOwner)
        {
            case SaveAsOwner.LocalMachine:
            {
                ownerID = $"M-{world.Engine.LocalDB.MachineID}";
                break;
            }
            case SaveAsOwner.CloudUser:
            {
                if (world.Engine.Cloud.CurrentUser is null)
                {
                    log.LogWarning("World is set to be saved under cloud user, but not user is logged in");
                    return startupParameters;
                }

                ownerID = world.Engine.Cloud.CurrentUser.Id;
                break;
            }
            case null:
            {
                return startupParameters;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        var record = world.CorrespondingRecord;
        if (record is null)
        {
            record = world.CreateNewRecord(ownerID);
            world.CorrespondingRecord = record;
        }
        else
        {
            record.OwnerId = ownerID;
            record.RecordId = RecordHelper.GenerateRecordID();
        }

        var transferer = new RecordOwnerTransferer(world.Engine, record.OwnerId, record.RecordId);
        log.LogInformation("Saving world under {SaveAs}", startupParameters.SaveAsOwner);

        var savedRecord = await Userspace.SaveWorld(world, record, transferer);
        log.LogInformation("Saved successfully");

        startupParameters = startupParameters with
        {
            SaveAsOwner = null,
            LoadWorldURL = new Uri(savedRecord.AssetURI)
        };

        return startupParameters;
    }
}
