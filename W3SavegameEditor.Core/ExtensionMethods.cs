using System;
using System.Buffers;
using System.IO;

namespace W3SavegameEditor.Core
{
    internal static class ExtensionMethods
    {
        public static string ReadString(this BinaryReader reader, int count)
        {
            return new string(reader.ReadChars(count));
        }


        private static T Peek<T>(BinaryReader reader, Func<T> getFunction)
        {
            long position = reader.BaseStream.Position;
            T value = getFunction();
            reader.BaseStream.Position = position;
            return value;
        }

        private static T ReadWithSize<T>(this BinaryReader reader, Func<T> getFunction, ref int size)
        {
            long position = reader.BaseStream.Position;
            T value = getFunction();
            size -= (int)(reader.BaseStream.Position - position);
            return value;
        }


        public static string PeekString(this BinaryReader reader, int count)
        {
            return Peek(reader, () => reader.ReadString(count));
        }

        public static char[] PeekChars(this BinaryReader reader, int count)
        {
            return Peek(reader, () => reader.ReadChars(count));
        }

        public static byte[] PeekBytes(this BinaryReader reader, int count)
        {
            return Peek(reader, () => reader.ReadBytes(count));
        }

        public static byte PeekByte(this BinaryReader reader)
        {
            return Peek(reader, () => reader.ReadByte());
        }

        public static ushort PeekUInt16(this BinaryReader reader)
        {
            return Peek(reader, () => reader.ReadUInt16());
        }

        public static uint PeekUInt32(this BinaryReader reader)
        {
            return Peek(reader, () => reader.ReadUInt32());
        }

        public static short PeekInt16(this BinaryReader reader)
        {
            return Peek(reader, () => reader.ReadInt16());
        }

        public static int PeekInt32(this BinaryReader reader)
        {
            return Peek(reader, () => reader.ReadInt32());
        }


        public static string ReadString(this BinaryReader reader, int count, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadString(count), ref size);
        }

        public static byte[] ReadBytes(this BinaryReader reader, int count, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadBytes(count), ref size);
        }

        public static byte ReadByte(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadByte(), ref size);
        }

        public static sbyte ReadSByte(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadSByte(), ref size);
        }

        public static bool ReadBoolean(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadBoolean(), ref size);
        }

        public static ushort ReadUInt16(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadUInt16(), ref size);
        }

        public static uint ReadUInt32(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadUInt32(), ref size);
        }

        public static ulong ReadUInt64(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadUInt64(), ref size);
        }

        public static short ReadInt16(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadInt16(), ref size);
        }

        public static int ReadInt32(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadInt32(), ref size);
        }

        public static long ReadInt64(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadInt64(), ref size);
        }

        public static double ReadDouble(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadDouble(), ref size);
        }

        public static float ReadSingle(this BinaryReader reader, ref int size)
        {
            return ReadWithSize(reader, () => reader.ReadSingle(), ref size);
        }

        // TODO: The following two methods are copied from source. Can remove this once library is moved to target .net standard 2.1
        //       can also remove System.Buffers package reference
        //       https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/Stream.cs#L737
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int numRead = stream.Read(sharedBuffer, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                    throw new IOException("Stream Too Long");
                new Span<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally { ArrayPool<byte>.Shared.Return(sharedBuffer); }
        }

        // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/Stream.cs#L770
        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(sharedBuffer);
                stream.Write(sharedBuffer, 0, buffer.Length);
            }
            finally { ArrayPool<byte>.Shared.Return(sharedBuffer); }
        }
    }
}
