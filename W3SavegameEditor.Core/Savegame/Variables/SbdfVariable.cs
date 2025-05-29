using System.Collections.Generic;

namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class SbdfVariable : Variable
    {
        public List<(short, byte, byte[], byte[], byte, byte, byte, string, string)> Values { get; set; }

        public override string ToString()
        {
            return "SBDF " + base.ToString();
        }
    }
}
