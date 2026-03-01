using Content.Server._OpenSpace.Shadowling;
using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingBlackRecuperationSystem : EntitySystem
{
    private const float RecuperationDurationSeconds = 5f;
    private const float RecuperationDistance = 2.5f;

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingBlackRecuperationActionEvent>(OnAction);
        SubscribeLocalEvent<ShadowlingEnthrallComponent, ShadowlingBlackRecuperationDoAfterEvent>(OnDoAfter);
    }

    private void OnAction(ShadowlingBlackRecuperationActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        var target = ev.Target;

        if (!EntityManager.EntityExists(target))
            return;

        if (!IsValidTarget(performer, target, out var reason))
        {
            if (EntityManager.EntityExists(target))
                _popup.PopupEntity(Loc.GetString(reason), target, performer);
            else
                _popup.PopupClient(Loc.GetString(reason), performer, performer);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, performer, RecuperationDurationSeconds,
            new ShadowlingBlackRecuperationDoAfterEvent(), performer, target)
        {
            DistanceThreshold = RecuperationDistance,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = true,
            Hidden = false,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupClient(Loc.GetString("shadowling-action-black-recuperation-start"), performer, performer);
        ev.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ShadowlingEnthrallComponent component, ref ShadowlingBlackRecuperationDoAfterEvent ev)
    {
        if (ev.Handled || ev.Cancelled || ev.Target == null)
            return;

        var performer = ev.User;
        var target = ev.Target.Value;

        if (!IsValidTarget(performer, target, out _))
            return;

        _rejuvenate.PerformRejuvenate(target);
        _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(2));
        _popup.PopupClient(Loc.GetString("shadowling-action-black-recuperation-success"), performer, performer);
        _popup.PopupClient(Loc.GetString("shadowling-action-black-recuperation-target"), target, target);
        ev.Handled = true;
    }

    private bool IsValidTarget(EntityUid performer, EntityUid target, out string locKey)
    {
        locKey = "shadowling-action-black-recuperation-invalid";

        if (target == performer)
            return false;

        if (!TryComp<ShadowlingThrallComponent>(target, out var thrall))
        {
            locKey = "shadowling-action-black-recuperation-not-thrall";
            return false;
        }

        if (thrall.Master != performer)
        {
            locKey = "shadowling-action-black-recuperation-not-master";
            return false;
        }

        if (!TryComp<MobStateComponent>(target, out var mobState) || mobState.CurrentState != MobState.Dead)
        {
            locKey = "shadowling-action-black-recuperation-not-dead";
            return false;
        }

        return true;
    }
}
