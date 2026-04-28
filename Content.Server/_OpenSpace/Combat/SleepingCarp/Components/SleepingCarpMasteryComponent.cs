using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.SleepingCarp.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Weapons.Reflect;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.SleepingCarp.Components;

[RegisterComponent, ComponentProtoName("SleepingCarpMastery")]
[Access(typeof(SleepingCarpMasterySystem))]
public sealed partial class SleepingCarpMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string WristWrenchTemplateName = "WristWrench";
    public const string BackKickTemplateName = "BackKick";
    public const string StomachKheeTemplateName = "StomachKhee";
    public const string HeadKickTemplateName = "HeadKick";
    public const string ElbowDropTemplateName = "ElbowDrop";

    [DataField]
    public int Priority = 9;

    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";

    [DataField]
    public float BaseUnarmedDamage = 10f;

    [DataField]
    public float RandomUnarmedBonusDamage = 5f;

    [DataField]
    public TimeSpan BasicHitKnockdownDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public float WristWrenchBluntDamage = 5f;

    [DataField]
    public TimeSpan BackKickKnockdownDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public float HeadKickBluntDamage = 20f;

    [DataField]
    public float StomachKheeStaminaDamage = 15f;

    [DataField]
    public TimeSpan StomachKheeKnockdownDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan StomachKheeStutterDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public float ElbowDropBluntDamage = 50f;

    [DataField]
    public float DeflectionChance = 1f;

    [DataField]
    public Angle DeflectionSpread = Angle.FromDegrees(45);

    [DataField]
    public string NoGunsPopupLoc = "sleeping-carp-no-guns-popup";

    [DataField]
    public TimeSpan NoGunsPopupCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public bool AddedReflectComponent;

    [ViewVariables]
    public bool ReflectionStateCaptured;

    [ViewVariables]
    public ReflectType OriginalReflects = ReflectType.None;

    [ViewVariables]
    public float OriginalReflectProb;

    [ViewVariables]
    public Angle OriginalSpread;

    [ViewVariables]
    public bool OriginalReflectInHands;

    [ViewVariables]
    public bool OriginalReflectInRightPlace;

    [ViewVariables]
    public bool OriginalReflectShowExamineInfo = true;

    [ViewVariables]
    public TimeSpan NextNoGunsPopup;

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new()
    {
        Templates =
        [
            new CombatMasteryTemplate
            {
                Name = WristWrenchTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.disarm],
            },
            new CombatMasteryTemplate
            {
                Name = BackKickTemplateName,
                Sequence = [ComboMasteryKeys.attack, ComboMasteryKeys.grab],
            },
            new CombatMasteryTemplate
            {
                Name = StomachKheeTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = HeadKickTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.attack, ComboMasteryKeys.attack],
            },
            new CombatMasteryTemplate
            {
                Name = ElbowDropTemplateName,
                Sequence =
                [
                    ComboMasteryKeys.attack,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.attack,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.attack,
                ],
            },
        ],
    };

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;
}
