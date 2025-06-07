using System.Linq;
using System.Reflection;
using System.Text;

namespace W3SavegameEditor.Core.EncodingTools
{
    public class ASCIIUnchangedEncoding : ASCIIEncoding
    {
        public ASCIIUnchangedEncoding() : base()
        {
            PropertyInfo fieldEncoding = typeof(ASCIIEncoding).GetProperties().Single(p => p.Name == nameof(IsReadOnly));
            fieldEncoding.SetValue(this, false);

            EncoderFallback = new EncoderUnchangedFallback();
            DecoderFallback = new DecoderUnchangedFallback();
        }

        public override byte[] GetBytes(string s)
        {
            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];

            for (int i = 0; i < byteCount; i++)
                bytes[i] = (byte)s[i];

            return bytes;
        }

        public override int GetByteCount(string chars)
        {
            return chars.Length;
        }
    }
}
