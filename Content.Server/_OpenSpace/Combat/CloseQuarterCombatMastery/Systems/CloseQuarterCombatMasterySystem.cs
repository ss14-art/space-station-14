using System.Numerics;
using Content.Server._OpenSpace.Combat.CloseQuarterCombatMastery.Components;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Jittering;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Content.Shared._Starlight.Medical.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Combat.CloseQuarterCombatMastery.Systems;

public sealed class CloseQuarterCombatMasterySystem : CombatMasteryTechniqueSystem<CloseQuarterCombatMasteryComponent>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, AttackAttemptEvent>(OnAttackAttempt,
            before: [typeof(CombatMasteryControllerSystem)]);
        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, DisarmedEvent>(OnDisarmed, before: [typeof(SharedStaminaSystem)]);
        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, CombatMasteryMeleeAttackedEvent>(OnMeleeAttacked);
        SubscribeLocalEvent<CloseQuarterCombatMasteryComponent, DamageBeforeApplyEvent>(OnDamageBeforeApply);
    }

    protected override void OnMasteryStarted(Entity<CloseQuarterCombatMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override void OnMasteryStopped(Entity<CloseQuarterCombatMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    private void OnCollectMeleeDamage(Entity<CloseQuarterCombatMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.UnarmedDamage);
    }

    protected override bool OnTemplateMatched(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        switch (template.Name)
        {
            case CloseQuarterCombatMasteryComponent.SlamTemplateName:
                return TryExecuteSlam(ent, target);
            case CloseQuarterCombatMasteryComponent.CQCKickTemplateName:
                return TryExecuteKick(ent, target);
            case CloseQuarterCombatMasteryComponent.RestrainTemplateName:
                return TryExecuteRestrain(ent, target);
            case CloseQuarterCombatMasteryComponent.PressureTemplateName:
                return TryExecutePressure(ent, target);
            case CloseQuarterCombatMasteryComponent.ConsecutiveCQCTemplateName:
                return TryExecuteConsecutiveCqc(ent, target);
        }

        return false;
    }

    private void OnAttackAttempt(Entity<CloseQuarterCombatMasteryComponent> ent, ref AttackAttemptEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        if (args.Cancelled || args.Uid != ent.Owner || !args.Disarm || args.Target is not { } target)
            return;

        if (!CanUseRestrainFollowup(ent, target))
            return;

        if (!TryComp<PullerComponent>(ent.Owner, out var puller) ||
            puller.Pulling != target ||
            puller.GrabStage == GrabStage.None)
        {
            return;
        }

        ExecuteRestrainFollowup(ent, target);
        args.Cancel();
    }

    protected override void OnComboUpdated(Entity<CloseQuarterCombatMasteryComponent> ent, ref CombatMasteryComboUpdatedEvent args)
    {
        base.OnComboUpdated(ent, ref args);

        if (!ent.Comp.RestrainFollowupReady)
            return;

        if (ent.Comp.SkipNextComboResetForRestrain)
        {
            ent.Comp.SkipNextComboResetForRestrain = false;
            return;
        }

        ResetRestrainFollowup(ent.Comp);
    }

    private void OnMeleeAttacked(Entity<CloseQuarterCombatMasteryComponent> ent, ref CombatMasteryMeleeAttackedEvent args)
    {
        if (args.Attacker == ent.Owner &&
            IsMasteryActive(ent) &&
            IsUnarmedMeleeAttack(args))
        {
            var targetDown = IsEntityDown(args.Target);
            var bonusDamage = 0f;

            if (targetDown)
                bonusDamage += ent.Comp.UnarmedDownedTargetBonusDamage;

            if (IsEntityDown(args.Attacker) && !targetDown)
            {
                bonusDamage += ent.Comp.ProneAttackerBonusDamage;
                _stun.TryKnockdown(args.Target,
                    ent.Comp.ProneAttackerKnockdownDuration,
                    refresh: true,
                    autoStand: true,
                    drop: true,
                    voluntary: true);
                StandImmediately(args.Attacker);
            }

            if (bonusDamage > 0f)
                AddUnarmedBonusDamage(ref args, ent.Comp, bonusDamage);
        }

        if (args.Target != ent.Owner ||
            args.Attacker == ent.Owner ||
            !IsMasteryActive(ent) ||
            !_random.Prob(ent.Comp.DefensiveMeleeNullifyChance))
        {
            return;
        }

        SetPendingDefensiveNullify(ent.Comp,
            args.Attacker,
            nullifyDamage: true,
            nullifyStamina: true);

        PopupDefensiveNullify(ent.Owner, args.Attacker);
        ApplyDefensiveCounter(ent.Comp, args.Attacker);
    }

    private void OnDisarmed(Entity<CloseQuarterCombatMasteryComponent> ent, ref DisarmedEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        if (args.Handled || !_random.Prob(ent.Comp.DefensiveMeleeNullifyChance))
            return;

        SetPendingDefensiveNullify(ent.Comp,
            args.Source,
            nullifyDamage: false,
            nullifyStamina: true);

        PopupDefensiveNullify(ent.Owner, args.Source);
        ApplyDefensiveCounter(ent.Comp, args.Source);
    }

    private void OnBeforeStaminaDamage(Entity<CloseQuarterCombatMasteryComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        if (!ent.Comp.PendingDefensiveMeleeNullifyStamina)
            return;

        if (IsPendingDefensiveNullifyExpired(ent.Comp))
        {
            ResetPendingDefensiveNullify(ent.Comp);
            return;
        }

        args.Cancelled = true;
        ent.Comp.PendingDefensiveMeleeNullifyStamina = false;
        ClearPendingDefensiveNullifyIfUnused(ent.Comp);
    }

    private void OnDamageBeforeApply(Entity<CloseQuarterCombatMasteryComponent> ent, ref DamageBeforeApplyEvent args)
    {
        if (!ent.Comp.PendingDefensiveMeleeNullify)
        {
            return;
        }

        if (!IsMasteryActive(ent))
        {
            ResetPendingDefensiveNullify(ent.Comp);
            return;
        }

        if (IsPendingDefensiveNullifyExpired(ent.Comp))
        {
            ResetPendingDefensiveNullify(ent.Comp);
            return;
        }

        if (args.Origin != ent.Comp.PendingDefensiveMeleeOrigin)
            return;

        args.Damage = new DamageSpecifier();

        ent.Comp.PendingDefensiveMeleeNullify = false;
        ClearPendingDefensiveNullifyIfUnused(ent.Comp);
    }

    private bool DoSlam(EntityUid user, EntityUid target, CloseQuarterCombatMasteryComponent component)
    {
        if (TerminatingOrDeleted(target) || IsTargetStunned(target))
            return false;

        ApplyBluntDamage(user, target, component.BluntDamageType, component.SlamBluntDamage);
        _stun.TryKnockdown(target, component.SlamKnockdownDuration, refresh: true, autoStand: true, drop: true, force: true);
        return true;
    }

    private bool DoCQCKick(EntityUid user, EntityUid target, CloseQuarterCombatMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        if (IsTargetStunned(target))
        {
            ApplyBluntDamage(user, target, component.BluntDamageType, component.CQCKickStunnedBluntDamage);
            _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, component.CQCKickSleepDuration);
            return true;
        }

        ApplyBluntDamage(user, target, component.BluntDamageType, component.CQCKickBluntDamage);
        ThrowAwayFromUser(user, target, component.CQCKickThrowDistance, component.CQCKickThrowSpeed);
        return true;
    }

    private bool DoRestrain(EntityUid user, EntityUid target, CloseQuarterCombatMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        _stamina.TakeStaminaDamage(target, component.RestrainStaminaDamage, source: user);
        _stun.TryKnockdown(target, component.RestrainKnockdownDuration, refresh: true, autoStand: true, drop: true, force: true);

        component.RestrainFollowupReady = true;
        component.SkipNextComboResetForRestrain = true;
        component.RestrainFollowupTarget = target;
        component.RestrainFollowupExpireAt = _timing.CurTime + component.RestrainFollowupWindow;
        return true;
    }

    private bool DoPressure(EntityUid user, EntityUid target, CloseQuarterCombatMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        _stamina.TakeStaminaDamage(target, component.PressureStaminaDamage, source: user);
        return true;
    }

    private bool DoConsecutiveCqc(EntityUid user, EntityUid target, CloseQuarterCombatMasteryComponent component)
    {
        if (TerminatingOrDeleted(target) || IsTargetStunned(target))
            return false;

        ApplyBluntDamage(user, target, component.BluntDamageType, component.ConsecutiveCqcBluntDamage);
        _stamina.TakeStaminaDamage(target, component.ConsecutiveCqcStaminaDamage, source: user);
        TryPickupTargetActiveItem(user, target);
        return true;
    }

    private bool CanUseRestrainFollowup(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!ent.Comp.RestrainFollowupReady || ent.Comp.RestrainFollowupTarget != target)
            return false;

        if (_timing.CurTime <= ent.Comp.RestrainFollowupExpireAt)
            return true;

        ResetRestrainFollowup(ent.Comp);
        return false;
    }

    private void ExecuteRestrainFollowup(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        _statusEffects.TryAddStatusEffectDuration(target,
            SleepingSystem.StatusEffectForcedSleeping,
            ent.Comp.RestrainFollowupSleepDuration);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TogglePull((target, pullable), ent.Owner);

        if (_random.Prob(ent.Comp.RestrainFollowupBonusDamageChance))
        {
            ApplyBluntDamage(ent.Owner, target, ent.Comp.BluntDamageType, ent.Comp.RestrainFollowupBluntDamage);
            _jittering.DoJitter(target, ent.Comp.RestrainFollowupJitterDuration, refresh: true);
        }

        TryPickupTargetActiveItem(ent.Owner, target);
        PopupTechnique(ent.Owner, target,
            "cqc-followup-pressure-attacker-popup",
            "cqc-followup-pressure-target-popup",
            true);
        ResetRestrainFollowup(ent.Comp);
    }

    private void PopupDefensiveNullify(EntityUid defender, EntityUid attacker)
    {
        PopupTechnique(defender, attacker,
            "cqc-defensive-nullify-defender-popup",
            "cqc-defensive-nullify-attacker-popup");
    }

    private void TryPickupTargetActiveItem(EntityUid user, EntityUid target)
    {
        if (!TryComp<HandsComponent>(target, out var targetHands))
            return;

        if (!_hands.TryGetActiveItem((target, targetHands), out var item))
            return;

        if (HasComp<VirtualItemComponent>(item.Value))
            return;

        _hands.PickupOrDrop(user, item.Value, checkActionBlocker: false, animate: false, dropNear: true);
    }

    private bool HasRealActiveItem(EntityUid user)
    {
        return _hands.TryGetActiveItem(user, out var held) &&
               !HasComp<VirtualItemComponent>(held.Value);
    }

    private void ThrowAwayFromUser(EntityUid user, EntityUid target, float distance, float speed)
    {
        var userPos = _transform.GetWorldPosition(Transform(user));
        var targetPos = _transform.GetWorldPosition(Transform(target));
        var direction = targetPos - userPos;

        if (direction == Vector2.Zero)
            direction = Transform(user).LocalRotation.ToWorldVec();

        if (direction == Vector2.Zero)
            return;

        var throwVector = direction.Normalized() * distance;
        _throwing.TryThrow(target, throwVector, speed, user, compensateFriction: true, doSpin: false);
    }

    private bool IsTargetStunned(EntityUid target)
    {
        return HasComp<StunnedComponent>(target)
               || HasComp<KnockedDownComponent>(target)
               || HasComp<SleepingComponent>(target);
    }

    private static bool IsUnarmedMeleeAttack(CombatMasteryMeleeAttackedEvent args)
    {
        return args.Attack.Used == args.Attack.User;
    }

    private void AddUnarmedBonusDamage(ref CombatMasteryMeleeAttackedEvent args, CloseQuarterCombatMasteryComponent component, float bonusDamage)
    {
        args.Attack.BonusDamage += CreateBluntDamage(component.BluntDamageType, bonusDamage);
    }

    private static void ResetRestrainFollowup(CloseQuarterCombatMasteryComponent component)
    {
        component.RestrainFollowupReady = false;
        component.SkipNextComboResetForRestrain = false;
        component.RestrainFollowupTarget = null;
        component.RestrainFollowupExpireAt = default;
    }

    private static void ResetPendingDefensiveNullify(CloseQuarterCombatMasteryComponent component)
    {
        component.PendingDefensiveMeleeNullify = false;
        component.PendingDefensiveMeleeNullifyStamina = false;
        component.PendingDefensiveMeleeOrigin = null;
        component.PendingDefensiveMeleeExpireAt = default;
    }

    private void SetPendingDefensiveNullify(
        CloseQuarterCombatMasteryComponent component,
        EntityUid origin,
        bool nullifyDamage,
        bool nullifyStamina)
    {
        component.PendingDefensiveMeleeOrigin = origin;
        component.PendingDefensiveMeleeExpireAt = _timing.CurTime + component.DefensiveMeleeNullifyWindow;
        component.PendingDefensiveMeleeNullify = nullifyDamage;
        component.PendingDefensiveMeleeNullifyStamina = nullifyStamina;
    }

    private void ApplyDefensiveCounter(CloseQuarterCombatMasteryComponent component, EntityUid attacker)
    {
        if (!HasRealActiveItem(attacker))
            return;

        _stun.TryUpdateStunDuration(attacker, component.DefensiveMeleeCounterKnockdownDuration);
        _stun.TryKnockdown(attacker,
            component.DefensiveMeleeCounterKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true);
    }

    private bool IsPendingDefensiveNullifyExpired(CloseQuarterCombatMasteryComponent component)
    {
        return _timing.CurTime > component.PendingDefensiveMeleeExpireAt;
    }

    private static void ClearPendingDefensiveNullifyIfUnused(CloseQuarterCombatMasteryComponent component)
    {
        if (component.PendingDefensiveMeleeNullify || component.PendingDefensiveMeleeNullifyStamina)
            return;

        component.PendingDefensiveMeleeOrigin = null;
        component.PendingDefensiveMeleeExpireAt = default;
    }
    private bool TryExecuteSlam(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!DoSlam(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target, "cqc-slam-attacker-popup", "cqc-slam-target-popup", includeTargetName: true);
        return true;
    }

    private bool TryExecuteKick(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!DoCQCKick(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target, "cqc-kick-attacker-popup", "cqc-kick-target-popup");
        return true;
    }

    private bool TryExecuteRestrain(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!DoRestrain(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target, "cqc-restrain-attacker-popup", "cqc-restrain-target-popup", includeTargetName: true);
        return true;
    }

    private bool TryExecutePressure(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!DoPressure(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target, "cqc-pressure-attacker-popup", "cqc-pressure-target-popup");
        return true;
    }

    private bool TryExecuteConsecutiveCqc(Entity<CloseQuarterCombatMasteryComponent> ent, EntityUid target)
    {
        if (!DoConsecutiveCqc(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target, "cqc-consecutive-attacker-popup", "cqc-consecutive-target-popup", includeTargetName: true);
        return true;
    }
}
