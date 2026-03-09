using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared._OpenSpace.Discord;

namespace Content.Server._OpenSpace.Discord;

public sealed class DiscordAuthSystem : SharedDiscordAuthSystem
{
    [Dependency] private readonly IDiscordOAuthManager _discordOAuth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerConnectEvent>(OnPlayerConnection);
    }

    private async void OnPlayerConnection(PlayerConnectEvent args)
    {
        var uuid = args.PlayerSession.UserId.ToString();
        await RequestRoles(uuid);
    }

    public async Task<HashSet<ulong>> RequestRoles(string uuid)
    {
        var playerRoles = await _discordOAuth.GetRoles(uuid);

        return playerRoles;
    }
}