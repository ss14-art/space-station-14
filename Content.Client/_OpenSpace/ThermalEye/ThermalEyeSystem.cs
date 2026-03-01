using Content.Shared._OpenSpace.ThermalEye;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._OpenSpace.ThermalEye;

public sealed class ThermalEyeSystem : SharedThermalEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ThermalEyeEntityHighlightOverlay _overlay = default!;
    private bool _overlayAdded;
    protected override bool IsPredict() => !_timing.IsFirstTimePredicted;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalEyeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ThermalEyeComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ThermalEyeComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new(_prototypeManager.Index<ShaderPrototype>("BrightnessShader"));
    }

    private void OnAttached(Entity<ThermalEyeComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateOverlay(ent.Owner, ent.Comp.Active);
    }

    private void OnDetached(Entity<ThermalEyeComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        UpdateOverlay(ent.Owner, false, force: true);
    }

    private void OnStartup(Entity<ThermalEyeComponent> ent, ref ComponentStartup args)
    {
        UpdateOverlay(ent.Owner, ent.Comp.Active);
    }

    protected override void ToggleOn(Entity<ThermalEyeComponent> ent)
    {
        UpdateOverlay(ent.Owner, true);
    }

    protected override void ToggleOff(Entity<ThermalEyeComponent> ent)
    {
        UpdateOverlay(ent.Owner, false);
    }

    private void UpdateOverlay(EntityUid uid, bool active, bool force = false)
    {
        if (_player.LocalEntity != uid && !force)
            return;

        if (active)
        {
            if (_overlayAdded)
                return;

            _overlayMan.AddOverlay(_overlay);
            _overlayAdded = true;
        }
        else
        {
            if (!_overlayAdded)
                return;

            _overlayMan.RemoveOverlay(_overlay);
            _overlayAdded = false;
        }
    }
}
