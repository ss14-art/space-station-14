using System;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared._OpenSpace.Genetics;
using Content.Shared._OpenSpace.Genetics.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Maths;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class MatterEaterSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, (Color Original, TimeSpan EndTime)> _actionGlow = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatterEaterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MatterEaterComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MatterEaterComponent, MatterEaterActionEvent>(OnMatterEater);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_actionGlow.Count == 0)
            return;

        var now = _timing.CurTime;
        var toClear = new List<EntityUid>();

        foreach (var (actionUid, data) in _actionGlow)
        {
            if (now < data.EndTime)
                continue;

            if (TryComp<ActionComponent>(actionUid, out var action))
                _actions.SetIconColor((actionUid, action), data.Original);

            toClear.Add(actionUid);
        }

        foreach (var uid in toClear)
            _actionGlow.Remove(uid);
    }

    private void OnInit(Entity<MatterEaterComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out ActionsComponent? actions))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, component: actions);
    }

    private void OnShutdown(Entity<MatterEaterComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnMatterEater(Entity<MatterEaterComponent> ent, ref MatterEaterActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_interaction.InRangeUnobstructed(args.Performer, args.Target, range: 1.5f))
            return;

        
        if (HasComp<ItemComponent>(args.Target))
        {
            EntityManager.DeleteEntity(args.Target);
            HealFromMeal(args.Performer);
            _popup.PopupEntity(Loc.GetString("genetics-matter-eater-item"), args.Performer, args.Performer);
            ActivateActionGlow(ent);
            args.Handled = true;
            return;
        }
    }

    private void ActivateActionGlow(Entity<MatterEaterComponent> ent)
    {
        if (ent.Comp.ActionEntity is not { } actionUid)
            return;

        if (!TryComp<ActionComponent>(actionUid, out var action))
            return;

        var original = action.IconColor;
        _actions.SetIconColor((actionUid, action), Color.FromHex("#FF4040"));
        _actionGlow[actionUid] = (original, _timing.CurTime + TimeSpan.FromSeconds(0.6));
    }

    private void HealFromMeal(EntityUid eater)
    {
        if (!TryComp<DamageableComponent>(eater, out var damageable))
            return;

        var partCount = 6;
        if (TryComp<BodyComponent>(eater, out var body))
        {
            var parts = _body.GetBodyChildren(eater, body);
            partCount = Math.Max(1, parts.Count());
        }

        var totalHeal = Math.Min(62, partCount * 4);

        
        var positives = damageable.Damage.DamageDict
            .Where(kv => kv.Value > 0)
            .ToList();

        if (positives.Count == 0)
            return;

        var totalPositive = positives.Sum(kv => kv.Value.Float());
        if (totalPositive <= 0f)
            return;

        var healCap = Math.Min(totalHeal, totalPositive);
        var heal = new DamageSpecifier();

        foreach (var (type, value) in positives)
        {
            var share = value.Float() / totalPositive;
            var amount = -(float) (healCap * share);
            if (amount != 0f)
                heal.DamageDict[type] = amount;
        }

        _damageable.TryChangeDamage(eater, heal, true, false, eater);
    }
}

