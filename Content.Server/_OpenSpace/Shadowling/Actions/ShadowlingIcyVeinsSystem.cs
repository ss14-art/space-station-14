using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Actions;
using Content.Server._OpenSpace.Shadowling;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingIcyVeinsSystem : EntitySystem
{
    private const float InjectAmount = 10f;
    private static readonly SoundSpecifier IcyVeinsSound =
        new SoundPathSpecifier("/Audio/_OpenSpace/Shadowling/icy_veins.ogg");

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingIcyVeinsComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingIcyVeinsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingIcyVeinsActionEvent>(OnAction);
    }

    private void OnInit(EntityUid uid, ShadowlingIcyVeinsComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingIcyVeinsComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingIcyVeinsActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        if (!TryComp<TransformComponent>(performer, out var xform))
            return;

        var range = _examine.GetExaminerRange(performer);
        var origin = _transform.GetMapCoordinates((performer, xform));

        EntityUid? closest = null;
        var closestDist = float.MaxValue;

        var targets = _lookup.GetEntitiesInRange(xform.Coordinates, range);
        foreach (var target in targets)
        {
            if (target == performer)
                continue;

            if (!TryComp<MobStateComponent>(target, out var mobState) || mobState.CurrentState == MobState.Dead)
                continue;

            if (HasComp<ShadowlingComponent>(target) || HasComp<ShadowlingThrallComponent>(target))
                continue;

            if (!_examine.InRangeUnOccluded(performer, target, range))
                continue;

            var targetCoords = _transform.GetMapCoordinates(target);
            var dist = (targetCoords.Position - origin.Position).Length();
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target;
            }
        }

        if (closest == null)
            return;

        if (!TryComp<BloodstreamComponent>(closest, out var bloodstream))
            return;

        if (!_solutions.TryGetSolution(closest.Value, bloodstream.BloodSolutionName, out var solutionEnt, out var solution))
            return;

        _solutions.TryAddReagent(solutionEnt.Value, "FrostOil", FixedPoint2.New(InjectAmount));

        SpawnAttachedTo("EffectShadowlingIcyVeins", performer.ToCoordinates());
        _audio.PlayGlobal(IcyVeinsSound, Filter.Pvs(performer), true);

        ev.Handled = true;
    }
}
