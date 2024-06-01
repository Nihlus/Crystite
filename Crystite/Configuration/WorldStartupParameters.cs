//
//  SPDX-FileName: WorldStartupParameters.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Text.Json.Serialization;
using SkyFrost.Base;

namespace Crystite.Configuration;

/// <summary>
/// These options are used to configure a startup word from a JSON configuration file.
/// </summary>
/// <remarks>
/// The base schema has been extended with some more properties, enabling further configuration.
/// </remarks>
/// <a href="https://raw.githubusercontent.com/Neos-Metaverse/JSONSchemas/main/schemas/NeosHeadlessConfig.schema.json"/>
/// <param name="IsEnabled">When set to false, this will disable this world entry from starting.</param>
/// <param name="SessionName">The name of the session as shown in the World/Session Browser.</param>
/// <param name="CustomSessionID">An optional custom session id for this session.</param>
/// <param name="Description">An optional description of this session, displayed within the world/session browser.</param>
/// <param name="MaxUsers">The maximum number of users, allowed to join this session.</param>
/// <param name="AccessLevel">The access level for this session. Please see <see cref="SessionAccessLevel"/> for more information.</param>
/// <param name="UseCustomJoinVerifier">When set to true, the VerifyJoinRequest ProtoFlux Node will be used in place of default session access rules.</param>
/// <param name="HideFromPublicListing">Determines if this session should be hidden from the world/session browser or not.</param>
/// <param name="Tags">A list of tags, to assist with searching or discovering sessions.</param>
/// <param name="MobileFriendly">Determines if this session is friendly for mobile/quest users.</param>
/// <param name="LoadWorldURL">When provided with a world URL this will load this world for the session.</param>
/// <param name="LoadWorldPresetName">When provided and valid, this will load the specified world preset into the session.</param>
/// <param name="OverrideCorrespondingWorldID">Overrides the world id for this session allowing it to be grouped and displayed with other sessions with this world id.</param>
/// <param name="ForcePort">Optional, If specified it will force this session to run on a specific network port.</param>
/// <param name="KeepOriginalRoles">Optional, If specified will keep the original roles as saved in the world.</param>
/// <param name="DefaultFriendRole">The default role to grant to joining friends.</param>
/// <param name="DefaultAnonymousRole">The default role to grant to joining anonymous users.</param>
/// <param name="DefaultVisitorRole">The default role to grant to joining visitors.</param>
/// <param name="DefaultUserRoles">When provided with a list of username and permission pairs it will grant those users the listed permissions when they join.</param>
/// <param name="RoleCloudVariable">An optional name of a cloud variable to use to determine the role of this user.</param>
/// <param name="AllowUserCloudVariable">An optional name of a cloud variable to use to determine if this user is allowed in the session.</param>
/// <param name="DenyUserCloudVariable">An optional name of a cloud variable to use to determine if this user is denied access to the session.</param>
/// <param name="RequiredUserJoinCloudVariable">An optional name of a cloud variable to use to determine if this user is granted access to the session.</param>
/// <param name="RequiredUserJoinCloudVariableDenyMessage">The name of a cloud variable whose value will be used, When a user is denied access by RequiredUserJoinCloudVariable this message will be displayed to them if present.</param>
/// <param name="AwayKickMinutes">Configures the number of minutes that a user can be away(shelled out) from a world before they are kicked. Setting this to -1 disables this option.</param>
/// <param name="ParentSessionIDs">Provides a list of Parent Session Ids for this session.</param>
/// <param name="AutoInviteUsernames">Users within this list will automatically be invited to this world when it starts.</param>
/// <param name="AutoInviteMessage">An automatic message sent to the users on the <paramref name="AutoInviteUsernames"/> list along with their invite.</param>
/// <param name="SaveAsOwner">Controls who saves this world when it is saved. See <see cref="SaveAsOwner"/> for more information.</param>
/// <param name="AutoRecover">Headless only(?).</param>
/// <param name="IdleRestartInterval">If this is set(&gt;0) and the world is empty, it will restart regularly using the value to determine the number of seconds between restarts.</param>
/// <param name="ForcedRestartInterval">If this is set(&gt;0), it will restart regularly using the value to determine the number of seconds between restarts.</param>
/// <param name="SaveOnExit">If set to true will save this world on exit.</param>
/// <param name="AutosaveInterval">If this is set(&gt;0), it will automatically save using the value to determine the number of seconds between saves.</param>
/// <param name="AutoSleep">If set to true, will prevent an empty(or filled with away users) world from running a full update cycle regularly.</param>
public record WorldStartupParameters
(
    bool IsEnabled = true,
    string? SessionName = null,
    string? CustomSessionID = null,
    string? Description = null,
    int MaxUsers = 32,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SessionAccessLevel AccessLevel = SessionAccessLevel.Private,
    bool UseCustomJoinVerifier = false,
    bool? HideFromPublicListing = null,
    IReadOnlyList<string>? Tags = null,
    bool MobileFriendly = false,
    Uri? LoadWorldURL = null,
    string? LoadWorldPresetName = null,
    RecordId? OverrideCorrespondingWorldID = null,
    ushort? ForcePort = null,
    bool KeepOriginalRoles = false,
    string? DefaultFriendRole = null,
    string? DefaultAnonymousRole = null,
    string? DefaultVisitorRole = null,
    IReadOnlyDictionary<string, string>? DefaultUserRoles = null,
    string? RoleCloudVariable = null,
    string? AllowUserCloudVariable = null,
    string? DenyUserCloudVariable = null,
    string? RequiredUserJoinCloudVariable = null,
    string? RequiredUserJoinCloudVariableDenyMessage = null,
    double AwayKickMinutes = -1,
    IReadOnlyList<string>? ParentSessionIDs = null,
    IReadOnlyList<string>? AutoInviteUsernames = null,
    string? AutoInviteMessage = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SaveAsOwner? SaveAsOwner = null,
    bool AutoRecover = true,
    double IdleRestartInterval = -1,
    double ForcedRestartInterval = -1,
    bool SaveOnExit = false,
    double AutosaveInterval = -1,
    bool AutoSleep = true
)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldStartupParameters"/> class.
    /// </summary>
    public WorldStartupParameters()
        : this(IsEnabled: true) // force overload resolution
    {
    }
}
