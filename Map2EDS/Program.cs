using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MisParse;
using static Util;

namespace Map2EDS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> arguments = new List<string>(args);

            if (arguments.Count < 2 || arguments.Count > 3)
            {
                Console.WriteLine("Usage:{0} Input.txt Output.eds", System.AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine();
                return;
            }

            string inputfile = arguments[0];
            string outputfile = arguments[1];

            using (var input = File.OpenRead(inputfile))
            {
                var mis = new MisParse();

                mis.Deserialize(input);

                using (var output = File.OpenWrite(outputfile))
                {
                    //start of header
                    //most likely not a real magic id but it's close enough
                    byte[] MagicBytes = new byte[] { 0x02, 0x00 };
                    Util.WriteBytes(output, MagicBytes);

                    //group name, will be overwritten in editor to file name after being resaved or edited
                    Util.WriteString(output, "dummy_box#00001", true, true);

                    byte[] unknownA = new byte[] { 0xE4, 0x00, 0x04, 0x00, 0x00, 0x00, 0x1E, 0x00, 0x00, 0x00, 0x09, 0x00, 0x30, 0x00, 0x00, 0x00 };
                    Util.WriteBytes(output, unknownA);

                    var HeaderTransform = new Util.EntityTransformMatrix();
                    HeaderTransform.m11 = 1;
                    HeaderTransform.m22 = 1;
                    HeaderTransform.m33 = 1;

                    Util.WriteTransform(output, HeaderTransform);

                    byte[] unknownB = new byte[] { 0x0A, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    Util.WriteBytes(output, unknownB);



                    foreach (Entity entity in mis.Entitys)
                    {
                        //start of object section
                        byte[] SectionBytes = new byte[] { 0x01, 0x00 };
                        Util.WriteBytes(output, SectionBytes);


                        using (var tempfile = new MemoryStream())
                        {
                            byte[] unknownC = new byte[] { 0x77, 0x00, 0x0D, 0x00, 0x00, 0x00 };
                            Util.WriteBytes(tempfile, unknownC);
                            
                            Util.WriteString(tempfile, entity.Class);

                            //Too lazy to reverse enginer object flags
                            byte[] unknownD = new byte[] { 0x3D, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x40, 0x00, 0x00, 0x5A, 0x14, 0x18, 0x00, 0x00, 0x00, 0x90, 0x01, 0x00, 0x00, 0x2C, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x13, 0x04, 0x1A, 0x00, 0x00, 0x00, 0x80, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC4, 0x00, 0x48, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                            Util.WriteBytes(tempfile, unknownD);


                            Util.WriteRGBA(tempfile, entity.Color0); // mesh color 0
                            Util.WriteRGBA(tempfile, entity.Color1); // mesh color 1

                            Util.WriteRGBA(tempfile, entity.Color0); // mesh color 0
                            Util.WriteRGBA(tempfile, entity.Color1); // mesh color 1

                            byte[] unknownE = new byte[] { 0xCB, 0x00, 0x14, 0x00,  0x00, 0x00 };
                            Util.WriteBytes(tempfile, unknownE);


                            //skin tags
                            Util.WriteInt64(tempfile, entity.Required_Tags);
                            Util.WriteInt64(tempfile, entity.Forbidden_Tags);
                            Util.WriteInt(tempfile, entity.Seed);

                            Util.WriteBytes(tempfile, new byte[] { 0xDF, 0x00, 0x30, 0x00,  0x00, 0x00 });


                            Util.EntityTransformMatrix EntityTransform = Util.CreateTransformMatrix(entity.Rotation, entity.Scale);

                            EntityTransform.posX = entity.Position.X;
                            EntityTransform.posY = entity.Position.Y;
                            EntityTransform.posZ = entity.Position.Z;
                            Util.WriteTransform(tempfile, EntityTransform);

                            byte[] unknownF = new byte[] { 0xBE, 0x00, 0x08, 0x00, 0x00, 0x00, 0x6E, 0xA3, 0x0B, 0x40, 0x01, 0x00, 0x00, 0x00, 0x14, 0x00, 0x04, 0x00, 0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x16, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x79, 0x00};
                            Util.WriteBytes(tempfile, unknownF);

                            //This is some of the worst things I've ever written, If you read this. Im Sorry
                            Util.WriteInt(tempfile, 1 + 2 + 8 + 6 + entity.MeshName.Length + 1 + 2 + 8 + 4 + ((entity.SkinName != null) ? (2 + entity.SkinName.Length) : 2) + 1);

                            byte[] characterbruh = new byte[] { 0x0C };
                            Util.WriteBytes(tempfile, characterbruh);

                            //I don't get why it does this, must be for parsing or something
                            Util.WriteString(tempfile, "MeshName");

                            //write size of the short value holding the string length (2 bytes) and the length of the complete string
                            //I think if this is -1 the parser will skip this section and assume no mesh??
                            Util.WriteInt(tempfile, entity.MeshName.Length + 2);
                            Util.WriteString(tempfile, entity.MeshName);

                            if (entity.SkinName != null)
                            {
                                //prob should just get the size of "SkinName", but relistically I should have just made a function to write both
                                byte[] SkinNameSizeTotal = new byte[] { 0x0C }; //10, short + size of "SkinName"
                                Util.WriteBytes(tempfile, SkinNameSizeTotal);

                                Util.WriteString(tempfile, "SkinName");

                                Util.WriteInt(tempfile, 2 + entity.SkinName.Length);
                                Util.WriteString(tempfile, entity.SkinName);
                            }

                            //Had a lot of issues with the end data so I just spit all this out.
                            byte[] unknownH = new byte[] { 0xFF, 0x7C, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xF7, 0x00, 0x01 };
                            Util.WriteBytes(tempfile, unknownH);

                            Util.WriteInt(output, (int)tempfile.Length + 4);

                            //wrute the entity to file
                            Util.WriteBytes(output, tempfile.ToArray());

                            //write the little section at the end of the entity section, Really should rewrite this program
                            byte[] endofsec = new byte[] { 0x00, 0x00, 0x00, 0x00 };
                            Util.WriteBytes(output, endofsec);
                        }
                    }

                    //real end of file
                    byte[] unknownI = new byte[] { 0x05, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 };
                    Util.WriteBytes(output, unknownI);
                }
            }
        }
    }
}
