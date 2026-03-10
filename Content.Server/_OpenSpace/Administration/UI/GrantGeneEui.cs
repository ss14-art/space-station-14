using Content.Server.EUI;
using Content.Server._OpenSpace.Genetics.Systems;
using Content.Shared.Administration;
using Content.Shared._OpenSpace.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server._OpenSpace.Administration.UI;

[UsedImplicitly]
public sealed class GrantGeneEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly GeneticsSystem _genetics;
    private readonly EntityUid _target;

    public GrantGeneEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);
        _genetics = _entityManager.System<GeneticsSystem>();
        _target = target;
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not GrantGeneEuiMsg.Grant grant)
            return;

        _genetics.TryGrantGene(_target, grant.GeneId);
    }

    public override EuiStateBase GetNewState()
    {
        return new GrantGeneEuiState(_entityManager.GetNetEntity(_target));
    }
}
