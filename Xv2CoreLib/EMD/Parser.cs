using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.EMD
{

    public class Parser
    {
        const int EMD_SIGNATURE = 1145914659;

        private byte[] rawBytes;
        public EMD_File emdFile { get; private set; } = new EMD_File();
        private int startAddress = 0;

        public Parser(string path, bool writeXml)
        {
            emdFile.Name = Path.GetFileName(path);
            rawBytes = File.ReadAllBytes(path);

            if (BitConverter.ToInt32(rawBytes, 0) != EMD_SIGNATURE) throw new InvalidDataException("EMD_SIGNATURE not found at offset 0x0. Parse failed.");

            ParseEmd();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMD_File));
                serializer.SerializeToFile(emdFile, path + ".xml");
            }
        }

        public Parser(byte[] _rawBytes, int startAddress = 0)
        {
            rawBytes = _rawBytes;
            this.startAddress = startAddress;

            if (BitConverter.ToInt32(rawBytes, 0 + startAddress) != EMD_SIGNATURE) throw new InvalidDataException("EMD_SIGNATURE not found at offset 0x0. Parse failed.");

            ParseEmd();
        }


        private void ParseEmd()
        {

            //Header
            emdFile.Version = BitConverter.ToUInt32(rawBytes, 8 + startAddress);
            int modelTableCount = BitConverter.ToUInt16(rawBytes, 18 + startAddress);
            int modelTableOffset = BitConverter.ToInt32(rawBytes, 20 + startAddress) + startAddress;
            int modelNameTableOffset = BitConverter.ToInt32(rawBytes, 24 + startAddress) + startAddress;

            for (int i = 0; i < modelTableCount; i++)
            {
                int modelOffset = BitConverter.ToInt32(rawBytes, modelTableOffset) + startAddress;
                int modelNameOffset = BitConverter.ToInt32(rawBytes, modelNameTableOffset) + startAddress;

                if (modelOffset != 0)
                {
                    EMD_Model model = new EMD_Model() { Meshes = new List<EMD_Mesh>() };
                    model.I_00 = BitConverter.ToUInt16(rawBytes, modelOffset);

                    //Name
                    if (modelNameOffset != 0)
                    {
                        model.Name = StringEx.GetString(rawBytes, modelNameOffset);
                    }
                    else
                    {
                        model.Name = String.Empty;
                    }

                    //Mesh
                    int meshCount = BitConverter.ToUInt16(rawBytes, modelOffset + 2);
                    int meshTableOffset = BitConverter.ToInt32(rawBytes, modelOffset + 4) + modelOffset;

                    for (int a = 0; a < meshCount; a++)
                    {
                        int meshOffset = BitConverter.ToInt32(rawBytes, meshTableOffset) + modelOffset;

                        //Mesh data
                        EMD_Mesh mesh = new EMD_Mesh() { Submeshes = new List<EMD_Submesh>() };
                        mesh.AABB = EMD_AABB.Read(rawBytes, meshOffset);
                        mesh.I_52 = BitConverter.ToUInt16(rawBytes, meshOffset + 52);

                        if (BitConverter.ToInt32(rawBytes, meshOffset + 48) != 0) //Checking if name offset is null
                        {
                            mesh.Name = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, meshOffset + 48) + meshOffset);
                        }
                        else
                        {
                            mesh.Name = String.Empty;
                        }

                        //Submeshes
                        int submeshCount = BitConverter.ToUInt16(rawBytes, meshOffset + 54);
                        int submeshTableOffset = BitConverter.ToInt32(rawBytes, meshOffset + 56) + meshOffset;

                        for (int s = 0; s < submeshCount; s++)
                        {
                            int submeshOffset = BitConverter.ToInt32(rawBytes, submeshTableOffset) + meshOffset;

                            EMD_Submesh submesh = new EMD_Submesh() { TextureSamplerDefs = new List<EMD_TextureSamplerDef>(), Triangles = new List<EMD_Triangle>(), Vertexes = new List<EMD_Vertex>() };
                            submesh.AABB = EMD_AABB.Read(rawBytes, submeshOffset);

                            //Vertex
                            submesh.VertexFlags = (VertexFlags)BitConverter.ToInt32(rawBytes, submeshOffset + 48);
                            int vertexSize = BitConverter.ToInt32(rawBytes, submeshOffset + 52);
                            int vertexCount = BitConverter.ToInt32(rawBytes, submeshOffset + 56);
                            int vertexOffset = BitConverter.ToInt32(rawBytes, submeshOffset + 60) + submeshOffset;
                            submesh.Vertexes = EMD_Vertex.ReadVertices(submesh.VertexFlags, rawBytes, vertexOffset, vertexCount, vertexSize);

                            //Name
                            if (BitConverter.ToInt32(rawBytes, submeshOffset + 64) != 0)
                            {
                                submesh.Name = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, submeshOffset + 64) + submeshOffset);
                            }
                            else
                            {
                                submesh.Name = String.Empty;
                            }

                            //Texture Definitions
                            int textureDefinitionCount = rawBytes[submeshOffset + 69];
                            int textureDefinitionOffset = BitConverter.ToInt32(rawBytes, submeshOffset + 72) + submeshOffset;
                            submesh.TextureSamplerDefs = EMD_TextureSamplerDef.Read(rawBytes, textureDefinitionOffset, textureDefinitionCount);

                            //Triangles
                            int triangleCount = BitConverter.ToUInt16(rawBytes, submeshOffset + 70);
                            int trianglesTableOffset = BitConverter.ToInt32(rawBytes, submeshOffset + 76) + submeshOffset;

                            for (int z = 0; z < triangleCount; z++)
                            {
                                int triangleOffset = BitConverter.ToInt32(rawBytes, trianglesTableOffset) + submeshOffset;
                                EMD_Triangle triangle = new EMD_Triangle() { Bones = new List<string>(), Faces = new List<ushort>() };

                                //Offsets & counts
                                int faceCount = BitConverter.ToInt32(rawBytes, triangleOffset + 0);
                                int faceNameCount = BitConverter.ToInt32(rawBytes, triangleOffset + 4);
                                int faceTableOffset = (BitConverter.ToInt32(rawBytes, triangleOffset + 8) != 0) ? BitConverter.ToInt32(rawBytes, triangleOffset + 8) + triangleOffset : triangleOffset + 16;

                                int faceNameTableOffset = BitConverter.ToInt32(rawBytes, triangleOffset + 12) + triangleOffset;

                                for (int h = 0; h < faceCount; h++)
                                {
                                    triangle.Faces.Add(BitConverter.ToUInt16(rawBytes, faceTableOffset));
                                    faceTableOffset += 2;
                                }

                                for (int h = 0; h < faceNameCount; h++)
                                {
                                    int faceNameOffset = BitConverter.ToInt32(rawBytes, faceNameTableOffset) + triangleOffset;
                                    triangle.Bones.Add(StringEx.GetString(rawBytes, faceNameOffset));
                                    faceNameTableOffset += 4;
                                }

                                submesh.Triangles.Add(triangle);
                                trianglesTableOffset += 4;
                            }


                            mesh.Submeshes.Add(submesh);
                            submeshTableOffset += 4;
                        }


                        model.Meshes.Add(mesh);
                        meshTableOffset += 4;
                    }


                    emdFile.Models.Add(model);
                }



                modelNameTableOffset += 4;
                modelTableOffset += 4;
            }

        }

    }
}
