namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class SsVariable : VariableSet
    {
        public int SizeInner { get; set; }

        public override string ToString()
        {
            return "SS " + base.ToString();
        }
    }
}