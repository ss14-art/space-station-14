using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Genetics
{
    [Serializable, NetSerializable]
    public enum GeneModificationConsoleUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class GeneModificationConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        
        public GeneModificationConsoleBoundUserInterfaceState()
        {
        }
    }
}

