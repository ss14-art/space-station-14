namespace Content.Server._OpenSpace.Shadowling.Objectives;

[RegisterComponent]
public sealed partial class ShadowlingAscendanceObjectiveComponent : Component
{
    [DataField]
    public int RequiredThralls = 15;
}
