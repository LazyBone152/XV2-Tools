using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xv2CoreLib.Havok;
using YAXLib;

namespace Xv2CoreLib.FMP
{
    [YAXSerializeAs("CollisionGroup")]
    public class FMP_CollisionGroup : IInstallable
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXDontSerialize]
        public int SortID => Index;

        string IInstallable.Index
        {
            get => Index.ToString();
            set => Index = int.Parse(value);
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Collider")]
        public List<FMP_Collider> Colliders { get; set; } = new List<FMP_Collider>();

        public List<FMP_Collider> UnorderedHitboxList = new List<FMP_Collider>();

        [YAXDontSerialize]
        public int ColliderCount => GetColliderCount(Colliders);

        #region Read/Write
        internal static void WriteAll(List<FMP_CollisionGroup> collisionGroups, List<byte> bytes, List<StringWriter.StringInfo> stringWriter, bool oldVersion)
        {
            int hitboxGroupStart = bytes.Count;

            //Write HitboxGroup
            for (int i = 0; i < collisionGroups.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { StringToWrite = collisionGroups[i].Name, Offset = bytes.Count });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(collisionGroups[i].UnorderedHitboxList?.Count ?? 0));
                bytes.AddRange(new byte[4]);
            }

            //Write Hitboxes, per group

            for (int i = 0; i < collisionGroups.Count; i++)
            {
                if ((collisionGroups[i].UnorderedHitboxList?.Count ?? 0) == 0) continue;
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxGroupStart + 8 + (i * 12));

                int hitboxStart = bytes.Count;

                for (int a = 0; a < collisionGroups[i].UnorderedHitboxList.Count; a++)
                {
                    FMP_Collider hitbox = collisionGroups[i].UnorderedHitboxList[a];

                    if (!string.IsNullOrWhiteSpace(hitbox.Name))
                        stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = hitbox.Name });

                    bytes.AddRange(BitConverter.GetBytes((int)-1));
                    bytes.AddRange(BitConverter.GetBytes(hitbox.ChildIdx));
                    bytes.AddRange(BitConverter.GetBytes(hitbox.SiblingIdx));
                    bytes.AddRange(new byte[4]); //DestructionList count, easier to fill in later as the lists are only created when written to binary
                    bytes.AddRange(new byte[4]);

                    if (!oldVersion)
                        bytes.AddRange(new byte[4]); //HVK file offset, only in newer versions

                    bytes.AddRange(BitConverter.GetBytes(hitbox.CollisionVertexData?.Vertices?.Count ?? 0));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(hitbox.CollisionVertexData?.Faces?.Length ?? 0));
                    bytes.AddRange(new byte[4]);
                }

                //Write sub data
                for (int a = 0; a < collisionGroups[i].UnorderedHitboxList.Count; a++)
                {
                    FMP_Collider hitbox = collisionGroups[i].UnorderedHitboxList[a];
                    int hitboxSize = oldVersion ? 36 : 40;

                    //Destruction Groups
                    if (hitbox.HavokColliders != null)
                    {
                        List<List<FMP_Shape>> destructionGroups = FMP_Shape.CreateHavokGroups(hitbox.HavokColliders);

                        if (destructionGroups.Count > 0)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(destructionGroups.Count), hitboxStart + 12 + (hitboxSize * a));
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + 16 + (hitboxSize * a));
                            FMP_Shape.Write(destructionGroups, bytes);
                        }
                    }

                    //Vertices
                    if (hitbox.CollisionVertexData?.Vertices?.Count > 0)
                    {
                        int verticesOffset = oldVersion ? 24 : 28;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + verticesOffset + (hitboxSize * a));

                        foreach (var vertex in hitbox.CollisionVertexData?.Vertices)
                        {
                            if (vertex.Pos.Length != 3) throw new Exception("Vertex array length not correct - must have 3 position values (XYZ)");
                            if (vertex.Normal.Length != 3) throw new Exception("Vertex array length not correct - must have 3 normal values (XYZ)");
                            bytes.AddRange(BitConverter_Ex.GetBytes(vertex.Pos));
                            bytes.AddRange(BitConverter_Ex.GetBytes(vertex.Normal));
                        }
                    }

                    //Face indices
                    if (hitbox.CollisionVertexData?.Vertices?.Count > 0)
                    {
                        int indicesOffset = oldVersion ? 32 : 36;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + indicesOffset + (hitboxSize * a));

                        foreach (var face in hitbox.CollisionVertexData.Faces)
                        {
                            bytes.AddRange(BitConverter.GetBytes((ushort)face));
                        }

                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                    }

                    //HVK file
                    if (hitbox.HvkFile?.Length > 0 && !oldVersion)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + 20 + (hitboxSize * a));

                        bytes.AddRange(BitConverter.GetBytes(1));
                        bytes.AddRange(BitConverter.GetBytes(hitbox.HvkFile.Length));
                        bytes.AddRange(BitConverter.GetBytes(bytes.Count + 4));

                        bytes.AddRange(hitbox.HvkFile);
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                    }
                }
            }
        }

        public static List<FMP_CollisionGroup> ReadAll(byte[] bytes, int offset, int count, bool oldVersion)
        {
            List<FMP_CollisionGroup> hitboxes = new List<FMP_CollisionGroup>();

            for (int i = 0; i < count; i++)
            {
                hitboxes.Add(Read(bytes, offset + (12 * i), i, oldVersion));
            }

            return hitboxes;
        }

        public static FMP_CollisionGroup Read(byte[] bytes, int offset, int index, bool oldVersion)
        {
            FMP_CollisionGroup hitboxGroup = new FMP_CollisionGroup();
            hitboxGroup.Index = index;
            hitboxGroup.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));

            int hitboxCount = BitConverter.ToInt32(bytes, offset + 4);
            int hitboxOffset = BitConverter.ToInt32(bytes, offset + 8);

            for (int i = 0; i < hitboxCount; i++)
            {
                hitboxGroup.UnorderedHitboxList.Add(FMP_Collider.Read(bytes, hitboxOffset + (i * 40), i, oldVersion));
            }

            hitboxGroup.Colliders = CreateHitboxTree(hitboxGroup.UnorderedHitboxList, 0);

            return hitboxGroup;
        }

        private static List<FMP_Collider> CreateHitboxTree(List<FMP_Collider> hitboxes, int index)
        {
            List<FMP_Collider> hitboxTree = new List<FMP_Collider>();

            int next = index;

            while (next != -1)
            {
                if (next == -1 || next >= hitboxes.Count)
                    throw new ArgumentException($"FMP_HitboxGroup.CreateHitboxTree: Hitbox index was out of range");

                hitboxTree.Add(hitboxes[next]);

                if (hitboxes[next].ChildIdx != -1)
                    hitboxes[next].Colliders.AddRange(CreateHitboxTree(hitboxes, hitboxes[next].ChildIdx));

                next = hitboxes[next].SiblingIdx;
            }

            return hitboxTree;
        }

        private static List<FMP_Collider> WriteHitboxTree(List<FMP_Collider> hitboxTree)
        {
            List<FMP_Collider> hitboxes = new List<FMP_Collider>();
            WriteHitboxTreeRecursive(hitboxes, hitboxTree);
            return hitboxes;
        }

        internal void CreateUnorderedHitboxList()
        {
            UnorderedHitboxList = WriteHitboxTree(Colliders);
        }

        private static void WriteHitboxTreeRecursive(List<FMP_Collider> hitboxes, List<FMP_Collider> hitboxTree)
        {
            for (int i = 0; i < hitboxTree.Count; i++)
            {
                hitboxTree[i].Index = hitboxes.Count;
                hitboxes.Add(hitboxTree[i]);

                if (hitboxTree[i].Colliders?.Count > 0)
                {
                    hitboxTree[i].ChildIdx = hitboxes.Count;
                    WriteHitboxTreeRecursive(hitboxes, hitboxTree[i].Colliders);
                }
                else
                {
                    hitboxTree[i].ChildIdx = -1;
                }

                hitboxTree[i].SiblingIdx = (i == hitboxTree.Count - 1) ? -1 : hitboxes.Count;
            }
        }
        #endregion

        private static int GetColliderCount(List<FMP_Collider> hitboxTree)
        {
            int count = 0;

            foreach (var hitbox in hitboxTree)
            {
                count++;

                if (hitbox.Colliders?.Count > 0)
                    count += GetColliderCount(hitbox.Colliders);
            }

            return count;
        }

        public bool HasHavokCollisionData()
        {
            if (UnorderedHitboxList == null || UnorderedHitboxList?.Count == 0)
                CreateUnorderedHitboxList();

            foreach (var collider in UnorderedHitboxList)
            {
                if (collider.HavokColliders == null) continue;

                if (collider.HvkFile?.Length > 0) return true;

                foreach (var havok in collider.HavokColliders)
                {
                    if (havok.HvkFile?.Length > 0)
                        return true;
                }
            }

            return false;
        }
    }

    [YAXSerializeAs("Collider")]
    public class FMP_Collider
    {
        [YAXDontSerialize]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXDontSerialize]
        public int ChildIdx { get; set; }
        [YAXDontSerialize]
        public int SiblingIdx { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement)]
        [YAXDontSerializeIfNull]
        public List<FMP_Shape> HavokColliders { get; set; } = new List<FMP_Shape>();

        [YAXDontSerializeIfNull]
        public FMP_CollisionVertexData CollisionVertexData { get; set; } //Seems to be unused by the game. Maybe leftover editor data?

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        [YAXDontSerializeIfNull]
        public byte[] HvkFile { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Collider")]
        public List<FMP_Collider> Colliders { get; set; } = new List<FMP_Collider>();

        public static FMP_Collider Read(byte[] bytes, int offset, int index, bool oldVersion)
        {
            FMP_Collider hitbox = new FMP_Collider();

            int nameOffset = BitConverter.ToInt32(bytes, offset);
            hitbox.Index = index;
            hitbox.Name = nameOffset != -1 ? StringEx.GetString(bytes, nameOffset) : null;
            hitbox.ChildIdx = BitConverter.ToInt32(bytes, offset + 4);
            hitbox.SiblingIdx = BitConverter.ToInt32(bytes, offset + 8);

            int shapesCount = BitConverter.ToInt32(bytes, offset + 12);
            int shepesOffset = BitConverter.ToInt32(bytes, offset + 16);
            int hvkGeometryOffset = 0;
            int vertexDataCount = 0;
            int vertexDataOffset = 0;
            int faceIndicesCount = 0;
            int faceIndicesOffset = 0;

            if (oldVersion)
            {
                vertexDataCount = BitConverter.ToInt32(bytes, offset + 20);
                vertexDataOffset = BitConverter.ToInt32(bytes, offset + 24);
                faceIndicesCount = BitConverter.ToInt32(bytes, offset + 28);
                faceIndicesOffset = BitConverter.ToInt32(bytes, offset + 32);
            }
            else
            {
                hvkGeometryOffset = BitConverter.ToInt32(bytes, offset + 20);
                vertexDataCount = BitConverter.ToInt32(bytes, offset + 24);
                vertexDataOffset = BitConverter.ToInt32(bytes, offset + 28);
                faceIndicesCount = BitConverter.ToInt32(bytes, offset + 32);
                faceIndicesOffset = BitConverter.ToInt32(bytes, offset + 36);
            }

            hitbox.HavokColliders = FMP_Shape.ReadAll(bytes, shepesOffset, shapesCount);
            hitbox.CollisionVertexData = vertexDataCount > 0 || faceIndicesCount > 0 ? FMP_CollisionVertexData.Read(bytes, vertexDataOffset, vertexDataCount, faceIndicesOffset, faceIndicesCount) : null;

            if (hvkGeometryOffset > 0 && !oldVersion)
            {
                //This is actually a count of Havok files, followed by size / offset pairs (8 bytes * Count). However, only the first one matters, so we wont bother handling the rest of them.
                //The count is always 1 in vanilla FMP files
                int hvkCount = BitConverter.ToInt32(bytes, hvkGeometryOffset); 

                if(hvkCount > 0)
                {
                    //Only loading the first one
                    int hvkSize = BitConverter.ToInt32(bytes, hvkGeometryOffset + 4);
                    int hvkOffset = BitConverter.ToInt32(bytes, hvkGeometryOffset + 8);

                    hitbox.HvkFile = hvkOffset > 0 ? bytes.GetRange(hvkOffset, hvkSize) : null;
                }
            }

            return hitbox;
        }

        public override string ToString()
        {
            return $"sibling: {SiblingIdx}, child: {ChildIdx}";
        }
    }

    [YAXSerializeAs("HavokGroupParameters")]
    public class FMP_HavokGroupParameters
    {
        [YAXAttributeForClass]
        public int Param1 { get; set; }
        [YAXAttributeForClass]
        public int Param2 { get; set; }

        public static List<FMP_HavokGroupParameters> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_HavokGroupParameters> indices = new List<FMP_HavokGroupParameters>();

            for (int i = 0; i < count; i++)
            {
                indices.Add(new FMP_HavokGroupParameters()
                {
                    Param1 = BitConverter.ToInt32(bytes, offset + (8 * i)),
                    Param2 = BitConverter.ToInt32(bytes, offset + 4 + (8 * i))
                });
            }

            return indices;
        }
    }

    [YAXSerializeAs("Shape")]
    [YAXAltAliases("Havok")]
    public class FMP_Shape
    {
        [YAXAttributeForClass]
        public int Group { get; set; }
        [CustomSerialize]
        public int FragmentGroup { get; set; }
        [CustomSerialize(isHex: true)]
        [YAXAltAliases("I_08/value")]
        [YAXShouldSerialize(nameof(ShouldSerializeFlags))]
        public int Flags { get; set; } //Zero when PrimitveShape is used. For Havok, defines the expected class type. With no PrimitiveShape and no Havok, this is an index count for an embedded mesh.
        [CustomSerialize]
        public int I_28 { get; set; }
        [CustomSerialize]
        public int I_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_36/value")]
        public float Margin { get; set; } = 0.01f;

        [YAXDontSerializeIfNull]
        [YAXAltAliases("SubPart1")]
        public FMP_PrimitiveShape PrimitiveShape { get; set; }

        #region LegacyCompatibility
        [YAXDontSerializeIfNull]
        public FMP_PrimitiveShape SubPart2
        {
            get => null;
            set
            {
                if(value != null && PrimitiveShape != null)
                    PrimitiveShape.HasDummyPrimitive = true;
            }
        }
        #endregion

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        [YAXDontSerializeIfNull]
        public byte[] HvkFile { get; set; }

        //To allow embedded XMLs for easier testing; TODO remove after, this doesn't need to stay
        [YAXDontSerializeIfNull]
        public HavokTagFile HavokFile { get; set; }

        public static List<FMP_Shape> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Shape> shapes = new List<FMP_Shape>();

            for (int i = 0; i < count; i++)
            {
                int destCount = BitConverter.ToInt32(bytes, offset + (i * 8));
                int destOffset = BitConverter.ToInt32(bytes, offset + 4 + (i * 8));

                for (int a = 0; a < destCount; a++)
                {
                    shapes.Add(Read(bytes, destOffset + (a * 40), i));
                }
            }

            return shapes;
        }

        public static FMP_Shape Read(byte[] bytes, int offset, int group)
        {
            FMP_Shape havok = new FMP_Shape();

            havok.Group = group;
            havok.FragmentGroup = BitConverter.ToInt32(bytes, offset);
            havok.Flags = BitConverter.ToInt32(bytes, offset + 8);
            havok.I_28 = BitConverter.ToInt32(bytes, offset + 28);
            havok.I_32 = BitConverter.ToInt32(bytes, offset + 32);
            havok.Margin = BitConverter.ToSingle(bytes, offset + 36);

            int primitiveShapeOffset = BitConverter.ToInt32(bytes, offset + 4);
            int primitiveShape2Offset = BitConverter.ToInt32(bytes, offset + 24);

            //Havok file is ignored by the game if a primitive shape exists
            if(primitiveShapeOffset == 0)
            {
                int hvkFileSize = BitConverter.ToInt32(bytes, offset + 16);
                int hvkFileOffset = BitConverter.ToInt32(bytes, offset + 20);

                havok.HvkFile = bytes.GetRange(hvkFileOffset, hvkFileSize);
                //havok.HavokFile = HavokTagFile.Load(bytes.GetRange(hvkFileOffset, hvkFileSize));
            }
            else if (primitiveShapeOffset > 0)
            {
                havok.PrimitiveShape = FMP_PrimitiveShape.Read(bytes, primitiveShapeOffset);
                havok.PrimitiveShape.HasDummyPrimitive = primitiveShape2Offset > 0;
            }

            //Verify that the indices offset is zero. The parser does not support these older types of FMP files (when FmpFlags.UseHavok is not set).
            if (BitConverter.ToInt32(bytes, offset + 12) > 0)
                throw new NotSupportedException($"FMP_Havok: Indices offset is not zero");
            
            return havok;
        }

        internal static void Write(List<List<FMP_Shape>> groups, List<byte> bytes)
        {
            int listStart = bytes.Count;

            //Write DestructionList, in sequence
            for (int i = 0; i < groups.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(groups[i].Count));
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < groups.Count; i++)
            {
                //Write Desruction entries, in sequence
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), listStart + 4 + (i * 8));
                int destStart = bytes.Count;

                for (int a = 0; a < groups[i].Count; a++)
                {
                    FMP_Shape destruction = groups[i][a];

                    //TODO: Remove after testing
                    if (destruction.HavokFile != null)
                    {
                        destruction.HavokFile.ResolveReferences();
                        destruction.HvkFile = destruction.HavokFile.Write();
                    }

                    bytes.AddRange(BitConverter.GetBytes((uint)destruction.FragmentGroup));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(destruction.Flags));
                    bytes.AddRange(new byte[4]); //Unk offset
                    bytes.AddRange(BitConverter.GetBytes(destruction.HvkFile?.Length ?? 0));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_28));
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_32));
                    bytes.AddRange(BitConverter.GetBytes(destruction.Margin));
                }

                //Write sub data, per Destruction
                for (int a = 0; a < groups[i].Count; a++)
                {
                    FMP_Shape shape = groups[i][a];

                    if (shape.PrimitiveShape != null)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 4 + (a * 40));
                        bytes.AddRange(shape.PrimitiveShape.Write());

                        if (shape.PrimitiveShape.HasDummyPrimitive)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 24 + (a * 40));
                            bytes.AddRange(shape.PrimitiveShape.Write());
                        }
                    }

                    if (shape.HvkFile?.Length > 0)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 20 + (a * 40));
                        bytes.AddRange(shape.HvkFile);
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                    }
                }

            }
        }

        internal static List<List<FMP_Shape>> CreateHavokGroups(List<FMP_Shape> havokEntries)
        {
            var groupValues = havokEntries.OrderBy(x => x.Group).GroupBy(x => x.Group).ToList();

            List<List<FMP_Shape>> groups = new List<List<FMP_Shape>>();
            foreach (var group in groupValues)
            {
                List<FMP_Shape> havokGroup = havokEntries.Where(x => x.Group == group.Key).ToList();

                foreach (var _group in groups)
                {
                    foreach (var _havok in havokGroup)
                    {
                        if (_group.Contains(_havok))
                        {
                            throw new Exception("FMP_Havok.CreateHavokGroups: Attempted to add a havok entry to the list a second time");
                        }
                    }
                }

                groups.Add(havokGroup);
            }

            return groups;
        }
    
        private bool ShouldSerializeFlags()
        {
            return PrimitiveShape == null || Flags != 0;
        }
    }

    [YAXSerializeAs("PrimitiveShape")]
    public class FMP_PrimitiveShape
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public bool HasDummyPrimitive { get; set; }

        [CustomSerialize]
        [YAXAltAliases("I_00/value")]
        public ColliderShapeType Type { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_04/value")]
        public float CenterX { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_08/value")]
        public float CenterY { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_12/value")]
        public float CenterZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Yaw { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Pitch { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Roll { get; set; }
        [CustomSerialize(isFloat: true, parent: "Scale", serializeAs: "X")]
        public float ScaleX { get; set; } //F_28
        [CustomSerialize(isFloat: true, parent: "Scale", serializeAs: "Y")]
        public float ScaleY { get; set; } //F_32
        [CustomSerialize(isFloat: true, parent: "Scale", serializeAs: "Z")]
        public float ScaleZ { get; set; } //F_36
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_48 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_52 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes((int)Type));
            bytes.AddRange(BitConverter.GetBytes(CenterX));
            bytes.AddRange(BitConverter.GetBytes(CenterY));
            bytes.AddRange(BitConverter.GetBytes(CenterZ));
            bytes.AddRange(BitConverter.GetBytes(Yaw));
            bytes.AddRange(BitConverter.GetBytes(Pitch));
            bytes.AddRange(BitConverter.GetBytes(Roll));
            bytes.AddRange(BitConverter.GetBytes(ScaleX));
            bytes.AddRange(BitConverter.GetBytes(ScaleY));
            bytes.AddRange(BitConverter.GetBytes(ScaleZ));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));

            if (bytes.Count != 56)
                throw new Exception("FMP_DestructionSubPart: Invalid size");

            return bytes.ToArray();
        }

        public static FMP_PrimitiveShape Read(byte[] bytes, int offset)
        {
            return new FMP_PrimitiveShape()
            {
                Type = (ColliderShapeType)BitConverter.ToInt32(bytes, offset),
                CenterX = BitConverter.ToSingle(bytes, offset + 4),
                CenterY = BitConverter.ToSingle(bytes, offset + 8),
                CenterZ = BitConverter.ToSingle(bytes, offset + 12),
                Yaw = BitConverter.ToSingle(bytes, offset + 16),
                Pitch = BitConverter.ToSingle(bytes, offset + 20),
                Roll = BitConverter.ToSingle(bytes, offset + 24),
                ScaleX = BitConverter.ToSingle(bytes, offset + 28),
                ScaleY = BitConverter.ToSingle(bytes, offset + 32),
                ScaleZ = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52)
            };
        }

        public bool Compare(FMP_PrimitiveShape other)
        {
            if(other == null) return false;
            
            return Type == other.Type &&
                   CenterX == other.CenterX &&
                   CenterY == other.CenterY &&
                   CenterZ == other.CenterZ &&
                   Yaw == other.Yaw &&
                   Pitch == other.Pitch &&
                   Roll == other.Roll &&
                   ScaleX == other.ScaleX &&
                   ScaleY == other.ScaleY &&
                   ScaleZ == other.ScaleZ &&
                   F_40 == other.F_40 &&
                   F_44 == other.F_44 &&
                   F_48 == other.F_48 &&
                   F_52 == other.F_52;
        }
    }

    [YAXSerializeAs("VertexData")]
    public class FMP_CollisionVertexData
    {
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "idx")]
        public ushort[] Faces { get; set; }

        public List<FMP_Vertex> Vertices { get; set; } = new List<FMP_Vertex>();

        public static FMP_CollisionVertexData Read(byte[] bytes, int vertexOffset, int vertexCount, int faceIndicesOffset, int faceIndicesCount)
        {
            FMP_CollisionVertexData data = new FMP_CollisionVertexData();

            data.Faces = BitConverter_Ex.ToUInt16Array(bytes, faceIndicesOffset, faceIndicesCount);

            for (int i = 0; i < vertexCount; i++)
            {
                data.Vertices.Add(new FMP_Vertex()
                {
                    Pos = BitConverter_Ex.ToFloat32Array(bytes, vertexOffset + (i * 24), 3),
                    Normal = BitConverter_Ex.ToFloat32Array(bytes, vertexOffset + 12 + (i * 24), 3)
                });
            }

            return data;
        }

        public bool HasData()
        {
            return Vertices?.Count > 0 && Faces?.Length > 0;
        }

    }

    [YAXSerializeAs("Vertex")]
    public class FMP_Vertex
    {
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public float[] Pos { get; set; } //3
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public float[] Normal { get; set; } //3

        public FMP_Vertex() { }

        public FMP_Vertex(Vector3 pos)
        {
            Pos = new float[3] { pos.X, pos.Y, pos.Z };
            Normal = new float[3];
        }

        public Vector3 GetPositionVector()
        {
            return new Vector3(Pos[0], Pos[1], Pos[2]);
        }

        public void SetPositionVector(Vector3 pos)
        {
            if (Pos?.Length != 3)
                Pos = new float[3];

            Pos[0] = pos.X;
            Pos[1] = pos.Y;
            Pos[2] = pos.Z;
        }
    }

}
