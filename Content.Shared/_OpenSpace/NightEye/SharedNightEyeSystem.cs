using Content.Shared.Actions;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._OpenSpace.NightEye;

public abstract class SharedNightEyeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public EntProtoId Action = "ActionToggleNightEye";

    protected virtual bool IsPredict() => false;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightEyeComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<NightEyeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NightEyeComponent, ToggleNightEyeEvent>(OnToggle);
    }

    private void OnInit(Entity<NightEyeComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, Action);
        if (ent.Comp.Active)
            ToggleOn(ent);
        else
            ToggleOff(ent);
    }

    private void OnShutdown(Entity<NightEyeComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
        ToggleOff(ent, forceRemove: true);
    }

    private void OnToggle(Entity<NightEyeComponent> ent, ref ToggleNightEyeEvent args)
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

    protected virtual void ToggleOn(Entity<NightEyeComponent> ent)
    {
        var nightVision = EnsureComp<NightVisionComponent>(ent.Owner);
        nightVision.Active = true;
        Dirty(ent.Owner, nightVision);
    }

    protected virtual void ToggleOff(Entity<NightEyeComponent> ent, bool forceRemove = false)
    {
        if (!TryComp<NightVisionComponent>(ent.Owner, out var nightVision))
            return;

        if (!forceRemove && nightVision.Clothes)
            return;

        RemComp<NightVisionComponent>(ent.Owner);
    }
}
