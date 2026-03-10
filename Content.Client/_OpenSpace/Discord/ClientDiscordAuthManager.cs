using System.Collections.Immutable;
using System.Linq;
using Content.Client.Popups;
using Content.Shared._OpenSpace.Discord;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client._OpenSpace;

public sealed class ClientDiscordOAuthManager : IClientDiscordOAuthManager
{
    [Dependency] private readonly INetManager _netMgr = default!;
    [Dependency] private readonly IUriOpener _uri = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    private ImmutableHashSet<ulong> _roles = [];

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<DiscordLinkResponseMessage>(OnLinkReceived);
        _netMgr.RegisterNetMessage<DiscordRolesUpdateMessage>(OnRolesUpdate);
    }

    private void OnLinkReceived(DiscordLinkResponseMessage msg)
    {
        if (!msg.AlreadyRegistered)
            _uri.OpenUri(msg.Link);
        else
            _entity.System<PopupSystem>().PopupCursor("You already registered!!!!!", Shared.Popups.PopupType.LargeCaution);
    }

    private void OnRolesUpdate(DiscordRolesUpdateMessage msg)
        => _roles = [.. msg.Roles];

    public void RequestLink()
        => _netMgr.ClientSendMessage(new DiscordLinkRequestMessage());

    public bool ContainsAny(ulong[] roles)
        => roles.Any(_roles.Contains);
}