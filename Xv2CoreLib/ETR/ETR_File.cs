using System;
using System.Linq;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.HslColor;
using LB_Common.Numbers;

namespace Xv2CoreLib.ETR
{
    [Serializable]
    public class ETR_File
    {
        private const int ETR_SIGNATURE = 1381254435;

        public VersionEnum Version { get; set; } = VersionEnum.DBXV2;

        public AsyncObservableCollection<ETR_Node> Nodes { get; set; } = new AsyncObservableCollection<ETR_Node>();
        public AsyncObservableCollection<EMP_TextureSamplerDef> Textures { get; set; } = new AsyncObservableCollection<EMP_TextureSamplerDef>();

        #region LoadSave
        public static ETR_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static ETR_File Load(byte[] bytes)
        {
            ETR_File etrFile = new ETR_File();

            if (BitConverter.ToInt32(bytes, 0) != ETR_SIGNATURE)
                throw new InvalidDataException("#ETR file signature not found.");

            //Read header
            int nodeCount = BitConverter.ToInt16(bytes, 12);
            int textureCount = BitConverter.ToInt16(bytes, 14);
            int nodeOffset = BitConverter.ToInt32(bytes, 16);
            int textureOffset = BitConverter.ToInt32(bytes, 20);
            etrFile.Version = (VersionEnum)BitConverter.ToInt32(bytes, 8);

            //Read TextureSamplers
            if (textureCount > 0)
            {
                for (int i = 0; i < textureCount; i++)
                {
                    etrFile.Textures.Add(EMP_TextureSamplerDef.Read(bytes, textureOffset, i, VersionEnum.DBXV2));
                }
            }

            //Read nodes
            for(int i = 0; i < nodeCount; i++)
            {
                etrFile.Nodes.Add(ETR_Node.Read(bytes, nodeOffset, textureOffset, etrFile.Textures));
                nodeOffset += 176;
            }

            return etrFile;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            foreach (EMP_TextureSamplerDef texture in Textures)
            {
                texture.ClearOffsets();
            }

            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(ETR_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)24));
            bytes.AddRange(BitConverter.GetBytes((int)Version));
            bytes.AddRange(BitConverter.GetBytes((ushort)Nodes.Count));
            bytes.AddRange(BitConverter.GetBytes((ushort)Textures.Count));
            bytes.AddRange(BitConverter.GetBytes(24)); //Nodes offset. This will always be 24 since thats the header size.
            bytes.AddRange(BitConverter.GetBytes(0)); //Texture offset, to fill in later

            //Nodes
            foreach(ETR_Node node in Nodes)
            {
                int start = bytes.Count;

                if (EffectContainer.EepkToolInterlop.FullDecompile)
                {
                    node.CompileAllKeyframes();
                }

                if (node.ExtrudeSegements.Count == 0)
                {
                    //There should always be at least 1 segement
                    node.ExtrudeSegements.Add(new ConeExtrudePoint(1, 0, 0, 0));
                }

                bytes.AddRange(StringEx.WriteFixedSizeString(node.AttachBone, 32));
                bytes.AddRange(StringEx.WriteFixedSizeString(node.AttachBone2, 32));
                bytes.AddRange(BitConverter.GetBytes(node.Position.X));
                bytes.AddRange(BitConverter.GetBytes(node.Position.Y));
                bytes.AddRange(BitConverter.GetBytes(node.Position.Z));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(node.Rotation.X)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(node.Rotation.Y)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(node.Rotation.Z)));
                bytes.Add(node.I_88);
                bytes.Add((byte)(node.ExtrudeSegements.Count - 1));
                bytes.AddRange(BitConverter.GetBytes(node.TimeStartExtrude));
                bytes.Add(node.I_92);
                bytes.Add(node.SegementFrameStep);
                bytes.AddRange(BitConverter.GetBytes(node.ExtrudeDuration));
                bytes.AddRange(BitConverter.GetBytes((int)node.Flags));
                bytes.AddRange(BitConverter.GetBytes(node.I_100));
                bytes.AddRange(BitConverter.GetBytes(node.PositionExtrudeZ));
                bytes.AddRange(BitConverter.GetBytes(node.MaterialID));
                bytes.AddRange(BitConverter.GetBytes((ushort)node.GetTextureCount()));
                bytes.AddRange(BitConverter.GetBytes(0)); //Textures offset
                bytes.AddRange(BitConverter.GetBytes(0)); //Floats offset
                bytes.AddRange(BitConverter.GetBytes(node.Color1.Constant.R));
                bytes.AddRange(BitConverter.GetBytes(node.Color1.Constant.G));
                bytes.AddRange(BitConverter.GetBytes(node.Color1.Constant.B));
                bytes.AddRange(BitConverter.GetBytes(node.Color1_Transparency.Constant));
                bytes.AddRange(BitConverter.GetBytes(node.Color2.Constant.R));
                bytes.AddRange(BitConverter.GetBytes(node.Color2.Constant.G));
                bytes.AddRange(BitConverter.GetBytes(node.Color2.Constant.B));
                bytes.AddRange(BitConverter.GetBytes(node.Color2_Transparency.Constant));
                bytes.AddRange(BitConverter.GetBytes(node.Scale.Constant));
                bytes.AddRange(BitConverter.GetBytes(node.ExtrudeShapePoints.Count > 0 ? node.ExtrudeShapePoints.Count - 1 : 0));
                bytes.AddRange(BitConverter.GetBytes(node.HoldDuration));
                bytes.AddRange(BitConverter.GetBytes((ushort)node.Modifiers.Count));
                bytes.AddRange(BitConverter.GetBytes(0)); //Modifiers offset
                bytes.AddRange(BitConverter.GetBytes(node.I_168));
                bytes.AddRange(BitConverter.GetBytes((ushort)node.KeyframedValues.Count));
                bytes.AddRange(BitConverter.GetBytes(0)); //KeyframedValues offset

                if (bytes.Count - start != 176)
                    throw new Exception("ETR_File.Write: ETR_Node size is incorrect.");
            }

            //Node extra data
            foreach(ETR_Node node in Nodes)
            {
                int nodeOffset = GetNodeOffset(Nodes.IndexOf(node));

                //Keyframed Values
                if(node.KeyframedValues.Count > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - nodeOffset - 168), nodeOffset + 172);
                    WriteKeyframedValues(node.KeyframedValues, bytes);
                }

                //Modifiers
                if(node.Modifiers.Count > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - nodeOffset - 120), nodeOffset + 164);
                    int modifersStart = bytes.Count;

                    //Modifer definition (8 bytes)
                    foreach(EMP_Modifier modifier in node.Modifiers)
                    {
                        bytes.Add((byte)modifier.Type);
                        bytes.Add((byte)modifier.Flags);
                        bytes.AddRange(BitConverter.GetBytes((ushort)modifier.KeyframedValues.Count));
                        bytes.AddRange(BitConverter.GetBytes(0)); //Offset to keyframed values
                    }

                    //Write keyframes
                    foreach(EMP_Modifier modifier in node.Modifiers)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - modifersStart), modifersStart + (8 * node.Modifiers.IndexOf(modifier)) + 4);
                        WriteKeyframedValues(modifier.KeyframedValues, bytes);
                    }

                }

                //Textures
                int textureCount = node.GetTextureCount();

                if(textureCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - nodeOffset), nodeOffset + 112);

                    for(int i = 0; i < textureCount; i++)
                    {
                        node.TextureEntryRef[i].TextureRef?.AddOffset(bytes.Count, nodeOffset);
                        bytes.AddRange(BitConverter.GetBytes(0));
                    }
                }

                //Extrude Segements
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - nodeOffset), nodeOffset + 116);

                foreach(ConeExtrudePoint extrudeSegement in node.ExtrudeSegements)
                {
                    bytes.AddRange(BitConverter.GetBytes(extrudeSegement.WorldScaleFactor));
                    bytes.AddRange(BitConverter.GetBytes(extrudeSegement.WorldScaleAdd));
                    bytes.AddRange(BitConverter.GetBytes(extrudeSegement.WorldOffsetFactor));
                    bytes.AddRange(BitConverter.GetBytes(extrudeSegement.WorldOffsetFactor2));
                }

                foreach(ShapeDrawPoint shapePoint in node.ExtrudeShapePoints)
                {
                    bytes.AddRange(BitConverter.GetBytes(shapePoint.X));
                    bytes.AddRange(BitConverter.GetBytes(shapePoint.Y));
                }


            }

            //Textures
            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);

            EMP_TextureSamplerDef.Write(bytes, Textures, VersionEnum.DBXV2);

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        private static void WriteKeyframedValues(IList<EMP_KeyframedValue> KeyframedValues, List<byte> bytes)
        {
            int keyframesStart = bytes.Count;

            //Write keyframe value definitions (16 bytes)
            foreach (EMP_KeyframedValue value in KeyframedValues)
            {
                bytes.Add((byte)value.ETR_InterpolationType);
                bytes.Add(value.Parameter);
                bytes.Add(Int4Converter.GetByte(value.Component, BitConverter_Ex.GetBytes(value.Interpolate)));
                bytes.Add(9); //always 9
                bytes.AddRange(new byte[2]); //ushort, always 0
                bytes.AddRange(BitConverter.GetBytes((ushort)value.Keyframes.Count));
                bytes.AddRange(BitConverter.GetBytes(0)); //Time offset
                bytes.AddRange(BitConverter.GetBytes(0)); //Value offset
            }

            //Write actual values
            foreach (EMP_KeyframedValue value in KeyframedValues)
            {
                int keyframeIndex = KeyframedValues.IndexOf(value);
                int keyframeStart = keyframesStart + (16 * keyframeIndex);

                //Time
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - keyframeStart), keyframeStart + 8);

                foreach (EMP_Keyframe keyframe in value.Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(keyframe.Time));
                }

                //Pad values to 4-byte alignment
                bytes.AddRange(new byte[Utils.CalculatePadding(value.Keyframes.Count * 2, 4)]);

                //Values
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - keyframeStart), keyframeStart + 12);

                foreach (EMP_Keyframe keyframe in value.Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(keyframe.Value));
                }

                //Dummy values
                bytes.AddRange(new byte[204]); //No idea what these are, but they are apparantly not important since XenoXML just nulls them out too
            }
        }

        private static int GetNodeOffset(int nodeIndex)
        {
            return (nodeIndex * 176) + 24;
        }
        #endregion

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();

            foreach(var node in Nodes)
            {
                colors.AddRange(node.GetUsedColors());
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (ETR_Node node in Nodes)
            {
                node.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }
        }

        /// <summary>
        /// Rescales the entire ETR.
        /// </summary>
        public void ScaleETRParts(float scaleFactor, List<IUndoRedo> undos)
        {
            foreach(ETR_Node node in Nodes)
            {
                undos.AddRange(node.Scale.RescaleValue(scaleFactor));
            }
        }

    }

    [Serializable]
    public class ETR_Node : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public enum ExtrudeFlags : int
        {
            UVPauseOnHold = 0x100,
            UVPauseOnExtrude = 0x20000,
            DisplayMiddleSection = 0x10000,
            AutoOrientation = 0x1000000,
            NoDegrade = 0x4000000,
            DoublePointOfMiddleSection = 0x8000000
        }

        #region References
        private EmmMaterial _materialRef = null;
        public EmmMaterial MaterialRef
        {
            get
            {
                return _materialRef;
            }
            set
            {
                if (_materialRef != value)
                    _materialRef = value;
                NotifyPropertyChanged(nameof(MaterialRef));
            }
        }
        public AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef { get; set; } = new AsyncObservableCollection<TextureEntry_Ref>();

        #endregion

        public string AttachBone { get; set; } //Max 32 characters
        public string AttachBone2 { get; set; } //Max 32 characters
        public CustomVector4 Position { get; set; } = new CustomVector4();
        public CustomVector4 Rotation { get; set; } = new CustomVector4();

        public ushort TimeStartExtrude { get; set; }
        public byte SegementFrameStep { get; set; } //NumberFrameForNextSegment
        public ushort ExtrudeDuration { get; set; }
        public ushort HoldDuration { get; set; }
        public ExtrudeFlags Flags { get; set; }
        public float PositionExtrudeZ { get; set; }
        public KeyframedFloatValue Scale { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ETR_Scale);

        public ushort MaterialID { get; set; }
        public KeyframedColorValue Color1 { get; set; } = new KeyframedColorValue(1, 1, 1, KeyframedValueType.ETR_Color1);
        public KeyframedColorValue Color2 { get; set; } = new KeyframedColorValue(1, 1, 1, KeyframedValueType.ETR_Color2);
        public KeyframedFloatValue Color1_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ETR_Color1_Transparency);
        public KeyframedFloatValue Color2_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ETR_Color2_Transparency);

        public byte I_92 { get; set; }
        public byte I_88 { get; set; }
        public int I_100 { get; set; }
        public ushort I_168 { get; set; }

        public AsyncObservableCollection<ConeExtrudePoint> ExtrudeSegements { get; set; } = new AsyncObservableCollection<ConeExtrudePoint>();
        public AsyncObservableCollection<ShapeDrawPoint> ExtrudeShapePoints { get; set; } = new AsyncObservableCollection<ShapeDrawPoint>();
        public AsyncObservableCollection<EMP_KeyframedValue> KeyframedValues { get; set; } = new AsyncObservableCollection<EMP_KeyframedValue>();
        public AsyncObservableCollection<EMP_Modifier> Modifiers { get; set; } = new AsyncObservableCollection<EMP_Modifier>();

        #region LoadSave
        public static ETR_Node Read(byte[] bytes, int nodeOffset, int texturesOffset, IList<EMP_TextureSamplerDef> textures)
        {
            ETR_Node node = new ETR_Node();

            node.AttachBone = StringEx.GetString(bytes, nodeOffset, false, maxSize: 32);
            node.AttachBone2 = StringEx.GetString(bytes, nodeOffset + 32, false, maxSize: 32);
            node.Position.X = BitConverter.ToSingle(bytes, nodeOffset + 64);
            node.Position.Y = BitConverter.ToSingle(bytes, nodeOffset + 68);
            node.Position.Z = BitConverter.ToSingle(bytes, nodeOffset + 72);
            node.Rotation.X = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, nodeOffset + 76));
            node.Rotation.Y = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, nodeOffset + 80));
            node.Rotation.Z = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, nodeOffset + 84));
            node.I_88 = bytes[nodeOffset + 88];
            node.TimeStartExtrude = BitConverter.ToUInt16(bytes, nodeOffset + 90);
            node.I_92 = bytes[nodeOffset + 92];
            node.SegementFrameStep = bytes[nodeOffset + 93];
            node.ExtrudeDuration = BitConverter.ToUInt16(bytes, nodeOffset + 94);
            node.Flags = (ExtrudeFlags)BitConverter.ToInt32(bytes, nodeOffset + 96);
            node.I_100 = BitConverter.ToInt32(bytes, nodeOffset + 100);
            node.PositionExtrudeZ = BitConverter.ToSingle(bytes, nodeOffset + 104);
            node.MaterialID = BitConverter.ToUInt16(bytes, nodeOffset + 108);
            node.Color1.Constant.R = BitConverter.ToSingle(bytes, nodeOffset + 120);
            node.Color1.Constant.G = BitConverter.ToSingle(bytes, nodeOffset + 124);
            node.Color1.Constant.B = BitConverter.ToSingle(bytes, nodeOffset + 128);
            node.Color1_Transparency.Constant = BitConverter.ToSingle(bytes, nodeOffset + 132);
            node.Color2.Constant.R = BitConverter.ToSingle(bytes, nodeOffset + 136);
            node.Color2.Constant.G = BitConverter.ToSingle(bytes, nodeOffset + 140);
            node.Color2.Constant.B = BitConverter.ToSingle(bytes, nodeOffset + 144);
            node.Color2_Transparency.Constant = BitConverter.ToSingle(bytes, nodeOffset + 148);
            node.Scale.Constant = BitConverter.ToSingle(bytes, nodeOffset + 152);
            node.HoldDuration = BitConverter.ToUInt16(bytes, nodeOffset + 160);
            node.I_168 = BitConverter.ToUInt16(bytes, nodeOffset + 168);

            //Textures
            ushort textureSamplerCount = BitConverter.ToUInt16(bytes, nodeOffset + 110);
            int texturePointerListOffset = BitConverter.ToInt32(bytes, nodeOffset + 112) + nodeOffset;

            if (texturePointerListOffset != nodeOffset)
            {
                int textureEntrySize = EMP_TextureSamplerDef.GetSize(VersionEnum.DBXV2);

                for (int e = 0; e < textureSamplerCount; e++)
                {
                    int textureOffset = BitConverter.ToInt32(bytes, texturePointerListOffset + (4 * e));

                    if (textureOffset != 0)
                    {
                        int texIdx = (textureOffset + nodeOffset - texturesOffset) / textureEntrySize;
                        node.TextureEntryRef.Add(new TextureEntry_Ref(textures[texIdx]));
                    }
                    else
                    {
                        node.TextureEntryRef.Add(new TextureEntry_Ref(null));
                    }
                }
            }

            //Fill out texture list to size 2
            while (node.TextureEntryRef.Count < 2)
            {
                node.TextureEntryRef.Add(new TextureEntry_Ref());
            }

            //Extrude Segements
            byte extrudeSegementCount = (byte)(bytes[nodeOffset + 89] + 1);
            int extrudeSegementOffset = BitConverter.ToInt32(bytes, nodeOffset + 116) + nodeOffset;

            for(int i = 0; i < extrudeSegementCount; i++)
            {
                node.ExtrudeSegements.Add(new ConeExtrudePoint(bytes, extrudeSegementOffset));
                extrudeSegementOffset += 16;
            }

            //Extrude Points
            int pointsCount = BitConverter.ToInt32(bytes, nodeOffset + 156);

            if (pointsCount > 0)
            {
                //When points == 0, ETR has no points
                //When points > 0, ETR points is equal to count + 1
                pointsCount++;

                for(int i = 0; i < pointsCount; i++)
                {
                    node.ExtrudeShapePoints.Add(new ShapeDrawPoint(BitConverter.ToSingle(bytes, extrudeSegementOffset), BitConverter.ToSingle(bytes, extrudeSegementOffset + 4)));
                    extrudeSegementOffset += 8;
                }
            }

            //Keyframed Values
            ushort keyframedValuesCount = BitConverter.ToUInt16(bytes, nodeOffset + 170);
            int keyframedValuesOffset = BitConverter.ToInt32(bytes, nodeOffset + 172) + 168 + nodeOffset;

            for(int i = 0; i < keyframedValuesCount; i++)
            {
                node.KeyframedValues.Add(ParseKeyframedValue(bytes, keyframedValuesOffset, i));
            }

            //Modifers
            ushort modifiersCount = BitConverter.ToUInt16(bytes, nodeOffset + 162);
            int modifiersOffset = BitConverter.ToInt32(bytes, nodeOffset + 164) + 120 + nodeOffset;

            for(int i = 0; i < modifiersCount; i++)
            {
                EMP_Modifier modifer = new EMP_Modifier();
                modifer.Type = (EMP_Modifier.ModifierType)bytes[modifiersOffset];
                modifer.Flags = (EMP_Modifier.ModifierFlags)bytes[modifiersOffset + 1];

                ushort keyframeCount = BitConverter.ToUInt16(bytes, modifiersOffset + 2);
                int keyframeOffset = BitConverter.ToInt32(bytes, modifiersOffset + 4) + modifiersOffset;

                for(int a = 0; a < keyframeCount; a++)
                {
                    modifer.KeyframedValues.Add(ParseKeyframedValue(bytes, keyframeOffset, a));
                }

                node.Modifiers.Add(modifer);
                modifiersOffset += 8;
            }

            if (EffectContainer.EepkToolInterlop.FullDecompile)
            {
                //Decompile keyframes:
                node.Color1.DecompileKeyframes(node.GetKeyframedValues(0, 0, 1, 2));
                node.Color2.DecompileKeyframes(node.GetKeyframedValues(1, 0, 1, 2));
                node.Color1_Transparency.DecompileKeyframes(node.GetKeyframedValues(0, 3));
                node.Color2_Transparency.DecompileKeyframes(node.GetKeyframedValues(1, 3));
                node.Scale.DecompileKeyframes(node.GetKeyframedValues(2, 0));

                //Fix old scaled ETRs:
                //ShapePoints should be between -1 and 1, but the old ETR Scale feature rescaled these outside this range. This code will rescale these back within that range, and apply the scale to the actual scale value and its keyframes
                if(node.ExtrudeShapePoints.Count > 0)
                {
                    float[] maxDimensions = new float[4];
                    maxDimensions[0] = Math.Abs(node.ExtrudeShapePoints.Max(x => x.X));
                    maxDimensions[1] = Math.Abs(node.ExtrudeShapePoints.Max(x => x.Y));
                    maxDimensions[2] = Math.Abs(node.ExtrudeShapePoints.Min(x => x.X));
                    maxDimensions[3] = Math.Abs(node.ExtrudeShapePoints.Min(x => x.Y));
                    float scaleFactor = maxDimensions.Max();

                    if(scaleFactor > 1f)
                    {
                        foreach(ShapeDrawPoint point in node.ExtrudeShapePoints)
                        {
                            point.X /= scaleFactor;
                            point.Y /= scaleFactor;
                        }

                        node.Scale.Constant *= scaleFactor;

                        foreach(KeyframeFloatValue keyframe in node.Scale.Keyframes)
                        {
                            keyframe.Value *= scaleFactor;
                        }
                    }
                }

            }

            return node;
        }

        private static EMP_KeyframedValue ParseKeyframedValue(byte[] bytes, int keyframesOffset, int index)
        {
            int keyframeOffset = keyframesOffset + (16 * index);

            EMP_KeyframedValue value = new EMP_KeyframedValue();
            value.ETR_InterpolationType = (ETR_InterpolationType)bytes[keyframeOffset + 0];
            value.Parameter = bytes[keyframeOffset + 1];
            value.Component = Int4Converter.ToInt4(bytes[keyframeOffset + 2])[0];
            value.Interpolate = Convert.ToBoolean(Int4Converter.ToInt4(bytes[keyframeOffset + 2])[1]);
            //value.I_03 = bytes[offset + 3]; //Always 9
            //value.I_04 = BitConverter.ToUInt16(bytes, offset + 4); //Always 0

            ushort keyframeCount = BitConverter.ToUInt16(bytes, keyframeOffset + 6);
            int timeOffset = BitConverter.ToInt32(bytes, keyframeOffset + 8) + keyframeOffset;
            int valueOffset = BitConverter.ToInt32(bytes, keyframeOffset + 12) + keyframeOffset;

            for(int i = 0; i < keyframeCount; i++)
            {
                value.Keyframes.Add(new EMP_Keyframe(BitConverter.ToUInt16(bytes, timeOffset), BitConverter.ToSingle(bytes, valueOffset)));
                timeOffset += 2;
                valueOffset += 4;
            }

            return value;
        }

        public EMP_KeyframedValue[] GetKeyframedValues(int parameter, params int[] components)
        {
            EMP_KeyframedValue[] values = new EMP_KeyframedValue[components.Length];

            for (int i = 0; i < components.Length; i++)
            {
                EMP_KeyframedValue value = KeyframedValues.FirstOrDefault(x => x.Parameter == parameter && x.Component == components[i]);

                if (value != null)
                    values[i] = value;
                else
                    values[i] = EMP_KeyframedValue.Default;
            }

            return values;
        }

        internal void CompileAllKeyframes()
        {
            KeyframedValues.Clear();

            AddKeyframedValues(Color1.CompileKeyframes());
            AddKeyframedValues(Color2.CompileKeyframes());
            AddKeyframedValues(Color1_Transparency.CompileKeyframes());
            AddKeyframedValues(Color2_Transparency.CompileKeyframes());
            AddKeyframedValues(Scale.CompileKeyframes());
        }

        internal void AddKeyframedValues(EMP_KeyframedValue[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    if (KeyframedValues.Any(x => x.Parameter == values[i].Parameter && x.Component == values[i].Component))
                    {
                        throw new Exception($"ETR_File: KeyframedValue already exists (parameter = {values[i].Parameter}, component = {values[i].Component})");
                    }

                    KeyframedValues.Add(values[i]);
                }
            }
        }

        /// <summary>
        /// Returns the amount of textures to write back to binary. Any nulls at the end will be removed, but nulls inbetween will be preserved.
        /// </summary>
        internal int GetTextureCount()
        {
            for (int i = TextureEntryRef.Count - 1; i >= 0; i--)
            {
                if (TextureEntryRef[i].TextureRef != null)
                    return i + 1;
            }

            return 0;
        }
        #endregion

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();

            RgbColor color1 = Color1.GetAverageColor();
            RgbColor color2 = Color2.GetAverageColor();

            if (!color1.IsWhiteOrBlack)
                colors.Add(color1);

            if (!color2.IsWhiteOrBlack)
                colors.Add(color2);

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            Color1.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            Color2.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
        }

    }

    public enum ETR_InterpolationType
    {
        ShapeStartToEnd = 2, //Interpolate down the length of the extruded parts, where the first keyframe is the end of the extrusion.
        Default = 1, //Interpolation is based on duration, where the first keyframe applies at the start, the last one at the end (so, like a regular animation)
        DefaultEnd = 0 //Interpolation is based on duration, and only starts at the end of the effect
    }
}
