using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("BLCK")]
    public class BlckVariableParser : VariableParserBase<BlckVariable>
    {
        public BlckVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override BlckVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            ushort blckSize = reader.ReadUInt16(ref size);
            ushort unknown3 = reader.ReadUInt16(ref size);

            // TODO: Only read blckSize
            List<Variable> variables = new List<Variable>();

            //TODO if this is inside another BlckVariable, then this outside loop will end after one run. Is it ok?
            while (size > 0)
            {
                Variable variable = _parser.Parse(reader, names, ref size);
                variables.Add(variable);
            }

            return new BlckVariable
            {
                Name = name,
                NameIndex = nameIndex,
                Variables = variables.ToArray(),
                BlckSize = blckSize,
                Unknown3 = unknown3,
            };
        }

        public override void WriteImpl(BinaryWriter writer, BlckVariable variable)
        {
            writer.Write(variable.NameIndex);
            writer.Write(variable.BlckSize);
            writer.Write(variable.Unknown3);

            foreach (Variable variable2 in variable.Variables)
                _parser.Write(writer, variable2);
        }
    }
}
