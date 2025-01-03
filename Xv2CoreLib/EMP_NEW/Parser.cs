﻿using System;
using System.Linq;
using System.IO;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EMP_NEW
{
    public class Parser
    {
        public EMP_File EmpFile { get; private set; } = new EMP_File();
        private readonly byte[] rawBytes;

        public int RootNodeCount { get; private set; }
        public int TextureSamplerCount { get; private set; }
        public int NodeOffset { get; private set; }
        public int TextureSamplersOffset { get; private set; }
        public int CurrentEntryEnd { get; private set; }

        public Parser(string path, bool fullDecompile)
        {
            EmpFile.FullDecompile = fullDecompile;
            rawBytes = File.ReadAllBytes(path);
            Parse();
        }

        public Parser(byte[] bytes, bool fullDecompile)
        {
            EmpFile.FullDecompile = fullDecompile;
            rawBytes = bytes;
            Parse();
        }

        private void Parse()
        {
            if (BitConverter.ToInt32(rawBytes, 0) != 1347241251)
                throw new InvalidDataException("#EMP file signature not found.");

            //Read header
            RootNodeCount = BitConverter.ToInt16(rawBytes, 12);
            TextureSamplerCount = BitConverter.ToInt16(rawBytes, 14);
            NodeOffset = BitConverter.ToInt32(rawBytes, 16);
            TextureSamplersOffset = BitConverter.ToInt32(rawBytes, 20);
            EmpFile.Version = (VersionEnum)BitConverter.ToUInt16(rawBytes, 8);

            //Read TextureSamplers
            if (TextureSamplerCount > 0)
            {
                for (int i = 0; i < TextureSamplerCount; i++)
                {
                    EmpFile.Textures.Add(EMP_TextureSamplerDef.Read(rawBytes, TextureSamplersOffset, i, EmpFile.Version));
                }
            }

            //Read ParticleNodes
            if (RootNodeCount > 0)
            {
                int finalParticleNodeEnd = (TextureSamplersOffset != 0) ? TextureSamplersOffset : rawBytes.Length - 1;
                EmpFile.ParticleNodes = ParseNodes(NodeOffset, finalParticleNodeEnd);
            }
        }

        //Nodes:
        private AsyncObservableCollection<ParticleNode> ParseNodes(int entryOffset, int nextParticleEffectOffset_Abs)
        {
            AsyncObservableCollection<ParticleNode> effectEntries = new AsyncObservableCollection<ParticleNode>();

            int nodeIndex = 0;

            while (true)
            {
                int SubEntry_Offset = BitConverter.ToInt32(rawBytes, entryOffset + 156);
                int NextEntry_Offset = BitConverter.ToInt32(rawBytes, entryOffset + 152);
                
                //Get entryEndOffset (relative)
                int nextEntry = (SubEntry_Offset != 0) ? SubEntry_Offset : NextEntry_Offset;
                CurrentEntryEnd = (nextEntry != 0) ? nextEntry + entryOffset : nextParticleEffectOffset_Abs;
                int nextEntryOffset = (NextEntry_Offset != 0) ? NextEntry_Offset + entryOffset : nextParticleEffectOffset_Abs;

                effectEntries.Add(ParseNode(entryOffset));

                if (SubEntry_Offset > 0)
                {
                    effectEntries[nodeIndex].ChildParticleNodes = ParseNodes(SubEntry_Offset + entryOffset, nextEntryOffset);
                }

                entryOffset += NextEntry_Offset;
                nodeIndex++;

                if (NextEntry_Offset == 0)
                {
                    break;
                }
            }

            return effectEntries;

        }

        private ParticleNode ParseNode(int mainNodeOffset)
        {
            ParticleNode node = ParticleNode.GetNew();

            //Flags and Offsets for extra data
            int secondaryNodeType = rawBytes[mainNodeOffset + 36];
            int nodeType = rawBytes[mainNodeOffset + 37];
            int keyframedValuesCount = BitConverter.ToInt16(rawBytes, mainNodeOffset + 138);
            int keyframedValuesOffset = BitConverter.ToInt32(rawBytes, mainNodeOffset + 140) + 136 + mainNodeOffset;
            int groupedKeyframeValuesCount = BitConverter.ToInt16(rawBytes, mainNodeOffset + 144);
            int groupedKeyframedValuesOffset = BitConverter.ToInt32(rawBytes, mainNodeOffset + 148) + mainNodeOffset;

            //Main Entry values
            node.NodeType = (ParticleNodeType)nodeType;
            node.Name = StringEx.GetString(rawBytes, mainNodeOffset, false, StringEx.EncodingType.ASCII, 32);

            node.NodeFlags = (NodeFlags1)BitConverter.ToUInt16(rawBytes, mainNodeOffset + 32);
            node.NodeFlags2 = (NodeFlags2)rawBytes[mainNodeOffset + 34];
            node.MaxInstances = BitConverter.ToInt16(rawBytes, mainNodeOffset + 38);
            node.Lifetime = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 40);
            node.Lifetime_Variance = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 42);
            node.StartTime = rawBytes[mainNodeOffset + 44];
            node.StartTime_Variance = rawBytes[mainNodeOffset + 45];
            node.BurstFrequency = rawBytes[mainNodeOffset + 46];
            node.BurstFrequency_Variance = rawBytes[mainNodeOffset + 47];
            node.I_48 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 48);
            node.I_50 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 50);
            node.Burst = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 52);
            node.Burst_Variance = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 54);
            node.I_56 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 56);
            node.I_58 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 58);
            node.I_60 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 60);
            node.I_62 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 62);

            node.Position.Constant.X = BitConverter.ToSingle(rawBytes, mainNodeOffset + 64);
            node.Position.Constant.Y = BitConverter.ToSingle(rawBytes, mainNodeOffset + 68);
            node.Position.Constant.Z = BitConverter.ToSingle(rawBytes, mainNodeOffset + 72);

            node.Position_Variance.X = BitConverter.ToSingle(rawBytes, mainNodeOffset + 80);
            node.Position_Variance.Y = BitConverter.ToSingle(rawBytes, mainNodeOffset + 84);
            node.Position_Variance.Z = BitConverter.ToSingle(rawBytes, mainNodeOffset + 88);

            node.Rotation.Constant.X = BitConverter.ToSingle(rawBytes, mainNodeOffset + 96);
            node.Rotation.Constant.Y = BitConverter.ToSingle(rawBytes, mainNodeOffset + 100);
            node.Rotation.Constant.Z = BitConverter.ToSingle(rawBytes, mainNodeOffset + 104);

            node.Rotation_Variance.X = BitConverter.ToSingle(rawBytes, mainNodeOffset + 112);
            node.Rotation_Variance.Y = BitConverter.ToSingle(rawBytes, mainNodeOffset + 116);
            node.Rotation_Variance.Z = BitConverter.ToSingle(rawBytes, mainNodeOffset + 120);

            node.I_128 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 128);
            node.I_130 = BitConverter.ToUInt16(rawBytes, mainNodeOffset + 130);
            node.F_132 = BitConverter.ToSingle(rawBytes, mainNodeOffset + 132);


            //NodeType:
            switch (nodeType)
            {
                case 0:
                    //Null. Entry has nothing on it.
                    break;
                case 1:
                    //Node is an emitter
                    switch (secondaryNodeType)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            node.EmitterNode = ParticleEmitter.Parse(rawBytes, mainNodeOffset);
                            break;
                        default:
                            throw new InvalidDataException($"EMP_File: EmitterType {secondaryNodeType} is not recognized!");
                    }
                    break;
                case 2:
                    //Node is an emission
                    switch (secondaryNodeType)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            node.EmissionNode = ParticleEmission.Parse(rawBytes, mainNodeOffset, this);
                            break;
                        default:
                            throw new InvalidDataException($"EMP_File: EmissionType {secondaryNodeType} is not recognized!");
                    }
                    break;
                default:
                    throw new InvalidDataException($"EMP_File: NodeType {nodeType} is not recognized!");
            }

            //KeyframedValues
            if (keyframedValuesCount > 0)
            {
                for (int a = 0; a < keyframedValuesCount; a++)
                {
                    int idx = node.KeyframedValues.Count();
                    ushort duration = BitConverter.ToUInt16(rawBytes, keyframedValuesOffset + 8);
                    bool interpolate = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[1]);
                    bool loop = BitConverter_Ex.ToBoolean(rawBytes, keyframedValuesOffset + 2);

                    node.KeyframedValues.Add(new EMP_KeyframedValue()
                    {
                        Interpolate = interpolate,
                        Loop = loop,
                        I_03 = rawBytes[keyframedValuesOffset + 3],
                        DefaultValue = BitConverter.ToSingle(rawBytes, keyframedValuesOffset + 4),
                        Keyframes = ParseKeyframes<EMP_Keyframe>(BitConverter.ToInt16(rawBytes, keyframedValuesOffset + 10), BitConverter.ToInt32(rawBytes, keyframedValuesOffset + 12) + keyframedValuesOffset, duration, loop, interpolate)
                    });

                    node.KeyframedValues[idx].SetParameters(rawBytes[keyframedValuesOffset + 0], Int4Converter.ToInt4(rawBytes[keyframedValuesOffset + 1])[0]);

                    keyframedValuesOffset += 16;
                }
            }

            //GroupKeyframedValues
            if (groupedKeyframeValuesCount > 0)
            {
                for (int a = 0; a < groupedKeyframeValuesCount; a++)
                {
                    int entryCount = BitConverter.ToInt16(rawBytes, groupedKeyframedValuesOffset + 2);
                    int entryOffset = BitConverter.ToInt32(rawBytes, groupedKeyframedValuesOffset + 4) + groupedKeyframedValuesOffset;

                    node.Modifiers.Add(new EMP_Modifier(false));
                    node.Modifiers[a].Type = rawBytes[groupedKeyframedValuesOffset];
                    node.Modifiers[a].Flags = (EMP_Modifier.ModifierFlags)rawBytes[groupedKeyframedValuesOffset + 1];

                    for (int d = 0; d < entryCount; d++)
                    {
                        int subEntryCount = BitConverter.ToInt16(rawBytes, entryOffset + 10);
                        int keyframesOffset = BitConverter.ToInt32(rawBytes, entryOffset + 12) + entryOffset;

                        node.Modifiers[a].KeyframedValues.Add(new EMP_KeyframedValue());

                        node.Modifiers[a].KeyframedValues[d].SetParameters(rawBytes[entryOffset + 0], Int4Converter.ToInt4(rawBytes[entryOffset + 1])[0]);
                        node.Modifiers[a].KeyframedValues[d].Interpolate = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[entryOffset + 1])[1]);
                        node.Modifiers[a].KeyframedValues[d].Loop = BitConverter_Ex.ToBoolean(rawBytes, entryOffset + 2);
                        node.Modifiers[a].KeyframedValues[d].I_03 = rawBytes[entryOffset + 3];
                        node.Modifiers[a].KeyframedValues[d].DefaultValue = BitConverter.ToSingle(rawBytes, entryOffset + 4);

                        ushort duration = BitConverter.ToUInt16(rawBytes, entryOffset + 8);
                        node.Modifiers[a].KeyframedValues[d].Keyframes = ParseKeyframes<EMP_Keyframe>(subEntryCount, keyframesOffset, duration, node.Modifiers[a].KeyframedValues[d].Loop, node.Modifiers[a].KeyframedValues[d].Interpolate);

                        entryOffset += 16;
                    }

                    groupedKeyframedValuesOffset += 8;
                }

            }

            //Decompile keyframed values
            if (EmpFile.FullDecompile)
            {
                //Position + Rotation keyframes exist on all node types
                node.Position.DecompileKeyframes(node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_POSITION, EMP_KeyframedValue.COMPONENT_X, EMP_KeyframedValue.COMPONENT_Y, EMP_KeyframedValue.COMPONENT_Z));
                node.Rotation.DecompileKeyframes(node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_ROTATION, EMP_KeyframedValue.COMPONENT_X, EMP_KeyframedValue.COMPONENT_Y, EMP_KeyframedValue.COMPONENT_Z));

                //Scale, Color1 and Color2 only exist on emission types
                if (node.NodeType == ParticleNodeType.Emission)
                {
                    EMP_KeyframedValue[] color1Keyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_COLOR1, EMP_KeyframedValue.COMPONENT_R, EMP_KeyframedValue.COMPONENT_G, EMP_KeyframedValue.COMPONENT_B);
                    EMP_KeyframedValue[] color2Keyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_COLOR2, EMP_KeyframedValue.COMPONENT_R, EMP_KeyframedValue.COMPONENT_G, EMP_KeyframedValue.COMPONENT_B);
                    EMP_KeyframedValue[] color1AlphaKeyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_COLOR1, EMP_KeyframedValue.COMPONENT_A);
                    EMP_KeyframedValue[] color2AlphaKeyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_COLOR2, EMP_KeyframedValue.COMPONENT_A);
                    EMP_KeyframedValue[] scaleBaseKeyframes;

                    if (node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
                    {
                        EMP_KeyframedValue[] scale2Keyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_SCALE, EMP_KeyframedValue.COMPONENT_X, EMP_KeyframedValue.COMPONENT_Y);
                        scaleBaseKeyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_SCALE, EMP_KeyframedValue.COMPONENT_Z);

                        node.EmissionNode.Texture.ScaleXY.DecompileKeyframes(scale2Keyframes);
                    }
                    else
                    {
                        scaleBaseKeyframes = node.GetKeyframedValues(EMP_KeyframedValue.PARAMETER_SCALE, EMP_KeyframedValue.COMPONENT_X);
                    }

                    node.EmissionNode.Texture.Color1.DecompileKeyframes(color1Keyframes);
                    node.EmissionNode.Texture.Color2.DecompileKeyframes(color2Keyframes);
                    node.EmissionNode.Texture.Color1_Transparency.DecompileKeyframes(color1AlphaKeyframes);
                    node.EmissionNode.Texture.Color2_Transparency.DecompileKeyframes(color2AlphaKeyframes);
                    node.EmissionNode.Texture.ScaleBase.DecompileKeyframes(scaleBaseKeyframes);

                }

                //Node-specific keyframed values
                switch (node.NodeSpecificType)
                {
                    case NodeSpecificType.SphericalDistribution:
                        node.EmitterNode.Size.DecompileKeyframes(node.GetKeyframedValues(2, 0));
                        node.EmitterNode.Velocity.DecompileKeyframes(node.GetKeyframedValues(2, 1));
                        break;
                    case NodeSpecificType.VerticalDistribution:
                        node.EmitterNode.Position.DecompileKeyframes(node.GetKeyframedValues(2, 0));
                        node.EmitterNode.Velocity.DecompileKeyframes(node.GetKeyframedValues(2, 1));
                        node.EmitterNode.Angle.DecompileKeyframes(node.GetKeyframedValues(2, 2));
                        break;
                    case NodeSpecificType.ShapeAreaDistribution:
                    case NodeSpecificType.ShapePerimeterDistribution:
                        node.EmitterNode.Position.DecompileKeyframes(node.GetKeyframedValues(2, 0));
                        node.EmitterNode.Velocity.DecompileKeyframes(node.GetKeyframedValues(2, 1));
                        node.EmitterNode.Angle.DecompileKeyframes(node.GetKeyframedValues(2, 2));
                        node.EmitterNode.Size.DecompileKeyframes(node.GetKeyframedValues(3, 0));
                        node.EmitterNode.Size2.DecompileKeyframes(node.GetKeyframedValues(3, 1));
                        break;
                    case NodeSpecificType.AutoOriented:
                    case NodeSpecificType.Default:
                    case NodeSpecificType.Mesh:
                    case NodeSpecificType.ShapeDraw:
                        node.EmissionNode.ActiveRotation.DecompileKeyframes(node.GetKeyframedValues(1, 3));
                        break;
                }

                node.KeyframedValues.Clear();

                //Decompile modifier keyframes
                foreach(EMP_Modifier modifier in node.Modifiers)
                {
                    modifier.DecompileEmp();
                }
            }

            return node;
        }

        private AsyncObservableCollection<TKeyframeType> ParseKeyframes<TKeyframeType>(int keyframeCount, int keyframeListOffset, ushort duration, bool loop, bool interpolate) where TKeyframeType : IKeyframe, new()
        {
            int floatOffset = 0;

            //Calculate float list offset
            float fCount = keyframeCount;

            if (Math.Floor(fCount / 2) != fCount / 2)
            {
                fCount += 1f;
            }
            fCount = fCount * 2;
            floatOffset = (int)fCount + keyframeListOffset;

            AsyncObservableCollection<TKeyframeType> keyframes = new AsyncObservableCollection<TKeyframeType>();

            for (int i = 0; i < keyframeCount; i++)
            {
                var keyframe = new TKeyframeType()
                {
                    Time = BitConverter.ToUInt16(rawBytes, keyframeListOffset),
                    Value = BitConverter.ToSingle(rawBytes, floatOffset)
                };
                keyframes.Add(keyframe);

                keyframeListOffset += 2;
                floatOffset += 4;
            }

            if (duration == 0)
                return keyframes;

            if (EmpFile.FullDecompile)
            {
                //Ensure that a keyframe exists at the end frame. If there is none, an interpolated one will be added
                TKeyframeType endKeyframe = keyframes.FirstOrDefault(x => x.Time == duration - 1);

                if (endKeyframe == null)
                {
                    keyframes.Add(new TKeyframeType()
                    {
                        Time = (ushort)(duration - 1),
                        Value = EMP_Keyframe.GetInterpolatedKeyframe(keyframes, duration - 1, interpolate)
                    });
                }

                //Delete all keyframes beyond the declared duration
                for (int i = keyframes.Count - 1; i >= 0; i--)
                {
                    if (keyframes[i].Time >= duration)
                    {
                        if (loop && duration < 101 && keyframes[i].Time == duration - 1)
                        {
                            //Special case: A non-looped animation with a duration less than a particles life will default to the last keyframe once the animation finishes, not the last keyframe within the duration.
                            //To account for this, this last keyframe should be kept on the animation and inserted on the time directly after the previous keyframe (of the delcared duration).
                            keyframes[i].Time = duration;
                        }
                        else
                        {
                            keyframes.RemoveAt(i);
                        }
                    }
                }
            }

            return keyframes;

        }


        /*
         * EMG file is now parsed directly
        internal int CalculateEmgSize(int EmgOffset, int mainEntryOffset)
        {
            int Type0_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 140);
            int Type1_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 148);
            int SubEntry_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 156);
            int NextEntry_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 152);

            if (Type0_Offset != 0)
            {
                return (Type0_Offset + 136) - EmgOffset;
            }
            else if (Type1_Offset != 0)
            {
                return Type1_Offset - EmgOffset;
            }
            else if (SubEntry_Offset != 0)
            {
                return SubEntry_Offset - EmgOffset;
            }
            else if (NextEntry_Offset != 0)
            {
                return NextEntry_Offset - EmgOffset;
            }
            else
            {
                //If no subdata, child particleEffect or nextParticleEffect. (So, the last Particle Effect in the current hierarchy... but NOT the last in the file)
                int relativeOffset = (CurrentEntryEnd - mainEntryOffset);
                return relativeOffset - EmgOffset;
            }
        }
        */
    }
}
