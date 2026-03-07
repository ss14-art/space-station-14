using Content.Shared._OpenSpace.Genetics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Content.Shared.Containers.ItemSlots;
using System.Collections.Generic;

namespace Content.Shared._OpenSpace.Genetics.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class GeneticsConsoleComponent : Component
    {
        public const int TransferBufferCount = 6;

        [ViewVariables]
        public EntityUid? GeneticScanner = null;

        [ViewVariables]
        public int SelectedUiBlock = -1;

        [ViewVariables]
        public int SelectedSeBlock = -1;

        [DataField]
        public ItemSlot BeakerSlot = new();

        [DataField]
        public ItemSlot SyringeSlot = new();

        [ViewVariables]
        public List<GeneticsTransferBuffer> TransferBuffers = new();
    }
}

