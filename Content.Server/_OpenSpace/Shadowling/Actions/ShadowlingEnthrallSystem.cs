using Content.Server._OpenSpace.Shadowling;
using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared._OpenSpace.NightEye;
using Content.Shared.Antag;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Server.Roles;
using Content.Shared.Mobs;
using Content.Shared.Actions;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Content.Shared.CollectiveMind;
using Content.Server.Objectives;
using Content.Server.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingEnthrallSystem : EntitySystem
{
    private const float EnthrallCycleSeconds = 3f;
    private const int EnthrallCycles = 4;
    private const int EnthrallStunStartStep = 2;
    private const float EnthrallDistance = 2.5f;
    private const string ThrallObjectiveId = "ShadowlingThrallObeyObjective";

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly ShadowlingHiveSystem _hive = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedCollectiveMindSystem _collectiveMind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjectives = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingEnthrallComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingEnthrallComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingEnthrallActionEvent>(OnAction);
        SubscribeLocalEvent<ShadowlingComponent, ShadowlingEnthrallDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ShadowlingThrallComponent, MobStateChangedEvent>(OnThrallMobStateChanged);
        SubscribeLocalEvent<ShadowlingThrallComponent, ComponentShutdown>(OnThrallShutdown);
    }

    private void OnInit(EntityUid uid, ShadowlingEnthrallComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingEnthrallComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;

        if (component.BlackRecuperationActionEnt != null)
        {
            _actions.RemoveAction(uid, component.BlackRecuperationActionEnt);
            component.BlackRecuperationActionEnt = null;
        }

        if (component.AscendanceActionEnt != null)
        {
            _actions.RemoveAction(uid, component.AscendanceActionEnt);
            component.AscendanceActionEnt = null;
        }

        if (component.BlindnessSmokeActionEnt != null)
        {
            _actions.RemoveAction(uid, component.BlindnessSmokeActionEnt);
            component.BlindnessSmokeActionEnt = null;
        }
    }

    private void OnAction(ShadowlingEnthrallActionEvent ev)
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

        if (!StartEnthrallCycle(performer, target, 1))
            return;

        _popup.PopupClient(Loc.GetString("shadowling-action-enthrall-start"), performer, performer);
        ev.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ShadowlingComponent component, ref ShadowlingEnthrallDoAfterEvent ev)
    {
        if (ev.Handled || ev.Target == null)
            return;

        var performer = ev.User;
        var target = ev.Target.Value;

        if (ev.Cancelled)
        {
            _status.TryRemoveStatusEffect(target, SharedStunSystem.StunId);
            ev.Handled = true;
            return;
        }

        if (!IsValidTarget(performer, target, out _))
        {
            _status.TryRemoveStatusEffect(target, SharedStunSystem.StunId);
            return;
        }

        if (ev.Step < EnthrallCycles)
        {
            StartEnthrallCycle(performer, target, ev.Step + 1);
            ev.Handled = true;
            return;
        }

        EnsureComp<ShadowlingThrallComponent>(target);
        EnsureComp<NightEyeComponent>(target);
        if (TryComp<ShadowlingThrallComponent>(target, out var thrall))
        {
            thrall.Master = performer;
            thrall.Counted = true;
        }
        _hive.RegisterThrall(target, true);
        TryInsertThrallTumor(target);
        EnsureComp<ShowAntagIconsComponent>(target);
        if (_mind.TryGetMind(target, out var mindId, out _))
            _roles.MindAddRole(mindId, "MindRoleShadowlingThrall", silent: true);
        TryAssignThrallObeyObjective(performer, target);
        if (TryComp<CollectiveMindComponent>(target, out var cmComp))
            _collectiveMind.UpdateCollectiveMind(target, cmComp);
        _popup.PopupClient(Loc.GetString("shadowling-action-enthrall-success"), performer, performer);
        _popup.PopupClient(Loc.GetString("shadowling-action-enthrall-target"), target, target);
        if (_player.TryGetSessionByEntity(target, out var targetSession))
            _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_OpenSpace/Shadowling/thrall.ogg"), targetSession);
        ev.Handled = true;

        _status.TryRemoveStatusEffect(target, SharedStunSystem.StunId);
    }

    private bool StartEnthrallCycle(EntityUid performer, EntityUid target, int step)
    {
        if (!IsValidTarget(performer, target, out _))
            return false;

        if (step >= EnthrallStunStartStep)
        {
            var remaining = (EnthrallCycles - step + 1) * EnthrallCycleSeconds;
            _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(remaining + 0.2f));
        }

        var doAfter = new DoAfterArgs(EntityManager, performer, EnthrallCycleSeconds,
            new ShadowlingEnthrallDoAfterEvent { Step = step }, performer, target)
        {
            DistanceThreshold = EnthrallDistance,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = true,
            Hidden = false,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private bool IsValidTarget(EntityUid performer, EntityUid target, out string locKey)
    {
        locKey = "shadowling-action-enthrall-invalid";
        if (target == performer)
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == Content.Shared.Mobs.MobState.Dead)
            return false;

        if (HasComp<ShadowlingThrallComponent>(target))
        {
            locKey = "shadowling-action-enthrall-already";
            return false;
        }

        if (HasComp<ShadowlingComponent>(target))
        {
            locKey = "shadowling-action-enthrall-shadowling";
            return false;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            locKey = "shadowling-action-enthrall-mindshield";
            return false;
        }

        return true;
    }

    private void OnThrallMobStateChanged(EntityUid uid, ShadowlingThrallComponent component, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Dead && component.Counted)
        {
            component.Counted = false;
            _hive.SetThrallAlive(uid, false);
            return;
        }

        if (args.NewMobState == Content.Shared.Mobs.MobState.Alive && !component.Counted)
        {
            component.Counted = true;
            _hive.SetThrallAlive(uid, true);
        }
    }

    private void OnThrallShutdown(EntityUid uid, ShadowlingThrallComponent component, ComponentShutdown args)
    {
        _hive.UnregisterThrall(uid, component.Counted);
    }

    private void TryAssignThrallObeyObjective(EntityUid master, EntityUid thrall)
    {
        if (!_mind.TryGetMind(thrall, out var thrallMindId, out var thrallMind)
            || !_mind.TryGetMind(master, out var masterMindId, out _))
            return;

        var objective = _objectives.TryCreateObjective(thrallMindId, thrallMind, ThrallObjectiveId);
        if (objective == null)
            return;

        _targetObjectives.SetTarget(objective.Value, masterMindId);
        _mind.AddObjective(thrallMindId, thrallMind, objective.Value);
    }

    private void TryInsertThrallTumor(EntityUid target)
    {
        if (TryComp<Content.Shared.Body.Components.BodyComponent>(target, out var body)
            && _body.TryGetBodyOrganEntityComps<ShadowlingThrallTumorComponent>((target, body), out var existing)
            && existing.Count > 0)
        {
            return;
        }

        foreach (var (partId, partComp) in _body.GetBodyChildrenOfType(target, BodyPartType.Head))
        {
            var tumor = Spawn("ShadowlingThrallTumor", Transform(target).Coordinates);
            if (!_body.InsertOrgan(partId, tumor, "brain_implant", partComp))
                Del(tumor);
            return;
        }
    }
}
