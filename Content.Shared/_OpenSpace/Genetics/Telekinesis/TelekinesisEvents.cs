using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Genetics.Telekinesis;

[Serializable, NetSerializable]
public sealed class TelekinesisStartRequestEvent : EntityEventArgs
{
    public TelekinesisStartRequestEvent(NetEntity target, MapCoordinates cursor)
    {
        Target = target;
        Cursor = cursor;
    }

    public NetEntity Target { get; }
    public MapCoordinates Cursor { get; }
}

[Serializable, NetSerializable]
public sealed class TelekinesisMoveRequestEvent : EntityEventArgs
{
    public TelekinesisMoveRequestEvent(MapCoordinates cursor)
    {
        Cursor = cursor;
    }

    public MapCoordinates Cursor { get; }
}

[Serializable, NetSerializable]
public sealed class TelekinesisStopRequestEvent : EntityEventArgs
{
    public TelekinesisStopRequestEvent(MapCoordinates cursor)
    {
        Cursor = cursor;
    }

    public MapCoordinates Cursor { get; }
}

