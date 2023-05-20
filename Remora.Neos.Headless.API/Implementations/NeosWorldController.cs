//
//  SPDX-FileName: NeosWorldController.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX.ProgramFlow;
using OneOf;
using Remora.Neos.Headless.API.Abstractions;
using Remora.Neos.Headless.API.Extensions;
using Remora.Results;

namespace Remora.Neos.Headless.API;

/// <summary>
/// Implements world control logic for the stock headless client.
/// </summary>
public abstract class NeosWorldController : INeosWorldController
{
    private readonly WorldManager _worldManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeosWorldController"/> class.
    /// </summary>
    /// <param name="worldManager">The world manager.</param>
    protected NeosWorldController(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    /// <inheritdoc />
    public abstract Task<Result<RestWorld>> StartWorldAsync
    (
        Uri? worldUrl = null,
        string? templateName = null,
        CancellationToken ct = default
    );

    /// <inheritdoc />
    public abstract Task<Result<RestWorld>> RestartWorldAsync(string worldId, CancellationToken ct = default);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RestWorld>>> GetWorldsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<Result<IReadOnlyList<RestWorld>>>
        (
            _worldManager.Worlds.Where(w => !w.IsUserspace()).Select(w => w.ToRestWorld()).ToArray()
        );
    }

    /// <inheritdoc />
    public Task<Result<RestWorld>> GetWorldAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        return world is null
            ? Task.FromResult<Result<RestWorld>>(new NotFoundError("No matching world found."))
            : Task.FromResult<Result<RestWorld>>(world.ToRestWorld());
    }

    /// <inheritdoc />
    public async Task<Result> SaveWorldAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return new NotFoundError("No matching world found.");
        }

        if (!Userspace.CanSave(world))
        {
            return new InvalidOperationError("The given world cannot be saved.");
        }

        await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);
        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public Task<Result> CloseWorldAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        world.Destroy();
        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result<RestWorld>> ModifyWorldAsync
    (
        string worldId,
        string? name = null,
        string? description = null,
        RestAccessLevel? accessLevel = null,
        float? awayKickInterval = null,
        bool? hideFromListing = null,
        int? maxUsers = null,
        CancellationToken ct = default
    )
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result<RestWorld>>(new NotFoundError("No matching world found."));
        }

        if (name is not null)
        {
            world.Name = name;
        }

        if (description is not null)
        {
            world.Description = description;
        }

        if (accessLevel is not null)
        {
            world.AccessLevel = accessLevel.Value.ToSessionAccessLevel();
        }

        if (awayKickInterval is not null)
        {
            if (awayKickInterval < 0.0)
            {
                return Task.FromResult<Result<RestWorld>>
                (
                    new ArgumentInvalidError
                    (
                        nameof(awayKickInterval),
                        "The away kick interval must be a positive value."
                    )
                );
            }

            world.AwayKickEnabled = awayKickInterval >= 0.0;
            world.AwayKickMinutes = awayKickInterval.Value;
        }

        if (hideFromListing is not null)
        {
            world.HideFromListing = hideFromListing.Value;
        }

        if (maxUsers is not null)
        {
            if (maxUsers is < 1 or > 256)
            {
                return Task.FromResult<Result<RestWorld>>
                (
                    new ArgumentInvalidError(nameof(maxUsers), "The maximum user count must be between 1 and 256.")
                );
            }

            world.MaxUsers = maxUsers.Value;
        }

        return Task.FromResult<Result<RestWorld>>(world.ToRestWorld());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RestUser>>> GetWorldUsersAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        return world is null
            ? Task.FromResult<Result<IReadOnlyList<RestUser>>>(new NotFoundError("No matching world found."))
            : Task.FromResult<Result<IReadOnlyList<RestUser>>>(world.AllUsers.Select(u => u.ToRestUser()).ToArray());
    }

    /// <inheritdoc />
    public Task<Result<RestUser>> GetWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result<RestUser>>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        return user is null
            ? Task.FromResult<Result<RestUser>>(new NotFoundError("No matching user found."))
            : Task.FromResult<Result<RestUser>>(user.ToRestUser());
    }

    /// <inheritdoc />
    public Task<Result> KickWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        if (user is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching user found."));
        }

        if (user.IsHost || !user.LocalUser.CanKick())
        {
            return Task.FromResult<Result>(new InvalidOperationError("You cannot kick that user."));
        }

        user.Kick();
        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result<RestBan>> BanWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result<RestBan>>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        if (user is null)
        {
            return Task.FromResult<Result<RestBan>>(new NotFoundError("No matching user found."));
        }

        if (user.IsHost || !user.LocalUser.CanBan())
        {
            return Task.FromResult<Result<RestBan>>(new InvalidOperationError("You cannot ban that user."));
        }

        user.Ban();
        return Task.FromResult<Result<RestBan>>(new RestBan(user.UserID, user.UserName, user.MachineID));
    }

    /// <inheritdoc />
    public Task<Result> SilenceUnsilenceWorldUserAsync
    (
        string worldId,
        string userIdOrName,
        bool isSilenced,
        CancellationToken ct = default
    )
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        if (user is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching user found."));
        }

        user.IsSilenced = isSilenced;
        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result> RespawnWorldUserAsync(string worldId, string userIdOrName, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        if (user is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching user found."));
        }

        user.Root?.Slot.Destroy();
        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result> SetWorldUserRoleAsync(string worldId, string userIdOrName, RestUserRole userRole, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        var user = world.AllUsers.FirstOrDefault
        (
            u => u.UserID == userIdOrName ||
                 u.UserName.Equals(userIdOrName, StringComparison.InvariantCultureIgnoreCase)
        );

        if (user is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching user found."));
        }

        var role = user.World.Permissions.Roles.FirstOrDefault<PermissionSet>
        (
            r => r.RoleName.Value.Equals(userRole.ToString(), StringComparison.InvariantCultureIgnoreCase)
        );

        if (role is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching role found."));
        }

        if (role > user.World.HostUser.Role)
        {
            return Task.FromResult<Result>
            (
                new InvalidOperationError("The role is greater than the hosting user's role.")
            );
        }

        user.Role = role;
        user.World.Permissions.AssignDefaultRole(user, role);

        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result<RestWorld>> GetFocusedWorldAsync(CancellationToken ct = default)
    {
        var world = _worldManager.FocusedWorld;

        return world is null
            ? Task.FromResult<Result<RestWorld>>(new NotFoundError("No world is focused."))
            : Task.FromResult<Result<RestWorld>>(world.ToRestWorld());
    }

    /// <inheritdoc />
    public Task<Result> FocusWorldAsync(string worldId, CancellationToken ct = default)
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        _worldManager.FocusWorld(world);

        return Task.FromResult(Result.FromSuccess());
    }

    /// <inheritdoc />
    public Task<Result> SendImpulseAsync
    (
        string worldId,
        string tag,
        OneOf<float, int, string>? value = null,
        CancellationToken ct = default
    )
    {
        var world = _worldManager.Worlds
            .Where(w => !w.IsUserspace())
            .FirstOrDefault(w => w.SessionId == worldId);

        if (world is null)
        {
            return Task.FromResult<Result>(new NotFoundError("No matching world found."));
        }

        switch (value)
        {
            case null:
            {
                var list = Pool.BorrowList<DynamicImpulseReceiver>();
                {
                    world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                    foreach (var dynamicImpulseReceiver in list)
                    {
                        dynamicImpulseReceiver.Impulse.Trigger();
                    }
                }
                Pool.Return(ref list);

                break;
            }
            default:
            {
                value.Value.Switch
                (
                    f =>
                    {
                        var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<float>>();
                        {
                            world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                            foreach (var dynamicImpulseReceiver in list)
                            {
                                dynamicImpulseReceiver.Trigger(f);
                            }
                        }
                        Pool.Return(ref list);
                    },
                    i =>
                    {
                        var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<int>>();
                        {
                            world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                            foreach (var dynamicImpulseReceiver in list)
                            {
                                dynamicImpulseReceiver.Trigger(i);
                            }
                        }
                        Pool.Return(ref list);
                    },
                    s =>
                    {
                        var list = Pool.BorrowList<DynamicImpulseReceiverWithValue<string>>();
                        {
                            world.RootSlot.GetComponentsInChildren(list, r => r.Tag.Evaluate() == tag);
                            foreach (var dynamicImpulseReceiver in list)
                            {
                                dynamicImpulseReceiver.Trigger(s);
                            }
                        }
                        Pool.Return(ref list);
                    }
                );

                break;
            }
        }

        return Task.FromResult(Result.FromSuccess());
    }
}
