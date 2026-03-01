using Content.Shared._OpenSpace.Actions;
using Content.Shared._OpenSpace.Shadowling;
using Content.Server.Polymorph.Systems;
using Content.Server.RoundEnd;


namespace Content.Server._OpenSpace.Shadowling.Actions;

public sealed class ShadowlingAscendanceSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingAscendanceActionEvent>(OnAction);
    }

    private void OnAction(ShadowlingAscendanceActionEvent ev)
    {
        if (ev.Handled)
            return;

        var performer = ev.Performer;

        if (!HasComp<ShadowlingComponent>(performer))
            return;

        _polymorph.PolymorphEntity(performer, "ShadowlingAscendance");
        _roundEnd.EndRound();
        ev.Handled = true;
    }
}
