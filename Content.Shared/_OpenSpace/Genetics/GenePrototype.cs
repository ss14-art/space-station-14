using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Genetics
{
    [Prototype("gene")]
    public sealed partial class GenePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        [DataField("description")]
        public string Description { get; private set; } = string.Empty;

        
        
        
        
        
        [DataField("instability")]
        public int Instability { get; private set; } = 0;

        
        
        
        
        [DataField("activationMin")]
        public string? ActivationMin { get; private set; }
    }
}

