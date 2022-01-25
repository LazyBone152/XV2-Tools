using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.DEM
{
    public class Deserializer
    {
        string saveLocation;
        DEM_File demFile;
        List<byte> bytes = new List<byte>();

        //Str
        List<StringWriter.StringInfo> stringInfo = new List<StringWriter.StringInfo>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(DEM_File), YAXSerializationOptions.DontSerializeNullObjects);
            demFile = (DEM_File)serializer.DeserializeFromFile(location);
            WriteDem();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        private void WriteDem()
        {
            //init
            int unkValueCount = (demFile.DEM_UnkValues != null) ? demFile.DEM_UnkValues.Count : 0;
            int section2Count = (demFile.Section2Entries != null) ? demFile.Section2Entries.Count : 0;
            int actorCount = (demFile.Settings.Characters != null) ? demFile.Settings.Characters.Count : 0;
            int charaOffsetPos = 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(DEM_File.DEM_SIGNATURE)); //Signature
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //Endianess
            bytes.AddRange(BitConverter.GetBytes((ushort)64)); //Header size
            bytes.AddRange(BitConverter.GetBytes(demFile.I_08)); //Version?
            bytes.AddRange(BitConverter.GetBytes(section2Count)); //Count
            bytes.AddRange(BitConverter.GetBytes(unkValueCount)); //UnkValues Count
            bytes.AddRange(new byte[12]);//Padding
            bytes.AddRange(BitConverter.GetBytes((UInt64)64)); //Offset to name
            bytes.AddRange(BitConverter.GetBytes((UInt64)0)); //Offset to Section 2 (fill in later)
            bytes.AddRange(BitConverter.GetBytes((UInt64)80)); //Offset to DemoSettings
            bytes.AddRange(BitConverter.GetBytes((UInt64)0)); //File size (fill in later)

            //Name
            Assertion.AssertStringSize(demFile.Name, 16, "DEM", "Name");
            bytes.AddRange(Utils.GetStringBytes(demFile.Name, 16));

            //DemoSettings
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_00, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //0
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_08, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //8
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_16, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //16
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_24, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //24
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_32, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //32
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_40, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //40
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_48, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //48
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_56, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //56
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_64, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //64
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_72, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //72
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_80, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //80
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_88, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //88
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_96, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[8]); //96
            stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Str_104, Offset = bytes.Count, RelativeOffset = 0 });
            bytes.AddRange(new byte[12]); //104
            bytes.AddRange(BitConverter.GetBytes(actorCount));
            charaOffsetPos = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes(208)); //Offset to actors, fill in later
            bytes.AddRange(new byte[4]);

            //Actors/charactors
            if (actorCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), charaOffsetPos);

                for (int i = 0; i < actorCount; i++)
                {
                    stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Characters[i].Str_00, Offset = bytes.Count, RelativeOffset = 0 });
                    bytes.AddRange(new byte[8]); //0
                    bytes.AddRange(BitConverter.GetBytes(demFile.Settings.Characters[i].I_08)); //8
                    bytes.AddRange(new byte[4]); //12
                    stringInfo.Add(new StringWriter.StringInfo() { StringToWrite = demFile.Settings.Characters[i].Str_16, Offset = bytes.Count, RelativeOffset = 0 });
                    bytes.AddRange(new byte[48]); //16
                }
            }

            //Pad the file (it must be in 16-byte blocks before string section starts)
            FilePad();

            //Write strings
            bytes = StringWriter.WritePointerStrings(stringInfo, bytes);

            //Pad the file (it must be in 16-byte blocks before Section2 starts)
            bytes.Add(0);
            FilePad();

            //Section2
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 40); //Filling in header offset
            List<int> section2Offsets = new List<int>();

            //Section2 main entries
            for(int i = 0; i < section2Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(demFile.Section2Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes((demFile.Section2Entries[i].SubEntries != null) ? demFile.Section2Entries[i].SubEntries.Count : 0));
                section2Offsets.Add(bytes.Count);
                bytes.AddRange(new byte[24]);
            }

            //Section2 subentries
            for(int i = 0; i < section2Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), section2Offsets[i]);
                int subEntryCount = (demFile.Section2Entries[i].SubEntries != null) ? demFile.Section2Entries[i].SubEntries.Count : 0;
                demFile.Section2Entries[i].SubEntries = demFile.Section2Entries[i].SubEntries.OrderBy(o => o.I_00).ToList();

                for (int a  = 0; a < subEntryCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(demFile.Section2Entries[i].SubEntries[a].I_00));

                    //Type
                    int[] type = demFile.Section2Entries[i].SubEntries[a].GetDemoType();
                    bytes.AddRange(BitConverter.GetBytes((ushort)type[0])); //Type1
                    bytes.AddRange(BitConverter.GetBytes((ushort)type[1])); //Type2
                    bytes.AddRange(new byte[4]); //Padding
                    demFile.Section2Entries[i].SubEntries[a].ValueCount = type[2];
                    bytes.AddRange(BitConverter.GetBytes(type[2])); //Count
                    demFile.Section2Entries[i].SubEntries[a].PointerOffset = bytes.Count;
                    bytes.AddRange(new byte[16]); //Offset and padding
                }
            }

            //Types (pointer list)
            for (int i = 0; i < section2Count; i++)
            {
                int subEntryCount = (demFile.Section2Entries[i].SubEntries != null) ? demFile.Section2Entries[i].SubEntries.Count : 0;
                for (int a = 0; a < subEntryCount; a++)
                {
                    if(demFile.Section2Entries[i].SubEntries[a].ValueCount > 0)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), demFile.Section2Entries[i].SubEntries[a].PointerOffset);
                    }

                    for (int z = 0; z < demFile.Section2Entries[i].SubEntries[a].ValueCount; z++)
                    {
                        demFile.Section2Entries[i].SubEntries[a].ValueOffsets.Add(bytes.Count);
                        bytes.AddRange(new byte[8]);
                    }
                }
            }

            //Types (values)
            for (int i = 0; i < section2Count; i++)
            {
                int subEntryCount = (demFile.Section2Entries[i].SubEntries != null) ? demFile.Section2Entries[i].SubEntries.Count : 0;
                for (int a = 0; a < subEntryCount; a++)
                {
                    DEM_Type.DemoDataTypes type = demFile.Section2Entries[i].SubEntries[a].GetDemoDataType();

                    switch (type)
                    {
                        case DEM_Type.DemoDataTypes.TextureSwitch:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_1_6.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.ScreenFade:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_2_7.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_3_8:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_3_8.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_16_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_16_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_19_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_19_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_20_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_20_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_21_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type0_21_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Position1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_1_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Position2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_1_9.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.RotateY_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_2_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.RotateY_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_2_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Transformation:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_4_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.StartTransparent:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_6_4.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.StopTransparent:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_7_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.SetEyes:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_8_6.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.ShadowVisible:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_11_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.CancelAnimation:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_12_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.EyeColor:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_13_10.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.ResetEyesColor:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_14_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type1_16_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_16_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type1_20_12:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_20_12.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Scale:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_26_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.SetHologramMaterial:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_27_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.AnimationSmall:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_0_9.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Animation:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_0_10.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.ActorVisibility:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_3_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.ActorDamage:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_9_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type1_10_8:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_10_8.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type1_17_6:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_17_6.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type1_19_3:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type1_19_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_6_3:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_6_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_7_5:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_7_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_9_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_9_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_10_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_10_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_11_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_11_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type3_0_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type3_0_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type3_1_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type3_1_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Camera:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_0_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type2_7_8:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type2_7_8.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type3_2_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type3_2_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type3_3_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type3_3_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type3_4_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type3_4_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Effect:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type4_0_12.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.PostEffect:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type4_1_8.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.AuraEffect:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type4_2_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type4_3_5:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type4_3_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type4_4_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type4_4_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Sound:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_0_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Music:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_2_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type5_0_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_0_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type5_1_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_1_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type5_3_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_3_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type5_4_3:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type5_4_3.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type6_0_1:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_0_1.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.DistanceFocus:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_16_6.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.SpmControl:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_17_19.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type6_18_7:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_18_7.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type6_19_15:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_19_15.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type6_20_2:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type6_20_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type7_0_5:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type7_0_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.YearDisplay:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type9_0_2.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Subtitle:
                            bytes = demFile.Section2Entries[i].SubEntries[a].Type9_1_5.Write(bytes, demFile.Section2Entries[i].SubEntries[a].ValueOffsets);
                            break;
                        case DEM_Type.DemoDataTypes.Type0_16_0:
                        case DEM_Type.DemoDataTypes.Type0_17_0:
                        case DEM_Type.DemoDataTypes.Type9_8_0:
                            //No values
                            break;
                        default:
                            throw new Exception(String.Format("Unrecognized DEM_Type: {0}", type));
                    }

                }
            }

            //Pad file before UnkValues
            bytes.Add(0);
            FilePad();
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 56);

            //UnkValues
            for (int i = 0; i < unkValueCount; i++)
            {
                Assertion.AssertArraySize(demFile.DEM_UnkValues[i].Values, 40, "DEM_UnknownValues", "uint16s");
                bytes.AddRange(BitConverter_Ex.GetBytes(demFile.DEM_UnkValues[i].Values));
            }
            
        }

        private void FilePad()
        {
            float fileSizeFloat = bytes.Count;
            while ((fileSizeFloat / 16) != Math.Floor(fileSizeFloat / 16))
            {
                fileSizeFloat++;
                bytes.Add(0);
            }
        }
    }
}
