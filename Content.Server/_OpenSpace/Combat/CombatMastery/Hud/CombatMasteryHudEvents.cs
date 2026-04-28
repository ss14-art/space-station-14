namespace Content.Server._OpenSpace.Combat.CombatMastery.Hud;

[ByRefEvent]
public sealed class CombatMasteryActiveStateQueryEvent : EntityEventArgs
{
    public bool HasActiveMastery { get; set; }
}

[ByRefEvent]
public sealed class CombatMasteryHighestPriorityQueryEvent : EntityEventArgs
{
    public int HighestPriority { get; set; } = int.MinValue;
}

[ByRefEvent]
public sealed class CombatMasteryHudRefreshEvent : EntityEventArgs;
