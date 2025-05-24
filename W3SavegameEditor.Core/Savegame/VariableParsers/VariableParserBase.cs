using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using W3SavegameEditor.Core.Exceptions;
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
            writer.Write(Encoding.ASCII.GetBytes(variable.MagicNumber));
            WriteImpl(writer, (T)variable);
        }
        public abstract void WriteImpl(BinaryWriter writer, T variable);

        public override void Verify(BinaryReader reader, ref int size)
        {
            var bytesToRead = MagicNumber.Length;
            var readMagicNumber = reader.ReadString(bytesToRead, ref size);
            if (readMagicNumber != MagicNumber)
            {
                throw new ParseVariableException(
                    string.Format(
                    "Expeced {0} but read {1} at {2}",
                    MagicNumber,
                    readMagicNumber,
                    reader.BaseStream.Position - bytesToRead));
            }
        }

        protected VariableValue ReadValue(BinaryReader reader, string type, List<string> names, ref int size)
        {
            //TODO Analyze how unknown bytes can be read. It is important to know how to write appropriate values
            //TODO all as separate classes

            //How to check if there are some repeatable names inside unknown bytes:
            //var bytes = reader.PeekBytes(size);
            //var potentialNames = bytes.Select(b => b == 0 ? "" : SavegameFile.GetVariableIndexName(b, names)).ToList();
            //of course it needs to repeat within different files

            switch (type)
            {
                case "String":
                    {
                        // TODO: Implement correct variable-length decoding
                        byte headerByte = reader.ReadByte(ref size);

                        byte singleByte = 0;
                        // HACK: Sometimes a single byte has to be skipped
                        if (reader.PeekByte() == 0x01)
                        {
                            singleByte = reader.ReadByte(ref size);
                        }

                        byte stringLength = (byte)(headerByte & 127);
                        string value = reader.ReadString(stringLength, ref size);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (headerByte, singleByte);
                        return variableValue;
                    }
                case "StringAnsi":
                    {
                        byte length = reader.ReadByte(ref size);
                        byte[] data = reader.ReadBytes(length, ref size);

                        string value = Encoding.ASCII.GetString(data).TrimEnd(char.MinValue);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (length, data);
                        return variableValue;
                    }
                case "CName":
                    {
                        ushort cnameIndex = reader.ReadUInt16(ref size);

                        string value = null;
                        if (cnameIndex != 0)
                            value = SavegameFile.GetVariableIndexName(cnameIndex, names);

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
                        //3 "Float"
                    }
                    return ReadUnknownBytes(reader, 5, ref size);
                //TODO 19?
                //case "Vector2":
                //    return ReadUnknownBytes(reader, 19, ref size);
                case "EulerAngles":
                    {
                        //3 "Float"
                    }
                    return ReadUnknownBytes(reader, 5, ref size);
                case "EngineTime":
                    {
                    }
                    return ReadUnknownBytes(reader, 3, ref size);
                case "GameTime":
                    {
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
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "EJournalStatus":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "EZoneName":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "EDifficultyMode":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "W3EnvironmentManager":
                    {
                    }
                    return ReadUnknownBytes(reader, 13, ref size);
                case "SQuestThreadSuspensionData":
                    {
                    }
                    //return ReadUnknownBytes(reader, 5, ref size);
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
                    return ReadUnknownBytes(reader, 2, ref size);
                case "W3TutorialManagerUIHandler":
                    {
                        //20 "CName"
                        //26 "CName"
                    }
                    return ReadUnknownBytes(reader, 13, ref size);
                case "ESignType":
                    return ReadUnknownBytes(reader, 2, ref size);
                case "W3Reputation":
                    {
                    }
                    return ReadUnknownBytes(reader, 56, ref size);
                case "SRewardMultiplier":
                    {
                        //3 "CName"
                        //13 "Float"
                    }
                    return ReadUnknownBytes(reader, 25, ref size);
                case "SBuffImmunity":
                    {
                        //13 "array:2,0,CName"
                    }
                    return ReadUnknownBytes(reader, 27, ref size);
                case "EVehicleSlot":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "SItemUniqueId":
                    {
                        //4 "value"
                        //6 "Uint32"

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
                        //indexes: 8, 12, 13. size
                        //8, 14, 12. 15
                        //...
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SGlossaryImageOverride":
                    {
                        //var u1 = reader.ReadBytes(3, ref size);

                        //byte typeIndex = reader.ReadByte(ref size);
                        //string handleType = SavegameFile.GetVariableIndexName(typeIndex, names);

                        //VariableValue value2 = ReadValue(reader, handleType, names, ref size);

                        //var u2 = reader.ReadBytes(7, ref size);

                        //byte typeIndex3 = reader.ReadByte(ref size);
                        //string handleType3 = SavegameFile.GetVariableIndexName(typeIndex3, names);

                        //byte unknown8 = reader.ReadByte(ref size);

                        //VariableValue value3 = ReadValue(reader, handleType3, names, ref size);

                        //byte unknown9 = reader.ReadByte(ref size);


                        //3 "CName"
                        //13 "String"

                        //first file
                        //indeksy: 1, 2, 3, 5, 9, 10, 11, 12, 13, 15, 19. size
                        //244, 5, 34, 6, 90, 12, 246, 5, 17, 37, 160. 54
                        //244, 5, 34, 6, 91, 12, 246, 5, 17, 35, 158. 52
                        //244, 5, 34, 6, 92, 12, 246, 5, 17, 37, 160. 54
                        //244, 5, 34, 6, 95, 12, 246, 5, 17, 28, 151. 45
                        //244, 5, 34, 6, 100, 12, 246, 5, 17, 36, 159. 53
                        //244, 5, 34, 6, 102, 12, 246, 5, 17, 37, 160. 54
                        //244, 5, 34, 6, 103, 12, 246, 5, 17, 36, 159. 53
                        //244, 5, 34, 6, 125, 26, 246, 5, 17, 21, 144. 38

                        //second file
                        //indeksy: 1, 2, 3, 5, 9, 10, 11, 12, 13, 15, 19. size
                        //236, 5, 25, 6, 218, 11, 238, 5, 3, 37, 160. 54
                        //236, 5, 25, 6, 219, 11, 238, 5, 3, 35, 158. 52
                        //236, 5, 25, 6, 220, 11, 238, 5, 3, 37, 160. 54
                        //236, 5, 25, 6, 221, 11, 238, 5, 3, 40, 163. 57
                        //236, 5, 25, 6, 223, 11, 238, 5, 3, 28, 151. 45
                        //236, 5, 25, 6, 224, 11, 238, 5, 3, 40, 163. 57
                        //236, 5, 25, 6, 227, 11, 238, 5, 3, 36, 159. 53
                        //236, 5, 25, 6, 228, 11, 238, 5, 3, 36, 159. 53
                        //236, 5, 25, 6, 230, 11, 238, 5, 3, 37, 160. 54
                        //236, 5, 25, 6, 231, 11, 238, 5, 3, 36, 159. 53

                        byte[] unknown = reader.ReadBytes(20, ref size);

                        //TODO index 19 is also dependent on length
                        int length = unknown[15] - 3;

                        byte[] data = reader.ReadBytes(length, ref size);
                        string value = Encoding.ASCII.GetString(data).TrimEnd(char.MinValue);

                        VariableValue<string> variableValue = VariableValue<string>.Create(value);
                        variableValue.AdditionalObject = (unknown, data);
                        return variableValue;
                    }
                case "SGameplayFact":
                    {
                        //size, elements number
                        //5, 6
                        //5, 6
                        //5, 1
                        //5, 3
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "SAbilityAttributeValue":
                    {
                        //3 "Float"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "EHorseMode":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "STutorialMessage":
                    {
                    }
                    return ReadUnknownBytes(reader, 5, ref size);
                case "EBehaviorGraph":
                    {
                    }
                    return ReadUnknownBytes(reader, 2, ref size);
                case "W3LevelManager":
                    //return ReadUnknownBytes(reader, 2687, ref size);
                    //return ReadUnknownBytes(reader, 2639, ref size);
                    {
                        //9 "owner"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "W3AbilityManager":
                    //return ReadUnknownBytes(reader, 28556, ref size);
                    {
                        //6 "W3PlayerAbilityManager"
                        //9 "statPoints"
                        //11 "array:2,0,SBaseStat"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "W3EffectManager":
                    //return ReadUnknownBytes(reader, 3554, ref size);
                    {
                        //6 "W3EffectManager"
                        //9 "owner"
                        //11 "handle:CActor"
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "CPlayerInput":
                    //return ReadUnknownBytes(reader, 464, ref size);
                    {
                    }
                    return ReadUnknownBytes(reader, size, ref size);
                case "WeaponHolster":
                    //return ReadUnknownBytes(reader, 56, ref size);
                    {
                    }
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
                //case "SRadialSlotDef":
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
                        (byte headerByte, byte singleByte) = ((byte, byte))variableValue.AdditionalObject;

                        writer.Write(headerByte);

                        if (singleByte == 0x01)
                        {
                            writer.Write(singleByte);
                        }

                        //writer.Write((string)variableValue.Object);
                        writer.Write(Encoding.ASCII.GetBytes((string)variableValue.Object));
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
                    WriteUnknownBytes(writer, variableValue);
                    break;
                //case "Vector2":
                //    WriteUnknownBytes(writer, variableValue);
                //    break;
                case "EulerAngles":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EngineTime":
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
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EJournalStatus":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EZoneName":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EDifficultyMode":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3EnvironmentManager":
                    WriteUnknownBytes(writer, variableValue);
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
                        {
                            writer.Write(unknown3);
                        }
                    }
                    break;
                case "EAIAttitude":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3TutorialManagerUIHandler":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "ESignType":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3Reputation":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SRewardMultiplier":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SBuffImmunity":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EVehicleSlot":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SItemUniqueId":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SGlossaryImageOverride":
                    {
                        (byte[] unknown, byte[] data) = ((byte[], byte[]))variableValue.AdditionalObject;
                        writer.Write(unknown);
                        writer.Write(data);
                    }
                    break;
                case "SGameplayFact":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "SAbilityAttributeValue":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EHorseMode":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "STutorialMessage":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "EBehaviorGraph":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3LevelManager":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3AbilityManager":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "W3EffectManager":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "CPlayerInput":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "WeaponHolster":
                    WriteUnknownBytes(writer, variableValue);
                    break;
                case "CEntityTemplate":
                    {
                        (byte headerByte, byte singleByte) = ((byte, byte))variableValue.AdditionalObject;

                        writer.Write(headerByte);

                        if (singleByte == 0x01)
                        {
                            writer.Write(singleByte);
                        }

                        writer.Write(Encoding.ASCII.GetBytes((string)variableValue.Object));
                    }
                    break;
                //case "SRadialSlotDef":
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

        private VariableValue<byte[]> ReadUnknownBytes(BinaryReader reader, int length, ref int size)
        {
            byte[] unknown = reader.ReadBytes(length, ref size);
            return VariableValue<byte[]>.Create(unknown);
        }

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
