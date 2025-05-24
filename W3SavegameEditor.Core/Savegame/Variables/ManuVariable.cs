using System.Collections.Generic;

namespace W3SavegameEditor.Core.Savegame.Variables
{
    /// <summary>
    /// A set of strings.
    /// </summary>
    public class ManuVariable : Variable
    {
        public List<string> Strings { get; set; }
        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        public override string ToString()
        {
            return string.Format("MANU[{0}] {1}", (Strings == null ? "null" : Strings.Count.ToString()), base.ToString());
        }
    }
}