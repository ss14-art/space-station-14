using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Discord;

public abstract class SharedDiscordAuthSystem : EntitySystem
{
    public HashSet<ulong> TryGetRoles(ICommonSession session)
    {
        var @event = new RequestDiscordRoles(session.UserId.ToString());
        RaiseNetworkEvent(@event);

        return @event.Roles;
    }
}

[Serializable, NetSerializable]
public sealed class RequestDiscordRoles(string uuid) : EntityEventArgs
{
    public string Uuid = uuid;

    public HashSet<ulong> Roles = [];
}