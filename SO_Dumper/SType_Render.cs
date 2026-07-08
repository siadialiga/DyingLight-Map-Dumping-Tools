using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class SType_Render
    {
        public string Mesh;
        public string Skin;

        public Int64 required_tags;
        public Int64 forbidden_tags;

        public int seed;

        //public uint collision_category_bits;
        public uint collision_collide_bits;

        public uint UnknownA;
        public string MeshElement;
        public ushort UnknownB;
        public void Deserialize(Stream input)
        {
            Mesh = Util.ReadStringChrome(input, Encoding.ASCII);
            Skin = Util.ReadStringChrome(input, Encoding.ASCII);

            required_tags = Util.ReadValueS64(input);
            forbidden_tags = Util.ReadValueS64(input);

            seed = Util.ReadValueS32(input);

            //collision_category_bits = Util.ReadValueU32(input); //CE5 only?
            collision_collide_bits = Util.ReadValueU32(input);



            //may technically be a seperate section or smt
            UnknownA = Util.ReadValueU32(input);
            MeshElement = Util.ReadStringChrome(input, Encoding.ASCII);

            //Short Could be somehow related to mesh_spu
            UnknownB = Util.ReadValueU16(input);
        }
        public void Serialize(Stream output)
        {
            Util.WriteStringChrome(output, Mesh, Encoding.ASCII);
            Util.WriteStringChrome(output, Skin, Encoding.ASCII);

            Util.WriteS64(output, required_tags);
            Util.WriteS64(output, forbidden_tags);

            Util.WriteS32(output, seed);

            //Util.WriteU32(output, collision_category_bits);
            Util.WriteU32(output, collision_collide_bits);




            //other section??
            Util.WriteU32(output, UnknownA);
            Util.WriteStringChrome(output, MeshElement, Encoding.ASCII);
            Util.WriteU16(output, UnknownB);
        }
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"  Mesh: {Mesh}");
            sb.AppendLine($"  Skin: {Skin}");
            sb.AppendLine($"  Required Tags: {required_tags}");
            sb.AppendLine($"  Forbidden Tags: {forbidden_tags}");
            sb.AppendLine($"  Seed: {seed}");
            //sb.AppendLine($"  Collision Category Bits: {collision_category_bits}");
            sb.AppendLine($"  Collision Collide Bits: {collision_collide_bits}");

            return sb.ToString();
        }
    }
}
