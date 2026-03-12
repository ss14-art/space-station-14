using System.Linq;
using Content.Server._OpenSpace;
using Content.Shared._NullLink;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._NullLink.PlayerData;

public sealed class PlayerRolesReqManager : SharedPlayerRolesReqManager
{
    //[Dependency] private readonly INullLinkPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IDiscordOAuthManager _discordOAuthManager = default!;

    public override bool IsAllRolesAvailable(EntityUid uid)
        => _player.TryGetSessionByEntity(uid, out var session)
            && AllRoles is not null
            && _discordOAuthManager.TryGetRoles(session, out var roles) // OpenSpace
            && AllRoles.Roles.Any(roles.Contains); // OpenSpace

    public override bool IsAllRolesAvailable(ICommonSession session)
        =>  AllRoles is not null
            && _discordOAuthManager.TryGetRoles(session, out var roles) // OpenSpace
            && AllRoles.Roles.Any(roles.Contains);

    public override bool IsAnyRole(ICommonSession session, ulong[] roles)
        => AllRoles is not null
            && _discordOAuthManager.TryGetRoles(session, out var userRoles) // OpenSpace
            && roles.Any(userRoles.Contains);
    public override bool IsMentor(EntityUid uid)
        => _player.TryGetSessionByEntity(uid, out var session)
            && _mentorReq is not null
            && _discordOAuthManager.TryGetRoles(session, out var roles) // OpenSpace
            && _mentorReq.Roles.Any(roles.Contains);
    public override bool IsMentor(ICommonSession session)
        =>  _mentorReq is not null
            && _discordOAuthManager.TryGetRoles(session, out var roles) // OpenSpace
            && _mentorReq.Roles.Any(roles.Contains);
    public override bool IsPeacefulBypass(EntityUid uid)
        => _player.TryGetSessionByEntity(uid, out var session)
            && _peacefulBypass is not null
            && _discordOAuthManager.TryGetRoles(session, out var roles) // OpenSpace
            && _peacefulBypass.Roles.Any(roles.Contains);
}