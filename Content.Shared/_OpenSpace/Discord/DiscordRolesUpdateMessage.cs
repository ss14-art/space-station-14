using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Discord;

public sealed class DiscordRolesUpdateMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public HashSet<ulong> Roles = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var capacity = buffer.ReadVariableInt32();
        Roles.EnsureCapacity(capacity);

        for (var i = 0; i < capacity; i++)
            Roles.Add(buffer.ReadUInt64());
    }
    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Roles.Count);
        foreach (var role in Roles)
            buffer.WriteVariableUInt64(role);
    }
}