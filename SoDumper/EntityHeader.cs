using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SO18_Dumper
{
    internal class EntityHeader
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion EQuaternion;

        ushort Flags;
        public ushort Type;

        public Util.BGRAColor Color0;
        public Util.BGRAColor Color1;

        public void Deserialize(Stream input)
        {
            float X = Util.ReadValueF32(input);
            float Y = Util.ReadValueF32(input);
            float Z = Util.ReadValueF32(input);
            Position = new Vector3(X, Y, Z);

            float ScaleX = Util.ReadValueF32(input);
            float ScaleY = Util.ReadValueF32(input);
            float ScaleZ = Util.ReadValueF32(input);
            Scale = new Vector3(ScaleX, ScaleY, ScaleZ);

            short rx = Util.ReadValueS16(input);
            short ry = Util.ReadValueS16(input);
            short rz = Util.ReadValueS16(input);
            short rw = Util.ReadValueS16(input);

            EQuaternion = new Quaternion(rx, ry, rz, rw);

            Flags = Util.ReadValueU16(input);
            Type = Util.ReadValueU16(input);

            Color0 = new Util.BGRAColor();
            Color0.B = Util.ReadValueU8(input);
            Color0.G = Util.ReadValueU8(input);
            Color0.R = Util.ReadValueU8(input);
            Color0.A = Util.ReadValueU8(input);

            Color1 = new Util.BGRAColor();
            Color1.B = Util.ReadValueU8(input);
            Color1.G = Util.ReadValueU8(input);
            Color1.R = Util.ReadValueU8(input);
            Color1.A = Util.ReadValueU8(input);

            input.Seek(4, SeekOrigin.Current); // DISCARD 4 bytes
        }
    }
}
