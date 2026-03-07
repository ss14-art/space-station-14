using Content.Shared._OpenSpace.Genetics.Telekinesis;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class TelekinesisDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelekinesisThrownComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<TelekinesisThrownComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<TelekinesisThrownComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnThrowHit(Entity<TelekinesisThrownComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(ent.Owner, out var melee))
            return;

        if (!HasComp<DamageableComponent>(args.Target))
            return;

        var damage = melee.Damage * melee.ClickDamageModifier;
        _damageable.TryChangeDamage(args.Target, damage, origin: ent.Comp.Thrower, ignoreResistances: melee.ResistanceBypass);
    }

    private void OnLand(Entity<TelekinesisThrownComponent> ent, ref LandEvent args)
    {
        RemCompDeferred<TelekinesisThrownComponent>(ent.Owner);
    }

    private void OnStopThrow(Entity<TelekinesisThrownComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred<TelekinesisThrownComponent>(ent.Owner);
    }
}

