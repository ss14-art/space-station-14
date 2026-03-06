using System;
using Content.Shared.Genetics;
using Robust.Client.GameObjects;

namespace Content.Client._OpenSpace.Genetics.UI
{
    public sealed class GeneticsConsoleBoundUserInterface : BoundUserInterface
    {
        private GeneticsConsoleWindow? _window;

        public GeneticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new GeneticsConsoleWindow();
            _window.OnClose += Close;

            _window.OnSelectBlock += (blockIndex, isUiBlock) =>
            {
                SendMessage(new GeneticsConsoleSelectBlockMessage(blockIndex, isUiBlock));
            };

            _window.OnIrradiateBlock += (blockIndex, isUiBlock) =>
            {
                SendMessage(new GeneticsConsoleIrradiateBlockMessage(blockIndex, isUiBlock));
            };

            _window.OnInject += (amount) =>
            {
                SendMessage(new GeneticsConsoleInjectMessage(amount));
            };

            _window.OnEjectBeaker += () =>
            {
                SendMessage(new GeneticsConsoleEjectBeakerMessage());
            };

            _window.OnToggleScannerLock += locked =>
            {
                SendMessage(new GeneticsConsoleToggleScannerLockMessage(locked));
            };

            _window.OnStoreBuffer += (bufferIndex, mode) =>
            {
                SendMessage(new GeneticsConsoleStoreBufferMessage(bufferIndex, mode));
            };

            _window.OnTransferBuffer += (bufferIndex, target) =>
            {
                SendMessage(new GeneticsConsoleTransferBufferMessage(bufferIndex, target));
            };

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not GeneticsConsoleBoundUserInterfaceState cast)
                return;

            _window?.UpdateState(cast);
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

