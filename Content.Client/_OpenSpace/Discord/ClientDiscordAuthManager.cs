using Content.Shared._OpenSpace.Discord;
using Robust.Shared.Network;

namespace Content.Client._OpenSpace;

public sealed class ClientDiscordOAuthManager : IClientDiscordOAuthManager
{
    [Dependency] private readonly INetManager _netMgr = default!;

    public event Action<string>? LinkReceived;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<DiscordLinkResponseMessage>(OnLinkReceived);
    }

    private void OnLinkReceived(DiscordLinkResponseMessage msg)
        => LinkReceived?.Invoke(msg.Link);

    public void RequestLink()
        => _netMgr.ClientSendMessage(new DiscordLinkRequestMessage());
}