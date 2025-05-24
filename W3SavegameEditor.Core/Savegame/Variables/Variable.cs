using System.Linq;

namespace W3SavegameEditor.Core.Savegame.Variables
{
    public abstract class Variable
    {
        public string MagicNumber { get; set; }

        public string Name { get; set; }
        public ushort NameIndex { get; set; }
        public int Size { get; set; }
        public int TokenSize { get; set; }
        public int Position { get; set; }
        //TODO use this in WholeTokenSize and inner variables' WriteImpl
        public bool Removed { get; set; }

        public int WholeTokenSize
        {
            get
            {
                int result = TokenSize;
                if (this is VariableSet variableSet)
                    result += variableSet.Variables.Sum(v => v.WholeTokenSize);
                return result;
            }
        }

        public Variable()
        {
            Position = -1;
            Removed = false;
        }
    }
}
