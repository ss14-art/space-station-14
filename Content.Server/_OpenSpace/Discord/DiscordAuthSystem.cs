using Content.Server.GameTicking;
using Content.Shared._OpenSpace.Discord;

namespace Content.Server._OpenSpace.Discord;

public sealed class DiscordAuthSystem : EntitySystem
{
    [Dependency] private readonly IDiscordOAuthManager _discordOAuth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerConnectEvent>(OnPlayerConnection);
    }

    private async void OnPlayerConnection(PlayerConnectEvent args) => await _discordOAuth.GetRoles(args.PlayerSession);
}