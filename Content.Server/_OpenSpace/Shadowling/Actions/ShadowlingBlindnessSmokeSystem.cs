using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared._OpenSpace.Actions;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingBlindnessSmokeSystem : EntitySystem
{
    private const float SmokeDurationSeconds = 20f;
    private const int SmokeSpreadAmount = 9;

    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingBlindnessSmokeActionEvent>(OnAction);
    }

    private void OnAction(ShadowlingBlindnessSmokeActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;

        if (!HasComp<ShadowlingEnthrallComponent>(performer))
            return;

        if (!_mapMan.TryFindGridAt(_transform.GetMapCoordinates(performer), out var gridUid, out var gridComp))
            return;

        var xform = Transform(performer);
        if (!_map.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) || tileRef.Tile.IsEmpty)
            return;

        if (_spreader.RequiresFloorToSpread("ShadowlingBlindnessSmoke") && _turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, _transform.GetMapCoordinates(performer));
        var smoke = Spawn("ShadowlingBlindnessSmoke", coords.SnapToGrid());
        if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            Del(smoke);
            return;
        }

        var solution = new Solution();
        _smoke.StartSmoke(smoke, solution, SmokeDurationSeconds, SmokeSpreadAmount, smokeComp);

        ev.Handled = true;
    }
}
