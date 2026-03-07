using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Genetics.Telekinesis;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelekinesisThrownComponent : Component
{
    [DataField]
    public EntityUid? Thrower;
}

