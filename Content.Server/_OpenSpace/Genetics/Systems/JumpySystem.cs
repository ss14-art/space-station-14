using System;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Cuffs.Components;
using Robust.Shared.Localization;
using Robust.Shared.Audio;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class JumpySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JumpyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<JumpyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<JumpyComponent, JumpyActionEvent>(OnJumpy);
    }

    private void OnInit(Entity<JumpyComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out ActionsComponent? actions))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, component: actions);
    }

    private void OnShutdown(Entity<JumpyComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnJumpy(Entity<JumpyComponent> ent, ref JumpyActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<PullableComponent>(args.Performer, out var pullable) && pullable.BeingPulled)
        {
            _stun.TryKnockdown(args.Performer, TimeSpan.FromSeconds(8), true);
            _popup.PopupEntity(Loc.GetString("genetics-jumpy-pulled"), args.Performer, args.Performer);
            args.Handled = true;
            return;
        }

        if (TryComp<CuffableComponent>(args.Performer, out var cuffable) && !cuffable.CanStillInteract)
        {
            _stun.TryKnockdown(args.Performer, TimeSpan.FromSeconds(8), true);
            _popup.PopupEntity(Loc.GetString("genetics-jumpy-cuffed"), args.Performer, args.Performer);
            args.Handled = true;
            return;
        }

        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * ent.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing);

        _throwing.TryThrow(args.Performer, direction, ent.Comp.JumpThrowSpeed);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), args.Performer, args.Performer);

        args.Handled = true;
    }
}

