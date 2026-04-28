using Content.Server._OpenSpace.Combat.CorporateJudo.Components;
using Content.Shared.Clothing;

namespace Content.Server._OpenSpace.Combat.CorporateJudo.Systems;

public sealed class CorporateJudoBeltSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporateJudoBeltComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CorporateJudoBeltComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<CorporateJudoBeltComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (HasComp<CorporateJudoMasteryComponent>(args.Wearer))
        {
            ent.Comp.AddedMasteryToWearer = false;
            return;
        }

        EnsureComp<CorporateJudoMasteryComponent>(args.Wearer);
        ent.Comp.AddedMasteryToWearer = true;
    }

    private void OnUnequipped(Entity<CorporateJudoBeltComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.AddedMasteryToWearer)
            return;

        RemComp<CorporateJudoMasteryComponent>(args.Wearer);
        ent.Comp.AddedMasteryToWearer = false;
    }
}
