using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EMD
{
    [Flags]
    public enum VertexFlags : int
    {
        Position = 0x1,
        Normal = 0x2,
        TexUV = 0x4,
        Tex2UV = 0x8,
        Unk5 = 0x10,
        Unk6 = 0x20,
        Color = 0x40,
        Tangent = 0x80,
        Unk9 = 0x100,
        BlendWeight = 0x200,
        Unk11 = 0x400,
        Unk12 = 0x800,
        Unk13 = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        CompressedFormat = 0x8000, //Use float16
    }

    [Serializable]
    public class EMD_File : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [YAXDontSerialize]
        public bool ModelChanged { get; set; }
        [YAXDontSerialize]
        public bool TextureSamplersChanged { get; set; }

        public void TriggerModelChanged()
        {
            NotifyPropertyChanged(nameof(ModelChanged));
        }

        public void TriggerTexturesChanged()
        {
            NotifyPropertyChanged(nameof(TextureSamplersChanged));
        }
        #endregion

        public const int EMD_SIGNATURE = 1145914659;

        [YAXDontSerialize]
        public string Name { get; set; }

        [YAXAttributeForClass]
        public uint Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Model")]
        public AsyncObservableCollection<EMD_Model> Models { get; set; } = new AsyncObservableCollection<EMD_Model>();


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

        public void RefreshValues()
        {
            foreach(var model in Models)
            {
                model.RefreshValues();

                foreach(var mesh in model.Meshes)
                {
                    mesh.RefreshValues();

                    foreach(var submesh in mesh.Submeshes)
                    {
                        submesh.RefreshValues();
                    }
                }
            }
        }
    
        public EMD_Model GetParentModel(EMD_Mesh mesh)
        {
            foreach(var model in Models)
            {
                if (model.Meshes.Contains(mesh)) return model;
            }

            return null;
        }

        public EMD_Mesh GetParentMesh(EMD_Submesh submesh)
        {
            foreach (var model in Models)
            {
                foreach(var mesh in model.Meshes)
                {
                    if (mesh.Submeshes.Contains(submesh)) return mesh;
                }
            }

            return null;
        }

        public EMD_Submesh GetParentSubmesh(EMD_TextureSamplerDef textureSampler)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        if (submesh.TextureSamplerDefs.Contains(textureSampler)) return submesh;
                    }
                }
            }

            return null;
        }
    

        public void CopyTextureSamplers(EMD_File copyEmdFile)
        {
            //Simple foreach loop over all submeshes
            foreach (EMD_Model model in Models)
            {
                foreach (EMD_Mesh mesh in model.Meshes)
                {
                    foreach (EMD_Submesh submesh in mesh.Submeshes)
                    {
                        //Gets the submesh with the same material name from copyEmdFile
                        EMD_Submesh submeshToCopyFrom = copyEmdFile.GetSubmesh(submesh.Name);

                        //Check if emdFile has a equivalent submesh
                        //If one isn't found, then nothing will be copied
                        if (submeshToCopyFrom != null)
                        {
                            submesh.TextureSamplerDefs = submeshToCopyFrom.TextureSamplerDefs;
                        }
                    }
                }
            }
        }

        public EMD_Submesh GetSubmesh(string name)
        {
            //Gets the first submesh with the matching name

            foreach (EMD_Model model in Models)
            {
                foreach (EMD_Mesh mesh in model.Meshes)
                {
                    foreach (EMD_Submesh submesh in mesh.Submeshes)
                    {
                        //Check if name is the same, allowing for casing differences
                        if (submesh.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            return submesh;
                    }
                }
            }

            return null;
        }
    }

    [YAXSerializeAs("Model")]
    [Serializable]
    public class EMD_Model : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ushort I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Mesh")]
        public AsyncObservableCollection<EMD_Mesh> Meshes { get; set; } = new AsyncObservableCollection<EMD_Mesh>();

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(Name));
        }
        
        public List<EMD_Submesh> GetAllSubmeshes()
        {
            List<EMD_Submesh> submeshes = new List<EMD_Submesh>();

            foreach(var mesh in Meshes)
            {
                submeshes.AddRange(mesh.Submeshes);
            }

            return submeshes;
        }
    }

    [YAXSerializeAs("Mesh")]
    [Serializable]
    public class EMD_Mesh : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ushort I_52 { get; set; }

        public EMD_AABB AABB { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Submesh")]
        public AsyncObservableCollection<EMD_Submesh> Submeshes { get; set; } = new AsyncObservableCollection<EMD_Submesh>();

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

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(Name));
        }
    }

    [Serializable]
    public class EMD_AABB
    {
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("X")]
        public float CenterX { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Y")]
        public float CenterY { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("Z")]
        public float CenterZ { get; set; }
        [YAXAttributeFor("AABB_Center")]
        [YAXSerializeAs("W")]
        public float CenterW { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("X")]
        public float MinX { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Y")]
        public float MinY { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("Z")]
        public float MinZ { get; set; }
        [YAXAttributeFor("AABB_Min")]
        [YAXSerializeAs("W")]
        public float MinW { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("X")]
        public float MaxX { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Y")]
        public float MaxY { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("Z")]
        public float MaxZ { get; set; }
        [YAXAttributeFor("AABB_Max")]
        [YAXSerializeAs("W")]
        public float MaxW { get; set; }

        public static EMD_AABB Read(byte[] bytes, int offset)
        {
            return new EMD_AABB()
            {
                CenterX = BitConverter.ToSingle(bytes, offset + 0),
                CenterY = BitConverter.ToSingle(bytes, offset + 4),
                CenterZ = BitConverter.ToSingle(bytes, offset + 8),
                CenterW = BitConverter.ToSingle(bytes, offset + 12),
                MinX = BitConverter.ToSingle(bytes, offset + 16),
                MinY = BitConverter.ToSingle(bytes, offset + 20),
                MinZ = BitConverter.ToSingle(bytes, offset + 24),
                MinW = BitConverter.ToSingle(bytes, offset + 28),
                MaxX = BitConverter.ToSingle(bytes, offset + 32),
                MaxY = BitConverter.ToSingle(bytes, offset + 36),
                MaxZ = BitConverter.ToSingle(bytes, offset + 40),
                MaxW = BitConverter.ToSingle(bytes, offset + 44)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(CenterX));
            bytes.AddRange(BitConverter.GetBytes(CenterY));
            bytes.AddRange(BitConverter.GetBytes(CenterZ));
            bytes.AddRange(BitConverter.GetBytes(CenterW));
            bytes.AddRange(BitConverter.GetBytes(MinX));
            bytes.AddRange(BitConverter.GetBytes(MinY));
            bytes.AddRange(BitConverter.GetBytes(MinZ));
            bytes.AddRange(BitConverter.GetBytes(MinW));
            bytes.AddRange(BitConverter.GetBytes(MaxX));
            bytes.AddRange(BitConverter.GetBytes(MaxY));
            bytes.AddRange(BitConverter.GetBytes(MaxZ));
            bytes.AddRange(BitConverter.GetBytes(MaxW));

            return bytes.ToArray();
        }
    }

    [YAXSerializeAs("Submesh")]
    [Serializable]
    public class EMD_Submesh : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [YAXAttributeFor("VertexFlags")]
        [YAXSerializeAs("values")]
        public VertexFlags VertexFlags { get; set; }
        public EMD_AABB AABB { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        //Counts
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
                if (TextureSamplerDefs != null) return TextureSamplerDefs.Count;
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
        [YAXDontSerialize]
        public int VertexSize
        {
            get
            {
                return EMD_Vertex.GetVertexSize(VertexFlags);
            }
        }

        [YAXComment("Sampler entries depend on the shader defined in EMM. Usually if there are duplicate entries then only the last one matters, the earlier ones are just there to maintain the sampler index. (Dont delete them) \n" +
            "There is a max of 4 possible entries that the shaders will accept, but no actual (vanilla) EMD shader uses more than 3.")]
        public AsyncObservableCollection<EMD_TextureSamplerDef> TextureSamplerDefs { get; set; } = new AsyncObservableCollection<EMD_TextureSamplerDef>();
        public List<EMD_Triangle> Triangles { get; set; } = new List<EMD_Triangle>();
        public List<EMD_Vertex> Vertexes { get; set; } = new List<EMD_Vertex>();

        public string GetBoneName(int vertexIdx, int boneIdx)
        {
            //Gets the name of a bone linked on a EMD_Vertex

            for (int i = 0; i < Triangles.Count; i++)
            {
                foreach (ushort vertex in Triangles[i].Faces)
                {
                    if (vertex == vertexIdx)
                    {
                        return Triangles[i].Bones[boneIdx];
                    }
                }
            }

            throw new InvalidDataException(String.Format("Could not get the bone name for boneIndex: {0} on vertex: {1}", boneIdx, vertexIdx));
        }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(Name));
        }
    }

    [YAXSerializeAs("TextureSamplerDef")]
    [Serializable]
    public class EMD_TextureSamplerDef
    {
        public enum AddressMode : byte
        {
            Wrap = 0,
            Mirror = 1,
            Clamp = 2
        }

        public enum Filtering : byte
        {
            None = 0,
            Point = 1,
            Linear = 2
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Flag0")]
        [YAXHexValue]
        public byte I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TextureIndex")]
        public byte EmbIndex { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("AddressModeU")]
        public AddressMode AddressModeU { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("AddressModeV")]
        public AddressMode AddressModeV { get; set; }


        [YAXAttributeForClass]
        [YAXSerializeAs("FilteringMin")]
        public Filtering FilteringMin { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("FilteringMag")]
        public Filtering FilteringMag { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("ScaleU")]
        public float ScaleU { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ScaleV")]
        public float ScaleV { get; set; }

        public static AsyncObservableCollection<EMD_TextureSamplerDef> Read(byte[] bytes, int offset, int count)
        {
            AsyncObservableCollection<EMD_TextureSamplerDef> samplerDefs = new AsyncObservableCollection<EMD_TextureSamplerDef>();

            for(int i = 0; i < count; i++)
            {
                samplerDefs.Add(Read(bytes, offset));
                offset += 12;
            }

            return samplerDefs;
        }

        public static EMD_TextureSamplerDef Read(byte[] bytes, int offset)
        {
            EMD_TextureSamplerDef samplerDef = new EMD_TextureSamplerDef();

            byte[] val2 = Int4Converter.ToInt4(bytes[offset + 2]);
            byte[] val3 = Int4Converter.ToInt4(bytes[offset + 3]);

            samplerDef.I_00 = bytes[offset + 0];
            samplerDef.EmbIndex = bytes[offset + 1];

            samplerDef.AddressModeU = (AddressMode)val2[0];
            samplerDef.AddressModeV = (AddressMode)val2[1];
            samplerDef.FilteringMin = (Filtering)val3[0];
            samplerDef.FilteringMag = (Filtering)val3[1];

            samplerDef.ScaleU = BitConverter.ToSingle(bytes, offset + 4);
            samplerDef.ScaleV = BitConverter.ToSingle(bytes, offset + 8);

            return samplerDef;
        }
    
        public static byte[] Write(IList<EMD_TextureSamplerDef> samplerDefs)
        {
            List<byte> bytes = new List<byte>();

            foreach (var samplerDef in samplerDefs)
                bytes.AddRange(samplerDef.Write());

            return bytes.ToArray();
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(I_00);
            bytes.Add(EmbIndex);
            bytes.Add(Int4Converter.GetByte((byte)AddressModeU, (byte)AddressModeV));
            bytes.Add(Int4Converter.GetByte((byte)FilteringMin, (byte)FilteringMag));
            bytes.AddRange(BitConverter.GetBytes(ScaleU));
            bytes.AddRange(BitConverter.GetBytes(ScaleV));

            return bytes;
        }
    }

    [YAXSerializeAs("Triangle")]
    [Serializable]
    public class EMD_Triangle
    {
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

        public List<ushort> Faces { get; set; } = new List<ushort>();
        public List<string> Bones { get; set; } = new List<string>();
    }

    [YAXSerializeAs("Vertex")]
    [Serializable]
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
        public byte[] BlendIndexes { get; set; } = new byte[4]; //Size 4
        [YAXAttributeFor("BlendWeights")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] BlendWeights { get; set; } = new float[4]; //Size 3 (if float16, then 2 bytes of padding after)

        public static int GetVertexSize(VertexFlags flags)
        {
            int size = 0;

            if (flags.HasFlag(VertexFlags.Position))
            {
                size += 12;
            }

            if (flags.HasFlag(VertexFlags.Normal))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
                {
                    size += 8;
                }
                else
                {
                    size += 12;
                }
            }

            if (flags.HasFlag(VertexFlags.Tangent))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
                {
                    size += 8;
                }
                else
                {
                    size += 12;
                }
            }

            if (flags.HasFlag(VertexFlags.TexUV))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
                {
                    size += 4;
                }
                else
                {
                    size += 8;
                }
            }

            if (flags.HasFlag(VertexFlags.Tex2UV))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
                {
                    size += 4;
                }
                else
                {
                    size += 8;
                }
            }

            if (flags.HasFlag(VertexFlags.Color))
            {
                size += 4;
            }

            if (flags.HasFlag(VertexFlags.BlendWeight))
            {
                size += 4;

                if (flags.HasFlag(VertexFlags.CompressedFormat))
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

        public static List<EMD_Vertex> ReadVertices(VertexFlags flags, byte[] rawBytes, int offset, int vertexCount, int vertexSize)
        {
            List<EMD_Vertex> vertices = new List<EMD_Vertex>();

            for (int z = 0; z < vertexCount; z++)
            {
                int addedOffset = 0;
                EMD_Vertex vertex = new EMD_Vertex();

                if (flags.HasFlag(VertexFlags.Position))
                {
                    vertex.PositionX = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                    vertex.PositionY = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                    vertex.PositionZ = BitConverter.ToSingle(rawBytes, offset + addedOffset + 8);
                    addedOffset += 12;
                }

                if (flags.HasFlag(VertexFlags.Normal))
                {
                    if (flags.HasFlag(VertexFlags.CompressedFormat))
                    {
                        vertex.NormalX = Half.ToHalf(rawBytes, offset + addedOffset + 0);
                        vertex.NormalY = Half.ToHalf(rawBytes, offset + addedOffset + 2);
                        vertex.NormalZ = Half.ToHalf(rawBytes, offset + addedOffset + 4);
                        addedOffset += 8;
                    }
                    else
                    {
                        vertex.NormalX = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                        vertex.NormalY = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                        vertex.NormalZ = BitConverter.ToSingle(rawBytes, offset + addedOffset + 8);
                        addedOffset += 12;
                    }
                }



                if (flags.HasFlag(VertexFlags.TexUV))
                {
                    if (flags.HasFlag(VertexFlags.CompressedFormat))
                    {
                        vertex.TextureU = Half.ToHalf(rawBytes, offset + addedOffset + 0);
                        vertex.TextureV = Half.ToHalf(rawBytes, offset + addedOffset + 2);
                        addedOffset += 4;
                    }
                    else
                    {
                        vertex.TextureU = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                        vertex.TextureV = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                        addedOffset += 8;
                    }
                }

                if (flags.HasFlag(VertexFlags.Tex2UV))
                {
                    if (flags.HasFlag(VertexFlags.CompressedFormat))
                    {
                        vertex.Texture2U = Half.ToHalf(rawBytes, offset + addedOffset + 0);
                        vertex.Texture2V = Half.ToHalf(rawBytes, offset + addedOffset + 2);
                        addedOffset += 4;
                    }
                    else
                    {
                        vertex.Texture2U = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                        vertex.Texture2V = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                        addedOffset += 8;
                    }
                }

                if (flags.HasFlag(VertexFlags.Tangent))
                {
                    if (flags.HasFlag(VertexFlags.CompressedFormat))
                    {
                        vertex.TangentX = Half.ToHalf(rawBytes, offset + addedOffset + 0);
                        vertex.TangentY = Half.ToHalf(rawBytes, offset + addedOffset + 2);
                        vertex.TangentZ = Half.ToHalf(rawBytes, offset + addedOffset + 4);
                        addedOffset += 8;
                    }
                    else
                    {
                        vertex.TangentX = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                        vertex.TangentY = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                        vertex.TangentZ = BitConverter.ToSingle(rawBytes, offset + addedOffset + 8);
                        addedOffset += 12;
                    }
                }

                if (flags.HasFlag(VertexFlags.Color))
                {
                    vertex.ColorR = rawBytes[offset + addedOffset + 0];
                    vertex.ColorG = rawBytes[offset + addedOffset + 1];
                    vertex.ColorB = rawBytes[offset + addedOffset + 2];
                    vertex.ColorA = rawBytes[offset + addedOffset + 3];
                    addedOffset += 4;
                }

                if (flags.HasFlag(VertexFlags.BlendWeight))
                {
                    vertex.BlendIndexes[0] = rawBytes[offset + addedOffset + 0];
                    vertex.BlendIndexes[1] = rawBytes[offset + addedOffset + 1];
                    vertex.BlendIndexes[2] = rawBytes[offset + addedOffset + 2];
                    vertex.BlendIndexes[3] = rawBytes[offset + addedOffset + 3];

                    addedOffset += 4;

                    if (flags.HasFlag(VertexFlags.CompressedFormat))
                    {
                        vertex.BlendWeights[0] = Half.ToHalf(rawBytes, offset + addedOffset + 0);
                        vertex.BlendWeights[1] = Half.ToHalf(rawBytes, offset + addedOffset + 2);
                        vertex.BlendWeights[2] = Half.ToHalf(rawBytes, offset + addedOffset + 4);

                        addedOffset += 8;
                    }
                    else
                    {
                        vertex.BlendWeights[0] = BitConverter.ToSingle(rawBytes, offset + addedOffset + 0);
                        vertex.BlendWeights[1] = BitConverter.ToSingle(rawBytes, offset + addedOffset + 4);
                        vertex.BlendWeights[2] = BitConverter.ToSingle(rawBytes, offset + addedOffset + 8);
                        addedOffset += 16;
                    }

                    vertex.BlendWeights[3] = 1.0f - (vertex.BlendWeights[0] + vertex.BlendWeights[1] + vertex.BlendWeights[2]);
                }

                if(vertexSize != addedOffset)
                {
                    throw new InvalidDataException("EMD_Vertex.ReadVertices: VertexSize mismatch.");
                }

                offset += addedOffset;

                vertices.Add(vertex);
            }

            return vertices;
        }

        public static List<byte> GetBytes(IList<EMD_Vertex> vertices, VertexFlags flags)
        {
            List<byte> bytes = new List<byte>();

            foreach (var vertex in vertices)
                bytes.AddRange(vertex.GetBytes(flags));

            return bytes;
        }

        public List<byte> GetBytes(VertexFlags flags)
        {
            List<byte> bytes = new List<byte>();
            int size = 0;

            if (flags.HasFlag(VertexFlags.Position))
            {
                bytes.AddRange(BitConverter.GetBytes(PositionX));
                bytes.AddRange(BitConverter.GetBytes(PositionY));
                bytes.AddRange(BitConverter.GetBytes(PositionZ));
                size += 12;
            }

            if (flags.HasFlag(VertexFlags.Normal))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
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

            if (flags.HasFlag(VertexFlags.Tangent))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
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

            if (flags.HasFlag(VertexFlags.TexUV))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
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

            if (flags.HasFlag(VertexFlags.Tex2UV))
            {
                if (flags.HasFlag(VertexFlags.CompressedFormat))
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

            if (flags.HasFlag(VertexFlags.Color))
            {
                bytes.Add(ColorR);
                bytes.Add(ColorG);
                bytes.Add(ColorB);
                bytes.Add(ColorA);
                size += 4;
            }

            if (flags.HasFlag(VertexFlags.BlendWeight))
            {
                bytes.Add(BlendIndexes[0]);
                bytes.Add(BlendIndexes[1]);
                bytes.Add(BlendIndexes[2]);
                bytes.Add(BlendIndexes[3]);
                size += 4;

                if (flags.HasFlag(VertexFlags.CompressedFormat))
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
