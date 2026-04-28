using Content.Server._OpenSpace.Combat.KravMaga.Components;
using Content.Shared.Actions;
using Content.Shared.Clothing;

namespace Content.Server._OpenSpace.Combat.KravMaga.Systems;

public sealed class KravMagaGlovesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KravMagaGlovesComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<KravMagaGlovesComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<KravMagaGlovesComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnEquipped(Entity<KravMagaGlovesComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (HasComp<KravMagaMasteryComponent>(args.Wearer))
        {
            ent.Comp.AddedMasteryToWearer = false;
            return;
        }

        EnsureComp<KravMagaMasteryComponent>(args.Wearer);
        ent.Comp.AddedMasteryToWearer = true;
    }

    private void OnUnequipped(Entity<KravMagaGlovesComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.AddedMasteryToWearer)
            return;

        RemComp<KravMagaMasteryComponent>(args.Wearer);
        ent.Comp.AddedMasteryToWearer = false;
    }

    private void OnGetActions(Entity<KravMagaGlovesComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.LegSweepActionEntity, ent.Comp.LegSweepAction);
        args.AddAction(ref ent.Comp.LungPunchActionEntity, ent.Comp.LungPunchAction);
        args.AddAction(ref ent.Comp.NeckChopActionEntity, ent.Comp.NeckChopAction);
    }
}
