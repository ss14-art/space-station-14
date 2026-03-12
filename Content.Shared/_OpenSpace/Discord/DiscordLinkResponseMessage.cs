using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Discord;

public sealed class DiscordLinkResponseMessage : NetMessage
{
    public override MsgGroups MsgGroup =>  MsgGroups.Core;

    public string Link = "";

    public bool AlreadyRegistered = false;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Link = buffer.ReadString();
        AlreadyRegistered = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Link);
        buffer.Write(AlreadyRegistered);
    }
}