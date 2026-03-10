using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._OpenSpace.Genetics.Components;


[RegisterComponent]
public sealed partial class PsyResistComponent : Component { }


[RegisterComponent]
public sealed partial class NoPrintsGeneComponent : Component { }


[RegisterComponent]
public sealed partial class NoBreathingGeneComponent : Component
{
    [ViewVariables]
    public bool HadRespirator = true;
}


[RegisterComponent]
public sealed partial class RegenerationGeneComponent : Component { }


[RegisterComponent]
public sealed partial class EpilepsyGeneComponent : Component { }


[RegisterComponent]
public sealed partial class HeatResistanceGeneComponent : Component
{
    [ViewVariables]
    public bool AddedTempProtection;

    [ViewVariables]
    public float OriginalHeating = 1f;

    [ViewVariables]
    public float OriginalCooling = 1f;

    [ViewVariables]
    public bool AddedPressureImmunity;
}


[RegisterComponent]
public sealed partial class ShockImmunityGeneComponent : Component
{
    [ViewVariables]
    public bool AddedInsulation;

    [ViewVariables]
    public float OriginalCoefficient = 1f;
}


[RegisterComponent]
public sealed partial class SoberGeneComponent : Component { }


[RegisterComponent]
public sealed partial class StrongGeneComponent : Component { }


[RegisterComponent]
public sealed partial class MidgetGeneComponent : Component
{
    [ViewVariables]
    public bool Applied;

    [ViewVariables]
    public Vector2 OriginalScale = Vector2.One;

    [ViewVariables]
    public float FixtureScaleFactor = 1f;

    [ViewVariables]
    public int? OriginalCollisionMask;

    [ViewVariables]
    public int? OriginalCollisionLayer;
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CryokinesisComponent : Component
{
    [DataField, ViewVariables]
    public EntProtoId Action = "ActionGeneticCryokinesis";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpathicThoughtComponent : Component
{
    [DataField, ViewVariables]
    public EntProtoId Action = "ActionGeneticEmpathicThought";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MatterEaterComponent : Component
{
    [DataField, ViewVariables]
    public EntProtoId Action = "ActionGeneticMatterEater";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}


[RegisterComponent, NetworkedComponent]
public sealed partial class FarVisionComponent : Component
{
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JumpyComponent : Component
{
    [DataField, ViewVariables]
    public EntProtoId Action = "ActionGeneticJumpy";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public float JumpDistance = 3f;

    [DataField]
    public float JumpThrowSpeed = 20f;
}

