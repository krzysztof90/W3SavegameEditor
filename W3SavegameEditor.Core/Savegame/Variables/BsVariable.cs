namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class BsVariable : VariableSet
    {
        public uint Unknown1 { get; set; }

        public override string ToString()
        {
            return $"BS {Name} {base.ToString()}";
        }
    }
}