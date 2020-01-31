using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.EMD
{
    [Flags]
    public enum VTX_FLAGS : Int32
    {
        EMD_VTX_FLAG_POS = 0x1,
        EMD_VTX_FLAG_NORM = 0x2,
        EMD_VTX_FLAG_TEX = 0x4,
        EMD_VTX_FLAG_TEX2 = 0x8,
        EMD_VTX_FLAG_COLOR = 0x40,
        EMD_VTX_FLAG_TANGENT = 0x80,
        EMD_VTX_FLAG_BLEND_WEIGHT = 0x200,
        EMD_VTX_FLAG_COMPRESSED_FORMAT = 0x8000
    }

    public class EMD_File
    {
        public const int EMD_SIGNATURE = 1145914659;

        [YAXAttributeForClass]
        public uint Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Model")]
        public List<EMD_Model> Models { get; set; }


        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static EMD_File Load(byte[] bytes)
        {
            return new Parser(bytes).emdFile;
        }

        public static EMD_File Load(string path)
        {
            return new Parser(path, false).emdFile;
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }
    }

    [YAXSerializeAs("Model")]
    public class EMD_Model
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ushort I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Mesh")]
        public List<EMD_Mesh> Meshes { get; set; }
    }

    [YAXSerializeAs("Mesh")]
    public class EMD_Mesh
    {
        [YAXDontSerialize]
        public string UniqueName { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("X")]
        public float F_00 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Y")]
        public float F_04 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Z")]
        public float F_08 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("W")]
        public float F_12 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("X")]
        public float F_16 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Y")]
        public float F_20 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Z")]
        public float F_24 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("W")]
        public float F_28 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("X")]
        public float F_32 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Y")]
        public float F_36 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Z")]
        public float F_40 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("W")]
        public float F_44 { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        public List<EMD_Submesh> Submeshes { get; set; }

        [YAXDontSerialize]
        public int VertexCount
        {
            get
            {
                int count = 0;
                foreach (var submesh in Submeshes)
                {
                    count += submesh.VertexCount;
                }
                return count;
            }
        }
    }

    [YAXSerializeAs("Submesh")]
    public class EMD_Submesh
    {
        [YAXDontSerialize]
        public string UniqueName { get; set; }

        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("X")]
        public float F_00 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Y")]
        public float F_04 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Z")]
        public float F_08 { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("W")]
        public float F_12 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("X")]
        public float F_16 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Y")]
        public float F_20 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Z")]
        public float F_24 { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("W")]
        public float F_28 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("X")]
        public float F_32 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Y")]
        public float F_36 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Z")]
        public float F_40 { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("W")]
        public float F_44 { get; set; }
        [YAXAttributeFor("VtxFlags")]
        [YAXSerializeAs("values")]
        public VTX_FLAGS VtxFlags { get; set; }

        [YAXDontSerialize]
        public int VertexSize
        {
            get
            {
                return EMD_Vertex.GetVertexSize(VtxFlags);
            }
        }

        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXAttributeForClass]
        public string RootBone
        {
            get
            {
                return GetRootBone();
            }
        }

        //Debug
        [YAXDontSerialize]
        public int VertexCount
        {
            get
            {
                if (Vertexes != null) return Vertexes.Count;
                return 0;
            }
        }
        [YAXDontSerialize]
        public int TextureDefinitionCount
        {
            get
            {
                if (TextureDefinitions != null) return TextureDefinitions.Count;
                return 0;
            }
        }
        [YAXDontSerialize]
        public int TriangleCount
        {
            get
            {
                int count = 0;
                if (Triangles != null)
                {
                    foreach (var triangleList in Triangles)
                    {
                        count += triangleList.FaceCount;
                    }
                }
                return count;
            }
        }
        [YAXDontSerialize]
        public int TriangleListCount
        {
            get
            {
                return (Triangles != null) ? Triangles.Count : 0;
            }
        }


        public List<EMD_TextureDefinition> TextureDefinitions { get; set; }
        public List<EMD_Triangle> Triangles { get; set; }
        public List<EMD_Vertex> Vertexes { get; set; }

        private string GetRootBone()
        {
            List<string> usedBones = new List<string>();


            return null;
        }

        public string GetBoneName(int vertexIdx, int boneIdx)
        {
            for (int i = 0; i < Triangles.Count; i++)
            {
                foreach (var vertex in Triangles[i].Faces)
                {
                    if (vertex == vertexIdx)
                    {
                        return Triangles[i].Bones[boneIdx];
                    }
                }
            }

            throw new InvalidDataException(String.Format("Could not get the bone name for boneIndex: {0} on vertex: {1}", boneIdx, vertexIdx));
        }
    }

    [YAXSerializeAs("TextureDefinition")]
    public class EMD_TextureDefinition
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag0")]
        public byte I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextureIndex")]
        public byte I_01 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag1")]
        public byte I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag2")]
        public byte I_03 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scale_U")]
        public float F_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scale_V")]
        public float F_08 { get; set; }
    }

    [YAXSerializeAs("Triangle")]
    public class EMD_Triangle
    {
        //Debug
        [YAXDontSerialize]
        public int FaceCount_Debug { get; set; }
        [YAXDontSerialize]
        public int FaceCountDivided_Debug { get; set; }

        [YAXDontSerialize]
        public int FaceCount
        {
            get
            {
                if (Faces != null) return Faces.Count;
                return 0;
            }
        }
        [YAXDontSerialize]
        public int BonesCount
        {
            get
            {
                if (Bones != null) return Bones.Count;
                return 0;
            }
        }

        public List<ushort> Faces { get; set; }
        public List<string> Bones { get; set; }
    }

    [YAXSerializeAs("Vertex")]
    public class EMD_Vertex
    {
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        public float PositionX { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        public float PositionY { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        public float PositionZ { get; set; }


        [YAXAttributeFor("Normal")]
        [YAXSerializeAs("X")]
        public float NormalX { get; set; }
        [YAXAttributeFor("Normal")]
        [YAXSerializeAs("Y")]
        public float NormalY { get; set; }
        [YAXAttributeFor("Normal")]
        [YAXSerializeAs("Z")]
        public float NormalZ { get; set; }


        [YAXAttributeFor("Tangent")]
        [YAXSerializeAs("X")]
        public float TangentX { get; set; }
        [YAXAttributeFor("Tangent")]
        [YAXSerializeAs("Y")]
        public float TangentY { get; set; }
        [YAXAttributeFor("Tangent")]
        [YAXSerializeAs("Z")]
        public float TangentZ { get; set; }


        [YAXAttributeFor("Texture1")]
        [YAXSerializeAs("U")]
        public float TextureU { get; set; }
        [YAXAttributeFor("Texture1")]
        [YAXSerializeAs("V")]
        public float TextureV { get; set; }


        [YAXAttributeFor("Texture2")]
        [YAXSerializeAs("U")]
        public float Texture2U { get; set; }
        [YAXAttributeFor("Texture2")]
        [YAXSerializeAs("V")]
        public float Texture2V { get; set; }


        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        public byte ColorR { get; set; } = 255;
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        public byte ColorG { get; set; } = 255;
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
        public byte ColorB { get; set; } = 255;
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("A")]
        public byte ColorA { get; set; } = 255;


        [YAXAttributeFor("BlendIndexes")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public byte[] BlendIndexes { get; set; } //Size 4
        [YAXAttributeFor("BlendWeights")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] BlendWeights { get; set; } //Size 3 (if float16, then 2 bytes of padding after)

        public static int GetVertexSize(VTX_FLAGS flags)
        {
            int size = 0;

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_POS))
            {
                size += 12;
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_NORM))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    size += 8;
                }
                else
                {
                    size += 12;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TANGENT))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    size += 8;
                }
                else
                {
                    size += 12;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    size += 4;
                }
                else
                {
                    size += 8;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX2))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    size += 4;
                }
                else
                {
                    size += 8;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COLOR))
            {
                size += 4;
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_BLEND_WEIGHT))
            {
                size += 4;

                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    size += 8;
                }
                else
                {
                    size += 16;
                }
            }

            return size;
        }

        public List<byte> GetBytes(VTX_FLAGS flags)
        {
            List<byte> bytes = new List<byte>();
            int size = 0;

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_POS))
            {
                bytes.AddRange(BitConverter.GetBytes(PositionX));
                bytes.AddRange(BitConverter.GetBytes(PositionY));
                bytes.AddRange(BitConverter.GetBytes(PositionZ));
                size += 12;
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_NORM))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    bytes.AddRange(Half.GetBytes((Half)NormalX));
                    bytes.AddRange(Half.GetBytes((Half)NormalY));
                    bytes.AddRange(Half.GetBytes((Half)NormalZ));
                    bytes.AddRange(new byte[2]);
                    size += 8;
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(NormalX));
                    bytes.AddRange(BitConverter.GetBytes(NormalY));
                    bytes.AddRange(BitConverter.GetBytes(NormalZ));
                    size += 12;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TANGENT))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    bytes.AddRange(Half.GetBytes((Half)TangentX));
                    bytes.AddRange(Half.GetBytes((Half)TangentY));
                    bytes.AddRange(Half.GetBytes((Half)TangentZ));
                    bytes.AddRange(new byte[2]);
                    size += 8;
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(TangentX));
                    bytes.AddRange(BitConverter.GetBytes(TangentY));
                    bytes.AddRange(BitConverter.GetBytes(TangentZ));
                    size += 12;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    bytes.AddRange(Half.GetBytes((Half)TextureU));
                    bytes.AddRange(Half.GetBytes((Half)TextureV));
                    size += 4;
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(TextureU));
                    bytes.AddRange(BitConverter.GetBytes(TextureV));
                    size += 8;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX2))
            {
                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    bytes.AddRange(Half.GetBytes((Half)Texture2U));
                    bytes.AddRange(Half.GetBytes((Half)Texture2V));
                    size += 4;
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(Texture2U));
                    bytes.AddRange(BitConverter.GetBytes(Texture2V));
                    size += 8;
                }
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COLOR))
            {
                bytes.Add(ColorR);
                bytes.Add(ColorG);
                bytes.Add(ColorB);
                bytes.Add(ColorA);
                size += 4;
            }

            if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_BLEND_WEIGHT))
            {
                bytes.Add(BlendIndexes[0]);
                bytes.Add(BlendIndexes[1]);
                bytes.Add(BlendIndexes[2]);
                bytes.Add(BlendIndexes[3]);
                size += 4;

                if (flags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                {
                    bytes.AddRange(Half.GetBytes((Half)BlendWeights[0]));
                    bytes.AddRange(Half.GetBytes((Half)BlendWeights[1]));
                    bytes.AddRange(Half.GetBytes((Half)BlendWeights[2]));
                    bytes.AddRange(Half.GetBytes((Half)0f));
                    size += 8; //An additional 2 bytes of padding added at the end
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(BlendWeights[0]));
                    bytes.AddRange(BitConverter.GetBytes(BlendWeights[1]));
                    bytes.AddRange(BitConverter.GetBytes(BlendWeights[2]));
                    bytes.AddRange(BitConverter.GetBytes(0f));
                    size += 16;
                }
            }

            if (size != bytes.Count) throw new Exception(String.Format("EMD_Vertex size mismatch. Expected a size of {0}, but it was {1}. (Flags: {2})", size, bytes.Count, flags));

            return bytes;

        }

        public int GetColorAsInt()
        {
            return BitConverter.ToInt32(new byte[4] { ColorR, ColorG, ColorB, ColorA }, 0);
        }
    }

}
