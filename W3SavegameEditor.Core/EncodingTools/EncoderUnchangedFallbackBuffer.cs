using System.Text;

namespace W3SavegameEditor.Core.EncodingTools
{
    public class EncoderUnchangedFallbackBuffer : EncoderFallbackBuffer
    {
        public EncoderUnchangedFallbackBuffer(EncoderUnchangedFallback fallback)
        {
        }

        public override bool Fallback(char charUnknown, int index)
        {
            return false;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            return false;
        }

        public override char GetNextChar()
        {
            return (char)0;
        }

        public override bool MovePrevious()
        {
            return false;
        }

        public override int Remaining => 0;
    }
}
