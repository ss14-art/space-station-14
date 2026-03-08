using Content.Shared._OpenSpace.Discord;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client._OpenSpace;

public sealed class ClientDiscordOAuthManager : IClientDiscordOAuthManager
{
    [Dependency] private readonly INetManager _netMgr = default!;
    [Dependency] private readonly IUriOpener _uri = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<DiscordLinkResponseMessage>(OnLinkReceived);
    }

    private void OnLinkReceived(DiscordLinkResponseMessage msg)
        => _uri.OpenUri(msg.Link);

    public void RequestLink()
        => _netMgr.ClientSendMessage(new DiscordLinkRequestMessage());
}