using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BSA
{
    public class Deserializer
    {

        string saveLocation;
        BSA_File bsaFile;
        public List<byte> bytes = new List<byte>() { 35, 66, 83, 65, 254, 255, 24, 0 };

        //Offset lists
        int EntryCount { get; set; }
        List<int> MainEntryOffsets = new List<int>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BSA_File), YAXSerializationOptions.DontSerializeNullObjects);
            bsaFile = (BSA_File)serializer.DeserializeFromFile(location);
            bsaFile.SortEntries();
            EntryCount = (bsaFile.BSA_Entries != null) ? int.Parse(bsaFile.BSA_Entries[bsaFile.BSA_Entries.Count() - 1].Index) + 1 : 0;
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BSA_File _bsaFile, string location)
        {
            saveLocation = location;
            bsaFile = _bsaFile;
            bsaFile.SortEntries();
            EntryCount = (bsaFile.BSA_Entries != null) ? int.Parse(bsaFile.BSA_Entries[bsaFile.BSA_Entries.Count() - 1].Index) + 1 : 0;
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BSA_File _bsaFile)
        {
            bsaFile = _bsaFile;
            bsaFile.SortEntries();
            EntryCount = (bsaFile.BSA_Entries != null) ? int.Parse(bsaFile.BSA_Entries[bsaFile.BSA_Entries.Count() - 1].Index) + 1 : 0;
            Write();
        }

        private void Write()
        {
            bytes.AddRange(BitConverter.GetBytes(bsaFile.I_08));
            bytes.AddRange(BitConverter.GetBytes(bsaFile.I_16));
            bytes.AddRange(BitConverter.GetBytes((short)EntryCount));
            bytes.AddRange(BitConverter.GetBytes(24));

            //MainEntry pointer list
            for(int a = 0; a < EntryCount; a++)
            {
                for (int i = 0; i < bsaFile.BSA_Entries.Count(); i++)
                {
                    if(a == int.Parse(bsaFile.BSA_Entries[i].Index))
                    {
                        //Entry exists
                        MainEntryOffsets.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                        break;
                    } if (i == bsaFile.BSA_Entries.Count() - 1)
                    {
                        //Null entry
                        bytes.AddRange(new byte[4]);
                        break;
                    }

                }
            }

            for(int i = 0; i < bsaFile.BSA_Entries.Count(); i++)
            {

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), MainEntryOffsets[i]);

                int Unk1_Count = 0;
                int Unk2_Count = 0;
                if(bsaFile.BSA_Entries[i].SubEntries != null)
                {
                    Unk1_Count = (bsaFile.BSA_Entries[i].SubEntries.CollisionEntries != null) ? bsaFile.BSA_Entries[i].SubEntries.CollisionEntries.Count() : 0;
                    Unk2_Count = (bsaFile.BSA_Entries[i].SubEntries.ExpirationEntries != null) ? bsaFile.BSA_Entries[i].SubEntries.ExpirationEntries.Count() : 0;
                }


                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes((short)Unk1_Count));
                bytes.AddRange(BitConverter.GetBytes((short)Unk2_Count));
                int Unk1_Offset = bytes.Count();
                bytes.AddRange(new byte[4]);
                int Unk2_Offset = bytes.Count();
                bytes.AddRange(new byte[4]);
                bytes.Add(Int4Converter.GetByte(bsaFile.BSA_Entries[i].I_16_a, bsaFile.BSA_Entries[i].I_16_b, "BSA_Entry: I_16 > a", "BSA_Entry: I_16 > b"));
                bytes.Add(bsaFile.BSA_Entries[i].I_17);
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_18));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_22));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_24));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].Expires));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].ImpactProjectile));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].ImpactEnemy));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].ImpactGround));
                bytes.AddRange(BitConverter.GetBytes((short)BsaTypeCount(bsaFile.BSA_Entries[i])));
                int SubEntries_Offset = bytes.Count();
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_40[0]));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_40[1]));
                bytes.AddRange(BitConverter.GetBytes(bsaFile.BSA_Entries[i].I_40[2]));

                if(bsaFile.BSA_Entries[i].SubEntries != null)
                {
                    WriteUnk1(bsaFile.BSA_Entries[i].SubEntries.CollisionEntries, Unk1_Offset);
                    WriteUnk2(bsaFile.BSA_Entries[i].SubEntries.ExpirationEntries, Unk2_Offset);
                }

                WriteTypes(bsaFile.BSA_Entries[i], SubEntries_Offset);

            }
            

        }
        
        private void WriteUnk1(List<BSA_Collision> unk1, int offsetToFill)
        {
            if(unk1 != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 8), offsetToFill);

                for (int i = 0; i < unk1.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)unk1[i].EepkType));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].SkillID));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].EffectID));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(unk1[i].I_20));
                }
            }
            
            
        }

        private void WriteUnk2(List<BSA_Expiration> unk2, int offsetToFill)
        {
            if (unk2 != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToFill + 12), offsetToFill);

                for (int i = 0; i < unk2.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(unk2[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(unk2[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(unk2[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(unk2[i].I_06));
                }
            }

        }
        
        private void WriteTypes(BSA_Entry types, int offsetToFill)
        {
            if (BsaTypeCount(types) > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - offsetToFill + 36), offsetToFill);
                List<int> hdrOffset = new List<int>();
                List<int> dataOffset = new List<int>();
                List<int> typeList = new List<int>();

                //Type Headers
                if(types.Type0 != null)
                {
                    var results = WriteTypeHeader(0, types.Type0.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(0);
                }
                if (types.Type1 != null)
                {
                    var results = WriteTypeHeader(1, types.Type1.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(1);
                }
                if (types.Type2 != null)
                {
                    var results = WriteTypeHeader(2, types.Type2.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(2);
                }
                if (types.Type3 != null)
                {
                    var results = WriteTypeHeader(3, types.Type3.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(3);
                }
                if (types.Type4 != null)
                {
                    var results = WriteTypeHeader(4, types.Type4.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(4);
                }
                if (types.Type6 != null)
                {
                    var results = WriteTypeHeader(6, types.Type6.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(6);
                }
                if (types.Type7 != null)
                {
                    var results = WriteTypeHeader(7, types.Type7.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(7);
                }
                if (types.Type8 != null)
                {
                    var results = WriteTypeHeader(8, types.Type8.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(8);
                }
                if (types.Type10 != null)
                {
                    var results = WriteTypeHeader(8, types.Type10.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(10);
                }
                if (types.Type12 != null)
                {
                    var results = WriteTypeHeader(12, types.Type12.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(12);
                }
                if (types.Type13 != null)
                {
                    var results = WriteTypeHeader(13, types.Type13.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(13);
                }
                if (types.Type14 != null)
                {
                    var results = WriteTypeHeader(14, types.Type14.Count);
                    hdrOffset.Add(results[0]);
                    dataOffset.Add(results[1]);
                    typeList.Add(14);
                }

                //Type Data
                for (int i = 0; i < typeList.Count; i++)
                {
                    switch (typeList[i])
                    {
                        case 0:
                            WriteType0(types.Type0, hdrOffset[i], dataOffset[i]);
                            break;
                        case 1:
                            WriteType1(types.Type1, hdrOffset[i], dataOffset[i]);
                            break;
                        case 2:
                            WriteType2(types.Type2, hdrOffset[i], dataOffset[i]);
                            break;
                        case 3:
                            WriteType3(types.Type3, hdrOffset[i], dataOffset[i]);
                            break;
                        case 4:
                            WriteType4(types.Type4, hdrOffset[i], dataOffset[i]);
                            break;
                        case 6:
                            WriteType6(types.Type6, hdrOffset[i], dataOffset[i]);
                            break;
                        case 7:
                            WriteType7(types.Type7, hdrOffset[i], dataOffset[i]);
                            break;
                        case 8:
                            WriteType8(types.Type8, hdrOffset[i], dataOffset[i]);
                            break;
                        case 10:
                            WriteType10(types.Type10, hdrOffset[i], dataOffset[i]);
                            break;
                        case 12:
                            WriteType12(types.Type12, hdrOffset[i], dataOffset[i]);
                            break;
                        case 13:
                            WriteType13(types.Type13, hdrOffset[i], dataOffset[i]);
                            break;
                        case 14:
                            WriteType14(types.Type14, hdrOffset[i], dataOffset[i]);
                            break;
                    }

                }

            }

        }
        
        //Type Header
        private int[] WriteTypeHeader(int type, int count)
        {
            int[] results = new int[2];

            bytes.AddRange(BitConverter.GetBytes((short)type));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes((short)count));
            results[0] = bytes.Count();
            bytes.AddRange(new byte[4]);
            results[1] = bytes.Count();
            bytes.AddRange(new byte[4]);

            return results;
        }

        //Type Writers
        private void WriteType0(List<BSA_Type0> type, int hdrOffset, int dataOffset)
        {
            if(type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }
                
                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for(int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].BSA_EntryID));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                }

            }

        }

        private void WriteType1(List<BSA_Type1> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
                }

            }

        }

        private void WriteType2(List<BSA_Type2> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                }

            }

        }

        private void WriteType3(List<BSA_Type3> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.Add(Int4Converter.GetByte(type[i].I_06_a, type[i].I_06_b, "Hitbox I_06 > a", "Hitbox I_06 > b"));
                    bytes.Add(Int4Converter.GetByte(type[i].I_06_c, type[i].I_06_d, "Hitbox I_06 > c", "Hitbox I_06 > d"));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_48));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_50));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_52));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_54));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_56));
                    bytes.AddRange(BitConverter.GetBytes(type[i].FirstHit));
                    bytes.AddRange(BitConverter.GetBytes(type[i].MultipleHits));
                    bytes.AddRange(BitConverter.GetBytes(type[i].LastHit));
                }

            }

        }

        private void WriteType4(List<BSA_Type4> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_44));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_48));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_50));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_52));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_54));
                }

            }

        }

        private void WriteType6(List<BSA_Type6> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)type[i].EepkType));
                    bytes.AddRange(BitConverter.GetBytes(type[i].SkillID));
                    bytes.AddRange(BitConverter.GetBytes(type[i].EffectID));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                    bytes.AddRange(BitConverter.GetBytes((ushort)type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_10));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                }

            }

        }

        private void WriteType7(List<BSA_Type7> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)type[i].AcbType));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].CueId));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
                }

            }

        }

        private void WriteType8(List<BSA_Type8> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_20));
                }

            }

        }

        private void WriteType10(List<BSA_Type10> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_06));
   
                }

            }

        }

        private void WriteType12(List<BSA_Type12> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_00));
                    bytes.AddRange(BitConverter.GetBytes((int)type[i].EepkType));
                    bytes.AddRange(BitConverter.GetBytes(type[i].SkillID));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                }

            }

        }

        private void WriteType13(List<BSA_Type13> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_28));

                }

            }

        }
        private void WriteType14(List<BSA_Type14> type, int hdrOffset, int dataOffset)
        {
            if (type != null)
            {

                //Hdr data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - hdrOffset + 8), hdrOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].StartTime));
                    bytes.AddRange(BitConverter.GetBytes(GetTypeEndTime(type[i].StartTime, type[i].Duration)));
                }

                //Main Type Data
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - dataOffset + 12), dataOffset);

                for (int i = 0; i < type.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_44));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_48));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_52));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_56));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_60));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_64));
                    bytes.AddRange(BitConverter.GetBytes(type[i].F_68));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_72));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_76));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_80));
                    bytes.AddRange(BitConverter.GetBytes(type[i].I_84));
                }

            }

        }

        //Utility 
        private int BsaTypeCount(BSA_Entry bsaEntry)
        {
            int count = 0;
            if(bsaEntry.Type0 != null)
            {
                count++;
            }
            if (bsaEntry.Type1 != null)
            {
                count++;
            }
            if (bsaEntry.Type2 != null)
            {
                count++;
            }
            if (bsaEntry.Type3 != null)
            {
                count++;
            }
            if (bsaEntry.Type4 != null)
            {
                count++;
            }
            if (bsaEntry.Type6 != null)
            {
                count++;
            }
            if (bsaEntry.Type7 != null)
            {
                count++;
            }
            if (bsaEntry.Type8 != null)
            {
                count++;
            }
            if (bsaEntry.Type10 != null)
            {
                count++;
            }
            if (bsaEntry.Type12 != null)
            {
                count++;
            }
            if (bsaEntry.Type13 != null)
            {
                count++;
            }
            if (bsaEntry.Type14 != null)
            {
                count++;
            }
            return count;
        }

        private short GetTypeEndTime(int startTime, int duration)
        {
            int endTime = startTime + duration;
            return (short)endTime;
        }

    }
}
