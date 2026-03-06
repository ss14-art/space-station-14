using Content.Shared._OpenSpace.Genetics.Telekinesis;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._OpenSpace.Genetics.Telekinesis;

public sealed class TelekinesisClientSystem : EntitySystem
{
    private const float MoveSendInterval = 0.05f;

    private static readonly ProtoId<TagPrototype> TelekinesisInteractionRangeTag = "TelekinesisInteractionRange";

    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private bool _active;
    private float _moveAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.AltActivateItemInWorld,
                new PointerInputCmdHandler(OnAltUse, false, true),
                new[] { typeof(SharedInteractionSystem) })
            .Register<TelekinesisClientSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<TelekinesisClientSystem>();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_active)
            return;

        var user = _player.LocalEntity;
        if (user == null || !HasTelekinesis(user.Value))
        {
            _active = false;
            return;
        }

        _moveAccumulator += frameTime;
        if (_moveAccumulator < MoveSendInterval)
            return;

        _moveAccumulator = 0f;
        RaiseNetworkEvent(new TelekinesisMoveRequestEvent(GetCursorMapCoords()));
    }

    private bool OnAltUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State == BoundKeyState.Down)
            return OnAltUseDown(args);

        if (args.State == BoundKeyState.Up)
            return OnAltUseUp();

        return false;
    }

    private bool OnAltUseDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        var user = _player.LocalEntity;
        if (user == null || !HasTelekinesis(user.Value))
            return false;

        if (!args.EntityUid.IsValid() || !HasComp<ItemComponent>(args.EntityUid))
            return false;

        _active = true;
        _moveAccumulator = 0f;

        RaiseNetworkEvent(new TelekinesisStartRequestEvent(GetNetEntity(args.EntityUid), GetCursorMapCoords()));
        return true;
    }

    private bool OnAltUseUp()
    {
        if (!_active)
            return false;

        _active = false;
        RaiseNetworkEvent(new TelekinesisStopRequestEvent(GetCursorMapCoords()));
        return true;
    }

    private bool HasTelekinesis(EntityUid user)
    {
        if (!_tagSystem.HasTag(user, TelekinesisInteractionRangeTag))
            return false;

        if (HasComp<Content.Shared._Starlight.Computers.RemoteEye.RemoteEyeActorComponent>(user))
            return false;

        return true;
    }

    private MapCoordinates GetCursorMapCoords()
    {
        return _eye.PixelToMap(_input.MouseScreenPosition);
    }
}

