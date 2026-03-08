using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom; // OpenSpace-Edit

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being pullable by an entity with <see cref="PullerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Systems.PullingSystem))]
public sealed partial class PullableComponent : Component
{
    /// <summary>
    /// The current entity pulling this component.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Puller;

    /// <summary>
    /// The pull joint.
    /// </summary>
    [AutoNetworkedField, DataField]
    public string? PullJointId;
    
    [AutoNetworkedField, DataField]
    public GrabStage PullerGrabStage = GrabStage.None;

    public bool BeingPulled => Puller != null || PullerGrabStage != GrabStage.None; // OpenSpace-Edit

    /// <summary>
    /// If the physics component has FixedRotation should we keep it upon being pulled
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [ViewVariables(VVAccess.ReadWrite), DataField("fixedRotation")]
    public bool FixedRotationOnPull;

    /// <summary>
    /// What the pullable's fixedrotation was set to before being pulled.
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField, DataField]
    public bool PrevFixedRotation;

    [DataField]
    public ProtoId<AlertPrototype> PulledAlert = "Pulled";
    // OpenSpace-Edit Start
    [AutoNetworkedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextBreakAttempt;
}
    // OpenSpace-Edit End
public sealed partial class StopBeingPulledAlertEvent : BaseAlertEvent;
