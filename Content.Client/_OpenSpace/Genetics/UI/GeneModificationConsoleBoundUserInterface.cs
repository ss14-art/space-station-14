using Content.Shared.Genetics;
using Robust.Client.GameObjects;

namespace Content.Client._OpenSpace.Genetics.UI
{
    public sealed class GeneModificationConsoleBoundUserInterface : BoundUserInterface
    {
        private GeneModificationConsoleWindow? _window;

        public GeneModificationConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new GeneModificationConsoleWindow();
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}

