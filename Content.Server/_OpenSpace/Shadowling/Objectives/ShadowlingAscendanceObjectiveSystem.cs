using Content.Shared.Objectives.Components;
using Content.Server._OpenSpace.Shadowling;

namespace Content.Server._OpenSpace.Shadowling.Objectives;

public sealed class ShadowlingAscendanceObjectiveSystem : EntitySystem
{
    [Dependency] private readonly ShadowlingHiveSystem _hive = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingAscendanceObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ShadowlingAscendanceObjectiveComponent component, ref ObjectiveGetProgressEvent args)
    {
        var required = Math.Max(1, component.RequiredThralls);
        var count = _hive.GetThrallCount();
        var progress = Math.Clamp(count / (float) required, 0f, 1f);
        args.Progress = progress;
    }
}
