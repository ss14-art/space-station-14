using Content.Shared._OpenSpace.NightEye;
using Robust.Shared.Timing;

namespace Content.Client._OpenSpace.NightEye;

public sealed class NightEyeSystem : SharedNightEyeSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override bool IsPredict() => !_timing.IsFirstTimePredicted;
}
