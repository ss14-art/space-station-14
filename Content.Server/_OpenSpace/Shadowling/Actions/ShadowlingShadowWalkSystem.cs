using Content.Shared._OpenSpace.Actions;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Timing;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Coordinates;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Maths;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingShadowWalkSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGodmodeSystem _godmode = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingShadowWalkComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingShadowWalkComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingShadowWalkActionEvent>(OnAction);
        SubscribeLocalEvent<ShadowlingPlaneShiftComponent, ComponentStartup>(OnPlaneShiftInit);
        SubscribeLocalEvent<ShadowlingPlaneShiftComponent, ComponentShutdown>(OnPlaneShiftShutdown);
        SubscribeLocalEvent<ShadowlingPlaneShiftActionEvent>(OnPlaneShiftAction);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ShadowlingShadowWalkActiveComponent, ThrowAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<ActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadowlingShadowWalkActiveComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (_timing.CurTime >= active.EndTime)
            {
                RemComp<ShadowlingShadowWalkActiveComponent>(uid);
                _movement.RefreshMovementSpeedModifiers(uid);
            }
        }
    }

    private void OnInit(EntityUid uid, ShadowlingShadowWalkComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingShadowWalkComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnPlaneShiftInit(EntityUid uid, ShadowlingPlaneShiftComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnPlaneShiftShutdown(EntityUid uid, ShadowlingPlaneShiftComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingShadowWalkActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        if (!TryComp<ShadowlingShadowWalkComponent>(performer, out var walk))
            return;

        if (HasComp<ShadowlingShadowWalkActiveComponent>(performer))
            return;

        if (!TryStartShadowWalk(performer, walk.SpeedMultiplier, walk.Duration, null))
            return;

        ev.Handled = true;
    }

    private void OnPlaneShiftAction(ShadowlingPlaneShiftActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        if (!TryComp<ShadowlingPlaneShiftComponent>(performer, out var planeShift))
            return;

        if (HasComp<ShadowlingShadowWalkActiveComponent>(performer))
        {
            RemComp<ShadowlingShadowWalkActiveComponent>(performer);
            _movement.RefreshMovementSpeedModifiers(performer);
            ev.Handled = true;
            return;
        }

        if (!TryStartShadowWalk(performer, planeShift.SpeedMultiplier, null, ev.Action.Owner))
            return;

        ev.Handled = true;
    }

    private bool TryStartShadowWalk(EntityUid performer, float speedMultiplier, TimeSpan? duration, EntityUid? allowedAction)
    {
        var enter = Spawn("EffectShadowlingShadowWalkEnter", Transform(performer).Coordinates);
        if (TryComp<TransformComponent>(performer, out var performerXform))
        {
            var angle = performerXform.LocalRotation.GetCardinalDir().ToAngle();
            _transform.SetLocalRotation(enter, angle);
        }

        var active = EnsureComp<ShadowlingShadowWalkActiveComponent>(performer);
        active.EndTime = duration != null ? _timing.CurTime + duration.Value : TimeSpan.MaxValue;
        active.SpeedMultiplier = speedMultiplier;
        active.AllowedAction = allowedAction;

        active.HadGodmode = HasComp<GodmodeComponent>(performer);
        if (!active.HadGodmode)
            _godmode.EnableGodmode(performer);

        StealthComponent? stealthComp;
        active.HadStealth = TryComp(performer, out stealthComp);
        if (!active.HadStealth)
        {
            EnsureComp<StealthComponent>(performer);
            if (!TryComp<StealthComponent>(performer, out stealthComp))
                return false;
        }

        if (stealthComp == null)
        {
            RemComp<ShadowlingShadowWalkActiveComponent>(performer);
            _movement.RefreshMovementSpeedModifiers(performer);
            return false;
        }

        active.PreviousStealthEnabled = stealthComp.Enabled;
        active.PreviousStealthVisibility = _stealth.GetVisibility(performer, stealthComp);
        _stealth.SetEnabled(performer, true, stealthComp);
        _stealth.SetVisibility(performer, -1f, stealthComp);

        EnsureComp<ShadowlingShadowWalkPhasedComponent>(performer);
        var invis = EnsureComp<ShadowlingShadowWalkInvisibleComponent>(performer);
        invis.IsVisible = false;
        Dirty(performer, invis);
        EnsureComp<ShadowlingShadowWalkSpeedComponent>(performer).SpeedMultiplier = speedMultiplier;

        _movement.RefreshMovementSpeedModifiers(performer);

        if (duration != null)
        {
            var d = duration.Value;
            Timer.Spawn(d, () =>
            {
                if (Deleted(performer))
                    return;
                RemComp<ShadowlingShadowWalkActiveComponent>(performer);
                _movement.RefreshMovementSpeedModifiers(performer);
            });
        }

        return true;
    }

    private void OnActiveShutdown(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ComponentShutdown args)
    {
        RemComp<ShadowlingShadowWalkPhasedComponent>(uid);
        if (TryComp<ShadowlingShadowWalkInvisibleComponent>(uid, out var invis))
        {
            invis.IsVisible = true;
            Dirty(uid, invis);
            RemComp<ShadowlingShadowWalkInvisibleComponent>(uid);
        }
        RemComp<ShadowlingShadowWalkSpeedComponent>(uid);
        component.AllowedAction = null;

        SpawnAttachedTo("EffectShadowlingShadowWalkExit", uid.ToCoordinates());

        if (!component.HadGodmode)
            _godmode.DisableGodmode(uid);

        if (component.HadStealth)
        {
            if (TryComp<StealthComponent>(uid, out var stealthComp))
            {
                _stealth.SetEnabled(uid, component.PreviousStealthEnabled, stealthComp);
                _stealth.SetVisibility(uid, component.PreviousStealthVisibility, stealthComp);
            }
        }
        else
        {
            RemComp<StealthComponent>(uid);
        }

        TryMoveOutOfWall(uid);

        if (TryComp<GravityAffectedComponent>(uid, out var gravity))
            _gravity.RefreshWeightless((uid, gravity));

        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnInteractionAttempt(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUseAttempt(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnDropAttempt(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref DropAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref PickupAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnThrowAttempt(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnActionAttempt(EntityUid uid, ActionComponent component, ref ActionAttemptEvent args)
    {
        if (!TryComp<ShadowlingShadowWalkActiveComponent>(args.User, out var active))
            return;

        if (active.AllowedAction == null || active.AllowedAction.Value != uid)
            args.Cancelled = true;
    }


    private void OnRefreshMoveSpeed(EntityUid uid, ShadowlingShadowWalkActiveComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedMultiplier);
    }

    private void TryMoveOutOfWall(EntityUid uid)
    {
        if (_physics.GetEntitiesIntersectingBody(uid, (int)CollisionGroup.Impassable).Count == 0)
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        if (!_turf.TryGetTileRef(xform.Coordinates, out var tileRef))
            return;

        var gridUid = tileRef.Value.GridUid;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var origin = tileRef.Value.GridIndices;
        var currentMap = _transform.ToMapCoordinates(xform.Coordinates);
        const int maxRadius = 6;
        TileRef? bestTile = null;
        var bestDistSq = float.MaxValue;

        for (var dx = -maxRadius; dx <= maxRadius; dx++)
        {
            for (var dy = -maxRadius; dy <= maxRadius; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var idx = origin + new Vector2i(dx, dy);
                if (!_mapSystem.TryGetTileRef(gridUid, grid, idx, out var candidate))
                    continue;

                if (_turf.IsTileBlocked(candidate, CollisionGroup.Impassable))
                    continue;

                var coords = _turf.GetTileCenter(candidate);
                var candidateMap = _transform.ToMapCoordinates(coords);
                var distSq = (candidateMap.Position - currentMap.Position).LengthSquared();
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                bestTile = candidate;
            }
        }

        if (bestTile.HasValue)
            _transform.SetCoordinates(uid, _turf.GetTileCenter(bestTile.Value));
    }
}
