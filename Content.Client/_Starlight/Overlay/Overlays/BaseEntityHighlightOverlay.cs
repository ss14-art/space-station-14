using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._OpenSpace.Shadowling;

namespace Content.Client._Starlight.Overlay;

public abstract class BaseEntityHighlightOverlay : BaseVisionOverlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly ContainerSystem _containerSystem;
    private readonly TransformSystem _transform = default!;
    public BaseEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        _containerSystem = _entityManager.System<ContainerSystem>();
        _transform = _entityManager.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        worldHandle.UseShader(_shader);
        var query = _entityManager.EntityQueryEnumerator<MobStateComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var meta, out var sprite, out var xform))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta)) continue;
            if (_entityManager.HasComponent<ShadowlingShadowWalkPhasedComponent>(uid) ||
                _entityManager.HasComponent<ShadowlingShadowWalkInvisibleComponent>(uid))
                continue;
            var (position, rotation) = _transform.GetWorldPositionRotation(xform);

            sprite.Render(worldHandle, eyeRotation, rotation, null, position);
        }

        worldHandle.UseShader(null);
    }
}
