using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Shared.Genetics.Components
{
    [RegisterComponent]
    public sealed partial class GeneticSyringeComponent : Component
    {
        [ViewVariables]
        public string SubjectName = string.Empty;

        [ViewVariables]
        public Dictionary<int, string>? UiDna;

        [ViewVariables]
        public Dictionary<int, string>? SeDna;

        public bool HasUi => UiDna != null && UiDna.Count > 0;
        public bool HasSe => SeDna != null && SeDna.Count > 0;
        public bool HasData => HasUi || HasSe;
    }
}

