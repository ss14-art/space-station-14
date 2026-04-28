namespace Content.Shared._OpenSpace.Combat.CombatMastery.Events;

[ByRefEvent]
public record struct CombatDisarmAttemptedEvent(EntityUid User, EntityUid Target, bool Cancelled = false);
