using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("PORP")]
    public class PorpVariableParser : VariableParserBase<PorpVariable>
    {
        public PorpVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override PorpVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            ushort typeIndex = reader.ReadUInt16(ref size);
            string type = SavegameFile.GetVariableIndexName(typeIndex, names);

            int valueSize = reader.ReadInt32(ref size);

            int readValueSize = valueSize;
            VariableValue value = ReadValue(reader, type, names, ref readValueSize);
            size -= valueSize;

            Debug.Assert(readValueSize == 0);

            return new PorpVariable
            {
                Name = name,
                NameIndex = nameIndex,
                Type = type,
                TypeIndex = typeIndex,
                Value = value,
                ValueSize = valueSize
            };
        }

        public override void WriteImpl(BinaryWriter writer, PorpVariable variable)
        {
            writer.Write(variable.NameIndex);
            writer.Write(variable.TypeIndex);
            writer.Write(variable.ValueSize);

            WriteValue(writer, variable.Type, variable.Value);
        }
    }
}
