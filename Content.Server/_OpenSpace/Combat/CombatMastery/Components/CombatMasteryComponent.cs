using System;
using System.Collections.Generic;
using Content.Server._OpenSpace.Combat.CombatMastery.Systems;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Content.Shared.Damage;

namespace Content.Server._OpenSpace.Combat.CombatMastery.Components;

[RegisterComponent, ComponentProtoName("CombatMastery")]
[Access(typeof(CombatMasteryControllerSystem))]
public sealed partial class CombatMasteryComponent : Component
{
    [DataField] public int MaxComboLength = 12;

    [DataField] public TimeSpan ComboTimeout = TimeSpan.FromSeconds(5);

    [DataField] public List<ComboMasteryKeys> CombatMasteryCurrentCombo = new();

    [DataField] public EntityUid? CurrentTarget;

    [ViewVariables] public DamageSpecifier? OriginalUnarmedMeleeDamage;

    [ViewVariables] public bool PendingMeleeDamageRefresh;

    [ViewVariables] public bool PendingHudStateRefresh;

    [ViewVariables] public TimeSpan? LastComboUpdateTime;

    [ViewVariables] public string? ActiveStyleId;

    [ViewVariables] public int ActiveStylePriority;
}

[DataDefinition]
public sealed partial class CombatMasteryTemplateCollection
{
    [DataField] public List<CombatMasteryTemplate> Templates = [];

    [NonSerialized] public List<CombatMasteryTemplateProgressState> ActiveTemplates = [];

    [NonSerialized] public bool ActiveTemplatesDirty = true;
}

public sealed class CombatMasteryTemplateProgressState
{
    public CombatMasteryTemplate Template = default!;
    public int NextStepIndex;
    public EntityUid? CurrentTarget;
}
