using System.Text;

namespace W3SavegameEditor.Core.EncodingTools
{
    public class DecoderUnchangedFallbackBuffer : DecoderFallbackBuffer
    {
        private char charCurrent;
        int fallbackCount = -1;
        int fallbackIndex = -1;

        public DecoderUnchangedFallbackBuffer(DecoderUnchangedFallback fallback)
        {
        }

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            charCurrent = (char)bytesUnknown[0];

            fallbackCount = 1;
            fallbackIndex = -1;

            return true;
        }

        public override char GetNextChar()
        {
            fallbackCount--;
            fallbackIndex++;

            if (fallbackCount < 0)
                return (char)0;

            return charCurrent;
        }

        public override bool MovePrevious()
        {
            if (fallbackCount >= -1 && fallbackIndex >= 0)
            {
                fallbackIndex--;
                fallbackCount++;
                return true;
            }

            return false;
        }

        public override int Remaining => fallbackCount < 0 ? 0 : fallbackCount;
    }
}
