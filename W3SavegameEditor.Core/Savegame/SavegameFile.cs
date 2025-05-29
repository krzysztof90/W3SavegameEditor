using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W3SavegameEditor.Core.ChunkedLz4;
using W3SavegameEditor.Core.Common;
using W3SavegameEditor.Core.Savegame.Values;
using W3SavegameEditor.Core.Savegame.VariableParsers;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame
{
    public class SavegameFile
    {
        private class RbEntry
        {
            public short Size { get; set; }
            public int Offset { get; set; }
        }

        public int TypeCode1 { get; set; }
        public int TypeCode2 { get; set; }
        public int TypeCode3 { get; set; }
        public byte[] Unknown1 { get; set; }

        private const int AllBytesLength = 1;
        public List<(byte[], int)> AllBytes { get; set; }

        public byte[] PreHeader { get; set; }
        public long HeaderStartOffset { get; set; }
        public int VariableTableOffset { get; set; }
        public long StringTableFooterOffset { get; set; }
        public long StringTableOffset { get; set; }
        public int RbSectionOffset { get; set; }
        private RbEntry[] RbEntries { get; set; }
        public int NmSectionOffset { get; set; }

        public VariableTableEntry[] Entries { get; set; }
        public VariableTableEntry[] VariableTableEntries { get; set; }
        public ManuVariable ManuVariable { get; set; }
        public VariableTableEntry ManuEntry { get; set; }
        public Variable[] OrigVariables { get; set; }
        public Variable[] Variables { get; set; }

        public SavegameRoot Root { get; set; }

        public static Task<SavegameFile> ReadAsync(
            string path,
            IReadSavegameProgress progress = null)
        {
            return Task.Run(() => Read(path, progress));
        }

        public static SavegameFile Read(
            string path,
            IReadSavegameProgress progress = null)
        {
            if (progress != null) progress.Report(true, true, 0, 0);
            using (FileStream compressedInputStream = File.OpenRead(path))
            using (Stream inputStream = ChunkedLz4File.Decompress(compressedInputStream))
            using (BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true))
            {
                reader.BaseStream.Position = 0;

                SavegameFile savegameFile = new SavegameFile();
                savegameFile.ReadPreHeader(reader);
                savegameFile.ReadHeader(reader);
                savegameFile.ReadFooter(reader);
                savegameFile.ReadStringTable(reader);
                savegameFile.ReadVariableTable(reader);
                if (progress != null) progress.Report(true, false, 0, savegameFile.VariableTableEntries.Length);
                savegameFile.ReadVariables(reader, progress);

                savegameFile.ReferenceVariable();

                //savegameFile.ReadAll(reader);

                if (progress != null) progress.Report(false, false, 0, 0);

                return savegameFile;
            }
        }

        public static void Write(SavegameFile savegameFile, string path)
        {
            MemoryStream inputStream = new MemoryStream();
            //inputStream.Position = ChunkedLz4File.HeaderSize;
            using (BinaryWriter writer = new BinaryWriter(inputStream, Encoding.ASCII, true))
            {
                savegameFile.WritePreHeader(writer);
                savegameFile.WriteHeader(writer);
                savegameFile.WriteVariables(writer);
                savegameFile.WriteRbSection(writer);
                savegameFile.WriteNmSection(writer);
                savegameFile.WriteVariableNameSection(writer);
                savegameFile.WriteStringTable(writer);
                savegameFile.WriteVariableTable(writer);
                savegameFile.WriteFooter(writer);

                //savegameFile.WriteAll(writer);
            }

            inputStream.Position = ChunkedLz4File.HeaderSize;

            using (Stream outputStream = ChunkedLz4File.Compress4(inputStream))
            {
                //validate read after write
                //using (Stream inputStream2 = ChunkedLz4File.Decompress(outputStream))
                //{
                //    using (BinaryReader reader = new BinaryReader(inputStream2, Encoding.ASCII, true))
                //    {
                //        var savegameFile2 = new SavegameFile();
                //        savegameFile2.ReadHeader(reader);
                //        savegameFile2.ReadFooter(reader);
                //        savegameFile2.ReadStringTable(reader);
                //        savegameFile2.ReadVariableTable(reader);
                //        savegameFile2.ReadVariables(reader);
                //        savegameFile2.ReferenceVariable();
                //    }
                //}

                using (var fileStream = File.Create(path))
                {
                    Stream s = outputStream;

                    s.Seek(0, SeekOrigin.Begin);
                    s.CopyTo(fileStream);
                }
            }
        }

        private void ReadAll(BinaryReader reader)
        {
            AllBytes = new List<(byte[], int)>();

            reader.BaseStream.Position = 0;

            while (true)
            {
                byte[] buffer = new byte[AllBytesLength];
                int current = reader.Read(buffer, 0, buffer.Length);
                if (current == 0)
                    break;

                AllBytes.Add((buffer, current));
            }
        }

        private void ReadPreHeader(BinaryReader reader)
        {
            PreHeader = reader.ReadBytes(ChunkedLz4File.HeaderSize);
        }

        private void ReadHeader(BinaryReader reader)
        {
            HeaderStartOffset = reader.BaseStream.Position;
            string magicNumber = reader.ReadString(4);
            if (magicNumber != "SAV3")
                throw new InvalidOperationException();

            TypeCode1 = reader.ReadInt32();
            TypeCode2 = reader.ReadInt32();
            TypeCode3 = reader.ReadInt32();
        }

        private void ReadFooter(BinaryReader reader)
        {
            reader.BaseStream.Seek(-6, SeekOrigin.End);
            VariableTableOffset = reader.ReadInt32();
            StringTableFooterOffset = VariableTableOffset - 10;
            string magicNumber = reader.ReadString(2);
            if (magicNumber != "SE")
                throw new InvalidOperationException();
        }

        private void ReadStringTable(BinaryReader reader)
        {
            reader.BaseStream.Position = StringTableFooterOffset;
            NmSectionOffset = reader.ReadInt32();
            RbSectionOffset = reader.ReadInt32();
            Unknown1 = reader.ReadBytes(2);
            ReadNmSection(reader);
            ReadRbSection(reader);
            ReadVariableNameSection(reader);
        }

        private void ReadNmSection(BinaryReader reader)
        {
            reader.BaseStream.Position = NmSectionOffset;
            string magicNumber = reader.ReadString(2);
            if (magicNumber != "NM")
                throw new InvalidOperationException();
            StringTableOffset = reader.BaseStream.Position;
        }

        private void ReadRbSection(BinaryReader reader)
        {
            reader.BaseStream.Position = RbSectionOffset;
            string magicNumber = reader.ReadString(2);
            if (magicNumber != "RB")
                throw new InvalidOperationException();
            int count = reader.ReadInt32();
            RbEntries = new RbEntry[count];
            for (int i = 0; i < count; i++)
            {
                RbEntries[i] = new RbEntry
                {
                    Size = reader.ReadInt16(),
                    Offset = reader.ReadInt32()
                };
            }
        }

        private void ReadVariableNameSection(BinaryReader reader)
        {
            reader.BaseStream.Position = StringTableOffset;
            var manuVariableParser = new ManuVariableParser(null);
            int manuVariableSize = (int)(StringTableFooterOffset - StringTableOffset);
            ManuVariable manuVariable = (ManuVariable)manuVariableParser.Parse(reader, null, ref manuVariableSize);
            manuVariable.MagicNumber = manuVariableParser.MagicNumber;
            ManuVariable = manuVariable;
        }

        private void ReadVariableTable(BinaryReader reader)
        {
            reader.BaseStream.Position = VariableTableOffset;
            int entryCount = reader.ReadInt32();
            Entries = new VariableTableEntry[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                Entries[i] = new VariableTableEntry
                {
                    Offset = reader.ReadInt32(),
                    Size = reader.ReadInt32()
                };
            }

            // Order all variables by their offset to calculate their actual size.
            VariableTableEntries = Entries.OrderBy(e => e.Offset).ToArray();
        }

        private void ReadVariables(BinaryReader reader, IReadSavegameProgress progress)
        {
            var parser = new VariableParser();

            Variable[] variables = new Variable[VariableTableEntries.Length];
            for (int i = 0; i < VariableTableEntries.Length; i++)
            {
                // Calculate the size of the next token
                var size = VariableTableEntries[i].Size;
                int tokenSize;

                // There is a hidden variable before the last one
                if (i < VariableTableEntries.Length - 2)
                {
                    tokenSize = VariableTableEntries[i + 1].Offset - VariableTableEntries[i].Offset;
                }
                else
                {
                    //TODO wrong because Size means size with all children which are read separately. If it was right then if statement would be unnecesary
                    tokenSize = VariableTableEntries[i].Size;
                }

                //TODO why UnknownVariable has size < tokenSize?

                reader.BaseStream.Position = VariableTableEntries[i].Offset;
                // Tokenizing
                var readTokenSize = tokenSize;

                var variable = parser.Parse(reader, ManuVariable.Strings, ref readTokenSize);

                Debug.Assert(readTokenSize == 0);

                if (VariableTableEntries[i].Offset == StringTableOffset)
                {
                    variable = ManuVariable;
                    ManuEntry = VariableTableEntries[i];
                }

                variable.Size = size;
                variable.TokenSize = tokenSize;
                variables[i] = variable;

                if (i % 250 == 0 && progress != null) progress.Report(true, false, i, VariableTableEntries.Length);
            }

            // Parsing
            var valueParser = new VariableValueParser();
            var stack = new Stack<Variable>(variables.Reverse());
            Root = valueParser.Parse<SavegameRoot>(stack);

            OrigVariables = variables;
        }

        private void ReferenceVariable()
        {
            for (int i = 0; i < OrigVariables.Length; i++)
                OrigVariables[i].Position = i;

            List<Variable> referencedVariables = new List<Variable>();
            for (int i = 0; i < OrigVariables.Length; i++)
            {
                Variable currentVariable = SetVariables(ref i);

                referencedVariables.Add(currentVariable);
            }

            Variables = referencedVariables.ToArray();
        }

        private Variable SetVariables(ref int i)
        {
            Variable currentVariable = OrigVariables[i];

            VariableSet currentVariableSet = currentVariable as VariableSet;
            if (currentVariableSet != null && currentVariableSet.Size > currentVariableSet.TokenSize)
            {
                int size = currentVariableSet.Size - currentVariableSet.TokenSize;
                List<Variable> childrenVariables = new List<Variable>();
                while (size > 0)
                {
                    i++;
                    Variable nextVariable = SetVariables(ref i);

                    childrenVariables.Add(nextVariable);
                    size -= nextVariable.WholeTokenSize;
                }

                Debug.Assert(size == 0);

                currentVariableSet.Variables = childrenVariables.ToArray();
            }
            else
            {
                //TODO what does currentVariable.Size > currentVariable.TokenSize of VL mean? Do the same as for VariableSet above? Is VL VariableSet? After change of properties set its Size in SetVariableProperties
            }

            return currentVariable;
        }

        /// <summary>
        /// used for validate if all writen bytes are like read ones
        /// </summary>
        /// <param name="writer"></param>
        private void WriteAll(BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;

            foreach (var buffer in AllBytes)
            {
                long l = writer.BaseStream.Position;
                byte[] buffer2 = new byte[AllBytesLength];
                writer.BaseStream.Read(buffer2, 0, buffer2.Length);
                writer.BaseStream.Position = l;

                Debug.Assert(buffer.Item1.SequenceEqual(buffer2));

                writer.Write(buffer.Item1, 0, buffer.Item2);
            }
        }

        private void WritePreHeader(BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;

            writer.Write(PreHeader);
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(Encoding.ASCII.GetBytes("SAV3"));
            //TODO what are these values and other like Unknown1? What to change them for?
            writer.Write(TypeCode1);
            writer.Write(TypeCode2);
            writer.Write(TypeCode3);
        }

        private void WriteFooter(BinaryWriter writer)
        {
            writer.Write(VariableTableOffset);
            writer.Write(Encoding.ASCII.GetBytes("SE"));
        }

        private void WriteStringTable(BinaryWriter writer)
        {
            StringTableFooterOffset = writer.BaseStream.Position;

            writer.Write(NmSectionOffset);
            writer.Write(RbSectionOffset);
            writer.Write(Unknown1);
        }

        private void WriteNmSection(BinaryWriter writer)
        {
            NmSectionOffset = (int)writer.BaseStream.Position;

            writer.Write(Encoding.ASCII.GetBytes("NM"));
        }

        private void WriteRbSection(BinaryWriter writer)
        {
            RbSectionOffset = (int)writer.BaseStream.Position;

            writer.Write(Encoding.ASCII.GetBytes("RB"));
            int count = RbEntries.Length;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writer.Write(RbEntries[i].Size);
                writer.Write(RbEntries[i].Offset);
            }
        }

        private void WriteVariableNameSection(BinaryWriter writer)
        {
            StringTableOffset = writer.BaseStream.Position;
            ManuEntry.Offset = (int)writer.BaseStream.Position;

            var manuVariableParser = new ManuVariableParser(null);
            manuVariableParser.Write(writer, ManuVariable);
        }

        private void WriteVariableTable(BinaryWriter writer)
        {
            VariableTableOffset = (int)writer.BaseStream.Position;

            int entryCount = Entries.Length - OrigVariables.Count(v => v.Removed);
            writer.Write(entryCount);
            for (int i = 0; i < Entries.Length; i++)
            {
                if (!OrigVariables[i].Removed)
                {
                    //with VariableTableEntries order will not be original (Entries has original order but little random)
                    //writer.Write(VariableTableEntries[i].Offset);
                    //writer.Write(VariableTableEntries[i].Size);
                    writer.Write(Entries[i].Offset);
                    writer.Write(Entries[i].Size);
                }
            }
        }

        private void WriteVariables(BinaryWriter writer)
        {
            //List<string> strings = new List<string>();
            List<string> strings = ManuVariable.Strings;
            foreach (Variable variable in OrigVariables)
            {
                SetVariableProperties(variable, strings);
            }
            //TODO must also contain names used in VariableParserBase.ReadValue
            ManuVariable.Strings = strings;

            VariableParser parser = new VariableParser();

            //TODO does OrigVariables order matter?
            //TODO The change of VariableTableEntries must imply change in Entries and ManuEntry
            for (int i = 0; i < VariableTableEntries.Length; i++)
            {
                Variable variable = OrigVariables[i];

                if (variable != ManuVariable)
                {
                    if (!variable.Removed)
                    {
                        VariableTableEntries[i].Offset = (int)writer.BaseStream.Position;

                        parser.Write(writer, variable);
                    }
                }
                else
                    ManuEntry = VariableTableEntries[i];
            }
        }

        private void SetVariableProperties(Variable variable, List<string> strings)
        {
            if (variable.Name != null)
            {
                variable.NameIndex = GetVariableNameIndex(variable.Name, strings);

                if (variable is VariableTyped variableTyped)
                    variableTyped.TypeIndex = GetVariableNameIndex(variableTyped.Type, strings);
            }

            //TODO what for UnknownVariable and VL?
            if (variable is VariableSet variableSet)
            {
                foreach (Variable variable2 in variableSet.Variables)
                    SetVariableProperties(variable2, strings);

                int i = variable.Position;
                if (i != -1) //can be from SsVariable.Variables
                {
                    VariableTableEntries[i].Size = variableSet.WholeTokenSize;
                }
            }
        }

        public static ushort GetVariableNameIndex(string name, List<string> strings)
        {
            if (!strings.Contains(name))
                strings.Add(name);
            return (ushort)(strings.IndexOf(name) + 1);
        }

        public static string GetVariableIndexName(ushort index, List<string> strings)
        {
            return strings[index - 1];
        }
    }
}
