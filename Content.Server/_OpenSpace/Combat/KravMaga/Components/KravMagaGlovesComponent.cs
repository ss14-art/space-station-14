using Content.Server._OpenSpace.Combat.KravMaga.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.KravMaga.Components;

[RegisterComponent]
[Access(typeof(KravMagaGlovesSystem))]
public sealed partial class KravMagaGlovesComponent : Component
{
    [DataField]
    public EntProtoId LegSweepAction = "ActionKravMagaLegSweep";

    [DataField]
    public EntProtoId LungPunchAction = "ActionKravMagaLungPunch";

    [DataField]
    public EntProtoId NeckChopAction = "ActionKravMagaNeckChop";

    [DataField]
    public EntityUid? LegSweepActionEntity;

    [DataField]
    public EntityUid? LungPunchActionEntity;

    [DataField]
    public EntityUid? NeckChopActionEntity;

    [ViewVariables]
    public bool AddedMasteryToWearer;
}
