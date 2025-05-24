namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class BlckVariable : VariableSet
    {
        public ushort BlckSize { get; set; }
        public ushort Unknown3 { get; set; }

        public override string ToString()
        {
            return "BLCK " + base.ToString();
        }
    }
}
