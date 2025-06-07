using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using W3SavegameEditor.Core.ChunkedLz4;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.Values;
using W3SavegameEditor.Core.Savegame.Values.Engine;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame.VariableParsers
{
    public abstract class VariableParserBase
    {
        public string MagicNumber => AttributeOperations.GetClassAttribute<VariableParserAttribute, string>(GetType(), (VariableParserAttribute attribute) => attribute.MagicNumber);

        public abstract Type SupportedType { get; }

        public abstract Variable Parse(BinaryReader reader, List<string> names, ref int size);
        public abstract void Write(BinaryWriter writer, Variable variable);

        public abstract void Verify(BinaryReader reader, ref int size);
    }

    public abstract class VariableParserBase<T> : VariableParserBase where T : Variable
    {
        protected readonly VariableParser _parser;

        public VariableParserBase(VariableParser parser)
        {
            _parser = parser;
        }

        public override Type SupportedType
        {
            get { return typeof(T); }
        }

        public override Variable Parse(BinaryReader reader, List<string> names, ref int size)
        {
            Verify(reader, ref size);
            return ParseImpl(reader, names, ref size);
        }
        public abstract T ParseImpl(BinaryReader reader, List<string> names, ref int size);

        public override void Write(BinaryWriter writer, Variable variable)
        {
            writer.Write(ChunkedLz4File.Encoding.GetBytes(variable.MagicNumber));
            WriteImpl(writer, (T)variable);
        }
        public abstract void WriteImpl(BinaryWriter writer, T variable);

        public override void Verify(BinaryReader reader, ref int size)
        {
            var bytesToRead = MagicNumber.Length;
            var readMagicNumber = reader.ReadString(bytesToRead, ref size);
            if (readMagicNumber != MagicNumber)
            {
                throw new InvalidOperationException(
                    string.Format(
                    "Expeced {0} but read {1} at {2}",
                    MagicNumber,
                    readMagicNumber,
                    reader.BaseStream.Position - bytesToRead));
            }
        }

        public static List<ushort> GetUInt16FromBytes(byte[] bytes)
        {
            List<ushort> ushorts = bytes.Select((x, b) => b == bytes.Length - 1 ? (0, 0) : (x, bytes[b + 1])).Select(b => BitConverter.ToUInt16(new byte[] { (byte)b.Item1, (byte)b.Item2 }, 0)).ToList();
            return ushorts;
        }

        public static List<string> GetPotentialNames(byte[] bytes, List<string> names)
        {
            List<ushort> ushorts = GetUInt16FromBytes(bytes);
            List<string> potentialNames = ushorts.Select(b => (b == 0 || b >= names.Count) ? "" : SavegameFile.GetVariableIndexName(b, names)).ToList();
            return potentialNames;
        }

        protected VariableValue ReadValue(BinaryReader reader, string type, List<string> names, ref int size)
        {
            //TODO Analyze how unknown bytes can be read. It is important to know how to write appropriate values
            //TODO all as separate classes

            //How to check if there are some repeatable names inside unknown bytes:
            //byte[] bytes = reader.PeekBytes(size);
            //var chars = bytes.Select(b => (char)b).ToList();
            //var ushorts = VariableParserBase<Variable>.GetUInt16FromBytes(bytes);
            //List<string> potentialNames = GetPotentialNames(bytes, names);
            //of course it needs to repeat within different files
            //Some places it can be different but similar - we know it needs to be read as name

            switch (type)
            {
                case "String":
                    {
                        byte headerByte = reader.ReadByte(ref size);

                        int moreSize = 0;

                        byte singleByte = reader.PeekByte();
                        //TODO the lowest found char is 'A' 65, the highest singleByte is 2. Where is the border?
                        if (singleByte < 32)
                        {
                            reader.ReadByte(ref size);

                            for (int i = 2; i <= singleByte; i++)
                                moreSize += 64;
                        }

                        int stringLength = headerByte - 128 + moreSize;

                        string value = reader.ReadString(stringLength, ref size);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        return variableValue;
                    }
                case "StringAnsi":
                    {
                        byte length = reader.ReadByte(ref size);
                        byte[] data = reader.ReadBytes(length, ref size);

                        string value = ChunkedLz4File.Encoding.GetString(data).TrimEnd(char.MinValue);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (length, data);
                        return variableValue;
                    }
                case "CName":
                    {
                        ushort cnameIndex = reader.ReadUInt16(ref size);

                        //TODO nameIndex != 0 inside this method
                        string value = cnameIndex != 0 ? SavegameFile.GetVariableIndexName(cnameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = cnameIndex;
                        return variableValue;
                    }
                case "CGUID":
                    {
                        int length = 16;

                        byte[] guidData = reader.ReadBytes(length, ref size);

                        var value = new Guid(guidData);

                        VariableValue<Guid> variableValue = VariableValue<Guid>.Create(value);
                        variableValue.AdditionalObject = guidData;
                        return variableValue;
                    }
                case "Bool":
                    {
                        bool value = reader.ReadBoolean(ref size);
                        return VariableValue<bool>.Create(value);
                    }
                case "Uint8":
                    {
                        byte value = reader.ReadByte(ref size);
                        return VariableValue<byte>.Create(value);
                    }
                case "Uint16":
                    {
                        ushort value = reader.ReadUInt16(ref size);
                        return VariableValue<ushort>.Create(value);
                    }
                case "Uint32":
                    {
                        uint value = reader.ReadUInt32(ref size);
                        return VariableValue<uint>.Create(value);
                    }
                case "Uint64":
                    {
                        ulong value = reader.ReadUInt64(ref size);
                        return VariableValue<ulong>.Create(value);
                    }
                case "Int8":
                    {
                        sbyte value = reader.ReadSByte(ref size);
                        return VariableValue<sbyte>.Create(value);
                    }
                case "Int16":
                    {
                        short value = reader.ReadInt16(ref size);
                        return VariableValue<short>.Create(value);
                    }
                case "Int32":
                    {
                        int value = reader.ReadInt32(ref size);
                        return VariableValue<int>.Create(value);
                    }
                case "Int64":
                    {
                        long value = reader.ReadInt64(ref size);
                        return VariableValue<long>.Create(value);
                    }
                case "Double":
                    {
                        double value = reader.ReadDouble(ref size);
                        return VariableValue<double>.Create(value);
                    }
                case "Float":
                    {
                        float value = reader.ReadSingle(ref size);
                        return VariableValue<float>.Create(value);
                    }
                case "Vector":
                    {
                        byte unknown1 = reader.ReadByte(ref size);

                        ushort nameIndex = reader.ReadUInt16(ref size);
                        string name = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue<(string, string)> variableValue = VariableValue<(string, string)>.Create((name, handleType));
                        variableValue.AdditionalObject = (unknown1, nameIndex, typeIndex);
                        return variableValue;
                    }
                case "Vector2":
                    {
                        byte unknown1 = reader.ReadByte(ref size);

                        ushort nameIndex = reader.ReadUInt16(ref size);
                        string name = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue<(string, string)> variableValue = VariableValue<(string, string)>.Create((name, handleType));
                        variableValue.AdditionalObject = (unknown1, nameIndex, typeIndex);
                        return variableValue;
                    }
                case "EulerAngles":
                    {
                        byte unknown1 = reader.ReadByte(ref size);

                        ushort nameIndex = reader.ReadUInt16(ref size);
                        string name = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue<(string, string)> variableValue = VariableValue<(string, string)>.Create((name, handleType));
                        variableValue.AdditionalObject = (unknown1, nameIndex, typeIndex);
                        return variableValue;
                    }
                case "EngineTime":
                    {
                    }
                    return ReadUnknownBytes(reader, 3, ref size);
                case "EngineTransform":
                    {
                    }
                    return ReadUnknownBytes(reader, 13, ref size);
                case "GameTime":
                    {
                        //size 5
                        //1-2 "m_seconds"
                        //3-4 "Int32"

                        //TODO or size 3 and every byte empty
                    }
                    //return ReadUnknownBytes(reader, 5, ref size);
                    //return ReadUnknownBytes(reader, 3, ref size);
                    return ReadUnknownBytes(reader, size, ref size);
                case "EntityHandle":
                    {
                        byte unknown1 = reader.ReadByte(ref size);
                        byte unknown2 = 0x00;
                        byte[] unknown3 = null;
                        if (unknown1 > 0)
                        {
                            unknown2 = reader.ReadByte(ref size);
                            unknown3 = reader.ReadBytes(16, ref size);
                        }

                        var value = new EntityHandle
                        {
                            Unknown1 = unknown1,
                            Unknown2 = unknown2,
                            Unknown3 = unknown3,
                        };

                        return VariableValue<EntityHandle>.Create(value);
                    }
                case "IdTag":
                    {
                    }
                    return ReadUnknownBytes(reader, 17, ref size);
                case "TagList":
                    {
                        byte tagListHeader = reader.ReadByte(ref size);
                        bool tagListFlag = (tagListHeader & 128) > 0;
                        byte tagListCount = (byte)(tagListHeader & 127);
                        short[] tagListEntries = new short[tagListCount];
                        for (int i = 0; i < tagListCount; i++)
                        {
                            tagListEntries[i] = reader.ReadInt16(ref size); // NameIndex?
                        }

                        var value = new TagList
                        {
                            Flag = tagListFlag,
                            Entities = tagListEntries
                        };

                        VariableValue<TagList> variableValue = VariableValue<TagList>.Create(value);
                        variableValue.AdditionalObject = tagListHeader;
                        return variableValue;
                    }
                case "eGwintFaction":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "EJournalStatus":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "EZoneName":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "EDifficultyMode":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "W3EnvironmentManager":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort nameIndex3 = reader.ReadUInt16(ref size);
                        string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                        VariableValue<(string, string, string)> variableValue = VariableValue<(string, string, string)>.Create((value1, value2, value3));
                        variableValue.AdditionalObject = (nameIndex1, nameIndex2, nameIndex3, unknown1, unknown2);
                        return variableValue;

                        //6-7 value1 "W3EnvironmentManager"
                        //9-10 value2 "m_envId"
                        //11-12 value3 "Int32"
                    }
                case "SQuestThreadSuspensionData":
                    {
                        //1-2 "scopeBlockGUID"
                        //3-4 "CGUID"
                        //sometimes size = 0
                    }
                    //return ReadUnknownBytes(reader, 5, ref size);
                    //return ReadUnknownBytes(reader, 2, ref size);
                    return ReadUnknownBytes(reader, size, ref size);
                case "SActionPointId":
                    {
                        byte unknown1 = reader.ReadByte(ref size);
                        short unknown2 = reader.ReadInt16(ref size);

                        byte[] unknown3 = new byte[0];
                        if (unknown2 > 0)
                        {
                            int length = 2;

                            unknown3 = reader.ReadBytes(length, ref size);
                        }

                        VariableValue<byte[]> variableValue = VariableValue<byte[]>.Create(unknown3);
                        variableValue.AdditionalObject = (unknown1, unknown2, unknown3);
                        return variableValue;
                    }
                case "EAIAttitude":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "EFocusModeVisibility":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "EEquipmentSlots":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "W3TutorialManagerUIHandler":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort nameIndex3 = reader.ReadUInt16(ref size);
                        string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                        VariableValue<(string, string, string)> variableValue = VariableValue<(string, string, string)>.Create((value1, value2, value3));
                        variableValue.AdditionalObject = (nameIndex1, nameIndex2, nameIndex3, unknown1, unknown2);
                        return variableValue;

                        //6-7 "W3TutorialManagerUIHandler"
                        //9-10 "listeners"
                        //11-12 "array:2,0,SUITutorial"
                    }
                case "ESignType":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "W3Reputation":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue value = ReadValue(reader, handleType, names, ref size);

                        VariableValue<(string, string, VariableValue)> variableValue = VariableValue<(string, string, VariableValue)>.Create((value1, value2, value));
                        variableValue.AdditionalObject = (nameIndex1, nameIndex2, typeIndex, handleType, unknown1, unknown2);
                        return variableValue;

                        //6-7 "W3Reputation"
                        //9-10 "factionReputations"
                        //11-12 "array:2,0,handle:W3FactionReputationPoints"
                    }
                case "W3FactionReputationPoints":
                    {
                        //10-11, 21-22, 32-33 "W3FactionReputationPoints"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SRewardMultiplier":
                    //{
                    //    byte unknown1 = reader.ReadByte(ref size);

                    //    ushort nameIndex1 = reader.ReadUInt16(ref size);
                    //    string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                    //    ushort nameIndex2 = reader.ReadUInt16(ref size);
                    //    string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                    //    byte[] unknown2 = reader.ReadBytes(8, ref size);

                    //    //TODO 'typeIndex' is just another name for 'nameIndex' - this is potential name to use ReadValue
                    //    ushort typeIndex = reader.ReadUInt16(ref size);
                    //    string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                    //    //VariableValue value = ReadValue(reader, handleType, names, ref size);

                    //    //byte[] unknown3 = reader.ReadBytes(6, ref size);
                    //    byte[] unknown3 = reader.ReadBytes(10, ref size);

                    //    VariableValue<(string, string/*, VariableValue*/)> variableValue = VariableValue<(string, string/*, VariableValue*/)>.Create((value1, value2/*, value*/));
                    //    variableValue.AdditionalObject = (nameIndex1, nameIndex2, typeIndex, handleType, unknown1, unknown2, unknown3);
                    //    return variableValue;

                    //    //1-2 "rewardName"
                    //    //3-4 "CName"
                    //    //13-14 "Float"
                    //}
                    return ReadUnknownBytes(reader, size, ref size);
                case "SBuffImmunity":
                    //{
                    //    byte unknown1 = reader.ReadByte(ref size);

                    //    ushort nameIndex1 = reader.ReadUInt16(ref size);
                    //    string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                    //    ushort nameIndex2 = reader.ReadUInt16(ref size);
                    //    string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                    //    byte[] unknown2 = reader.ReadBytes(4, ref size);

                    //    ushort nameIndex3 = reader.ReadUInt16(ref size);
                    //    string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                    //    ushort nameIndex4 = reader.ReadUInt16(ref size);
                    //    string value4 = nameIndex4 != 0 ? SavegameFile.GetVariableIndexName(nameIndex4, names) : null;

                    //    ushort nameIndex5 = reader.ReadUInt16(ref size);
                    //    string value5 = nameIndex5 != 0 ? SavegameFile.GetVariableIndexName(nameIndex5, names) : null;

                    //    //VariableValue value = ReadValue(reader, value5, names, ref size);

                    //    byte[] unknown3 = reader.ReadBytes(8, ref size);

                    //    ushort nameIndex6 = reader.ReadUInt16(ref size);
                    //    string value6 = nameIndex6 != 0 ? SavegameFile.GetVariableIndexName(nameIndex6, names) : null;

                    //    //VariableValue value = ReadValue(reader, value6, names, ref size);

                    //    byte[] unknown4 = reader.ReadBytes(2, ref size);

                    //    VariableValue<(string, string, string, string, string, string)> variableValue = VariableValue<(string, string, string, string, string, string)>.Create((value1, value2, value3, value4, value5, value6));
                    //    variableValue.AdditionalObject = (nameIndex1, nameIndex2, nameIndex3, nameIndex4, nameIndex5, nameIndex6, unknown1, unknown2, unknown3, unknown4);
                    //    return variableValue;

                    //    //1-2 "buffType"
                    //    //3-4 "EEffectType"
                    //    //9-10 "EET_Pull", "EET_Snowstorm", "EET_SnowstormQ403"
                    //    //11-12 "sources"
                    //    //13-14 "array:2,0,CName"
                    //    //23-24 "HorseRidingBuffImmunity", "BuffImmunityInteractiveEntity"
                    //}
                    //return ReadUnknownBytes(reader, 27, ref size);
                    //TODO if there is 3-elements array with this, then at the end of the array are 2 empty bytes. The same in STutorialMessage or CActor. Empty bytes count is arrayLength - 1 ? To change then SQuestThreadSuspensionData
                    return ReadUnknownBytes(reader, size, ref size);
                case "EVehicleSlot":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "SItemUniqueId":
                    {
                        //4-5 "value"
                        //6-7 "Uint32"

                        //first file
                        //indexes: 8, 12, 13. size
                        //8, 183, 11. 15
                        //8, 184, 11. 15
                        //8, 81, 12. 15
                        //8, 80, 12. 15
                        //8, 82, 12. 15
                        //8, 208, 10. 15
                        //8, 236, 10. 15
                        //8, 79, 8. 15
                        //8, 34, 11. 15
                        //8, 8, 11. 15
                        //8, 84, 11. 21
                        //8, 177, 6. 15
                        //8, 17, 3. 18
                        //8, 85, 12. 27
                        //8, 62, 4. 15
                        //8, 131, 7. 33
                        //8, 244, 10. 15
                        //8, 2, 6. 15 (14-18)
                        //...empty

                        //second file
                        //TODO in this file indexes are shifted
                        //indexes: 8, 12, 13. size
                        //8, 14, 12. 15
                        //...
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SGlossaryImageOverride":
                    {
                        byte unknown1 = reader.ReadByte(ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        byte[] unknown2 = reader.ReadBytes(4, ref size);

                        ushort nameIndex3 = reader.ReadUInt16(ref size);
                        string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                        ushort nameIndex4 = reader.ReadUInt16(ref size);
                        string value4 = nameIndex4 != 0 ? SavegameFile.GetVariableIndexName(nameIndex4, names) : null;

                        ushort nameIndex5 = reader.ReadUInt16(ref size);
                        string value5 = nameIndex5 != 0 ? SavegameFile.GetVariableIndexName(nameIndex5, names) : null;

                        byte[] unknown3 = reader.ReadBytes(5, ref size);

                        //1-2 "uniqueTag"
                        //3-4 "CName"
                        //9-10 "mh102 Arachas 1E5A6867-40C6EFBC-C8FFB3BB-08C73878"
                        //11-12 "imageFileName"
                        //13-14 "String"

                        //TODO index 4 is also dependent on length
                        int length = unknown3[0] - 3;

                        byte[] data = reader.ReadBytes(length, ref size);
                        string value = ChunkedLz4File.Encoding.GetString(data).TrimEnd(char.MinValue);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (unknown1, unknown2, unknown3, nameIndex1, nameIndex2, nameIndex3, nameIndex4, nameIndex5, data);
                        return variableValue;
                    }
                case "SGameplayFact":
                    {
                        //size, elements number
                        //5, 6
                        //5, 6
                        //5, 1
                        //5, 3

                        //1-2 "factName"
                        //3-4 "String"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SAbilityAttributeValue":
                    {
                        //1-2 "valueAdditive"
                        //3-4 "Float"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "EHorseMode":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "STutorialMessage":
                    {
                    }
                    //return ReadUnknownBytes(reader, 5, ref size);
                    return ReadUnknownBytes(reader, size, ref size);
                case "EBehaviorGraph":
                    {
                        ushort nameIndex = reader.ReadUInt16(ref size);

                        string value = nameIndex != 0 ? SavegameFile.GetVariableIndexName(nameIndex, names) : null;

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = nameIndex;
                        return variableValue;
                    }
                case "W3LevelManager":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort nameIndex3 = reader.ReadUInt16(ref size);
                        string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                        byte[] unknown3 = reader.ReadBytes(5, ref size);

                        ushort nameIndex4 = reader.ReadUInt16(ref size);
                        string value4 = nameIndex4 != 0 ? SavegameFile.GetVariableIndexName(nameIndex4, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue value = ReadValue(reader, handleType, names, ref size);

                        VariableValue<object> variableValue = VariableValue<object>.Create(value);
                        variableValue.AdditionalObject = (unknown1, unknown2, unknown3, nameIndex1, nameIndex2, nameIndex3, nameIndex4, typeIndex, handleType);
                        return variableValue;

                        //6-7 "W3LevelManager"
                        //9-10 "owner"
                        //11-12 "handle:W3PlayerWitcher"
                        //18-19 "levelDefinitions"
                        //20-21 "array:2,0,SLevelDefinition"
                    }
                case "SLevelDefinition":
                    {
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "W3AbilityManager":
                    //return ReadUnknownBytes(reader, 28556, ref size);
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        VariableValue value = ReadValue(reader, handleType, names, ref size);

                        VariableValue<object> variableValue = VariableValue<object>.Create(value);
                        variableValue.AdditionalObject = (unknown1, unknown2, nameIndex1, nameIndex2, typeIndex, handleType);
                        return variableValue;

                        //6-7 "W3PlayerAbilityManager"
                        //9-10 "statPoints"
                        //11-12 "array:2,0,SBaseStat"
                        //TODO next bytes are repeat of the last ones here, then next bytes are proper array? Other cases similar
                    }
                case "SBaseStat":
                    {
                        //5-6 "current", "max"
                        //7-8 "Float"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "W3EffectManager":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort nameIndex3 = reader.ReadUInt16(ref size);
                        string value3 = nameIndex3 != 0 ? SavegameFile.GetVariableIndexName(nameIndex3, names) : null;

                        byte[] unknown3 = reader.ReadBytes(5, ref size);

                        ushort nameIndex4 = reader.ReadUInt16(ref size);
                        string value4 = nameIndex4 != 0 ? SavegameFile.GetVariableIndexName(nameIndex4, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        //array length exceeds available bytes
                        //VariableValue value = ReadValue(reader, handleType, names, ref size);
                        byte[] unknown4 = reader.ReadBytes(size, ref size);

                        //VariableValue<object> variableValue = VariableValue<object>.Create(value);
                        VariableValue<object> variableValue = VariableValue<object>.Create(unknown4);
                        variableValue.AdditionalObject = (unknown1, unknown2, unknown3, nameIndex1, nameIndex2, nameIndex3, nameIndex4, typeIndex, handleType);
                        return variableValue;

                        //6-7 "W3EffectManager"
                        //9-10 "owner"
                        //11-12 "handle:CActor"
                        //18-19 "effects"
                        //20-21 "array:2,0,handle:CBaseGameplayEffect"
                    }
                case "CBaseGameplayEffect":
                    return ReadUnknownBytes(reader, size, ref size);
                case "CPlayerInput":
                    {
                        byte[] unknown1 = reader.ReadBytes(6, ref size);

                        ushort nameIndex1 = reader.ReadUInt16(ref size);
                        string value1 = nameIndex1 != 0 ? SavegameFile.GetVariableIndexName(nameIndex1, names) : null;

                        byte unknown2 = reader.ReadByte(ref size);

                        ushort nameIndex2 = reader.ReadUInt16(ref size);
                        string value2 = nameIndex2 != 0 ? SavegameFile.GetVariableIndexName(nameIndex2, names) : null;

                        ushort typeIndex = reader.ReadUInt16(ref size);
                        string handleType = typeIndex != 0 ? SavegameFile.GetVariableIndexName(typeIndex, names) : null;

                        //VariableValue value = ReadValue(reader, handleType, names, ref size);
                        byte[] unknown3 = reader.ReadBytes(size, ref size);

                        //VariableValue<object> variableValue = VariableValue<object>.Create(value);
                        VariableValue<object> variableValue = VariableValue<object>.Create(unknown3);
                        variableValue.AdditionalObject = (unknown1, unknown2, nameIndex1, nameIndex2, typeIndex, handleType);
                        return variableValue;

                        //6-7 "W3PlayerTutorialInput"
                        //9-10 "actionLocks"
                        //11-12 "array:2,0,array:2,0,SInputActionLock"
                    }
                case "SInputActionLock":
                    return ReadUnknownBytes(reader, size, ref size);
                    //return ReadUnknownBytes(reader, 1, ref size);
                case "WeaponHolster":
                    {
                        //6-7 "WeaponHolster"
                    }
                    //return ReadUnknownBytes(reader, 56, ref size);
                    return ReadUnknownBytes(reader, size, ref size);
                case "CEntityTemplate":
                    {
                        // Might just be the same format as "String"
                        byte headerByte = reader.ReadByte(ref size);

                        byte singleByte = 0;
                        // HACK: Sometimes a single byte has to be skipped
                        if (reader.PeekByte() == 0x01)
                        {
                            singleByte = reader.ReadByte(ref size);
                        }

                        byte stringLength = (byte)(headerByte & 127);
                        string value = reader.ReadString(stringLength, ref size);

                        VariableValue variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (headerByte, singleByte);
                        return variableValue;
                    }
                case "CActor":
                    {
                        //0-1 "Bool"
                        //3-4, 13-14 "S_Sword_4", "S_Alchemy_5", "S_Sword_s11"
                        //5-6 "charStats"
                        //7-8 "handle:CCharacterStats"
                        //9-10 "c"
                        //15-16 "Entity"
                        //19-20 "CCharacterStats"
                        //24-25/34-35 "difficultyAbilities"
                        //26-27/36-37 "array:2,0,array:2,0,CName"
                        //32-33/42-43 "Bool"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SSelectedQuickslotItem":
                    {
                        //1-2 "sourceName"
                        //3-4 "CName"
                        //5-6 "isInteractive"
                        //11-12 "itemID"
                        //13-14 "SItemUniqueId"
                        //15-16 "e"
                        //20-21 "value"
                        //22-23 "Uint32"
                        //24-25 "isVisible"
                    }
                    return ReadUnknownBytes(reader, 36, ref size);
                case "SAnimMultiplyCauser":
                    return ReadUnknownBytes(reader, 15, ref size);
                case "SRadialSlotDef":
                    return ReadUnknownBytes(reader, size, ref size);
                default:
                    {
                        if (type.StartsWith("array:2,0,"))
                        {
                            var arrayElementType = type.Substring("array:2,0,".Length);
                            var arrayElementClrType = GetClrType(arrayElementType);
                            int arrayLength = reader.ReadInt32(ref size);

                            VariableArrayValue arrayValue = VariableArrayValue.Create(arrayElementClrType, arrayLength);
                            for (int i = 0; i < arrayLength; i++)
                            {
                                VariableValue value = ReadValue(reader, arrayElementType, names, ref size);
                                arrayValue[i] = value.Object;
                                arrayValue.SetVariableValue(value, i);
                            }

                            return arrayValue;
                        }
                        else if (type.StartsWith("handle:"))
                        {
                            var handleType = type.Substring("handle:".Length);
                            VariableValue value = ReadValue(reader, handleType, names, ref size);
                            return VariableHandleValue<object>.Create(value);
                        }
                        else if (type.StartsWith("soft:"))
                        {
                            var handleType = type.Substring("soft:".Length);
                            VariableValue value = ReadValue(reader, handleType, names, ref size);
                            return VariableSoftValue<object>.Create(value);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
            }
        }

        protected void WriteValue(BinaryWriter writer, string type, VariableValue variableValue)
        {
            switch (type)
            {
                case "String":
                    {
                        string value = (string)variableValue.Object;

                        int stringLength = value.Length;
                        byte singleByte = (byte)(stringLength / 64);

                        int moreSize = 0;
                        for (int i = 2; i <= singleByte; i++)
                            moreSize += 64;
                        byte headerByte = (byte)(stringLength  + 128 - moreSize);

                        writer.Write(headerByte);
                        if (singleByte != 0)
                            writer.Write(singleByte);
                        writer.Write(ChunkedLz4File.Encoding.GetBytes(value));
                    }
                    break;
                case "StringAnsi":
                    {
                        (byte length, byte[] data) = ((byte, byte[]))variableValue.AdditionalObject;
                        writer.Write(length);
                        writer.Write(data);
                    }
                    break;
                case "CName":
                    {
                        ushort cnameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(cnameIndex);
                    }
                    break;
                case "CGUID":
                    writer.Write((byte[])variableValue.AdditionalObject);
                    break;
                case "Bool":
                    writer.Write((bool)variableValue.Object);
                    break;
                case "Uint8":
                    writer.Write((byte)variableValue.Object);
                    break;
                case "Uint16":
                    writer.Write((ushort)variableValue.Object);
                    break;
                case "Uint32":
                    writer.Write((uint)variableValue.Object);
                    break;
                case "Uint64":
                    writer.Write((ulong)variableValue.Object);
                    break;
                case "Int8":
                    writer.Write((sbyte)variableValue.Object);
                    break;
                case "Int16":
                    writer.Write((short)variableValue.Object);
                    break;
                case "Int32":
                    writer.Write((int)variableValue.Object);
                    break;
                case "Int64":
                    writer.Write((long)variableValue.Object);
                    break;
                case "Double":
                    writer.Write((double)variableValue.Object);
                    break;
                case "Float":
                    writer.Write((float)variableValue.Object);
                    break;
                case "Vector":
                    {
                        (byte unknown1, ushort nameIndex, ushort typeIndex) = ((byte, ushort, ushort))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex);
                        writer.Write(typeIndex);
                    }
                    break;
                case "Vector2":
                    {
                        (byte unknown1, ushort nameIndex, ushort typeIndex) = ((byte, ushort, ushort))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex);
                        writer.Write(typeIndex);
                    }
                    break;
                case "EulerAngles":
                    {
                        (byte unknown1, ushort nameIndex, ushort typeIndex) = ((byte, ushort, ushort))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex);
                        writer.Write(typeIndex);
                    }
                    break;
                case "EngineTime":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EngineTransform":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "GameTime":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EntityHandle":
                    {
                        EntityHandle value = (EntityHandle)variableValue.Object;
                        writer.Write(value.Unknown1);
                        if (value.Unknown1 > 0)
                        {
                            writer.Write(value.Unknown2);
                            writer.Write(value.Unknown3);
                        }
                    }
                    break;
                case "IdTag":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "TagList":
                    {
                        TagList value = (TagList)variableValue.Object;

                        byte tagListHeader = (byte)variableValue.AdditionalObject;
                        writer.Write(tagListHeader);

                        byte tagListCount = (byte)(tagListHeader & 127);
                        for (int i = 0; i < tagListCount; i++)
                        {
                            writer.Write(value.Entities[i]);
                        }
                    }
                    break;
                case "eGwintFaction":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "EJournalStatus":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "EZoneName":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "EDifficultyMode":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "W3EnvironmentManager":
                    {
                        (ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, byte[] unknown1, byte unknown2) = ((ushort, ushort, ushort, byte[], byte))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(nameIndex3);
                    }
                    break;
                case "SQuestThreadSuspensionData":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SActionPointId":
                    {
                        (byte unknown1, short unknown2, byte[] unknown3) = ((byte, short, byte[]))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(unknown2);
                        if (unknown2 > 0)
                            writer.Write(unknown3);
                    }
                    break;
                case "EAIAttitude":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "EFocusModeVisibility":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "EEquipmentSlots":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "W3TutorialManagerUIHandler":
                    {
                        (ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, byte[] unknown1, byte unknown2) = ((ushort, ushort, ushort, byte[], byte))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(nameIndex3);
                    }
                    break;
                case "ESignType":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "W3Reputation":
                    {
                        (ushort nameIndex1, ushort nameIndex2, ushort typeIndex, string handleType, byte[] unknown1, byte unknown2) = ((ushort, ushort, ushort, string, byte[], byte))variableValue.AdditionalObject;
                        (string value1, string value2, VariableValue value) = ((string, string, VariableValue))variableValue.Object;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(typeIndex);
                        WriteValue(writer, handleType, value);
                    }
                    break;
                case "W3FactionReputationPoints":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SRewardMultiplier":
                    //{
                    //    (ushort nameIndex1, ushort nameIndex2, ushort typeIndex, string handleType, byte unknown1, byte[] unknown2, byte[] unknown3) = ((ushort, ushort, ushort, string, byte, byte[], byte[]))variableValue.AdditionalObject;
                    //    (string value1, string value2/*, VariableValue value*/) = ((string, string/*, VariableValue*/))variableValue.Object;
                    //    writer.Write(unknown1);
                    //    writer.Write(nameIndex1);
                    //    writer.Write(nameIndex2);
                    //    writer.Write(unknown2);
                    //    writer.Write(typeIndex);
                    //    //WriteValue(writer, handleType, value);
                    //    writer.Write(unknown3);
                    //}
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SBuffImmunity":
                    //{
                    //    (ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, ushort nameIndex4, ushort nameIndex5, ushort nameIndex6, byte unknown1, byte[] unknown2, byte[] unknown3, byte[] unknown4) = ((ushort, ushort, ushort, ushort, ushort, ushort, byte, byte[], byte[], byte[]))variableValue.AdditionalObject;
                    //    writer.Write(unknown1);
                    //    writer.Write(nameIndex1);
                    //    writer.Write(nameIndex2);
                    //    writer.Write(unknown2);
                    //    writer.Write(nameIndex3);
                    //    writer.Write(nameIndex4);
                    //    writer.Write(nameIndex5);
                    //    writer.Write(unknown3);
                    //    writer.Write(nameIndex6);
                    //    writer.Write(unknown4);
                    //}
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EVehicleSlot":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "SItemUniqueId":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SGlossaryImageOverride":
                    {
                        (byte unknown1, byte[] unknown2, byte[] unknown3, ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, ushort nameIndex4, ushort nameIndex5, byte[] data) = ((byte, byte[], byte[], ushort, ushort, ushort, ushort, ushort, byte[]))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(nameIndex2);
                        writer.Write(unknown2);
                        writer.Write(nameIndex3);
                        writer.Write(nameIndex4);
                        writer.Write(nameIndex5);
                        writer.Write(unknown3);
                        writer.Write(data);

                        //(byte[] unknown, byte[] data) = ((byte[], byte[]))variableValue.AdditionalObject;
                        //writer.Write(unknown);
                        //writer.Write(data);
                    }
                    break;
                case "SGameplayFact":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SAbilityAttributeValue":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EHorseMode":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "STutorialMessage":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EBehaviorGraph":
                    {
                        ushort nameIndex = (ushort)variableValue.AdditionalObject;
                        writer.Write(nameIndex);
                    }
                    break;
                case "W3LevelManager":
                    {
                        (byte[] unknown1, byte unknown2, byte[] unknown3, ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, ushort nameIndex4, ushort typeIndex, string handleType) = ((byte[], byte, byte[], ushort, ushort, ushort, ushort, ushort, string))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(nameIndex3);
                        writer.Write(unknown3);
                        writer.Write(nameIndex4);
                        writer.Write(typeIndex);
                        WriteValue(writer, handleType, (VariableValue)variableValue.Object);
                    }
                    break;
                case "SLevelDefinition":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3AbilityManager":
                    {
                        (byte[] unknown1, byte unknown2, ushort nameIndex1, ushort nameIndex2, ushort typeIndex, string handleType) = ((byte[], byte, ushort, ushort, ushort, string))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(typeIndex);
                        WriteValue(writer, handleType, (VariableValue)variableValue.Object);
                    }
                    break;
                case "SBaseStat":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3EffectManager":
                    {
                        (byte[] unknown1, byte unknown2, byte[] unknown3, ushort nameIndex1, ushort nameIndex2, ushort nameIndex3, ushort nameIndex4, ushort typeIndex, string handleType) = ((byte[], byte, byte[], ushort, ushort, ushort, ushort, ushort, string))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(nameIndex3);
                        writer.Write(unknown3);
                        writer.Write(nameIndex4);
                        writer.Write(typeIndex);
                        //WriteValue(writer, handleType, (VariableValue)variableValue.Object);
                        writer.Write((byte[])variableValue.Object);
                    }
                    break;
                case "CPlayerInput":
                    {
                        (byte[] unknown1, byte unknown2, ushort nameIndex1, ushort nameIndex2, ushort typeIndex, string handleType) = ((byte[], byte, ushort, ushort, ushort, string))variableValue.AdditionalObject;
                        writer.Write(unknown1);
                        writer.Write(nameIndex1);
                        writer.Write(unknown2);
                        writer.Write(nameIndex2);
                        writer.Write(typeIndex);
                        //WriteValue(writer, handleType, (VariableValue)variableValue.Object);
                        writer.Write((byte[])variableValue.Object);
                    }
                    break;
                case "WeaponHolster":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "CEntityTemplate":
                    {
                        (byte headerByte, byte singleByte) = ((byte, byte))variableValue.AdditionalObject;
                        writer.Write(headerByte);
                        if (singleByte == 0x01)
                            writer.Write(singleByte);
                        writer.Write(ChunkedLz4File.Encoding.GetBytes((string)variableValue.Object));
                    }
                    break;
                case "CActor":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SSelectedQuickslotItem":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SAnimMultiplyCauser":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SRadialSlotDef":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                default:
                    if (type.StartsWith("array:2,0,"))
                    {
                        VariableArrayValue variableArrayValue = variableValue as VariableArrayValue;
                        int arrayLength = ((Array)variableValue.Object).Length;

                        var arrayElementType = type.Substring("array:2,0,".Length);
                        var arrayElementClrType = GetClrType(arrayElementType);

                        writer.Write(arrayLength);

                        for (int i = 0; i < arrayLength; i++)
                        {
                            //if (!(arrayElementType == "SQuestThreadSuspensionData" && i > 0))
                            WriteValue(writer, arrayElementType, variableArrayValue.GetVariableValue(i));
                        }

                        break;
                    }
                    else if (type.StartsWith("handle:"))
                    {
                        var handleType = type.Substring("handle:".Length);
                        WriteValue(writer, handleType, (VariableValue)variableValue.Object);

                        break;
                    }
                    else if (type.StartsWith("soft:"))
                    {
                        var handleType = type.Substring("soft:".Length);
                        WriteValue(writer, handleType, (VariableValue)variableValue.Object);

                        break;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        //TODO finally to remove
        private VariableValue<byte[]> ReadUnknownBytes(BinaryReader reader, int length, ref int size)
        {
            byte[] unknown = reader.ReadBytes(length, ref size);
            return VariableValue<byte[]>.Create(unknown);
        }

        //TODO finally to remove
        private void WriteUnknownBytes(BinaryWriter writer, VariableValue variableValue)
        {
            writer.Write((byte[])variableValue.Object);
        }

        private Type GetClrType(string type)
        {
            switch (type)
            {
                case "Uint8":
                    return typeof(byte);
            }
            return typeof(object);
        }
    }
}
