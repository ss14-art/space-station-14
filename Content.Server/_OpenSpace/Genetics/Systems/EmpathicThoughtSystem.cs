using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Localization;
using Robust.Shared.Random;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class EmpathicThoughtSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string[] Thoughts =
    {
        "hunger",
        "fear",
        "confusion",
        "curiosity",
        "anger",
        "relief",
        "boredom",
        "determination",
        "fatigue",
        "hope"
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpathicThoughtComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EmpathicThoughtComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmpathicThoughtComponent, EmpathicThoughtActionEvent>(OnEmpathicThought);
    }

    private void OnInit(Entity<EmpathicThoughtComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out ActionsComponent? actions))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, component: actions);
    }

    private void OnShutdown(Entity<EmpathicThoughtComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnEmpathicThought(Entity<EmpathicThoughtComponent> ent, ref EmpathicThoughtActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<PsyResistComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("genetics-empathic-blocked"), args.Performer, args.Performer);
            args.Handled = true;
            return;
        }

        var pain = GetPainLevel(args.Target);
        var intent = _combat.IsInCombatMode(args.Target) ? Loc.GetString("genetics-empathic-intent-aggro") : Loc.GetString("genetics-empathic-intent-calm");
        var thought = Loc.GetString("genetics-empathic-thought", ("thought", _random.Pick(Thoughts)));

        _popup.PopupEntity(Loc.GetString("genetics-empathic-result", ("pain", pain), ("intent", intent), ("thought", thought)), args.Performer, args.Performer);
        args.Handled = true;
    }

    private string GetPainLevel(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return Loc.GetString("genetics-empathic-pain-none");

        var total = damageable.Damage.GetTotal();
        if (_thresholds.TryGetThresholdForState(target, Content.Shared.Mobs.MobState.Critical, out var crit))
        {
            var pct = (float) total / (float) crit;
            if (pct < 0.25f)
                return Loc.GetString("genetics-empathic-pain-low");
            if (pct < 0.6f)
                return Loc.GetString("genetics-empathic-pain-medium");
            return Loc.GetString("genetics-empathic-pain-high");
        }

        if (total < 20)
            return Loc.GetString("genetics-empathic-pain-low");
        if (total < 60)
            return Loc.GetString("genetics-empathic-pain-medium");
        return Loc.GetString("genetics-empathic-pain-high");
    }
}

