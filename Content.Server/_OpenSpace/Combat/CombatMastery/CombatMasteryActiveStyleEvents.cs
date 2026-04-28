namespace Content.Server._OpenSpace.Combat.CombatMastery;

[ByRefEvent]
public sealed class CombatMasteryRefreshActiveStyleEvent : EntityEventArgs
{
    public string? IgnoredStyleId { get; }

    public CombatMasteryRefreshActiveStyleEvent(string? ignoredStyleId = null)
    {
        IgnoredStyleId = ignoredStyleId;
    }
}

[ByRefEvent]
public sealed class CombatMasterySelectActiveStyleEvent : EntityEventArgs
{
    public string? PreferredStyleId { get; }
    public string? IgnoredStyleId { get; }
    public string? SelectedStyleId { get; private set; }
    public int SelectedPriority { get; private set; } = int.MinValue;

    public CombatMasterySelectActiveStyleEvent(string? preferredStyleId, string? ignoredStyleId = null)
    {
        PreferredStyleId = preferredStyleId;
        IgnoredStyleId = ignoredStyleId;
    }

    public void ConsiderStyle(string styleId, int priority)
    {
        if (priority <= 0 || styleId == IgnoredStyleId)
            return;

        if (SelectedStyleId == null || priority > SelectedPriority)
        {
            SelectedStyleId = styleId;
            SelectedPriority = priority;
            return;
        }

        if (priority == SelectedPriority &&
            styleId == PreferredStyleId &&
            SelectedStyleId != PreferredStyleId)
        {
            SelectedStyleId = styleId;
        }
    }
}

[ByRefEvent]
public sealed class CombatMasteryActiveStyleChangedEvent : EntityEventArgs
{
    public string? OldStyleId { get; }
    public string? NewStyleId { get; }
    public int NewStylePriority { get; }

    public CombatMasteryActiveStyleChangedEvent(string? oldStyleId, string? newStyleId, int newStylePriority)
    {
        OldStyleId = oldStyleId;
        NewStyleId = newStyleId;
        NewStylePriority = newStylePriority;
    }
}
