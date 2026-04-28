using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.CorporateJudo.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.CorporateJudo.Components;

[RegisterComponent, ComponentProtoName("CorporateJudoMastery")]
[Access(typeof(CorporateJudoMasterySystem))]
public sealed partial class CorporateJudoMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string DiscombobulateTemplateName = "Discombobulate";
    public const string EyePokeTemplateName = "EyePoke";
    public const string JudoThrowTemplateName = "JudoThrow";
    public const string ArmbarTemplateName = "Armbar";
    public const string WheelThrowTemplateName = "WheelThrow";
    public const string GoldenBlastTemplateName = "GoldenBlast";

    public const string FlashedStatusEffectId = "Flashed";
    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public float UnarmedDamage = 10f;

    [DataField]
    public float DiscombobulateStaminaDamage = 10f;

    [DataField]
    public TimeSpan DiscombobulateDisorientDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public float EyePokeBluntDamage = 10f;

    [DataField]
    public TimeSpan EyePokeBlindDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float JudoThrowStaminaDamage = 25f;

    [DataField]
    public TimeSpan JudoThrowKnockdownDuration = TimeSpan.FromSeconds(7);

    [DataField]
    public float ArmbarStaminaDamage = 45f;

    [DataField]
    public TimeSpan ArmbarStunDuration = TimeSpan.FromSeconds(7);

    [DataField]
    public float WheelThrowStaminaDamage = 100f;

    [DataField]
    public TimeSpan WheelThrowStunDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan WheelThrowDisorientDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan GoldenBlastStunDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan GoldenBlastDisorientDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public float FlashSlowTo = 0.8f;

    [DataField]
    public int Priority = 5;

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new()
    {
        Templates =
        [
            new CombatMasteryTemplate
            {
                Name = DiscombobulateTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = EyePokeTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = JudoThrowTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.disarm],
            },
            new CombatMasteryTemplate
            {
                Name = ArmbarTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.disarm, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = WheelThrowTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.disarm, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = GoldenBlastTemplateName,
                Sequence =
                [
                    ComboMasteryKeys.help,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.help,
                    ComboMasteryKeys.grab,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.grab,
                    ComboMasteryKeys.help,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.grab,
                    ComboMasteryKeys.help,
                ],
            },
        ],
    };

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;
}
