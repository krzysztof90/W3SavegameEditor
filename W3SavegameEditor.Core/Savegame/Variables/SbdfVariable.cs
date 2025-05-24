namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class SbdfVariable : Variable
    {
        public byte[] Uknown { get; set; }

        public override string ToString()
        {
            return "SBDF " + base.ToString();
        }
    }
}
