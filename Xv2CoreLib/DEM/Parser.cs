using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.DEM
{

    public class DebugInfo
    {
        public int flag { get; set; }
        public int I_04 { get; set; }
        public int I_06 { get; set; }
        public List<int> Count { get; set; }
    }

    public class Parser
    {
        string saveLocation { get; set; }
        public DEM_File demFile { get; private set; }
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }

        //Debug
        public List<DebugInfo> debugList { get; set; }

        public Parser(string location, bool writeXml)
        {
            demFile = new DEM_File();
            debugList = new List<DebugInfo>();
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();

            ParseDem();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(DEM_File));
                serializer.SerializeToFile(demFile, saveLocation + ".xml");
            }
        }


        public Parser(string location, bool writeXml, List<DebugInfo> _debug)
        {
            demFile = new DEM_File();
            debugList = _debug;
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();

            ParseDem();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(DEM_File));
                serializer.SerializeToFile(demFile, saveLocation + ".xml");
            }
        }

        private void ParseDem()
        {
            //Validation
            if (BitConverter.ToInt16(rawBytes, 6) == 32) throw new Exception("Xenoverse 1 DEM format not supported.");
            if (BitConverter.ToInt16(rawBytes, 6) != 64 || BitConverter.ToInt32(rawBytes, 0) != DEM_File.DEM_SIGNATURE) throw new Exception("DEM header validation failed.");
            
            //Header
            demFile.I_08 = BitConverter.ToInt32(rawBytes, 8);
            int unkValuesCount = BitConverter.ToInt32(rawBytes, 16);
            int unkValuesOffset = BitConverter.ToInt32(rawBytes, 56);
            int nameOffset = BitConverter.ToInt32(rawBytes, 32);
            int defineOffset = BitConverter.ToInt32(rawBytes, 48);
            int section2Count = BitConverter.ToInt32(rawBytes, 12);
            int section2Offset = BitConverter.ToInt32(rawBytes, 40);

            //Name
            demFile.Name = Utils.GetString(bytes, nameOffset, 16);

            //Define Section
            demFile.Settings = new DemoSettings()
            {
                Str_00 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 0)),
                Str_08 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 8)),
                Str_16 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 16)),
                Str_24 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 24)),
                Str_32 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 32)),
                Str_40 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 40)),
                Str_48 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 48)),
                Str_56 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 56)),
                Str_64 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 64)),
                Str_72 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 72)),
                Str_80 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 80)),
                Str_88 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 88)),
                Str_96 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 96)),
                Str_104 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, defineOffset + 104)),
                Characters = ParseCharacters(BitConverter.ToInt32(rawBytes, defineOffset + 120), BitConverter.ToInt32(rawBytes, defineOffset + 116))
            };

            //Section 2
            demFile.Section2Entries = new List<Section2Entry>();
            for(int i = 0; i < section2Count; i++)
            {
                demFile.Section2Entries.Add(new Section2Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, section2Offset + 0),
                    SubEntries = ParseSection2SubEntry(BitConverter.ToInt32(rawBytes, section2Offset + 8), BitConverter.ToInt32(rawBytes, section2Offset + 4))
                });
                section2Offset += 32;
            }

            //UnkValues
            if(unkValuesCount > 0)
            {
                demFile.DEM_UnkValues = new List<DEM_UnknownValues>();

                for (int i = 0; i < unkValuesCount; i++)
                {
                    demFile.DEM_UnkValues.Add(new DEM_UnknownValues()
                    {
                        Values = BitConverter_Ex.ToUInt16Array(rawBytes, unkValuesOffset, 40)
                    });
                    unkValuesOffset += 80;
                }
            }

        }

        private List<DEM_Type> ParseSection2SubEntry(int offset, int count)
        {
            List<DEM_Type> subEntries = new List<DEM_Type>();

            for (int i = 0; i < count; i++)
            {
                DEM_Type subEntry = new DEM_Type();

                subEntry.I_00 = BitConverter.ToInt32(rawBytes, offset + 0);
                subEntry.I_04 = DEM_Type.GetDemoDataType(BitConverter.ToUInt16(rawBytes, offset + 4), BitConverter.ToUInt16(rawBytes, offset + 6), BitConverter.ToInt32(rawBytes, offset + 12));
                subEntry.Offset = BitConverter.ToInt32(rawBytes, offset + 16);
                int pointerCount = BitConverter.ToInt32(rawBytes, offset + 12);
                int pointerOffset = BitConverter.ToInt32(rawBytes, offset + 16);
                subEntry = ParseTypes(subEntry.I_04, subEntry, pointerOffset, BitConverter.ToUInt16(rawBytes, offset + 4), BitConverter.ToUInt16(rawBytes, offset + 6), BitConverter.ToInt32(rawBytes, offset + 12));
                subEntries.Add(subEntry);
                offset += 32;
            }

            return subEntries;
        }
        
        private DEM_Type ParseTypes(DEM_Type.DemoDataTypes demoType, DEM_Type subEntry, int offset, int type1, int type2, int count)
        {
            switch (demoType)
            {
                case DEM_Type.DemoDataTypes.Type0_1_6:
                    subEntry.Type0_1_6 = Type0_1_6.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.FadeInOut:
                    subEntry.Type0_2_7 = Type0_2_7.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_3_8:
                    subEntry.Type0_3_8 = Type0_3_8.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_16_1:
                    subEntry.Type0_16_1 = Type0_16_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_19_1:
                    subEntry.Type0_19_1 = Type0_19_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_20_2:
                    subEntry.Type0_20_2 = Type0_20_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_21_2:
                    subEntry.Type0_21_2 = Type0_21_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_1_5:
                    subEntry.Type1_1_5 = Type1_1_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_1_9:
                    subEntry.Type1_1_9 = Type1_1_9.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_2_3:
                    subEntry.Type1_2_3 = Type1_2_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_2_5:
                    subEntry.Type1_2_5 = Type1_2_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Transformation:
                    subEntry.Type1_4_2 = Type1_4_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_6_4:
                    subEntry.Type1_6_4 = Type1_6_4.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_7_1:
                    subEntry.Type1_7_1 = Type1_7_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_8_6:
                    subEntry.Type1_8_6 = Type1_8_6.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_11_2:
                    subEntry.Type1_11_2 = Type1_11_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_12_2:
                    subEntry.Type1_12_2 = Type1_12_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_13_10:
                    subEntry.Type1_13_10 = Type1_13_10.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_14_1:
                    subEntry.Type1_14_1 = Type1_14_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_16_2:
                    subEntry.Type1_16_2 = Type1_16_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_20_12:
                    subEntry.Type1_20_12 = Type1_20_12.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_26_2:
                    subEntry.Type1_26_2 = Type1_26_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_27_2:
                    subEntry.Type1_27_2 = Type1_27_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.AnimationSmall:
                    subEntry.Type1_0_9 = Type1_0_9.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Animation:
                    subEntry.Type1_0_10 = Type1_0_10.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.ActorVisibility:
                    subEntry.Type1_3_2 = Type1_3_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.ActorDamage:
                    subEntry.Type1_9_5 = Type1_9_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_10_8:
                    subEntry.Type1_10_8 = Type1_10_8.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_17_6:
                    subEntry.Type1_17_6 = Type1_17_6.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type1_19_3:
                    subEntry.Type1_19_3 = Type1_19_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Camera:
                    subEntry.Type2_0_1 = Type2_0_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_6_3:
                    subEntry.Type2_6_3 = Type2_6_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_7_5:
                    subEntry.Type2_7_5 = Type2_7_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_7_8:
                    subEntry.Type2_7_8 = Type2_7_8.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_9_2:
                    subEntry.Type2_9_2 = Type2_9_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_10_2:
                    subEntry.Type2_10_2 = Type2_10_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type2_11_1:
                    subEntry.Type2_11_1 = Type2_11_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type3_0_1:
                    subEntry.Type3_0_1 = Type3_0_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type3_1_1:
                    subEntry.Type3_1_1 = Type3_1_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type3_2_1:
                    subEntry.Type3_2_1 = Type3_2_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type3_3_1:
                    subEntry.Type3_3_1 = Type3_3_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type3_4_2:
                    subEntry.Type3_4_2 = Type3_4_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type4_2_3:
                    subEntry.Type4_2_3 = Type4_2_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type4_3_5:
                    subEntry.Type4_3_5 = Type4_3_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type4_4_1:
                    subEntry.Type4_4_1 = Type4_4_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Effect:
                    subEntry.Type4_0_12 = Type4_0_12.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.ScreenEffect:
                    subEntry.Type4_1_8 = Type4_1_8.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type5_0_2:
                    subEntry.Type5_0_2 = Type5_0_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type5_1_2:
                    subEntry.Type5_1_2 = Type5_1_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Sound:
                    subEntry.Type5_0_3 = Type5_0_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Music:
                    subEntry.Type5_2_3 = Type5_2_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type5_3_2:
                    subEntry.Type5_3_2 = Type5_3_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type5_4_3:
                    subEntry.Type5_4_3 = Type5_4_3.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type6_0_1:
                    subEntry.Type6_0_1 = Type6_0_1.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.DistanceFocus:
                    subEntry.Type6_16_6 = Type6_16_6.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.SpmControl:
                    subEntry.Type6_17_19 = Type6_17_19.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type6_18_7:
                    subEntry.Type6_18_7 = Type6_18_7.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type6_19_15:
                    subEntry.Type6_19_15 = Type6_19_15.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type6_20_2:
                    subEntry.Type6_20_2 = Type6_20_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type7_0_5:
                    subEntry.Type7_0_5 = Type7_0_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.YearDisplay:
                    subEntry.Type9_0_2 = Type9_0_2.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type9_1_5:
                    subEntry.Type9_1_5 = Type9_1_5.Read(rawBytes, bytes, offset);
                    break;
                case DEM_Type.DemoDataTypes.Type0_17_0:
                case DEM_Type.DemoDataTypes.Type9_8_0:
                case DEM_Type.DemoDataTypes.Type0_16_0:
                    //No values
                    break;
                default:
                    //Console.WriteLine(String.Format("Unknown DEM_Type: {0}_{1}_{2} (offset={3})", type1, type2, count, offset));
                    //Console.Read();
                    //break;
                    throw new Exception(String.Format("Unknown DEM_Type: {0}_{1}_{2} (offset={3})", type1, type2, count, offset));
            }

            return subEntry;
        }

        private List<Character> ParseCharacters(int offset, int count)
        {
            List<Character> _characters = new List<Character>();

            for(int i = 0; i < count; i++)
            {
                _characters.Add(new Character()
                {
                    Str_00 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    Str_16 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 16))
                });

                offset += 64;
            }

            return _characters;
        }
        
    }
}
