using System.IO;

namespace SO_Dumper
{
    internal class SType_PreRender
    {
        public float[] Origin = new float[3];
        public float radius2;
        public float[] span = new float[3];
        //Half UnkownA; //visibility_range2;??
        //hfloat UnkownB;
        public float UnknownA; //visibility_range2
        public float[] geom_lod_distance2 = new float[3];
        //might actually just be the first byte?? only 0-256 or whatever
        public uint lodcount;

        public void Deserialize(Stream input)
        {
            for (int i = 0; i < 3; i++)
                Origin[i] = Util.ReadFloat(input);

            radius2 = Util.ReadFloat(input);

            for (int i = 0; i < 3; i++)
                span[i] = Util.ReadFloat(input);

            UnknownA = Util.ReadFloat(input);

            for (int i = 0; i < 3; i++)
                geom_lod_distance2[i] = Util.ReadFloat(input);

            lodcount = Util.ReadValueU32(input);
        }

        public void Serialize(Stream output)
        {
            for (int i = 0; i < 3; i++)
                Util.WriteFloat(output, Origin[i]);

            Util.WriteFloat(output, radius2);

            for (int i = 0; i < 3; i++)
                Util.WriteFloat(output, span[i]);

            Util.WriteFloat(output, UnknownA);

            for (int i = 0; i < 3; i++)
                Util.WriteFloat(output, geom_lod_distance2[i]);

            Util.WriteU32(output, lodcount);
        }

        public override string ToString()
        {
            return $"SType_PreRender:\n" +
                   $"  Origin: [{string.Join(", ", Origin)}]\n" +
                   $"  radius2: {radius2}\n" +
                   $"  span: [{string.Join(", ", span)}]\n" +
                   $"  UnknownA: {UnknownA}\n" +
                   $"  geom_lod_distance2: [{string.Join(", ", geom_lod_distance2)}]" +
                   $"  lodcount: {lodcount}\n";
        }
    }
}
