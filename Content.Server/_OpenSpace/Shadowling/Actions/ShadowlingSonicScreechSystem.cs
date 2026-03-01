using Content.Server._OpenSpace.Shadowling;
using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Maths;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server._Starlight.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingSonicScreechSystem : EntitySystem
{
    private const float ScreechRange = 2.5f;
    private static readonly SoundSpecifier ScreechSound =
        new SoundPathSpecifier("/Audio/_OpenSpace/Shadowling/screech.ogg");

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SLMoverController _mover = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingSonicScreechComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingSonicScreechActionEvent>(OnAction);
    }

    private void OnShutdown(EntityUid uid, ShadowlingSonicScreechComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingSonicScreechActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;

        if (HasComp<ShadowlingAscendantComponent>(performer))
            return;

        _audio.PlayPvs(ScreechSound, performer);

        foreach (var target in _lookup.GetEntitiesInRange(performer, ScreechRange))
        {
            if (target == performer)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(target))
                continue;

            if (HasComp<ShadowlingComponent>(target) || HasComp<ShadowlingThrallComponent>(target))
                continue;

            if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;

            _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(1));

            if (!TryComp<InputMoverComponent>(target, out var mover))
                continue;

            var originalRotation = mover.TargetRelativeRotation;
            var delta = _random.Prob(0.5f) ? Direction.East.ToAngle() : Direction.West.ToAngle();
            _mover.RotateCamera(target, delta);

            if (!TryComp<ShadowlingCameraLockComponent>(target, out var lockComp))
            {
                lockComp = EnsureComp<ShadowlingCameraLockComponent>(target);
                lockComp.Locked = true;
                lockComp.OriginalRotation = originalRotation;
                Dirty(target, lockComp);
            }

            _status.TryAddStatusEffectDuration(target, "StatusEffectShadowlingCameraLock", TimeSpan.FromSeconds(10));
        }

        _popup.PopupClient(Loc.GetString("shadowling-action-sonic-screech"), performer, performer);
        ev.Handled = true;
    }
}
