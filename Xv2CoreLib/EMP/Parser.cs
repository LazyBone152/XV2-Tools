using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;
using System.Collections;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EMP
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        public EMP_File empFile { get; private set; } = new EMP_File();
        bool writeXml = false;

        //info
        int totalMainEntries;
        int totalTextureEntries;
        int mainEntryOffset;
        int textureEntryOffset;
        int currentEntryEnd = 0;

        private ParserMode parserMode = ParserMode.Xml;

        public Parser(string location, bool _writeXml = true)
        {
            writeXml = _writeXml;
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            totalMainEntries = BitConverter.ToInt16(rawBytes, 12);
            totalTextureEntries = BitConverter.ToInt16(rawBytes, 14);
            mainEntryOffset = BitConverter.ToInt32(rawBytes, 16);
            textureEntryOffset = BitConverter.ToInt32(rawBytes, 20);
            Parse();

            if (_writeXml)
                WriteXmlFile();
        }

        public Parser(byte[] _bytes, ParserMode _parserMode)
        {
            parserMode = _parserMode;
            rawBytes = _bytes;
            totalMainEntries = BitConverter.ToInt16(rawBytes, 12);
            totalTextureEntries = BitConverter.ToInt16(rawBytes, 14);
            mainEntryOffset = BitConverter.ToInt32(rawBytes, 16);
            textureEntryOffset = BitConverter.ToInt32(rawBytes, 20);
            Parse();
        }


        public EMP_File GetEmpFile()
        {
            return empFile;
        }

        private void Parse()
        {
            empFile.Version = (VersionEnum)BitConverter.ToUInt16(rawBytes, 8);

            empFile.ParticleEffects = new AsyncObservableCollection<ParticleEffect>();
            if (totalMainEntries > 0)
            {
                int finalParticleEffectEnd = (textureEntryOffset != 0) ? textureEntryOffset : rawBytes.Length - 1;
                empFile.ParticleEffects = SortEffect(mainEntryOffset, finalParticleEffectEnd);
            }

            empFile.Textures = new AsyncObservableCollection<EMP_TextureDefinition>();
            if (totalTextureEntries > 0)
            {
                for (int i = 0; i < totalTextureEntries; i++)
                {
                    empFile.Textures.Add(ParseEmbEntry(textureEntryOffset, i));

                    if (empFile.Version == VersionEnum.SDBH)
                    {
                        textureEntryOffset += 36;
                    }
                    else
                    {
                        textureEntryOffset += 28;
                    }
                }
            }

            //Only do the following if in tool mode
            if(parserMode == ParserMode.Tool)
            {
                LinkTextureEntries();
            }
        }

        private AsyncObservableCollection<ParticleEffect> SortEffect(int entryOffset, int nextParticleEffectOffset_Abs)
        {

            AsyncObservableCollection<ParticleEffect> effectEntries = new AsyncObservableCollection<ParticleEffect>();

            int i = 0;
            while (true)
            {
                int SubEntry_Offset = BitConverter.ToInt32(rawBytes, entryOffset + 156);
                int NextEntry_Offset = BitConverter.ToInt32(rawBytes, entryOffset + 152);
                
                //Get entryEndOffset (relative)
                int nextEntry = (SubEntry_Offset != 0) ? SubEntry_Offset : NextEntry_Offset;
                currentEntryEnd = (nextEntry != 0) ? nextEntry + entryOffset : nextParticleEffectOffset_Abs;
                int nextEntryOffset = (NextEntry_Offset != 0) ? NextEntry_Offset + entryOffset : nextParticleEffectOffset_Abs;

                effectEntries.Add(ParseEffect(entryOffset));

                if (SubEntry_Offset > 0)
                {
                    effectEntries[i].ChildParticleEffects = SortEffect(SubEntry_Offset + entryOffset, nextEntryOffset);
                }

                entryOffset += NextEntry_Offset;
                i++;
                if (NextEntry_Offset == 0)
                {
                    break;
                }
            }

            return effectEntries;

        }

        private ParticleEffect ParseEffect(int mainEntryOffset)
        {
            ParticleEffect newEffect = ParticleEffect.GetNew((parserMode == ParserMode.Tool) ? true : false);

            //Flags and Offsets for extra data
            int FLAG_36 = rawBytes[mainEntryOffset + 36];
            int FLAG_37 = rawBytes[mainEntryOffset + 37];
            int Type0_Count = BitConverter.ToInt16(rawBytes, mainEntryOffset + 138);
            int Type0_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 140) + 136 + mainEntryOffset;
            int Type1_Count = BitConverter.ToInt16(rawBytes, mainEntryOffset + 144);
            int Type1_Offset = BitConverter.ToInt32(rawBytes, mainEntryOffset + 148) + mainEntryOffset;

            //Main Entry values
            newEffect.Component_Type = ParticleEffect.GetComponentType(new int[2] { FLAG_36, FLAG_37 });
            newEffect.Name = StringEx.GetString(rawBytes, mainEntryOffset, false, StringEx.EncodingType.ASCII, 32);

            BitArray compositeBits_I_32 = new BitArray(new byte[1] { rawBytes[mainEntryOffset + 32] });
            BitArray compositeBits_I_33 = new BitArray(new byte[1] { rawBytes[mainEntryOffset + 33] });
            BitArray compositeBits_I_34 = new BitArray(new byte[1] { rawBytes[mainEntryOffset + 34] });


            newEffect.I_32_0 = compositeBits_I_32[0];
            newEffect.I_32_1 = compositeBits_I_32[1];
            newEffect.I_32_2 = compositeBits_I_32[2];
            newEffect.I_32_3 = compositeBits_I_32[3];
            newEffect.I_32_4 = compositeBits_I_32[4];
            newEffect.I_32_5 = compositeBits_I_32[5];
            newEffect.I_32_6 = compositeBits_I_32[6];
            newEffect.I_32_7 = compositeBits_I_32[7];
            newEffect.I_33_0 = compositeBits_I_33[0];
            newEffect.I_33_1 = compositeBits_I_33[1];
            newEffect.I_33_2 = compositeBits_I_33[2];
            newEffect.I_33_3 = compositeBits_I_33[3];
            newEffect.I_33_4 = compositeBits_I_33[4];
            newEffect.I_33_5 = compositeBits_I_33[5];
            newEffect.I_33_6 = compositeBits_I_33[6];
            newEffect.I_33_7 = compositeBits_I_33[7];
            newEffect.I_34_0 = compositeBits_I_34[0];
            newEffect.I_34_1 = compositeBits_I_34[1];
            newEffect.I_34_2 = compositeBits_I_34[2];
            newEffect.I_34_3 = compositeBits_I_34[3];
            newEffect.I_34_4 = compositeBits_I_34[4];
            newEffect.I_34_5 = compositeBits_I_34[5];
            newEffect.I_34_6 = compositeBits_I_34[6];
            newEffect.I_34_7 = compositeBits_I_34[7];
            newEffect.I_35 = (ParticleEffect.AutoOrientationType)rawBytes[mainEntryOffset + 35];
            newEffect.I_38 = BitConverter.ToInt16(rawBytes, mainEntryOffset + 38);
            newEffect.I_40 = BitConverter.ToInt16(rawBytes, mainEntryOffset + 40);
            newEffect.I_42 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 42);
            newEffect.I_44 = rawBytes[mainEntryOffset + 44];
            newEffect.I_45 = rawBytes[mainEntryOffset + 45];
            newEffect.I_46 = rawBytes[mainEntryOffset + 46];
            newEffect.I_47 = rawBytes[mainEntryOffset + 47];
            newEffect.I_48 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 48);
            newEffect.I_50 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 50);
            newEffect.I_52 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 52);
            newEffect.I_54 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 54);
            newEffect.I_56 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 56);
            newEffect.I_58 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 58);
            newEffect.I_60 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 60);
            newEffect.I_62 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 62);

            newEffect.F_64 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 64);
            newEffect.F_68 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 68);
            newEffect.F_72 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 72);
            newEffect.F_76 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 76);
            newEffect.F_80 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 80);
            newEffect.F_84 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 84);
            newEffect.F_88 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 88);
            newEffect.F_92 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 92);
            newEffect.F_96 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 96);
            newEffect.F_100 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 100);
            newEffect.F_104 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 104);
            newEffect.F_108 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 108);
            newEffect.F_112 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 112);
            newEffect.F_116 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 116);
            newEffect.F_120 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 120);
            newEffect.F_124 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 124);
            newEffect.F_128 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 128);
            newEffect.F_132 = BitConverter.ToSingle(rawBytes, mainEntryOffset + 132);

            newEffect.I_136 = BitConverter.ToUInt16(rawBytes, mainEntryOffset + 136);

            if (Type0_Count > 0 || Type1_Count > 0)
            {

                //Type0
                if (Type0_Count > 0)
                {
                    newEffect.Type_0 = new AsyncObservableCollection<Type0>();

                    for (int a = 0; a < Type0_Count; a++)
                    {
                        int idx = newEffect.Type_0.Count();

                        newEffect.Type_0.Add(new Type0()
                        {
                            I_01_b = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[Type0_Offset + 1])[1]),
                            I_02 = BitConverter_Ex.ToBoolean(rawBytes, Type0_Offset + 2),
                            I_03 = rawBytes[Type0_Offset + 3],
                            F_04 = BitConverter.ToSingle(rawBytes, Type0_Offset + 4),
                            I_08 = BitConverter.ToInt16(rawBytes, Type0_Offset + 8),
                            Keyframes = ParseKeyframes<Type0_Keyframe>(BitConverter.ToInt16(rawBytes, Type0_Offset + 10), BitConverter.ToInt32(rawBytes, Type0_Offset + 12) + Type0_Offset)
                        });

                        newEffect.Type_0[idx].SetParameters(rawBytes[Type0_Offset + 0], Int4Converter.ToInt4(rawBytes[Type0_Offset + 1])[0], newEffect.IsScale2Enabled());

                        Type0_Offset += 16;
                    }
                }

                //Type1
                if (Type1_Count > 0)
                {
                    newEffect.Type_1 = new AsyncObservableCollection<Type1_Header>();

                    for (int a = 0; a < Type1_Count; a++)
                    {
                        int entryCount = BitConverter.ToInt16(rawBytes, Type1_Offset + 2);
                        int entryOffset = BitConverter.ToInt32(rawBytes, Type1_Offset + 4) + Type1_Offset;

                        newEffect.Type_1.Add(new Type1_Header());
                        newEffect.Type_1[a].I_00 = rawBytes[Type1_Offset];
                        newEffect.Type_1[a].I_01 = rawBytes[Type1_Offset + 1];
                        newEffect.Type_1[a].Entries = new AsyncObservableCollection<Type0>();

                        for (int d = 0; d < entryCount; d++)
                        {
                            int subEntryCount = BitConverter.ToInt16(rawBytes, entryOffset + 10);

                            newEffect.Type_1[a].Entries.Add(new Type0());

                            newEffect.Type_1[a].Entries[d].SetParameters(rawBytes[entryOffset + 0], Int4Converter.ToInt4(rawBytes[entryOffset + 1])[0], newEffect.IsScale2Enabled());
                            newEffect.Type_1[a].Entries[d].I_01_b = BitConverter_Ex.ToBoolean(Int4Converter.ToInt4(rawBytes[entryOffset + 1])[1]);
                            newEffect.Type_1[a].Entries[d].I_02 = BitConverter_Ex.ToBoolean(rawBytes, entryOffset + 2);
                            newEffect.Type_1[a].Entries[d].I_03 = rawBytes[entryOffset + 3];
                            newEffect.Type_1[a].Entries[d].F_04 = BitConverter.ToSingle(rawBytes, entryOffset + 4);

                            newEffect.Type_1[a].Entries[d].I_08 = BitConverter.ToInt16(rawBytes, entryOffset + 8);
                            newEffect.Type_1[a].Entries[d].Keyframes = ParseKeyframes<Type0_Keyframe>(BitConverter.ToInt16(rawBytes, entryOffset + 10), BitConverter.ToInt32(rawBytes, entryOffset + 12) + entryOffset);

                            entryOffset += 16;
                        }

                        Type1_Offset += 8;
                    }

                }

            }
            //Extra Parts
            //If no extra parts exist, this code wont execute
            if (FLAG_37 != 0)
            {
                switch (FLAG_37)
                {
                    case 1:
                        switch (FLAG_36)
                        {
                            case 0:
                                newEffect.FloatPart_00_01 = ParseFloatPart8<FloatPart_0_1>(mainEntryOffset + 160);
                                break;
                            case 1:
                                newEffect.FloatPart_01_01 = ParseFloatPart4<FloatPart_1_1>(mainEntryOffset + 160);
                                break;
                            case 2:
                                newEffect.FloatPart_02_01 = ParseFloatPart_2_1(mainEntryOffset + 160);
                                break;
                            case 3:
                                newEffect.FloatPart_03_01 = ParseFloatPart_3_1(mainEntryOffset + 160);
                                break;
                        }
                        break;
                    case 2:
                        switch (FLAG_36)
                        {
                            case 0:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                newEffect.FloatPart_00_02 = ParseFloatPart4<FloatPart_0_2>(mainEntryOffset + 160 + 112);
                                break;
                            case 1:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                break;
                            case 2:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                newEffect.FloatPart_02_02 = ParseFloatPart8<FloatPart_2_2>(mainEntryOffset + 160 + 112);
                                break;
                            case 3:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                newEffect.Type_Struct3 = ParseStruct3(mainEntryOffset + 160 + 112, mainEntryOffset);
                                break;
                            case 4:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                newEffect.Type_Model = ParseModelStruct(mainEntryOffset + 160 + 112, mainEntryOffset);
                                break;
                            case 5:
                                newEffect.Type_Texture = ParseTexturePart(mainEntryOffset + 160, mainEntryOffset, textureEntryOffset);
                                newEffect.Type_Struct5 = ParseStruct5(mainEntryOffset + 160 + 112, mainEntryOffset);
                                break;
                        }
                        break;
                }
            }


            return newEffect;
        }

        private EMP_TextureDefinition ParseEmbEntry(int textureOffset, int index)
        {
            EMP_TextureDefinition embEntry = EMP_TextureDefinition.GetNew();
            int subDataType = BitConverter.ToInt16(rawBytes, textureOffset + 10);
            embEntry.TextureType = (EMP_TextureDefinition.TextureAnimationType)subDataType;
            embEntry.I_00 = rawBytes[textureOffset + 0];
            embEntry.I_01 = rawBytes[textureOffset + 1];
            embEntry.I_02 = rawBytes[textureOffset + 2];
            embEntry.I_03 = rawBytes[textureOffset + 3];
            embEntry.I_04 = rawBytes[textureOffset + 4];
            embEntry.I_05 = rawBytes[textureOffset + 5];
            embEntry.I_06_byte = (EMP_TextureDefinition.TextureRepitition)rawBytes[textureOffset + 6];
            embEntry.I_07_byte = (EMP_TextureDefinition.TextureRepitition)rawBytes[textureOffset + 7];
            embEntry.I_08 = rawBytes[textureOffset + 8];
            embEntry.I_09 = rawBytes[textureOffset + 9];
            embEntry.EntryIndex = index;

            switch (subDataType)
            {
                case 0:
                    if (empFile.Version == VersionEnum.SDBH)
                    {
                        var keyframes = AsyncObservableCollection<SubData_2_Entry>.Create();
                        keyframes.Add(new SubData_2_Entry()
                        {
                            I_00 = -1,
                            ScrollX = BitConverter.ToSingle(rawBytes, textureOffset + 12),
                            ScrollY = BitConverter.ToSingle(rawBytes, textureOffset + 16),
                            ScaleX = BitConverter.ToSingle(rawBytes, textureOffset + 20),
                            ScaleY = BitConverter.ToSingle(rawBytes, textureOffset + 24),
                            F_20 = BitConverter.ToSingle(rawBytes, textureOffset + 28).ToString("0.0#######"),
                            F_24 = BitConverter.ToSingle(rawBytes, textureOffset + 32).ToString("0.0#######")
                        });

                        embEntry.SubData2 = new ScrollAnimation()
                        {
                            useSpeedInsteadOfKeyFrames = false,
                            Keyframes = keyframes
                        };
                    }
                    else
                    {
                        var keyframes = AsyncObservableCollection<SubData_2_Entry>.Create();
                        keyframes.Add(new SubData_2_Entry()
                        {
                            I_00 = -1,
                            ScrollX = BitConverter.ToSingle(rawBytes, textureOffset + 12),
                            ScrollY = BitConverter.ToSingle(rawBytes, textureOffset + 16),
                            ScaleX = BitConverter.ToSingle(rawBytes, textureOffset + 20),
                            ScaleY = BitConverter.ToSingle(rawBytes, textureOffset + 24),
                        });

                        embEntry.SubData2 = new ScrollAnimation()
                        {
                            useSpeedInsteadOfKeyFrames = false,
                            Keyframes = keyframes
                        };
                    }
                    break;
                case 1:
                    embEntry.SubData2 = new ScrollAnimation()
                    {
                        useSpeedInsteadOfKeyFrames = true,
                        ScrollSpeed_U = BitConverter.ToSingle(rawBytes, textureOffset + 12),
                        ScrollSpeed_V = BitConverter.ToSingle(rawBytes, textureOffset + 16),
                    };
                    break;
                case 2:
                    embEntry.SubData2 = new ScrollAnimation()
                    {
                        useSpeedInsteadOfKeyFrames = false,
                        Keyframes = AsyncObservableCollection<SubData_2_Entry>.Create()
                    };
                    int count = BitConverter.ToInt16(rawBytes, textureOffset + 22);
                    int subEntryOffset = BitConverter.ToInt32(rawBytes, textureOffset + 24) + textureOffset + 12;
                    for (int i = 0; i < count; i++)
                    {
                        embEntry.SubData2.Keyframes.Add(ParseSubData2Entry(subEntryOffset));
                        if (empFile.Version == VersionEnum.SDBH)
                        {
                            subEntryOffset += 28;
                        }
                        else
                        {
                            subEntryOffset += 20;
                        }
                    }
                    break;
            }

            return embEntry;

        }

        private SubData_2_Entry ParseSubData2Entry(int offset)
        {
            if (empFile.Version == VersionEnum.SDBH)
            {
                return new SubData_2_Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    ScrollX = BitConverter.ToSingle(rawBytes, offset + 4),
                    ScrollY = BitConverter.ToSingle(rawBytes, offset + 8),
                    ScaleX = BitConverter.ToSingle(rawBytes, offset + 12),
                    ScaleY = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20).ToString("0.0######"),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24).ToString("0.0######"),
                };
            }
            else
            {
                return new SubData_2_Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    ScrollX = BitConverter.ToSingle(rawBytes, offset + 4),
                    ScrollY = BitConverter.ToSingle(rawBytes, offset + 8),
                    ScaleX = BitConverter.ToSingle(rawBytes, offset + 12),
                    ScaleY = BitConverter.ToSingle(rawBytes, offset + 16)
                };
            }
        }

        private void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(EMP_File));
            serializer.SerializeToFile(empFile, saveLocation + ".xml");

        }


        //Type Parsers
        private AsyncObservableCollection<TKeyframeType> ParseKeyframes<TKeyframeType>(int keyframeCount, int keyframeListOffset) where TKeyframeType : IKeyframe, new()
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

            AsyncObservableCollection<TKeyframeType> keyframes = AsyncObservableCollection<TKeyframeType>.Create();

            for (int i = 0; i < keyframeCount; i++)
            {
                keyframes.Add(new TKeyframeType()
                {
                    Index = BitConverter.ToInt16(rawBytes, keyframeListOffset),
                    Float = BitConverter.ToSingle(rawBytes, floatOffset)
                });
                keyframeListOffset += 2;
                floatOffset += 4;
            }

            return keyframes;

        }

        private TexturePart ParseTexturePart(int TextureOffset, int _mainEntryOffset, int _textureEntryOffset)
        {
            TexturePart newTexture = new TexturePart();

            int TexturePointer = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, TextureOffset + 20) + _mainEntryOffset) + _mainEntryOffset;

            newTexture.I_00 = rawBytes[TextureOffset + 0];
            newTexture.I_01 = rawBytes[TextureOffset + 1];
            newTexture.I_02 = rawBytes[TextureOffset + 2];
            newTexture.I_03 = rawBytes[TextureOffset + 3];
            newTexture.I_08 = BitConverter.ToInt32(rawBytes, TextureOffset + 8);
            newTexture.I_12 = BitConverter.ToInt32(rawBytes, TextureOffset + 12);
            newTexture.I_16 = BitConverter.ToUInt16(rawBytes, TextureOffset + 16);
            newTexture.F_04 = BitConverter.ToSingle(rawBytes, TextureOffset + 4);
            newTexture.F_24 = BitConverter.ToSingle(rawBytes, TextureOffset + 24);
            newTexture.F_28 = BitConverter.ToSingle(rawBytes, TextureOffset + 28);
            newTexture.F_32 = BitConverter.ToSingle(rawBytes, TextureOffset + 32);
            newTexture.F_36 = BitConverter.ToSingle(rawBytes, TextureOffset + 36);
            newTexture.F_40 = BitConverter.ToSingle(rawBytes, TextureOffset + 40);
            newTexture.F_44 = BitConverter.ToSingle(rawBytes, TextureOffset + 44);
            newTexture.Color1 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, TextureOffset + 48), BitConverter.ToSingle(rawBytes, TextureOffset + 52), BitConverter.ToSingle(rawBytes, TextureOffset + 56), BitConverter.ToSingle(rawBytes, TextureOffset + 60));
            newTexture.Color_Variance = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, TextureOffset + 64), BitConverter.ToSingle(rawBytes, TextureOffset + 68), BitConverter.ToSingle(rawBytes, TextureOffset + 72), BitConverter.ToSingle(rawBytes, TextureOffset + 76));
            newTexture.Color2 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, TextureOffset + 80), BitConverter.ToSingle(rawBytes, TextureOffset + 84), BitConverter.ToSingle(rawBytes, TextureOffset + 88), BitConverter.ToSingle(rawBytes, TextureOffset + 92));
            
            newTexture.F_96 = BitConverter.ToSingle(rawBytes, TextureOffset + 96);
            newTexture.F_100 = BitConverter.ToSingle(rawBytes, TextureOffset + 100);
            newTexture.F_104 = BitConverter.ToSingle(rawBytes, TextureOffset + 104);
            newTexture.F_108 = BitConverter.ToSingle(rawBytes, TextureOffset + 108);

            if (TexturePointer != 0)
            {
                int _tempOffset = _textureEntryOffset;
                newTexture.TextureIndex = new AsyncObservableCollection<int>();

                for (int e = 0; e < BitConverter.ToInt16(rawBytes, TextureOffset + 18); e++)
                {
                    for (int a = 0; a < totalTextureEntries; a++)
                    {
                        //Null entry
                        if (TexturePointer == 0)
                        {
                            newTexture.TextureIndex.Add(-1);
                            break;
                        }

                        if (TexturePointer == _tempOffset)
                        {
                            newTexture.TextureIndex.Add(a);
                            break;
                        }
                        else
                        {
                            if (empFile.Version == VersionEnum.SDBH)
                            {
                                _tempOffset += 36;
                            }
                            else
                            {
                                _tempOffset += 28;
                            }
                        }
                    }
                }
            }

            return newTexture;
        }

        private List<float> ParseFloatList(int FloatListOffset, int count)
        {
            List<float> newFloatList = new List<float>();

            for (int i = 0; i < count * 4; i += 4)
            {
                newFloatList.Add(BitConverter.ToSingle(rawBytes, FloatListOffset + i));
            }

            return newFloatList;
        }

        private Struct3 ParseStruct3(int StructOffset, int mainEntryOffset)
        {
            Struct3 _struct3 = new Struct3()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, StructOffset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, StructOffset + 2),
                I_04 = BitConverter.ToUInt16(rawBytes, StructOffset + 4),
                I_08 = BitConverter.ToUInt16(rawBytes, StructOffset + 8),
                I_10 = BitConverter.ToUInt16(rawBytes, StructOffset + 10),
                FloatList = AsyncObservableCollection<Struct3_Entries>.Create()
            };

            int count = BitConverter.ToInt16(rawBytes, StructOffset + 6) + 1;
            int listOffset = BitConverter.ToInt32(rawBytes, StructOffset + 12) + mainEntryOffset;

            for (int i = 0; i < count; i++)
            {
                _struct3.FloatList.Add(new Struct3_Entries()
                {
                    F_00 = BitConverter.ToSingle(rawBytes, listOffset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, listOffset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, listOffset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, listOffset + 12)
                });

                listOffset += 16;
            }

            return _struct3;
        }

        private Struct5 ParseStruct5(int StructOffset, int mainEntryOffset)
        {
            Struct5 _struct5 = new Struct5()
            {
                F_00 = BitConverter.ToSingle(rawBytes, StructOffset + 0),
                F_04 = BitConverter.ToSingle(rawBytes, StructOffset + 4),
                F_08 = BitConverter.ToSingle(rawBytes, StructOffset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, StructOffset + 12),
            };
            StructOffset += 16;

            _struct5.I_18 = (Struct5.AutoOrientation)BitConverter.ToUInt16(rawBytes, StructOffset + 2);
            _struct5.I_24 = BitConverter.ToUInt16(rawBytes, StructOffset + 8);
            _struct5.I_26 = BitConverter.ToUInt16(rawBytes, StructOffset + 10);
            _struct5.I_28 = BitConverter.ToUInt16(rawBytes, StructOffset + 12);
            _struct5.I_30 = BitConverter.ToUInt16(rawBytes, StructOffset + 14);

            _struct5.FloatList = AsyncObservableCollection<Struct5_Entries>.Create();

            int count = BitConverter.ToInt16(rawBytes, StructOffset + 0) + 1;
            int listOffset = BitConverter.ToInt32(rawBytes, StructOffset + 4) + mainEntryOffset;

            for (int i = 0; i < count; i++)
            {
                _struct5.FloatList.Add(new Struct5_Entries()
                {
                    F_00 = BitConverter.ToSingle(rawBytes, listOffset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, listOffset + 4)
                });

                listOffset += 8;
            }

            return _struct5;
        }

        private ModelStruct ParseModelStruct(int StructOffset, int mainEntryOffset)
        {
            ModelStruct modelStruct = new ModelStruct();
            modelStruct.F_00 = BitConverter.ToSingle(rawBytes, StructOffset + 0);
            modelStruct.F_04 = BitConverter.ToSingle(rawBytes, StructOffset + 4);
            modelStruct.F_08 = BitConverter.ToSingle(rawBytes, StructOffset + 8);
            modelStruct.F_12 = BitConverter.ToSingle(rawBytes, StructOffset + 12);
            StructOffset += 16;

            modelStruct.F_16 = BitConverter.ToSingle(rawBytes, StructOffset + 0);
            modelStruct.F_20 = BitConverter.ToSingle(rawBytes, StructOffset + 4);
            modelStruct.F_24 = BitConverter.ToSingle(rawBytes, StructOffset + 8);
            modelStruct.F_28 = BitConverter.ToSingle(rawBytes, StructOffset + 12);
            modelStruct.I_32 = BitConverter.ToUInt32(rawBytes, StructOffset + 16);
            modelStruct.I_40 = BitConverter.ToUInt32(rawBytes, StructOffset + 24);
            modelStruct.I_44 = BitConverter.ToUInt32(rawBytes, StructOffset + 28);

            int emgOffset = BitConverter.ToInt32(rawBytes, StructOffset + 20) + mainEntryOffset;
            int emgSize = CalculateEmgSize(BitConverter.ToInt32(rawBytes, StructOffset + 20), mainEntryOffset);


            byte[] EmgFile = rawBytes.GetRange(emgOffset, emgSize);
            modelStruct.emgBytes = EmgFile.ToList();

            return modelStruct;
        }

        //FloatPart parsers
        private T ParseFloatPart4<T>(int offset) where T : IFloatSize4, new()
        {
            T FloatParts = new T();

            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 12);

            return FloatParts;
        }

        private T ParseFloatPart8<T>(int offset) where T : IFloatSize8, new()
        {
            T FloatParts = new T();

            FloatParts.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 16);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 20);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 24);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 28);

            return FloatParts;
        }

        private T ParseFloatPart12Type0<T>(int offset) where T : IFloatSize12Type0, new()
        {
            T FloatParts = new T();

            FloatParts.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 16);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 20);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 24);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 28);
            FloatParts.F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
            FloatParts.F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
            FloatParts.F_40 = BitConverter.ToSingle(rawBytes, offset + 40);
            FloatParts.F_44 = BitConverter.ToSingle(rawBytes, offset + 44);

            return FloatParts;
        }

        private T ParseFloatPart12Type1<T>(int offset) where T : IFloatSize12Type1, new()
        {
            T FloatParts = new T();

            FloatParts.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 16);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 20);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 24);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 28);
            FloatParts.F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
            FloatParts.F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
            FloatParts.I_40 = FloatPart4Type1_GetShape(BitConverter.ToInt32(rawBytes, offset + 40));
            FloatParts.F_44 = BitConverter.ToSingle(rawBytes, offset + 44);

            return FloatParts;
        }

        private FloatPart_2_1 ParseFloatPart_2_1(int offset)
        {
            FloatPart_2_1 FloatParts = new FloatPart_2_1();

            FloatParts.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 16);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 20);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 24);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 28);
            FloatParts.F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
            FloatParts.F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
            FloatParts.I_40 = (FloatPart_2_1.Shape)BitConverter.ToInt32(rawBytes, offset + 40);
            FloatParts.F_44 = BitConverter.ToSingle(rawBytes, offset + 44);

            return FloatParts;
        }

        private FloatPart_3_1 ParseFloatPart_3_1(int offset)
        {
            FloatPart_3_1 FloatParts = new FloatPart_3_1();

            FloatParts.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            FloatParts.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            FloatParts.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            FloatParts.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            FloatParts.F_16 = BitConverter.ToSingle(rawBytes, offset + 16);
            FloatParts.F_20 = BitConverter.ToSingle(rawBytes, offset + 20);
            FloatParts.F_24 = BitConverter.ToSingle(rawBytes, offset + 24);
            FloatParts.F_28 = BitConverter.ToSingle(rawBytes, offset + 28);
            FloatParts.F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
            FloatParts.F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
            FloatParts.I_40 = (FloatPart_3_1.Shape)BitConverter.ToInt32(rawBytes, offset + 40);
            FloatParts.F_44 = BitConverter.ToSingle(rawBytes, offset + 44);

            return FloatParts;
        }


        //Utility

        private int GetTexturePartSize(TexturePart texture)
        {
            int size = 112;

            if (texture.TextureIndex != null)
            {
                for (int i = 0; i < texture.TextureIndex.Count(); i++)
                {
                    size += 4;
                }
            }

            return size;
        }

        private int CalculateEmgSize(int EmgOffset, int mainEntryOffset)
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
                int relativeOffset = (currentEntryEnd - mainEntryOffset);
                return relativeOffset - EmgOffset;
            }
        }

        public static string FloatPart4Type1_GetShape(int _shape)
        {
            switch (_shape)
            {
                case 0:
                    return "Circle";
                case 1:
                    return "Square";
                default:
                    return _shape.ToString();
            }
        }

        //Tool Mode
        private void LinkTextureEntries()
        {
            foreach(var texture in empFile.Textures)
            {
                LinkTextureEntries_Recursive(empFile.ParticleEffects, texture.EntryIndex, new TextureEntry_Ref() { TextureRef = texture });
            }

        }

        private void LinkTextureEntries_Recursive(IList<ParticleEffect> particleEffects, int id, TextureEntry_Ref textureRef)
        {
            foreach(var particleEffect in particleEffects)
            {
                if(particleEffect.Type_Texture != null)
                {
                    //Add default empty texture entries
                    while (particleEffect.Type_Texture.TextureEntryRef.Count < 2)
                        particleEffect.Type_Texture.TextureEntryRef.Add(new TextureEntry_Ref());

                    int idx = particleEffect.Type_Texture.TextureIndex.IndexOf(id);

                    if (idx != -1)
                    {
                        particleEffect.Type_Texture.TextureEntryRef[idx] = new TextureEntry_Ref() { TextureRef = textureRef.TextureRef };
                    }

                }

                if(particleEffect.ChildParticleEffects != null)
                {
                    LinkTextureEntries_Recursive(particleEffect.ChildParticleEffects, id, textureRef);
                }
            }
        }

    }

}
