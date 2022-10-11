using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Xv2CoreLib.EMP_NEW
{
    public class Deserializer
    {
        private string saveLocation;
        public EMP_File EmpFile { get; private set; }
        public List<byte> bytes = new List<byte>();

        //Offset storage
        internal readonly List<List<int>> EmbTextureOffsets = new List<List<int>>();
        internal readonly List<List<int>> EmbTextureOffsets_Minus = new List<List<int>>();

        public Deserializer(EMP_File empFile, string location)
        {
            saveLocation = location;
            EmpFile = empFile;
            TextureSamplerList_Setup();

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMP_File empFile)
        {
            EmpFile = empFile;
            TextureSamplerList_Setup();
            Write();
        }

        private void TextureSamplerList_Setup()
        {
            for (int i = 0; i < EmpFile.Textures.Count; i++)
            {
                EmbTextureOffsets.Add(new List<int>());
                EmbTextureOffsets_Minus.Add(new List<int>());
            }
        }

        private void Write()
        {
            //Counts
            int nodeCount = (EmpFile.ParticleNodes != null) ? EmpFile.ParticleNodes.Count : 0;
            int textureSamplerCount = (EmpFile.Textures != null) ? EmpFile.Textures.Count : 0;

            //Create copy of empFile and work of that, so the original isn't altered
            //Why??
            //EmpFile = EmpFile.Clone();

            //Header
            bytes.AddRange(BitConverter.GetBytes(EMP_File.EMP_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)24));
            bytes.AddRange(BitConverter.GetBytes((ushort)EmpFile.Version));
            bytes.AddRange(BitConverter.GetBytes((ushort)0));
            bytes.AddRange(BitConverter.GetBytes((short)nodeCount));
            bytes.AddRange(BitConverter.GetBytes((short)textureSamplerCount));
            bytes.AddRange(BitConverter.GetBytes(32));
            bytes.AddRange(new byte[12]);

            if (EmpFile.ParticleNodes?.Count > 0)
            {
                WriteNodes(EmpFile.ParticleNodes);
            }

            if (EmpFile.Textures?.Count > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);
                WriteTextureSamplers(EmpFile.Textures);
            }

        }

        private void WriteNodes(IList<ParticleNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                int relativeOffsetToEntryStart = bytes.Count + CalculatePaddingAfterMainEntry();
                int nextEntryOffset_ToReplace = bytes.Count + 152 + CalculatePaddingAfterMainEntry();
                int nextSubEntryOffset_ToReplace = bytes.Count + 156 + CalculatePaddingAfterMainEntry();
                WriteNode(nodes[i]);

                if(nodes[i].ChildParticleNodes != null)
                {
                    if (nodes[i].ChildParticleNodes.Count > 0)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - relativeOffsetToEntryStart + CalculatePaddingAfterMainEntry()), nextSubEntryOffset_ToReplace);
                        WriteNodes(nodes[i].ChildParticleNodes);
                    }
                }

                if (i != nodes.Count - 1)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - relativeOffsetToEntryStart + CalculatePaddingAfterMainEntry()), nextEntryOffset_ToReplace);
                }
            }

        }

        private void WriteNode(ParticleNode node)
        {
            if (EmpFile.FullDecompile)
            {
                node.CompileAllKeyframes();
            }

            //Add padding for byte-alignment
            bytes.AddRange(new byte[CalculatePaddingAfterMainEntry()]);

            int nodeOffset = bytes.Count;

            //Trim node name if its too long
            if (node.Name.Length > 32)
            {
                node.Name = node.Name.Substring(0, 32);
            }

            //Write name, and padd it out to 32 bytes
            bytes.AddRange(Encoding.ASCII.GetBytes(node.Name));
            bytes.AddRange(new byte[32 - node.Name.Length]);

            //Counts
            int keyframedValuesCount = (node.KeyframedValues != null) ? node.KeyframedValues.Count : 0;
            int groupKeyframedValuesCount = (node.GroupKeyframedValues != null) ? node.GroupKeyframedValues.Count : 0;
            int childNodeCount = (node.ChildParticleNodes != null) ? node.ChildParticleNodes.Count : 0;

            //Base Entry
            BitArray compositeBits_I_32 = new BitArray(new bool[8] { node.I_32_0, node.Loop, node.I_32_2, node.FlashOnGeneration, node.I_32_4, node.I_32_5, node.I_32_6, node.I_32_7 });
            BitArray compositeBits_I_33 = new BitArray(new bool[8] { node.I_33_0, node.I_33_1, node.I_33_2, node.Hide, node.UseScaleXY, node.I_33_5, node.UseColor2, node.I_33_7, });
            BitArray compositeBits_I_34 = new BitArray(new bool[8] { node.I_34_0, node.I_34_1, node.I_34_2, node.I_34_3, node.EnableRandomRotationDirection, node.I_34_5, node.EnableRandomUpVectorOnVirtualCone, node.I_34_7, });

            bytes.AddRange(new byte[6] { Utils.ConvertToByte(compositeBits_I_32), Utils.ConvertToByte(compositeBits_I_33), Utils.ConvertToByte(compositeBits_I_34), (byte)node.AutoRotationType, (byte)0, (byte)node.NodeType });

            bytes.AddRange(BitConverter.GetBytes(node.MaxInstances));
            bytes.AddRange(BitConverter.GetBytes(node.Lifetime));
            bytes.AddRange(BitConverter.GetBytes(node.Lifetime_Variance));
            bytes.Add(node.StartTime);
            bytes.Add(node.StartTime_Variance);
            bytes.Add(node.BurstFrequency);
            bytes.Add(node.BurstFrequency_Variance);
            bytes.AddRange(BitConverter.GetBytes(node.I_48));
            bytes.AddRange(BitConverter.GetBytes(node.I_50));
            bytes.AddRange(BitConverter.GetBytes(node.Burst));
            bytes.AddRange(BitConverter.GetBytes(node.Burst_Variance));
            bytes.AddRange(BitConverter.GetBytes(node.I_56));
            bytes.AddRange(BitConverter.GetBytes(node.I_58));
            bytes.AddRange(BitConverter.GetBytes(node.I_60));
            bytes.AddRange(BitConverter.GetBytes(node.I_62));
            bytes.AddRange(BitConverter.GetBytes(node.Position.Constant.X));
            bytes.AddRange(BitConverter.GetBytes(node.Position.Constant.Y));
            bytes.AddRange(BitConverter.GetBytes(node.Position.Constant.Z));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange(BitConverter.GetBytes(node.Position_Variance.X));
            bytes.AddRange(BitConverter.GetBytes(node.Position_Variance.Y));
            bytes.AddRange(BitConverter.GetBytes(node.Position_Variance.Z));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation.Constant.X));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation.Constant.Y));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation.Constant.Z));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation_Variance.X));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation_Variance.Y));
            bytes.AddRange(BitConverter.GetBytes(node.Rotation_Variance.Z));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange(BitConverter.GetBytes(node.F_128));
            bytes.AddRange(BitConverter.GetBytes(node.F_132));

            bytes.AddRange(BitConverter.GetBytes(node.I_136));
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(keyframedValuesCount)));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(groupKeyframedValuesCount)));
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(childNodeCount)));
            bytes.AddRange(new byte[12]);

            //Write Emitter or Emission data
            byte secondaryNodeType = 0;

            switch (node.NodeType)
            {
                case ParticleNodeType.Null:
                    break;
                case ParticleNodeType.Emitter:
                    bytes.AddRange(node.EmitterNode.Write(ref secondaryNodeType));
                    break;
                case ParticleNodeType.Emission:
                    bytes.AddRange(node.EmissionNode.Write(ref secondaryNodeType, nodeOffset, this));
                    break;
                default:
                    throw new InvalidDataException($"EMP_File: NodeType {node.NodeType} not recognized! Save failed.");
            }

            bytes[nodeOffset + 36] = secondaryNodeType;

            //Keyframes
            if (keyframedValuesCount > 0)
            {
                WriteKeyframedValues(node.KeyframedValues, nodeOffset + 140, nodeOffset);
            }
            if (groupKeyframedValuesCount > 0)
            {
                WriteGroupKeyframedValues(node.GroupKeyframedValues, nodeOffset + 148, nodeOffset);
            }
        }

        //Writers (Section 1)
        private void WriteKeyframedValues(IList<EMP_KeyframedValue> type0, int Type0_Offset, int mainEntryOffset)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - mainEntryOffset - 136), Type0_Offset);

            List<int> entryOffsets = new List<int>();

            for (int i = 0; i < type0.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type0[i].GetParameters()));
                bytes.AddRange(new byte[2] { BitConverter_Ex.GetBytes(type0[i].Loop), type0[i].I_03 });
                bytes.AddRange(BitConverter.GetBytes(type0[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type0[i].Duration));
                bytes.AddRange(BitConverter.GetBytes((short)type0[i].Keyframes.Count()));
                entryOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);

                //Sort keyframes
                if(type0[i].Keyframes != null)
                {
                    type0[i].Keyframes = Sorting.SortEntries2(type0[i].Keyframes);
                }
            }

            for (int i = 0; i < type0.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - entryOffsets[i] + 12), entryOffsets[i]);

                float keyframeSize = 0;

                //Keyframes
                foreach (var e in type0[i].Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(e.Time));
                    keyframeSize += 2;
                }
                if (Math.Floor(keyframeSize / 4) != keyframeSize / 4)
                {
                    bytes.AddRange(new byte[2]);
                }

                //Floats
                foreach (var e in type0[i].Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(e.Value));
                }

                //Index List
                if (type0[i].Keyframes.Count() > 1)
                {
                    //Writing IndexList
                    bool specialCase_FirstKeyFrameIsNotZero = (type0[i].Keyframes[0].Time == 0) ? false : true;
                    float totalIndex = 0;

                    for (int s = 0; s < type0[i].Keyframes.Count(); s++)
                    {
                        int thisFrameLength = 0;
                        if (type0[i].Keyframes.Count() - 1 == s)
                        {
                            thisFrameLength = 1;
                        }
                        else if (specialCase_FirstKeyFrameIsNotZero == true && s == 0)
                        {
                            thisFrameLength = type0[i].Keyframes[s].Time;
                            thisFrameLength += type0[i].Keyframes[s + 1].Time - type0[i].Keyframes[s].Time;
                        }
                        else
                        {
                            thisFrameLength = type0[i].Keyframes[s + 1].Time - type0[i].Keyframes[s].Time;
                        }

                        for (int a = 0; a < thisFrameLength; a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes((short)s));
                            totalIndex += 1;
                        }
                    }

                    //Add padding if needed
                    if (Math.Floor(totalIndex / 2) != totalIndex / 2)
                    {
                        bytes.AddRange(new byte[2]);
                    }
                }


            }

        }

        private void WriteGroupKeyframedValues(IList<EMP_KeyframeGroup> type1, int Type1_Offset, int mainEntryOffset)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset), Type1_Offset);

            //Offsets to replace
            List<int> HeaderOffsets = new List<int>();

            for (int i = 0; i < type1.Count(); i++)
            {
                int subEntryCount = (type1[i].KeyframedValues == null) ? 0 : type1[i].KeyframedValues.Count();
                bytes.AddRange(new byte[2] { type1[i].I_00, type1[i].I_01 });
                bytes.AddRange(BitConverter.GetBytes((ushort)subEntryCount));
                HeaderOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < type1.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - HeaderOffsets[i] + 4), HeaderOffsets[i]);
                List<int> EntryOffsets = new List<int>();

                if (type1[i].KeyframedValues != null)
                {
                    for (int a = 0; a < type1[i].KeyframedValues.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(type1[i].KeyframedValues[a].GetParameters()));
                        bytes.AddRange(new byte[2] { BitConverter_Ex.GetBytes(type1[i].KeyframedValues[a].Loop), type1[i].KeyframedValues[a].I_03 });
                        bytes.AddRange(BitConverter.GetBytes(type1[i].KeyframedValues[a].F_04));
                        bytes.AddRange(BitConverter.GetBytes(type1[i].KeyframedValues[a].Duration));
                        bytes.AddRange(BitConverter.GetBytes((ushort)type1[i].KeyframedValues[a].Keyframes.Count()));
                        EntryOffsets.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                    }

                    for (int a = 0; a < type1[i].KeyframedValues.Count(); a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - EntryOffsets[a] + 12), EntryOffsets[a]);

                        float keyframeSize = 0;

                        //Keyframes
                        foreach (var e in type1[i].KeyframedValues[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Time));
                            keyframeSize += 2;
                        }
                        if (Math.Floor(keyframeSize / 4) != keyframeSize / 4)
                        {
                            bytes.AddRange(new byte[2]);
                        }

                        //Floats
                        foreach (var e in type1[i].KeyframedValues[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Value));
                        }

                        //Index List
                        if (type1[i].KeyframedValues[a].Keyframes.Count() > 1)
                        {
                            //Writing IndexList
                            bool specialCase_FirstKeyFrameIsNotZero = (type1[i].KeyframedValues[a].Keyframes[0].Time == 0) ? false : true;
                            float totalIndex = 0;
                            for (int s = 0; s < type1[i].KeyframedValues[a].Keyframes.Count(); s++)
                            {
                                int thisFrameLength = 0;
                                if (type1[i].KeyframedValues[a].Keyframes.Count() - 1 == s)
                                {
                                    thisFrameLength = 1;
                                }
                                else if (specialCase_FirstKeyFrameIsNotZero == true && s == 0)
                                {
                                    thisFrameLength = type1[i].KeyframedValues[a].Keyframes[s].Time;
                                    thisFrameLength += type1[i].KeyframedValues[a].Keyframes[s + 1].Time - type1[i].KeyframedValues[a].Keyframes[s].Time;
                                }
                                else
                                {
                                    thisFrameLength = type1[i].KeyframedValues[a].Keyframes[s + 1].Time - type1[i].KeyframedValues[a].Keyframes[s].Time;
                                }

                                for (int e = 0; e < thisFrameLength; e++)
                                {
                                    bytes.AddRange(BitConverter.GetBytes((short)s));
                                    totalIndex += 1;
                                }
                            }

                            //Add padding if needed
                            if (Math.Floor(totalIndex / 2) != totalIndex / 2)
                            {
                                bytes.AddRange(new byte[2]);
                            }
                        }
                    }
                }
            }
        }

        //Writers (Section 2)
        private void WriteTextureSamplers(IList<EMP_TextureSamplerDef> embEntries)
        {
            List<int> subData2Offsets_ToReplace = new List<int>();


            for (int i = 0; i < embEntries.Count; i++)
            {

                //Filling in offsets
                for (int a = 0; a < EmbTextureOffsets[i].Count; a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - EmbTextureOffsets_Minus[i][a]), EmbTextureOffsets[i][a]);
                }

                //getting subdata type, and defaulting it if it doesn't exist

                EMP_TextureSamplerDef.TextureAnimationType textureType = embEntries[i].TextureType;

                bytes.AddRange(new byte[10] { embEntries[i].I_00, embEntries[i].EmbIndex, embEntries[i].I_02, embEntries[i].I_03, embEntries[i].FilteringMin, embEntries[i].FilteringMax, (byte)embEntries[i].RepitionX, (byte)embEntries[i].RepetitionY, embEntries[i].RandomSymetryX, embEntries[i].RandomSymetryY });
                bytes.AddRange(BitConverter.GetBytes((ushort)textureType));


                switch (textureType)
                {
                    case EMP_TextureSamplerDef.TextureAnimationType.Static:
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[0].ScrollX));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[0].ScrollY));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[0].ScaleX));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[0].ScaleY));

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            embEntries[i].ScrollAnimation.Keyframes[0].SetDefaultValuesForSDBH();
                            bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].ScrollAnimation.Keyframes[0].F_20)));
                            bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].ScrollAnimation.Keyframes[0].F_24)));
                        }

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_TextureSamplerDef.TextureAnimationType.Speed:
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.ScrollSpeed_U));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.ScrollSpeed_V));
                        bytes.AddRange(new byte[8]);

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_TextureSamplerDef.TextureAnimationType.SpriteSheet:
                        bytes.AddRange(new byte[10]);
                        int animationCount = (embEntries[i].ScrollAnimation.Keyframes != null) ? embEntries[i].ScrollAnimation.Keyframes.Count() : 0;
                        bytes.AddRange(BitConverter.GetBytes((short)animationCount));

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }
                        break;
                    default:
                        throw new InvalidDataException("Unknown EmbEntry.TextureAnimationType: " + textureType);
                }


            }

            for (int i = 0; i < embEntries.Count; i++)
            {
                if (embEntries[i].ScrollAnimation != null)
                {
                    EMP_TextureSamplerDef.TextureAnimationType textureType = embEntries[i].TextureType;

                    if (textureType == EMP_TextureSamplerDef.TextureAnimationType.SpriteSheet)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - subData2Offsets_ToReplace[i] + 12), subData2Offsets_ToReplace[i]);

                        for (int a = 0; a < embEntries[i].ScrollAnimation.Keyframes.Count; a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[a].Time));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[a].ScrollX));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[a].ScrollY));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[a].ScaleX));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].ScrollAnimation.Keyframes[a].ScaleY));

                            if (EmpFile.Version == VersionEnum.SDBH)
                            {
                                embEntries[i].ScrollAnimation.Keyframes[a].SetDefaultValuesForSDBH();
                                bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].ScrollAnimation.Keyframes[a].F_20)));
                                bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].ScrollAnimation.Keyframes[a].F_24)));
                            }
                        }
                    }
                }
            }

        }

        //Utility
        private bool TextureScrollAnimationIsType0Check(ObservableCollection<EMP_ScrollKeyframe> keyframes)
        {
            if (keyframes != null)
            {
                if (keyframes.Count() == 1)
                {
                    if (keyframes[0].Time == -1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int CalculatePaddingAfterMainEntry()
        {
            return Utils.CalculatePadding(bytes.Count, 16);
        }


    }
}
