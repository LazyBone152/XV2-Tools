using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.BDM
{
    public class Parser
    {
        public List<string> UsedValues = new List<string>();

        string saveLocation;
        List<byte> bytes;
        byte[] rawBytes;
        public BDM_File bdmFile { get; set; } = new BDM_File();
        private BDM_Type type;


        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            type = GetBdmType();
            bdmFile.BDM_Type = type;
            Parse();
        }

        public Parser(string location, bool _writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            bytes = rawBytes.ToList();
            type = GetBdmType();
            bdmFile.BDM_Type = type;
            Parse();
            if (_writeXml)
            {
                SaveXmlFile();
            }
        }

        public BDM_File GetBdmFile()
        {
            return bdmFile;
        }

        void Parse()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);

            bdmFile.BDM_Entries = new List<BDM_Entry>();

            if (count > 0)
            {
                switch (type)
                {
                    case BDM_Type.XV2_0:
                        for (int i = 0; i < count; i++)
                        {
                            bdmFile.BDM_Entries.Add(new BDM_Entry()
                            {
                                I_00 = BitConverter.ToInt32(rawBytes, offset),
                                Type0Entries = GetType0_Xv2(offset + 4, i)
                            });
                            offset += 1284;
                        }
                        break;
                    case BDM_Type.XV2_1:
                        for (int i = 0; i < count; i++)
                        {
                            bdmFile.BDM_Entries.Add(new BDM_Entry()
                            {
                                I_00 = BitConverter.ToInt32(rawBytes, offset),
                                Type1Entries = GetType1_Xv2(offset + 4, i)
                            });
                            offset += 1084;
                        }
                        break;
                    case BDM_Type.XV1:
                        for (int i = 0; i < count; i++)
                        {
                            bdmFile.BDM_Entries.Add(new BDM_Entry()
                            {
                                I_00 = i,
                                Type1Entries = GetType1_Xv1(offset, i)
                            });
                            offset += 756;
                        }
                        break;
                }

                
            }

            //Auto-convert
            bdmFile.ConvertToXv2();
            type = BDM_Type.XV2_0;
        }

        private BDM_Type GetBdmType()
        {
            int size = rawBytes.Count() - 16;
            int count = BitConverter.ToInt32(rawBytes, 8);

            switch (size / count) {
                case 1284:
                    return BDM_Type.XV2_0;
                case 1084:
                    return BDM_Type.XV2_1;
                case 756:
                    return BDM_Type.XV1;
                default:
                    throw new InvalidDataException("ERROR: Undefined BDM_Type encountered (entry size = " + size / count + "). \nLoad failed.");
            }
        }

        void SaveXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(BDM_File));
            serializer.SerializeToFile(bdmFile, saveLocation + ".xml");
        }

        //Readers

        List<Type0SubEntry> GetType0_Xv2(int offset, int index)
        {
            List<Type0SubEntry> type0 = new List<Type0SubEntry>();

            for (int i = 0; i < 10; i++)
            {
                type0.Add(new Type0SubEntry());
                type0[i].Index = i;
                type0[i].I_00 = BitConverter.ToUInt16(rawBytes, offset + 0);
                type0[i].I_02 = BitConverter.ToUInt16(rawBytes, offset + 2);
                type0[i].I_04 = BitConverter.ToUInt16(rawBytes, offset + 4);
                type0[i].I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
                type0[i].F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
                type0[i].I_12 = (AcbType)BitConverter.ToUInt16(rawBytes, offset + 12);
                type0[i].I_14 = BitConverter.ToInt16(rawBytes, offset + 14);
                type0[i].I_16 = BitConverter.ToInt16(rawBytes, offset + 16);
                type0[i].Effect1_SkillID = BitConverter.ToUInt16(rawBytes, offset + 18);
                type0[i].I_20 = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 20);
                type0[i].I_22 = BitConverter.ToUInt16(rawBytes, offset + 22);
                type0[i].I_24 = BitConverter.ToInt16(rawBytes, offset + 24);
                type0[i].Effect2_SkillID = BitConverter.ToUInt16(rawBytes, offset + 26);
                type0[i].I_28 = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 28);
                type0[i].I_30 = BitConverter.ToUInt16(rawBytes, offset + 30);
                type0[i].I_32 = BitConverter.ToInt16(rawBytes, offset + 32);
                type0[i].Effect3_SkillID = BitConverter.ToUInt16(rawBytes, offset + 34);
                type0[i].I_36 = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 36);
                type0[i].I_38 = BitConverter.ToUInt16(rawBytes, offset + 38);
                type0[i].F_40 = BitConverter.ToSingle(rawBytes, offset + 40);
                type0[i].F_44 = BitConverter.ToSingle(rawBytes, offset + 44);
                type0[i].I_48 = BitConverter.ToUInt16(rawBytes, offset + 48);
                type0[i].I_50 = BitConverter.ToUInt16(rawBytes, offset + 50);
                type0[i].I_52 = BitConverter.ToUInt16(rawBytes, offset + 52);
                type0[i].I_54 = BitConverter.ToUInt16(rawBytes, offset + 54);
                type0[i].I_56 = BitConverter.ToUInt16(rawBytes, offset + 56);
                type0[i].I_58 = BitConverter.ToUInt16(rawBytes, offset + 58);
                type0[i].F_60 = BitConverter.ToSingle(rawBytes, offset + 60);
                type0[i].F_64 = BitConverter.ToSingle(rawBytes, offset + 64);
                type0[i].F_68 = BitConverter.ToSingle(rawBytes, offset + 68);
                type0[i].F_72 = BitConverter.ToSingle(rawBytes, offset + 72);
                type0[i].I_76 = BitConverter.ToUInt16(rawBytes, offset + 76);
                type0[i].I_78 = BitConverter.ToUInt16(rawBytes, offset + 78);
                type0[i].I_80 = BitConverter.ToInt16(rawBytes, offset + 80);
                type0[i].I_82 = BitConverter.ToUInt16(rawBytes, offset + 82);
                type0[i].I_84 = BitConverter.ToUInt16(rawBytes, offset + 84);
                type0[i].I_86 = BitConverter.ToInt16(rawBytes, offset + 86);
                type0[i].I_94 = BitConverter.ToUInt16(rawBytes, offset + 94);
                type0[i].I_100 = BitConverter.ToUInt16(rawBytes, offset + 100);
                type0[i].I_102 = BitConverter.ToUInt16(rawBytes, offset + 102);
                type0[i].I_104 = (SByte)rawBytes[offset + 104];
                type0[i].I_106 = BitConverter.ToUInt16(rawBytes, offset + 106);
                type0[i].I_108 = BitConverter.ToInt16(rawBytes, offset + 108);
                type0[i].I_110 = BitConverter.ToInt16(rawBytes, offset + 110);
                type0[i].I_112 = BitConverter.ToInt16(rawBytes, offset + 112);
                type0[i].I_114 = BitConverter.ToUInt16(rawBytes, offset + 114);
                type0[i].I_116 = BitConverter.ToUInt16(rawBytes, offset + 116);
                type0[i].I_118 = BitConverter.ToUInt16(rawBytes, offset + 118);
                type0[i].F_120 = BitConverter.ToSingle(rawBytes, offset + 120);
                type0[i].F_124 = BitConverter.ToSingle(rawBytes, offset + 124);

                //Arrays
                type0[i].I_88 = new ushort[3];
                for (int a = 0; a < 6; a += 2)
                {
                    type0[i].I_88[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 88 + a);
                }

                type0[i].I_96 = new ushort[2];
                for (int a = 0; a < 4; a += 2)
                {
                    type0[i].I_96[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 96 + a);
                }

                offset += 128;
            }


            return type0;
        }

        List<Type1SubEntry> GetType1_Xv2(int offset, int index)
        {
            List<Type1SubEntry> type = new List<Type1SubEntry>();

            for (int i = 0; i < 10; i++)
            {
                type.Add(new Type1SubEntry());
                type[i].Index = i;
                type[i].I_00 = BitConverter.ToUInt16(rawBytes, offset + 0);
                type[i].I_02 = BitConverter.ToUInt16(rawBytes, offset + 2);
                type[i].I_04 = BitConverter.ToUInt16(rawBytes, offset + 4);
                type[i].I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
                type[i].F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
                type[i].I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
                type[i].I_14 = BitConverter.ToInt16(rawBytes, offset + 14);
                type[i].I_16 = BitConverter.ToInt16(rawBytes, offset + 16);
                type[i].I_18 = BitConverter.ToInt16(rawBytes, offset + 18);
                type[i].I_20 = BitConverter.ToInt16(rawBytes, offset + 20);
                type[i].I_22 = BitConverter.ToUInt16(rawBytes, offset + 22);
                type[i].I_24 = BitConverter.ToInt16(rawBytes, offset + 24);
                type[i].I_26 = BitConverter.ToInt16(rawBytes, offset + 26);
                type[i].I_28 = BitConverter.ToInt16(rawBytes, offset + 28);
                type[i].I_30 = BitConverter.ToUInt16(rawBytes, offset + 30);
                type[i].F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
                type[i].F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
                type[i].I_40 = BitConverter.ToUInt16(rawBytes, offset + 40);
                type[i].I_42 = BitConverter.ToUInt16(rawBytes, offset + 42);
                type[i].I_44 = BitConverter.ToUInt16(rawBytes, offset + 44);
                type[i].I_46 = BitConverter.ToUInt16(rawBytes, offset + 46);
                type[i].I_48 = BitConverter.ToUInt16(rawBytes, offset + 48);
                type[i].I_50 = BitConverter.ToUInt16(rawBytes, offset + 50);
                type[i].F_52 = BitConverter.ToSingle(rawBytes, offset + 52);
                type[i].F_56 = BitConverter.ToSingle(rawBytes, offset + 56);
                type[i].F_60 = BitConverter.ToSingle(rawBytes, offset + 60);
                type[i].F_64 = BitConverter.ToSingle(rawBytes, offset + 64);
                type[i].I_68 = BitConverter.ToUInt16(rawBytes, offset + 68);
                type[i].I_70 = BitConverter.ToUInt16(rawBytes, offset + 70);
                type[i].I_72 = BitConverter.ToInt16(rawBytes, offset + 72);
                type[i].I_74 = BitConverter.ToUInt16(rawBytes, offset + 74);
                type[i].I_76 = BitConverter.ToUInt16(rawBytes, offset + 76);
                type[i].I_78 = BitConverter.ToInt16(rawBytes, offset + 78);
                type[i].I_86 = BitConverter.ToUInt16(rawBytes, offset + 86);
                type[i].I_92 = BitConverter.ToUInt16(rawBytes, offset + 92);
                type[i].I_94 = BitConverter.ToUInt16(rawBytes, offset + 94);
                type[i].I_96 = (SByte)rawBytes[offset + 96];
                type[i].I_98 = BitConverter.ToUInt16(rawBytes, offset + 98);
                type[i].I_100 = BitConverter.ToInt16(rawBytes, offset + 100);
                type[i].I_102 = BitConverter.ToInt16(rawBytes, offset + 102);
                type[i].I_104 = BitConverter.ToInt16(rawBytes, offset + 104);

                //Arrays
                type[i].I_80 = new ushort[3];
                for (int a = 0; a < 6; a += 2)
                {
                    type[i].I_80[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 80 + a);
                }

                type[i].I_88 = new ushort[2];
                for (int a = 0; a < 4; a += 2)
                {
                    type[i].I_88[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 88 + a);
                }

            offset += 108;
            }


            return type;
        }

        List<Type1SubEntry> GetType1_Xv1(int offset, int index)
        {
            List<Type1SubEntry> type = new List<Type1SubEntry>();

            for (int i = 0; i < 7; i++)
            {
                if(BitConverter.ToUInt16(rawBytes, offset + 0) == 16)
                {
                    Console.WriteLine("This");
                    Console.ReadLine();
                }

                UsedValues.Add(BitConverter.ToUInt16(rawBytes, offset + 0).ToString());
                type.Add(new Type1SubEntry());
                type[i].Index = i;
                type[i].I_00 = BitConverter.ToUInt16(rawBytes, offset + 0);
                type[i].I_02 = BitConverter.ToUInt16(rawBytes, offset + 2);
                type[i].I_04 = BitConverter.ToUInt16(rawBytes, offset + 4);
                type[i].I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
                type[i].F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
                type[i].I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
                type[i].I_14 = BitConverter.ToInt16(rawBytes, offset + 14);
                type[i].I_16 = BitConverter.ToInt16(rawBytes, offset + 16);
                type[i].I_18 = BitConverter.ToInt16(rawBytes, offset + 18);
                type[i].I_20 = BitConverter.ToInt16(rawBytes, offset + 20);
                type[i].I_22 = BitConverter.ToUInt16(rawBytes, offset + 22);
                type[i].I_24 = BitConverter.ToInt16(rawBytes, offset + 24);
                type[i].I_26 = BitConverter.ToInt16(rawBytes, offset + 26);
                type[i].I_28 = BitConverter.ToInt16(rawBytes, offset + 28);
                type[i].I_30 = BitConverter.ToUInt16(rawBytes, offset + 30);
                type[i].F_32 = BitConverter.ToSingle(rawBytes, offset + 32);
                type[i].F_36 = BitConverter.ToSingle(rawBytes, offset + 36);
                type[i].I_40 = BitConverter.ToUInt16(rawBytes, offset + 40);
                type[i].I_42 = BitConverter.ToUInt16(rawBytes, offset + 42);
                type[i].I_44 = BitConverter.ToUInt16(rawBytes, offset + 44);
                type[i].I_46 = BitConverter.ToUInt16(rawBytes, offset + 46);
                type[i].I_48 = BitConverter.ToUInt16(rawBytes, offset + 48);
                type[i].I_50 = BitConverter.ToUInt16(rawBytes, offset + 50);
                type[i].F_52 = BitConverter.ToSingle(rawBytes, offset + 52);
                type[i].F_56 = BitConverter.ToSingle(rawBytes, offset + 56);
                type[i].F_60 = BitConverter.ToSingle(rawBytes, offset + 60);
                type[i].F_64 = BitConverter.ToSingle(rawBytes, offset + 64);
                type[i].I_68 = BitConverter.ToUInt16(rawBytes, offset + 68);
                type[i].I_70 = BitConverter.ToUInt16(rawBytes, offset + 70);
                type[i].I_72 = BitConverter.ToInt16(rawBytes, offset + 72);
                type[i].I_74 = BitConverter.ToUInt16(rawBytes, offset + 74);
                type[i].I_76 = BitConverter.ToUInt16(rawBytes, offset + 76);
                type[i].I_78 = BitConverter.ToInt16(rawBytes, offset + 78);
                type[i].I_86 = BitConverter.ToUInt16(rawBytes, offset + 86);
                type[i].I_92 = BitConverter.ToUInt16(rawBytes, offset + 92);
                type[i].I_94 = BitConverter.ToUInt16(rawBytes, offset + 94);
                type[i].I_96 = (SByte)rawBytes[offset + 96];
                type[i].I_98 = BitConverter.ToUInt16(rawBytes, offset + 98);
                type[i].I_100 = BitConverter.ToInt16(rawBytes, offset + 100);
                type[i].I_102 = BitConverter.ToInt16(rawBytes, offset + 102);
                type[i].I_104 = BitConverter.ToInt16(rawBytes, offset + 104);

                //Arrays
                type[i].I_80 = new ushort[3];
                for (int a = 0; a < 6; a += 2)
                {
                    type[i].I_80[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 80 + a);
                }

                type[i].I_88 = new ushort[2];
                for (int a = 0; a < 4; a += 2)
                {
                    type[i].I_88[a / 2] = BitConverter.ToUInt16(rawBytes, offset + 88 + a);
                }

                offset += 108;
            }


            return type;
        }


    }
}
