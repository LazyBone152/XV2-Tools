using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.FMP
{
    [YAXSerializeAs("InstanceData")]
    public class FMP_InstanceData
    {
        public List<FMP_InstanceGroup> InstanceGroups { get; set; }
        public FMP_InstanceBVHNode BVHRoot { get; set; }

        public void Write(List<byte> bytes)
        {
            List<FMP_InstanceTransform> transforms = GetNodeTransforms();
            int nodeCount = InstanceGroups?.Count ?? 0;
            int start = bytes.Count;

            bytes.AddRange(BitConverter.GetBytes(transforms.Count));
            bytes.AddRange(BitConverter.GetBytes(nodeCount));
            bytes.AddRange(BitConverter.GetBytes(nodeCount > 0 ? start + 20 : 0)); //Directly after FMP_Hierarchy
            bytes.AddRange(new byte[8]); //HierarchyNode and NodeTransform offset

            for (int i = 0; i < nodeCount; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].CenterX));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].CenterY));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].CenterZ));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MaxDistanceFromCenter));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MinX));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MinY));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MinZ));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MaxX));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MaxY));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].MaxZ));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].Transforms?.Count > 0 ? InstanceGroups[i].Transforms.Count : 0));
                bytes.AddRange(BitConverter.GetBytes(InstanceGroups[i].NodeTransformIndex));
            }

            //NodeTransforms
            if (transforms.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), start + 16);

                for (int i = 0; i < transforms.Count; i++)
                {
                    bytes.AddRange(transforms[i].Write());
                }
            }

            //HierarchyNode
            if (BVHRoot != null)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), start + 12);
                BVHRoot.Write(bytes);
            }
        }

        private List<FMP_InstanceTransform> GetNodeTransforms()
        {
            List<FMP_InstanceTransform> nodes = new List<FMP_InstanceTransform>();

            foreach (var node in InstanceGroups)
            {
                if (node.Transforms?.Count > 0)
                {
                    node.NodeTransformIndex = nodes.Count;
                    nodes.AddRange(node.Transforms);
                }
                else
                {
                    node.NodeTransformIndex = -1;
                }
            }

            return nodes;
        }

        public static FMP_InstanceData Read(byte[] bytes, int offset)
        {
            int nodeTransformCount = BitConverter.ToInt32(bytes, offset);
            int nodeCount = BitConverter.ToInt32(bytes, offset + 4);
            int nodeOffset = BitConverter.ToInt32(bytes, offset + 8);
            int hierarchyNodeOffset = BitConverter.ToInt32(bytes, offset + 12);
            int nodeTransformOffset = BitConverter.ToInt32(bytes, offset + 16);

            return new FMP_InstanceData()
            {
                BVHRoot = FMP_InstanceBVHNode.Read(bytes, hierarchyNodeOffset),
                InstanceGroups = FMP_InstanceGroup.ReadAll(bytes, nodeOffset, nodeCount, nodeTransformOffset)
            };
        }

        public IEnumerable<FMP_InstanceTransform> GetTransforms()
        {
            foreach (FMP_InstanceGroup instanceGroup in InstanceGroups)
            {
                foreach (FMP_InstanceTransform transform in instanceGroup.Transforms)
                    yield return transform;
            }
        }
    }

    [YAXSerializeAs("InstanceGroup")]
    public class FMP_InstanceGroup
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXComment("These values represent the bounding box for the children InstanceTransforms (used for frustum culling)")]
        [CustomSerialize(isFloat: true, serializeAs: "X", parent: "Center")]
        public float CenterX { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Y", parent: "Center")]
        public float CenterY { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Z", parent: "Center")]
        public float CenterZ { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "MaxDistance", parent: "Center")]
        public float MaxDistanceFromCenter { get; set; } //Max distance from center to min/max point on any axis
        [CustomSerialize(isFloat: true, serializeAs: "X", parent: "Min")]
        public float MinX { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Y", parent: "Min")]
        public float MinY { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Z", parent: "Min")]
        public float MinZ { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "X", parent: "Max")]
        public float MaxX { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Y", parent: "Max")]
        public float MaxY { get; set; }
        [CustomSerialize(isFloat: true, serializeAs: "Z", parent: "Max")]
        public float MaxZ { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "InstanceTransform")]
        public List<FMP_InstanceTransform> Transforms { get; set; }

        internal int NodeTransformIndex { get; set; }

        public static List<FMP_InstanceGroup> ReadAll(byte[] bytes, int offset, int count, int nodeTransformsOffset)
        {
            List<FMP_InstanceGroup> entries = new List<FMP_InstanceGroup>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (48 * i), nodeTransformsOffset, i));
            }

            return entries;
        }

        public static FMP_InstanceGroup Read(byte[] bytes, int offset, int nodeTransformsOffset, int index)
        {
            FMP_InstanceGroup node = new FMP_InstanceGroup();
            node.Index = index;
            node.CenterX = BitConverter.ToSingle(bytes, offset);
            node.CenterY = BitConverter.ToSingle(bytes, offset + 4);
            node.CenterZ = BitConverter.ToSingle(bytes, offset + 8);
            node.MaxDistanceFromCenter = BitConverter.ToSingle(bytes, offset + 12);
            node.MinX = BitConverter.ToSingle(bytes, offset + 16);
            node.MinY = BitConverter.ToSingle(bytes, offset + 20);
            node.MinZ = BitConverter.ToSingle(bytes, offset + 24);
            node.MaxX = BitConverter.ToSingle(bytes, offset + 28);
            node.MaxY = BitConverter.ToSingle(bytes, offset + 32);
            node.MaxZ = BitConverter.ToSingle(bytes, offset + 36);

            int transformCount = BitConverter.ToInt32(bytes, offset + 40);
            int transformOffset = nodeTransformsOffset + (FMP_InstanceTransform.SIZE * BitConverter.ToInt32(bytes, offset + 44));

            node.Transforms = FMP_InstanceTransform.ReadAll(bytes, transformOffset, transformCount);
            return node;
        }

    }

    [YAXSerializeAs("InstanceTransfor")]
    public class FMP_InstanceTransform
    {
        internal const int SIZE = 36;

        [CustomSerialize(isFloat: true)]
        public float PositionX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float PositionY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float PositionZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleZ { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(36);

            bytes.AddRange(BitConverter.GetBytes(PositionX));
            bytes.AddRange(BitConverter.GetBytes(PositionY));
            bytes.AddRange(BitConverter.GetBytes(PositionZ));

            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationX)));
            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationY)));
            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationZ)));

            bytes.AddRange(BitConverter.GetBytes(ScaleX));
            bytes.AddRange(BitConverter.GetBytes(ScaleY));
            bytes.AddRange(BitConverter.GetBytes(ScaleZ));

            if (bytes.Count != SIZE)
                throw new Exception("FMP_NodeTransform.Write: Incorrect size!");

            return bytes.ToArray();
        }

        public static List<FMP_InstanceTransform> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_InstanceTransform> entries = new List<FMP_InstanceTransform>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (SIZE * i)));
            }

            return entries;
        }

        public static FMP_InstanceTransform Read(byte[] bytes, int offset)
        {
            return new FMP_InstanceTransform()
            {
                PositionX = BitConverter.ToSingle(bytes, offset),
                PositionY = BitConverter.ToSingle(bytes, offset + 4),
                PositionZ = BitConverter.ToSingle(bytes, offset + 8),
                RotationX = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 12)),
                RotationY = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 16)),
                RotationZ = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 20)),
                ScaleX = BitConverter.ToSingle(bytes, offset + 24),
                ScaleY = BitConverter.ToSingle(bytes, offset + 28),
                ScaleZ = BitConverter.ToSingle(bytes, offset + 32),
            };
        }

        public Matrix4x4 ToMatrix()
        {
            Matrix4x4 matrix = Matrix4x4.CreateScale(ScaleX, ScaleY, ScaleZ);
            matrix *= Matrix4x4.CreateFromQuaternion(MathHelpers.EulerToQuaternion(new Vector3(RotationX, RotationY, RotationZ)));
            matrix *= Matrix4x4.CreateTranslation(PositionX, PositionY, PositionZ);
            return matrix;
        }
    }

    [YAXSerializeAs("BVH_Node")]
    public class FMP_InstanceBVHNode
    {
        [YAXAttributeForClass]
        [YAXHexValue]
        public byte Flags { get; set; }
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        [CustomSerialize]
        public ushort[] GroupIndices { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        [CustomSerialize]
        public float[] Center { get; set; } //size 3 array

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BVH_Node")]
        [YAXDontSerializeIfNull]
        public List<FMP_InstanceBVHNode> BVHNodes { get; set; }

        public void Write(List<byte> bytes)
        {
            byte isLeaf = (byte)(GroupIndices?.Length > 0 ? 1 : 0);

            bytes.Add(isLeaf);
            bytes.Add(Flags);
            int indicesCount = GroupIndices?.Length ?? 0;
            bytes.AddRange(BitConverter.GetBytes((ushort)indicesCount));

            if (isLeaf == 0)
            {
                //Has children
                float[] values = Center;

                if (values?.Length != 3)
                {
                    values = new float[3];
                    //throw new Exception("FMP_HierarchyNode: Invalid number of Values.");
                }


                bytes.AddRange(BitConverter.GetBytes(values[0]));
                bytes.AddRange(BitConverter.GetBytes(values[1]));
                bytes.AddRange(BitConverter.GetBytes(values[2]));
                int childrenOffsets = bytes.Count;
                bytes.AddRange(new byte[4 * 8]); //Offsets for children

                if (BVHNodes.Count > 8)
                {
                    Console.WriteLine($"WARNING: A InstanceTreeNode cannot have more than 8 InstanceTreeNodes as children.");
                }

                for (int i = 0; i < 8; i++)
                {
                    FMP_InstanceBVHNode child = BVHNodes.FirstOrDefault(x => x.Index == i);

                    if (child != null)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), childrenOffsets + (4 * i));
                        child.Write(bytes);
                    }
                }
            }
            else if (isLeaf == 1)
            {
                for (int i = 0; i < indicesCount; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(GroupIndices[i]));
                }

                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
            }
            else
            {
                throw new Exception("FMP_HierarchyNode: Type is invalid value");
            }

        }

        public static FMP_InstanceBVHNode Read(byte[] bytes, int offset, int index = 0)
        {
            FMP_InstanceBVHNode node = new FMP_InstanceBVHNode();
            byte isLeaf = bytes[offset];
            node.Flags = bytes[offset + 1];
            node.Index = index;

            if (isLeaf == 0)
            {
                //Has children
                node.Center = BitConverter_Ex.ToFloat32Array(bytes, offset + 4, 3);
                int[] offsets = BitConverter_Ex.ToInt32Array(bytes, offset + 16, 8);
                node.BVHNodes = new List<FMP_InstanceBVHNode>();

                for (int i = 0; i < offsets.Length; i++)
                {
                    if (offsets[i] != 0 && offsets[i] != offset)
                    {
                        node.BVHNodes.Add(Read(bytes, offsets[i], i));
                    }
                }
            }
            else if (isLeaf == 1)
            {
                //No children, instead has indices (of a FMP_Node)
                int indicesCount = BitConverter.ToUInt16(bytes, offset + 2);
                node.GroupIndices = BitConverter_Ex.ToUInt16Array(bytes, offset + 4, indicesCount);
            }

            return node;
        }

    }

}
