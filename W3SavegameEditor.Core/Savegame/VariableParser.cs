using System;
using System.Collections.Generic;
using System.IO;
using W3SavegameEditor.Core.Savegame.Attributes;
using W3SavegameEditor.Core.Savegame.VariableParsers;
using W3SavegameEditor.Core.Savegame.Variables;

namespace W3SavegameEditor.Core.Savegame
{
    public class VariableParser
    {
        public VariableParser()
        {
        }

        private VariableParserBase CreateParser(string magicNumber)
        {
            Type parserType = AttributeOperations.GetTypeByAttribute<VariableParserBase, VariableParserAttribute>((VariableParserAttribute attribute) => attribute?.MagicNumber == magicNumber);

            if (parserType == null)
            {
                magicNumber = magicNumber.Substring(0, 2);
                parserType = AttributeOperations.GetTypeByAttribute<VariableParserBase, VariableParserAttribute>((VariableParserAttribute attribute) => attribute?.MagicNumber == magicNumber);
            }

            if (parserType == null)
                return null;

            return (VariableParserBase)Activator.CreateInstance(parserType, this);
        }
        private VariableParserBase CreateParser(BinaryReader reader)
        {
            return CreateParser(reader.PeekString(4));
        }

        public Variable Parse(BinaryReader reader, List<string> names, ref int size)
        {
            var magic = reader.PeekString(4);
            VariableParserBase parser = CreateParser(reader);

            if (parser == null)
            {
                //TODO Is it always after some variable type? Is its size constant?
                //some contains magic strings inside
                //TODO finally remove this
                var unknownVariable = new UnknownVariable
                {
                    Data = reader.ReadBytes(size, ref size)
                };

                //var text = System.Text.Encoding.UTF8.GetString(unknownVariable.Data);

                return unknownVariable;
            }

            Variable variable = parser.Parse(reader, names, ref size);
            variable.MagicNumber = parser.MagicNumber;
            return variable;
        }

        public void Write(BinaryWriter writer, Variable variable, List<string> names)
        {
            if (variable is UnknownVariable unknownVariable)
            {
                writer.Write(unknownVariable.Data);
            }
            else
            {
                VariableParserBase parser = CreateParser(variable.MagicNumber);
                parser.Write(writer, variable, names);
            }
        }
    }

}
