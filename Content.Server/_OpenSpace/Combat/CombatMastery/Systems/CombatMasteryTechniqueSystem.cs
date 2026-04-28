using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Combat.CombatMastery.Systems;

public abstract class CombatMasteryTechniqueSystem<TComponent> : CombatMasteryTemplateCollectionSystem<TComponent>
    where TComponent : Component, ICombatMasteryTemplateProvider
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    protected void RequestMeleeDamageRefresh(EntityUid uid)
    {
        var ev = new CombatMasteryRefreshMeleeDamageEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    protected void ApplyBluntDamage(EntityUid user, EntityUid target, ProtoId<DamageTypePrototype> damageType, float amount)
    {
        if (!TryComp<DamageableComponent>(target, out _))
            return;

        _damageable.TryChangeDamage(target, CreateBluntDamage(damageType, amount), ignoreResistances: true, origin: user);
    }

    protected DamageSpecifier CreateBluntDamage(ProtoId<DamageTypePrototype> damageType, float amount)
    {
        return new DamageSpecifier(_prototypeManager.Index(damageType), FixedPoint2.New(amount));
    }

    protected void PopupTechnique(
        EntityUid user,
        EntityUid target,
        string userLocKey,
        string targetLocKey,
        bool includeTargetName = false)
    {
        var userMessage = includeTargetName
            ? Loc.GetString(userLocKey, ("target", Identity.Entity(target, EntityManager)))
            : Loc.GetString(userLocKey);
        var targetMessage = Loc.GetString(targetLocKey);

        _popup.PopupEntity(userMessage, user, user);
        _popup.PopupEntity(targetMessage, target, target);
    }

    protected bool IsEntityDown(EntityUid uid)
    {
        return _standing.IsDown(uid)
               || HasComp<KnockedDownComponent>(uid)
               || HasComp<SleepingComponent>(uid);
    }

    protected void StandImmediately(EntityUid uid)
    {
        RemComp<KnockedDownComponent>(uid);
        _standing.Stand(uid);
    }
}
