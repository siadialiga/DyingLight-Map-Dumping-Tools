using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class SCollisionNode
    {
        public aabb bbox;

        public uint right_child;
        public uint data;

        public void Deserialize(Stream input)
        {
            bbox = new aabb();
            bbox.Deserialize(input);

            right_child = Util.ReadValueU32(input);
            data = Util.ReadValueU32(input);
        }

        public void Serialize(Stream output)
        {
            bbox.Serialize(output);

            Util.WriteU32(output, right_child);
            Util.WriteU32(output, data);
        }

        public override string ToString()
        {
            return $"SCollisionNode:\n" +
                   $"  bbox: {bbox}\n" +
                   $"  right_child: {right_child}\n" +
                   $"  data: {data}";
        }
    }
}
