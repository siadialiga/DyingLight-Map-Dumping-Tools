using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class STreeNode
    {
        public struct Unsure
        {
            public float a;
            public float b;
            public float c;
        }


        /*
        struct __cppobj gatb::SNode // sizeof=0x1C
        {                                       // XREF: SObj::STreeNode/r
            vec3 origin;
            unsigned int data;
            vec3 span;
        };
        */
           
        //prob vec3 origin
        public Unsure SomeFloatStruct;

        //SPreRenderBatch
        public SPreRenderBatch m_PreRenderBatch;

        public uint Unknown;
        public ushort max_visibility_range;
        public ushort sum_of_entity_flags;

        public void Deserialize(Stream input)
        {
            SomeFloatStruct = new Unsure();
            SomeFloatStruct.a = Util.ReadFloat(input);
            SomeFloatStruct.b = Util.ReadFloat(input);
            SomeFloatStruct.c = Util.ReadFloat(input);

            m_PreRenderBatch = new SPreRenderBatch();
            m_PreRenderBatch.Deserialize(input);

            Unknown = Util.ReadValueU32(input); //always 0?

            max_visibility_range = Util.ReadValueU16(input);
            sum_of_entity_flags = Util.ReadValueU16(input);
        }
        public void Serialize(Stream output)
        {
            Util.WriteFloat(output, SomeFloatStruct.a);
            Util.WriteFloat(output, SomeFloatStruct.b);
            Util.WriteFloat(output, SomeFloatStruct.c);

            m_PreRenderBatch.Serialize(output);

            Util.WriteU32(output, Unknown);

            Util.WriteU16(output, max_visibility_range);
            Util.WriteU16(output, sum_of_entity_flags);
        }
        public override string ToString()
        {
            return $"STreeNode:\n" +
                   $"  SomeFloatStruct:\n" +
                   $"    a: {SomeFloatStruct.a}\n" +
                   $"    b: {SomeFloatStruct.b}\n" +
                   $"    c: {SomeFloatStruct.c}\n" +
                   $"  PreRenderBatch:\n{m_PreRenderBatch}\n" +
                   $"  Unknown: 0x{Unknown:X8}\n" +
                   $"  Max Visibility Range: {max_visibility_range}\n" +
                   $"  Sum of Entity Flags: 0x{sum_of_entity_flags:X4}";
        }

    }
}
