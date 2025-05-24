using System;
using System.IO;
using System.Text;

namespace W3SavegameEditor.Core.ChunkedLz4
{
    public class ChunkedLz4FileHeader
    {
        public int ChunkCount { get; set; }
        public int HeaderSize { get; set; }

        public static ChunkedLz4FileHeader Read(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true))
            {
                string saveFileHeader = reader.ReadString(4);
                if (saveFileHeader != ChunkedLz4File.header1)
                {
                    throw new InvalidOperationException();
                }

                string chunkedLz4FileHeader = reader.ReadString(4);
                if (chunkedLz4FileHeader != ChunkedLz4File.header2)
                {
                    throw new InvalidOperationException();
                }

                return new ChunkedLz4FileHeader
                {
                    ChunkCount = reader.ReadInt32(),
                    HeaderSize = reader.ReadInt32()
                };
            }
        }
    }
}
