using Content.Shared.Actions;
using Content.Shared._OpenSpace.Actions;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions.Components;
using Content.Server._OpenSpace.Shadowling;
using Content.Shared.StepTrigger.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffectNew;
using Content.Server._Starlight.Shadekin;
using Content.Shared.Humanoid;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingEngageHatchSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly ShadekinSystem _shadekin = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private const float EngageLightThreshold = 0.2f;
    private const string HatchWallPrototype = "WallShadowlingHatch";
    private static readonly Vector2i[] WallOffsets =
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1, 0),              new(1, 0),
        new(-1, 1),  new(0, 1),  new(1, 1),
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingEngageHatchComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadowlingEngageHatchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowlingEngageHatchActionEvent>(OnAction);
        SubscribeLocalEvent<ShadowlingEngageHatchComponent, ShadowlingEngageHatchDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(EntityUid uid, ShadowlingEngageHatchComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEnt, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ShadowlingEngageHatchComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEnt);
        component.ActionEnt = null;
    }

    private void OnAction(ShadowlingEngageHatchActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;
        if (!TryComp<ShadowlingEngageHatchComponent>(performer, out var hatchComp))
            return;

        if (IsInLight(performer))
        {
            var msg = Loc.GetString("shadowling-action-engage-hatch-need-dark");
            _popup.PopupClient(msg, performer, performer);
            _popup.PopupEntity(msg, performer, performer);
            ev.Handled = true;
            return;
        }

        if (HasNearbyPeople(performer, 4f))
        {
            var msg = Loc.GetString("shadowling-action-engage-hatch-nearby");
            _popup.PopupClient(msg, performer, performer);
            _popup.PopupEntity(msg, performer, performer);
            ev.Handled = true;
            return;
        }

        var duration = TimeSpan.FromSeconds(Math.Max(1f, hatchComp.TransformSeconds));
        var doAfter = new DoAfterArgs(EntityManager, performer, duration,
            new ShadowlingEngageHatchDoAfterEvent(), performer, null, null)
        {
            BreakOnDamage = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = false,
            Hidden = false,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupClient(Loc.GetString("shadowling-action-engage-hatch-activated"), performer, performer);
        StripAll(performer);
        hatchComp.ActiveWalls = SpawnWallsAround(performer);
        _stun.TryAddStunDuration(performer, duration + TimeSpan.FromSeconds(0.2f));
        ev.Handled = true;
    }

    private bool HasNearbyPeople(EntityUid performer, float range)
    {
        var coords = Transform(performer).Coordinates;
        foreach (var uid in _lookup.GetEntitiesInRange(coords, range))
        {
            if (uid == performer)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(uid))
                continue;

            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState != MobState.Dead)
                return true;
        }

        return false;
    }

    private void OnDoAfter(EntityUid uid, ShadowlingEngageHatchComponent component, ref ShadowlingEngageHatchDoAfterEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.User;
        CleanupWalls(component.ActiveWalls);
        component.ActiveWalls.Clear();
        _status.TryRemoveStatusEffect(performer, SharedStunSystem.StunId);

        if (ev.Cancelled)
        {
            ev.Handled = true;
            return;
        }

        if (TryComp<MobStateComponent>(performer, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            ev.Handled = true;
            return;
        }

        TransformToShadowling(performer);
        _actions.RemoveAction(performer, component.ActionEnt);
        component.ActionEnt = null;
        ev.Handled = true;
    }

    private void StripAll(EntityUid target)
    {
        if (TryComp<InventoryComponent>(target, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((target, inventory));
            while (slots.NextItem(out _, out var slot))
            {
                _inventory.TryUnequip(target, target, slot.Name, true, true, inventory: inventory);
            }
        }

        if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var hand in _hands.EnumerateHands((target, hands)))
            {
                _hands.TryDrop((target, hands),
                    hand,
                    checkActionBlocker: false,
                    doDropInteraction: false);
            }
        }
    }

    private List<EntityUid> SpawnWallsAround(EntityUid target)
    {
        var spawned = new List<EntityUid>();
        if (!TryComp<TransformComponent>(target, out var xform))
            return spawned;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return spawned;

        if (!_turf.TryGetTileRef(xform.Coordinates, out var centerTile))
            return spawned;

        var center = centerTile.Value.GridIndices;
        foreach (var offset in WallOffsets)
        {
            var indices = center + offset;
            if (!_map.TryGetTileRef(gridUid, gridComp, indices, out var tileRef))
                continue;

            var hasWall = false;
            foreach (var ent in _map.GetAnchoredEntities(gridUid, gridComp, indices))
            {
                if (_tags.HasTag(ent, WallTag))
                {
                    hasWall = true;
                    break;
                }
            }

            if (hasWall)
                continue;

            var coords = _turf.GetTileCenter(tileRef);
            var wall = Spawn(HatchWallPrototype, coords);
            spawned.Add(wall);
        }

        return spawned;
    }

    private void CleanupWalls(List<EntityUid> walls)
    {
        foreach (var wall in walls)
        {
            if (!Deleted(wall))
                QueueDel(wall);
        }
    }

    private void TransformToShadowling(EntityUid target)
    {
        if (Deleted(target))
            return;

        var newEnt = _polymorph.PolymorphEntity(target, "ShadowlingEngageHatch");
        if (newEnt != null)
            EnsureComp<ProtectedFromStepTriggersComponent>(newEnt.Value);
    }

    private bool IsInLight(EntityUid uid)
    {
        if (_turf.TryGetTileRef(Transform(uid).Coordinates, out var tileRef) && _turf.IsSpace(tileRef.Value))
            return true;

        var exposure = _shadekin.GetLightExposure(uid);
        return exposure >= EngageLightThreshold;
    }
}
