using Content.Server.Antag;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private const string ShadowlingRoleId = "MindRoleShadowling";
    private const string AscendanceObjectiveId = "ShadowlingAscendanceObjective";
    private static readonly SoundSpecifier ShadowlingSound = new SoundPathSpecifier("/Audio/_OpenSpace/Shadowling/shadowling.ogg");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        var mindId = args.MindId;
        var mind = args.Mind;

        if (!MindHasShadowlingRole(mind))
            return;

        if (_mind.TryFindObjective((mindId, mind), AscendanceObjectiveId, out _))
            return;

        _mind.TryAddObjective(mindId, mind, AscendanceObjectiveId);

        if (!HasComp<ShadowlingAntagSoundPlayedComponent>(mindId))
        {
            if (_player.TryGetSessionById(mind.UserId, out var session))
            {
                _audio.PlayGlobal(ShadowlingSound, session);
                _antag.SendBriefing(session, Loc.GetString("shadowling-role-greeting"), Color.MediumPurple, null);
            }

            EnsureComp<ShadowlingAntagSoundPlayedComponent>(mindId);
        }
    }

    private bool MindHasShadowlingRole(MindComponent mind)
    {
        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            var proto = MetaData(role).EntityPrototype?.ID;
            if (proto == ShadowlingRoleId)
                return true;
        }

        return false;
    }
}
