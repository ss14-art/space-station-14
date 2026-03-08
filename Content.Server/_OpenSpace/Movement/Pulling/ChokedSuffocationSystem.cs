using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._OpenSpace.Movement.Pulling.Components;
namespace Content.Server._OpenSpace.Movement.Pulling;

public sealed class ChokedSuffocationSystem : EntitySystem
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    private const float ChokeDamageScale = 1f / 15f;
    private readonly HashSet<EntityUid> _tracked = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var current = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<ChokedComponent, RespiratorComponent>();
        while (query.MoveNext(out var uid, out _, out var respirator))
        {
            _respirator.ApplyChokedSuffocation(uid, respirator, ChokeDamageScale);
            current.Add(uid);
        }

        foreach (var uid in _tracked)
        {
            if (current.Contains(uid))
                continue;

            if (TryComp(uid, out RespiratorComponent? respirator))
                _respirator.ClearChokedSuffocation(uid, respirator);
        }

        _tracked.Clear();
        _tracked.UnionWith(current);
    }
}
