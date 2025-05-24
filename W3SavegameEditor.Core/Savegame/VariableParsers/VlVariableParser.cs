using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("VL")]
    public class VlVariableParser : VariableParserBase<VlVariable>
    {
        public VlVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override VlVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            ushort typeIndex = reader.ReadUInt16(ref size);
            string type = SavegameFile.GetVariableIndexName(typeIndex, names);

            VariableValue value = ReadValue(reader, type, names, ref size);

            return new VlVariable
            {
                Name = name,
                NameIndex = nameIndex,
                Type = type,
                TypeIndex = typeIndex,
                Value = value
            };
        }

        public override void WriteImpl(BinaryWriter writer, VlVariable variable)
        {
            writer.Write(variable.NameIndex);
            writer.Write(variable.TypeIndex);

            WriteValue(writer, variable.Type, variable.Value);
        }
    }
}
