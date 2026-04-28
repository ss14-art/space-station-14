using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.MimeJutsu.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.MimeJutsu.Components;

[RegisterComponent, ComponentProtoName("MimeJutsuMastery")]
[Access(typeof(MimeJutsuMasterySystem))]
public sealed partial class MimeJutsuMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string SilentExecutionTemplateName = "SilentExecution";
    public const string MimechucksTemplateName = "Mimechucks";
    public const string SilencerTemplateName = "Silencer";
    public const string SilentPalmTemplateName = "SilentPalm";

    public const string MutedStatusEffectId = "StatusEffectMuted";

    [DataField]
    public int Priority = 6;

    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public float UnarmedDamage = 15f;

    [DataField]
    public float BasicHitKnockdownChance = 0.3f;

    [DataField]
    public TimeSpan BasicHitKnockdownDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float DisarmSpecialChance = 0.5f;

    [DataField]
    public float DisarmBluntDamage = 5f;

    [DataField]
    public TimeSpan DisarmJitterDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float DisarmJitterAmplitude = 80f;

    [DataField]
    public float DisarmJitterFrequency = 8f;

    [DataField]
    public float DefensiveNullifyChance = 0.5f;

    [DataField]
    public TimeSpan DefensiveNullifyWindow = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan DefensiveCounterStunDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public float SilentExecutionBluntDamage = 40f;

    [DataField]
    public float MimechucksStaminaDamage = 60f;

    [DataField]
    public TimeSpan SilencerKnockdownDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan SilencerMuteDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public float SilentPalmThrowDistance = 4f;

    [DataField]
    public float SilentPalmThrowSpeed = 8f;

    [DataField]
    public float SilentPalmBluntDamage = 20f;

    [DataField]
    public TimeSpan SilentPalmKnockdownDuration = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public EntityUid? PendingDefensiveMeleeOrigin;

    [ViewVariables]
    public TimeSpan PendingDefensiveMeleeExpireAt;

    [ViewVariables]
    public bool PendingDefensiveMeleeNullify;

    [ViewVariables]
    public bool PendingDefensiveMeleeNullifyStamina;

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new()
    {
        Templates =
        [
            new CombatMasteryTemplate
            {
                Name = SilentExecutionTemplateName,
                Sequence =
                [
                    ComboMasteryKeys.attack,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.attack,
                ],
            },
            new CombatMasteryTemplate
            {
                Name = MimechucksTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = SilencerTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.disarm],
            },
            new CombatMasteryTemplate
            {
                Name = SilentPalmTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.attack],
            },
        ],
    };

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;
}
