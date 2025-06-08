namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class BsVariable : VariableSet
    {
        public ushort Unknown1 { get; set; }
        public uint Unknown2 { get; set; }

        public override string ToString()
        {
            return $"BS {Name} {base.ToString()}";
        }
    }
}