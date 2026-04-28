using System;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.KravMaga.Systems;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.KravMaga.Components;

[RegisterComponent, ComponentProtoName("KravMagaMastery")]
[Access(typeof(KravMagaMasterySystem))]
public sealed partial class KravMagaMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string MutedStatusEffectId = "StatusEffectMuted";

    [DataField]
    public int Priority = 7;

    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public ProtoId<DamageTypePrototype> AsphyxiationDamageType = "Asphyxiation";

    [DataField]
    public float UnarmedDamage = 10f;

    [DataField]
    public float DownedTargetBonusDamage = 5f;

    [DataField]
    public float DisarmStealChance = 0.6f;

    [DataField]
    public float LegSweepBluntDamage = 5f;

    [DataField]
    public TimeSpan LegSweepStunDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public float LungPunchAsphyxiationDamage = 10f;

    [DataField]
    public TimeSpan LungPunchHeldBreathDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public float NeckChopBluntDamage = 5f;

    [DataField]
    public TimeSpan NeckChopMuteDuration = TimeSpan.FromSeconds(20);

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new();

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;
}
