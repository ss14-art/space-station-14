using Content.Server.Chat.Managers;
using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingCollectiveMindSystem : EntitySystem
{
    private const float CastSeconds = 2f;

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ShadowlingHiveSystem _hive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingCollectiveMindComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingCollectiveMindComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingCollectiveMindActionEvent>(OnAction);
        SubscribeLocalEvent<ShadowlingComponent, ShadowlingCollectiveMindDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(EntityUid uid, ShadowlingCollectiveMindComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingCollectiveMindComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingCollectiveMindActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;

        var doAfter = new DoAfterArgs(EntityManager, performer, CastSeconds,
            new ShadowlingCollectiveMindDoAfterEvent(), performer)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = true,
            Hidden = false,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        ev.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ShadowlingComponent component, ref ShadowlingCollectiveMindDoAfterEvent ev)
    {
        if (ev.Handled || ev.Cancelled)
            return;

        var performer = ev.User;
        var count = _hive.GetAliveThrallCount();

        if (TryComp<ShadowlingEnthrallComponent>(performer, out var enthrall))
            UpdateUnlockActions(performer, enthrall, count);

        if (_player.TryGetSessionByEntity(performer, out var session))
        {
            var msg = Loc.GetString("shadowling-action-collective-mind-report", ("count", count));
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("shadowling-action-collective-mind-report", ("count", count)), performer, performer);
        }

        ev.Handled = true;
    }

    private void UpdateUnlockActions(EntityUid uid, ShadowlingEnthrallComponent component, int thrallCount)
    {
        if (thrallCount >= component.BlackRecuperationUnlockCount &&
            component.BlackRecuperationActionEnt == null)
        {
            _actions.AddAction(uid, ref component.BlackRecuperationActionEnt, component.BlackRecuperationActionId);
        }

        if (thrallCount >= component.AscendanceUnlockCount &&
            component.AscendanceActionEnt == null)
        {
            _actions.AddAction(uid, ref component.AscendanceActionEnt, component.AscendanceActionId);
        }

        if (thrallCount >= component.SonicScreechUnlockCount &&
            !HasComp<ShadowlingAscendantComponent>(uid) &&
            TryComp<ShadowlingSonicScreechComponent>(uid, out var screech) &&
            screech.ActionEnt == null)
        {
            _actions.AddAction(uid, ref screech.ActionEnt, screech.ActionId);
        }

        if (thrallCount >= component.BlindnessSmokeUnlockCount &&
            component.BlindnessSmokeActionEnt == null)
        {
            _actions.AddAction(uid, ref component.BlindnessSmokeActionEnt, component.BlindnessSmokeActionId);
        }
    }
}
