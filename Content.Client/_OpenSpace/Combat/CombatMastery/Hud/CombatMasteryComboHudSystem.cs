using System.Collections.Generic;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Content.Shared._OpenSpace.Combat.CombatMastery.Hud.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._OpenSpace.Combat.CombatMastery.Hud;

public sealed class CombatMasteryComboHudSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayManager.AddOverlay(new CombatMasteryComboHudOverlay(this));
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<CombatMasteryComboHudOverlay>();

        base.Shutdown();
    }

    public bool TryGetVisibleCombo(out IReadOnlyList<ComboMasteryKeys>? combo)
    {
        combo = null;

        if (_playerManager.LocalEntity is not { } localEntity)
            return false;

        if (!TryComp<CombatMasteryComboHudComponent>(localEntity, out var hud) ||
            !hud.HasActiveMastery ||
            hud.VisibleCombo.Count == 0)
        {
            return false;
        }

        combo = hud.VisibleCombo;
        return true;
    }
}
