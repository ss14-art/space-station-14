using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._OpenSpace.Genetics.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class GeneticsComponent : Component
    {
        
        
        
        
        
        [DataField("uiDna"), ViewVariables]
        public Dictionary<int, string> UiDna = new();

        
        
        
        
        
        [DataField("seDna"), ViewVariables]
        public Dictionary<int, string> SeDna = new();

        
        
        
        [DataField("innateGenes"), ViewVariables]
        public HashSet<string> InnateGenes = new();

        
        
        
        [ViewVariables]
        public int Instability = 0;

        
        
        
        [ViewVariables]
        public int RadiationExposure = 0;
    }
}

