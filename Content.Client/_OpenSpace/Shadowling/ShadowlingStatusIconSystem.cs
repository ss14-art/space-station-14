using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._OpenSpace.Shadowling;

public sealed class ShadowlingStatusIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<FactionIconPrototype> ShadowlingIcon = "ShadowlingFaction";
    private static readonly ProtoId<FactionIconPrototype> ShadowlingThrallIcon = "ShadowlingThrallFaction";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingComponent, GetStatusIconsEvent>(OnShadowlingIcons);
        SubscribeLocalEvent<ShadowlingThrallComponent, GetStatusIconsEvent>(OnThrallIcons);
    }

    private void OnShadowlingIcons(EntityUid uid, ShadowlingComponent component, ref GetStatusIconsEvent ev)
    {
        if (_prototype.TryIndex(ShadowlingIcon, out var icon))
            ev.StatusIcons.Add(icon);
    }

    private void OnThrallIcons(EntityUid uid, ShadowlingThrallComponent component, ref GetStatusIconsEvent ev)
    {
        if (_prototype.TryIndex(ShadowlingThrallIcon, out var icon))
            ev.StatusIcons.Add(icon);
    }
}
