using System.Collections.Generic;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Combat.CombatMastery.Hud.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatMasteryComboHudComponent : Component
{
    public const int MaxVisibleKeys = 4;

    [DataField, AutoNetworkedField]
    public bool HasActiveMastery;

    [DataField, AutoNetworkedField]
    public List<ComboMasteryKeys> VisibleCombo = [];
}
