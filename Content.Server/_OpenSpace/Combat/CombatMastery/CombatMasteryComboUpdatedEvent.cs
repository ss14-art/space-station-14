using System.Collections.Generic;
using Content.Shared._OpenSpace.Combat.CombatMastery;

namespace Content.Server._OpenSpace.Combat.CombatMastery;

[ByRefEvent]
public sealed class CombatMasteryComboUpdatedEvent : EntityEventArgs
{
    public EntityUid Target { get; }
    public ComboMasteryKeys Step { get; }
    public IReadOnlyList<ComboMasteryKeys> Combo { get; }
    public bool TemplateExecuted { get; set; }

    public CombatMasteryComboUpdatedEvent(EntityUid target, ComboMasteryKeys step, IReadOnlyList<ComboMasteryKeys> combo)
    {
        Target = target;
        Step = step;
        Combo = combo;
    }
}

[ByRefEvent]
public sealed class CombatMasteryComboResetEvent : EntityEventArgs;
