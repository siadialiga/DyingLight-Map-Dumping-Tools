using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Numerics;

namespace SO18_Dumper
{
    internal class Util
    {
        public struct BGRAColor
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
        };

        public static Vector3 ToZYXEulerAngles(Quaternion quaternion)
        {
            // Normalize quaternion to avoid errors
            float magnitude = (float)Math.Sqrt(
                quaternion.X * quaternion.X +
                quaternion.Y * quaternion.Y +
                quaternion.Z * quaternion.Z +
                quaternion.W * quaternion.W);

            float rx = quaternion.X / magnitude;
            float ry = quaternion.Y / magnitude;
            float rz = quaternion.Z / magnitude;
            float rw = quaternion.W / magnitude;

            // ZYX Euler Angles (Yaw, Pitch, Roll)
            double yawZ = -Math.Atan2(2.0 * (rw * rz + rx * ry), 1.0 - 2.0 * (ry * ry + rz * rz)) * (180.0 / Math.PI);
            double pitchY = -Math.Asin(Clamp(2.0 * (rw * ry - rz * rx), -1.0, 1.0)) * (180.0 / Math.PI);
            double rollX = -Math.Atan2(2.0 * (rw * rx + ry * rz), 1.0 - 2.0 * (rx * rx + ry * ry)) * (180.0 / Math.PI);

            return new Vector3((float)rollX, (float)pitchY, (float)yawZ);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static string ReadString(Stream stream, Encoding encoding, int size)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                byte[] array = reader.ReadBytes(size);

                // Check for incomplete reads
                if (array.Length != size)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading string.");
                }

                return encoding.GetString(array);
            }
        }

        public static float ReadValueF32(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadSingle();
            }
        }
        public static byte ReadValueU8(Stream stream)
        {
            // Read a single byte from the stream
            int value = stream.ReadByte();

            // Ensure the stream hasn't ended
            if (value == -1)
                throw new EndOfStreamException("Attempted to read past the end of the stream.");

            return (byte)value; // Cast to byte
        }

        public static int ReadValueS32(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadInt32();
            }
        }

        public static uint ReadValueU32(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadUInt32();
            }
        }
        public static ushort ReadValueU16(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadUInt16();
            }
        }
        public static short ReadValueS16(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadInt16();
            }
        }
        public static long ReadValueS64(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                return reader.ReadInt64();
            }
        }
    }
}
