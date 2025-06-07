using System;
using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.ChunkedLz4;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    [VariableParser("MANU")]
    public class ManuVariableParser : VariableParserBase<ManuVariable>
    {
        public ManuVariableParser(VariableParser parser) : base(parser)
        {
        }

        public override ManuVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            int stringCount = reader.ReadInt32(ref size);
            int unknown1 = reader.ReadInt32(ref size);

            List<string> strings = new List<string>();
            for (int i = 0; i < stringCount; i++)
            {
                byte stringSize = reader.ReadByte(ref size);
                string text = reader.ReadString(stringSize, ref size);
                strings.Add(text);
            }

            int unknown2 = reader.ReadInt32(ref size);
            string doneMagicNumber = reader.ReadString(4, ref size);
            if (doneMagicNumber != "ENOD")
                throw new InvalidOperationException();

            return new ManuVariable
            {
                Strings = strings,
                Unknown1 = unknown1,
                Unknown2 = unknown2,
            };
        }

        public override void WriteImpl(BinaryWriter writer, ManuVariable manuVariable)
        {
            int stringCount = manuVariable.Strings.Count;

            writer.Write(stringCount);
            writer.Write(manuVariable.Unknown1);

            for (int i = 0; i < stringCount; i++)
            {
                byte stringSize = (byte)ChunkedLz4File.Encoding.GetByteCount(manuVariable.Strings[i]);

                writer.Write(stringSize);
                writer.Write(ChunkedLz4File.Encoding.GetBytes(manuVariable.Strings[i]));
            }

            writer.Write(manuVariable.Unknown2);
            writer.Write(ChunkedLz4File.Encoding.GetBytes("ENOD"));
        }
    }
}
