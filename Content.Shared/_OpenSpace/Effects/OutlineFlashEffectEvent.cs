using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Effects;

[Serializable, NetSerializable]
public sealed class OutlineFlashEffectEvent : EntityEventArgs
{
    public NetEntity Target;
    public NetEntity? Viewer;

    public OutlineFlashEffectEvent(NetEntity target, NetEntity? viewer)
    {
        Target = target;
        Viewer = viewer;
    }
}
