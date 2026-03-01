namespace Content.Server._OpenSpace.Shadowling;

[RegisterComponent]
public sealed partial class ShadowlingVeiledPoweredLightComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingVeiledHandheldLightComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingVeiledBorgLightComponent : Component
{
    [DataField]
    public TimeSpan EndTime;
}
