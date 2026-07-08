using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO_Dumper
{
    internal class SPreRenderBatch
    {
        //SPreRenderBatch
        ushort batch_tree_index;
        ushort num_batch_tree_nodes;
        ushort entity_index;
        ushort num_entities;
        ushort type_index;
        ushort num_types;

        public void Deserialize(Stream input)
        {
            batch_tree_index = Util.ReadValueU16(input);
            num_batch_tree_nodes = Util.ReadValueU16(input);
            entity_index = Util.ReadValueU16(input);
            num_entities = Util.ReadValueU16(input);
            type_index = Util.ReadValueU16(input);
            num_types = Util.ReadValueU16(input);
        }
        public void Serialize(Stream output)
        {
            Util.WriteU16(output, batch_tree_index);
            Util.WriteU16(output, num_batch_tree_nodes);
            Util.WriteU16(output, entity_index);
            Util.WriteU16(output, num_entities);
            Util.WriteU16(output, type_index);
            Util.WriteU16(output, num_types);
        }
        public override string ToString()
        {
            return $"    Batch Tree Index: {batch_tree_index}\n" +
                   $"    Num Batch Tree Nodes: {num_batch_tree_nodes}\n" +
                   $"    Entity Index: {entity_index}\n" +
                   $"    Num Entities: {num_entities}\n" +
                   $"    Type Index: {type_index}\n" +
                   $"    Num Types: {num_types}";
        }

    }
}
