using System;
using Content.Shared._OpenSpace.Effects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._OpenSpace.Effects;

public sealed class OutlineFlashEffectSystem : SharedOutlineFlashEffectSystem
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "SelectionOutlineInrange";
    private const float OutlineWidth = 1f;
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.30f);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<OutlineFlashEffectEvent>(OnOutlineFlashEffect);
    }

    public override void RaiseEffect(EntityUid target, EntityUid? viewer = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        OnOutlineFlashEffect(new OutlineFlashEffectEvent(GetNetEntity(target),
            viewer != null ? GetNetEntity(viewer.Value) : null));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<OutlineFlashEffectComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;

            if (comp.AppliedShader != null && sprite.PostShader == comp.AppliedShader)
            {
                sprite.PostShader = null;
                sprite.RenderOrder = 0u;
            }

            comp.AppliedShader = null;
            RemCompDeferred<OutlineFlashEffectComponent>(uid);
        }
    }

    private void OnOutlineFlashEffect(OutlineFlashEffectEvent ev)
    {
        if (ev.Viewer != null)
        {
            var local = _playerManager.LocalEntity;
            if (local == null || GetNetEntity(local.Value) != ev.Viewer.Value)
                return;
        }

        var ent = GetEntity(ev.Target);
        if (Deleted(ent) || !TryComp(ent, out SpriteComponent? sprite))
            return;

        if (!TryComp(ent, out OutlineFlashEffectComponent? comp))
            comp = EnsureComp<OutlineFlashEffectComponent>(ent);

        var shader = _prototypeManager.Index(ShaderId).InstanceUnique();
        shader.SetParameter("outline_width", OutlineWidth);
        sprite.PostShader = shader;
        sprite.RenderOrder = (uint) EntityManager.CurrentTick.Value;
        comp.AppliedShader = shader;
        comp.EndTime = _timing.CurTime + Duration;
    }
}
