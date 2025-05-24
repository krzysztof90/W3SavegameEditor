namespace W3SavegameEditor.Core.Savegame.Variables
{
    public class UnknownVariable : Variable
    {
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return string.Format("Unknown[{0}] {1}", (Data == null ? "null" : Data.Length.ToString()), base.ToString());
        }
    }
}