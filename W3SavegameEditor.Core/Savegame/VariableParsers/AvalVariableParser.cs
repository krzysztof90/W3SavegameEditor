using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("AVAL")]
    public class AvalVariableParser : VariableParserBase<AvalVariable>
    {
        public AvalVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override AvalVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            ushort typeIndex = reader.ReadUInt16(ref size);
            string type = SavegameFile.GetVariableIndexName(typeIndex, names);

            int unknown = reader.ReadInt32(ref size);

            VariableValue value = ReadValue(reader, type, names, ref size);

            return new AvalVariable
            {
                Name = name,
                NameIndex = nameIndex,
                Type = type,
                TypeIndex = typeIndex,
                Value = value,
                Unknown = unknown,
            };
        }

        public override void WriteImpl(BinaryWriter writer, AvalVariable variable)
        {
            //TODO don't store NameIndex and TypeIndex, get it from SavegameFile.GetVariableNameIndex
            writer.Write(variable.NameIndex);
            writer.Write(variable.TypeIndex);
            writer.Write(variable.Unknown);

            WriteValue(writer, variable.Type, variable.Value);
        }
    }
}
