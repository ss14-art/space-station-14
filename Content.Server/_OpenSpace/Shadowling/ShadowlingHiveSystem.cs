using Content.Server._OpenSpace.GameTicking.Rules.Components;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingHiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingRuleComponent, MapInitEvent>(OnRuleInit);
    }

    private void OnRuleInit(EntityUid uid, ShadowlingRuleComponent component, MapInitEvent args)
    {
        EnsureComp<ShadowlingHiveComponent>(uid);
    }

    public int GetAliveThrallCount()
    {
        return TryGetHive(out _, out var hive) && hive != null ? hive.AliveThralls : 0;
    }

    public int GetThrallCount()
    {
        return TryGetHive(out _, out var hive) && hive != null ? hive.Thralls.Count : 0;
    }

    public void RegisterThrall(EntityUid thrall, bool alive)
    {
        if (!TryGetHive(out _, out var hive))
            return;
        if (hive == null)
            return;

        if (hive.Thralls.Add(thrall) && alive)
            hive.AliveThralls += 1;
    }

    public void SetThrallAlive(EntityUid thrall, bool alive)
    {
        if (!TryGetHive(out _, out var hive))
            return;
        if (hive == null)
            return;

        if (!hive.Thralls.Contains(thrall))
            hive.Thralls.Add(thrall);

        if (alive)
            hive.AliveThralls += 1;
        else
            hive.AliveThralls = Math.Max(0, hive.AliveThralls - 1);
    }

    public void UnregisterThrall(EntityUid thrall, bool wasAlive)
    {
        if (!TryGetHive(out _, out var hive))
            return;
        if (hive == null)
            return;

        if (hive.Thralls.Remove(thrall) && wasAlive)
            hive.AliveThralls = Math.Max(0, hive.AliveThralls - 1);
    }

    private bool TryGetHive(out EntityUid uid, out ShadowlingHiveComponent? hive)
    {
        var query = EntityQueryEnumerator<ShadowlingHiveComponent>();
        if (query.MoveNext(out uid, out hive))
            return true;

        uid = default;
        hive = null;
        return false;
    }
}
