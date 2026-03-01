using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.Body.Events;
using Content.Shared.CollectiveMind;
using Content.Shared.Mind;
using Content.Server.Roles;
using Content.Shared._OpenSpace.NightEye;
using Content.Shared.Antag;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingThrallTumorSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedCollectiveMindSystem _collectiveMind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingThrallTumorComponent, OrganRemovedFromBodyEvent>(OnTumorRemoved);
    }

    private void OnTumorRemoved(EntityUid uid, ShadowlingThrallTumorComponent component, ref OrganRemovedFromBodyEvent args)
    {
        var body = args.OldBody;

        if (HasComp<ShadowlingThrallComponent>(body))
            RemComp<ShadowlingThrallComponent>(body);

        RemComp<NightEyeComponent>(body);
        RemComp<ShowAntagIconsComponent>(body);

        if (TryComp<CollectiveMindComponent>(body, out var cmComp))
            _collectiveMind.UpdateCollectiveMind(body, cmComp);

        if (_mind.TryGetMind(body, out var mindId, out var mind))
        {
            _roles.MindRemoveRoleSilent((mindId, mind), "MindRoleShadowlingThrall");
            RemoveThrallObjectives(mindId, mind);
        }
    }

    private void RemoveThrallObjectives(EntityUid mindId, MindComponent mind)
    {
        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            var objective = mind.Objectives[i];
            if (!HasComp<ShadowlingThrallObjectiveComponent>(objective))
                continue;

            mind.Objectives.RemoveAt(i);

            var stillUsed = false;
            var mindQuery = AllEntityQuery<MindComponent>();
            while (mindQuery.MoveNext(out _, out var otherMind))
            {
                if (otherMind.Objectives.Contains(objective))
                {
                    stillUsed = true;
                    break;
                }
            }

            if (!stillUsed)
                Del(objective);
        }

        Dirty(mindId, mind);
    }
}
