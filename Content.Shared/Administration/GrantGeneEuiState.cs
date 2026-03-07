using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class GrantGeneEuiState(NetEntity target) : EuiStateBase
{
    public NetEntity Target { get; } = target;
}

public static class GrantGeneEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Grant(string geneId) : EuiMessageBase
    {
        public string GeneId { get; } = geneId;
    }
}
