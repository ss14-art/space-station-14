using System;
using Content.Server._OpenSpace.Combat.CloseQuarterCombatMastery.Systems;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.CloseQuarterCombatMastery.Components;

[RegisterComponent, ComponentProtoName("CloseQuarterCombatMastery")]
[Access(typeof(CloseQuarterCombatMasterySystem))]
public sealed partial class CloseQuarterCombatMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string SlamTemplateName = "Slam";
    public const string CQCKickTemplateName = "CQCKick";
    public const string RestrainTemplateName = "Restrain";
    public const string PressureTemplateName = "Pressure";
    public const string ConsecutiveCQCTemplateName = "ConsecutiveCQC";

    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public float SlamBluntDamage = 10f;

    [DataField]
    public TimeSpan SlamKnockdownDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float CQCKickBluntDamage = 10f;

    [DataField]
    public float CQCKickStunnedBluntDamage = 15f;

    [DataField]
    public float CQCKickThrowDistance = 1f;

    [DataField]
    public float CQCKickThrowSpeed = 5f;

    [DataField]
    public TimeSpan CQCKickSleepDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public float RestrainStaminaDamage = 30f;

    [DataField]
    public TimeSpan RestrainKnockdownDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public float PressureStaminaDamage = 60f;

    [DataField]
    public float ConsecutiveCqcBluntDamage = 25f;

    [DataField]
    public float ConsecutiveCqcStaminaDamage = 50f;

    [DataField]
    public float UnarmedDamage = 13f;

    [DataField]
    public int Priority = 6;

    [DataField]
    public float UnarmedDownedTargetBonusDamage = 5f;

    [DataField]
    public float ProneAttackerBonusDamage = 10f;

    [DataField]
    public TimeSpan ProneAttackerKnockdownDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float DefensiveMeleeNullifyChance = 0.75f;

    [DataField]
    public TimeSpan DefensiveMeleeCounterKnockdownDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan DefensiveMeleeNullifyWindow = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan RestrainFollowupWindow = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan RestrainFollowupSleepDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public float RestrainFollowupBonusDamageChance = 0.5f;

    [DataField]
    public float RestrainFollowupBluntDamage = 5f;

    [DataField]
    public TimeSpan RestrainFollowupJitterDuration = TimeSpan.FromSeconds(5);

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new()
    {
        Templates =
        [
            new CombatMasteryTemplate
            {
                Name = SlamTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = CQCKickTemplateName,
                Sequence = [ComboMasteryKeys.attack, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = RestrainTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = PressureTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = ConsecutiveCQCTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.disarm, ComboMasteryKeys.attack],
            },
        ],
    };

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;

    [ViewVariables]
    public bool RestrainFollowupReady;

    [ViewVariables]
    public bool SkipNextComboResetForRestrain;

    [ViewVariables]
    public EntityUid? RestrainFollowupTarget;

    [ViewVariables]
    public TimeSpan RestrainFollowupExpireAt;

    [ViewVariables]
    public EntityUid? PendingDefensiveMeleeOrigin;

    [ViewVariables]
    public TimeSpan PendingDefensiveMeleeExpireAt;

    [ViewVariables]
    public bool PendingDefensiveMeleeNullify;

    [ViewVariables]
    public bool PendingDefensiveMeleeNullifyStamina;
}
