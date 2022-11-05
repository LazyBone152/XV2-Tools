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

            bytes.AddRange(StringEx.WriteFixedSizeString(node.Name, 32));

            //Counts
            int keyframedValuesCount = (node.KeyframedValues != null) ? node.KeyframedValues.Count : 0;
            int groupKeyframedValuesCount = (node.GroupKeyframedValues != null) ? node.GroupKeyframedValues.Count : 0;
            int childNodeCount = (node.ChildParticleNodes != null) ? node.ChildParticleNodes.Count : 0;

            bytes.AddRange(BitConverter.GetBytes((ushort)node.NodeFlags));
            bytes.Add((byte)node.NodeFlags2);

            if(node.NodeSpecificType == NodeSpecificType.AutoOriented || node.NodeSpecificType == NodeSpecificType.AutoOriented_VisibleOnSpeed)
            {
                if(node.EmissionNode.BillboardType != ParticleBillboardType.Camera && node.EmissionNode.BillboardType != ParticleBillboardType.Front)
                    node.EmissionNode.BillboardType = ParticleBillboardType.Camera;

                bytes.Add((byte)node.EmissionNode.BillboardType);
            }
            else
            {
                bytes.Add((byte)ParticleBillboardType.Camera);
            }

            bytes.Add(0); //Emission | Emitter type
            bytes.Add((byte)node.NodeType);
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
            bytes.AddRange(BitConverter.GetBytes(node.I_128));
            bytes.AddRange(BitConverter.GetBytes(node.I_130));
            bytes.AddRange(BitConverter.GetBytes(node.F_132));

            //I_136 depends on the node type. 
            //0 = ShapeDraw, ConeExtrude or Null
            //1 = Emitters
            //2 = Emissions, other than ShapeDraw or ConeExtrude
            if(node.NodeSpecificType == NodeSpecificType.ShapeDraw || node.NodeSpecificType == NodeSpecificType.ConeExtrude || node.NodeSpecificType == NodeSpecificType.Null)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)0));
            }
            else if(node.NodeType == ParticleNodeType.Emitter)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)1));
            }
            else
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)2));
            }

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
                bytes.AddRange(BitConverter.GetBytes(type0[i].DefaultValue));
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

        private void WriteGroupKeyframedValues(IList<EMP_Modifier> modifiers, int modifiersOffset, int mainEntryOffset)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset), modifiersOffset);

            //Offsets to replace
            List<int> HeaderOffsets = new List<int>();

            for (int i = 0; i < modifiers.Count; i++)
            {
                int keyframedValuesCount = (modifiers[i].KeyframedValues == null) ? 0 : modifiers[i].KeyframedValues.Count;

                bytes.Add((byte)modifiers[i].Type);
                bytes.Add((byte)modifiers[i].Flags);
                bytes.AddRange(BitConverter.GetBytes((ushort)keyframedValuesCount));
                HeaderOffsets.Add(bytes.Count);
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - HeaderOffsets[i] + 4), HeaderOffsets[i]);
                List<int> EntryOffsets = new List<int>();

                if (modifiers[i].KeyframedValues != null)
                {
                    for (int a = 0; a < modifiers[i].KeyframedValues.Count; a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(modifiers[i].KeyframedValues[a].GetParameters()));
                        bytes.AddRange(new byte[2] { BitConverter_Ex.GetBytes(modifiers[i].KeyframedValues[a].Loop), modifiers[i].KeyframedValues[a].I_03 });
                        bytes.AddRange(BitConverter.GetBytes(modifiers[i].KeyframedValues[a].DefaultValue));
                        bytes.AddRange(BitConverter.GetBytes(modifiers[i].KeyframedValues[a].Duration));
                        bytes.AddRange(BitConverter.GetBytes((ushort)modifiers[i].KeyframedValues[a].Keyframes.Count));
                        EntryOffsets.Add(bytes.Count);
                        bytes.AddRange(new byte[4]);
                    }

                    for (int a = 0; a < modifiers[i].KeyframedValues.Count(); a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - EntryOffsets[a] + 12), EntryOffsets[a]);

                        float keyframeSize = 0;

                        //Keyframes
                        foreach (EMP_Keyframe e in modifiers[i].KeyframedValues[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Time));
                            keyframeSize += 2;
                        }
                        if (Math.Floor(keyframeSize / 4) != keyframeSize / 4)
                        {
                            bytes.AddRange(new byte[2]);
                        }

                        //Floats
                        foreach (var e in modifiers[i].KeyframedValues[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Value));
                        }

                        //Index List
                        if (modifiers[i].KeyframedValues[a].Keyframes.Count > 1)
                        {
                            //Writing IndexList
                            bool specialCase_FirstKeyFrameIsNotZero = (modifiers[i].KeyframedValues[a].Keyframes[0].Time == 0) ? false : true;
                            float totalIndex = 0;
                            for (int s = 0; s < modifiers[i].KeyframedValues[a].Keyframes.Count; s++)
                            {
                                int thisFrameLength = 0;
                                if (modifiers[i].KeyframedValues[a].Keyframes.Count - 1 == s)
                                {
                                    thisFrameLength = 1;
                                }
                                else if (specialCase_FirstKeyFrameIsNotZero == true && s == 0)
                                {
                                    thisFrameLength = modifiers[i].KeyframedValues[a].Keyframes[s].Time;
                                    thisFrameLength += modifiers[i].KeyframedValues[a].Keyframes[s + 1].Time - modifiers[i].KeyframedValues[a].Keyframes[s].Time;
                                }
                                else
                                {
                                    thisFrameLength = modifiers[i].KeyframedValues[a].Keyframes[s + 1].Time - modifiers[i].KeyframedValues[a].Keyframes[s].Time;
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
        private void WriteTextureSamplers(IList<EMP_TextureSamplerDef> textures)
        {
            List<int> KeyframeOffsets_ToReplace = new List<int>();

            for (int i = 0; i < textures.Count; i++)
            {
                //Filling in offsets
                for (int a = 0; a < EmbTextureOffsets[i].Count; a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - EmbTextureOffsets_Minus[i][a]), EmbTextureOffsets[i][a]);
                }

                bytes.AddRange(new byte[10] { textures[i].I_00, textures[i].EmbIndex, textures[i].I_02, textures[i].I_03, (byte)textures[i].FilteringMin, (byte)textures[i].FilteringMag, (byte)textures[i].RepetitionU, (byte)textures[i].RepetitionV, textures[i].RandomSymetryU, textures[i].RandomSymetryV });
                bytes.AddRange(BitConverter.GetBytes((ushort)textures[i].ScrollState.ScrollType));

                switch (textures[i].ScrollState.ScrollType)
                {
                    case EMP_ScrollState.ScrollTypeEnum.Static:
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScrollU));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScrollV));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScaleU));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScaleV));

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].I_20));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].I_24));
                        }

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_ScrollState.ScrollTypeEnum.Speed:
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.ScrollSpeed_U));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.ScrollSpeed_V));
                        bytes.AddRange(new byte[8]);

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_ScrollState.ScrollTypeEnum.SpriteSheet:
                        bytes.AddRange(new byte[10]);
                        int animationCount = (textures[i].ScrollState.Keyframes != null) ? textures[i].ScrollState.Keyframes.Count() : 0;
                        bytes.AddRange(BitConverter.GetBytes((short)animationCount));

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);

                        if (EmpFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }
                        break;
                    default:
                        throw new InvalidDataException("Unknown ScrollState.ScrollType: " + textures[i].ScrollState.ScrollType);
                }
            }

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i].ScrollState != null)
                {
                    if (textures[i].ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - KeyframeOffsets_ToReplace[i] + 12), KeyframeOffsets_ToReplace[i]);

                        for (int a = 0; a < textures[i].ScrollState.Keyframes.Count; a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].Time));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScrollU));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScrollV));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScaleU));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScaleV));

                            if (EmpFile.Version == VersionEnum.SDBH)
                            {
                                bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].I_20));
                                bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].I_24));
                            }
                        }
                    }
                }
            }

        }

        private int CalculatePaddingAfterMainEntry()
        {
            return Utils.CalculatePadding(bytes.Count, 16);
        }


    }
}
