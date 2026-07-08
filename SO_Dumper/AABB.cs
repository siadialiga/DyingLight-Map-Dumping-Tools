using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SO_Dumper.Util;

namespace SO_Dumper
{
    internal class aabb
    {
        public vec3 origin;
        public vec3 span;

        public void Deserialize(Stream input)
        {
            origin = new vec3
            {
                x = Util.ReadFloat(input),
                y = Util.ReadFloat(input),
                z = Util.ReadFloat(input)
            };

            span = new vec3
            {
                x = Util.ReadFloat(input),
                y = Util.ReadFloat(input),
                z = Util.ReadFloat(input)
            };
        }
        public void Serialize(Stream output)
        {
            Util.WriteFloat(output, origin.x);
            Util.WriteFloat(output, origin.y);
            Util.WriteFloat(output, origin.z);

            Util.WriteFloat(output, span.x);
            Util.WriteFloat(output, span.y);
            Util.WriteFloat(output, span.z);
        }
        public override string ToString()
        {
            return $"Origin: {origin}, Span: {span}";
        }
    }
}
