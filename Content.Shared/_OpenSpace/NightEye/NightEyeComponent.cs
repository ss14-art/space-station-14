using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.NightEye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
