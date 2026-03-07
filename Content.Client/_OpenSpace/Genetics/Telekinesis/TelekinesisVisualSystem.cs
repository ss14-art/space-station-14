using Content.Shared._OpenSpace.Genetics.Telekinesis;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._OpenSpace.Genetics.Telekinesis;

public sealed class TelekinesisVisualSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelekinesisVisualComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TelekinesisVisualComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<TelekinesisVisualComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteEnt = (ent.Owner, sprite);
        if (!_sprite.LayerMapTryGet(spriteEnt, TelekinesisVisualComponent.LayerKey, out _, false))
            _sprite.LayerMapReserve(spriteEnt, TelekinesisVisualComponent.LayerKey);

        var layer = _sprite.LayerMapGet(spriteEnt, TelekinesisVisualComponent.LayerKey);
        _sprite.LayerSetRsi(spriteEnt, layer, new ResPath(TelekinesisVisualComponent.RsiPath));
        _sprite.LayerSetRsiState(spriteEnt, layer, TelekinesisVisualComponent.State);
        _sprite.LayerSetOffset(spriteEnt, layer, TelekinesisVisualComponent.Offset);
        _sprite.LayerSetVisible(spriteEnt, layer, true);
    }

    private void OnShutdown(Entity<TelekinesisVisualComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.RemoveLayer((ent.Owner, sprite), TelekinesisVisualComponent.LayerKey);
    }
}

