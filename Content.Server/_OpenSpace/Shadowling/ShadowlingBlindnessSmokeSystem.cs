using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared._OpenSpace.Shadowling;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingBlindnessSmokeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private const float HealRange = 0.45f;
    private const string BruteGroupId = "Brute";
    private const string BurnGroupId = "Burn";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (frameTime <= 0f)
            return;

        var inSmoke = new HashSet<EntityUid>();

        var smokeQuery = EntityQueryEnumerator<ShadowlingBlindnessSmokeComponent, TransformComponent>();
        while (smokeQuery.MoveNext(out var smokeUid, out var smokeComp, out var smokeXform))
        {
            foreach (var target in _lookup.GetEntitiesInRange(smokeXform.Coordinates, HealRange))
            {
                if (!HasComp<ShadowlingThrallComponent>(target))
                    continue;

                inSmoke.Add(target);

                var active = EnsureComp<ShadowlingBlindnessSmokeAffectedComponent>(target);
                if (_timing.CurTime < active.NextHeal)
                    continue;

                active.NextHeal = _timing.CurTime + TimeSpan.FromSeconds(1);
                var heal = smokeComp.HealPerSecond;
                if (heal <= 0f)
                    continue;

                var spec = new DamageSpecifier();
                spec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BruteGroupId), -FixedPoint2.New(heal));
                spec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BurnGroupId), -FixedPoint2.New(heal));

                _damageable.TryChangeDamage(target, spec, ignoreResistances: true, interruptsDoAfters: false);
            }
        }

        var cleanupQuery = EntityQueryEnumerator<ShadowlingBlindnessSmokeAffectedComponent>();
        while (cleanupQuery.MoveNext(out var uid, out _))
        {
            if (!inSmoke.Contains(uid))
                RemComp<ShadowlingBlindnessSmokeAffectedComponent>(uid);
        }
    }
}
