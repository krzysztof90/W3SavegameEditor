using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("SXAP")]
    public class SxapVariableParser : VariableParserBase<SxapVariable>
    {
        public SxapVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override SxapVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            int typeCode1 = reader.ReadInt32(ref size);
            int typeCode2 = reader.ReadInt32(ref size);
            int typeCode3 = reader.ReadInt32(ref size);

            return new SxapVariable
            {
                TypeCode1 = typeCode1,
                TypeCode2 = typeCode2,
                TypeCode3 = typeCode3
            };
        }

        public override void WriteImpl(BinaryWriter writer, SxapVariable variable)
        {
            writer.Write(variable.TypeCode1);
            writer.Write(variable.TypeCode2);
            writer.Write(variable.TypeCode3);
        }
    }
}
