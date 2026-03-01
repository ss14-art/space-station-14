using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Shadowling;

[RegisterComponent]
public sealed partial class ShadowlingLightSensitivityComponent : Component
{
    public TimeSpan NextUpdate;

    [DataField]
    public float UpdateInterval = 1f;

    [DataField]
    public float LightThreshold = 0.2f;

    [DataField]
    public float DarkThreshold = 0.2f;

    [DataField]
    public float BurnDamage = 10f;

    [DataField]
    public float BurnHeal = 10f;

    [DataField]
    public float BruteHeal = 0f;

    [DataField]
    public ProtoId<DamageTypePrototype> HeatType = "Heat";

}
