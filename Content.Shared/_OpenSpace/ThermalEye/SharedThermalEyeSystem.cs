using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._OpenSpace.ThermalEye;

public abstract class SharedThermalEyeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    protected virtual bool IsPredict() => false;

    public EntProtoId Action = "ActionToggleThermalEye";

    public override void Initialize()
    {
        SubscribeLocalEvent<ThermalEyeComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ThermalEyeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ThermalEyeComponent, ToggleThermalEyeEvent>(OnToggle);
    }

    private void OnInit(Entity<ThermalEyeComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, Action);
    }

    private void OnShutdown(Entity<ThermalEyeComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
        ToggleOff(ent);
    }

    private void OnToggle(Entity<ThermalEyeComponent> ent, ref ToggleThermalEyeEvent args)
    {
        if (args.Handled || IsPredict())
            return;

        args.Handled = true;

        ent.Comp.Active = !ent.Comp.Active;

        if (ent.Comp.Active)
            ToggleOn(ent);
        else
            ToggleOff(ent);
    }

    protected virtual void ToggleOn(Entity<ThermalEyeComponent> ent)
    {
    }

    protected virtual void ToggleOff(Entity<ThermalEyeComponent> ent)
    {
    }
}
