using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Server._OpenSpace.Combat.KravMaga.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared._OpenSpace.Combat.CombatMastery.Events;
using Content.Shared._OpenSpace.Combat.KravMaga;
using Content.Shared._Starlight.BreathOrgan;
using Robust.Shared.Random;

namespace Content.Server._OpenSpace.Combat.KravMaga.Systems;

public sealed class KravMagaMasterySystem : CombatMasteryTechniqueSystem<KravMagaMasteryComponent>
{
    [Dependency] private readonly HeldBreathSystem _heldBreath = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KravMagaMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
        SubscribeLocalEvent<KravMagaMasteryComponent, KravMagaLegSweepActionEvent>(OnLegSweepAction);
        SubscribeLocalEvent<KravMagaMasteryComponent, KravMagaLungPunchActionEvent>(OnLungPunchAction);
        SubscribeLocalEvent<KravMagaMasteryComponent, KravMagaNeckChopActionEvent>(OnNeckChopAction);
        SubscribeLocalEvent<KravMagaMasteryComponent, CombatDisarmAttemptedEvent>(OnCombatDisarmAttempted);
        SubscribeLocalEvent<KravMagaMasteryComponent, CombatMasteryMeleeAttackedEvent>(OnMeleeAttacked);
    }

    protected override void OnMasteryStarted(Entity<KravMagaMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override void OnMasteryStopped(Entity<KravMagaMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override bool OnTemplateMatched(Entity<KravMagaMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        return false;
    }

    private void OnCollectMeleeDamage(Entity<KravMagaMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.UnarmedDamage);
    }

    private void OnMeleeAttacked(Entity<KravMagaMasteryComponent> ent, ref CombatMasteryMeleeAttackedEvent args)
    {
        if (args.Attacker != ent.Owner ||
            !IsMasteryActive(ent) ||
            !IsUnarmedMeleeAttack(args) ||
            !IsEntityDown(args.Target))
        {
            return;
        }

        args.Attack.BonusDamage += CreateBluntDamage(ent.Comp.BluntDamageType, ent.Comp.DownedTargetBonusDamage);
    }

    private void OnCombatDisarmAttempted(Entity<KravMagaMasteryComponent> ent, ref CombatDisarmAttemptedEvent args)
    {
        if (args.User != ent.Owner || !IsMasteryActive(ent))
            return;

        args.Cancelled = true;

        if (!_random.Prob(ent.Comp.DisarmStealChance) ||
            !_hands.TryGetActiveItem(args.Target, out var activeItem) ||
            HasComp<VirtualItemComponent>(activeItem.Value) ||
            !_hands.TryDrop(args.Target, activeItem.Value, checkActionBlocker: false, doDropInteraction: false))
        {
            return;
        }

        _hands.PickupOrDrop(args.User, activeItem.Value, checkActionBlocker: false, animate: false, dropNear: true);
    }

    private void OnLegSweepAction(Entity<KravMagaMasteryComponent> ent, ref KravMagaLegSweepActionEvent args)
    {
        if (args.Handled ||
            !CanUseTechnique(ent, args.Performer, args.Target) ||
            IsTargetStunned(args.Target))
        {
            return;
        }

        ApplyBluntDamage(ent.Owner, args.Target, ent.Comp.BluntDamageType, ent.Comp.LegSweepBluntDamage);
        ApplySlipLikeStun(args.Target, ent.Comp.LegSweepStunDuration);
        PopupTechnique(ent.Owner, args.Target,
            "krav-maga-leg-sweep-attacker-popup",
            "krav-maga-leg-sweep-target-popup",
            includeTargetName: true);
        args.Handled = true;
    }

    private void OnLungPunchAction(Entity<KravMagaMasteryComponent> ent, ref KravMagaLungPunchActionEvent args)
    {
        if (args.Handled || !CanUseTechnique(ent, args.Performer, args.Target))
            return;

        ApplyBluntDamage(ent.Owner, args.Target, ent.Comp.AsphyxiationDamageType, ent.Comp.LungPunchAsphyxiationDamage);
        _heldBreath.TryAddHeldBreathDuration(args.Target, ent.Comp.LungPunchHeldBreathDuration);
        PopupTechnique(ent.Owner, args.Target,
            "krav-maga-lung-punch-attacker-popup",
            "krav-maga-lung-punch-target-popup",
            includeTargetName: true);
        args.Handled = true;
    }

    private void OnNeckChopAction(Entity<KravMagaMasteryComponent> ent, ref KravMagaNeckChopActionEvent args)
    {
        if (args.Handled || !CanUseTechnique(ent, args.Performer, args.Target))
            return;

        ApplyBluntDamage(ent.Owner, args.Target, ent.Comp.BluntDamageType, ent.Comp.NeckChopBluntDamage);
        _statusEffects.TryAddStatusEffectDuration(args.Target,
            KravMagaMasteryComponent.MutedStatusEffectId,
            ent.Comp.NeckChopMuteDuration);
        PopupTechnique(ent.Owner, args.Target,
            "krav-maga-neck-chop-attacker-popup",
            "krav-maga-neck-chop-target-popup",
            includeTargetName: true);
        args.Handled = true;
    }

    private static bool IsUnarmedMeleeAttack(CombatMasteryMeleeAttackedEvent args)
    {
        return args.Attack.Used == args.Attack.User;
    }

    private bool CanUseTechnique(Entity<KravMagaMasteryComponent> ent, EntityUid performer, EntityUid target)
    {
        return performer == ent.Owner &&
               target != ent.Owner &&
               !TerminatingOrDeleted(target) &&
               IsMasteryActive(ent);
    }

    private bool IsTargetStunned(EntityUid target)
    {
        return HasComp<StunnedComponent>(target)
               || HasComp<KnockedDownComponent>(target)
               || HasComp<SleepingComponent>(target);
    }

    private void ApplySlipLikeStun(EntityUid target, TimeSpan duration)
    {
        _stun.TryUpdateStunDuration(target, duration);
        _stun.TryKnockdown(target,
            duration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);
    }
}
