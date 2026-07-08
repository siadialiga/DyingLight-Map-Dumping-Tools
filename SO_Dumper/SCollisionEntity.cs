using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SO_Dumper
{
    internal class SCollisionEntity
    {
        public aabb aabb;
        public uint type_index;
        public uint flags;
        public uint entity_index;

        public void Deserialize(Stream input)
        {
            aabb = new aabb();
            aabb.Deserialize(input);

            type_index = Util.ReadValueU32(input);
            flags = Util.ReadValueU32(input);
            entity_index = Util.ReadValueU32(input);
        }

        public void Serialize(Stream output)
        {
            aabb.Serialize(output);

            Util.WriteU32(output, type_index);
            Util.WriteU32(output, flags);
            Util.WriteU32(output, entity_index);
        }

        public override string ToString()
        {
            return $"SCollisionEntity:\n" +
                   $"  aabb: {aabb}\n" +
                   $"  type_index: {type_index}\n" +
                   $"  flags: {flags}\n" +
                   $"  entity_index: {entity_index}";
        }
    }
}