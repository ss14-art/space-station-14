using Robust.Shared.Maths;
using Robust.Shared.Physics;
using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Movement.Pulling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChokedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? OldParent;
    [DataField, AutoNetworkedField]
    public bool HadOldParent;

    [DataField, AutoNetworkedField]
    public bool AddedMuted;

    [DataField, AutoNetworkedField]
    public bool HadPhysics;
    [DataField, AutoNetworkedField]
    public BodyType OldBodyType;
    [DataField, AutoNetworkedField]
    public bool OldCanCollide;

    [DataField, AutoNetworkedField]
    public EntityUid? Puller;
    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    [DataField, AutoNetworkedField]
    public Angle LockedWorldRotation;
}



