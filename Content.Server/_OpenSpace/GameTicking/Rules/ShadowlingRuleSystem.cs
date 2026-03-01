using Content.Server.Antag;
using Content.Server._OpenSpace.GameTicking.Rules.Components;
using Content.Server._OpenSpace.Shadowling;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._OpenSpace.GameTicking.Rules;

public sealed class ShadowlingRuleSystem : GameRuleSystem<ShadowlingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ShadowlingHiveSystem _hive = default!;

    protected override void Added(EntityUid uid, ShadowlingRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        UpdateAntagCounts(uid);
    }

    protected override void Started(EntityUid uid, ShadowlingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        UpdateAntagCounts(uid);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ShadowlingRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("shadowling-round-end-count", ("count", antags.Count)));
        args.AddLine(Loc.GetString("shadowling-round-end-thralls", ("count", _hive.GetThrallCount())));
        args.AddLine("");
    }

    private void UpdateAntagCounts(EntityUid uid)
    {
        var players = GameTicker.ReadyPlayerCount();
        var count = GetShadowlingCount(players);
        _antag.UpdateDefinitionCounts(uid, count);
    }

    private static int GetShadowlingCount(int players)
    {
        if (players >= 80)
            return 3;
        if (players >= 60)
            return 2;
        if (players >= 40)
            return 1;
        
        return 0;
    }
}
