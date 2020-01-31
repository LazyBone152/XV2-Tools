using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.EMP
{
    public class Deserializer
    {
        string saveLocation;
        EMP_File empFile;
        public List<byte> bytes = new List<byte>();
        private bool fromXml = false;

        //Offset storage
        List<List<int>> EmbTextureOffsets = new List<List<int>>();
        List<List<int>> EmbTextureOffsets_Minus = new List<List<int>>();

        private ParserMode parserMode = ParserMode.Xml;

        public Deserializer(string location)
        {
            fromXml = true;
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(EMP_File), YAXSerializationOptions.DontSerializeNullObjects);
            empFile = (EMP_File)serializer.DeserializeFromFile(location);
            EmbTextureList_Setup();

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMP_File _empFile, string location, ParserMode _parserMode)
        {
            parserMode = _parserMode;
            saveLocation = location;
            empFile = _empFile;
            EmbTextureList_Setup();

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMP_File _empFile, ParserMode _parserMode)
        {
            parserMode = _parserMode;
            empFile = _empFile;
            EmbTextureList_Setup();
            Write();
        }


        private void EmbTextureList_Setup()
        {
            int maxID = 0;

            if (empFile.Textures != null)
            {
                foreach (EMP_TextureDefinition e in empFile.Textures)
                {
                    if (e.EntryIndex > maxID)
                    {
                        maxID = e.EntryIndex;
                    }
                }
            }



            for (int i = 0; i < maxID + 1; i++)
            {
                EmbTextureOffsets.Add(new List<int>());
                EmbTextureOffsets_Minus.Add(new List<int>());
            }

        }

        private void Write()
        {
            //Prep
            if(parserMode == ParserMode.Tool)
            {
                RegenerateTextureIds(); //Reset Texture IDs to match Index. (Texture ID is for XMLs only. In tool mode it can be ignored as we use direct refs instead.)
                LinkTextureRefs(empFile.ParticleEffects); //Link the texture refs to the generated texture IDs
            }


            //Count
            int particleEffectCount = (empFile.ParticleEffects != null) ? empFile.ParticleEffects.Count() : 0;
            int textureEntryCount = (empFile.Textures != null) ? empFile.Textures.Count() : 0;


            //Header
            bytes.AddRange(BitConverter.GetBytes(EMP_File.EMP_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)24));
            bytes.AddRange(BitConverter.GetBytes((ushort)empFile.Version));
            bytes.AddRange(BitConverter.GetBytes((ushort)0));
            bytes.AddRange(BitConverter.GetBytes((short)particleEffectCount));
            bytes.AddRange(BitConverter.GetBytes((short)textureEntryCount));
            bytes.AddRange(BitConverter.GetBytes(32));
            bytes.AddRange(new byte[12]);

            if (empFile.ParticleEffects != null)
            {
                SortEntry(empFile.ParticleEffects);
            }

            if (empFile.Textures != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 20);
                WriteEmbEntries(empFile.Textures);
            }


        }

        private void SortEntry(ObservableCollection<ParticleEffect> effectEntries)
        {
            for (int i = 0; i < effectEntries.Count(); i++)
            {
                int relativeOffsetToEntryStart = bytes.Count() + CalculatePaddingAfterMainEntry();
                int nextEntryOffset_ToReplace = bytes.Count() + 152 + CalculatePaddingAfterMainEntry();
                int nextSubEntryOffset_ToReplace = bytes.Count() + 156 + CalculatePaddingAfterMainEntry();
                WriteEffect(effectEntries[i]);

                if(effectEntries[i].ChildParticleEffects != null)
                {
                    if (effectEntries[i].ChildParticleEffects.Count > 0)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - relativeOffsetToEntryStart + CalculatePaddingAfterMainEntry()), nextSubEntryOffset_ToReplace);
                        SortEntry(effectEntries[i].ChildParticleEffects);
                    }
                }

                if (i != effectEntries.Count() - 1)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - relativeOffsetToEntryStart + CalculatePaddingAfterMainEntry()), nextEntryOffset_ToReplace);
                }
            }

        }

        private void WriteEffect(ParticleEffect effect)
        {
            ValidateParticleEffectEntry(effect);
            //Add padding until on new line
            bytes.AddRange(new byte[CalculatePaddingAfterMainEntry()]);

            //Get Data_Flags and validate components
            int[] Data_Flags = GetDataFlags(effect);


            //Effect Name
            if (effect.Name.Length > 32)
            {
                throw new InvalidDataException(String.Format("ParticleEffect Name ({0}) exceeds the maximum allowed length of 32.", effect.Name));
            }
            bytes.AddRange(Encoding.ASCII.GetBytes(effect.Name));

            int remainingBytesAfterName = 32 - effect.Name.Length;
            bytes.AddRange(new byte[remainingBytesAfterName]);

            //Offsets
            int mainEntryOffset = bytes.Count() - 32;
            int currentRelativeOffset = 160;
            int texturePartOffset = 0;
            int meshPartOffset = 0;

            //Type counts
            int Type0_Count = (effect.Type_0 != null) ? effect.Type_0.Count() : 0;
            int Type1_Count = (effect.Type_1 != null) ? effect.Type_1.Count() : 0;
            int SubEntry_Count = (effect.ChildParticleEffects != null) ? effect.ChildParticleEffects.Count() : 0;


            //Base Entry
            BitArray compositeBits_I_32 = new BitArray(new bool[8] { effect.I_32_0, effect.I_32_1, effect.I_32_2, effect.I_32_3, effect.I_32_4, effect.I_32_5, effect.I_32_6, effect.I_32_7 });
            BitArray compositeBits_I_33 = new BitArray(new bool[8] { effect.I_33_0, effect.I_33_1, effect.I_33_2, effect.I_33_3, effect.I_33_4, effect.I_33_5, effect.I_33_6, effect.I_33_7, });
            BitArray compositeBits_I_34 = new BitArray(new bool[8] { effect.I_34_0, effect.I_34_1, effect.I_34_2, effect.I_34_3, effect.I_34_4, effect.I_34_5, effect.I_34_6, effect.I_34_7, });

            bytes.AddRange(new byte[6] { Utils.ConvertToByte(compositeBits_I_32), Utils.ConvertToByte(compositeBits_I_33), Utils.ConvertToByte(compositeBits_I_34), (byte)effect.I_35, (byte)Data_Flags[0], (byte)Data_Flags[1] });

            bytes.AddRange(BitConverter.GetBytes(effect.I_38));
            bytes.AddRange(BitConverter.GetBytes(effect.I_40));
            bytes.AddRange(BitConverter.GetBytes(effect.I_42));
            bytes.Add((effect.I_44));
            bytes.Add((effect.I_45));
            bytes.Add(effect.I_46);
            bytes.Add(effect.I_47);
            bytes.AddRange(BitConverter.GetBytes(effect.I_48));
            bytes.AddRange(BitConverter.GetBytes(effect.I_50));
            bytes.AddRange(BitConverter.GetBytes(effect.I_52));
            bytes.AddRange(BitConverter.GetBytes(effect.I_54));
            bytes.AddRange(BitConverter.GetBytes(effect.I_56));
            bytes.AddRange(BitConverter.GetBytes(effect.I_58));
            bytes.AddRange(BitConverter.GetBytes(effect.I_60));
            bytes.AddRange(BitConverter.GetBytes(effect.I_62));
            bytes.AddRange(BitConverter.GetBytes(effect.F_64));
            bytes.AddRange(BitConverter.GetBytes(effect.F_68));
            bytes.AddRange(BitConverter.GetBytes(effect.F_72));
            bytes.AddRange(BitConverter.GetBytes(effect.F_76));
            bytes.AddRange(BitConverter.GetBytes(effect.F_80));
            bytes.AddRange(BitConverter.GetBytes(effect.F_84));
            bytes.AddRange(BitConverter.GetBytes(effect.F_88));
            bytes.AddRange(BitConverter.GetBytes(effect.F_92));
            bytes.AddRange(BitConverter.GetBytes(effect.F_96));
            bytes.AddRange(BitConverter.GetBytes(effect.F_100));
            bytes.AddRange(BitConverter.GetBytes(effect.F_104));
            bytes.AddRange(BitConverter.GetBytes(effect.F_108));
            bytes.AddRange(BitConverter.GetBytes(effect.F_112));
            bytes.AddRange(BitConverter.GetBytes(effect.F_116));
            bytes.AddRange(BitConverter.GetBytes(effect.F_120));
            bytes.AddRange(BitConverter.GetBytes(effect.F_124));
            bytes.AddRange(BitConverter.GetBytes(effect.F_128));
            bytes.AddRange(BitConverter.GetBytes(effect.F_132));

            bytes.AddRange(BitConverter.GetBytes(effect.I_136));
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(Type0_Count)));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(Type1_Count)));
            bytes.AddRange(BitConverter.GetBytes(Convert.ToUInt16(SubEntry_Count)));
            bytes.AddRange(new byte[12]);


            switch (Data_Flags[1])
            {
                case 0:
                    break;
                case 1:
                    switch (Data_Flags[0])
                    {
                        case 0:
                            currentRelativeOffset += WriteFloatPart8(effect.FloatPart_00_01);
                            break;
                        case 1:
                            currentRelativeOffset += WriteFloatPart4(effect.FloatPart_01_01);
                            break;
                        case 2:
                            currentRelativeOffset += WriteFloatPart_2_1(effect.FloatPart_02_01);
                            break;
                        case 3:
                            currentRelativeOffset += WriteFloatPart_3_1(effect.FloatPart_03_01);
                            break;
                    }
                    break;
                case 2:
                    switch (Data_Flags[0])
                    {
                        case 0:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            currentRelativeOffset += WriteFloatPart4(effect.FloatPart_00_02);
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            break;
                        case 1:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            break;
                        case 2:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            currentRelativeOffset += WriteFloatPart8(effect.FloatPart_02_02);
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            break;
                        case 3:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            WriteStruct3(effect.Type_Struct3, currentRelativeOffset);
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            break;
                        case 4:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            meshPartOffset += WriteModelStruct(effect.Type_Model, currentRelativeOffset + 16);
                            currentRelativeOffset += 32;
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            currentRelativeOffset += EmbedMesh(effect.Type_Model, meshPartOffset, mainEntryOffset);
                            break;
                        case 5:
                            texturePartOffset = WriteTextureData(effect.Type_Texture);
                            currentRelativeOffset += 112;
                            WriteStruct5(effect.Type_Struct5, currentRelativeOffset);
                            currentRelativeOffset += WriteTexturePartOffsets(effect.Type_Texture, texturePartOffset, mainEntryOffset);
                            break;
                    }
                    break;
            }





            //Types
            if (Type0_Count > 0)
            {
                WriteType0(effect.Type_0, mainEntryOffset + 140, mainEntryOffset, effect.IsScale2Enabled());
            }
            if (Type1_Count > 0)
            {
                WriteType1(effect.Type_1, mainEntryOffset + 148, mainEntryOffset, effect.IsScale2Enabled());
            }



        }

        //Writers (Section 1)
        private int WriteTextureData(TexturePart texture)
        {
            bytes.AddRange(new byte[4] { texture.I_00, texture.I_01, texture.I_02, texture.I_03 });
            bytes.AddRange(BitConverter.GetBytes(texture.F_04));
            bytes.AddRange(BitConverter.GetBytes(texture.I_08));
            bytes.AddRange(BitConverter.GetBytes(texture.I_12));
            bytes.AddRange(BitConverter.GetBytes(texture.I_16));
            bytes.AddRange(BitConverter.GetBytes((ushort)texture.TextureIndex.Count()));
            int textureOffset = bytes.Count();
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(texture.F_24));
            bytes.AddRange(BitConverter.GetBytes(texture.F_28));
            bytes.AddRange(BitConverter.GetBytes(texture.F_32));
            bytes.AddRange(BitConverter.GetBytes(texture.F_36));
            bytes.AddRange(BitConverter.GetBytes(texture.F_40));
            bytes.AddRange(BitConverter.GetBytes(texture.F_44));
            bytes.AddRange(BitConverter.GetBytes(texture.F_48));
            bytes.AddRange(BitConverter.GetBytes(texture.F_52));
            bytes.AddRange(BitConverter.GetBytes(texture.F_56));
            bytes.AddRange(BitConverter.GetBytes(texture.F_60));
            bytes.AddRange(BitConverter.GetBytes(texture.F_64));
            bytes.AddRange(BitConverter.GetBytes(texture.F_68));
            bytes.AddRange(BitConverter.GetBytes(texture.F_72));
            bytes.AddRange(BitConverter.GetBytes(texture.F_76));
            bytes.AddRange(BitConverter.GetBytes(texture.F_80));
            bytes.AddRange(BitConverter.GetBytes(texture.F_84));
            bytes.AddRange(BitConverter.GetBytes(texture.F_88));
            bytes.AddRange(BitConverter.GetBytes(texture.F_92));
            bytes.AddRange(BitConverter.GetBytes(texture.F_96));
            bytes.AddRange(BitConverter.GetBytes(texture.F_100));
            bytes.AddRange(BitConverter.GetBytes(texture.F_104));
            bytes.AddRange(BitConverter.GetBytes(texture.F_108));

            return textureOffset;

        }

        private int WriteTexturePartOffsets(TexturePart texture, int offset, int mainEntryOffset)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset), offset);
            int size = 0;

            foreach (var i in texture.TextureIndex)
            {
                EmbTextureOffsets[i].Add(bytes.Count());
                EmbTextureOffsets_Minus[i].Add(mainEntryOffset);
                bytes.AddRange(new byte[4]);
                size += 4;
            }

            return size;
        }

        private int WriteStruct3(Struct3 struct3, int offset)
        {
            int size = 16;
            int indexCount = struct3.FloatList.Count() - 1;

            bytes.AddRange(BitConverter.GetBytes(struct3.I_00));
            bytes.AddRange(BitConverter.GetBytes(struct3.I_02));
            bytes.AddRange(BitConverter.GetBytes(struct3.I_04));
            bytes.AddRange(BitConverter.GetBytes((ushort)indexCount));
            bytes.AddRange(BitConverter.GetBytes(struct3.I_08));
            bytes.AddRange(BitConverter.GetBytes(struct3.I_10));
            bytes.AddRange(BitConverter.GetBytes(offset + 16));

            foreach (Struct3_Entries e in struct3.FloatList)
            {
                bytes.AddRange(BitConverter.GetBytes(e.F_00));
                bytes.AddRange(BitConverter.GetBytes(e.F_04));
                bytes.AddRange(BitConverter.GetBytes(e.F_08));
                bytes.AddRange(BitConverter.GetBytes(e.F_12));
                size += 16;
            }

            return size;
        }

        private int WriteStruct5(Struct5 struct5, int offset)
        {
            bytes.AddRange(BitConverter.GetBytes(struct5.F_00));
            bytes.AddRange(BitConverter.GetBytes(struct5.F_04));
            bytes.AddRange(BitConverter.GetBytes(struct5.F_08));
            bytes.AddRange(BitConverter.GetBytes(struct5.F_12));
            offset += 16;

            int size = 16;
            int indexCount = struct5.FloatList.Count() - 1;

            bytes.AddRange(BitConverter.GetBytes((ushort)indexCount));
            bytes.AddRange(BitConverter.GetBytes((short)struct5.I_18));
            bytes.AddRange(BitConverter.GetBytes(offset + 16));
            bytes.AddRange(BitConverter.GetBytes(struct5.I_24));
            bytes.AddRange(BitConverter.GetBytes(struct5.I_26));
            bytes.AddRange(BitConverter.GetBytes(struct5.I_28));
            bytes.AddRange(BitConverter.GetBytes(struct5.I_30));

            foreach (Struct5_Entries e in struct5.FloatList)
            {
                bytes.AddRange(BitConverter.GetBytes(e.F_00));
                bytes.AddRange(BitConverter.GetBytes(e.F_04));
                size += 4;
            }
            return size;
        }

        private int WriteModelStruct(ModelStruct modelStruct, int offset)
        {
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_00));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_04));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_08));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_12));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_16));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_20));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_24));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.F_28));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.I_32));
            int modelOffset = bytes.Count();
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(modelStruct.I_40));
            bytes.AddRange(BitConverter.GetBytes(modelStruct.I_44));

            return modelOffset;
        }

        private int EmbedMesh(ModelStruct modelStruct, int offsetToReplace, int mainEntryOffset)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset), offsetToReplace);
            byte[] mesh = null;

            mesh = modelStruct.emgBytes.ToArray();

            bytes.AddRange(mesh);

            return mesh.Count();
        }

        private int WriteFloatArray(float[] array)
        {
            int size = array.Count() * 4;

            foreach (float f in array)
            {
                bytes.AddRange(BitConverter.GetBytes(f));
            }

            return size;

        }

        private void WriteType0(ObservableCollection<Type0> type0, int Type0_Offset, int mainEntryOffset, bool scale2)
        {

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset - 136), Type0_Offset);

            List<int> entryOffsets = new List<int>();


            for (int i = 0; i < type0.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(type0[i].GetParameters(scale2)));
                bytes.AddRange(new byte[2] { BitConverter_Ex.GetBytes(type0[i].I_02), type0[i].I_03 });
                bytes.AddRange(BitConverter.GetBytes(type0[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(type0[i].I_08));
                bytes.AddRange(BitConverter.GetBytes((short)type0[i].Keyframes.Count()));
                entryOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);

                //Sort keyframes
                if(type0[i].Keyframes != null)
                {
                    var sortedList = type0[i].Keyframes.ToList();
                    sortedList.Sort((x, y) => x.Index - y.Index);
                    type0[i].Keyframes = new ObservableCollection<Type0_Keyframe>(sortedList);
                }
            }

            for (int i = 0; i < type0.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - entryOffsets[i] + 12), entryOffsets[i]);

                float keyframeSize = 0;

                //Keyframes
                foreach (var e in type0[i].Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(e.Index));
                    keyframeSize += 2;
                }
                if (Math.Floor(keyframeSize / 4) != keyframeSize / 4)
                {
                    bytes.AddRange(new byte[2]);
                }

                //Floats
                foreach (var e in type0[i].Keyframes)
                {
                    bytes.AddRange(BitConverter.GetBytes(e.Float));
                }

                //Index List
                if (type0[i].Keyframes.Count() > 1)
                {
                    //Writing IndexList
                    bool specialCase_FirstKeyFrameIsNotZero = (type0[i].Keyframes[0].Index == 0) ? false : true;
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
                            thisFrameLength = type0[i].Keyframes[s].Index;
                            thisFrameLength += type0[i].Keyframes[s + 1].Index - type0[i].Keyframes[s].Index;
                        }
                        else
                        {
                            thisFrameLength = type0[i].Keyframes[s + 1].Index - type0[i].Keyframes[s].Index;
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

        private void WriteType1(ObservableCollection<Type1_Header> type1, int Type1_Offset, int mainEntryOffset, bool scale2)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - mainEntryOffset), Type1_Offset);

            //Offsets to replace
            List<int> HeaderOffsets = new List<int>();

            for (int i = 0; i < type1.Count(); i++)
            {
                int subEntryCount = (type1[i].Entries == null) ? 0 : type1[i].Entries.Count();
                bytes.AddRange(new byte[2] { type1[i].I_00, type1[i].I_01 });
                bytes.AddRange(BitConverter.GetBytes((ushort)subEntryCount));
                HeaderOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < type1.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - HeaderOffsets[i] + 4), HeaderOffsets[i]);
                List<int> EntryOffsets = new List<int>();

                if (type1[i].Entries != null)
                {
                    for (int a = 0; a < type1[i].Entries.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(type1[i].Entries[a].GetParameters(scale2)));
                        bytes.AddRange(new byte[2] { BitConverter_Ex.GetBytes(type1[i].Entries[a].I_02), type1[i].Entries[a].I_03 });
                        bytes.AddRange(BitConverter.GetBytes(type1[i].Entries[a].F_04));
                        bytes.AddRange(BitConverter.GetBytes(type1[i].Entries[a].I_08));
                        bytes.AddRange(BitConverter.GetBytes((ushort)type1[i].Entries[a].Keyframes.Count()));
                        EntryOffsets.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                    }

                    for (int a = 0; a < type1[i].Entries.Count(); a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - EntryOffsets[a] + 12), EntryOffsets[a]);

                        float keyframeSize = 0;

                        //Keyframes
                        foreach (var e in type1[i].Entries[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Index));
                            keyframeSize += 2;
                        }
                        if (Math.Floor(keyframeSize / 4) != keyframeSize / 4)
                        {
                            bytes.AddRange(new byte[2]);
                        }

                        //Floats
                        foreach (var e in type1[i].Entries[a].Keyframes)
                        {
                            bytes.AddRange(BitConverter.GetBytes(e.Float));
                        }

                        //Index List
                        if (type1[i].Entries[a].Keyframes.Count() > 1)
                        {
                            //Writing IndexList
                            bool specialCase_FirstKeyFrameIsNotZero = (type1[i].Entries[a].Keyframes[0].Index == 0) ? false : true;
                            float totalIndex = 0;
                            for (int s = 0; s < type1[i].Entries[a].Keyframes.Count(); s++)
                            {
                                int thisFrameLength = 0;
                                if (type1[i].Entries[a].Keyframes.Count() - 1 == s)
                                {
                                    thisFrameLength = 1;
                                }
                                else if (specialCase_FirstKeyFrameIsNotZero == true && s == 0)
                                {
                                    thisFrameLength = type1[i].Entries[a].Keyframes[s].Index;
                                    thisFrameLength += type1[i].Entries[a].Keyframes[s + 1].Index - type1[i].Entries[a].Keyframes[s].Index;
                                }
                                else
                                {
                                    thisFrameLength = type1[i].Entries[a].Keyframes[s + 1].Index - type1[i].Entries[a].Keyframes[s].Index;
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
        private void WriteEmbEntries(ObservableCollection<EMP_TextureDefinition> embEntries)
        {
            List<int> subData2Offsets_ToReplace = new List<int>();


            for (int i = 0; i < embEntries.Count(); i++)
            {

                //Filling in offsets
                for (int a = 0; a < EmbTextureOffsets[i].Count(); a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - EmbTextureOffsets_Minus[i][a]), EmbTextureOffsets[i][a]);
                }

                //getting subdata type, and defaulting it if it doesn't exist

                EMP_TextureDefinition.TextureAnimationType textureType = (fromXml) ? embEntries[i].CalculateTextureType() : embEntries[i].TextureType;

                bytes.AddRange(new byte[10] { embEntries[i].I_00, embEntries[i].I_01, embEntries[i].I_02, embEntries[i].I_03, embEntries[i].I_04, embEntries[i].I_05, (byte)embEntries[i].I_06_byte, (byte)embEntries[i].I_07_byte, embEntries[i].I_08, embEntries[i].I_09 });
                bytes.AddRange(BitConverter.GetBytes((ushort)textureType));


                switch (textureType)
                {
                    case EMP_TextureDefinition.TextureAnimationType.Static:
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[0].F_04));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[0].F_08));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[0].F_12));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[0].F_16));

                        if (empFile.Version == VersionEnum.SDBH)
                        {
                            embEntries[i].SubData2.Keyframes[0].SetDefaultValuesForSDBH();
                            bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].SubData2.Keyframes[0].F_20)));
                            bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].SubData2.Keyframes[0].F_24)));
                        }

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_TextureDefinition.TextureAnimationType.Speed:
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.ScrollSpeed_U));
                        bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.ScrollSpeed_V));
                        bytes.AddRange(new byte[8]);

                        if (empFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_TextureDefinition.TextureAnimationType.SpriteSheet:
                        bytes.AddRange(new byte[10]);
                        int animationCount = (embEntries[i].SubData2.Keyframes != null) ? embEntries[i].SubData2.Keyframes.Count() : 0;
                        bytes.AddRange(BitConverter.GetBytes((short)animationCount));

                        subData2Offsets_ToReplace.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);

                        if (empFile.Version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }
                        break;
                    default:
                        throw new InvalidDataException("Unknown EmbEntry.TextureAnimationType: " + textureType);
                }


            }

            for (int i = 0; i < embEntries.Count(); i++)
            {
                if (embEntries[i].SubData2 != null)
                {
                    EMP_TextureDefinition.TextureAnimationType textureType = (fromXml) ? embEntries[i].CalculateTextureType() : embEntries[i].TextureType;

                    if (textureType == EMP_TextureDefinition.TextureAnimationType.SpriteSheet)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - subData2Offsets_ToReplace[i] + 12), subData2Offsets_ToReplace[i]);

                        for (int a = 0; a < embEntries[i].SubData2.Keyframes.Count(); a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[a].I_00));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[a].F_04));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[a].F_08));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[a].F_12));
                            bytes.AddRange(BitConverter.GetBytes(embEntries[i].SubData2.Keyframes[a].F_16));

                            if (empFile.Version == VersionEnum.SDBH)
                            {
                                embEntries[i].SubData2.Keyframes[a].SetDefaultValuesForSDBH();
                                bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].SubData2.Keyframes[a].F_20)));
                                bytes.AddRange(BitConverter.GetBytes(float.Parse(embEntries[i].SubData2.Keyframes[a].F_24)));
                            }
                        }
                    }
                }
            }

        }


        //Float Part writers
        private int WriteFloatPart4<T>(T floatParts) where T : IFloatSize4, new()
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));

            return 16;
        }

        private int WriteFloatPart8<T>(T floatParts) where T : IFloatSize8, new()
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_00));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_04));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_08));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_12));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));

            return 32;
        }

        private int WriteFloatPart12Type0<T>(T floatParts) where T : IFloatSize12Type0, new()
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_00));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_04));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_08));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_12));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_32));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_36));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_40));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_44));

            return 48;
        }

        private int WriteFloatPart12Type1<T>(T floatParts) where T : IFloatSize12Type1, new()
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_00));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_04));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_08));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_12));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_32));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_36));
            bytes.AddRange(BitConverter.GetBytes(floatParts.GetShape()));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_44));

            return 48;
        }

        private int WriteFloatPart_2_1(FloatPart_2_1 floatParts)
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_00));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_04));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_08));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_12));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_32));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_36));
            bytes.AddRange(BitConverter.GetBytes((int)floatParts.I_40));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_44));

            return 48;
        }

        private int WriteFloatPart_3_1(FloatPart_3_1 floatParts)
        {
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_00));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_04));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_08));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_12));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_16));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_20));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_24));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_28));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_32));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_36));
            bytes.AddRange(BitConverter.GetBytes((int)floatParts.I_40));
            bytes.AddRange(BitConverter.GetBytes(floatParts.F_44));

            return 48;
        }

        //Utility
        private bool TextureScrollAnimationIsType0Check(ObservableCollection<SubData_2_Entry> keyframes)
        {
            if (keyframes != null)
            {
                if (keyframes.Count() == 1)
                {
                    if (keyframes[0].I_00 == -1)
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

        private void ValidateParticleEffectEntry(ParticleEffect effect)
        {
        }

        //Flag/Component Checking (implicit)

        private int[] GetDataFlags(ParticleEffect particleEffect)
        {

            if (DoesComponentExist(particleEffect, (particleEffect.GetDataFlagsFromDeclaredType())))
            {
                return GetDataFlagsFromComponentType(particleEffect);
            }


            if (particleEffect.FloatPart_00_01 != null)
            {
                DoMoreComponentsExist(particleEffect, "00_01");
                ConfirmTexturePartDoesNotExist(particleEffect, "VerticalDistribution_Component");
                return new int[2] { 0, 1 };
            }
            else if (particleEffect.FloatPart_01_01 != null)
            {
                DoMoreComponentsExist(particleEffect, "01_01");
                ConfirmTexturePartDoesNotExist(particleEffect, "SphericalDistribution_Component");
                return new int[2] { 1, 1 };
            }
            else if (particleEffect.FloatPart_02_01 != null)
            {
                DoMoreComponentsExist(particleEffect, "02_01");
                ConfirmTexturePartDoesNotExist(particleEffect, "ShapeDistribution1_Component");
                return new int[2] { 2, 1 };
            }
            else if (particleEffect.FloatPart_03_01 != null)
            {
                DoMoreComponentsExist(particleEffect, "03_01");
                ConfirmTexturePartDoesNotExist(particleEffect, "ShapeDistribution2_Component");
                return new int[2] { 3, 1 };
            }
            else if (particleEffect.FloatPart_00_02 != null)
            {
                DoMoreComponentsExist(particleEffect, "00_02");
                ConfirmTexturePartExist(particleEffect, "AutoOriented_Component");
                return new int[2] { 0, 2 };
            }
            else if (particleEffect.FloatPart_02_02 != null)
            {
                DoMoreComponentsExist(particleEffect, "02_02");
                ConfirmTexturePartExist(particleEffect, "Default_Component");
                return new int[2] { 2, 2 };
            }
            else if (particleEffect.Type_Struct3 != null)
            {
                DoMoreComponentsExist(particleEffect, "03_02");
                ConfirmTexturePartExist(particleEffect, "ConeExtrude_Component");
                return new int[2] { 3, 2 };
            }
            else if (particleEffect.Type_Model != null)
            {
                DoMoreComponentsExist(particleEffect, "04_02");
                ConfirmTexturePartExist(particleEffect, "Mesh_Component");
                return new int[2] { 4, 2 };
            }
            else if (particleEffect.Type_Struct5 != null)
            {
                DoMoreComponentsExist(particleEffect, "05_02");
                ConfirmTexturePartExist(particleEffect, "ShapeDraw_Component");
                return new int[2] { 5, 2 };
            }
            else
            {
                if (particleEffect.Type_Texture == null)
                {
                    return new int[2] { 0, 0 };
                }
                else
                {
                    return new int[2] { 1, 2 };
                }

            }
        }

        private void DoMoreComponentsExist(ParticleEffect particleEffect, string flags)
        {
            if (flags != "00_01" && particleEffect.FloatPart_00_01 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "01_01" && particleEffect.FloatPart_01_01 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "02_01" && particleEffect.FloatPart_02_01 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "03_01" && particleEffect.FloatPart_03_01 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "00_02" && particleEffect.FloatPart_00_02 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "02_02" && particleEffect.FloatPart_02_02 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "03_02" && particleEffect.Type_Struct3 != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "04_02" && particleEffect.Type_Model != null)
            {
                AnotherComponentExists(particleEffect);
            }
            if (flags != "05_02" && particleEffect.Type_Struct5 != null)
            {
                AnotherComponentExists(particleEffect);
            }
        }

        private void ConfirmTexturePartExist(ParticleEffect particleEffect, string type)
        {
            if (particleEffect.Type_Texture == null)
            {
                Console.WriteLine(String.Format("Error on ParticleEffect with the name: {0}\nA TexturePart is expected with this Component ({1}), but none was found!\n\nDeserialization failed.", particleEffect.Name, type));
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private void ConfirmTexturePartDoesNotExist(ParticleEffect particleEffect, string type)
        {
            if (particleEffect.Type_Texture != null)
            {
                Console.WriteLine(String.Format("Error on ParticleEffect with the name: {0}\nNo TexturePart is expected with this Component ({1}), but one was found!\n\nDeserialization failed.", particleEffect.Name, type));
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private void AnotherComponentExists(ParticleEffect particleEffect)
        {
            Console.WriteLine(String.Format("Error on ParticleEffect with the name: {0}\nThere are multiple Components present. The maximum allowed amount is 1.\n\nDeserialization failed.", particleEffect.Name));
            Console.ReadLine();
            Environment.Exit(0);
        }

        private bool DoesComponentExist(ParticleEffect particleEffect, string flags)
        {
            if (flags == "00_01" && particleEffect.FloatPart_00_01 != null)
            {
                return true;
            }
            else if (flags == "01_01" && particleEffect.FloatPart_01_01 != null)
            {
                return true;
            }
            else if (flags == "02_01" && particleEffect.FloatPart_02_01 != null)
            {
                return true;
            }
            else if (flags == "03_01" && particleEffect.FloatPart_03_01 != null)
            {
                return true;
            }
            else if (flags == "00_02" && particleEffect.FloatPart_00_02 != null && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "02_02" && particleEffect.FloatPart_02_02 != null && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "03_02" && particleEffect.Type_Struct3 != null && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "04_02" && particleEffect.Type_Model != null && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "05_02" && particleEffect.Type_Struct5 != null && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "01_02" && particleEffect.Type_Texture != null)
            {
                return true;
            }
            else if (flags == "00_00")
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //Component Checking (explicit)

        private int[] GetDataFlagsFromComponentType(ParticleEffect particleEffect)
        {

            switch (particleEffect.GetDataFlagsFromDeclaredType())
            {
                case "00_02":
                    return new int[2] { 0, 2 };
                case "01_02":
                    return new int[2] { 1, 2 };
                case "02_02":
                    return new int[2] { 2, 2 };
                case "03_02":
                    return new int[2] { 3, 2 };
                case "04_02":
                    return new int[2] { 4, 2 };
                case "05_02":
                    return new int[2] { 5, 2 };
                case "00_01":
                    return new int[2] { 0, 1 };
                case "01_01":
                    return new int[2] { 1, 1 };
                case "02_01":
                    return new int[2] { 2, 1 };
                case "03_01":
                    return new int[2] { 3, 1 };
                case "00_00":
                    return new int[2] { 0, 0 };
                default:
                    return null;
            }
        }

        private void ValidateComponents(ParticleEffect particleEffect, string _component)
        {
            if (_component == "00_02")
            {
                particleEffect.Type_Texture = ValidateTexturePart(particleEffect);
                particleEffect.FloatPart_00_02 = (particleEffect.FloatPart_00_02 != null) ? particleEffect.FloatPart_00_02 : new FloatPart_0_2();
            }
        }

        /// <summary>
        /// If TexturePart is null, create a new one and return it, otherwise return the already existing one.
        /// </summary>
        private TexturePart ValidateTexturePart(ParticleEffect particleEffect)
        {
            if (particleEffect.Type_Texture == null)
            {
                return new TexturePart();
            }
            else
            {
                return particleEffect.Type_Texture;
            }
        }

        //Tool Mode
        private void RegenerateTextureIds()
        {
            //Reset IDs to match Index
            for(int i = 0; i < empFile.Textures.Count; i++)
            {
                empFile.Textures[i].EntryIndex = i;
            }
        }

        private void LinkTextureRefs(ObservableCollection<ParticleEffect> particleEffects)
        {
            //Regenerate particleEffect.Type_Texture.TextureIndex to match the reference Texture entries

            foreach (var particleEffect in particleEffects)
            {
                if(particleEffect.Type_Texture != null)
                {
                    particleEffect.Type_Texture.TextureIndex.Clear();

                    foreach(var texture in particleEffect.Type_Texture.TextureEntryRef)
                    {
                        particleEffect.Type_Texture.TextureIndex.Add(texture.TextureRef.EntryIndex);
                    }
                }

                if(particleEffect.ChildParticleEffects != null)
                {
                    LinkTextureRefs(particleEffect.ChildParticleEffects);
                }
            }
        }

    }
}
