using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Shadowling;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShadowlingComponent : Component
{
    [AutoNetworkedField]
    public bool Active = true;
}

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShadowlingThrallComponent : Component
{
    [DataField]
    public EntityUid? Master;

    [DataField]
    public bool Counted = true;

    [AutoNetworkedField]
    public bool Active = true;
}

[RegisterComponent]
public sealed partial class ShadowlingThrallTumorComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingThrallObjectiveComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingSurgeryLightToolComponent : Component
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ShadowlingShadowWalkInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsVisible = true;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ShadowlingShadowWalkPhasedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadowlingShadowWalkSpeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 2.5f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadowlingCameraLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField]
    public Angle OriginalRotation = Angle.Zero;
}

[RegisterComponent]
public sealed partial class ShadowlingCameraLockStatusEffectComponent : Component
{
}
