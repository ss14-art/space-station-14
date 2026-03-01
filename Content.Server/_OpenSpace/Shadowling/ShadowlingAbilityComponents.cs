using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Shadowling;

[RegisterComponent]
public sealed partial class ShadowlingPowersComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingEngageHatchComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingEngageHatch";

    [DataField]
    public EntityUid? ActionEnt;

    [DataField]
    public float TransformSeconds = 30f;

    public List<EntityUid> ActiveWalls = new();
}

[RegisterComponent]
public sealed partial class ShadowlingEnthrallComponent : Component
{
    [DataField]
    public int SonicScreechUnlockCount = 4;

    [DataField]
    public int BlindnessSmokeUnlockCount = 7;

    [DataField]
    public int BlackRecuperationUnlockCount = 10;

    [DataField]
    public int AscendanceUnlockCount = 15;

    [DataField]
    public EntProtoId ActionId = "ActionShadowlingEnthrall";

    [DataField]
    public EntityUid? ActionEnt;

    [DataField]
    public EntProtoId BlackRecuperationActionId = "ActionShadowlingBlackRecuperation";

    [DataField]
    public EntityUid? BlackRecuperationActionEnt;

    [DataField]
    public EntProtoId AscendanceActionId = "ActionShadowlingAscendance";

    [DataField]
    public EntityUid? AscendanceActionEnt;

    [DataField]
    public EntProtoId BlindnessSmokeActionId = "ActionShadowlingBlindnessSmoke";

    [DataField]
    public EntityUid? BlindnessSmokeActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingGlareComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingGlare";

    [DataField]
    public EntityUid? ActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingVeilComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingVeil";

    [DataField]
    public EntityUid? ActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingIcyVeinsComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingIcyVeins";

    [DataField]
    public EntityUid? ActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingShadowWalkComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingShadowWalk";

    [DataField]
    public EntityUid? ActionEnt;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4);

    [DataField]
    public float SpeedMultiplier = 1.7f;
}

[RegisterComponent]
public sealed partial class ShadowlingPlaneShiftComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingPlaneShift";

    [DataField]
    public EntityUid? ActionEnt;

    [DataField]
    public float SpeedMultiplier = 1.7f;
}

[RegisterComponent]
public sealed partial class ShadowlingShadowWalkActiveComponent : Component
{
    [DataField]
    public TimeSpan EndTime;

    [DataField]
    public bool HadGodmode;

    [DataField]
    public bool HadStealth;

    [DataField]
    public float PreviousStealthVisibility;

    [DataField]
    public bool PreviousStealthEnabled;

    [DataField]
    public float SpeedMultiplier = 2.5f;
    public EntityUid? AllowedAction;
}

[RegisterComponent]
public sealed partial class ShadowlingSonicScreechComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingSonicScreech";

    [DataField]
    public EntityUid? ActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingCollectiveMindComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingCollectiveMind";

    [DataField]
    public EntityUid? ActionEnt;
}

[RegisterComponent]
public sealed partial class ShadowlingAscendantComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionShadowlingAnnihilate";

    [DataField]
    public EntityUid? ActionEnt;
}
