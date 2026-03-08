using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared._OpenSpace.Effects;
using Content.Shared._OpenSpace.Movement.Pulling.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Speech.Muting;
using Content.Shared.Verbs;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed class PullingSystem : EntitySystem
{
    private static readonly TimeSpan BreakAttemptCooldown = TimeSpan.FromSeconds(0.5);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifierSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly HeldSpeedModifierSystem _clothingMoveSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtual = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedOutlineFlashEffectSystem _outlineFlash = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var chokedQuery = EntityQueryEnumerator<ChokedComponent>();
        while (chokedQuery.MoveNext(out var target, out var choked))
        {
            if (choked.Puller is not { } puller)
                continue;

            if (_netMan.IsClient)
            {
                if (Transform(target).ParentUid != puller)
                    _transform.SetParent(target, puller);
                _transform.SetLocalPositionNoLerp(target, choked.Offset);
                _transform.SetWorldRotation(target, choked.LockedWorldRotation);
                continue;
            }

            if (!_netMan.IsServer)
                continue;

            _transform.SetWorldRotation(target, choked.LockedWorldRotation);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PullableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<PullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);
        SubscribeLocalEvent<PullableComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<PullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);
        SubscribeLocalEvent<PullableComponent, EntGotInsertedIntoContainerMessage>(OnPullableContainerInsert);
        SubscribeLocalEvent<PullableComponent, ModifyUncuffDurationEvent>(OnModifyUncuffDuration);
        SubscribeLocalEvent<PullableComponent, StopBeingPulledAlertEvent>(OnStopBeingPulledAlert);
        SubscribeLocalEvent<PullableComponent, GetInteractingEntitiesEvent>(OnGetInteractingEntities);
        SubscribeLocalEvent<PullableComponent, UpdateCanMoveEvent>(OnPullableUpdateCanMove);
        SubscribeLocalEvent<ChokedComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<ChokedComponent, ComponentShutdown>(OnChokedShutdown);
        SubscribeLocalEvent<ChokedComponent, AttackAttemptEvent>(OnChokedAttackAttempt);
        SubscribeLocalEvent<PullerComponent, AttackAttemptEvent>(OnPullerAttackAttempt);
        SubscribeLocalEvent<PullableComponent, DragDropDraggedEvent>(OnPullableDragDropGrab);
        SubscribeLocalEvent<ClimbableComponent, InteractUsingEvent>(OnClimbableInteractUsing);
        SubscribeLocalEvent<PullerComponent, UserInteractUsingEvent>(OnPullerInteractUsing);
        SubscribeLocalEvent<PullerComponent, UserInteractHandEvent>(OnPullerInteractHand);

        SubscribeLocalEvent<PullerComponent, MobStateChangedEvent>(OnStateChanged, after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<PullerComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<PullerComponent, EntGotInsertedIntoContainerMessage>(OnPullerContainerInsert);
        SubscribeLocalEvent<PullerComponent, EntityUnpausedEvent>(OnPullerUnpaused);
        SubscribeLocalEvent<PullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<PullerComponent, DropHandItemsEvent>(OnDropHandItems);
        SubscribeLocalEvent<PullerComponent, StopPullingAlertEvent>(OnStopPullingAlert);
        SubscribeLocalEvent<PullerComponent, MoveEvent>(OnPullerMove);

        SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
        SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

        SubscribeLocalEvent<BuckleComponent, BuckleAttemptEvent>(OnBuckleAttempt);

        SubscribeLocalEvent<PullableComponent, StrappedEvent>(OnBuckled);
        SubscribeLocalEvent<PullableComponent, BuckledEvent>(OnGotBuckled);
        SubscribeLocalEvent<ActivePullerComponent, TargetHandcuffedEvent>(OnTargetHandcuffed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(OnReleasePulledObject, handle: false))
            .Register<PullingSystem>();
    }

    private void OnTargetHandcuffed(Entity<ActivePullerComponent> ent, ref TargetHandcuffedEvent args)
    {
        if (!TryComp<PullerComponent>(ent, out var comp))
            return;

        if (comp.Pulling == null)
            return;

        if (CanPull(ent, comp.Pulling.Value, comp))
            return;

        if (!TryComp<PullableComponent>(comp.Pulling, out var pullableComp))
            return;

        TryStopPull(comp.Pulling.Value, pullableComp);
    }

    private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        if (TryComp(args.PullerUid, out PullerComponent? pullerComp) && !pullerComp.NeedsHands)
            return;

        if (!_virtual.TrySpawnVirtualItemInHand(args.PulledUid, uid))
        {
            DebugTools.Assert("Unable to find available hand when starting pulling??");
        }
    }

    private void HandlePullStopped(EntityUid uid, HandsComponent component, PullStoppedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        // Try find hand that is doing this pull.
        // and clear it.
        foreach (var held in _handsSystem.EnumerateHeld((uid, component)))
        {
            if (!TryComp(held, out VirtualItemComponent? virtualItem) || virtualItem.BlockingEntity != args.PulledUid)
                continue;

            _handsSystem.TryDrop((args.PullerUid, component), held);
            break;
        }
    }

    private void OnStateChanged(EntityUid uid, PullerComponent component, ref MobStateChangedEvent args)
    {
        if (component.Pulling == null)
            return;

        if (TryComp<PullableComponent>(component.Pulling, out var comp) && (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead))
        {
            TryStopPull(component.Pulling.Value, comp);
        }
    }

    private void OnBuckled(Entity<PullableComponent> ent, ref StrappedEvent args)
    {
        // Prevent people from pulling the entity they are buckled to
        if (ent.Comp.Puller == args.Buckle.Owner && !args.Buckle.Comp.PullStrap)
            StopPulling(ent, ent);
    }

    private void OnGotBuckled(Entity<PullableComponent> ent, ref BuckledEvent args)
    {
        StopPulling(ent, ent);
    }

    private void OnBuckleAttempt(Entity<BuckleComponent> ent, ref BuckleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var target = args.Buckle.Owner;

        if (HasComp<ChokedComponent>(target))
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<PullableComponent>(target, out var pullable) || pullable.Puller == null)
            return;

        if (!TryComp<PullerComponent>(pullable.Puller, out var puller))
            return;

        if (puller.GrabStage is GrabStage.Medium or GrabStage.Heavy or GrabStage.Choke)
            args.Cancelled = true;
    }

    private void OnGetInteractingEntities(Entity<PullableComponent> ent, ref GetInteractingEntitiesEvent args)
    {
        if (ent.Comp.Puller != null)
            args.InteractingEntities.Add(ent.Comp.Puller.Value);
    }

    private void OnAfterState(Entity<PullerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Pulling == null)
            RemComp<ActivePullerComponent>(ent.Owner);
        else
            EnsureComp<ActivePullerComponent>(ent.Owner);
    }

    private void OnDropHandItems(EntityUid uid, PullerComponent pullerComp, DropHandItemsEvent args)
    {
        if (pullerComp.Pulling == null || pullerComp.NeedsHands)
            return;

        if (!TryComp(pullerComp.Pulling, out PullableComponent? pullableComp))
            return;

        TryStopPull(pullerComp.Pulling.Value, pullableComp, uid);
    }

    private void OnStopPullingAlert(Entity<PullerComponent> ent, ref StopPullingAlertEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<PullableComponent>(ent.Comp.Pulling, out var pullable))
            return;
        args.Handled = TryStopPull(ent.Comp.Pulling.Value, pullable, ent);
    }

    private void OnPullerMove(EntityUid uid, PullerComponent component, ref MoveEvent args)
    {
        if (!_netMan.IsServer)
            return;

        if (component.GrabStage != GrabStage.Choke || component.Pulling is not { } target)
            return;

        if (!TryComp<ChokedComponent>(target, out var choked) || choked.Puller != uid)
            return;

        var pullerMap = _transform.GetMapCoordinates(uid);
        _transform.SetMapCoordinates(target,
            new MapCoordinates(pullerMap.Position + choked.Offset, pullerMap.MapId));
    }

    private void OnPullerContainerInsert(Entity<PullerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Pulling == null)
            return;

        if (!TryComp(ent.Comp.Pulling.Value, out PullableComponent? pulling))
            return;

        TryStopPull(ent.Comp.Pulling.Value, pulling, ent.Owner);
    }

    private void OnPullableContainerInsert(Entity<PullableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        TryStopPull(ent.Owner, ent.Comp);
    }

    private void OnModifyUncuffDuration(Entity<PullableComponent> ent, ref ModifyUncuffDurationEvent args)
    {
        if (!ent.Comp.BeingPulled)
            return;

        // We don't care if the person is being uncuffed by someone else
        if (args.User != args.Target)
            return;

        args.Duration *= 2;
    }

    private void OnStopBeingPulledAlert(Entity<PullableComponent> ent, ref StopBeingPulledAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStopPull(ent, ent, ent);
    }

    private void OnStandUpAttempt(Entity<ChokedComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnPullableUpdateCanMove(EntityUid uid, PullableComponent component, ref UpdateCanMoveEvent args)
    {
        if (!component.BeingPulled || component.Puller == null)
            return;

        if (component.PullerGrabStage is GrabStage.Medium or GrabStage.Heavy or GrabStage.Choke)
            args.Cancel();
    }

    private void OnChokedShutdown(Entity<ChokedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Puller is { } puller)
            _virtual.DeleteInHandsMatching(puller, ent.Owner);
    }

    private void OnChokedAttackAttempt(Entity<ChokedComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPullableDragDropGrab(EntityUid uid, PullableComponent component, ref DragDropDraggedEvent args)
    {
        if (!TryComp<PullerComponent>(args.User, out var pullerComp) ||
            pullerComp.Pulling is not { } pulled ||
            pulled != uid ||
            pullerComp.GrabStage is GrabStage.None)
        {
            return;
        }

        if (!TryComp<ClimbableComponent>(args.Target, out _))
            return;

        if (!TryStopPull(pulled, component, args.User))
            return;

        _transform.SetCoordinates(pulled, _transform.GetMoverCoordinates(args.Target));
        _stun.TryUpdateParalyzeDuration(pulled, TimeSpan.FromSeconds(4));
        args.Handled = true;
    }

    private void OnClimbableInteractUsing(EntityUid uid, ClimbableComponent component, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PullerComponent>(args.User, out var pullerComp) ||
            pullerComp.Pulling is not { } pulled ||
            pullerComp.GrabStage is GrabStage.None)
        {
            return;
        }

        if (args.Used == uid)
            return;

        if (!TryComp<VirtualItemComponent>(args.Used, out var virt) || virt.BlockingEntity != pulled)
            return;

        if (!TryComp<PullableComponent>(pulled, out var pullableComp))
            return;

        if (!TryStopPull(pulled, pullableComp, args.User))
            return;

        _climbSystem.ForciblySetClimbing(pulled, uid);
        _stun.TryUpdateParalyzeDuration(pulled, TimeSpan.FromSeconds(4));
        args.Handled = true;
    }

    private void OnPullerInteractUsing(EntityUid uid, PullerComponent component, ref UserInteractUsingEvent args)
    {
        if (component.Pulling is not { } pulled ||
            component.GrabStage is GrabStage.None)
        {
            return;
        }

        if (!TryComp<ClimbableComponent>(args.Target, out _))
            return;

        if (!HasPullVirtualInHands(uid, pulled))
            return;

        if (!TryComp<PullableComponent>(pulled, out var pullableComp))
            return;

        if (!TryStopPull(pulled, pullableComp, uid))
            return;

        _transform.SetCoordinates(pulled, _transform.GetMoverCoordinates(args.Target));
        _stun.TryUpdateParalyzeDuration(pulled, TimeSpan.FromSeconds(4));
        args.Handled = true;
    }

    private void OnPullerInteractHand(EntityUid uid, PullerComponent component, ref UserInteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.Pulling is not { } pulled ||
            component.GrabStage is GrabStage.None)
        {
            return;
        }

        if (!TryComp<ClimbableComponent>(args.Target, out _))
            return;

        if (!HasPullVirtualInHands(uid, pulled))
            return;

        if (!TryComp<PullableComponent>(pulled, out var pullableComp))
            return;

        if (!TryStopPull(pulled, pullableComp, uid))
            return;

        _transform.SetCoordinates(pulled, _transform.GetMoverCoordinates(args.Target));
        _stun.TryUpdateParalyzeDuration(pulled, TimeSpan.FromSeconds(4));
        args.Handled = true;
    }

    private void OnPullerAttackAttempt(EntityUid uid, PullerComponent component, ref AttackAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (component.Pulling is not { } pulled ||
            component.GrabStage is GrabStage.None)
        {
            return;
        }

        if (args.Target is not { } target || !TryComp<ClimbableComponent>(target, out _))
            return;

        if (!HasPullVirtualInHands(uid, pulled))
            return;

        if (!TryComp<PullableComponent>(pulled, out var pullableComp))
            return;

        if (!TryStopPull(pulled, pullableComp, uid))
            return;

        _transform.SetCoordinates(pulled, _transform.GetMoverCoordinates(target));
        _stun.TryUpdateParalyzeDuration(pulled, TimeSpan.FromSeconds(4));
        args.Cancel();
    }

    private bool HasPullVirtualInHands(EntityUid user, EntityUid pulled)
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return false;

        foreach (var held in _handsSystem.EnumerateHeld((user, hands)))
        {
            if (TryComp<VirtualItemComponent>(held, out var virt) && virt.BlockingEntity == pulled)
                return true;
        }

        return false;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PullingSystem>();
    }

    private void OnPullerUnpaused(EntityUid uid, PullerComponent component, ref EntityUnpausedEvent args)
    {
        component.NextThrow += args.PausedTime;
    }

    private void OnVirtualItemDeleted(EntityUid uid, PullerComponent component, VirtualItemDeletedEvent args)
    {
        // If client deletes the virtual hand then stop the pull.
        if (component.Pulling == null)
            return;

        if (component.Pulling != args.BlockingEntity)
            return;

        if (TryComp(args.BlockingEntity, out PullableComponent? comp))
        {
            TryStopPull(args.BlockingEntity, comp);
        }
    }

    private void AddPullVerbs(EntityUid uid, PullableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Are they trying to pull themselves up by their bootstraps?
        if (args.User == args.Target)
            return;

        //TODO VERB ICONS add pulling icon
        if (component.Puller == args.User)
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling"),
                Act = () => TryStopPull(uid, component, user: args.User),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
        else if (CanPull(args.User, args.Target))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text"),
                Act = () => TryStartPull(args.User, args.Target),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, PullerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp<HeldSpeedModifierComponent>(component.Pulling, out var heldMoveSpeed) && component.Pulling.HasValue)
        {
            var (walkMod, sprintMod) =
                _clothingMoveSpeed.GetHeldMovementSpeedModifiers(component.Pulling.Value, heldMoveSpeed);
            args.ModifySpeed(walkMod, sprintMod);
            return;
        }

        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnPullableMoveInput(EntityUid uid, PullableComponent component, ref MoveInputEvent args)
    {
        // If someone moves then break their pulling.
        if (!component.BeingPulled)
            return;

        var entity = args.Entity;

        if (!_blocker.CanMove(entity))
            return;

        if (_timing.CurTime < component.NextBreakAttempt)
            return;

        var chance = 1f;
        if (component.Puller != null && TryComp(component.Puller, out PullerComponent? pullerComp))
        {
            chance = pullerComp.GrabStage switch
            {
                GrabStage.Medium => 0.5f,
                GrabStage.Heavy => 0.15f,
                GrabStage.Choke => 0f,
                _ => 1f
            };
        }

        if (_random.Prob(chance))
        {
            TryStopPull(uid, component, user: uid);
        }
        else
        {
            component.NextBreakAttempt = _timing.CurTime + BreakAttemptCooldown;
            Dirty(uid, component);
        }
    }

    private void OnPullableCollisionChange(EntityUid uid, PullableComponent component, ref CollisionChangeEvent args)
    {
        // IDK what this is supposed to be.
        if (!_timing.ApplyingState && component.PullJointId != null && !args.CanCollide)
        {
            _joints.RemoveJoint(uid, component.PullJointId);
        }
    }

    private void OnJointRemoved(EntityUid uid, PullableComponent component, JointRemovedEvent args)
    {
        // Just handles the joint getting nuked without going through pulling system (valid behavior).

        // Not relevant / pullable state handle it.
        if (component.Puller != args.OtherEntity ||
            args.Joint.ID != component.PullJointId ||
            _timing.ApplyingState)
        {
            return;
        }

        if (args.Joint.ID != component.PullJointId || component.Puller == null)
            return;

        if (TryComp<PullerComponent>(component.Puller, out var pullerComp) &&
            pullerComp.GrabStage == GrabStage.Choke &&
            pullerComp.Pulling == uid)
        {
            return;
        }

        StopPulling(uid, component);
    }

    /// <summary>
    /// Forces pulling to stop and handles cleanup.
    /// </summary>
    private void StopPulling(EntityUid pullableUid, PullableComponent pullableComp)
    {
        if (pullableComp.Puller == null)
            return;

        if (!_timing.ApplyingState)
        {
            // Joint shutdown
            if (pullableComp.PullJointId != null)
            {
                _joints.RemoveJoint(pullableUid, pullableComp.PullJointId);
                pullableComp.PullJointId = null;
            }

            if (TryComp<PhysicsComponent>(pullableUid, out var pullablePhysics))
            {
                _physics.SetFixedRotation(pullableUid, pullableComp.PrevFixedRotation, body: pullablePhysics);
            }
        }

        var oldPuller = pullableComp.Puller;
        if (oldPuller != null)
            RemComp<ActivePullerComponent>(oldPuller.Value);

        pullableComp.PullJointId = null;
        pullableComp.Puller = null;
        pullableComp.PullerGrabStage = GrabStage.None;
        Dirty(pullableUid, pullableComp);

        // No more joints with puller -> force stop pull.
        if (TryComp<PullerComponent>(oldPuller, out var pullerComp))
        {
            var pullerUid = oldPuller.Value;
            _alertsSystem.ClearAlert(pullerUid, pullerComp.PullingAlert);
            pullerComp.Pulling = null;
            if (pullerComp.GrabStage == GrabStage.Choke && TryComp<ChokedComponent>(pullableUid, out var choked))
            {
                var world = _transform.GetMapCoordinates(pullableUid);
                EntityUid oldParent;
                if (choked.HadOldParent && choked.OldParent is { } parent)
                    oldParent = parent;
                else
                    oldParent = _mapManager.GetMapEntityId(world.MapId);

                _transform.SetParent(pullableUid, oldParent);
                _transform.SetMapCoordinates(pullableUid, world);
                if (choked.HadPhysics && TryComp<PhysicsComponent>(pullableUid, out var chokedPhysics))
                {
                    _physics.SetBodyType(pullableUid, choked.OldBodyType, body: chokedPhysics);
                    _physics.SetCanCollide(pullableUid, choked.OldCanCollide, body: chokedPhysics);
                }
                if (choked.AddedMuted)
                    RemComp<MutedComponent>(pullableUid);
                _virtual.DeleteInHandsMatching(pullerUid, pullableUid);
                RemComp<ChokedComponent>(pullableUid);
            }

            pullerComp.GrabStage = GrabStage.None;
            Dirty(oldPuller.Value, pullerComp);

            // Messaging
            var message = new PullStoppedMessage(pullerUid, pullableUid);
            _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(pullerUid):user} stopped pulling {ToPrettyString(pullableUid):target}");

            RaiseLocalEvent(pullerUid, message);
            RaiseLocalEvent(pullableUid, message);
        }

        _alertsSystem.ClearAlert(pullableUid, pullableComp.PulledAlert);
    }

    public bool IsPulled(EntityUid uid, PullableComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.BeingPulled;
    }

    public bool IsPulling(EntityUid puller, PullerComponent? component = null)
    {
        return Resolve(puller, ref component, false) && component.Pulling != null;
    }

    public EntityUid? GetPuller(EntityUid puller, PullableComponent? component = null)
    {
        return !Resolve(puller, ref component, false) ? null : component.Puller;
    }

    public EntityUid? GetPulling(EntityUid puller, PullerComponent? component = null)
    {
        return !Resolve(puller, ref component, false) ? null : component.Pulling;
    }

    private void OnReleasePulledObject(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp(player, out PullerComponent? pullerComp) ||
            !TryComp(pullerComp.Pulling, out PullableComponent? pullableComp))
        {
            return;
        }

        TryStopPull(pullerComp.Pulling.Value, pullableComp, user: player);
    }

    public bool CanPull(EntityUid puller, EntityUid pullableUid, PullerComponent? pullerComp = null)
    {
        if (!Resolve(puller, ref pullerComp, false))
        {
            return false;
        }

        if (HasComp<ChokedComponent>(puller))
        {
            return false;
        }

        if (pullerComp.NeedsHands
            && !_handsSystem.TryGetEmptyHand(puller, out _)
            && pullerComp.Pulling == null)
        {
            return false;
        }

        if (!_blocker.CanInteract(puller, pullableUid))
        {
            return false;
        }

        if (!TryComp<PhysicsComponent>(pullableUid, out var physics))
        {
            return false;
        }

        if (physics.BodyType == BodyType.Static)
        {
            return false;
        }

        if (puller == pullableUid)
        {
            return false;
        }

        if (!_containerSystem.IsInSameOrNoContainer(puller, pullableUid))
        {
            return false;
        }

        var getPulled = new BeingPulledAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(pullableUid, getPulled, true);
        var startPull = new StartPullAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(puller, startPull, true);
        return !startPull.Cancelled && !getPulled.Cancelled;
    }

    public bool TogglePull(Entity<PullableComponent?> pullable, EntityUid pullerUid)
    {
        if (!Resolve(pullable, ref pullable.Comp, false))
            return false;

        if (pullable.Comp.Puller == pullerUid)
        {
            if (CanCombatGrab(pullerUid, pullable.Owner))
            {
                if (!TryComp<PullerComponent>(pullerUid, out var pullerComp))
                    return true;

                switch (pullerComp.GrabStage)
                {
                    case GrabStage.None:
                    case GrabStage.Light:
                    {
                        if (_timing.CurTime < pullerComp.NextMediumGrab)
                            return true;
                        DoGrabStage(pullerUid, pullable.Owner, "pulling-combat-medium-popup");
                        pullerComp.GrabStage = GrabStage.Medium;
                        pullerComp.NextHeavyGrab = _timing.CurTime + pullerComp.HeavyGrabCooldown;
                        pullable.Comp.PullerGrabStage = GrabStage.Medium;
                        Dirty(pullerUid, pullerComp);
                        Dirty(pullable.Owner, pullable.Comp);
                        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
                        var severity = GetGrabSeverity(pullerComp.GrabStage);
                        _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert, severity);
                        _alertsSystem.ShowAlert(pullable.Owner, pullable.Comp.PulledAlert, severity);
                        return true;
                    }
                    case GrabStage.Medium:
                    {
                        if (_timing.CurTime < pullerComp.NextHeavyGrab)
                            return true;
                        DoGrabStage(pullerUid, pullable.Owner, "pulling-combat-heavy-popup");
                        pullerComp.GrabStage = GrabStage.Heavy;
                        pullerComp.NextChokeGrab = _timing.CurTime + pullerComp.ChokeGrabCooldown;
                        pullable.Comp.PullerGrabStage = GrabStage.Heavy;
                        Dirty(pullerUid, pullerComp);
                        Dirty(pullable.Owner, pullable.Comp);
                        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
                        var severity = GetGrabSeverity(pullerComp.GrabStage);
                        _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert, severity);
                        _alertsSystem.ShowAlert(pullable.Owner, pullable.Comp.PulledAlert, severity);
                        return true;
                    }
                    case GrabStage.Heavy:
                    {
                        if (_timing.CurTime < pullerComp.NextChokeGrab)
                            return true;
                        DoGrabStage(pullerUid, pullable.Owner, "pulling-combat-choke-popup");
                        pullerComp.GrabStage = GrabStage.Choke;
                        EnsureChokeState(pullerUid, pullable.Owner);
                        pullable.Comp.PullerGrabStage = GrabStage.Choke;
                        Dirty(pullerUid, pullerComp);
                        Dirty(pullable.Owner, pullable.Comp);
                        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
                        var severity = GetGrabSeverity(pullerComp.GrabStage);
                        _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert, severity);
                        _alertsSystem.ShowAlert(pullable.Owner, pullable.Comp.PulledAlert, severity);
                        return true;
                    }
                    case GrabStage.Choke:
                    default:
                        return true;
                }
            }

            return TryStopPull(pullable, pullable.Comp);
        }

        return TryStartPull(pullerUid, pullable, pullableComp: pullable);
    }

    public bool TogglePull(EntityUid pullerUid, PullerComponent puller)
    {
        if (!TryComp<PullableComponent>(puller.Pulling, out var pullable))
            return false;

        return TogglePull((puller.Pulling.Value, pullable), pullerUid);
    }

    public bool TryStartPull(EntityUid pullerUid, EntityUid pullableUid,
        PullerComponent? pullerComp = null, PullableComponent? pullableComp = null)
    {
        if (!Resolve(pullerUid, ref pullerComp, false) ||
            !Resolve(pullableUid, ref pullableComp, false))
        {
            return false;
        }

        if (pullerComp.Pulling == pullableUid)
            return true;

        if (!CanPull(pullerUid, pullableUid))
            return false;

        if (!TryComp(pullerUid, out PhysicsComponent? pullerPhysics) || !TryComp(pullableUid, out PhysicsComponent? pullablePhysics))
            return false;

        // Ensure that the puller is not currently pulling anything.
        if (TryComp<PullableComponent>(pullerComp.Pulling, out var oldPullable)
            && !TryStopPull(pullerComp.Pulling.Value, oldPullable, pullerUid))
            return false;

        // Stop anyone else pulling the entity we want to pull
        if (pullableComp.Puller != null)
        {
            // We're already pulling this item
            if (pullableComp.Puller == pullerUid)
                return false;

            if (!TryStopPull(pullableUid, pullableComp, pullableComp.Puller))
                return false;
        }

        var pullAttempt = new PullAttemptEvent(pullerUid, pullableUid);
        RaiseLocalEvent(pullerUid, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        RaiseLocalEvent(pullableUid, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        // Pulling confirmed

        _interaction.DoContactInteraction(pullableUid, pullerUid);

        // Use net entity so it's consistent across client and server.
        pullableComp.PullJointId = $"pull-joint-{GetNetEntity(pullableUid)}";

        EnsureComp<ActivePullerComponent>(pullerUid);
        pullerComp.Pulling = pullableUid;
        pullerComp.GrabStage = GrabStage.None;
        pullerComp.NextMediumGrab = _timing.CurTime + pullerComp.LightGrabDelay;
        pullableComp.Puller = pullerUid;
        pullableComp.PullerGrabStage = GrabStage.None;
        pullableComp.NextBreakAttempt = _timing.CurTime;

        // store the pulled entity's physics FixedRotation setting in case we change it
        pullableComp.PrevFixedRotation = pullablePhysics.FixedRotation;

        // joint state handling will manage its own state
        if (!_timing.ApplyingState)
        {
            var joint = _joints.CreateDistanceJoint(pullableUid, pullerUid,
                    pullablePhysics.LocalCenter, pullerPhysics.LocalCenter,
                    id: pullableComp.PullJointId);
            joint.CollideConnected = false;
            // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
            // Internally, the joint length has been set to the distance between the pivots.
            // Add an additional 15cm (pretty arbitrary) to the maximum length for the hard limit.
            joint.MaxLength = joint.Length + 0.15f;
            joint.MinLength = 0f;
            // Set the spring stiffness to zero. The joint won't have any effect provided
            // the current length is beteen MinLength and MaxLength. At those limits, the
            // joint will have infinite stiffness.
            joint.Stiffness = 0f;

            _physics.SetFixedRotation(pullableUid, pullableComp.FixedRotationOnPull, body: pullablePhysics);
        }

        // Messaging
        var message = new PullStartedMessage(pullerUid, pullableUid);
        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
        var initialSeverity = GetGrabSeverity(pullerComp.GrabStage);
        _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert, initialSeverity);
        _alertsSystem.ShowAlert(pullableUid, pullableComp.PulledAlert, initialSeverity);

        RaiseLocalEvent(pullerUid, message);
        RaiseLocalEvent(pullableUid, message);

        Dirty(pullerUid, pullerComp);
        Dirty(pullableUid, pullableComp);

        if (!CanCombatGrab(pullerUid, pullableUid))
        {
            var pullingMessage =
                Loc.GetString("getting-pulled-popup", ("puller", Identity.Entity(pullerUid, EntityManager)));
            if (_netMan.IsServer)
                _popup.PopupEntity(pullingMessage, pullableUid, pullableUid);
        }

        if (CanCombatGrab(pullerUid, pullableUid))
        {
            DoGrabStage(pullerUid, pullableUid, "pulling-combat-medium-popup");
            pullerComp.GrabStage = GrabStage.Medium;
            pullerComp.NextHeavyGrab = _timing.CurTime + pullerComp.HeavyGrabCooldown;
            pullableComp.PullerGrabStage = GrabStage.Medium;
            Dirty(pullerUid, pullerComp);
            Dirty(pullableUid, pullableComp);
            _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
            var severity = GetGrabSeverity(pullerComp.GrabStage);
            _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert, severity);
            _alertsSystem.ShowAlert(pullableUid, pullableComp.PulledAlert, severity);
        }

        // Grab stages start after the first successful pull.

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(pullerUid):user} started pulling {ToPrettyString(pullableUid):target}");
        return true;
    }

    public bool TryStopPull(EntityUid pullableUid, PullableComponent pullable, EntityUid? user = null)
    {
        var pullerUidNull = pullable.Puller;

        if (pullerUidNull == null)
            return true;

        if (TryComp<PullerComponent>(pullerUidNull.Value, out var pullerComp) &&
            pullerComp.GrabStage == GrabStage.Choke &&
            pullerComp.Pulling == pullableUid &&
            user != pullerUidNull.Value)
        {
            return false;
        }

        var msg = new AttemptStopPullingEvent(user);
        RaiseLocalEvent(pullableUid, ref msg, true);

        if (msg.Cancelled)
            return false;

        StopPulling(pullableUid, pullable);
        return true;
    }

    private void DoGrabStage(EntityUid pullerUid, EntityUid targetUid, string locKey, bool playSound = true)
    {
        var popupMessage = Loc.GetString(locKey,
            ("target", Identity.Entity(targetUid, EntityManager)));
        if (_netMan.IsServer)
            _popup.PopupEntity(popupMessage, pullerUid, pullerUid);
        var targetKey = locKey.Replace("-popup", "-target-popup");
        var targetMessage = Loc.GetString(targetKey,
            ("puller", Identity.Entity(pullerUid, EntityManager)));
        if (_netMan.IsServer)
            _popup.PopupEntity(targetMessage, targetUid, targetUid);
        _outlineFlash.RaiseEffect(targetUid, pullerUid);
        _colorFlash.RaiseEffect(Color.Yellow, new List<EntityUid> { targetUid },
            Filter.Pvs(targetUid, entityManager: EntityManager));
        if (playSound && _netMan.IsServer && TryComp<CombatModeComponent>(pullerUid, out var combatMode))
            _audio.PlayPvs(combatMode.DisarmSuccessSound, targetUid);
    }

    private bool CanCombatGrab(EntityUid pullerUid, EntityUid targetUid)
    {
        return _combatMode.IsInCombatMode(pullerUid)
               && HasComp<HumanoidAppearanceComponent>(pullerUid)
               && HasComp<MobStateComponent>(targetUid);
    }

    private static short GetGrabSeverity(GrabStage stage)
    {
        return stage switch
        {
            GrabStage.Medium => 1,
            GrabStage.Heavy => 2,
            GrabStage.Choke => 3,
            _ => 0
        };
    }

    private void EnsureChokeState(EntityUid pullerUid, EntityUid targetUid)
    {
        if (!_netMan.IsServer)
            return;

        var choked = EnsureComp<ChokedComponent>(targetUid);
        choked.OldParent = Transform(targetUid).ParentUid;
        choked.HadOldParent = choked.OldParent != null;
        choked.AddedMuted = false;
        choked.HadPhysics = false;
        choked.Puller = pullerUid;
        choked.Offset = Vector2.Zero;
        choked.LockedWorldRotation = _transform.GetWorldRotation(targetUid);

        if (!HasComp<MutedComponent>(targetUid))
        {
            EnsureComp<MutedComponent>(targetUid);
            choked.AddedMuted = true;
        }

        _transform.SetParent(targetUid, pullerUid);
        _transform.SetCoordinates(targetUid, new EntityCoordinates(pullerUid, choked.Offset));
        _transform.SetWorldRotation(targetUid, choked.LockedWorldRotation);

        _stun.TryKnockdown(targetUid, TimeSpan.FromSeconds(2), refresh: true, autoStand: false, drop: false, force: true);

        if (TryComp<PhysicsComponent>(targetUid, out var physics))
        {
            choked.HadPhysics = true;
            choked.OldBodyType = physics.BodyType;
            choked.OldCanCollide = physics.CanCollide;
            _physics.SetBodyType(targetUid, BodyType.Kinematic, body: physics);
            _physics.SetCanCollide(targetUid, false, body: physics);
        }
        
        _virtual.DeleteInHandsMatching(pullerUid, targetUid);
        if (_virtual.TrySpawnVirtualItemInHand(targetUid, pullerUid, out var virt1, dropOthers: true, silent: true))
            EnsureComp<UnremoveableComponent>(virt1.Value);
        if (_virtual.TrySpawnVirtualItemInHand(targetUid, pullerUid, out var virt2, dropOthers: true, silent: true))
            EnsureComp<UnremoveableComponent>(virt2.Value);
    }
}
