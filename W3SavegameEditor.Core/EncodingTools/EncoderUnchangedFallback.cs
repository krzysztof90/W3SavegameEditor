using System.Text;

namespace W3SavegameEditor.Core.EncodingTools
{
    public class EncoderUnchangedFallback : EncoderFallback
    {
        public EncoderUnchangedFallback()
        {
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new EncoderUnchangedFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get
            {
                return 1;
            }
        }
    }
}
