using System;
using System.IO;
using System.Text;

namespace NetX.AutoServiceGenerator.Definitions
{
    public static class MemoryStreamExtensions
    {
        public static void Skip(this MemoryStream stream, in int size)
        {
            for (var i = 0; i < size; i++)
                stream.WriteByte(0);
        }

        public static void Write(this MemoryStream stream, in bool value)
        {
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public static void Write(this MemoryStream stream, in byte value)
        {
            stream.WriteByte(value);
        }

        public static void Write(this MemoryStream stream, in sbyte value)
        {
            stream.WriteByte((byte)value);
        }

        public static void Write(this MemoryStream stream, in short value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        public static void Write(this MemoryStream stream, in ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        public static void Write(this MemoryStream stream, in int value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        public static void Write(this MemoryStream stream, in uint value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        public static void Write(this MemoryStream stream, in long value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 32));
            stream.WriteByte((byte)(value >> 40));
            stream.WriteByte((byte)(value >> 48));
            stream.WriteByte((byte)(value >> 56));
        }

        public static void Write(this MemoryStream stream, in ulong value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 32));
            stream.WriteByte((byte)(value >> 40));
            stream.WriteByte((byte)(value >> 48));
            stream.WriteByte((byte)(value >> 56));
        }

        public static void Write(this MemoryStream stream, in float value)
        {
            var src = BitConverter.GetBytes(value);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(src);

            stream.Write(src, 0, src.Length);
        }

        public static void Write(this MemoryStream stream, string value, in int length, in int offset = 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                stream.Skip(length);
                return;
            }

            var bytes = Encoding.Default.GetBytes(value);

            var valueLen = bytes.Length - offset;
            if (valueLen > length)
                throw new ArgumentException("String length is bigger than reported length", nameof(length));

            stream.Write(bytes, offset, valueLen);

            var rest = length - valueLen;
            if (rest > 0)
                stream.Skip(in rest);
        }

        public static void Write(this MemoryStream stream, byte[] value, in int length, in int offset = 0)
        {
            if (value == null)
            {
                stream.Skip(length);
                return;
            }

            var valueLen = value.Length - offset;
            if (valueLen > length)
                throw new ArgumentException("Byte[] length is bigger than reported length", nameof(length));

            stream.Write(value, offset, valueLen);

            var rest = length - valueLen;
            if (rest > 0)
                stream.Skip(in rest);
        }

        public static void Write(this MemoryStream stream, byte[] value)
        {
            
        }

        public static void Write(this MemoryStream stream, Guid guid)
        {
            var bytes = guid.ToByteArray();
            stream.Write(bytes.Length);
            stream.Write(bytes, bytes.Length);
        }

        public static void Write(this MemoryStream stream, string value)
        {
            stream.Write(value.Length);
            stream.Write(value, value.Length);
        }
    }
}