using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Throwing;

public sealed class ThrowImpactDamageSystem : EntitySystem
{
    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowImpactDamageComponent, ThrowDoHitEvent>(OnThrowDoHit);
        SubscribeLocalEvent<ThrowImpactDamageComponent, LandEvent>(OnLand);
    }

    private void OnThrowDoHit(Entity<ThrowImpactDamageComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp<PhysicsComponent>(ent.Owner, out var physics))
            return;

        var speed = physics.LinearVelocity.Length();
        if (speed <= 0f)
            return;

        const float baseSpeed = 10f;
        var scale = speed / baseSpeed;
        var bluntAmount = ent.Comp.BluntDamage * scale;

        if (HasComp<MobStateComponent>(ent.Owner) && TryComp<DamageableComponent>(args.Target, out _))
        {
            var targetBlunt = new DamageSpecifier(_prototypeManager.Index(BluntDamageType),
                FixedPoint2.New(ent.Comp.BluntDamage * scale));
            _damageable.TryChangeDamage(args.Target, targetBlunt, true, false);
        }

        if (TryComp<DamageableComponent>(ent.Owner, out _))
        {
            var blunt = new DamageSpecifier(_prototypeManager.Index(BluntDamageType),
                FixedPoint2.New(bluntAmount));
            _damageable.TryChangeDamage(ent.Owner, blunt, true, false);
        }

        if (HasComp<MobStateComponent>(ent.Owner) && TryComp<StandingStateComponent>(args.Target, out _))
        {
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(2), refresh: true, autoStand: true, drop: true, force: true);
            var dropEv = new DropHandItemsEvent();
            RaiseLocalEvent(args.Target, ref dropEv);
        }

        RemComp<ThrowImpactDamageComponent>(ent.Owner);
    }

    private void OnLand(Entity<ThrowImpactDamageComponent> ent, ref LandEvent args)
    {
        RemComp<ThrowImpactDamageComponent>(ent.Owner);
    }
}
