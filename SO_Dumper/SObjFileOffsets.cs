using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class SObjFileOffsets
    {


        public uint types;
        public uint entities;
        public uint batches;
        public uint Unkn1_Offset;
        public uint Unkn2_Offset;
        public uint Unkn3_Offset;

        /*
        //chrome engine 5
        public uint types;
        public uint entities;
        public uint batches;
        public uint batch_trees;
        public uint prerender_types;
        public uint collision_trees;
        public uint collision_entities;
        public uint spu_mesh_data;
        public uint spu_meshes;
        */

        public void Deserialize(Stream input)
        {
            types = Util.ReadValueU32(input);
            entities = Util.ReadValueU32(input);
            batches = Util.ReadValueU32(input);
            Unkn1_Offset = Util.ReadValueU32(input);
            Unkn2_Offset = Util.ReadValueU32(input);
            Unkn3_Offset = Util.ReadValueU32(input);
        }
        public void Serialize(Stream output)
        {
            Util.WriteU32(output, types);
            Util.WriteU32(output, entities);
            Util.WriteU32(output, batches);
            Util.WriteU32(output, Unkn1_Offset);
            Util.WriteU32(output, Unkn2_Offset);
            Util.WriteU32(output, Unkn3_Offset);
        }
    }
}
