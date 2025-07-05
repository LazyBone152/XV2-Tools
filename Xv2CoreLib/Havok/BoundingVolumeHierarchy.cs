using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Xv2CoreLib.Havok
{
    internal static class BoundingVolumeHierarchy
    {
        internal static BVH_Node[] CreateBVH(HavokCollisionMesh mesh)
        {
            //Create the triangle AABB nodes (leaf nodes)
            List<TriangleNode> triangleNodes = new List<TriangleNode>();

            for(int i = 0; i < mesh.Indices.Length; i += 3)
            {
                mesh.CalculateFaceAABB(out Vector3 min, out Vector3 max, mesh.Indices[i], mesh.Indices[i + 1], mesh.Indices[i + 2]);
                TriangleNode node = new TriangleNode();
                node.SetAabb(min, max);
                node.TriangleIndex = triangleNodes.Count;
                triangleNodes.Add(node);
            }

            //Group nodes together based on distance, into groups of up to 4, and then repeat this step untill all nodes are grouped together (one parent node)
            List<BVH_Node> nodes = GroupTriangles(triangleNodes); //This is slow

            while (nodes.Count > 1)
            {
                nodes = GroupNodes(nodes);
            }

            //Flatten node stack
            List<BVH_Node> flatNodes = new List<BVH_Node>();
            FlattenNodes(nodes[0], flatNodes);

            return flatNodes.ToArray();
        }

        private static void FlattenNodes(BVH_Node node, List<BVH_Node> flatNodes)
        {
            flatNodes.Add(node);

            //Nodes appear to be written in reverse order in the havok files, so lets emulate that
            for (int i = 3; i >= 0; i--)
            {
                if (node.ChildNodes[i] == null) continue;

                node.Indices[i] = flatNodes.Count;
                FlattenNodes(node.ChildNodes[i], flatNodes);
            }
        }

        private static List<BVH_Node> GroupTriangles(List<TriangleNode> triangleNodes)
        {
            HashSet<int> alreadyGrouped = new HashSet<int>();
            List<BVH_Node> nodes = new List<BVH_Node>();
            List<DistanceSort> distances = new List<DistanceSort>(triangleNodes.Count);

            for(int i = 0; i < triangleNodes.Count; i++)
            {
                if (alreadyGrouped.Contains(i)) continue;

                //Calculate all distances for all nodes to this node
                CalculateDistances(distances, triangleNodes, triangleNodes[i], alreadyGrouped);

                TriangleNode[] group = new TriangleNode[4];
                group[0] = triangleNodes[i];
                int amountGrouped = 1;

                //Find the three triangles closest to this one and add them to the group
                foreach(DistanceSort distance in distances.OrderBy(x => x.Distance).Take(4))
                {
                    if (amountGrouped == 4) break;
                    if (alreadyGrouped.Contains(distance.Index) || distance.Index == triangleNodes[i].TriangleIndex) continue;

                    group[amountGrouped] = triangleNodes[distance.Index];
                    amountGrouped++;
                }

                //Create the BVH node
                BVH_Node bvhNode = BVH_Node.Create();

                for (int a = 0; a < amountGrouped; a++)
                {
                    alreadyGrouped.Add(group[a].TriangleIndex);
                    bvhNode.AddAabb(group[a].AabbMin, group[a].AabbMax, group[a].TriangleIndex, true, a);
                }

                bvhNode.CalculateCentroid();
                nodes.Add(bvhNode);
            }

            return nodes;
        }

        private static List<BVH_Node> GroupNodes(List<BVH_Node> inputNodes)
        {
            HashSet<BVH_Node> alreadyGrouped = new HashSet<BVH_Node>();
            List<BVH_Node> outputNodes = new List<BVH_Node>();
            List<DistanceSort> distances = new List<DistanceSort>(inputNodes.Count);

            for(int i = 0; i < inputNodes.Count; i++)
            {
                if (alreadyGrouped.Contains(inputNodes[i])) continue;

                //Calculate all distances for all nodes to this node
                CalculateDistances(distances, inputNodes, inputNodes[i], alreadyGrouped);

                BVH_Node[] group = new BVH_Node[4];
                group[0] = inputNodes[i];
                int amountGrouped = 1;

                //Find the three nodes closest to this one and add them to the group
                foreach (DistanceSort distance in distances.OrderBy(x => x.Distance).Take(4))
                {
                    if (amountGrouped == 4) break;
                    if (alreadyGrouped.Contains(inputNodes[distance.Index]) || distance.Index == i) continue;

                    group[amountGrouped] = inputNodes[distance.Index];
                    alreadyGrouped.Add(inputNodes[distance.Index]);
                    amountGrouped++;
                }

                //Create the BVH node
                BVH_Node bvhNode = BVH_Node.Create();

                for (int a = 0; a < amountGrouped; a++)
                {
                    group[a].CalculateNodeAabb(out Vector3 min, out Vector3 max);
                    bvhNode.AddNode(min, max, group[a], false, a);
                }

                bvhNode.CalculateCentroid();
                outputNodes.Add(bvhNode);
            }

            return outputNodes;
        }

        private static void CalculateDistances(List<DistanceSort> distances, List<TriangleNode> nodes, TriangleNode node, HashSet<int> alreadyGrouped)
        {
            distances.Clear();

            for(int i = 0; i < nodes.Count; i++)
            {
                //Skip calculating distance for nodes that have already been grouped
                if (!alreadyGrouped.Contains(i))
                {
                    distances.Add(new DistanceSort()
                    {
                        Distance = Vector3.Distance(nodes[i].Centroid, node.Centroid),
                        Index = i
                    });
                }
            }
        }

        private static void CalculateDistances(List<DistanceSort> distances, List<BVH_Node> nodes, BVH_Node node, HashSet<BVH_Node> alreadyGrouped)
        {
            distances.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                //Skip calculating distance for nodes that have already been grouped
                if (!alreadyGrouped.Contains(nodes[i]))
                {
                    distances.Add(new DistanceSort()
                    {
                        Distance = Vector3.Distance(nodes[i].Centroid, node.Centroid),
                        Index = i
                    });
                }
            }
        }
    }

    internal struct DistanceSort
    {
        internal int Index;
        internal float Distance;
    }

    internal struct TriangleNode
    {
        internal Vector3 AabbMin;
        internal Vector3 AabbMax;
        internal Vector3 Centroid;
        internal int TriangleIndex;

        internal void SetAabb(Vector3 min, Vector3 max)
        {
            AabbMin = min;
            AabbMax = max;
            Centroid = BVH_Node.CalculateCentroid(min, max);
        }
    }

    internal class BVH_Node
    {
        internal Vector3[] AabbMin;
        internal Vector3[] AabbMax;
        internal Vector3 Centroid;
        internal int[] Indices;
        internal BVH_Node[] ChildNodes;
        internal bool isLeaf;

        internal static BVH_Node Create()
        {
            return new BVH_Node()
            {
                AabbMin = new Vector3[4] { new Vector3(float.PositiveInfinity), new Vector3(float.PositiveInfinity), new Vector3(float.PositiveInfinity), new Vector3(float.PositiveInfinity) },
                AabbMax = new Vector3[4] { new Vector3(float.NegativeInfinity), new Vector3(float.NegativeInfinity), new Vector3(float.NegativeInfinity), new Vector3(float.NegativeInfinity) },
                Indices = new int[4] { -1, -1, -1, -1 },
                ChildNodes = new BVH_Node[4]
            };
        }

        internal void AddAabb(Vector3 min, Vector3 max, int index, bool isLeaf, int atIndex)
        {
            AabbMin[atIndex] = min;
            AabbMax[atIndex] = max;
            Indices[atIndex] = index;
            this.isLeaf = isLeaf;
        }

        internal void AddNode(Vector3 min, Vector3 max, BVH_Node node, bool isLeaf, int atIndex)
        {
            AabbMin[atIndex] = min;
            AabbMax[atIndex] = max;
            ChildNodes[atIndex] = node;
            this.isLeaf = isLeaf;
        }

        internal void CalculateCentroid()
        {
            Vector3 sum = Vector3.Zero;
            int numNodes = 0;

            for(int i = 0; i < AabbMax.Length; i++)
            {
                if (Indices[i] == -1 && ChildNodes[i] == null) continue;

                sum += CalculateCentroid(AabbMin[i], AabbMax[i]);
                numNodes++;
            }

            Centroid = sum / numNodes;
        }

        internal static Vector3 CalculateCentroid(Vector3 min, Vector3 max)
        {
            return (min + max) * 0.5f;
        }
    
        internal void CalculateNodeAabb(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.PositiveInfinity);
            max = new Vector3(float.NegativeInfinity);

            for(int i = 0; i < 4; i++)
            {
                if (Indices[i] == -1 && ChildNodes[i] == null) break;

                min = Vector3.Min(min, AabbMin[i]);
                max = Vector3.Max(max, AabbMax[i]);
            }
        }
    }
}