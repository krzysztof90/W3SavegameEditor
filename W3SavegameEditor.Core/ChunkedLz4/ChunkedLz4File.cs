using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace W3SavegameEditor.Core.ChunkedLz4
{
    public static class ChunkedLz4File
    {
        public const string header1 = "SNFH";
        public const string header2 = "FZLC";

        private const int ChunkSize = 1024 * 1024;
        public const int HeaderSize = 3084;

        public static Stream Decompress(Stream input)
        {
            ChunkedLz4FileHeader header = ChunkedLz4FileHeader.Read(input);
            var table = ChunkedLz4FileTable.Read(input, header.ChunkCount);
            input.Position = header.HeaderSize;

            var data = new byte[header.HeaderSize + table.Chunks.Sum(c => c.DecompressedChunkSize)];
            var memoryStream = new MemoryStream(data) { Position = header.HeaderSize };
            foreach (Lz4Chunk chunk in table.Chunks)
            {
                Span<byte> chunkData = chunk.Read(input);
                memoryStream.Write(chunkData);
                Debug.Assert(input.Position == chunk.EndOfChunkOffset || chunk.EndOfChunkOffset == 0);
            }

            memoryStream.Position = header.HeaderSize;
            return memoryStream;
        }

        public static Stream Compress4(Stream input)
        {
            const int chunkSize = ChunkSize;
            List<ChunkCompressionData> chunkList = new List<ChunkCompressionData>();

            byte[] readBuffer = new byte[chunkSize];
            int bytesRead;
            while ((bytesRead = input.Read(readBuffer, 0, chunkSize)) > 0)
            {
                byte[] decompressedData = new byte[bytesRead];
                Buffer.BlockCopy(readBuffer, 0, decompressedData, 0, bytesRead);

                byte[] compressedBuffer = new byte[LZ4Codec.MaximumOutputSize(bytesRead)];
                int compressedSize = LZ4Codec.Encode(decompressedData, compressedBuffer);

                // Store exactly the compressed result.
                byte[] actualCompressedData = new byte[compressedSize];
                Buffer.BlockCopy(compressedBuffer, 0, actualCompressedData, 0, compressedSize);

                // EndOfChunkOffset will be computed later
                chunkList.Add(new ChunkCompressionData
                {
                    CompressedData = actualCompressedData,
                    CompressedChunkSize = compressedSize,
                    DecompressedChunkSize = bytesRead,
                    EndOfChunkOffset = 0
                });
            }

            int chunkCount = chunkList.Count;
            int headerSize = HeaderSize;

            int currentOffset = headerSize;
            ChunkCompressionData lastChunk = null;
            foreach (ChunkCompressionData chunk in chunkList)
            {
                currentOffset += chunk.CompressedChunkSize;
                chunk.EndOfChunkOffset = currentOffset;
                lastChunk = chunk;
            }
            lastChunk.EndOfChunkOffset = 0;

            MemoryStream outputStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(outputStream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes(header1));
                writer.Write(Encoding.ASCII.GetBytes(header2));

                writer.Write(chunkCount);
                writer.Write(headerSize);

                foreach (ChunkCompressionData chunk in chunkList)
                {
                    writer.Write(chunk.CompressedChunkSize);
                    writer.Write(chunk.DecompressedChunkSize);
                    writer.Write(chunk.EndOfChunkOffset);
                }

                writer.BaseStream.Position = headerSize;

                foreach (ChunkCompressionData chunk in chunkList)
                    writer.Write(chunk.CompressedData);
            }

            outputStream.Position = 0;
            return outputStream;
        }
    }
}