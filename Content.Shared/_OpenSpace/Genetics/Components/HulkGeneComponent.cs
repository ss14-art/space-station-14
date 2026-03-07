using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Genetics.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class HulkGeneComponent : Component
    {
        
        
        
        [ViewVariables, DataField, AutoNetworkedField]
        public bool IsTransformed;

        
        
        
        [ViewVariables]
        public string Variant = "Hulk";

        
        
        
        [ViewVariables]
        public float OriginalDeadThreshold;

        
        
        
        [ViewVariables]
        public float OriginalCritThreshold;

        
        
        
        [ViewVariables]
        public float OriginalSprintSpeed;

        
        
        
        [ViewVariables]
        public float OriginalWalkSpeed;

        
        
        
        [ViewVariables]
        public float OriginalAcceleration;

        
        
        
        [ViewVariables]
        public Color OriginalSkinColor;

        
        
        
        [ViewVariables]
        public System.Numerics.Vector2 OriginalScale;

        
        
        
        [ViewVariables]
        public float FixtureScaleFactor = 1f;

        
        
        
        [ViewVariables]
        public HashSet<EntityUid> SuppressedActions = new();
    }
}

