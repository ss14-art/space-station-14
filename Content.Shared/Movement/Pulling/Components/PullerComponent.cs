using Content.Shared.Alert;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being able to pull another entity with <see cref="PullableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(PullingSystem))]
public sealed partial class PullerComponent : Component
{
    // My raiding guild
    /// <summary>
    /// Next time the puller can throw what is being pulled.
    /// Used to avoid spamming it for infinite spin + velocity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextThrow;

    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextMediumGrab;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextHeavyGrab;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextChokeGrab;

    [DataField]
    public TimeSpan MediumGrabCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan HeavyGrabCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ChokeGrabCooldown = TimeSpan.FromSeconds(2);

    [AutoNetworkedField, DataField]
    public GrabStage GrabStage = GrabStage.None;

    [DataField]
    public TimeSpan LightGrabDelay = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : GrabStage switch
    {
        GrabStage.Medium => 0.70f,
        GrabStage.Heavy => 0.60f,
        GrabStage.Choke => 0.20f,
        _ => 0.95f
    };

    public float SprintSpeedModifier => Pulling == default ? 1.0f : GrabStage switch
    {
        GrabStage.Medium => 0.70f,
        GrabStage.Heavy => 0.60f,
        GrabStage.Choke => 0.20f,
        _ => 0.95f
    };

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Pulling;

    /// <summary>
    ///     Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField]
    public bool NeedsHands = true;

    [DataField]
    public ProtoId<AlertPrototype> PullingAlert = "Pulling";
}

public sealed partial class StopPullingAlertEvent : BaseAlertEvent;

public enum GrabStage : byte
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Choke = 4
}
