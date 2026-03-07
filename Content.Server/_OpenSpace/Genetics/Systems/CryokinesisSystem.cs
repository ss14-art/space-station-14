using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class CryokinesisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    private static readonly ProtoId<TagPrototype> SuitEvatag = "SuitEVA";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CryokinesisComponent, CryokinesisActionEvent>(OnCryokinesis);
        SubscribeLocalEvent<CryokinesisComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CryokinesisComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<CryokinesisComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out ActionsComponent? actions))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, component: actions);
    }

    private void OnShutdown(Entity<CryokinesisComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnCryokinesis(Entity<CryokinesisComponent> ent, ref CryokinesisActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_interaction.InRangeUnobstructed(args.Performer, args.Target, range: 7f))
            return;

        if (!TryComp<TemperatureComponent>(args.Target, out var temp))
            return;

        var delta = IsInRig(args.Target) ? 100f : 200f;
        var newTemp = temp.CurrentTemperature - delta;
        _temperature.ForceChangeTemperature(args.Target, newTemp, temp);

        _popup.PopupEntity(Loc.GetString("genetics-cryokinesis-target"), args.Performer, args.Performer);
        _popup.PopupEntity(Loc.GetString("genetics-cryokinesis-victim"), args.Target, args.Target);

        args.Handled = true;
    }

    private bool IsInRig(EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer) && outer != null)
        {
            if (_tag.HasTag(outer.Value, HardsuitTag) || _tag.HasTag(outer.Value, SuitEvatag))
                return true;
        }

        return false;
    }
}

