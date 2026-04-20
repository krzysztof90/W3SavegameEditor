using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("SS")]
    public class SsVariableParser : VariableParserBase<SsVariable>
    {
        public SsVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override SsVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            int sizeInner = reader.ReadInt32(ref size);

            Debug.Assert(sizeInner == size);

            List<Variable> variables = new List<Variable>();
            while (size > 0)
            {
                Variable variable = _parser.Parse(reader, names, ref size);
                variables.Add(variable);
            }

            return new SsVariable
            {
                Variables = variables.ToArray(),
                SizeInner = sizeInner
            };
        }

        public override void WriteImpl(BinaryWriter writer, SsVariable variable, List<string> names)
        {
            writer.Write(variable.SizeInner);
            foreach (Variable variable2 in variable.Variables)
                _parser.Write(writer, variable2, names);
        }
    }
}
