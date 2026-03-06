using Robust.Shared.Serialization;

namespace Content.Shared.Genetics
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

