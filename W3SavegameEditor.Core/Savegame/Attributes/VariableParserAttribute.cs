using System;

namespace W3SavegameEditor.Core.Savegame.Attributes
{
    public class VariableParserAttribute : Attribute
    {
        public string MagicNumber { get; set; }

        public VariableParserAttribute(string magicNumber)
        {
            MagicNumber = magicNumber;
        }
    }
}
