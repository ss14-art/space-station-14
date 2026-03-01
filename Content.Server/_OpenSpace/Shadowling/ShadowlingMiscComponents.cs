using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._OpenSpace.Shadowling;

[RegisterComponent]
public sealed partial class ShadowlingAntagSoundPlayedComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShadowlingBlindnessSmokeComponent : Component
{
    [DataField]
    public float HealPerSecond = 4f;
}

[RegisterComponent]
public sealed partial class ShadowlingBlindnessSmokeAffectedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextHeal;
}

[RegisterComponent]
public sealed partial class ShadowlingHiveComponent : Component
{
    public readonly HashSet<EntityUid> Thralls = new();
    public int AliveThralls;
}

