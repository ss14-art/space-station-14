using System;
using System.Collections.Generic;
using Content.Shared._OpenSpace.Shadowling;
using Robust.Client.GameObjects;

namespace Content.Client._OpenSpace.Shadowling;

public sealed class ShadowlingShadowWalkInvisibilitySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, float> _previousAlpha = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingShadowWalkInvisibleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ShadowlingShadowWalkInvisibleComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadowlingShadowWalkInvisibleComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (!_previousAlpha.TryGetValue(uid, out var prev))
            {
                _previousAlpha[uid] = sprite.Color.A;
                prev = sprite.Color.A;
            }

            var targetAlpha = comp.IsVisible ? prev : 0f;
            if (Math.Abs(sprite.Color.A - targetAlpha) > 0.001f)
                sprite.Color = sprite.Color.WithAlpha(targetAlpha);
        }
    }

    private void OnStartup(EntityUid uid, ShadowlingShadowWalkInvisibleComponent component, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            _previousAlpha[uid] = sprite.Color.A;
    }

    private void OnShutdown(EntityUid uid, ShadowlingShadowWalkInvisibleComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            if (_previousAlpha.TryGetValue(uid, out var prev))
                sprite.Color = sprite.Color.WithAlpha(prev);
        }

        _previousAlpha.Remove(uid);
    }
}
