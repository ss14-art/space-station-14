using Content.Client._Starlight.Overlay;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared._OpenSpace.Shadowling;

namespace Content.Client._OpenSpace.ThermalEye;

public sealed class ThermalEyeEntityHighlightOverlay : BaseVisionOverlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly ContainerSystem _containerSystem;
    private readonly TransformSystem _transform;

    public ThermalEyeEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        _containerSystem = _entityManager.System<ContainerSystem>();
        _transform = _entityManager.System<TransformSystem>();
        ZIndex = (int?)OverlayZIndexes.ThermalVisionEntityHighlight;
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

            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta))
                continue;
            if (_entityManager.HasComponent<ShadowlingShadowWalkPhasedComponent>(uid) ||
                _entityManager.HasComponent<ShadowlingShadowWalkInvisibleComponent>(uid))
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            sprite.Render(worldHandle, eyeRotation, rotation, null, position);
        }

        worldHandle.UseShader(null);
    }
}
