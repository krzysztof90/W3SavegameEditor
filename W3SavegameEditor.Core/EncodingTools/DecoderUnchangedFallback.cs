using System.Text;

namespace W3SavegameEditor.Core.EncodingTools
{
    public class DecoderUnchangedFallback : DecoderFallback
    {
        public DecoderUnchangedFallback()
        {
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new DecoderUnchangedFallbackBuffer(this);
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
