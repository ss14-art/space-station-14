using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared._OpenSpace.Discord;
using Robust.Shared.Network;

namespace Content.Server._OpenSpace.Discord;

public sealed class DiscordAuthSystem : SharedDiscordAuthSystem
{
    [Dependency] private readonly IDiscordOAuthManager _discordOAuth = default!;
    [Dependency] private readonly INetManager _netMgr = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerConnectEvent>(OnPlayerConnection);
        SubscribeNetworkEvent<RequestDiscordRoles>(OnRolesRequest);
    }

    ConcurrentDictionary<string, HashSet<ulong>> _roles = [];

    private async void OnRolesRequest(RequestDiscordRoles args)
    {
        if (_roles.TryGetValue(args.Uuid, out var roles) && roles.Count > 0)
            args.Roles = roles;
        else
            args.Roles = await RequestRoles(args.Uuid);
    }

    private async void OnPlayerConnection(PlayerConnectEvent args)
    {
        var uuid = args.PlayerSession.UserId.ToString();
        await RequestRoles(uuid);
    }

    private async Task<HashSet<ulong>> RequestRoles(string uuid)
    {
        var playerRoles = await _discordOAuth.GetRoles(uuid);
        _roles[uuid] = playerRoles;

        return playerRoles;
    }
}