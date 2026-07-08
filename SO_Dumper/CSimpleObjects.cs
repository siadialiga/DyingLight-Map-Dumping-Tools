using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static SO_Dumper.SObj;
using static SO_Dumper.Util;

namespace SO_Dumper
{
    internal class CSimpleObjects
    {
        /*
         * Chrome Engine 5
        public uint m_NumEntitiesSum;
        public uint m_NumPreRenderBatches;
        public uint m_NumBatchTreeNodes;
        public uint m_NumTypes;
        public uint m_NumMeshes;
        public uint m_MeshDataSize;

        public void Deserialize(Stream input)
        {
            m_NumEntitiesSum = Util.ReadValueU32(input);
            m_NumPreRenderBatches = Util.ReadValueU32(input);
            m_NumBatchTreeNodes = Util.ReadValueU32(input);
            m_NumTypes = Util.ReadValueU32(input);
            m_NumMeshes = Util.ReadValueU32(input);
            m_MeshDataSize = Util.ReadValueU32(input);
        }
        public void Serialize(Stream output)
        {
            Util.WriteU32(output, m_NumEntitiesSum);
            Util.WriteU32(output, m_NumPreRenderBatches);
            Util.WriteU32(output, m_NumBatchTreeNodes);
            Util.WriteU32(output, m_NumTypes);
            Util.WriteU32(output, m_NumMeshes);
            Util.WriteU32(output, m_MeshDataSize);
        }
        */
        public uint m_NumEntitiesSum;
        public uint m_NumTypes;
        public uint m_NumMeshes;
        public uint[] m_NumCollisionNodes;
        public uint m_NumCollisionEntities;
        public float m_MaxVisibilityRange; 
        
        public extents m_Extents;
        public aabb m_AABB;

        public SType_Render[] m_TypesRender;
        public SEntity[] m_Entities;
        public STreeNode[] m_BatchTrees;
        public SType_PreRender[] m_TypesPreRender;
        public SCollisionNode[] m_CollisoinTree; //*m_CollisoinTree[4]
        public SCollisionEntity[] m_CollisionEntities;

        public void Deserialize(Stream input)
        {
            m_NumEntitiesSum = Util.ReadValueU32(input);
            m_NumTypes = Util.ReadValueU32(input);
            m_NumMeshes = Util.ReadValueU32(input);

            m_NumCollisionNodes = new uint[4];
            m_NumCollisionNodes[0] = Util.ReadValueU32(input);
            m_NumCollisionNodes[1] = Util.ReadValueU32(input);
            m_NumCollisionNodes[2] = Util.ReadValueU32(input);
            m_NumCollisionNodes[3] = Util.ReadValueU32(input);

            m_NumCollisionEntities = Util.ReadValueU32(input);
            m_MaxVisibilityRange = Util.ReadFloat(input);

            m_Extents = new extents();
            m_Extents.Deserialize(input);


            m_AABB = new aabb();
            //m_AABB.Deserialize(input);


            m_AABB.origin = new vec3
            {
                x = (m_Extents.min.x + m_Extents.max.x) * 0.5f,
                y = (m_Extents.min.y + m_Extents.max.y) * 0.5f,
                z = 0.5f * (m_Extents.min.z + m_Extents.max.z)
            };

            m_AABB.span = new vec3
            {
                x = m_Extents.max.x - m_AABB.origin.x,
                y = m_Extents.max.y - m_AABB.origin.y,
                z = m_Extents.max.z - m_AABB.origin.z,
            };

            m_TypesRender = new SType_Render[m_NumMeshes];
            m_Entities = new SEntity[m_NumEntitiesSum];
            m_BatchTrees = new STreeNode[m_NumTypes];
            m_TypesPreRender = new SType_PreRender[m_NumMeshes];
            m_CollisoinTree = new SCollisionNode[m_NumCollisionNodes[0] + m_NumCollisionNodes[1] + m_NumCollisionNodes[2] + m_NumCollisionNodes[3]];
            m_CollisionEntities = new SCollisionEntity[m_NumCollisionEntities];

            for (uint i = 0; i < m_TypesRender.Count(); i++)
            {
                m_TypesRender[i] = new SType_Render();
                m_TypesRender[i].Deserialize(input);
            }

            for (uint i = 0; i < m_Entities.Count(); i++)
            {
                m_Entities[i] = new SEntity();
                m_Entities[i].Deserialize(input);

                //Discard pading pretty sure
                _ = Util.ReadValueU32(input);
            }

            for (uint i = 0; i < m_BatchTrees.Count(); i++)
            {
                m_BatchTrees[i] = new STreeNode();
                m_BatchTrees[i].Deserialize(input);
            }
            //SType_PreRender[header.m_NumMeshes];

            for (uint i = 0; i < m_TypesPreRender.Count(); i++)
            {
                m_TypesPreRender[i] = new SType_PreRender();
                m_TypesPreRender[i].Deserialize(input);
            }


            //SCollisionNode[header.m_NumCollisionNodes[0] + header.m_NumCollisionNodes[1] + header.m_NumCollisionNodes[2] + header.m_NumCollisionNodes[3]];
            for (uint i = 0; i < m_CollisoinTree.Count(); i++)
            {
                m_CollisoinTree[i] = new SCollisionNode();
                m_CollisoinTree[i].Deserialize(input);
            }

            for (uint i = 0; i < m_CollisionEntities.Count(); i++)
            {
                m_CollisionEntities[i] = new SCollisionEntity();
                m_CollisionEntities[i].Deserialize(input);
            }
        }
        public void Serialize(Stream output)
        {
            Util.WriteU32(output, m_NumEntitiesSum);
            Util.WriteU32(output, m_NumTypes);
            Util.WriteU32(output, m_NumMeshes);
            Util.WriteU32(output, m_NumCollisionNodes[0]);
            Util.WriteU32(output, m_NumCollisionNodes[1]);
            Util.WriteU32(output, m_NumCollisionNodes[2]);
            Util.WriteU32(output, m_NumCollisionNodes[3]);
            Util.WriteU32(output, m_NumCollisionEntities);
            Util.WriteFloat(output, m_MaxVisibilityRange);

            m_Extents.Serialize(output);

            for (uint i = 0; i < m_NumMeshes; i++)
            {
                m_TypesRender[i].Serialize(output);
            }

            // SEntity
            for (uint i = 0; i < m_NumEntitiesSum; i++)
            {
                m_Entities[i].Serialize(output);
                Util.WriteU32(output, 0); // padding
            }

            // STreeNode
            for (uint i = 0; i < m_NumTypes; i++)
            {
                m_BatchTrees[i].Serialize(output);
            }

            // SType_PreRender
            for (uint i = 0; i < m_NumMeshes; i++)
            {
                m_TypesPreRender[i].Serialize(output);
            }

            // SCollisionNode
            uint numCollisionNodesTotal = m_NumCollisionNodes[0] + m_NumCollisionNodes[1] + m_NumCollisionNodes[2] + m_NumCollisionNodes[3];
            for (uint i = 0; i < numCollisionNodesTotal; i++)
            {
                m_CollisoinTree[i].Serialize(output);
            }

            // SCollisionEntity
            for (uint i = 0; i < m_NumCollisionEntities; i++)
            {
                m_CollisionEntities[i].Serialize(output);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== CSimpleObjects Data ===");
            sb.AppendLine($"NumEntitiesSum: {m_NumEntitiesSum}");
            sb.AppendLine($"NumTypes: {m_NumTypes}");
            sb.AppendLine($"NumMeshes: {m_NumMeshes}");
            sb.AppendLine();

            sb.AppendLine($"MaxVisibilityRange: {m_MaxVisibilityRange}");
            sb.AppendLine($"Extents: {m_Extents}");
            sb.AppendLine($"AABB: {m_AABB}");
            sb.AppendLine();


            if (m_TypesRender == null)
            {
                sb.AppendLine("No Types Render data.");
            }
            for (int i = 0; i < m_TypesRender.Length; i++)
            {
                sb.AppendLine($"Render Type {i}:");
                sb.AppendLine($"{m_TypesRender[i]}");
            }

            if (m_Entities == null)
            {
                sb.AppendLine("No Types Render data.");
            }
            for (int i = 0; i < m_Entities.Length; i++)
            {
                sb.AppendLine($"Entity {i}:");
                sb.AppendLine($"{m_Entities[i]}");
            }


            if (m_BatchTrees == null)
            {
                sb.AppendLine("No BatchTrees data.");
            }
            for (int i = 0; i < m_BatchTrees.Length; i++)
            {
                sb.AppendLine($"BatchTrees {i}:");
                sb.AppendLine($"{m_BatchTrees[i]}");
            }

            if (m_TypesPreRender == null)
            {
                sb.AppendLine("No Types PreRender data.");
            }
            for (int i = 0; i < m_TypesPreRender.Length; i++)
            {
                sb.AppendLine($"TypesPreRender {i}:");
                sb.AppendLine($"{m_TypesPreRender[i]}");
            }


            return sb.ToString();
        }
    }
}
