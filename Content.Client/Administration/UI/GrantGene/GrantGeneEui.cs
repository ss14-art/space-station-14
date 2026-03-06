using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.GrantGene;

[UsedImplicitly]
public sealed class GrantGeneEui : BaseEui
{
    private readonly GrantGeneWindow _window;

    public GrantGeneEui()
    {
        _window = new GrantGeneWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnGrantGene += geneId => SendMessage(new GrantGeneEuiMsg.Grant(geneId));
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase baseState)
    {
        _ = (GrantGeneEuiState) baseState;
    }
}
