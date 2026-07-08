using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class Util
    {
        internal struct vec3
        {
            public float x;
            public float y;
            public float z;
            public override string ToString() => $"(x: {x}, y: {y}, z: {z})";
        }

        public static string ReadString(Stream stream, Encoding encoding, int size)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                byte[] array = reader.ReadBytes(size);

                if (array.Length != size)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading string.");
                }

                return encoding.GetString(array);
            }
        }

        public static string ReadStringChrome(Stream stream, Encoding encoding)
        {
            int size = ReadValueU16(stream);

            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                byte[] array = reader.ReadBytes(size);

                if (array.Length != size)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading string.");
                }

                return encoding.GetString(array);
            }
        }

        public static uint ReadValueU32(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadUInt32();
            }
        }

        public static uint[] ReadValueU32(Stream stream, uint count)
        {
            uint[] values = new uint[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadValueU32(stream);
            }
            return values;
        }

        public static ushort ReadValueU16(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadUInt16();
            }
        }
        public static byte ReadByte(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadByte();
            }
        }
        public static byte[] ReadByteArray(Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            int bytesRead = stream.Read(buffer, 0, length);

            if (bytesRead < length)
            {
                throw new EndOfStreamException($"Expected {length} bytes, but only {bytesRead} were read.");
            }

            return buffer;
        }
        public static float ReadFloat(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadSingle();
            }
        }
        public static long ReadValueS64(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadInt64();
            }
        }
        public static int ReadValueS32(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadInt32();
            }
        }

        //write
        public static void WriteString(Stream stream, string value, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(value);
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, leaveOpen: true))
            {
                writer.Write(bytes);
            }
        }
        public static void WriteStringChrome(Stream stream, string value, Encoding encoding)
        {
            WriteU16(stream, (ushort)value.Length);

            byte[] bytes = encoding.GetBytes(value);
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, leaveOpen: true))
            {
                writer.Write(bytes);
            }
        }


        public static void WriteU32(Stream stream, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteU16(Stream stream, ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteByte(Stream stream, byte value)
        {
            stream.WriteByte(value);
        }
        public static void WriteFloat(Stream stream, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteS64(Stream stream, long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }
        public static void WriteS32(Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
