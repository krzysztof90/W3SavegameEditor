using System.Collections.Generic;
using System.IO;
using System.Linq;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    /// <summary>
    /// A set of variables
    /// </summary>
    [VariableParser("BS")]
    public class BsVariableParser : VariableParserBase<BsVariable>
    {
        public BsVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override BsVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = null;
            uint unknown2 = 0;

            if (nameIndex >= names.Count)
            {
                //TODO why is that + why additional bytes are needed
                unknown2 = reader.ReadUInt32(ref size);
            }
            else
                name = SavegameFile.GetVariableIndexName(nameIndex, names);

            return new BsVariable
            {
                Name = name,
                Unknown1 = nameIndex,
                Unknown2 = unknown2,
                Variables = new Variable[0]
            };
        }

        public override void WriteImpl(BinaryWriter writer, BsVariable variable, List<string> names)
        {
            if (variable.Name == null)
            {
                writer.Write(variable.Unknown1);
                writer.Write(variable.Unknown2);
            }
            else
                writer.Write(SavegameFile.GetVariableNameIndex(variable.Name, names));
        }
    }
}
