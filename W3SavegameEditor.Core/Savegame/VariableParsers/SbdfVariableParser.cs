using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("SBDF")]
    public class SbdfVariableParser : VariableParserBase<SbdfVariable>
    {
        public SbdfVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override SbdfVariable ParseImpl(BinaryReader reader, List<string> names,ref int size)
        {
            //TODO how to read that?

            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            int binarySize = size;

            byte[] data = reader.ReadBytes(binarySize, ref size);

            return new SbdfVariable
            {
                NameIndex = nameIndex,
                Uknown = data
            };
        }

        public override void WriteImpl(BinaryWriter writer, SbdfVariable variable)
        {
            writer.Write(variable.NameIndex);
            writer.Write(variable.Uknown);
        }
    }
}
