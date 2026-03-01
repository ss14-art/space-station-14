using System.Threading;
using Content.Server._OpenSpace.Shadowling;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;
using Content.Shared.Gibbing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingAnnihilateSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingAscendantComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingAscendantComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingAnnihilateActionEvent>(OnAction);
    }

    private void OnInit(EntityUid uid, ShadowlingAscendantComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingAscendantComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingAnnihilateActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        var target = ev.Target;

        if (!EntityManager.EntityExists(target))
            return;

        if (!IsValidTarget(performer, target))
            return;

        var coords = _transform.GetMapCoordinates(target);
        Timer.Spawn(_timing.TickPeriod, () =>
            _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId, 4, 1, 2, target, maxTileBreak: 0),
            CancellationToken.None);

        _gibbing.Gib(target);
        ev.Handled = true;
    }

    private bool IsValidTarget(EntityUid performer, EntityUid target)
    {
        if (target == performer)
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (HasComp<ShadowlingComponent>(target))
            return false;

        return true;
    }
}
