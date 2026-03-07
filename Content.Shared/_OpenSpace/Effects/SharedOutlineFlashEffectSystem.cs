namespace Content.Shared._OpenSpace.Effects;

public abstract class SharedOutlineFlashEffectSystem : EntitySystem
{
    public abstract void RaiseEffect(EntityUid target, EntityUid? viewer = null);
}
