using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SO18_Dumper.Header;

namespace SO18_Dumper
{
    internal class MeshHeader
    {
        public string MeshName;
        public string SkinName;

        public Int64 required_tags;
        public Int64 forbidden_tags;

        public uint RandomSeed;
        public uint collision_collide_bits; //m_TypesRender->collision_collide_bits
        public uint SectionEnd; //I don't like that it's not actually the end

        public string MeshElement;
        public ushort UnknownShort;
        public void Deserialize(Stream input, int sobjver)
        {
            ushort StrLen = Util.ReadValueU16(input);
            MeshName = Util.ReadString(input, Encoding.ASCII, StrLen);

            StrLen = Util.ReadValueU16(input);
            SkinName = Util.ReadString(input, Encoding.ASCII, StrLen);

            required_tags = Util.ReadValueS64(input);
            forbidden_tags = Util.ReadValueS64(input);


            RandomSeed = Util.ReadValueU32(input);
            collision_collide_bits = Util.ReadValueU32(input);
            SectionEnd = Util.ReadValueU32(input);

            StrLen = Util.ReadValueU16(input);
            MeshElement = Util.ReadString(input, Encoding.ASCII, StrLen);
            UnknownShort = Util.ReadValueU16(input);

            if (sobjver == 16)
            {   //to lazy to write code, should discard 132 bytes
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
            }
            if (sobjver == 13) //still don't care
            {
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
                _ = Util.ReadValueU32(input);
            }
        }
    }
}
