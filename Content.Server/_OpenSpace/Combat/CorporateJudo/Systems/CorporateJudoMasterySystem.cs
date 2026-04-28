using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Server._OpenSpace.Combat.CorporateJudo.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using LegacyStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;

namespace Content.Server._OpenSpace.Combat.CorporateJudo.Systems;

public sealed class CorporateJudoMasterySystem : CombatMasteryTechniqueSystem<CorporateJudoMasteryComponent>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LegacyStatusEffectsSystem _legacyStatusEffects = default!;
    [Dependency] private readonly MovementModStatusSystem _movement = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporateJudoMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
    }

    protected override void OnMasteryStarted(Entity<CorporateJudoMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override void OnMasteryStopped(Entity<CorporateJudoMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    private void OnCollectMeleeDamage(Entity<CorporateJudoMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.UnarmedDamage);
    }

    protected override bool OnTemplateMatched(Entity<CorporateJudoMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        switch (template.Name)
        {
            case CorporateJudoMasteryComponent.DiscombobulateTemplateName:
                return TryExecuteDiscombobulate(ent, target);
            case CorporateJudoMasteryComponent.EyePokeTemplateName:
                return TryExecuteEyePoke(ent, target);
            case CorporateJudoMasteryComponent.JudoThrowTemplateName:
                return TryExecuteJudoThrow(ent, target);
            case CorporateJudoMasteryComponent.ArmbarTemplateName:
                return TryExecuteArmbar(ent, target);
            case CorporateJudoMasteryComponent.WheelThrowTemplateName:
                return TryExecuteWheelThrow(ent, target);
            case CorporateJudoMasteryComponent.GoldenBlastTemplateName:
                return TryExecuteGoldenBlast(ent, target);
        }

        return false;
    }

    private bool DoDiscombobulate(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        ApplyDisorient(target, component, component.DiscombobulateDisorientDuration);
        _stamina.TakeStaminaDamage(target, component.DiscombobulateStaminaDamage, source: user);
        return true;
    }

    private bool DoEyePoke(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        _legacyStatusEffects.TryAddStatusEffect<TemporaryBlindnessComponent>(
            target,
            TemporaryBlindnessSystem.BlindingStatusEffect,
            component.EyePokeBlindDuration,
            refresh: false);

        ApplyBluntDamage(user, target, component.BluntDamageType, component.EyePokeBluntDamage);
        return true;
    }

    private bool DoJudoThrow(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target) || IsEntityDown(user) || IsEntityDown(target))
            return false;

        _stamina.TakeStaminaDamage(target, component.JudoThrowStaminaDamage, source: user);
        ApplySlipLikeStun(target, component.JudoThrowKnockdownDuration);
        return true;
    }

    private bool DoArmbar(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target) || !IsEntityDown(target))
            return false;

        _stamina.TakeStaminaDamage(target, component.ArmbarStaminaDamage, source: user);
        ApplySlipLikeStun(target, component.ArmbarStunDuration);
        return true;
    }

    private bool DoWheelThrow(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target) || !CanUseWheelThrow(user, target))
            return false;

        _stamina.TakeStaminaDamage(target, component.WheelThrowStaminaDamage, source: user);
        ApplySlipLikeStun(target, component.WheelThrowStunDuration);
        ApplyDisorient(target, component, component.WheelThrowDisorientDuration);
        return true;
    }

    private bool DoGoldenBlast(EntityUid user, EntityUid target, CorporateJudoMasteryComponent component)
    {
        if (TerminatingOrDeleted(target))
            return false;

        ApplySlipLikeStun(target, component.GoldenBlastStunDuration);
        ApplyDisorient(target, component, component.GoldenBlastDisorientDuration);
        return true;
    }

    private bool CanUseWheelThrow(EntityUid user, EntityUid target)
    {
        if (_hands.TryGetActiveItem(user, out _))
            return false;

        if (!TryComp<PullerComponent>(user, out var puller) ||
            puller.Pulling != target ||
            puller.GrabStage == GrabStage.None)
        {
            return false;
        }

        return TryComp<CombatMasteryComponent>(user, out var mastery) &&
               mastery.CurrentTarget == target;
    }

    private void ApplyDisorient(EntityUid target, CorporateJudoMasteryComponent component, TimeSpan duration)
    {
        EnsureComp<Content.Shared.StatusEffect.StatusEffectsComponent>(target);
        _legacyStatusEffects.TryAddStatusEffect<FlashedComponent>(
            target,
            CorporateJudoMasteryComponent.FlashedStatusEffectId,
            duration,
            refresh: false);
        _movement.TryAddMovementSpeedModDuration(target, MovementModStatusSystem.FlashSlowdown, duration, component.FlashSlowTo);
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

    private bool TryExecuteDiscombobulate(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoDiscombobulate(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-discombobulate-attacker-popup",
            "corporate-judo-discombobulate-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteEyePoke(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoEyePoke(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-eye-poke-attacker-popup",
            "corporate-judo-eye-poke-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteJudoThrow(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoJudoThrow(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-throw-attacker-popup",
            "corporate-judo-throw-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteArmbar(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoArmbar(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-armbar-attacker-popup",
            "corporate-judo-armbar-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteWheelThrow(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoWheelThrow(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-wheel-throw-attacker-popup",
            "corporate-judo-wheel-throw-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteGoldenBlast(Entity<CorporateJudoMasteryComponent> ent, EntityUid target)
    {
        if (!DoGoldenBlast(ent.Owner, target, ent.Comp))
            return false;

        PopupTechnique(ent.Owner, target,
            "corporate-judo-golden-blast-attacker-popup",
            "corporate-judo-golden-blast-target-popup",
            includeTargetName: true);
        return true;
    }
}
