using System;
using System.Collections.Generic;
using YAXLib;
using Xv2CoreLib.EMD;
using System.IO;

namespace Xv2CoreLib.EMG
{
    [YAXSerializeAs("EMG")]
    [Serializable]
    public class EMG_File
    {
        private const int EMG_SIGNATURE = 1196246307;

        [YAXAttributeForClass]
        public ushort I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Mesh")]
        public List<EMG_Mesh> Mesh { get; set; } = new List<EMG_Mesh>();

        public static EMG_File Read(byte[] bytes, int offset)
        {
            EMG_File emgFile = new EMG_File();

            //Sanity check
            if (BitConverter.ToInt32(bytes, offset) != EMG_SIGNATURE)
                throw new InvalidDataException("EMG_File.Read: \"#EMG\" signature not found!");

            emgFile.I_04 = BitConverter.ToUInt16(bytes, offset + 4);

            ushort meshCount = BitConverter.ToUInt16(bytes, offset + 6);

            //Parse all meshes
            for(int i = 0; i < meshCount; i++)
            {
                int meshOffset = BitConverter.ToInt32(bytes, offset + 8 + (4 * i)) + offset;
                emgFile.Mesh.Add(EMG_Mesh.Read(bytes, meshOffset));
            }

            return emgFile;
        }

        public byte[] Write(bool writeVertices = true, int absOffsetInFile = 0)
        {
            ushort meshCount = (ushort)(Mesh != null ? Mesh.Count : 0);
            List<byte> bytes = new List<byte>();

            //EMG Header:
            bytes.AddRange(BitConverter.GetBytes(EMG_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(meshCount));

            //Mesh offsets
            bytes.AddRange(new byte[4 * meshCount]);

            for(int i = 0; i < meshCount; i++)
            {
                //Pad file to be in alignment
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                //Add offset
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 8 + (4 * i));

                bytes.AddRange(Mesh[i].Write(writeVertices, absOffsetInFile + bytes.Count));
            }

            return bytes.ToArray();
        }
    }

    [YAXSerializeAs("Mesh")]
    [Serializable]
    public class EMG_Mesh
    {
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public ushort Strips { get; set; }

        [YAXAttributeFor("VertexFlags")]
        [YAXSerializeAs("value")]
        public VertexFlags VertexFlags { get; set; }
        public EMD_AABB AABB { get; set; }

        public List<EMG_TextureList> TextureLists { get; set; } = new List<EMG_TextureList>();
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Submesh")]
        public List<EMG_Submesh> Submesh { get; set; } = new List<EMG_Submesh>();
        public List<EMD_Vertex> Vertices { get; set; } = new List<EMD_Vertex>();

        //Saving values:
        [YAXDontSerialize]
        public int VertexOffset { get; private set; } 
        [YAXDontSerialize]
        public int StartOffset { get; private set; }

        public static EMG_Mesh Read(byte[] bytes, int offset)
        {
            EMG_Mesh mesh = new EMG_Mesh();
            mesh.VertexFlags = (VertexFlags)BitConverter.ToInt32(bytes, offset);
            mesh.I_08 = BitConverter.ToInt32(bytes, offset + 8);
            mesh.Strips = BitConverter.ToUInt16(bytes, offset + 24);
            mesh.AABB = EMD_AABB.Read(bytes, offset + 32);

            int textureListsCount = BitConverter.ToInt32(bytes, offset + 4);
            int textureListsOffset = BitConverter.ToInt32(bytes, offset + 12) + offset;
            ushort vertexCount = BitConverter.ToUInt16(bytes, offset + 16);
            ushort vertexSize = BitConverter.ToUInt16(bytes, offset + 18);
            int vertexOffset = BitConverter.ToInt32(bytes, offset + 20) + offset;
            ushort submeshCount = BitConverter.ToUInt16(bytes, offset + 26);
            int submeshListOffset = BitConverter.ToInt32(bytes, offset + 28) + offset;

            //Textures
            for(int i = 0; i < textureListsCount; i++)
            {
                int texOffset = BitConverter.ToInt32(bytes, textureListsOffset + (4 * i)) + offset;

                int count = BitConverter.ToInt32(bytes, texOffset);
                EMG_TextureList texList = new EMG_TextureList();
                texList.TextureSamplerDefs = EMD_TextureSamplerDef.Read(bytes, texOffset + 4, count);

                mesh.TextureLists.Add(texList);
            }

            //Submesh
            for(int i = 0; i < submeshCount; i++)
            {
                int submeshOffset = BitConverter.ToInt32(bytes, submeshListOffset + (4 * i)) + offset;

                mesh.Submesh.Add(EMG_Submesh.Read(bytes, submeshOffset));
            }

            //Read vertices
            //These can either be in the EMG file, or after the skeleton in the parent EMO file. But when reading them, we dont need to care about that.
            mesh.Vertices = EMD_Vertex.ReadVertices(mesh.VertexFlags, bytes, vertexOffset, vertexCount, vertexSize);

            return mesh;
        }
        
        public List<byte> Write(bool writeVertices, int absOffsetInFile)
        {
            StartOffset = absOffsetInFile;

            List<byte> bytes = new List<byte>();

            int textureListsCount = (TextureLists != null) ? TextureLists.Count : 0;
            int submeshCount = (Submesh != null) ? Submesh.Count : 0;
            int vertexCount = (Vertices != null) ? Vertices.Count : 0;

            //Mesh header:
            bytes.AddRange(BitConverter.GetBytes((int)VertexFlags));
            bytes.AddRange(BitConverter.GetBytes(textureListsCount));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(textureListsCount > 0 ? 80 : 0)); //Textures offset (12). Since textures come first we can just set this now.
            bytes.AddRange(BitConverter.GetBytes((ushort)vertexCount));
            bytes.AddRange(BitConverter.GetBytes((ushort)EMD_Vertex.GetVertexSize(VertexFlags)));
            VertexOffset = bytes.Count + absOffsetInFile;
            bytes.AddRange(BitConverter.GetBytes(0)); //Vertex offset (20)
            bytes.AddRange(BitConverter.GetBytes(Strips));
            bytes.AddRange(BitConverter.GetBytes((ushort)submeshCount));
            bytes.AddRange(BitConverter.GetBytes(0)); //Submesh offset (28)
            bytes.AddRange(AABB.Write());

            //Textures:
            int pointerList = bytes.Count;
            bytes.AddRange(new byte[4 * textureListsCount]); //Write null bytes that will be replaced with the actual pointers 

            for(int i = 0; i < textureListsCount; i++)
            {
                //Update offset to point to this entry
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), pointerList + (4 * i));

                bytes.AddRange(TextureLists[i].Write());
            }

            //Submesh:
            if (submeshCount > 0)
            {
                pointerList = bytes.Count;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(pointerList), 28); //Add submesh offset to mesh header
                bytes.AddRange(new byte[4 * submeshCount]); //Write null bytes that will be replaced with the actual pointers 


                for(int i = 0; i < submeshCount; i++)
                {
                    //Pad file to 16-byte alignment
                    bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                    //Update offset to point to this entry
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), pointerList + (4 * i));
                    bytes.AddRange(Submesh[i].Write());
                }
            }

            if (writeVertices)
            {
                //Pad file to 16-byte alignment
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                //Vertices:
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(pointerList), 20); //Add vertex offset to mesh header
                bytes.AddRange(EMD_Vertex.GetBytes(Vertices, VertexFlags));
            }

            return bytes;
        }
    
    }

    [YAXSerializeAs("TextureList")]
    [Serializable]
    public class EMG_TextureList
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TextureSamplerDef")]
        public List<EMD_TextureSamplerDef> TextureSamplerDefs { get; set; } = new List<EMD_TextureSamplerDef>();

        public static EMG_TextureList Read(byte[] bytes, int offset)
        {
            EMG_TextureList textureList = new EMG_TextureList();
            textureList.TextureSamplerDefs = EMD_TextureSamplerDef.Read(bytes, offset + 4, BitConverter.ToInt32(bytes, offset + 0));

            return textureList;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(TextureSamplerDefs.Count));
            bytes.AddRange(EMD_TextureSamplerDef.Write(TextureSamplerDefs));

            return bytes;
        }
    }

    [YAXSerializeAs("Submesh")]
    [Serializable]
    public class EMG_Submesh
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Material")]
        public string MaterialName { get; set; } = string.Empty;
        [YAXAttributeForClass]
        [YAXSerializeAs("TextureListIndex")]
        public ushort TextureListIndex { get; set; }

        [YAXAttributeFor("Barycenter")]
        [YAXSerializeAs("X")]
        public float BarycenterX { get; set; }
        [YAXAttributeFor("Barycenter")]
        [YAXSerializeAs("Y")]
        public float BarycenterY { get; set; }
        [YAXAttributeFor("Barycenter")]
        [YAXSerializeAs("Z")]
        public float BarycenterZ { get; set; }
        [YAXAttributeFor("Barycenter")]
        [YAXSerializeAs("W")]
        public float BarycenterW { get; set; }

        public List<ushort> Faces { get; set; } = new List<ushort>();
        public List<ushort> Bones { get; set; } = new List<ushort>();

        public static EMG_Submesh Read(byte[] bytes, int offset)
        {
            EMG_Submesh submesh = new EMG_Submesh();
            submesh.BarycenterX = BitConverter.ToSingle(bytes, offset + 0);
            submesh.BarycenterY = BitConverter.ToSingle(bytes, offset + 4);
            submesh.BarycenterZ = BitConverter.ToSingle(bytes, offset + 8);
            submesh.BarycenterW = BitConverter.ToSingle(bytes, offset + 12);
            submesh.TextureListIndex = BitConverter.ToUInt16(bytes, offset + 16);

            ushort faceCount = BitConverter.ToUInt16(bytes, offset + 18);
            ushort boneCount = BitConverter.ToUInt16(bytes, offset + 20);

            submesh.MaterialName = StringEx.GetString(bytes, offset + 22, false, StringEx.EncodingType.UTF8, 32);

            //Faces
            for(int i = 0; i < faceCount; i++)
            {
                submesh.Faces.Add(BitConverter.ToUInt16(bytes, offset + 54 + (i * 2)));
            }

            //Bones
            int boneStartOffset = offset + 54 + (faceCount * 2);

            for (int i = 0; i < boneCount; i++)
            {
                submesh.Bones.Add(BitConverter.ToUInt16(bytes, boneStartOffset + (i * 2)));
            }

            return submesh;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(BarycenterX));
            bytes.AddRange(BitConverter.GetBytes(BarycenterY));
            bytes.AddRange(BitConverter.GetBytes(BarycenterZ));
            bytes.AddRange(BitConverter.GetBytes(BarycenterW));
            bytes.AddRange(BitConverter.GetBytes(TextureListIndex));
            bytes.AddRange(BitConverter.GetBytes((ushort)Faces.Count));
            bytes.AddRange(BitConverter.GetBytes((ushort)Bones.Count));

            if (MaterialName.Length > 32)
                throw new InvalidDataException("EMG_Submesh.MaterialName cannot be greater than 32 characters!");

            bytes.AddRange(Utils.GetStringBytes(MaterialName, 32));

            //Write Faces
            foreach (var face in Faces)
                bytes.AddRange(BitConverter.GetBytes(face));

            //Write Bones
            foreach (var bone in Bones)
                bytes.AddRange(BitConverter.GetBytes(bone));

            return bytes;
        }
    }
}
