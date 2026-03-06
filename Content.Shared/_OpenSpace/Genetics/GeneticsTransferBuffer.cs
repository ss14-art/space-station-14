using System.Collections.Generic;

namespace Content.Shared.Genetics
{
    public sealed class GeneticsTransferBuffer
    {
        public string SubjectName = "Empty";
        public Dictionary<int, string>? UiDna;
        public Dictionary<int, string>? SeDna;

        public bool HasUi => UiDna != null && UiDna.Count > 0;
        public bool HasSe => SeDna != null && SeDna.Count > 0;
    }
}

