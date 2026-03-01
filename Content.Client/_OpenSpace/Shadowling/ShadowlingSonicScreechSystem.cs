using Content.Client.PhysicsSystem.Controllers;
using Content.Shared._OpenSpace.Shadowling;
using Robust.Client.Player;
using Robust.Shared.Maths;

namespace Content.Client._OpenSpace.Shadowling;

public sealed class ShadowlingSonicScreechSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MoverController _mover = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShadowlingSonicScreechScreenEvent>(OnScreenEvent);
    }

    private void OnScreenEvent(ShadowlingSonicScreechScreenEvent ev)
    {
        var uid = _player.LocalSession?.AttachedEntity;
        if (uid == null)
            return;
        _mover.RotateCamera(uid.Value, Direction.East.ToAngle());
    }
}
