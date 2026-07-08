using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SO18_Dumper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> arguments = new List<string>(args);

            if (arguments.Count < 1 || arguments.Count > 2)
            {
                Console.WriteLine("Usage: {0} input_file.sobj", System.AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine();
                return;
            }

            string inputfile = arguments[0];

            using (var input = File.OpenRead(inputfile))
            {
                var header = new Header();
                header.Deserialize(input);


                var MeshHeaders = new MeshHeader[header.flags.m_NumMeshes];
                for (uint i = 0; i < header.flags.m_NumMeshes; i++)
                {
                    MeshHeaders[i] = new MeshHeader();
                    if (header.macicID == "SO16")
                        MeshHeaders[i].Deserialize(input, 16);
                    else if (header.macicID == "SO13")
                        MeshHeaders[i].Deserialize(input, 13);
                    else
                        MeshHeaders[i].Deserialize(input, 18);
                }

                var EntityHeaders = new EntityHeader[header.flags.m_NumEntities];
                for (uint i = 0; i < header.flags.m_NumEntities; i++)
                {
                    EntityHeaders[i] = new EntityHeader();
                    EntityHeaders[i].Deserialize(input);
                }

                foreach (EntityHeader entity in EntityHeaders)
                {
                    Vector3 rotation = Util.ToZYXEulerAngles(entity.EQuaternion);

                    Console.WriteLine("Class = ModelObject");
                    Console.WriteLine("Position = <" + entity.Position.X + ", " + entity.Position.Y + ", " + entity.Position.Z + ">");
                    Console.WriteLine("Rotation = <" + rotation.X + ", " + rotation.Y + ", " + rotation.Z + ">");
                    Console.WriteLine("Scale = <" + entity.Scale.X + ", " + entity.Scale.Y + ", " + entity.Scale.Z + ">");
                    Console.WriteLine("MeshName = " + MeshHeaders[entity.Type].MeshName);
                    Console.WriteLine("SkinName = " + MeshHeaders[entity.Type].SkinName);

                    //reformat BGRA -> RGBA
                    Console.WriteLine("Color0 = <" + entity.Color0.R + ", " + entity.Color0.G + ", " + entity.Color0.B + ", " + entity.Color0.A + ">");
                    Console.WriteLine("Color1 = <" + entity.Color1.R + ", " + entity.Color1.G + ", " + entity.Color1.B + ", " + entity.Color1.A + ">");

                    Console.WriteLine("Seed = " + MeshHeaders[entity.Type].RandomSeed); //might have issues?? random seed is taken from meshheader but I thought it would be in entity. good enough for most
                    Console.WriteLine("required_tags = " + MeshHeaders[entity.Type].required_tags);
                    Console.WriteLine("forbidden_tags = " + MeshHeaders[entity.Type].forbidden_tags);

                    Console.Write("\n");//4 next entity
                }
            }
        }
    }
}
