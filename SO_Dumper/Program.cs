using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
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
                string MagicID = Util.ReadString(input, Encoding.ASCII, 4);

                if (MagicID == "SO13")
                {
                    Console.WriteLine($"File: {inputfile}, MagicID: {MagicID} — Simple Object version 13 not supported.");
                }
                if (MagicID == "SO16")
                {
                    Console.WriteLine($"File: {inputfile}, MagicID: {MagicID} — Simple Object version 16 not supported.");
                }
                if (MagicID == "SO18")
                {
                    var offsets = new SObjFileOffsets();
                    offsets.Deserialize(input);

                    var SObjects = new CSimpleObjects();
                    SObjects.Deserialize(input);

                    for (uint i = 0; i < SObjects.m_Entities.Count(); i++)
                    {
                        Console.WriteLine(SObjects.m_Entities[i]);
                        Console.WriteLine(SObjects.m_TypesRender[SObjects.m_Entities[i].type]);
                        //Console.WriteLine(SObjects.m_TypesPreRender[SObjects.m_Entities[i].type]);
                    }



                    //Console.Write(SObjects.ToString());




                    //replace every mesh with forklift
                    /*
                    for (uint i = 0; i < SObjects.m_TypesRender.Count(); i++)
                    {
                        SObjects.m_TypesRender[i].Mesh = "forklift.msh";
                        SObjects.m_TypesRender[i].Skin = "Default";
                        SObjects.m_TypesRender[i].MeshElement = "forklift";

                        SObjects.m_TypesPreRender[i].lodcount = 0; //lod

                    }

                    using (var output = File.OpenWrite("OutTest.sobj"))
                    {
                        Util.WriteString(output, "SO18", Encoding.ASCII);
                        offsets.Serialize(output);
                        SObjects.Serialize(output);
                    }
                    */
                }
                }
        }
    }
}
