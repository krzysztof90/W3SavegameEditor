using System;

namespace W3SavegameEditor.Core.Savegame.Variables
{
    public abstract class VariableTyped : Variable
    {
        public string Type { get; set; }
        public ushort TypeIndex { get; set; }
        public Type ClrType { get; set; }
        public VariableValue Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Type, base.ToString(), Value);
        }
    }
}