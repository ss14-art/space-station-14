using Content.Shared._OpenSpace.Effects;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace.Effects;

public sealed class OutlineFlashEffectSystem : SharedOutlineFlashEffectSystem
{
    public override void RaiseEffect(EntityUid target, EntityUid? viewer = null)
    {
        var ev = new OutlineFlashEffectEvent(GetNetEntity(target), viewer != null ? GetNetEntity(viewer.Value) : null);
        var filter = Filter.Pvs(Transform(target).Coordinates, entityMan: EntityManager);
        RaiseNetworkEvent(ev, filter);
    }
}
