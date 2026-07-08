using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class SEntity
    {
        public struct xform
        {
            public float x;
            public float y;
            public float z;

            public float xScale;
            public float yScale;
            public float zScale;

            public ushort rx;
            public ushort ry;
            public ushort rz;
            public ushort rw; //Quaternion real part

            public void Deserialize(Stream input)
            {
                x = Util.ReadFloat(input);
                y = Util.ReadFloat(input);
                z = Util.ReadFloat(input);

                xScale = Util.ReadFloat(input);
                yScale = Util.ReadFloat(input);
                zScale = Util.ReadFloat(input);

                rx = Util.ReadValueU16(input);
                ry = Util.ReadValueU16(input);
                rz = Util.ReadValueU16(input);
                rw = Util.ReadValueU16(input);
            }
            public void Serialize(Stream output)
            {
                Util.WriteFloat(output, x);
                Util.WriteFloat(output, y);
                Util.WriteFloat(output, z);

                Util.WriteFloat(output, xScale);
                Util.WriteFloat(output, yScale);
                Util.WriteFloat(output, zScale);

                Util.WriteU16(output, rx);
                Util.WriteU16(output, ry);
                Util.WriteU16(output, rz);
                Util.WriteU16(output, rw);
            }
            public override string ToString()
            {
                return $"  Position: ({x}, {y}, {z})\n" +
                       $"  Scale:    ({xScale}, {yScale}, {zScale})\n" +
                       $"  Rotation Quaternion (ushort): (rx: {rx}, ry: {ry}, rz: {rz}, rw: {rw})";
            }

        }

        public xform world;
        public ushort flags;
        public ushort type;
        public uint color1;
        public uint color2;
        public void Deserialize(Stream input)
        {
            world = new xform();
            world.Deserialize(input);

            flags = Util.ReadValueU16(input);
            type = Util.ReadValueU16(input);
            color1 = Util.ReadValueU32(input);
            color2 = Util.ReadValueU32(input);
        }
        public void Serialize(Stream output)
        {
            world.Serialize(output);

            Util.WriteU16(output, flags);
            Util.WriteU16(output, type);
            Util.WriteU32(output, color1);
            Util.WriteU32(output, color2);
        }
        public override string ToString()
        {
            return $"SEntity:\n" +
                   $"  World Transform:\n{world}\n" +
                   $"  Flags: {flags}\n" +
                   $"  Type: {type}\n" +
                   $"  Color1: 0x{color1:X8}\n" +
                   $"  Color2: 0x{color2:X8}";
        }
    }
}
