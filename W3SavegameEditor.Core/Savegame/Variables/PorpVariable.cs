namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class PorpVariable : VariableTyped
    {
        public int ValueSize { get; set; }

        public override string ToString()
        {
            return "PORP " + base.ToString();
        }
    }
}
