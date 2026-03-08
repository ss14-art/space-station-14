using Content.Server.GameTicking;

namespace Content.Server._OpenSpace.Discord;

public sealed class DiscordAuthSystem : EntitySystem
{
    [Dependency] private readonly IDiscordOAuthManager _discordOAuth = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerConnectEvent>(OnPlayerConnection);
    }

    private async void OnPlayerConnection(PlayerConnectEvent args)
    {
        await _discordOAuth.GetRoles(args.PlayerSession.UserId.ToString());
    }
}