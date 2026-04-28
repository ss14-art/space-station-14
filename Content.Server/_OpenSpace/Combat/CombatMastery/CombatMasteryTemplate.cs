using System.Collections.Generic;
using Content.Shared._OpenSpace.Combat.CombatMastery;

namespace Content.Server._OpenSpace.Combat.CombatMastery;

[DataDefinition]
public sealed partial class CombatMasteryTemplate
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<ComboMasteryKeys> Sequence = [];

    [DataField]
    public bool RequireSameTarget = true;

    public bool IsValid() => !string.IsNullOrWhiteSpace(Name) && Sequence.Count > 0;
}
