using System;
using Content.Server._OpenSpace.Combat.CombatMastery;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.CreepingWidow.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.CreepingWidow.Components;

[RegisterComponent, ComponentProtoName("CreepingWidowMastery")]
[Access(typeof(CreepingWidowMasterySystem))]
public sealed partial class CreepingWidowMasteryComponent : Component, ICombatMasteryTemplateProvider
{
    public const string EnergyTornadoTemplateName = "EnergyTornado";
    public const string PalmStrikeTemplateName = "PalmStrike";
    public const string NeckSliceTemplateName = "NeckSlice";
    public const string WrenchWristTemplateName = "WrenchWrist";

    [DataField]
    public int Priority = 8;

    [DataField]
    public ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [DataField]
    public ProtoId<DamageTypePrototype> SlashDamageType = "Slash";

    [DataField]
    public float UnarmedDamage = 10f;

    [DataField]
    public TimeSpan PalmStrikeFocusCooldown = TimeSpan.FromSeconds(50);

    [DataField]
    public TimeSpan EnergyTornadoFocusCooldown = TimeSpan.FromSeconds(100);

    [DataField]
    public TimeSpan NeckSliceFocusCooldown = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan WrenchWristFocusCooldown = TimeSpan.FromSeconds(50);

    [DataField]
    public string FocusFailPopupLoc = "creeping-widow-focus-fail-popup";

    [DataField]
    public float PalmStrikeThrowDistance = 50f;

    [DataField]
    public float PalmStrikeThrowSpeed = 20f;

    [DataField]
    public TimeSpan PalmStrikeKnockdownDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public float EnergyTornadoRange = 1.5f;

    [DataField]
    public float EnergyTornadoThrowDistance = 5f;

    [DataField]
    public float EnergyTornadoThrowSpeed = 12f;

    [DataField]
    public EntProtoId EnergyTornadoSmokePrototype = "Smoke";

    [DataField]
    public float EnergyTornadoSmokeDurationSeconds = 10f;

    [DataField]
    public int EnergyTornadoSmokeSpreadAmount = 5;

    [ViewVariables]
    public TimeSpan NextFocusReadyAt;

    [DataField]
    private CombatMasteryTemplateCollection _templateCollection = new()
    {
        Templates =
        [
            new CombatMasteryTemplate
            {
                Name = EnergyTornadoTemplateName,
                Sequence =
                [
                    ComboMasteryKeys.attack,
                    ComboMasteryKeys.disarm,
                    ComboMasteryKeys.attack,
                    ComboMasteryKeys.attack,
                ],
            },
            new CombatMasteryTemplate
            {
                Name = PalmStrikeTemplateName,
                Sequence = [ComboMasteryKeys.grab, ComboMasteryKeys.disarm],
            },
            new CombatMasteryTemplate
            {
                Name = NeckSliceTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.attack, ComboMasteryKeys.disarm],
            },
            new CombatMasteryTemplate
            {
                Name = WrenchWristTemplateName,
                Sequence = [ComboMasteryKeys.disarm, ComboMasteryKeys.disarm],
            },
        ],
    };

    public CombatMasteryTemplateCollection TemplateCollection => _templateCollection;
    public int StylePriority => Priority;
}
