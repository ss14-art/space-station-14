using System.Numerics;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Server._OpenSpace.Combat.MimeJutsu.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Content.Shared._OpenSpace.Combat.CombatMastery.Events;
using Content.Shared._Starlight.Combat.Disarming;
using Content.Shared._Starlight.Medical.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Combat.MimeJutsu.Systems;

public sealed class MimeJutsuMasterySystem : CombatMasteryTechniqueSystem<MimeJutsuMasteryComponent>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimeJutsuMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
        SubscribeLocalEvent<MimeJutsuMasteryComponent, CombatDisarmAttemptedEvent>(OnCombatDisarmAttempted);
        SubscribeLocalEvent<MimeJutsuMasteryComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<MimeJutsuMasteryComponent, DamageBeforeApplyEvent>(OnDamageBeforeApply);
    }

    protected override void OnMasteryStarted(Entity<MimeJutsuMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
    }

    protected override void OnMasteryStopped(Entity<MimeJutsuMasteryComponent> ent)
    {
        ResetPendingDefensiveNullify(ent.Comp);
        RequestMeleeDamageRefresh(ent.Owner);
    }

    private void OnCollectMeleeDamage(Entity<MimeJutsuMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.UnarmedDamage);
    }

    protected override void OnComboUpdated(Entity<MimeJutsuMasteryComponent> ent, ref CombatMasteryComboUpdatedEvent args)
    {
        base.OnComboUpdated(ent, ref args);

        if (args.Step != ComboMasteryKeys.attack ||
            args.TemplateExecuted ||
            !IsMasteryActive(ent) ||
            _mobState.IsDead(args.Target) ||
            !_random.Prob(ent.Comp.BasicHitKnockdownChance))
        {
            return;
        }

        _stun.TryKnockdown(args.Target,
            ent.Comp.BasicHitKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);
    }

    private void OnCombatDisarmAttempted(Entity<MimeJutsuMasteryComponent> ent, ref CombatDisarmAttemptedEvent args)
    {
        if (args.User != ent.Owner ||
            !IsMasteryActive(ent) ||
            !_random.Prob(ent.Comp.DisarmSpecialChance))
        {
            return;
        }

        ApplyBluntDamage(ent.Owner, args.Target, ent.Comp.BluntDamageType, ent.Comp.DisarmBluntDamage);
        _jittering.DoJitter(args.Target,
            ent.Comp.DisarmJitterDuration,
            refresh: true,
            amplitude: ent.Comp.DisarmJitterAmplitude,
            frequency: ent.Comp.DisarmJitterFrequency,
            forceValueChange: true);
        TransferTargetActiveItemToAttacker(ent.Owner, args.Target);
    }

    private void OnBeforeStaminaDamage(Entity<MimeJutsuMasteryComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (!IsMasteryActive(ent) || !ent.Comp.PendingDefensiveMeleeNullifyStamina)
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

    private void OnDamageBeforeApply(Entity<MimeJutsuMasteryComponent> ent, ref DamageBeforeApplyEvent args)
    {
        if (!IsMasteryActive(ent))
        {
            ResetPendingDefensiveNullify(ent.Comp);
            return;
        }

        if (args.Origin is not { } origin)
            return;

        if (!ent.Comp.PendingDefensiveMeleeNullify &&
            !ent.Comp.PendingDefensiveMeleeNullifyStamina)
        {
            if (!_random.Prob(ent.Comp.DefensiveNullifyChance))
                return;

            SetPendingDefensiveNullify(ent.Comp, origin, nullifyDamage: true, nullifyStamina: true);
            ApplyDefensiveCounter(ent.Comp, origin);
        }

        if (IsPendingDefensiveNullifyExpired(ent.Comp))
        {
            ResetPendingDefensiveNullify(ent.Comp);
            return;
        }

        if (origin != ent.Comp.PendingDefensiveMeleeOrigin || !ent.Comp.PendingDefensiveMeleeNullify)
            return;

        args.Damage = new DamageSpecifier();

        ent.Comp.PendingDefensiveMeleeNullify = false;
        ClearPendingDefensiveNullifyIfUnused(ent.Comp);
    }

    protected override bool OnTemplateMatched(Entity<MimeJutsuMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        return template.Name switch
        {
            MimeJutsuMasteryComponent.SilentExecutionTemplateName => TryExecuteSilentExecution(ent, target),
            MimeJutsuMasteryComponent.MimechucksTemplateName => TryExecuteMimechucks(ent, target),
            MimeJutsuMasteryComponent.SilencerTemplateName => TryExecuteSilencer(ent, target),
            MimeJutsuMasteryComponent.SilentPalmTemplateName => TryExecuteSilentPalm(ent, target),
            _ => false
        };
    }

    private bool TryExecuteSilentExecution(Entity<MimeJutsuMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseTechniqueTarget(ent.Owner, target))
            return false;

        ApplyBluntDamage(ent.Owner, target, ent.Comp.BluntDamageType, ent.Comp.SilentExecutionBluntDamage);
        PopupTechnique(ent.Owner, target,
            "mime-jutsu-silent-execution-attacker-popup",
            "mime-jutsu-silent-execution-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteMimechucks(Entity<MimeJutsuMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechniqueTarget(ent.Owner, target))
            return false;

        _stamina.TakeStaminaDamage(target, ent.Comp.MimechucksStaminaDamage, source: ent.Owner);
        PopupTechnique(ent.Owner, target,
            "mime-jutsu-mimechucks-attacker-popup",
            "mime-jutsu-mimechucks-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteSilencer(Entity<MimeJutsuMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechniqueTarget(ent.Owner, target))
            return false;

        _stun.TryKnockdown(target,
            ent.Comp.SilencerKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);
        _statusEffects.TryAddStatusEffectDuration(target,
            MimeJutsuMasteryComponent.MutedStatusEffectId,
            ent.Comp.SilencerMuteDuration);
        PopupTechnique(ent.Owner, target,
            "mime-jutsu-silencer-attacker-popup",
            "mime-jutsu-silencer-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool TryExecuteSilentPalm(Entity<MimeJutsuMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechniqueTarget(ent.Owner, target))
            return false;

        ThrowAwayFromUser(ent.Owner, target, ent.Comp.SilentPalmThrowDistance, ent.Comp.SilentPalmThrowSpeed);
        ApplyBluntDamage(ent.Owner, target, ent.Comp.BluntDamageType, ent.Comp.SilentPalmBluntDamage);
        _stun.TryKnockdown(target,
            ent.Comp.SilentPalmKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);
        PopupTechnique(ent.Owner, target,
            "mime-jutsu-silent-palm-attacker-popup",
            "mime-jutsu-silent-palm-target-popup",
            includeTargetName: true);
        return true;
    }

    private bool CanUseTechniqueTarget(EntityUid user, EntityUid target)
    {
        return target != user &&
               !TerminatingOrDeleted(target) &&
               !_mobState.IsDead(target);
    }

    private bool CanUseStandingTechniqueTarget(EntityUid user, EntityUid target)
    {
        return CanUseTechniqueTarget(user, target) &&
               !_mobState.IsCritical(target) &&
               !IsEntityDown(target) &&
               !HasComp<StunnedComponent>(target);
    }

    private void TransferTargetActiveItemToAttacker(EntityUid attacker, EntityUid target)
    {
        if (!_hands.TryGetActiveItem(target, out var activeItem) ||
            HasComp<VirtualItemComponent>(activeItem.Value) ||
            HasComp<NoDisarmComponent>(activeItem.Value) ||
            !_hands.TryDrop(target, activeItem.Value, checkActionBlocker: false, doDropInteraction: false))
        {
            return;
        }

        var activeHand = _hands.GetActiveHand(attacker);
        if (activeHand != null &&
            _hands.TryForcePickup((attacker, null), activeItem.Value, activeHand, checkActionBlocker: false, animate: false))
        {
            return;
        }

        _hands.PickupOrDrop(attacker, activeItem.Value, checkActionBlocker: false, animate: false, dropNear: true);
    }

    private bool HasRealActiveItem(EntityUid user)
    {
        return _hands.TryGetActiveItem(user, out var held) &&
               !HasComp<VirtualItemComponent>(held.Value);
    }

    private void SetPendingDefensiveNullify(
        MimeJutsuMasteryComponent component,
        EntityUid origin,
        bool nullifyDamage,
        bool nullifyStamina)
    {
        component.PendingDefensiveMeleeOrigin = origin;
        component.PendingDefensiveMeleeExpireAt = _timing.CurTime + component.DefensiveNullifyWindow;
        component.PendingDefensiveMeleeNullify = nullifyDamage;
        component.PendingDefensiveMeleeNullifyStamina = nullifyStamina;
    }

    private void ApplyDefensiveCounter(MimeJutsuMasteryComponent component, EntityUid attacker)
    {
        if (!HasRealActiveItem(attacker))
            return;

        _stun.TryUpdateParalyzeDuration(attacker, component.DefensiveCounterStunDuration);
    }

    private bool IsPendingDefensiveNullifyExpired(MimeJutsuMasteryComponent component)
    {
        return _timing.CurTime > component.PendingDefensiveMeleeExpireAt;
    }

    private static void ResetPendingDefensiveNullify(MimeJutsuMasteryComponent component)
    {
        component.PendingDefensiveMeleeOrigin = null;
        component.PendingDefensiveMeleeExpireAt = default;
        component.PendingDefensiveMeleeNullify = false;
        component.PendingDefensiveMeleeNullifyStamina = false;
    }

    private static void ClearPendingDefensiveNullifyIfUnused(MimeJutsuMasteryComponent component)
    {
        if (component.PendingDefensiveMeleeNullify || component.PendingDefensiveMeleeNullifyStamina)
            return;

        component.PendingDefensiveMeleeOrigin = null;
        component.PendingDefensiveMeleeExpireAt = default;
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

        _throwing.TryThrow(target,
            direction.Normalized() * distance,
            speed,
            user,
            compensateFriction: true,
            doSpin: false);
    }

}
