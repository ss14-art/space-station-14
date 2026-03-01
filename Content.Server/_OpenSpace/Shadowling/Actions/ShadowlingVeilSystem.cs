using Content.Shared._OpenSpace.Actions;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Containers;
using Content.Server._OpenSpace.Shadowling;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Light;
using Content.Shared.Power;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Timing;
using Content.Shared.Actions;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingVeilSystem : EntitySystem
{
    private const float VeilRange = 5f; 

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingVeilComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingVeilComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingVeilActionEvent>(OnAction);

        SubscribeLocalEvent<ShadowlingVeiledPoweredLightComponent, PowerChangedEvent>(OnPoweredLightPowerChanged);
        SubscribeLocalEvent<ShadowlingVeiledPoweredLightComponent, SignalReceivedEvent>(OnPoweredLightSignal);
        SubscribeLocalEvent<ShadowlingVeiledPoweredLightComponent, DeviceNetworkPacketEvent>(OnPoweredLightPacket);
        SubscribeLocalEvent<ShadowlingVeiledPoweredLightComponent, EntRemovedFromContainerMessage>(OnPoweredLightBulbRemoved);
        SubscribeLocalEvent<ShadowlingVeiledPoweredLightComponent, EntInsertedIntoContainerMessage>(OnPoweredLightBulbInserted);

        SubscribeLocalEvent<HandheldLightComponent, LightToggleEvent>(OnHandheldToggle);
        SubscribeLocalEvent<ShadowlingVeiledHandheldLightComponent, EntRemovedFromContainerMessage>(OnHandheldContainerRemoved);
    }

    private void OnInit(EntityUid uid, ShadowlingVeilComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingVeilComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingVeilActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        if (!TryComp<TransformComponent>(performer, out var xform))
            return;

        var targets = _lookup.GetEntitiesInRange(xform.Coordinates, VeilRange);
        foreach (var target in targets)
        {
            if (TryComp<PoweredLightComponent>(target, out var powered))
            {
                EnsureComp<ShadowlingVeiledPoweredLightComponent>(target);
                _poweredLight.SetState(target, false, powered);
                continue;
            }

            if (TryComp<HandheldLightComponent>(target, out var handheld))
            {
                if (HasComp<BorgChassisComponent>(target))
                {
                    var borgVeil = EnsureComp<ShadowlingVeiledBorgLightComponent>(target);
                    borgVeil.EndTime = _timing.CurTime + TimeSpan.FromSeconds(15);
                    _handheldLight.SetActivated(target, false, handheld);
                    Timer.Spawn(TimeSpan.FromSeconds(15), () =>
                    {
                        if (Deleted(target))
                            return;
                        if (TryComp<ShadowlingVeiledBorgLightComponent>(target, out var comp) && _timing.CurTime >= comp.EndTime)
                            RemComp<ShadowlingVeiledBorgLightComponent>(target);
                    });
                }
                else
                {
                    EnsureComp<ShadowlingVeiledHandheldLightComponent>(target);
                    _handheldLight.SetActivated(target, false, handheld);
                }
            }
        }

        ev.Handled = true;
    }

    private void OnPoweredLightPowerChanged(EntityUid uid, ShadowlingVeiledPoweredLightComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<PoweredLightComponent>(uid, out var light))
            _poweredLight.SetState(uid, false, light);
    }

    private void OnPoweredLightSignal(EntityUid uid, ShadowlingVeiledPoweredLightComponent component, ref SignalReceivedEvent args)
    {
        if (TryComp<PoweredLightComponent>(uid, out var light))
            _poweredLight.SetState(uid, false, light);
    }

    private void OnPoweredLightPacket(EntityUid uid, ShadowlingVeiledPoweredLightComponent component, ref DeviceNetworkPacketEvent args)
    {
        if (TryComp<PoweredLightComponent>(uid, out var light))
            _poweredLight.SetState(uid, false, light);
    }

    private void OnPoweredLightBulbRemoved(EntityUid uid, ShadowlingVeiledPoweredLightComponent component, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != SharedPoweredLightSystem.LightBulbContainer)
            return;

        RemComp<ShadowlingVeiledPoweredLightComponent>(uid);
        if (TryComp<PoweredLightComponent>(uid, out var light))
            _poweredLight.SetState(uid, true, light);
    }

    private void OnPoweredLightBulbInserted(EntityUid uid, ShadowlingVeiledPoweredLightComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != SharedPoweredLightSystem.LightBulbContainer)
            return;

        RemComp<ShadowlingVeiledPoweredLightComponent>(uid);
        if (TryComp<PoweredLightComponent>(uid, out var light))
            _poweredLight.SetState(uid, true, light);
    }

    private void OnHandheldToggle(EntityUid uid, HandheldLightComponent component, ref LightToggleEvent args)
    {
        if (TryComp<ShadowlingVeiledBorgLightComponent>(uid, out var borgVeil))
        {
            if (_timing.CurTime >= borgVeil.EndTime)
            {
                RemComp<ShadowlingVeiledBorgLightComponent>(uid);
            }
            else if (args.IsOn)
            {
                _handheldLight.SetActivated(uid, false, component);
                return;
            }
        }

        if (!HasComp<ShadowlingVeiledHandheldLightComponent>(uid))
            return;

        if (!args.IsOn)
            return;

        _handheldLight.SetActivated(uid, false, component);
    }

    private void OnHandheldContainerRemoved(EntityUid uid, ShadowlingVeiledHandheldLightComponent component, ref EntRemovedFromContainerMessage args)
    {
        RemComp<ShadowlingVeiledHandheldLightComponent>(uid);
    }
}
