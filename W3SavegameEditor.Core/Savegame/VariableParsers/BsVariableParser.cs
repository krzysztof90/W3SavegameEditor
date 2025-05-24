using System.Collections.Generic;
using System.IO;
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
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            return new BsVariable
            {
                Name = name,
                NameIndex = nameIndex,
                Variables = new Variable[0]
            };
        }

        public override void WriteImpl(BinaryWriter writer, BsVariable variable)
        {
            writer.Write(variable.NameIndex);
        }
    }
}
