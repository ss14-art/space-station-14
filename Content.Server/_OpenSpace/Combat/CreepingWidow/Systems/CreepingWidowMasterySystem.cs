using System;
using System.Numerics;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Server._OpenSpace.Combat.CreepingWidow.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Combat.CreepingWidow.Systems;

public sealed class CreepingWidowMasterySystem : CombatMasteryTechniqueSystem<CreepingWidowMasteryComponent>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreepingWidowMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
    }

    protected override void OnMasteryStarted(Entity<CreepingWidowMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override void OnMasteryStopped(Entity<CreepingWidowMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override bool OnTemplateMatched(Entity<CreepingWidowMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        return template.Name switch
        {
            CreepingWidowMasteryComponent.EnergyTornadoTemplateName => TryExecuteEnergyTornado(ent, target),
            CreepingWidowMasteryComponent.PalmStrikeTemplateName => TryExecutePalmStrike(ent, target),
            CreepingWidowMasteryComponent.NeckSliceTemplateName => TryExecuteNeckSlice(ent, target),
            CreepingWidowMasteryComponent.WrenchWristTemplateName => TryExecuteWrenchWrist(ent, target),
            _ => false
        };
    }

    private void OnCollectMeleeDamage(Entity<CreepingWidowMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.UnarmedDamage);
    }

    private bool TryExecuteEnergyTornado(Entity<CreepingWidowMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseTechniqueTarget(ent.Owner, target) ||
            !TryConsumeFocus(ent, ent.Comp.EnergyTornadoFocusCooldown))
        {
            return false;
        }

        ThrowAwayFromUser(ent.Owner, target, ent.Comp.EnergyTornadoThrowDistance, ent.Comp.EnergyTornadoThrowSpeed);

        var userCoords = Transform(ent.Owner).Coordinates;
        foreach (var uid in _lookup.GetEntitiesInRange(userCoords,
                     ent.Comp.EnergyTornadoRange,
                     LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (uid == ent.Owner || uid == target || !HasComp<MobStateComponent>(uid))
                continue;

            ThrowAwayFromUser(ent.Owner, uid, ent.Comp.EnergyTornadoThrowDistance, ent.Comp.EnergyTornadoThrowSpeed);
        }

        if (CanSpawnSmoke(ent.Owner))
            SpawnEnergyTornadoSmoke(ent);

        PopupTechnique(ent.Owner, target,
            "creeping-widow-energy-tornado-attacker-popup",
            "creeping-widow-energy-tornado-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecutePalmStrike(Entity<CreepingWidowMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechniqueTarget(ent.Owner, target) ||
            !TryConsumeFocus(ent, ent.Comp.PalmStrikeFocusCooldown))
        {
            return false;
        }

        ThrowAwayFromUser(ent.Owner,
            target,
            ent.Comp.PalmStrikeThrowDistance,
            MathF.Min(ent.Comp.PalmStrikeThrowSpeed, 20f));

        _stun.TryKnockdown(target,
            ent.Comp.PalmStrikeKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);

        PopupTechnique(ent.Owner, target,
            "creeping-widow-palm-strike-attacker-popup",
            "creeping-widow-palm-strike-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteNeckSlice(Entity<CreepingWidowMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseTechniqueTarget(ent.Owner, target) ||
            _mobState.IsDead(target) ||
            !IsEntityDown(target) ||
            !HasNeckSliceWeaponReady(ent.Owner) ||
            !TryConsumeFocus(ent, ent.Comp.NeckSliceFocusCooldown) ||
            !TryComp<DamageableComponent>(target, out var damageable) ||
            !_mobThreshold.TryGetDeadThreshold(target, out var deadThreshold))
        {
            return false;
        }

        var damageToDeath = deadThreshold.Value.Float() - damageable.TotalDamage.Float();
        if (damageToDeath <= 0f)
            return false;

        _damageable.TryChangeDamage(target,
            CreateBluntDamage(ent.Comp.SlashDamageType, damageToDeath),
            ignoreResistances: true,
            origin: ent.Owner);

        PopupTechnique(ent.Owner, target,
            "creeping-widow-neck-slice-attacker-popup",
            "creeping-widow-neck-slice-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteWrenchWrist(Entity<CreepingWidowMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechniqueTarget(ent.Owner, target) ||
            !TryConsumeFocus(ent, ent.Comp.WrenchWristFocusCooldown))
        {
            return false;
        }

        if (!TryForceDropActiveItem(target))
            return false;

        PopupTechnique(ent.Owner, target,
            "creeping-widow-wrench-wrist-attacker-popup",
            "creeping-widow-wrench-wrist-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryConsumeFocus(Entity<CreepingWidowMasteryComponent> ent, TimeSpan cooldown)
    {
        if (_timing.CurTime < ent.Comp.NextFocusReadyAt)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.FocusFailPopupLoc), ent.Owner, ent.Owner);
            return false;
        }

        ent.Comp.NextFocusReadyAt = _timing.CurTime + cooldown;
        return true;
    }

    private bool CanUseTechniqueTarget(EntityUid user, EntityUid target)
    {
        return target != user && !TerminatingOrDeleted(target);
    }

    private bool CanUseStandingTechniqueTarget(EntityUid user, EntityUid target)
    {
        return CanUseTechniqueTarget(user, target) &&
               !_mobState.IsDead(target) &&
               !_mobState.IsCritical(target) &&
               !IsEntityDown(target) &&
               !HasComp<StunnedComponent>(target);
    }

    private bool TryForceDropActiveItem(EntityUid target)
    {
        if (!_hands.TryGetActiveItem(target, out var activeItem) || HasComp<VirtualItemComponent>(activeItem.Value))
            return false;

        return _hands.TryDrop(target, activeItem.Value, checkActionBlocker: false, doDropInteraction: false);
    }

    private bool HasNeckSliceWeaponReady(EntityUid user)
    {
        if (!TryComp<HandsComponent>(user, out var hands) ||
            _hands.TryGetActiveItem((user, hands), out _))
        {
            return false;
        }

        var activeHand = _hands.GetActiveHand((user, hands));
        foreach (var hand in hands.Hands.Keys)
        {
            if (hand == activeHand || !_hands.TryGetHeldItem((user, hands), hand, out var held))
                continue;

            if (HasComp<EnergyKatanaComponent>(held.Value))
                return true;
        }

        return false;
    }

    private bool CanSpawnSmoke(EntityUid user)
    {
        return _inventory.TryGetSlotEntity(user, "outerClothing", out var outer) &&
               HasComp<NinjaSuitComponent>(outer.Value);
    }

    private void SpawnEnergyTornadoSmoke(Entity<CreepingWidowMasteryComponent> ent)
    {
        var smoke = Spawn(ent.Comp.EnergyTornadoSmokePrototype, Transform(ent.Owner).Coordinates.SnapToGrid());
        if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            Del(smoke);
            return;
        }

        var solution = new Solution();
        _smoke.StartSmoke(smoke,
            solution,
            ent.Comp.EnergyTornadoSmokeDurationSeconds,
            ent.Comp.EnergyTornadoSmokeSpreadAmount,
            smokeComp);
    }

    private void ThrowAwayFromUser(EntityUid user, EntityUid target, float distance, float speed)
    {
        if (TerminatingOrDeleted(target))
            return;

        var userPos = _transform.GetWorldPosition(Transform(user));
        var targetPos = _transform.GetWorldPosition(Transform(target));
        var direction = targetPos - userPos;

        if (direction == Vector2.Zero)
            direction = Transform(user).LocalRotation.ToWorldVec();

        if (direction == Vector2.Zero)
            return;

        _throwing.TryThrow(target,
            direction.Normalized() * distance,
            speed,
            user,
            compensateFriction: true,
            doSpin: false);
    }
}
