using Content.Shared._OpenSpace.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.Mobs.Components;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Actions;
using Content.Server._OpenSpace.Shadowling;
using Content.Shared.Speech.Muting;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingGlareSystem : EntitySystem
{
    private const float GlareRange = 4f;
    private const float InnerRange = 2f;
    private const float InnerFlashSeconds = 5f;
    private const float InnerStunSeconds = 3f;
    private const float OuterFlashSeconds = 2f;
    private const float OuterStunSeconds = 2f;
    private const float InnerMuteSeconds = 8f;
    private const float OuterMuteSeconds = 2f;
    private const float StaminaTickDamage = 18f;
    private const int StaminaTicks = 7;

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingGlareComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingGlareComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingGlareActionEvent>(OnAction);
    }

    private void OnInit(EntityUid uid, ShadowlingGlareComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingGlareComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingGlareActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;

        if (!TryComp<TransformComponent>(performer, out var xform))
            return;

        var origin = _transform.GetMapCoordinates((performer, xform));
        var targets = _lookup.GetEntitiesInRange(xform.Coordinates, GlareRange);
        foreach (var target in targets)
        {
            if (target == performer)
                continue;

            if (!HasComp<MobStateComponent>(target))
                continue;
            if (HasComp<ShadowlingComponent>(target))
                continue;
            if (HasComp<ShadowlingThrallComponent>(target))
                continue;

            if (!_examine.InRangeUnOccluded(performer, target, GlareRange))
                continue;

            var targetCoords = _transform.GetMapCoordinates(target);
            var distance = (targetCoords.Position - origin.Position).Length();

            if (distance <= InnerRange)
            {
                _flash.Flash(target, performer, performer, TimeSpan.FromSeconds(InnerFlashSeconds), 0.8f, true, stunDuration: TimeSpan.FromSeconds(InnerStunSeconds));
                EnsureComp<MutedComponent>(target);
                Timer.Spawn(TimeSpan.FromSeconds(InnerMuteSeconds), () =>
                {
                    if (Exists(target))
                        RemComp<MutedComponent>(target);
                });
                if (HasComp<StaminaComponent>(target))
                {
                    for (var i = 1; i <= StaminaTicks; i++)
                    {
                        var tick = i;
                        Timer.Spawn(TimeSpan.FromSeconds(tick), () =>
                        {
                            if (Deleted(target))
                                return;

                            _stamina.TakeStaminaDamage(target, StaminaTickDamage, ignoreResist: true);
                        });
                    }
                }
            }
            else if (distance <= GlareRange)
            {
                _flash.Flash(target, performer, performer, TimeSpan.FromSeconds(OuterFlashSeconds), 0.8f, true, stunDuration: TimeSpan.FromSeconds(OuterStunSeconds));
                EnsureComp<MutedComponent>(target);
                Timer.Spawn(TimeSpan.FromSeconds(OuterMuteSeconds), () =>
                {
                    if (Exists(target))
                        RemComp<MutedComponent>(target);
                });
            }
        }

        ev.Handled = true;
    }
}
