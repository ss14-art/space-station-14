using Content.Server._Starlight.Shadekin;
using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Examine;
using Content.Shared.Light.Components;
using Content.Server.Light.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingLightSensitivitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ShadekinSystem _shadekin = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly SoundSpecifier BurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
    private sealed class LightCone
    {
        public float Direction { get; set; }
        public float InnerWidth { get; set; }
        public float OuterWidth { get; set; }
    }

    private static readonly Dictionary<string, List<LightCone>> LightMasks = new()
    {
        ["/Textures/Effects/LightMasks/cone.png"] = new List<LightCone>
        {
            new LightCone { Direction = 0f, InnerWidth = 30f, OuterWidth = 60f }
        },
        ["/Textures/Effects/LightMasks/double_cone.png"] = new List<LightCone>
        {
            new LightCone { Direction = 0f, InnerWidth = 30f, OuterWidth = 60f },
            new LightCone { Direction = 180f, InnerWidth = 30f, OuterWidth = 60f }
        }
    };

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ShadowlingComponent, ShadowlingLightSensitivityComponent, DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var sensitivity, out _, out var mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (_timing.CurTime < sensitivity.NextUpdate)
                continue;

            sensitivity.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(0.1f, sensitivity.UpdateInterval));

            var exposure = _shadekin.GetLightExposure(uid);
            var directExposure = GetDirectLightExposure(uid);
            if (directExposure > exposure)
                exposure = directExposure;
            var inSpace = !_turf.TryGetTileRef(Transform(uid).Coordinates, out var tileRef) ||
                          _turf.IsSpace(tileRef.Value);
            if (inSpace)
                exposure = Math.Max(exposure, sensitivity.LightThreshold);

            if (exposure >= sensitivity.LightThreshold)
            {
                if (sensitivity.BurnDamage <= 0f)
                    continue;

                var heatType = _prototypes.Index<DamageTypePrototype>(sensitivity.HeatType);
                var spec = new DamageSpecifier(heatType, FixedPoint2.New(sensitivity.BurnDamage));
                _damageable.TryChangeDamage(uid, spec, true, false);
                _audio.PlayPvs(BurnSound, uid);
            }
            else if (exposure <= sensitivity.DarkThreshold)
            {
                var spec = new DamageSpecifier();
                var healAmount = FixedPoint2.New(sensitivity.BurnHeal);
                var damage = Comp<DamageableComponent>(uid).Damage;

                foreach (var (type, amount) in damage.DamageDict)
                {
                    if (amount <= FixedPoint2.Zero)
                        continue;

                    var heal = FixedPoint2.Min(amount, healAmount);
                    if (heal <= FixedPoint2.Zero)
                        continue;

                    spec.DamageDict[type] = -heal;
                }

                if (spec.Empty || spec.GetTotal() == FixedPoint2.Zero)
                    continue;

                _damageable.TryChangeDamage(uid, spec, true, false);
            }
        }
    }

    private float GetDirectLightExposure(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;
        var best = 0f;
        foreach (var ent in _lookup.GetEntitiesInRange<PointLightComponent>(coords, 10f, LookupFlags.All | LookupFlags.Approximate))
        {
            best = Math.Max(best, CheckDirectLight(ent, uid, null));
        }

        foreach (var holder in _lookup.GetEntitiesInRange<HandsComponent>(coords, 10f, LookupFlags.All | LookupFlags.Approximate))
        {
            if (!TryComp<HandsComponent>(holder, out var hands))
                continue;

            foreach (var handName in hands.Hands.Keys)
            {
                if (!_hands.TryGetHeldItem((holder, hands), handName, out var held) || held == null)
                    continue;

                best = Math.Max(best, CheckDirectLight(held.Value, uid, holder));
            }
        }

        foreach (var holder in _lookup.GetEntitiesInRange<InventoryComponent>(coords, 10f, LookupFlags.All | LookupFlags.Approximate))
        {
            if (!TryComp<InventoryComponent>(holder, out var inventory))
                continue;

            if (!_inventory.TryGetSlots(holder, out var slots))
                continue;

            foreach (var slot in slots)
            {
                if (!_inventory.TryGetSlotEntity(holder, slot.Name, out var item) || item == null)
                    continue;

                best = Math.Max(best, CheckDirectLight(item.Value, uid, holder));
            }
        }

        return best;
    }

    private float CheckDirectLight(EntityUid lightUid, EntityUid targetUid, EntityUid? holderUid)
    {
        if (!TryComp<PointLightComponent>(lightUid, out var light))
            return 0f;

        var hasHandheld = TryComp<HandheldLightComponent>(lightUid, out var handheld);
        var hasExpendable = TryComp<ExpendableLightComponent>(lightUid, out var expendable);

        var handheldActive = hasHandheld && handheld!.Activated;
        var expendableActive = hasExpendable && expendable!.Activated;

        if (hasHandheld && !handheldActive)
            return 0f;

        if (hasExpendable && !expendableActive)
            return 0f;

        var radius = light.Radius <= 0f ? 0f : light.Radius;
        if (radius <= 0f)
            return 0f;

        // Expendable lights (glowsticks) may keep point light disabled or energy at 0 server-side.
        if (!expendableActive)
        {
            if (!light.Enabled)
                return 0f;

            if (light.Energy <= 0f && !handheldActive)
                return 0f;
        }

        var sourceUid = holderUid ?? lightUid;
        if (!_examine.InRangeUnOccluded(sourceUid, targetUid, radius, null))
            return 0f;

        if (!IsLightFacingTarget(lightUid, light, targetUid, holderUid))
            return 0f;

        var energy = light.Energy;
        if (expendableActive && energy <= 0f)
            energy = 3f;

        var (lightPos, lightRot) = _xform.GetWorldPositionRotation(lightUid);
        if (holderUid != null && light.MaskAutoRotate)
        {
            var holderRot = _xform.GetWorldRotation(holderUid.Value);
            var holderPos = _xform.GetWorldPosition(holderUid.Value);
            lightPos = holderPos + holderRot.RotateVec(light.Offset);
            lightRot = holderRot;
        }
        else
        {
            lightPos += lightRot.RotateVec(light.Offset);
        }

        var targetPos = _xform.GetWorldPosition(targetUid);
        var dist = (targetPos - lightPos).Length();
        var denom = dist / radius;
        var attenuation = 1 - (denom * denom);
        if (attenuation <= 0f)
            return 0f;

        return energy * attenuation * attenuation;
    }

    private bool IsLightFacingTarget(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid, EntityUid? holderUid)
    {
        if (lightComp.MaskPath == null)
            return true;

        if (!LightMasks.TryGetValue(lightComp.MaskPath, out var cones))
            return true;

        var angle = GetAngle(lightUid, lightComp, targetUid, holderUid);
        if (double.IsNaN(angle.Degrees))
            return true;

        foreach (var cone in cones)
        {
            var delta = Math.Abs(NormalizeDegrees(angle.Degrees - cone.Direction));
            if (delta <= cone.OuterWidth)
                return true;
        }

        return false;
    }

    private Angle GetAngle(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid, EntityUid? holderUid)
    {
        var (lightPos, lightRot) = _xform.GetWorldPositionRotation(lightUid);

        if (holderUid != null && lightComp.MaskAutoRotate)
        {
            var holderRot = _xform.GetWorldRotation(holderUid.Value);
            var holderPos = _xform.GetWorldPosition(holderUid.Value);
            lightPos = holderPos + holderRot.RotateVec(lightComp.Offset);
            lightRot = holderRot;
        }
        else
        {
            lightPos += lightRot.RotateVec(lightComp.Offset);
        }

        var (targetPos, _) = _xform.GetWorldPositionRotation(targetUid);
        var mapDiff = targetPos - lightPos;

        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        return oppositeMapDiff.ToWorldAngle();
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        if (degrees > 180.0)
            degrees -= 360.0;
        if (degrees < -180.0)
            degrees += 360.0;
        return degrees;
    }
}
