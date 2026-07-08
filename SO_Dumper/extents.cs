using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SO_Dumper.Util;

namespace SO_Dumper
{
    internal class extents
    {
        public vec3 min;
        public vec3 max;
        public void Deserialize(Stream input)
        {
            min = new vec3
            {
                x = Util.ReadFloat(input),
                y = Util.ReadFloat(input),
                z = Util.ReadFloat(input)
            };

            max = new vec3
            {
                x = Util.ReadFloat(input),
                y = Util.ReadFloat(input),
                z = Util.ReadFloat(input)
            };
        }
        public void Serialize(Stream output)
        {
            Util.WriteFloat(output, min.x);
            Util.WriteFloat(output, min.y);
            Util.WriteFloat(output, min.z);

            Util.WriteFloat(output, max.x);
            Util.WriteFloat(output, max.y);
            Util.WriteFloat(output, max.z);

        }
        public override string ToString()
        {
            return $"Min: {min}, Max: {max}";
        }
    }
}
