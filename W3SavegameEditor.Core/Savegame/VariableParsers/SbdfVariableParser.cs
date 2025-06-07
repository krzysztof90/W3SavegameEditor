using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using W3SavegameEditor.Core.ChunkedLz4;
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

        public override SbdfVariable ParseImpl(BinaryReader reader, List<string> names, ref int size)
        {
            List<(byte[], byte, byte, byte, byte[], short, byte, string)> values = new List<(byte[], byte, byte, byte, byte[], short, byte, string)>();

            int entryCount = reader.ReadInt32(ref size);
            for (int i = 0; i < entryCount; i++)
            {
                //TODO what is in unknowns? How to create that based on 'text'

                byte stringSize = reader.ReadByte(ref size);

                byte[] unknown1 = null;
                byte unknown2 = 0;
                string text = null;

                if (stringSize < 128)
                {
                    //TODO this adds empty entry to 'values'. What is it?

                    if (stringSize < 64)
                    {
                        unknown1 = reader.ReadBytes(stringSize * 2, ref size);
                    }
                    //TODO to algorithm
                    else if (stringSize == 64)
                    {
                        unknown1 = reader.ReadBytes(128 + 1, ref size);
                    }
                    else if (stringSize == 71)
                    {
                        unknown1 = reader.ReadBytes(655, ref size);
                    }
                    else if (stringSize == 95)
                    {
                        unknown1 = reader.ReadBytes(319, ref size);
                    }
                    else if (stringSize == 111)
                    {
                        var e = reader.PeekByte();
                        if (e == 7)
                        {
                            unknown1 = reader.ReadBytes(991, ref size);
                        }
                        else if (e == 2)
                        {
                            unknown1 = reader.ReadBytes(351, ref size);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    unknown2 = reader.PeekByte();
                    if (unknown2 == 1)
                    {
                        reader.ReadByte(ref size);
                    }

                    text = reader.ReadString(stringSize - 128, ref size);
                }

                byte unknown3 = reader.ReadByte(ref size);
                byte unknown4 = reader.ReadByte(ref size);

                short headerSize = reader.ReadInt16(ref size);
                byte[] unknown5 = reader.ReadBytes(headerSize * 10, ref size);

                values.Add((unknown1, unknown2, unknown3, unknown4, unknown5, headerSize, stringSize, text));
            }

            string doneMagicNumber = reader.ReadString(4, ref size);
            if (doneMagicNumber != "EBDF")
                throw new InvalidOperationException();

            Debug.Assert(size == 0);

            return new SbdfVariable
            {
                Values = values
            };
        }

        public override void WriteImpl(BinaryWriter writer, SbdfVariable variable)
        {
            writer.Write(variable.Values.Count);
            foreach ((byte[] unknown1, byte unknown2, byte unknown3, byte unknown4, byte[] unknown5, short headerSize, byte stringSize, string text) value in variable.Values)
            {
                writer.Write(value.stringSize);
                if (value.stringSize < 128)
                    writer.Write(value.unknown1);
                else
                {
                    if (value.unknown2 == 1)
                        writer.Write(value.unknown2);

                    writer.Write(ChunkedLz4File.Encoding.GetBytes(value.text));
                }
                writer.Write(value.unknown3);
                writer.Write(value.unknown4);
                writer.Write(value.headerSize);
                writer.Write(value.unknown5);
            }
            writer.Write(ChunkedLz4File.Encoding.GetBytes("EBDF"));
        }
    }
}
