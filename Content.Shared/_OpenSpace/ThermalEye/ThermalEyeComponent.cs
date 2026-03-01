using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.ThermalEye;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ThermalEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
