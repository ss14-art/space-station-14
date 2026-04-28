using Content.Server._OpenSpace.Combat.CorporateJudo.Systems;

namespace Content.Server._OpenSpace.Combat.CorporateJudo.Components;

[RegisterComponent]
[Access(typeof(CorporateJudoBeltSystem))]
public sealed partial class CorporateJudoBeltComponent : Component
{
    [ViewVariables]
    public bool AddedMasteryToWearer;
}
