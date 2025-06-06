using System.Collections.Generic;

namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class SbdfVariable : Variable
    {
        public List<(byte[] unknown1, byte unknown2, byte unknown3, byte unknown4, byte[] unknown5, short headerSize, byte stringSize, string text)> Values { get; set; }

        public override string ToString()
        {
            return "SBDF " + base.ToString();
        }
    }
}
