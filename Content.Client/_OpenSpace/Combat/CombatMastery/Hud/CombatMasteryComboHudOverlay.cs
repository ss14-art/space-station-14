using System.Collections.Generic;
using System.Numerics;
using Content.Shared._OpenSpace.Combat.CombatMastery;
using Content.Shared._OpenSpace.Combat.CombatMastery.Hud.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._OpenSpace.Combat.CombatMastery.Hud;

public sealed class CombatMasteryComboHudOverlay : Overlay
{
    private const float CursorOffsetX = 15f;
    private const float CursorOffsetY = 10f;
    private const float IconSpacing = 5f;
    private static readonly ComboMasteryKeys[] IconLoadOrder =
    [
        ComboMasteryKeys.help,
        ComboMasteryKeys.attack,
        ComboMasteryKeys.disarm,
        ComboMasteryKeys.grab,
    ];

    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly CombatMasteryComboHudSystem _hudSystem;
    private readonly Dictionary<ComboMasteryKeys, AnimatedComboIcon> _icons = [];

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CombatMasteryComboHudOverlay(CombatMasteryComboHudSystem hudSystem)
    {
        IoCManager.InjectDependencies(this);

        _hudSystem = hudSystem;

        foreach (var key in IconLoadOrder)
        {
            LoadIcon(key);
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _hudSystem.TryGetVisibleCombo(out _);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_hudSystem.TryGetVisibleCombo(out var combo) || combo == null)
            return;

        var comboStart = Math.Max(0, combo.Count - CombatMasteryComboHudComponent.MaxVisibleKeys);
        var animationTime = (float) _timing.CurTime.TotalSeconds;
        var maxWidth = 0;

        for (var index = comboStart; index < combo.Count; index++)
        {
            var frame = GetCurrentFrame(combo[index], animationTime);
            if (frame != null && frame.Width > maxWidth)
                maxWidth = frame.Width;
        }

        if (maxWidth == 0)
            return;

        var mousePos = _inputManager.MouseScreenPosition.Position;
        var drawPos = new Vector2(
            mousePos.X - CursorOffsetX - maxWidth,
            mousePos.Y - CursorOffsetY);

        for (var index = comboStart; index < combo.Count; index++)
        {
            var frame = GetCurrentFrame(combo[index], animationTime);
            if (frame == null)
                continue;

            args.ScreenHandle.DrawTextureRect(frame, UIBox2.FromDimensions(drawPos, new Vector2(frame.Width, frame.Height)));
            drawPos.Y += frame.Height + IconSpacing;
        }
    }

    private void LoadIcon(ComboMasteryKeys key)
    {
        var stateName = key.ToString();
        var rsiPath = new ResPath($"/Textures/_OpenSpace/Interface/Combat/CombatMastery/HudCombo/{stateName}.rsi");

        if (!_resourceCache.TryGetResource<RSIResource>(rsiPath, out var rsiResource) ||
            !rsiResource.RSI.TryGetState(stateName, out RSI.State? state))
        {
            return;
        }

        _icons[key] = new AnimatedComboIcon(state.GetFrames(RsiDirection.South), state.GetDelays());
    }

    private Texture? GetCurrentFrame(ComboMasteryKeys key, float animationTime)
    {
        if (!_icons.TryGetValue(key, out var icon) || icon.Frames.Length == 0)
            return null;

        return icon.GetFrame(animationTime);
    }

    private sealed record AnimatedComboIcon(Texture[] Frames, float[] Delays)
    {
        private readonly float _totalDuration = SumDelays(Delays);

        public Texture? GetFrame(float animationTime)
        {
            if (Frames.Length == 0)
                return null;

            if (Delays.Length == 0 || Frames.Length == 1 || _totalDuration <= 0f)
                return Frames[0];

            var normalizedTime = animationTime % _totalDuration;
            var accumulatedDelay = 0f;

            for (var index = 0; index < Delays.Length; index++)
            {
                accumulatedDelay += Delays[index];
                if (normalizedTime < accumulatedDelay)
                    return Frames[index];
            }

            return Frames[^1];
        }

        private static float SumDelays(IReadOnlyList<float> delays)
        {
            var total = 0f;
            foreach (var delay in delays)
            {
                total += delay;
            }

            return total;
        }
    }
}
