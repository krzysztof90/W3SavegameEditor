using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            ushort nameIndex = reader.ReadUInt16(ref size);
            string name = SavegameFile.GetVariableIndexName(nameIndex, names);

            int binarySize = size;

            List<(short, byte, byte[], byte[], byte, byte, byte, string, string)> values = new List<(short, byte, byte[], byte[], byte, byte, byte, string, string)>();
            while (size > 0)
            {
                //TODO what is in unknowns? How to create that based on 'text'

                short headerSize = reader.ReadInt16(ref size);

                byte[] unknown1 = reader.ReadBytes(headerSize * 10, ref size);

                string doneMagicNumber = reader.PeekString(4);
                if (doneMagicNumber == "EBDF")
                {
                    reader.ReadString(4, ref size);
                    Debug.Assert(size == 0);

                    values.Add((headerSize, 0, unknown1, null, 0, 0, 0, doneMagicNumber, null));

                    break;
                }

                byte stringSize = reader.ReadByte(ref size);

                byte[] unknown2 = null;
                byte unknown3 = 0;
                string text = null;

                if (stringSize < 128)
                {
                    //TODO is this part of next element? Now it doesn't add 'text' to 'values'

                    if (stringSize < 64)
                    {
                        unknown2 = reader.ReadBytes(stringSize * 2, ref size);
                    }
                    //TODO to algorithm
                    else if (stringSize == 64)
                    {
                        unknown2 = reader.ReadBytes(128 + 1, ref size);
                    }
                    else if (stringSize == 95)
                    {
                        unknown2 = reader.ReadBytes(319, ref size);
                    }
                    else if (stringSize == 111)
                    {
                        var e = reader.PeekByte();
                        if (e == 7)
                        {
                            unknown2 = reader.ReadBytes(991, ref size);
                        }
                        else if (e == 2)
                        {
                            unknown2 = reader.ReadBytes(351, ref size);
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
                    unknown3 = reader.PeekByte();
                    if (unknown3 == 1)
                    {
                        reader.ReadByte(ref size);
                    }

                    text = reader.ReadString(stringSize - 128, ref size);
                }

                byte unknown4 = reader.ReadByte(ref size);
                byte unknown5 = reader.ReadByte(ref size);

                if (unknown4 != 0 || unknown5 != 0)
                {
                }

                values.Add((headerSize, stringSize, unknown1, unknown2, unknown3, unknown4, unknown5, doneMagicNumber, text));
            }

            Debug.Assert(size == 0);

            return new SbdfVariable
            {
                NameIndex = nameIndex,
                Values = values
            };
        }

        public override void WriteImpl(BinaryWriter writer, SbdfVariable variable)
        {
            writer.Write(variable.NameIndex);
            foreach ((short headerSize, byte stringSize, byte[] unknown1, byte[] unknown2, byte unknown3, byte unknown4, byte unknown5, string doneMagicNumber, string text) value in variable.Values)
            {
                writer.Write(value.headerSize);
                writer.Write(value.unknown1);
                if (value.doneMagicNumber == "EBDF")
                {
                    writer.Write(Encoding.ASCII.GetBytes(value.doneMagicNumber));
                    break;
                }
                writer.Write(value.stringSize);
                if (value.stringSize < 128)
                    writer.Write(value.unknown2);
                else
                {
                    if (value.unknown3 == 1)
                        writer.Write(value.unknown3);

                    writer.Write(Encoding.ASCII.GetBytes(value.text));
                }
                writer.Write(value.unknown4);
                writer.Write(value.unknown5);
            }
        }
    }
}
