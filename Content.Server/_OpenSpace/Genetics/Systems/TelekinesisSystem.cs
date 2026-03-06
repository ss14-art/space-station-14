using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Mobs.Systems;
using Content.Shared._OpenSpace.Genetics.Telekinesis;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class TelekinesisSystem : EntitySystem
{
    private const float TelekinesisRange = 7f;
    private const float MoveSpeed = 10f;
    private const float StopDistance = 0.1f;
    private const float ThrowSpeed = 10f;

    private static readonly ProtoId<TagPrototype> TelekinesisInteractionRangeTag = "TelekinesisInteractionRange";

    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly Dictionary<EntityUid, TelekinesisState> _active = new();

    private sealed class TelekinesisState
    {
        public EntityUid Item;
        public MapCoordinates Target;
        public TimeSpan LastUpdate;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TelekinesisStartRequestEvent>(OnStart);
        SubscribeNetworkEvent<TelekinesisMoveRequestEvent>(OnMove);
        SubscribeNetworkEvent<TelekinesisStopRequestEvent>(OnStop);
    }

    private void OnStart(TelekinesisStartRequestEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user is not { Valid: true })
            return;

        if (!CanUseTelekinesis(user.Value))
            return;

        if (!_tagSystem.HasTag(user.Value, TelekinesisInteractionRangeTag))
            return;

        if (!TryGetEntity(ev.Target, out var target) || target == null || Deleted(target.Value))
            return;

        var targetUid = target.Value;

        if (!HasComp<ItemComponent>(targetUid))
            return;

        if (_containers.IsEntityInContainer(targetUid))
            return;

        if (!TryComp<PhysicsComponent>(targetUid, out var physics))
            return;

        if (physics.BodyType == BodyType.Static || Transform(targetUid).Anchored)
            return;

        var userMap = _transform.GetMapCoordinates(user.Value);
        if (!_interaction.InRangeUnobstructed(userMap, targetUid, TelekinesisRange))
            return;

        foreach (var state in _active.Values)
        {
            if (state.Item == targetUid)
                return;
        }

        _active[user.Value] = new TelekinesisState
        {
            Item = targetUid,
            Target = ev.Cursor,
            LastUpdate = _timing.CurTime
        };
    }

    private void OnMove(TelekinesisMoveRequestEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user is not { Valid: true })
            return;

        if (!CanUseTelekinesis(user.Value))
        {
            StopTelekinesis(user.Value, null, throwItem: false);
            return;
        }

        if (_active.TryGetValue(user.Value, out var state))
        {
            state.Target = ev.Cursor;
            state.LastUpdate = _timing.CurTime;
        }
    }

    private void OnStop(TelekinesisStopRequestEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user is not { Valid: true })
            return;

        StopTelekinesis(user.Value, ev.Cursor, throwItem: CanUseTelekinesis(user.Value));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_active.Count == 0)
            return;

        var toStop = new List<EntityUid>();

        foreach (var (user, state) in _active)
        {
            if (!Exists(user) || Deleted(state.Item) || !Exists(state.Item))
            {
                toStop.Add(user);
                continue;
            }

            if (!_tagSystem.HasTag(user, TelekinesisInteractionRangeTag))
            {
                toStop.Add(user);
                continue;
            }

            if (!CanUseTelekinesis(user))
            {
                toStop.Add(user);
                continue;
            }

            if (_containers.IsEntityInContainer(state.Item))
            {
                toStop.Add(user);
                continue;
            }

            var userMap = _transform.GetMapCoordinates(user);
            if (state.Target.MapId != userMap.MapId)
            {
                toStop.Add(user);
                continue;
            }

            var itemMap = _transform.GetMapCoordinates(state.Item);
            var desired = ClampTarget(userMap, state.Target);
            var toTarget = desired.Position - itemMap.Position;

            if (toTarget.LengthSquared() <= StopDistance * StopDistance)
            {
                if (TryComp<PhysicsComponent>(state.Item, out var physics))
                    _physics.SetLinearVelocity(state.Item, Vector2.Zero, body: physics);
                continue;
            }

            if (toTarget.Length() > TelekinesisRange + 1f)
            {
                toStop.Add(user);
                continue;
            }

            if (!TryComp<PhysicsComponent>(state.Item, out var body))
            {
                toStop.Add(user);
                continue;
            }

            var speed = MathF.Min(MoveSpeed, toTarget.Length() * 6f);
            var velocity = Vector2.Normalize(toTarget) * speed;
            _physics.SetLinearVelocity(state.Item, velocity, body: body);
        }

        foreach (var user in toStop)
            StopTelekinesis(user, null, throwItem: false);
    }

    private void StopTelekinesis(EntityUid user, MapCoordinates? releaseTarget, bool throwItem)
    {
        if (!_active.TryGetValue(user, out var state))
            return;

        _active.Remove(user);

        if (!Exists(state.Item) || Deleted(state.Item))
            return;

        if (!TryComp<PhysicsComponent>(state.Item, out var physics))
            return;

        if (!throwItem || releaseTarget == null)
        {
            _physics.SetLinearVelocity(state.Item, Vector2.Zero, body: physics);
            return;
        }

        var userMap = _transform.GetMapCoordinates(user);
        var target = ClampTarget(userMap, releaseTarget.Value);
        var itemMap = _transform.GetMapCoordinates(state.Item);
        var dir = target.Position - itemMap.Position;

        if (dir.LengthSquared() <= StopDistance * StopDistance)
            return;

        _throwing.TryThrow(state.Item, dir, ThrowSpeed, user);

        var thrown = EnsureComp<TelekinesisThrownComponent>(state.Item);
        thrown.Thrower = user;
        Dirty(state.Item, thrown);
    }

    private MapCoordinates ClampTarget(MapCoordinates origin, MapCoordinates target)
    {
        if (target.MapId != origin.MapId)
            return origin;

        var dir = target.Position - origin.Position;
        if (dir.LengthSquared() < 0.0001f)
            return origin;

        var distance = dir.Length();
        var maxRange = MathF.Min(TelekinesisRange, distance);

        var unobstructed = _interaction.UnobstructedDistance(origin, target);
        if (unobstructed < maxRange)
            maxRange = unobstructed;

        var clampedPos = origin.Position + Vector2.Normalize(dir) * maxRange;
        return new MapCoordinates(clampedPos, origin.MapId);
    }

    private bool CanUseTelekinesis(EntityUid user)
    {
        return !_mobState.IsDead(user) && !_mobState.IsCritical(user);
    }
}

