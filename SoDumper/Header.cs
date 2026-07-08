using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO18_Dumper
{
    internal class Header
    {
        public string macicID; //SO18
        public class Flags
        {
            // Sections
            public uint Mesh_Offset { get; set; }
            public uint Entity_Offset { get; set; }
            public uint BatchTree_Offset { get; set; }
            public uint Unkn1_Offset { get; set; }
            public uint Unkn2_Offset { get; set; }
            public uint Unkn3_Offset { get; set; }

            // Flags
            public uint m_NumEntities { get; set; }
            public uint m_NumTypes { get; set; }
            public uint m_NumMeshes { get; set; }

            // Unknowns
            public uint Unkn4_Offset { get; set; }
            public uint Unkn5_Offset { get; set; }
            public uint Unkn6_Offset { get; set; }
            public uint Unkn7_Offset { get; set; }
            public uint Unkn8_Offset { get; set; }

            public void Load(Stream input, int sobjver)
            {
                if (sobjver == 18) {
                // Read all values in the order they appear in the stream
                    Mesh_Offset = Util.ReadValueU32(input);
                    Entity_Offset = Util.ReadValueU32(input);
                    BatchTree_Offset = Util.ReadValueU32(input);
                    Unkn1_Offset = Util.ReadValueU32(input);
                    Unkn2_Offset = Util.ReadValueU32(input);
                    Unkn3_Offset = Util.ReadValueU32(input);

                    m_NumEntities = Util.ReadValueU32(input);
                    m_NumTypes = Util.ReadValueU32(input);
                    m_NumMeshes = Util.ReadValueU32(input);

                    Unkn4_Offset = Util.ReadValueU32(input);
                    Unkn5_Offset = Util.ReadValueU32(input);
                    Unkn6_Offset = Util.ReadValueU32(input);
                    Unkn7_Offset = Util.ReadValueU32(input);
                    Unkn8_Offset = Util.ReadValueU32(input);
                }

                if (sobjver == 16) //don't really care that much, only supporting these for a friend
                {
                    Mesh_Offset = Util.ReadValueU32(input);
                    Entity_Offset = Util.ReadValueU32(input);
                    BatchTree_Offset = Util.ReadValueU32(input);
                    //discard, don't use any of it currently anyways
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);

                    m_NumEntities = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    m_NumMeshes = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);

                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                }
                if (sobjver == 13)
                {

                    Mesh_Offset = Util.ReadValueU32(input);
                    Entity_Offset = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    m_NumEntities = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    _ = Util.ReadValueU32(input);
                    m_NumMeshes = Util.ReadValueU32(input);
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

        public class AABB
        {
            public float Unknown { get; set; }
            public float m_Extents_min_x { get; set; }
            public float m_Extents_min_y { get; set; }
            public float m_Extents_min_z { get; set; }
            public float m_Extents_max_x { get; set; }
            public float m_Extents_max_y { get; set; }
            public float m_Extents_max_z { get; set; }

            public void Load(Stream input)
            {
                // Read all values in the order they appear in the stream
                Unknown = Util.ReadValueF32(input);
                m_Extents_min_x = Util.ReadValueF32(input);
                m_Extents_min_y = Util.ReadValueF32(input);
                m_Extents_min_z = Util.ReadValueF32(input);
                m_Extents_max_x = Util.ReadValueF32(input);
                m_Extents_max_y = Util.ReadValueF32(input);
                m_Extents_max_z = Util.ReadValueF32(input);
            }
        }

        public Flags flags { get; set; } = new Flags();
        public AABB aabb { get; set; } = new AABB();

        public void Deserialize(Stream input)
        {
            macicID = Util.ReadString(input, Encoding.ASCII, 4);

            if (macicID == "SO18")
            {
                flags.Load(input, 18);
            }
            if (macicID == "SO16")
            {
                flags.Load(input, 16);
            }
            if (macicID == "SO13")
            {
                flags.Load(input, 13);
            }
            aabb.Load(input);
        }
    }
}
