using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Gravity;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._OpenSpace.Shadowling;

public sealed class ShadowlingShadowWalkSharedSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingShadowWalkPhasedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadowlingShadowWalkPhasedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingShadowWalkPhasedComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ShadowlingShadowWalkPhasedComponent, CanWeightlessMoveEvent>(OnCanWeightlessMove);
        SubscribeLocalEvent<ShadowlingShadowWalkPhasedComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<ShadowlingShadowWalkSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    private void OnInit(EntityUid uid, ShadowlingShadowWalkPhasedComponent component, ComponentInit args)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.SetCanCollide(uid, false, body: body);
    }

    private void OnShutdown(EntityUid uid, ShadowlingShadowWalkPhasedComponent component, ComponentShutdown args)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.SetCanCollide(uid, true, body: body);
    }

    private void OnPreventCollide(EntityUid uid, ShadowlingShadowWalkPhasedComponent component, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCanWeightlessMove(EntityUid uid, ShadowlingShadowWalkPhasedComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnIsWeightless(EntityUid uid, ShadowlingShadowWalkPhasedComponent component, ref IsWeightlessEvent args)
    {
        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ShadowlingShadowWalkSpeedComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedMultiplier);
    }
}
