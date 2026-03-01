using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingCameraLockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingCameraLockStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    private void OnStatusRemoved(EntityUid uid, ShadowlingCameraLockStatusEffectComponent component, StatusEffectRemovedEvent args)
    {
        var target = args.Target;

        if (!TryComp<ShadowlingCameraLockComponent>(target, out var lockComp))
            return;

        if (TryComp<InputMoverComponent>(target, out var mover))
        {
            mover.TargetRelativeRotation = lockComp.OriginalRotation;
            mover.RelativeRotation = lockComp.OriginalRotation;
            Dirty(target, mover);
        }

        RemComp<ShadowlingCameraLockComponent>(target);
    }
}
