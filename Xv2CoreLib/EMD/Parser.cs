using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.EMD
{

    public class Parser
    {
        const int EMD_SIGNATURE = 1145914659;

        private byte[] rawBytes;
        private List<byte> bytes;
        public EMD_File emdFile { get; private set; }

        public Parser(string path, bool writeXml)
        {
            rawBytes = File.ReadAllBytes(path);
            bytes = rawBytes.ToList();

            if (BitConverter.ToInt32(rawBytes, 0) != EMD_SIGNATURE) throw new InvalidDataException("EMD_SIGNATURE not found at offset 0x0. Parse failed.");

            ParseEmd();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMD_File));
                serializer.SerializeToFile(emdFile, path + ".xml");
            }
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();

            if (BitConverter.ToInt32(rawBytes, 0) != EMD_SIGNATURE) throw new InvalidDataException("EMD_SIGNATURE not found at offset 0x0. Parse failed.");

            ParseEmd();
        }


        private void ParseEmd()
        {
            emdFile = new EMD_File() { Models = new List<EMD_Model>() };

            //Header
            emdFile.Version = BitConverter.ToUInt32(rawBytes, 8);
            int modelTableCount = BitConverter.ToUInt16(rawBytes, 18);
            int modelTableOffset = BitConverter.ToInt32(rawBytes, 20);
            int modelNameTableOffset = BitConverter.ToInt32(rawBytes, 24);

            for (int i = 0; i < modelTableCount; i++)
            {
                int modelOffset = BitConverter.ToInt32(rawBytes, modelTableOffset);
                int modelNameOffset = BitConverter.ToInt32(rawBytes, modelNameTableOffset);

                if (modelOffset != 0)
                {
                    EMD_Model model = new EMD_Model() { Meshes = new List<EMD_Mesh>() };
                    model.I_00 = BitConverter.ToUInt16(rawBytes, modelOffset);

                    //Name
                    if (modelNameOffset != 0)
                    {
                        model.Name = Utils.GetString(bytes, modelNameOffset);
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
                        mesh.F_00 = BitConverter.ToSingle(rawBytes, meshOffset + 0);
                        mesh.F_04 = BitConverter.ToSingle(rawBytes, meshOffset + 4);
                        mesh.F_08 = BitConverter.ToSingle(rawBytes, meshOffset + 8);
                        mesh.F_12 = BitConverter.ToSingle(rawBytes, meshOffset + 12);
                        mesh.F_16 = BitConverter.ToSingle(rawBytes, meshOffset + 16);
                        mesh.F_20 = BitConverter.ToSingle(rawBytes, meshOffset + 20);
                        mesh.F_24 = BitConverter.ToSingle(rawBytes, meshOffset + 24);
                        mesh.F_28 = BitConverter.ToSingle(rawBytes, meshOffset + 28);
                        mesh.F_32 = BitConverter.ToSingle(rawBytes, meshOffset + 32);
                        mesh.F_36 = BitConverter.ToSingle(rawBytes, meshOffset + 36);
                        mesh.F_40 = BitConverter.ToSingle(rawBytes, meshOffset + 40);
                        mesh.F_44 = BitConverter.ToSingle(rawBytes, meshOffset + 44);
                        mesh.I_52 = BitConverter.ToUInt16(rawBytes, meshOffset + 52);

                        if (BitConverter.ToInt32(rawBytes, meshOffset + 48) != 0) //Checking if name offset is null
                        {
                            mesh.Name = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, meshOffset + 48) + meshOffset);
                        }
                        else
                        {
                            mesh.Name = String.Empty;
                        }
                        mesh.UniqueName = String.Format("", model.Name, mesh.Name);

                        //Submeshes
                        int submeshCount = BitConverter.ToUInt16(rawBytes, meshOffset + 54);
                        int submeshTableOffset = BitConverter.ToInt32(rawBytes, meshOffset + 56) + meshOffset;

                        for (int s = 0; s < submeshCount; s++)
                        {
                            int submeshOffset = BitConverter.ToInt32(rawBytes, submeshTableOffset) + meshOffset;

                            EMD_Submesh submesh = new EMD_Submesh() { TextureDefinitions = new List<EMD_TextureDefinition>(), Triangles = new List<EMD_Triangle>(), Vertexes = new List<EMD_Vertex>() };
                            submesh.F_00 = BitConverter.ToSingle(rawBytes, submeshOffset + 0);              //aabb (aligned axis box)
                            submesh.F_04 = BitConverter.ToSingle(rawBytes, submeshOffset + 4);
                            submesh.F_08 = BitConverter.ToSingle(rawBytes, submeshOffset + 8);
                            submesh.F_12 = BitConverter.ToSingle(rawBytes, submeshOffset + 12);
                            submesh.F_16 = BitConverter.ToSingle(rawBytes, submeshOffset + 16);
                            submesh.F_20 = BitConverter.ToSingle(rawBytes, submeshOffset + 20);
                            submesh.F_24 = BitConverter.ToSingle(rawBytes, submeshOffset + 24);
                            submesh.F_28 = BitConverter.ToSingle(rawBytes, submeshOffset + 28);
                            submesh.F_32 = BitConverter.ToSingle(rawBytes, submeshOffset + 32);
                            submesh.F_36 = BitConverter.ToSingle(rawBytes, submeshOffset + 36);
                            submesh.F_40 = BitConverter.ToSingle(rawBytes, submeshOffset + 40);
                            submesh.F_44 = BitConverter.ToSingle(rawBytes, submeshOffset + 44);

                            //Vertex
                            submesh.VtxFlags = (VTX_FLAGS)BitConverter.ToInt32(rawBytes, submeshOffset + 48);
                            int vertexSize = BitConverter.ToInt32(rawBytes, submeshOffset + 52);
                            int vertexCount = BitConverter.ToInt32(rawBytes, submeshOffset + 56);
                            int vertexOffset = BitConverter.ToInt32(rawBytes, submeshOffset + 60) + submeshOffset;
                            //submesh.VertexCount = vertexCount;

                            for (int z = 0; z < vertexCount; z++)
                            {
                                int addedOffset = 0;
                                EMD_Vertex vertex = new EMD_Vertex();

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_POS))
                                {
                                    vertex.PositionX = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0);
                                    vertex.PositionY = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4);
                                    vertex.PositionZ = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 8);
                                    addedOffset += 12;
                                }

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_NORM))
                                {
                                    if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                                    {
                                        vertex.NormalX = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.NormalY = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 2);
                                        vertex.NormalZ = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 4);
                                        addedOffset += 8;
                                    }
                                    else
                                    {
                                        vertex.NormalX = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.NormalY = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4);
                                        vertex.NormalZ = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 8);
                                        addedOffset += 12;
                                    }
                                }



                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX))
                                {
                                    if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                                    {
                                        vertex.TextureU = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.TextureV = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 2);
                                        addedOffset += 4;
                                    }
                                    else
                                    {
                                        vertex.TextureU = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.TextureV = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4);
                                        addedOffset += 8;
                                    }
                                }

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TEX2))
                                {
                                    if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                                    {
                                        vertex.Texture2U = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.Texture2V = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 2);
                                        addedOffset += 4;
                                    }
                                    else
                                    {
                                        vertex.Texture2U = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.Texture2V = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4);
                                        addedOffset += 8;
                                    }
                                }

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_TANGENT))
                                {
                                    if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                                    {
                                        vertex.TangentX = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.TangentY = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 2);
                                        vertex.TangentZ = Half.ToHalf(rawBytes, vertexOffset + addedOffset + 4);
                                        addedOffset += 8;
                                    }
                                    else
                                    {
                                        vertex.TangentX = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0);
                                        vertex.TangentY = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4);
                                        vertex.TangentZ = BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 8);
                                        addedOffset += 12;
                                    }
                                }

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COLOR))
                                {
                                    vertex.ColorR = rawBytes[vertexOffset + addedOffset + 0];
                                    vertex.ColorG = rawBytes[vertexOffset + addedOffset + 1];
                                    vertex.ColorB = rawBytes[vertexOffset + addedOffset + 2];
                                    vertex.ColorA = rawBytes[vertexOffset + addedOffset + 3];
                                    addedOffset += 4;
                                }

                                if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_BLEND_WEIGHT))
                                {
                                    vertex.BlendIndexes = new byte[4] { rawBytes[vertexOffset + addedOffset + 0], rawBytes[vertexOffset + addedOffset + 1], rawBytes[vertexOffset + addedOffset + 2], rawBytes[vertexOffset + addedOffset + 3] };
                                    //vertex.BlendIndexes = new byte[4] { rawBytes[vertexOffset + addedOffset + 3], rawBytes[vertexOffset + addedOffset + 2], rawBytes[vertexOffset + addedOffset + 1], rawBytes[vertexOffset + addedOffset + 0] };       //order is inversed , because the tool's game considere to be a uint32 to write in binary , instead of 4 x uint8. so on windows files of the game ,it's inversed
                                    addedOffset += 4;

                                    if (submesh.VtxFlags.HasFlag(VTX_FLAGS.EMD_VTX_FLAG_COMPRESSED_FORMAT))
                                    {
                                        vertex.BlendWeights = new float[4] { Half.ToHalf(rawBytes, vertexOffset + addedOffset + 0), Half.ToHalf(rawBytes, vertexOffset + addedOffset + 2), Half.ToHalf(rawBytes, vertexOffset + addedOffset + 4), 0f };
                                        addedOffset += 8;
                                    }
                                    else
                                    {
                                        vertex.BlendWeights = new float[4] { BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 0), BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 4), BitConverter.ToSingle(rawBytes, vertexOffset + addedOffset + 8), 0f };
                                        addedOffset += 16;
                                    }

                                    vertex.BlendWeights[3] = 1.0f - (vertex.BlendWeights[0] + vertex.BlendWeights[1] + vertex.BlendWeights[2]);
                                }

                                if (vertexSize != addedOffset)
                                {
                                    throw new InvalidDataException("The calculated vertex size did not match up with the declared size in the file. \nParse failed.");
                                }

                                submesh.Vertexes.Add(vertex);
                                vertexOffset += vertexSize;
                            }

                            //Name
                            if (BitConverter.ToInt32(rawBytes, submeshOffset + 64) != 0)
                            {
                                submesh.Name = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, submeshOffset + 64) + submeshOffset);
                            }
                            else
                            {
                                submesh.Name = String.Empty;
                            }
                            submesh.UniqueName = String.Format("", model.Name, mesh.Name, submesh.Name);

                            //Texture Definitions
                            int textureDefinitionCount = rawBytes[submeshOffset + 69];
                            int textureDefinitionOffset = BitConverter.ToInt32(rawBytes, submeshOffset + 72) + submeshOffset;

                            for (int z = 0; z < textureDefinitionCount; z++)
                            {
                                EMD_TextureDefinition textDefinition = new EMD_TextureDefinition();

                                textDefinition.I_00 = rawBytes[textureDefinitionOffset + 0];
                                textDefinition.I_01 = rawBytes[textureDefinitionOffset + 1];
                                textDefinition.I_02 = rawBytes[textureDefinitionOffset + 2];
                                textDefinition.I_03 = rawBytes[textureDefinitionOffset + 3];
                                textDefinition.F_04 = BitConverter.ToSingle(rawBytes, textureDefinitionOffset + 4);
                                textDefinition.F_08 = BitConverter.ToSingle(rawBytes, textureDefinitionOffset + 8);

                                textureDefinitionOffset += 12;
                                submesh.TextureDefinitions.Add(textDefinition);
                            }

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
                                int faceTableOffset = 16 + triangleOffset;
                                int faceNameTableOffset = faceTableOffset + faceCount * 2;
                                if ((faceNameTableOffset % 4) == 2) faceNameTableOffset += 2;
                                triangle.FaceCount_Debug = faceCount;
                                triangle.FaceCountDivided_Debug = faceCount / 3;

                                for (int h = 0; h < faceCount; h++)
                                {
                                    triangle.Faces.Add(BitConverter.ToUInt16(rawBytes, faceTableOffset));
                                    faceTableOffset += 2;
                                }

                                for (int h = 0; h < faceNameCount; h++)
                                {
                                    int faceNameOffset = BitConverter.ToInt32(rawBytes, faceNameTableOffset) + triangleOffset;
                                    triangle.Bones.Add(Utils.GetString(bytes, faceNameOffset));
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
