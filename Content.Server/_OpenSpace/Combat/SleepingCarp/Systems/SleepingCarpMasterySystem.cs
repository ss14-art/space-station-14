using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Server._OpenSpace.Combat.SleepingCarp.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Combat.SleepingCarp.Systems;

public sealed class SleepingCarpMasterySystem : CombatMasteryTechniqueSystem<SleepingCarpMasteryComponent>
{
    private static readonly string[] WristWrenchShoutLocs =
    [
        "sleeping-carp-shout-wristy-twirly",
        "sleeping-carp-shout-we-fight-like-men",
        "sleeping-carp-shout-you-dishonor-yourself",
        "sleeping-carp-shout-pohyah",
        "sleeping-carp-shout-where-is-your-baton-now",
        "sleeping-carp-shout-say-uncle",
    ];

    private static readonly string[] BackKickShoutLocs =
    [
        "sleeping-carp-shout-surprizu",
        "sleeping-carp-shout-back-strike",
        "sleeping-carp-shout-wopah",
        "sleeping-carp-shout-wataah",
        "sleeping-carp-shout-zota",
        "sleeping-carp-shout-never-turn-your-back",
    ];

    private static readonly string[] StomachKheeShoutLocs =
    [
        "sleeping-carp-shout-hwop",
        "sleeping-carp-shout-kuh",
        "sleeping-carp-shout-yakuuh",
        "sleeping-carp-shout-kyuh",
        "sleeping-carp-shout-kneestrike",
    ];

    private static readonly string[] HeadKickShoutLocs =
    [
        "sleeping-carp-shout-oohyoo",
        "sleeping-carp-shout-oopyah",
        "sleeping-carp-shout-hyooaa",
        "sleeping-carp-shout-wooaaa",
        "sleeping-carp-shout-shuryukick",
        "sleeping-carp-shout-hiyah",
    ];

    private static readonly string[] ElbowDropShoutLocs =
    [
        "sleeping-carp-shout-banzaiii",
        "sleeping-carp-shout-kiyaaaa",
        "sleeping-carp-shout-omae-wa-mou-shindeiru",
        "sleeping-carp-shout-you-cant-see-me",
        "sleeping-carp-shout-my-time-is-now",
        "sleeping-carp-shout-cowabunga",
    ];

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleepingCarpMasteryComponent, CombatMasteryCollectMeleeDamageEvent>(OnCollectMeleeDamage);
        SubscribeLocalEvent<SleepingCarpMasteryComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<SleepingCarpMasteryComponent, CombatMasteryMeleeAttackedEvent>(OnMeleeAttacked);
    }

    protected override void OnMasteryStarted(Entity<SleepingCarpMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
        SyncReflectState(ent);
    }

    protected override void OnMasteryStopped(Entity<SleepingCarpMasteryComponent> ent)
    {
        RequestMeleeDamageRefresh(ent.Owner);
        RestoreReflectState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SleepingCarpMasteryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            SyncReflectState((uid, comp));
        }
    }

    private void OnCollectMeleeDamage(Entity<SleepingCarpMasteryComponent> ent, ref CombatMasteryCollectMeleeDamageEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        args.ConsiderDamage(ent.Comp.BaseUnarmedDamage);
    }

    private void OnShotAttempted(Entity<SleepingCarpMasteryComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.User != ent.Owner || !IsMasteryActive(ent))
            return;

        args.Cancel();

        if (_timing.CurTime < ent.Comp.NextNoGunsPopup)
            return;

        ent.Comp.NextNoGunsPopup = _timing.CurTime + ent.Comp.NoGunsPopupCooldown;
        _popup.PopupEntity(Loc.GetString(ent.Comp.NoGunsPopupLoc), ent.Owner, ent.Owner);
    }

    private void OnMeleeAttacked(Entity<SleepingCarpMasteryComponent> ent, ref CombatMasteryMeleeAttackedEvent args)
    {
        if (args.Attacker != ent.Owner ||
            !IsMasteryActive(ent) ||
            !IsUnarmedMeleeAttack(args))
        {
            return;
        }

        var randomBonus = _random.NextFloat(0f, ent.Comp.RandomUnarmedBonusDamage);
        if (randomBonus > 0f)
            args.Attack.BonusDamage += CreateBluntDamage(ent.Comp.BluntDamageType, randomBonus);

        if (IsEntityDown(args.Target))
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var targetDamage))
            return;

        var bruteDamage = _damageable.GetDamage((args.Target, targetDamage), ent.Comp.BruteDamageGroup).GetTotal().Float();
        var knockdownChance = Math.Clamp(bruteDamage / 100f, 0f, 1f);
        if (!_random.Prob(knockdownChance))
            return;

        _stun.TryKnockdown(args.Target,
            ent.Comp.BasicHitKnockdownDuration,
            refresh: true,
            autoStand: true,
            drop: true,
            force: true,
            voluntary: false);
    }

    protected override bool OnTemplateMatched(Entity<SleepingCarpMasteryComponent> ent, EntityUid target, CombatMasteryTemplate template)
    {
        return template.Name switch
        {
            SleepingCarpMasteryComponent.WristWrenchTemplateName => TryExecuteWristWrench(ent, target),
            SleepingCarpMasteryComponent.BackKickTemplateName => TryExecuteBackKick(ent, target),
            SleepingCarpMasteryComponent.StomachKheeTemplateName => TryExecuteStomachKhee(ent, target),
            SleepingCarpMasteryComponent.HeadKickTemplateName => TryExecuteHeadKick(ent, target),
            SleepingCarpMasteryComponent.ElbowDropTemplateName => TryExecuteElbowDrop(ent, target),
            _ => false
        };
    }

    private bool TryExecuteWristWrench(Entity<SleepingCarpMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechnique(target))
            return false;

        ApplyBluntDamage(ent.Owner, target, ent.Comp.BluntDamageType, ent.Comp.WristWrenchBluntDamage);
        TryForceDropActiveItem(target);
        PopupTechnique(ent.Owner, target,
            "sleeping-carp-wrist-wrench-attacker-popup",
            "sleeping-carp-wrist-wrench-target-popup",
            includeTargetName: true);
        TryShout(ent.Owner, 0.6f, WristWrenchShoutLocs);
        return true;
    }

    private bool TryExecuteBackKick(Entity<SleepingCarpMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechnique(target))
            return false;

        ApplySlipLikeStun(target, ent.Comp.BackKickKnockdownDuration);
        PopupTechnique(ent.Owner, target,
            "sleeping-carp-back-kick-attacker-popup",
            "sleeping-carp-back-kick-target-popup",
            includeTargetName: true);
        TryShout(ent.Owner, 0.8f, BackKickShoutLocs);
        return true;
    }

    private bool TryExecuteStomachKhee(Entity<SleepingCarpMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechnique(target))
            return false;

        ApplySlipLikeStun(target, ent.Comp.StomachKheeKnockdownDuration);
        _stamina.TakeStaminaDamage(target, ent.Comp.StomachKheeStaminaDamage, source: ent.Owner);
        _stuttering.DoStutter(target, ent.Comp.StomachKheeStutterDuration, refresh: true);
        PopupTechnique(ent.Owner, target,
            "sleeping-carp-stomach-khee-attacker-popup",
            "sleeping-carp-stomach-khee-target-popup",
            includeTargetName: true);
        TryShout(ent.Owner, 0.8f, StomachKheeShoutLocs);
        return true;
    }

    private bool TryExecuteHeadKick(Entity<SleepingCarpMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseStandingTechnique(target))
            return false;

        ApplyBluntDamage(ent.Owner, target, ent.Comp.BluntDamageType, ent.Comp.HeadKickBluntDamage);
        TryForceDropActiveItem(target);
        PopupTechnique(ent.Owner, target,
            "sleeping-carp-head-kick-attacker-popup",
            "sleeping-carp-head-kick-target-popup",
            includeTargetName: true);
        TryShout(ent.Owner, 0.6f, HeadKickShoutLocs);
        return true;
    }

    private bool TryExecuteElbowDrop(Entity<SleepingCarpMasteryComponent> ent, EntityUid target)
    {
        if (!CanUseElbowDrop(target))
            return false;

        // Paradise behavior: base elbow drop damage goes through normal mitigation,
        // while extra finisher damage against critical targets ignores armor.
        ApplyBluntDamage(ent.Owner,
            target,
            ent.Comp.BluntDamageType,
            ent.Comp.ElbowDropBluntDamage,
            ignoreResistances: false);

        if (_mobState.IsCritical(target) &&
            TryComp<DamageableComponent>(target, out var damageable) &&
            _mobThreshold.TryGetDeadThreshold(target, out var deadThreshold))
        {
            var remainingToDeath = deadThreshold.Value.Float() - damageable.TotalDamage.Float();
            if (remainingToDeath > 0f)
            {
                ApplyBluntDamage(ent.Owner,
                    target,
                    ent.Comp.BluntDamageType,
                    remainingToDeath,
                    ignoreResistances: true);
            }
        }

        PopupTechnique(ent.Owner, target,
            "sleeping-carp-elbow-drop-attacker-popup",
            "sleeping-carp-elbow-drop-target-popup",
            includeTargetName: true);
        TryShout(ent.Owner, 0.8f, ElbowDropShoutLocs);
        return true;
    }

    private void TryShout(EntityUid user, float chance, string[] locKeys)
    {
        if (locKeys.Length == 0 || !_random.Prob(chance))
            return;

        var phrase = Loc.GetString(_random.Pick(locKeys));
        _chat.TrySendInGameICMessage(user,
            phrase,
            InGameICChatType.Speak,
            hideChat: false,
            ignoreActionBlocker: true);
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

    private void TryForceDropActiveItem(EntityUid target)
    {
        if (!_hands.TryGetActiveItem(target, out var item) || HasComp<VirtualItemComponent>(item.Value))
            return;

        _hands.TryDrop(target, item.Value, checkActionBlocker: false, doDropInteraction: false);
    }

    private bool CanUseStandingTechnique(EntityUid target)
    {
        return !TerminatingOrDeleted(target) &&
               !_mobState.IsDead(target) &&
               !_mobState.IsCritical(target) &&
               !IsEntityDown(target) &&
               !HasComp<StunnedComponent>(target);
    }

    private bool CanUseElbowDrop(EntityUid target)
    {
        return !TerminatingOrDeleted(target) &&
               (_mobState.IsDead(target) || _mobState.IsCritical(target) || IsEntityDown(target));
    }

    private static bool IsUnarmedMeleeAttack(CombatMasteryMeleeAttackedEvent args)
    {
        return args.Attack.Used == args.Attack.User;
    }

    private void ApplyBluntDamage(
        EntityUid user,
        EntityUid target,
        ProtoId<DamageTypePrototype> damageType,
        float amount,
        bool ignoreResistances)
    {
        if (!TryComp<DamageableComponent>(target, out _))
            return;

        _damageable.TryChangeDamage(target,
            CreateBluntDamage(damageType, amount),
            ignoreResistances: ignoreResistances,
            origin: user);
    }

    private void SyncReflectState(Entity<SleepingCarpMasteryComponent> ent)
    {
        if (IsMasteryActive(ent))
        {
            ApplyReflectState(ent);
            return;
        }

        RestoreReflectState(ent);
    }

    private void ApplyReflectState(Entity<SleepingCarpMasteryComponent> ent)
    {
        var reflect = EnsureReflectComponent(ent);
        if (reflect == null)
            return;

        var changed = false;

        if (reflect.Reflects != (ReflectType.Energy | ReflectType.NonEnergy))
        {
            reflect.Reflects = ReflectType.Energy | ReflectType.NonEnergy;
            changed = true;
        }

        if (!MathHelper.CloseToPercent(reflect.ReflectProb, ent.Comp.DeflectionChance))
        {
            reflect.ReflectProb = ent.Comp.DeflectionChance;
            changed = true;
        }

        if (!reflect.Spread.EqualsApprox(ent.Comp.DeflectionSpread))
        {
            reflect.Spread = ent.Comp.DeflectionSpread;
            changed = true;
        }

        if (!reflect.ReflectingInHands)
        {
            reflect.ReflectingInHands = true;
            changed = true;
        }

        if (!reflect.InRightPlace)
        {
            reflect.InRightPlace = true;
            changed = true;
        }

        if (reflect.ShowExamineInfo)
        {
            reflect.ShowExamineInfo = false;
            changed = true;
        }

        if (changed)
            Dirty(ent.Owner, reflect);
    }

    private ReflectComponent? EnsureReflectComponent(Entity<SleepingCarpMasteryComponent> ent)
    {
        if (TryComp<ReflectComponent>(ent.Owner, out var reflect))
        {
            if (!ent.Comp.ReflectionStateCaptured)
            {
                ent.Comp.OriginalReflects = reflect.Reflects;
                ent.Comp.OriginalReflectProb = reflect.ReflectProb;
                ent.Comp.OriginalSpread = reflect.Spread;
                ent.Comp.OriginalReflectInHands = reflect.ReflectingInHands;
                ent.Comp.OriginalReflectInRightPlace = reflect.InRightPlace;
                ent.Comp.OriginalReflectShowExamineInfo = reflect.ShowExamineInfo;
                ent.Comp.ReflectionStateCaptured = true;
                ent.Comp.AddedReflectComponent = false;
            }

            return reflect;
        }

        reflect = EnsureComp<ReflectComponent>(ent.Owner);
        ent.Comp.AddedReflectComponent = true;
        ent.Comp.ReflectionStateCaptured = true;
        return reflect;
    }

    private void RestoreReflectState(Entity<SleepingCarpMasteryComponent> ent)
    {
        if (!ent.Comp.ReflectionStateCaptured)
            return;

        if (ent.Comp.AddedReflectComponent)
        {
            RemComp<ReflectComponent>(ent.Owner);
            ent.Comp.AddedReflectComponent = false;
            ent.Comp.ReflectionStateCaptured = false;
            return;
        }

        if (TryComp<ReflectComponent>(ent.Owner, out var reflect))
        {
            reflect.Reflects = ent.Comp.OriginalReflects;
            reflect.ReflectProb = ent.Comp.OriginalReflectProb;
            reflect.Spread = ent.Comp.OriginalSpread;
            reflect.ReflectingInHands = ent.Comp.OriginalReflectInHands;
            reflect.InRightPlace = ent.Comp.OriginalReflectInRightPlace;
            reflect.ShowExamineInfo = ent.Comp.OriginalReflectShowExamineInfo;
            Dirty(ent.Owner, reflect);
        }

        ent.Comp.ReflectionStateCaptured = false;
        ent.Comp.AddedReflectComponent = false;
    }
}
