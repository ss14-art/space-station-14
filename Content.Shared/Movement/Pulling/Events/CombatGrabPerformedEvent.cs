using Content.Shared.Movement.Pulling.Components;

namespace Content.Shared.Movement.Pulling.Events;

[ByRefEvent]
public sealed class CombatGrabPerformedEvent(EntityUid pullerUid, EntityUid targetUid, GrabStage stage) : EntityEventArgs
{
    public EntityUid PullerUid { get; } = pullerUid;
    public EntityUid TargetUid { get; } = targetUid;
    public GrabStage Stage { get; } = stage;
    public bool SuppressPopup { get; set; }
}
