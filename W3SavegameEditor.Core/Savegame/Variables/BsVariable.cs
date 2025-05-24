namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class BsVariable : VariableSet
    {
        public override string ToString()
        {
            return $"BS {Name} {base.ToString()}";
        }
    }
}