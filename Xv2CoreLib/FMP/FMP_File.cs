using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.FMP
{
    [YAXSerializeAs("FMP")]
    public class FMP_File
    {
        private const int SIGNATURE = 1347241507;

        private bool IsOldVersion => (Version & 0xFFF00) == 0;

        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_96 { get; set; }

        public FMP_SettingsA SettingsA { get; set; }
        public FMP_SettingsB SettingsB { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Section1")]
        public List<FMP_Section1> Section1List { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Section2")]
        public List<FMP_Section2> Section2List { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "FragmentGroup")]
        public List<FMP_FragmentGroup> FragmentGroups { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Object")]
        public List<FMP_Object> Objects { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "HitboxGroup")]
        public List<FMP_HitboxGroup> HitboxGroups { get; set; }

        #region SaveLoad
        public static void SerializeToXml(string path)
        {
            FMP_File fmpFile = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(FMP_File));
            serializer.SerializeToFile(fmpFile, path + ".xml");
        }

        public static void DeserializeFromXml(string path)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            YAXSerializer serializer = new YAXSerializer(typeof(FMP_File), YAXSerializationOptions.DontSerializeNullObjects);
            FMP_File fmpFile = (FMP_File)serializer.DeserializeFromFile(path);

            File.WriteAllBytes(saveLocation, fmpFile.Write());
        }

        public static FMP_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != SIGNATURE)
            {
                throw new InvalidDataException("#FMP magic bytes not found. This is not a .map file!");
            }

            FMP_File fmpFile = new FMP_File();

            //Header
            fmpFile.Version = BitConverter.ToInt32(bytes, 4);
            fmpFile.I_08 = BitConverter.ToInt32(bytes, 8);
            fmpFile.I_12 = BitConverter.ToInt32(bytes, 12);
            fmpFile.I_96 = BitConverter_Ex.ToInt32Array(bytes, 96, 4);

            int settingsA_Offset = BitConverter.ToInt32(bytes, 16);
            int settingsB_Offset = BitConverter.ToInt32(bytes, 20);
            int section1_Count = BitConverter.ToInt32(bytes, 24);
            int section1_Offset = BitConverter.ToInt32(bytes, 28);
            int section2_Count = BitConverter.ToInt32(bytes, 32);
            int section2_Offset = BitConverter.ToInt32(bytes, 36);
            int fragmentGroup_Count = BitConverter.ToInt32(bytes, 40);
            int fragmentGroup_Offset = BitConverter.ToInt32(bytes, 44);
            int object_Count = BitConverter.ToInt32(bytes, 48);
            int object_Offset = BitConverter.ToInt32(bytes, 52);
            int hitboxGroup_Count = BitConverter.ToInt32(bytes, 56);
            int hitboxGroup_Offset = BitConverter.ToInt32(bytes, 60);
            int depot1_Count = BitConverter.ToInt32(bytes, 64);
            int depot1_Offset = BitConverter.ToInt32(bytes, 68);
            int depot2_Count = BitConverter.ToInt32(bytes, 72);
            int depot2_Offset = BitConverter.ToInt32(bytes, 76);
            int depot3_Count = BitConverter.ToInt32(bytes, 80);
            int depot3_Offset = BitConverter.ToInt32(bytes, 84);
            int depot4_Count = BitConverter.ToInt32(bytes, 88);
            int depot4_Offset = BitConverter.ToInt32(bytes, 92);

            string[] depot1 = GetDepotStrings(bytes, depot1_Offset, depot1_Count);
            string[] depot2 = GetDepotStrings(bytes, depot2_Offset, depot2_Count);
            string[] depot3 = GetDepotStrings(bytes, depot3_Offset, depot3_Count);
            string[] depot4 = GetDepotStrings(bytes, depot4_Offset, depot4_Count);

            fmpFile.SettingsA = FMP_SettingsA.Read(bytes, settingsA_Offset);
            fmpFile.SettingsB = FMP_SettingsB.Read(bytes, settingsB_Offset);
            fmpFile.Section1List = FMP_Section1.ReadAll(bytes, section1_Offset, section1_Count);
            fmpFile.Section2List = FMP_Section2.ReadAll(bytes, section2_Offset, section2_Count);
            fmpFile.FragmentGroups = FMP_FragmentGroup.ReadAll(bytes, fragmentGroup_Offset, fragmentGroup_Count);
            fmpFile.HitboxGroups = FMP_HitboxGroup.ReadAll(bytes, hitboxGroup_Offset, hitboxGroup_Count, fmpFile.IsOldVersion);
            fmpFile.Objects = FMP_Object.ReadAll(bytes, object_Offset, object_Count, depot1, depot2, depot3, depot4, fmpFile);

            return fmpFile;
        }
    
        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            List<string> depot1 = new List<string>();
            List<string> depot2 = new List<string>();
            List<string> depot3 = new List<string>();
            List<string> depot4 = new List<string>();
            CreateDepots(depot1, depot2, depot3, depot4);

            int section1Count = Section1List != null ? Section1List.Count : 0;
            int section2Count = Section2List != null ? Section2List.Count : 0;
            int fragmentCount = FragmentGroups != null ? FragmentGroups.Count : 0;
            int objectCount = Objects != null ? Objects.Count : 0;
            int hitboxGroupCount = HitboxGroups != null ? HitboxGroups.Count : 0;

            List<StringWriter.StringInfo> stringsWriter = new List<StringWriter.StringInfo>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes((int)112)); //Offset to SettingsA. Comes after header, so always 112
            bytes.AddRange(BitConverter.GetBytes((int)252)); //20, offset to SettingsB, always 252 since it comes after SettingsA, which is also fixed size
            bytes.AddRange(BitConverter.GetBytes(section1Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(section2Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(fragmentCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(objectCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(hitboxGroupCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot1.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot2.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot3.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot4.Count));
            bytes.AddRange(new byte[4]);
            Assertion.AssertArraySize(I_96, 4, "FMP Header", nameof(I_96));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_96));

            if (bytes.Count != 112)
                throw new Exception("FMP_File.Write: Header size is incorrect!");

            bytes.AddRange(SettingsA.Write());
            bytes.AddRange(SettingsB.Write(bytes.Count, stringsWriter));

            if(section1Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);
                FMP_Section1.WriteAll(Section1List, bytes, stringsWriter);
            }

            if (section2Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 36);
                FMP_Section2.WriteAll(Section2List, bytes, stringsWriter);
            }

            if (fragmentCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 44);
                FMP_FragmentGroup.WriteAll(FragmentGroups, bytes, stringsWriter);
            }

            if (objectCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 52);
                FMP_Object.WriteAll(Objects, bytes, stringsWriter);
            }

            if (hitboxGroupCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 60);
                FMP_HitboxGroup.WriteAll(HitboxGroups, bytes, stringsWriter, IsOldVersion);
            }

            //Write depots
            if (depot1.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 68);
                WriteDepot(bytes, depot1, stringsWriter);
            }

            if (depot2.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 76);
                WriteDepot(bytes, depot2, stringsWriter);
            }

            if (depot3.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 84);
                WriteDepot(bytes, depot3, stringsWriter);
            }

            if (depot4.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 92);
                WriteDepot(bytes, depot4, stringsWriter);
            }

            //Write name strings
            StringWriter.WriteStrings(stringsWriter, bytes, true);

            return bytes.ToArray();
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, Write());
        }
        
        private static string[] GetDepotStrings(byte[] bytes, int offset, int count)
        {
            string[] files = new string[count];

            for(int i = 0; i < count; i++)
            {
                files[i] = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset + (4 * i)));
            }

            return files;
        }
        
        private void CreateDepots(List<string> depot1,  List<string> depot2, List<string> depot3, List<string> depot4)
        {
            if (Objects == null) return;

            foreach(var obj in Objects)
            {
                if (obj.Entities == null) continue;

                foreach(var entity in obj.Entities)
                {
                    if(entity.Visual != null)
                    {
                        entity.Visual.CreateDepots(depot1, depot2, depot3, depot4);
                    }
                }
            }
        }
        
        private static void WriteDepot(List<byte> bytes, List<string> depotStrings, List<StringWriter.StringInfo> stringWriter)
        {
            foreach(string depotString in depotStrings)
            {
                if (!string.IsNullOrWhiteSpace(depotString))
                    stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = depotString });

                bytes.AddRange(new byte[4]);
            }
        }
        #endregion
    }

    public class FMP_SettingsA
    {
        [CustomSerialize(parent: "Width", serializeAs: "X", isFloat: true)]
        public float WidthX { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Y", isFloat: true)]
        public float WidthY { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Z", isFloat: true)]
        public float WidthZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float NearDistance { get; set; } //128
        [CustomSerialize(isFloat: true)]
        public float FarDistance { get; set; } //132
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_16 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_20 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }
        [CustomSerialize]
        public int I_28 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_48 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_52 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_56 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_60 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_64 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_68 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_72 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_76 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_80 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_84 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_88 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_92 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_96 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_100 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_104 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_108 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_112 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_116 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_120 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_124 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_136 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(WidthX));
            bytes.AddRange(BitConverter.GetBytes(WidthY));
            bytes.AddRange(BitConverter.GetBytes(WidthZ));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));
            bytes.AddRange(BitConverter.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(F_72));
            bytes.AddRange(BitConverter.GetBytes(F_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(F_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(F_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(F_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));
            bytes.AddRange(BitConverter.GetBytes(F_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));
            bytes.AddRange(BitConverter.GetBytes(F_120));
            bytes.AddRange(BitConverter.GetBytes(F_124));
            bytes.AddRange(BitConverter.GetBytes(NearDistance));
            bytes.AddRange(BitConverter.GetBytes(FarDistance));
            bytes.AddRange(BitConverter.GetBytes(F_136));

            if (bytes.Count != 140)
                throw new Exception("SettingsA.Write: Incorrect size!");

            return bytes.ToArray();
        }

        public static FMP_SettingsA Read(byte[] bytes, int offset)
        {
            return new FMP_SettingsA()
            {
                WidthX = BitConverter.ToSingle(bytes, offset),
                WidthY = BitConverter.ToSingle(bytes, offset + 4),
                WidthZ = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                I_28 = BitConverter.ToInt32(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
                F_64 = BitConverter.ToSingle(bytes, offset + 64),
                F_68 = BitConverter.ToSingle(bytes, offset + 68),
                F_72 = BitConverter.ToSingle(bytes, offset + 72),
                F_76 = BitConverter.ToSingle(bytes, offset + 76),
                F_80 = BitConverter.ToSingle(bytes, offset + 80),
                F_84 = BitConverter.ToSingle(bytes, offset + 84),
                F_88 = BitConverter.ToSingle(bytes, offset + 88),
                F_92 = BitConverter.ToSingle(bytes, offset + 92),
                F_96 = BitConverter.ToSingle(bytes, offset + 96),
                F_100 = BitConverter.ToSingle(bytes, offset + 100),
                F_104 = BitConverter.ToSingle(bytes, offset + 104),
                F_108 = BitConverter.ToSingle(bytes, offset + 108),
                F_112 = BitConverter.ToSingle(bytes, offset + 112),
                F_116 = BitConverter.ToSingle(bytes, offset + 116),
                F_120 = BitConverter.ToSingle(bytes, offset + 120),
                F_124 = BitConverter.ToSingle(bytes, offset + 124),
                NearDistance = BitConverter.ToSingle(bytes, offset + 128),
                FarDistance = BitConverter.ToSingle(bytes, offset + 132),
                F_136 = BitConverter.ToSingle(bytes, offset + 136),
            };
        }
    }

    public class FMP_SettingsB
    {
        [CustomSerialize(isHex:true)]
        public ulong I_00 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_08 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_16 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_24 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_32 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_40 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_48 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_56 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_64 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_72 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_80 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_88 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_96 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_104 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_112 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_120 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_128 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_136 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_144 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_152 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_160 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_168 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_176 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_184 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_192 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_200 { get; set; }
        [CustomSerialize(isHex: true)]
        public ulong I_208 { get; set; }
        [CustomSerialize(isHex: true)]
        public uint I_216 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Item")]
        public List<FMP_SettingsItem> Items { get; set; } = new List<FMP_SettingsItem>(); //Should be size 51 at most

        public byte[] Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_88));
            bytes.AddRange(BitConverter.GetBytes(I_96));
            bytes.AddRange(BitConverter.GetBytes(I_104));
            bytes.AddRange(BitConverter.GetBytes(I_112));
            bytes.AddRange(BitConverter.GetBytes(I_120));
            bytes.AddRange(BitConverter.GetBytes(I_128));
            bytes.AddRange(BitConverter.GetBytes(I_136));
            bytes.AddRange(BitConverter.GetBytes(I_144));
            bytes.AddRange(BitConverter.GetBytes(I_152));
            bytes.AddRange(BitConverter.GetBytes(I_160));
            bytes.AddRange(BitConverter.GetBytes(I_168));
            bytes.AddRange(BitConverter.GetBytes(I_176));
            bytes.AddRange(BitConverter.GetBytes(I_184));
            bytes.AddRange(BitConverter.GetBytes(I_192));
            bytes.AddRange(BitConverter.GetBytes(I_200));
            bytes.AddRange(BitConverter.GetBytes(I_208));
            bytes.AddRange(BitConverter.GetBytes(I_216));
            bytes.AddRange(new byte[36]);

            if (bytes.Count != 256)
                throw new Exception($"SettingsB.Write: Incorrect size! (was {bytes.Count})");

            int nameCount = Items != null ? Items.Count : 0;

            bytes.AddRange(new byte[4 * 51]); //Offsets to fill in later
            bytes.AddRange(new byte[48]); //Padding

            if (nameCount > 51)
                throw new Exception($"SettingsB.Write: There are too many Items. 51 is the maximum amount allowed! (you have {nameCount})");

            for(int i = 0; i < nameCount; i++)
            {
                if (string.IsNullOrWhiteSpace(Items[i].Name)) continue;

                stringWriter.Add(new StringWriter.StringInfo()
                {
                    Offset = fileSize + 256 + (4 * i),
                    StringToWrite = Items[i].Name
                });
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(fileSize + bytes.Count, 4)]);

            return bytes.ToArray();
        }
        public static FMP_SettingsB Read(byte[] bytes, int offset)
        {
            FMP_SettingsB settingsB = new FMP_SettingsB()
            {
                I_00 = BitConverter.ToUInt64(bytes, offset + 0),
                I_08 = BitConverter.ToUInt64(bytes, offset + 8),
                I_16 = BitConverter.ToUInt64(bytes, offset + 16),
                I_24 = BitConverter.ToUInt64(bytes, offset + 24),
                I_32 = BitConverter.ToUInt64(bytes, offset + 32),
                I_40 = BitConverter.ToUInt64(bytes, offset + 40),
                I_48 = BitConverter.ToUInt64(bytes, offset + 48),
                I_56 = BitConverter.ToUInt64(bytes, offset + 56),
                I_64 = BitConverter.ToUInt64(bytes, offset + 64),
                I_72 = BitConverter.ToUInt64(bytes, offset + 72),
                I_80 = BitConverter.ToUInt64(bytes, offset + 80),
                I_88 = BitConverter.ToUInt64(bytes, offset + 88),
                I_96 = BitConverter.ToUInt64(bytes, offset + 96),
                I_104 = BitConverter.ToUInt64(bytes, offset + 104),
                I_112 = BitConverter.ToUInt64(bytes, offset + 112),
                I_120 = BitConverter.ToUInt64(bytes, offset + 120),
                I_128 = BitConverter.ToUInt64(bytes, offset + 128),
                I_136 = BitConverter.ToUInt64(bytes, offset + 136),
                I_144 = BitConverter.ToUInt64(bytes, offset + 144),
                I_152 = BitConverter.ToUInt64(bytes, offset + 152),
                I_160 = BitConverter.ToUInt64(bytes, offset + 160),
                I_168 = BitConverter.ToUInt64(bytes, offset + 168),
                I_176 = BitConverter.ToUInt64(bytes, offset + 176),
                I_184 = BitConverter.ToUInt64(bytes, offset + 184),
                I_192 = BitConverter.ToUInt64(bytes, offset + 192),
                I_200 = BitConverter.ToUInt64(bytes, offset + 200),
                I_208 = BitConverter.ToUInt64(bytes, offset + 208),
                I_216 = BitConverter.ToUInt32(bytes, offset + 216)
            };

            for(int i = 0; i < 51; i++)
            {
                int nameOffset = offset + 256 + (4 * i);
                nameOffset = BitConverter.ToInt32(bytes, nameOffset);
                string name = nameOffset > 0 ? StringEx.GetString(bytes, nameOffset) : null;

                settingsB.Items.Add(new FMP_SettingsItem(name));
            }

            return settingsB;
        }
    }

    public class FMP_SettingsItem
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        public FMP_SettingsItem() { }

        public FMP_SettingsItem(string name)
        {
            Name = name;
        }
    }

    public class FMP_Section1 : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } // No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }

        [CustomSerialize]
        public int I_04 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_08 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_16 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_20 { get; set; }

        internal static void WriteAll(List<FMP_Section1> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                bytes.AddRange(entries[i].Write(bytes.Count, stringWriter));
            }
        }

        internal List<byte> Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            List<byte> bytes = new List<byte>();

            stringWriter.Add(new StringWriter.StringInfo() { Offset = fileSize, StringToWrite = Index });
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));

            if (bytes.Count != 24)
                throw new Exception("FMP_Section1.Write: Invalid size");

            return bytes;
        }

        public static List<FMP_Section1> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Section1> entries = new List<FMP_Section1>();

            for(int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (24 * i)));
            }

            return entries;
        }

        public static FMP_Section1 Read(byte[] bytes, int offset)
        {
            return new FMP_Section1()
            {
                Index = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset)),
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20)
            };
        }
    }

    public class FMP_Section2 : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } // No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }

        [CustomSerialize(isFloat: true)]
        public float F_04 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_08 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_16 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_20 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }

        internal static void WriteAll(List<FMP_Section2> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                bytes.AddRange(entries[i].Write(bytes.Count, stringWriter));
            }
        }

        internal List<byte> Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            List<byte> bytes = new List<byte>();

            stringWriter.Add(new StringWriter.StringInfo() { Offset = fileSize, StringToWrite = Index });
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));

            if (bytes.Count != 28)
                throw new Exception("FMP_Section2.Write: Invalid size");

            return bytes;
        }

        public static List<FMP_Section2> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Section2> entries = new List<FMP_Section2>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (28 * i)));
            }

            return entries;
        }

        public static FMP_Section2 Read(byte[] bytes, int offset)
        {
            return new FMP_Section2()
            {
                Index = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset)),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24)
            };
        }
    }

    public class FMP_FragmentGroup : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } // No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ObjectIndex")]
        [YAXAttributeFor("ObjectIndex")]
        public List<FMP_ObjectIndex> Indices { get; set; } = new List<FMP_ObjectIndex>();

        internal static void WriteAll(List<FMP_FragmentGroup> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int fragmentStart = bytes.Count;

            //Main body, 12 bytes each
            for (int i = 0; i < entries.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = entries[i].Index });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entries[i].Indices != null ? entries[i].Indices.Count : 0));
                bytes.AddRange(new byte[4]);
            }

            //Values
            for (int i = 0; i < entries.Count; i++)
            {
                int indicesCount = entries[i].Indices != null ? entries[i].Indices.Count : 0;

                if(indicesCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), fragmentStart + (12 * i) + 8);

                    foreach(var entry in entries[i].Indices)
                    {
                        bytes.AddRange(BitConverter.GetBytes(entry.Index));
                    }
                }
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
        }

        public static List<FMP_FragmentGroup> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_FragmentGroup> entries = new List<FMP_FragmentGroup>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (12 * i)));
            }

            return entries;
        }

        public static FMP_FragmentGroup Read(byte[] bytes, int offset)
        {
            FMP_FragmentGroup fragmentGroup = new FMP_FragmentGroup();
            fragmentGroup.Index = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));

            int indexCount = BitConverter.ToInt32(bytes, offset + 4);
            int indexOffset = BitConverter.ToInt32(bytes, offset + 8);

            for(int i = 0; i < indexCount; i++)
            {
                fragmentGroup.Indices.Add(new FMP_ObjectIndex() { Index = BitConverter.ToUInt16(bytes, indexOffset + (2 * i)) });
            }

            return fragmentGroup;
        }
    }

    [YAXSerializeAs("ObjectIndex")]
    public class FMP_ObjectIndex
    {
        [YAXAttributeForClass]
        public ushort Index { get; set; }
    }

    [YAXSerializeAs("Object")]
    public class FMP_Object : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } // No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }

        [CustomSerialize(isHex: true)]
        public int I_04 { get; set; }
        [CustomSerialize]
        public ushort HitboxGroupIndex { get; set; } //8
        [CustomSerialize]
        public ushort I_10 { get; set; }

        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Entity")]
        [YAXSerializeAs("EntityList")]
        [YAXDontSerializeIfNull]
        public List<FMP_Entity> Entities { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_Action Action { get; set; }
        [YAXDontSerializeIfNull]
        public List<FMP_VirtualSubPart> VirtualSubParts { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_Hierarchy Hierarchy { get; set; }
        public FMP_Matrix Matrix { get; set; }

        internal static void WriteAll(List<FMP_Object> objects, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int objectStart = bytes.Count;

            //Write main Object entries
            for(int i = 0; i < objects.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = objects[i].Index });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(objects[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(objects[i].HitboxGroupIndex));
                bytes.AddRange(BitConverter.GetBytes(objects[i].I_10));
                bytes.AddRange(new byte[8]); //VirtualSubPart and Action offsets
                bytes.AddRange(BitConverter.GetBytes(objects[i].Entities != null ? objects[i].Entities.Count : 0));
                bytes.AddRange(new byte[8]); //Entity and Hiearchy offsets
                bytes.AddRange(BitConverter.GetBytes(objects[i].F_32));
                bytes.AddRange(objects[i].Matrix.Write());
            }

            //Write sub data, per object
            for (int i = 0; i < objects.Count; i++)
            {
                int virtualSubObjectsCount = objects[i].VirtualSubParts?.Count ?? 0; //Should be equal to number of hitboxes in hitboxGroup
                int entityCount = objects[i].Entities?.Count ?? 0;
                int virtualObjectStart = bytes.Count;
                
                //Write main bodies of virtualSubObject
                if(virtualSubObjectsCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 12 + (84 * i));

                    for (int a = 0; a < virtualSubObjectsCount; a++)
                    {
                        FMP_VirtualSubPart virtualPart = objects[i].VirtualSubParts[a];
                        bytes.AddRange(BitConverter.GetBytes(virtualPart.IndexPairs?.Count ?? 0));
                        bytes.AddRange(new byte[16]); //IndexPair, ObjectSubPart1, ObjectSubPart2, Action offsets
                        bytes.AddRange(BitConverter.GetBytes(virtualPart.I_20));
                        bytes.AddRange(BitConverter.GetBytes(virtualPart.I_22));
                        bytes.AddRange(BitConverter.GetBytes(virtualPart.F_24));
                        bytes.AddRange(BitConverter.GetBytes(virtualPart.F_28));
                        bytes.AddRange(virtualPart.Matrix.Write());
                    }
                }

                //Write Action (comes before the rest of VirtualSubObject data)
                if (objects[i].Action != null)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 16 + (84 * i));
                    objects[i].Action.Write(bytes, stringWriter);
                }

                //Write VirtualSubObject subdata
                if (virtualSubObjectsCount > 0)
                {
                    for (int a = 0; a < virtualSubObjectsCount; a++)
                    {
                        FMP_VirtualSubPart virtualPart = objects[i].VirtualSubParts[a];

                        //First SubPart
                        if(virtualPart.SubPart1 != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), virtualObjectStart + 8 + (a * 80));
                            bytes.AddRange(virtualPart.SubPart1.Write());
                        }

                        //Second SubPart (unsure on the order of this one - assume its after SubPart1 for now)
                        if (virtualPart.SubPart2 != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), virtualObjectStart + 12 + (a * 80));
                            bytes.AddRange(virtualPart.SubPart2.Write());
                        }

                        //Index pairs come next
                        int indexPairs = virtualPart.IndexPairs?.Count ?? 0;

                        if(indexPairs > 0)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), virtualObjectStart + 4 + (a * 80));

                            for(int p = 0; p < indexPairs; p++)
                            {
                                bytes.AddRange(BitConverter.GetBytes(virtualPart.IndexPairs[p].Index0));
                                bytes.AddRange(BitConverter.GetBytes(virtualPart.IndexPairs[p].Index1));
                            }
                        }

                        //Action
                        if(virtualPart.Action != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), virtualObjectStart + 16 + (a * 80));
                            virtualPart.Action.Write(bytes, stringWriter);
                        }
                    }
                }
            
                //Entities
                if(entityCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 24 + (84 * i));
                    FMP_Entity.Write(objects[i].Entities, bytes, stringWriter);
                }

                //Hierarchy
                if (objects[i].Hierarchy != null)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 28 + (84 * i));
                    objects[i].Hierarchy.Write(bytes);
                }
            }

        }

        public static List<FMP_Object> ReadAll(byte[] bytes, int offset, int count, string[] depot1, string[] depot2, string[] depot3, string[] depot4, FMP_File fmpFile)
        {
            List<FMP_Object> entries = new List<FMP_Object>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (84 * i), depot1, depot2, depot3, depot4, fmpFile));
            }

            return entries;
        }

        public static FMP_Object Read(byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4, FMP_File fmpFile)
        {
            FMP_Object obj = new FMP_Object();
            obj.Index = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));
            obj.I_04 = BitConverter.ToInt32(bytes, offset + 4);
            obj.HitboxGroupIndex = BitConverter.ToUInt16(bytes, offset + 8);
            obj.I_10 = BitConverter.ToUInt16(bytes, offset + 10);
            obj.F_32 = BitConverter.ToSingle(bytes, offset + 32);
            obj.Matrix = FMP_Matrix.Read(bytes, offset + 36);

            int virtualSubPartOffset = BitConverter.ToInt32(bytes, offset + 12);
            int actionOffset = BitConverter.ToInt32(bytes, offset + 16);
            int entityCount = BitConverter.ToInt32(bytes, offset + 20);
            int entityOffset = BitConverter.ToInt32(bytes, offset + 24);
            int hierarchyOffset = BitConverter.ToInt32(bytes, offset + 28);

            if(obj.HitboxGroupIndex < fmpFile.HitboxGroups.Count && obj.HitboxGroupIndex >= 0)
            {
                FMP_HitboxGroup hitboxGroup = fmpFile.HitboxGroups[obj.HitboxGroupIndex];
                obj.VirtualSubParts = FMP_VirtualSubPart.ReadAll(bytes, virtualSubPartOffset, hitboxGroup.Hitboxes.Count);
            }

            if(entityCount > 0)
                obj.Entities = FMP_Entity.ReadAll(bytes, entityOffset, entityCount, depot1, depot2, depot3, depot4);

            if(actionOffset > 0)
                obj.Action = FMP_Action.Read(bytes, actionOffset);

            if (hierarchyOffset > 0)
                obj.Hierarchy = FMP_Hierarchy.Read(bytes, hierarchyOffset);

            return obj;
        }
    }

    [YAXSerializeAs("Hierarchy")]
    public class FMP_Hierarchy
    {
        public List<FMP_Node> Nodes { get; set; }
        public FMP_HierarchyNode HierarchyNode { get; set; }

        public void Write(List<byte> bytes)
        {
            List<FMP_NodeTransform> transforms = GetNodeTransforms();
            int nodeCount = Nodes?.Count ?? 0;
            int start = bytes.Count;

            bytes.AddRange(BitConverter.GetBytes(transforms.Count));
            bytes.AddRange(BitConverter.GetBytes(nodeCount));
            bytes.AddRange(BitConverter.GetBytes(nodeCount > 0 ? start + 20 : 0)); //Directly after FMP_Hierarchy
            bytes.AddRange(new byte[8]); //HierarchyNode and NodeTransform offset

            for(int i = 0; i < nodeCount; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_00));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_04));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_08));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_12));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_16));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].Transforms?.Count > 0 ? Nodes[i].Transforms.Count : 0));
                bytes.AddRange(BitConverter.GetBytes(Nodes[i].NodeTransformIndex));
            }

            //NodeTransforms
            if(transforms.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), start + 16);

                for(int i = 0; i < transforms.Count; i++)
                {
                    bytes.AddRange(transforms[i].Write());
                }
            }

            //HierarchyNode
            if(HierarchyNode != null)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), start + 12);
                HierarchyNode.Write(bytes);
            }
        }

        private List<FMP_NodeTransform> GetNodeTransforms()
        {
            List<FMP_NodeTransform> nodes = new List<FMP_NodeTransform>();

            foreach(var node in Nodes)
            {
                if(node.Transforms?.Count > 0)
                {
                    node.NodeTransformIndex = nodes.Count;
                    nodes.AddRange(node.Transforms);
                }
                else
                {
                    node.NodeTransformIndex = -1;
                }
            }

            return nodes;
        }

        public static FMP_Hierarchy Read(byte[] bytes, int offset)
        {
            int nodeTransformCount = BitConverter.ToInt32(bytes, offset);
            int nodeCount = BitConverter.ToInt32(bytes, offset + 4);
            int nodeOffset = BitConverter.ToInt32(bytes, offset + 8);
            int hierarchyNodeOffset = BitConverter.ToInt32(bytes, offset + 12);
            int nodeTransformOffset = BitConverter.ToInt32(bytes, offset + 16);

            return new FMP_Hierarchy()
            {
                HierarchyNode = FMP_HierarchyNode.Read(bytes, hierarchyNodeOffset),
                Nodes = FMP_Node.ReadAll(bytes, nodeOffset, nodeCount, nodeTransformOffset)
            };
        }
    }

    [YAXSerializeAs("Node")]
    public class FMP_Node
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_00 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_04 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_08 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_16 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_20 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_28 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }

        public List<FMP_NodeTransform> Transforms { get; set; }

        internal int NodeTransformIndex { get; set; }

        public static List<FMP_Node> ReadAll(byte[] bytes, int offset, int count, int nodeTransformsOffset)
        {
            List<FMP_Node> entries = new List<FMP_Node>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (48 * i), nodeTransformsOffset, i));
            }

            return entries;
        }

        public static FMP_Node Read(byte[] bytes, int offset, int nodeTransformsOffset, int index)
        {
            FMP_Node node = new FMP_Node();
            node.Index = index;
            node.F_00 = BitConverter.ToSingle(bytes, offset);
            node.F_04 = BitConverter.ToSingle(bytes, offset + 4);
            node.F_08 = BitConverter.ToSingle(bytes, offset + 8);
            node.F_12 = BitConverter.ToSingle(bytes, offset + 12);
            node.F_16 = BitConverter.ToSingle(bytes, offset + 16);
            node.F_20 = BitConverter.ToSingle(bytes, offset + 20);
            node.F_24 = BitConverter.ToSingle(bytes, offset + 24);
            node.F_28 = BitConverter.ToSingle(bytes, offset + 28);
            node.F_32 = BitConverter.ToSingle(bytes, offset + 32);
            node.F_36 = BitConverter.ToSingle(bytes, offset + 36);

            int transformCount = BitConverter.ToInt32(bytes, offset + 40);
            int transformOffset = nodeTransformsOffset + (FMP_NodeTransform.SIZE * BitConverter.ToInt32(bytes, offset + 44));

            node.Transforms = FMP_NodeTransform.ReadAll(bytes, transformOffset, transformCount);
            return node;
        }

    }

    [YAXSerializeAs("NodeTransform")]
    public class FMP_NodeTransform
    {
        internal const int SIZE = 36;

        [CustomSerialize(isFloat: true)]
        public float PositionX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float PositionY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float PositionZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RotationZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleX { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleY { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ScaleZ { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(36);

            bytes.AddRange(BitConverter.GetBytes(PositionX));
            bytes.AddRange(BitConverter.GetBytes(PositionY));
            bytes.AddRange(BitConverter.GetBytes(PositionZ));

            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationX)));
            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationY)));
            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(RotationZ)));

            bytes.AddRange(BitConverter.GetBytes(ScaleX));
            bytes.AddRange(BitConverter.GetBytes(ScaleY));
            bytes.AddRange(BitConverter.GetBytes(ScaleZ));

            if (bytes.Count != SIZE)
                throw new Exception("FMP_NodeTransform.Write: Incorrect size!");

            return bytes.ToArray();
        }

        public static List<FMP_NodeTransform> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_NodeTransform> entries = new List<FMP_NodeTransform>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (SIZE * i)));
            }

            return entries;
        }

        public static FMP_NodeTransform Read(byte[] bytes, int offset)
        {
            return new FMP_NodeTransform()
            {
                PositionX = BitConverter.ToSingle(bytes, offset),
                PositionY = BitConverter.ToSingle(bytes, offset + 4),
                PositionZ = BitConverter.ToSingle(bytes, offset + 8),
                RotationX = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 12)),
                RotationY = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 16)),
                RotationZ = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 20)),
                ScaleX = BitConverter.ToSingle(bytes, offset + 24),
                ScaleY = BitConverter.ToSingle(bytes, offset + 28),
                ScaleZ = BitConverter.ToSingle(bytes, offset + 32),
            };
        }
    }

    [YAXSerializeAs("HierarchyNode")]
    public class FMP_HierarchyNode
    {
        [YAXAttributeForClass]
        public byte Type { get; set; } //Test value range for this. Maybe its a a boolean, 1 = transforms, 0 = children
        [YAXAttributeForClass]
        [YAXHexValue]
        public byte TypeB { get; set; }
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        [CustomSerialize]
        public ushort[] Indices { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        [CustomSerialize]
        public float[] Values { get; set; } //size 3 array

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "HierarchyNode")]
        [YAXDontSerializeIfNull]
        public List<FMP_HierarchyNode> HierarchyNodes { get; set; }

        public void Write(List<byte> bytes)
        {
            bytes.Add(Type);
            bytes.Add(TypeB);
            int indicesCount = Indices?.Length ?? 0;
            bytes.AddRange(BitConverter.GetBytes((ushort)indicesCount));

            if (Type == 0)
            {
                //Has children
                if (Values?.Length != 3)
                    throw new Exception("FMP_HierarchyNode: Invalid number of Values.");

                bytes.AddRange(BitConverter.GetBytes(Values[0]));
                bytes.AddRange(BitConverter.GetBytes(Values[1]));
                bytes.AddRange(BitConverter.GetBytes(Values[2]));
                int childrenOffsets = bytes.Count;
                bytes.AddRange(new byte[4 * 8]); //Offsets for children

                for (int i = 0; i < 8; i++)
                {
                    FMP_HierarchyNode child = HierarchyNodes.FirstOrDefault(x => x.Index == i);

                    if(child != null)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), childrenOffsets + (4 * i));
                        child.Write(bytes);
                    }
                }
            }
            else if (Type == 1)
            {
                for(int i = 0; i < indicesCount; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(Indices[i]));
                }

                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
            }
            else
            {
                throw new Exception("FMP_HierarchyNode: Type is invalid value");
            }

        }

        public static FMP_HierarchyNode Read(byte[] bytes, int offset, int index = 0)
        {
            FMP_HierarchyNode node = new FMP_HierarchyNode();
            node.Type = bytes[offset];
            node.TypeB = bytes[offset + 1];
            node.Index = index;
            
            if(node.Type == 0)
            {
                //Has children
                node.Values = BitConverter_Ex.ToFloat32Array(bytes, offset + 4, 3);
                int[] offsets = BitConverter_Ex.ToInt32Array(bytes, offset + 16, 8);
                node.HierarchyNodes = new List<FMP_HierarchyNode>();

                for(int i = 0; i < offsets.Length; i++)
                {
                    if (offsets[i] != 0 && offsets[i] != offset)
                    {
                        node.HierarchyNodes.Add(Read(bytes, offsets[i], i));
                    }
                }
            }
            else if(node.Type == 1)
            {
                //No children, instead has indices (of a FMP_Node)
                int indicesCount = BitConverter.ToUInt16(bytes, offset + 2);
                node.Indices = BitConverter_Ex.ToUInt16Array(bytes, offset + 4, indicesCount);
            }
            else
            {
                throw new Exception("FMP_HierarchyNode: Unexpected Type value of " + node.Type);
            }

            return node;
        }
    }

    public class FMP_Action
    {
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        public byte I_04 { get; set; }
        [YAXAttributeForClass]
        public byte I_05 { get; set; }
        [YAXAttributeForClass]
        public byte I_06 { get; set; }
        [YAXAttributeForClass]
        public byte I_07 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        [YAXDontSerializeIfNull]
        public List<FMP_Command> Commands { get; set; }

        internal void Write(List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int commandCount = Commands?.Count ?? 0;
            int actionStart = bytes.Count;

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.Add(I_04);
            bytes.Add(I_05);
            bytes.Add(I_06);
            bytes.Add(I_07);
            bytes.AddRange(BitConverter.GetBytes(commandCount));
            bytes.AddRange(BitConverter.GetBytes(commandCount > 0 ? (actionStart + 16) : 0)); //Comes straight after Action block

            //Write commands
            int commandStart = bytes.Count;

            for (int i = 0; i < commandCount; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = Commands[i].Name });
                bytes.AddRange(new byte[4]);
                bytes.Add(Commands[i].I_04);
                bytes.Add(Commands[i].I_05);
                bytes.Add(Commands[i].I_06);
                bytes.Add(Commands[i].I_07);
                bytes.AddRange(BitConverter.GetBytes(Commands[i].Parameters?.Count ?? 0));
                bytes.AddRange(new byte[4]); //Offset to parameters
            }

            //Write parameters
            for(int i = 0; i < commandCount; i++)
            {
                int parameterStart = bytes.Count;
                int parameterCount = Commands[i].Parameters?.Count ?? 0;

                if(parameterCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), commandStart + 12 + (16 * i));

                    for (int a = 0; a < parameterCount; a++)
                    {
                        stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = Commands[i].Parameters[a].Name });
                        bytes.AddRange(new byte[4]);
                        bytes.AddRange(BitConverter.GetBytes((int)Commands[i].Parameters[a].Type));

                        if(Commands[i].Parameters[a].Type == FMP_Parameter.ParameterType.String)
                        {
                            stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = Commands[i].Parameters[a].Value as string });
                        }

                        bytes.AddRange(new byte[4]);
                    }

                    //Write values for this commands parameters
                    for (int a = 0; a < parameterCount; a++)
                    {
                        if (Commands[i].Parameters[a].Type == FMP_Parameter.ParameterType.String) continue; //String is handled by StringWriter

                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), parameterStart + 8 + (12 * a));

                        switch (Commands[i].Parameters[a].Type)
                        {
                            case FMP_Parameter.ParameterType.Int:
                                int val = (int)Commands[i].Parameters[a].Value;
                                bytes.AddRange(BitConverter.GetBytes(val));
                                break;
                            case FMP_Parameter.ParameterType.UInt:
                                bytes.AddRange(BitConverter.GetBytes((uint)Commands[i].Parameters[a].Value));
                                break;
                            case FMP_Parameter.ParameterType.Float:
                                bytes.AddRange(BitConverter.GetBytes((float)Commands[i].Parameters[a].Value));
                                break;
                            case FMP_Parameter.ParameterType.Direction:
                            case FMP_Parameter.ParameterType.Position:
                                if (Commands[i].Parameters[a].Value is float[] values && values.Length == 4)
                                {
                                    bytes.AddRange(BitConverter_Ex.GetBytes(values));
                                }
                                break;
                            case FMP_Parameter.ParameterType.Bool:
                                bool value = ((bool)Commands[i].Parameters[a].Value);
                                bytes.AddRange(BitConverter.GetBytes(value ? 1 : 0)); //Write as a 32 bit value
                                break;
                        }
                    }
                }
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
        }

        public static FMP_Action Read(byte[] bytes, int offset)
        {
            FMP_Action action = new FMP_Action();
            action.I_00 = BitConverter.ToInt32(bytes, offset);
            action.I_04 = bytes[offset + 4];
            action.I_05 = bytes[offset + 5];
            action.I_06 = bytes[offset + 6];
            action.I_07 = bytes[offset + 7];

            int commandCount = BitConverter.ToInt32(bytes, offset + 8);
            int commandOffset = BitConverter.ToInt32(bytes, offset + 12);
            action.Commands = FMP_Command.ReadAll(bytes, commandOffset, commandCount);

            return action;
        }

    }

    public class FMP_Command
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public byte I_04 { get; set; }
        [YAXAttributeForClass]
        public byte I_05 { get; set; }
        [YAXAttributeForClass]
        public byte I_06 { get; set; }
        [YAXAttributeForClass]
        public byte I_07 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Parameter")]
        [YAXDontSerializeIfNull]
        public List<FMP_Parameter> Parameters { get; set; }

        public static List<FMP_Command> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Command> entries = new List<FMP_Command>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (16 * i)));
            }

            return entries;
        }

        public static FMP_Command Read(byte[] bytes, int offset)
        {
            FMP_Command command = new FMP_Command();

            int nameOffset = BitConverter.ToInt32(bytes, offset);

            command.Name = nameOffset > 0 ? StringEx.GetString(bytes, nameOffset) : null;
            command.I_04 = bytes[offset + 4];
            command.I_05 = bytes[offset + 5];
            command.I_06 = bytes[offset + 6];
            command.I_07 = bytes[offset + 7];

            int parameterCount = BitConverter.ToInt32(bytes, offset + 8);
            int parameterOffset = BitConverter.ToInt32(bytes, offset + 12);

            command.Parameters = FMP_Parameter.ReadAll(bytes, parameterOffset, parameterCount);

            return command;
        }
    }

    public class FMP_Parameter
    {
        public enum ParameterType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            Direction = 3, //normalized vector4
            Position = 4, //position vector4
            String = 6,
            UInt = 8
        }

        [YAXDontSerialize]
        public object Value { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ParameterType Type { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Value")]
        public string XmlValue
        {
            get => GetXmlFormattedValue();
            set => SetValueFromXmlFormattedValue(value);
        }



        public static List<FMP_Parameter> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Parameter> entries = new List<FMP_Parameter>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (12 * i)));
            }

            return entries;
        }

        public static FMP_Parameter Read(byte[] bytes, int offset)
        {
            FMP_Parameter parameter = new FMP_Parameter();

            int nameOffset = BitConverter.ToInt32(bytes, offset);
            parameter.Name = nameOffset > 0 ? StringEx.GetString(bytes, nameOffset) : null;
            parameter.Type = (ParameterType)BitConverter.ToInt32(bytes, offset + 4);
            int valueOffset = BitConverter.ToInt32(bytes, offset + 8);

            //if ==6, unk1_offset is a nameOffset. so it's a type. 0: bool , 1: uint, 2  float)

            switch (parameter.Type)
            {
                case ParameterType.Int:
                    parameter.Value = BitConverter.ToInt32(bytes, valueOffset);
                    break;
                case ParameterType.Bool:
                    parameter.Value = BitConverter.ToInt32(bytes, valueOffset) == 1;
                    break;
                case ParameterType.Float:
                    parameter.Value = BitConverter.ToSingle(bytes, valueOffset);
                    break;
                case ParameterType.Direction:
                case ParameterType.Position:
                    parameter.Value = BitConverter_Ex.ToFloat32Array(bytes, valueOffset, 4);
                    break;
                case ParameterType.String:
                    parameter.Value = StringEx.GetString(bytes, valueOffset);
                    break;
                case ParameterType.UInt:
                    parameter.Value = BitConverter.ToUInt32(bytes, valueOffset);
                    break;
                default:
                    throw new Exception("FMP_Parameter: unknown parameter type " + parameter.Type);
            }

            return parameter;
        }

        private void SetValueFromXmlFormattedValue(string value)
        {
            switch (Type)
            {
                case ParameterType.Int:
                    Value = int.Parse(value);
                    break;
                case ParameterType.Bool:
                    Value = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case ParameterType.UInt:
                    Value = uint.Parse(value);
                    break;
                case ParameterType.Float:
                    Value = float.Parse(value);
                    break;
                case ParameterType.String:
                    Value = value;
                    break;
                case ParameterType.Direction:
                case ParameterType.Position:
                    string[] values = value.Replace(" ", "").Split(',');

                    if(values.Length != 4)
                    {
                        throw new InvalidDataException($"FMP_Parameter: Type {Type} must have 4 values.");
                    }

                    float[] vector4 = new float[4];
                    for(int i = 0; i < 4; i++)
                    {
                        float val;
                        if (!float.TryParse(values[i], out val))
                            throw new InvalidDataException($"FMP_Parameter: Couldn't parse the values on type {Type}");

                        vector4[i] = val;
                    }

                    Value = vector4;
                    break;
                default:
                    throw new Exception("FMP_Parameter: Unknown parameter type " + Type);
            }
        }

        private string GetXmlFormattedValue()
        {
            switch (Type)
            {
                case ParameterType.Direction:
                case ParameterType.Position:
                    if(Value is float[] vector4)
                    {
                        if (vector4.Length != 4)
                            throw new InvalidDataException($"FMP_Parameter: Type {Type} must have 4 values.");

                        return $"{vector4[0]}, {vector4[1]}, {vector4[2]}, {vector4[3]}";
                    }
                    else
                    {
                        throw new InvalidDataException("FMP_Parameter.GetXmlFormattedValue: Type does not match the internal value.");
                    }
                case ParameterType.Int:
                case ParameterType.Bool:
                case ParameterType.Float:
                case ParameterType.String:
                case ParameterType.UInt:
                default:
                    return Value.ToString();
            }
        }
    }

    [YAXSerializeAs("Entity")]
    public class FMP_Entity
    {
        [CustomSerialize]
        public int I_04 { get; set; }
        public FMP_Visual Visual { get; set; }
        public FMP_Matrix Matrix { get; set; }

        internal static void Write(List<FMP_Entity> entities, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int entityStart = bytes.Count;

            for(int i = 0; i < entities.Count; i++)
            {
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entities[i].I_04));
                bytes.AddRange(entities[i].Matrix.Write());
            }

            //Write visual subcomponent
            int visualStart = bytes.Count;

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Visual == null) continue;

                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), entityStart + (56 * i));

                if (entities[i].Visual.LODs.Count == 0)
                    throw new Exception("FMP_Visual: cannot have zero LODs");

                stringWriter.Add(new StringWriter.StringInfo() { StringToWrite = entities[i].Visual.Name, Offset = bytes.Count });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.I_04));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count > 1 ? 0 : entities[i].Visual.LODs[0].DepotNskIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotEmbIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count > 1 ? 0 : entities[i].Visual.LODs[0].DepotEmmIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.I_24));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.I_28));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotEmaIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.I_36));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.F_40));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.F_44));

                if (entities[i].Visual.LODs.Count > 1)
                {
                    bytes.AddRange(BitConverter.GetBytes(0));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs[0].Distance));
                }
            }

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Visual == null) continue;

                if (entities[i].Visual.LODs.Count > 1)
                {
                    //Distances
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 48 + (52 * i));

                    foreach(var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.Distance));
                    }

                    //NSKs
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 12 + (52 * i));

                    foreach (var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.DepotNskIndex));
                    }

                    //EMMs
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 20 + (52 * i));

                    foreach (var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.DepotEmmIndex));
                    }

                }
            }
        }

        public static List<FMP_Entity> ReadAll(byte[] bytes, int offset, int count, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            List<FMP_Entity> entries = new List<FMP_Entity>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (56 * i), depot1, depot2, depot3, depot4));
            }

            return entries;
        }

        public static FMP_Entity Read(byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            int visualOffset = BitConverter.ToInt32(bytes, offset);

            return new FMP_Entity()
            {
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                Visual = visualOffset > 0 ? FMP_Visual.Read(bytes, visualOffset, depot1, depot2, depot3, depot4) : null,
                Matrix = FMP_Matrix.Read(bytes, offset + 8)
            };
        }

    }

    public class FMP_Visual
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [CustomSerialize]
        public int I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "LOD")]
        public List<FMP_Lod> LODs { get; set; } = new List<FMP_Lod>();

        [CustomSerialize]
        public string EmbFile { get; set; }
        [CustomSerialize]
        public string EmaFile { get; set; } //32
        [CustomSerialize]
        public int I_24 { get; set; }
        [CustomSerialize]
        public int I_28 { get; set; }
        [CustomSerialize]
        public int I_36 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }

        [YAXDontSerialize]
        internal int DepotEmbIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmaIndex { get; private set; }

        public static FMP_Visual Read(byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            FMP_Visual visual = new FMP_Visual();

            visual.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));
            visual.I_04 = BitConverter.ToInt32(bytes, offset + 4);
            visual.I_24 = BitConverter.ToInt32(bytes, offset + 24);
            visual.I_28 = BitConverter.ToInt32(bytes, offset + 28);
            visual.I_36 = BitConverter.ToInt32(bytes, offset + 36);
            visual.F_40 = BitConverter.ToSingle(bytes, offset + 40);
            visual.F_44 = BitConverter.ToSingle(bytes, offset + 44);

            int embIndex = BitConverter.ToInt32(bytes, offset + 16);
            int emaIndex = BitConverter.ToInt32(bytes, offset + 32);

            visual.EmbFile = embIndex != -1 ? depot2[embIndex] : null;
            visual.EmaFile = emaIndex != -1 ? depot4[emaIndex] : null;

            //Read lods
            int lodCount = BitConverter.ToInt32(bytes, offset + 8);

            if(lodCount > 1)
            {
                int nskOffset = BitConverter.ToInt32(bytes, offset + 12);
                int emmOffset = BitConverter.ToInt32(bytes, offset + 20);
                int lodDistanceOffset = BitConverter.ToInt32(bytes, offset + 48);

                for(int i = 0; i < lodCount; i++)
                {
                    FMP_Lod lod = new FMP_Lod();

                    int nskIndex = BitConverter.ToInt32(bytes, nskOffset + (4 * i));
                    int emmIndex = BitConverter.ToInt32(bytes, emmOffset + (4 * i));

                    lod.NskFile = nskIndex != -1 ? depot1[nskIndex] : null;
                    lod.EmmFile = emmIndex != -1 ? depot3[emmIndex] : null;
                    lod.Distance = BitConverter.ToSingle(bytes, lodDistanceOffset + (4 * i));

                    visual.LODs.Add(lod);
                }
            }
            else
            {
                visual.LODs.Add(new FMP_Lod());
                int nskIndex = BitConverter.ToInt32(bytes, offset + 12);
                int emmIndex = BitConverter.ToInt32(bytes, offset + 20);

                visual.LODs[0].NskFile = nskIndex != -1 ? depot1[nskIndex] : null;
                visual.LODs[0].EmmFile = emmIndex != -1 ? depot3[emmIndex] : null;
                visual.LODs[0].Distance = BitConverter.ToSingle(bytes, offset + 48);
            }

            return visual;
        }
    
        public void CreateDepots(List<string> depot1, List<string> depot2,  List<string> depot3, List<string> depot4)
        {
            foreach(var lod in LODs)
            {
                lod.CreateDepots(depot1, depot3);
            }

            DepotEmbIndex = -1;
            DepotEmaIndex = -1;

            if (!string.IsNullOrWhiteSpace(EmbFile))
            {
                DepotEmbIndex = depot2.IndexOf(EmbFile);

                if (DepotEmbIndex == -1)
                {
                    DepotEmbIndex = depot2.Count;
                    depot2.Add(EmbFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(EmaFile))
            {
                DepotEmaIndex = depot4.IndexOf(EmaFile);

                if (DepotEmaIndex == -1)
                {
                    DepotEmaIndex = depot4.Count;
                    depot4.Add(EmaFile);
                }
            }
        }
    }

    [YAXSerializeAs("LOD")]
    public class FMP_Lod
    {
        [YAXAttributeForClass]
        public float Distance { get; set; }
        [YAXAttributeForClass]
        public string NskFile { get; set; }
        [YAXAttributeForClass]
        public string EmmFile { get; set; }

        [YAXDontSerialize]
        internal int DepotNskIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmmIndex { get; private set; }

        public void CreateDepots(List<string> depot1, List<string> depot3)
        {
            DepotNskIndex = -1;
            DepotEmmIndex = -1;

            if (!string.IsNullOrWhiteSpace(EmmFile))
            {
                DepotEmmIndex = depot3.IndexOf(EmmFile);

                if (DepotEmmIndex == -1)
                {
                    DepotEmmIndex = depot3.Count;
                    depot3.Add(EmmFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(NskFile))
            {
                DepotNskIndex = depot1.IndexOf(NskFile);

                if (DepotNskIndex == -1)
                {
                    DepotNskIndex = depot1.Count;
                    depot1.Add(NskFile);
                }
            }
        }
    }

    [YAXSerializeAs("Matrix")]
    public class FMP_Matrix
    {
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public float[] L0 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public float[] L1 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public float[] L2 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public float[] L3 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            if(L0.Length != 3 || L1.Length != 3 || L2.Length != 3 || L3.Length != 3)
            {
                throw new Exception("FMP_Matrix: invalid number of elements in array. There must be 3 values per line.");
            }

            bytes.AddRange(BitConverter_Ex.GetBytes(L0));
            bytes.AddRange(BitConverter_Ex.GetBytes(L1));
            bytes.AddRange(BitConverter_Ex.GetBytes(L2));
            bytes.AddRange(BitConverter_Ex.GetBytes(L3));

            if (bytes.Count != 48)
                throw new Exception("FMP_Matrix: invalid size");

            return bytes.ToArray();
        }

        public static FMP_Matrix Read(byte[] bytes, int offset)
        {
            return new FMP_Matrix()
            {
                L0 = BitConverter_Ex.ToFloat32Array(bytes, offset, 3),
                L1 = BitConverter_Ex.ToFloat32Array(bytes, offset + 12, 3),
                L2 = BitConverter_Ex.ToFloat32Array(bytes, offset + 24, 3),
                L3 = BitConverter_Ex.ToFloat32Array(bytes, offset + 36, 3)
            };
        }
    }

    [YAXSerializeAs("VirtualSubPart")]
    public class FMP_VirtualSubPart
    {
        [CustomSerialize]
        public ushort I_20 { get; set; }
        [CustomSerialize]
        public ushort I_22 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_28 { get; set; }
        public FMP_Matrix Matrix { get; set; }

        [YAXDontSerializeIfNull]
        public List<FMP_IndexPair> IndexPairs { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_ObjectSubPart SubPart1 { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_ObjectSubPart SubPart2 { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_Action Action { get; set; }

        public static List<FMP_VirtualSubPart> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_VirtualSubPart> entries = new List<FMP_VirtualSubPart>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (80 * i)));
            }

            return entries;
        }

        public static FMP_VirtualSubPart Read(byte[] bytes, int offset)
        {
            FMP_VirtualSubPart virtualPart = new FMP_VirtualSubPart();
            virtualPart.I_20 = BitConverter.ToUInt16(bytes, offset + 20);
            virtualPart.I_22 = BitConverter.ToUInt16(bytes, offset + 22);
            virtualPart.F_24 = BitConverter.ToSingle(bytes, offset + 24);
            virtualPart.F_28 = BitConverter.ToSingle(bytes, offset + 28);
            virtualPart.Matrix = FMP_Matrix.Read(bytes, offset + 32);

            int indexPairCount = BitConverter.ToInt32(bytes, offset);
            int indexPairOffset = BitConverter.ToInt32(bytes, offset + 4);
            int subPart1 = BitConverter.ToInt32(bytes, offset + 8);
            int subPart2 = BitConverter.ToInt32(bytes, offset + 12);
            int nextD_Offset = BitConverter.ToInt32(bytes, offset + 16);

            virtualPart.IndexPairs = FMP_IndexPair.ReadAll(bytes, indexPairOffset, indexPairCount);

            if (subPart1 > 0)
                virtualPart.SubPart1 = FMP_ObjectSubPart.Read(bytes, subPart1);

            if (subPart2 > 0)
                virtualPart.SubPart2 = FMP_ObjectSubPart.Read(bytes, subPart2);

            if(nextD_Offset > 0)
                virtualPart.Action = FMP_Action.Read(bytes, nextD_Offset);


            return virtualPart;
        }

    }

    [YAXSerializeAs("ObjectSubPart")]
    public class FMP_ObjectSubPart
    {
        [CustomSerialize(parent: "Width", serializeAs: "X", isFloat: true)]
        public float WidthX { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Y", isFloat: true)]
        public float WidthY { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Z", isFloat: true)]
        public float WidthZ { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "X", isFloat: true)]
        public float QuaternionX { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "Y", isFloat: true)]
        public float QuaternionY { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "Z", isFloat: true)]
        public float QuaternionZ { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "W", isFloat: true)]
        public float QuaternionW { get; set; }

        [CustomSerialize]
        public ushort I_00 { get; set; }
        [CustomSerialize]
        public ushort I_02 { get; set; }
        [CustomSerialize]
        public ushort I_04 { get; set; }
        [CustomSerialize]
        public ushort I_06 { get; set; }
        [CustomSerialize]
        public int I_08 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_16 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_20 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_28 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_48 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_52 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(WidthX));
            bytes.AddRange(BitConverter.GetBytes(WidthY));
            bytes.AddRange(BitConverter.GetBytes(WidthZ));
            bytes.AddRange(BitConverter.GetBytes(QuaternionX));
            bytes.AddRange(BitConverter.GetBytes(QuaternionY));
            bytes.AddRange(BitConverter.GetBytes(QuaternionZ));
            bytes.AddRange(BitConverter.GetBytes(QuaternionW));

            if (bytes.Count != 84)
                throw new Exception("FMP_FMP_ObjectSubPart.Write: Invalid size");

            return bytes.ToArray();
        }

        public static FMP_ObjectSubPart Read(byte[] bytes, int offset)
        {
            return new FMP_ObjectSubPart()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset),
                I_02 = BitConverter.ToUInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                I_08 = BitConverter.ToInt32(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                WidthX = BitConverter.ToSingle(bytes, offset + 56),
                WidthY = BitConverter.ToSingle(bytes, offset + 60),
                WidthZ = BitConverter.ToSingle(bytes, offset + 64),
                QuaternionX = BitConverter.ToSingle(bytes, offset + 68),
                QuaternionY = BitConverter.ToSingle(bytes, offset + 72),
                QuaternionZ = BitConverter.ToSingle(bytes, offset + 76),
                QuaternionW = BitConverter.ToSingle(bytes, offset + 80),
            };
        }
    }

    [YAXSerializeAs("IndexPair")]
    public class FMP_IndexPair
    {
        [YAXAttributeForClass]
        public int Index0 { get; set; }
        [YAXAttributeForClass]
        public int Index1 { get; set; }

        public static List<FMP_IndexPair> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_IndexPair> indices = new List<FMP_IndexPair>();

            for(int i = 0; i < count; i++)
            {
                indices.Add(new FMP_IndexPair()
                {
                    Index0 = BitConverter.ToInt32(bytes, offset + (8 * i)),
                    Index1 = BitConverter.ToInt32(bytes, offset + 4 + (8 * i))
                });
            }

            return indices;
        }
    }

    [YAXSerializeAs("HitboxGroup")]
    public class FMP_HitboxGroup : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [BindingAutoId()]
        public string Index { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        public List<FMP_Hitbox> Hitboxes { get; set; } = new List<FMP_Hitbox>();

        internal static void WriteAll(List<FMP_HitboxGroup> hitboxGroups, List<byte> bytes, List<StringWriter.StringInfo> stringWriter, bool oldVersion)
        {
            int hitboxGroupStart = bytes.Count;

            //Write HitboxGroup
            for(int i = 0; i < hitboxGroups.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { StringToWrite = hitboxGroups[i].Name, Offset = bytes.Count });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(hitboxGroups[i].Hitboxes?.Count ?? 0));
                bytes.AddRange(new byte[4]);
            }

            //Write Hitboxes, per group
            
            for(int i = 0; i < hitboxGroups.Count; i++)
            {
                if ((hitboxGroups[i].Hitboxes?.Count ?? 0) == 0) continue;
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxGroupStart + 8 + (i * 12));

                int hitboxStart = bytes.Count;

                for (int a = 0; a < hitboxGroups[i].Hitboxes.Count; a++)
                {
                    FMP_Hitbox hitbox = hitboxGroups[i].Hitboxes[a];

                    if(!string.IsNullOrWhiteSpace(hitbox.Name))
                        stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = hitbox.Name });

                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(hitbox.ChildIdx));
                    bytes.AddRange(BitConverter.GetBytes(hitbox.unk_a0));
                    bytes.AddRange(BitConverter.GetBytes(hitbox.SiblingIdx));
                    bytes.AddRange(BitConverter.GetBytes(hitbox.ParentIdx));
                    bytes.AddRange(new byte[4]); //DestructionList count, easier to fill in later as the lists are only created when written to binary
                    bytes.AddRange(new byte[4]);

                    if(!oldVersion)
                        bytes.AddRange(new byte[4]); //HVK file offset, only in newer versions

                    bytes.AddRange(BitConverter.GetBytes(hitbox.CollisionVertexData?.Vertices?.Count ?? 0));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(hitbox.CollisionVertexData?.Faces?.Length ?? 0));
                    bytes.AddRange(new byte[4]);
                }

                //Write sub data
                for (int a = 0; a < hitboxGroups[i].Hitboxes.Count; a++)
                {
                    FMP_Hitbox hitbox = hitboxGroups[i].Hitboxes[a];
                    int hitboxSize = oldVersion ? 36 : 40;

                    //Destruction Groups
                    if(hitbox.Destructions != null)
                    {
                        List<List<FMP_Destruction>> destructionGroups = FMP_Destruction.CreateDesructionGroups(hitbox.Destructions);

                        if (destructionGroups.Count > 0)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(destructionGroups.Count), hitboxStart + 12 + (hitboxSize * a));
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + 16 + (hitboxSize * a));
                            FMP_Destruction.Write(destructionGroups, bytes);
                        }
                    }

                    //Vertices
                    if(hitbox.CollisionVertexData?.Vertices?.Count > 0)
                    {
                        int verticesOffset = oldVersion ? 24 : 28;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + verticesOffset + (hitboxSize * a));

                        foreach (var vertex in hitbox.CollisionVertexData?.Vertices)
                        {
                            if (vertex.Pos.Length != 3) throw new Exception("Vertex array length not correct - must have 3 position values (XYZ)");
                            if (vertex.Normal.Length != 3) throw new Exception("Vertex array length not correct - must have 3 normal values (XYZ)");
                            bytes.AddRange(BitConverter_Ex.GetBytes(vertex.Pos));
                            bytes.AddRange(BitConverter_Ex.GetBytes(vertex.Normal));
                        }
                    }

                    //Face indices
                    if (hitbox.CollisionVertexData?.Vertices?.Count > 0)
                    {
                        int indicesOffset = oldVersion ? 32 : 36;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + indicesOffset + (hitboxSize * a));
                        
                        bytes.AddRange(BitConverter_Ex.GetBytes(hitbox.CollisionVertexData.Faces));
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                    }

                    //HVK file
                    if(hitbox.HvkCollisionData != null && !oldVersion)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxStart + 20 + (hitboxSize * a));

                        bytes.AddRange(BitConverter.GetBytes(hitbox.HvkCollisionData.I_00));
                        bytes.AddRange(BitConverter.GetBytes(hitbox.HvkCollisionData.HvkFile?.Length ?? 0));
                        bytes.AddRange(BitConverter.GetBytes(hitbox.HvkCollisionData.HvkFile?.Length > 0 ? bytes.Count + 4 : 0)); //Right after 
                        
                        if(hitbox.HvkCollisionData.HvkFile?.Length > 0)
                        {
                            bytes.AddRange(hitbox.HvkCollisionData.HvkFile);
                            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                        }
                    }
                }
            }
        }

        public static List<FMP_HitboxGroup> ReadAll(byte[] bytes, int offset, int count, bool oldVersion)
        {
            List<FMP_HitboxGroup> hitboxes = new List<FMP_HitboxGroup>();

            for(int i = 0; i < count; i++)
            {
                hitboxes.Add(Read(bytes, offset + (12 * i), i, oldVersion));
            }

            return hitboxes;
        }

        public static FMP_HitboxGroup Read(byte[] bytes, int offset, int index, bool oldVersion)
        {
            FMP_HitboxGroup hitboxGroup = new FMP_HitboxGroup();
            hitboxGroup.Index = index.ToString();
            hitboxGroup.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));

            int hitboxCount = BitConverter.ToInt32(bytes, offset + 4);
            int hitboxOffset = BitConverter.ToInt32(bytes, offset + 8);

            for(int i = 0; i < hitboxCount; i++)
            {
                hitboxGroup.Hitboxes.Add(FMP_Hitbox.Read(bytes, hitboxOffset + (i * 40), oldVersion));
            }

            return hitboxGroup;
        }
    }

    [YAXSerializeAs("Hitbox")]
    public class FMP_Hitbox
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [CustomSerialize]
        public ushort ChildIdx { get; set; }
        [CustomSerialize]
        public ushort unk_a0 { get; set; }
        [CustomSerialize]
        public ushort SiblingIdx { get; set; }
        [CustomSerialize]
        public ushort ParentIdx { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Destruction")]
        [YAXDontSerializeIfNull]
        public List<FMP_Destruction> Destructions { get; set; } = new List<FMP_Destruction>();

        [YAXDontSerializeIfNull]
        public FMP_CollisionVertexData CollisionVertexData { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_HvkCollisionData HvkCollisionData { get; set; }

        public static FMP_Hitbox Read(byte[] bytes, int offset, bool oldVersion) 
        {
            FMP_Hitbox hitbox = new FMP_Hitbox();

            hitbox.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));
            hitbox.ChildIdx = BitConverter.ToUInt16(bytes, offset + 4);
            hitbox.unk_a0 = BitConverter.ToUInt16(bytes, offset + 6);
            hitbox.SiblingIdx = BitConverter.ToUInt16(bytes, offset + 8);
            hitbox.ParentIdx = BitConverter.ToUInt16(bytes, offset + 10);

            int destructionListCount = BitConverter.ToInt32(bytes, offset + 12);
            int destructionListOffset = BitConverter.ToInt32(bytes, offset + 16);
            int hvkGeometryOffset = 0;
            int vertexDataCount = 0;
            int vertexDataOffset = 0;
            int faceIndicesCount = 0;
            int faceIndicesOffset = 0;

            if (oldVersion)
            {
                vertexDataCount = BitConverter.ToInt32(bytes, offset + 20);
                vertexDataOffset = BitConverter.ToInt32(bytes, offset + 24);
                faceIndicesCount = BitConverter.ToInt32(bytes, offset + 28);
                faceIndicesOffset = BitConverter.ToInt32(bytes, offset + 32);
            }
            else
            {
                hvkGeometryOffset = BitConverter.ToInt32(bytes, offset + 20);
                vertexDataCount = BitConverter.ToInt32(bytes, offset + 24);
                vertexDataOffset = BitConverter.ToInt32(bytes, offset + 28);
                faceIndicesCount = BitConverter.ToInt32(bytes, offset + 32);
                faceIndicesOffset = BitConverter.ToInt32(bytes, offset + 36);
            }

            hitbox.Destructions = FMP_Destruction.ReadAll(bytes, destructionListOffset, destructionListCount);
            hitbox.CollisionVertexData = FMP_CollisionVertexData.Read(bytes, vertexDataOffset, vertexDataCount, faceIndicesOffset, faceIndicesCount);

            if (hvkGeometryOffset > 0 && !oldVersion)
                hitbox.HvkCollisionData = FMP_HvkCollisionData.Read(bytes, hvkGeometryOffset);

            return hitbox;
        }

    }

    [YAXSerializeAs("Destruction")]
    public class FMP_Destruction
    {
        [YAXAttributeForClass]
        public int Group { get; set; }
        [CustomSerialize]
        public int I_00 { get; set; }
        [CustomSerialize]
        public int I_08 { get; set; }
        [CustomSerialize]
        public int I_12 { get; set; }
        [CustomSerialize]
        public int I_28 { get; set; }
        [CustomSerialize]
        public int I_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }

        [YAXDontSerializeIfNull]
        public FMP_DestructionSubPart SubPart1 { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_DestructionSubPart SubPart2 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public byte[] HvkFile { get; set; }

        public static List<FMP_Destruction> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Destruction> destructionList = new List<FMP_Destruction>();

            for(int i = 0; i < count; i++)
            {
                int destCount = BitConverter.ToInt32(bytes, offset + (i * 8));
                int destOffset = BitConverter.ToInt32(bytes, offset + 4 + (i * 8));

                for(int a = 0; a < destCount; a++)
                {
                    destructionList.Add(Read(bytes, destOffset + (a * 40), i));
                }
            }

            return destructionList;
        }

        public static FMP_Destruction Read(byte[] bytes, int offset, int group)
        {
            FMP_Destruction destruction = new FMP_Destruction();

            destruction.Group = group;
            destruction.I_00 = BitConverter.ToInt32(bytes, offset);
            destruction.I_08 = BitConverter.ToInt32(bytes, offset + 8);
            destruction.I_12 = BitConverter.ToInt32(bytes, offset + 12);
            destruction.I_28 = BitConverter.ToInt32(bytes, offset + 28);
            destruction.I_32 = BitConverter.ToInt32(bytes, offset + 32);
            destruction.F_36 = BitConverter.ToSingle(bytes, offset + 36);

            int subPartOffset = BitConverter.ToInt32(bytes, offset + 4);
            int subPartOffset2 = BitConverter.ToInt32(bytes, offset + 24);
            int hvkFileSize = BitConverter.ToInt32(bytes, offset + 16);
            int hvkFileOffset = BitConverter.ToInt32(bytes, offset + 20);

            destruction.HvkFile = bytes.GetRange(hvkFileOffset, hvkFileSize);

            if (subPartOffset > 0)
                destruction.SubPart1 = FMP_DestructionSubPart.Read(bytes, subPartOffset);

            if (subPartOffset2 > 0)
                destruction.SubPart2 = FMP_DestructionSubPart.Read(bytes, subPartOffset2);

            return destruction;
        }

        internal static void Write(List<List<FMP_Destruction>> groups, List<byte> bytes)
        {
            int listStart = bytes.Count;

            //Write DestructionList, in sequence
            for(int i = 0; i < groups.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(groups[i].Count));
                bytes.AddRange(new byte[4]);
            }

            for(int i = 0; i < groups.Count; i++)
            {
                //Write Desruction entries, in sequence
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), listStart + 4 + (i * 8));
                int destStart = bytes.Count;

                for(int a = 0; a < groups[i].Count; a++)
                {
                    FMP_Destruction destruction = groups[i][a];
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_00));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_08));
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_12));
                    bytes.AddRange(BitConverter.GetBytes(destruction.HvkFile?.Length ?? 0));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_28));
                    bytes.AddRange(BitConverter.GetBytes(destruction.I_32));
                    bytes.AddRange(BitConverter.GetBytes(destruction.F_36));
                }

                //Write sub data, per Destruction
                for (int a = 0; a < groups[i].Count; a++)
                {
                    FMP_Destruction destruction = groups[i][a];

                    if(destruction.SubPart1 != null)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 4 + (a * 40));
                        bytes.AddRange(destruction.SubPart1.Write());
                    }

                    if(destruction.HvkFile?.Length > 0)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 20 + (a * 40));
                        bytes.AddRange(destruction.HvkFile);
                        bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
                    }

                    if (destruction.SubPart2 != null)
                    {
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), destStart + 24 + (a * 40));
                        bytes.AddRange(destruction.SubPart2.Write());
                    }
                }

            }
        }

        internal static List<List<FMP_Destruction>> CreateDesructionGroups(List<FMP_Destruction> destructions)
        {
            List<int> groupValues = destructions.Select(x => x.Group).ToList();
            groupValues.Sort();

            List<List<FMP_Destruction>> groups = new List<List<FMP_Destruction>>();

            foreach(var group in groupValues)
            {
                List<FMP_Destruction> destructionGroup = destructions.Where(x => x.Group == group).ToList();
                groups.Add(destructionGroup);
            }

            return groups;
        }
    }

    [YAXSerializeAs("DestructionSubPart")]
    public class FMP_DestructionSubPart
    {
        [CustomSerialize]
        public int I_00 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_04 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_08 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_12 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Yaw { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Pitch { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Roll { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_28 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_48 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_52 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(Yaw));
            bytes.AddRange(BitConverter.GetBytes(Pitch));
            bytes.AddRange(BitConverter.GetBytes(Roll));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));

            if (bytes.Count != 56)
                throw new Exception("FMP_DestructionSubPart: Invalid size");

            return bytes.ToArray();
        }

        public static FMP_DestructionSubPart Read(byte[] bytes, int offset)
        {
            return new FMP_DestructionSubPart()
            {
                I_00 = BitConverter.ToInt32(bytes, offset),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                Yaw = BitConverter.ToSingle(bytes, offset + 16),
                Pitch = BitConverter.ToSingle(bytes, offset + 20),
                Roll = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52)
            };
        }

    }

    [YAXSerializeAs("VertexData")]
    public class FMP_CollisionVertexData
    {
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "VertexIndex")]
        public ushort[] Faces { get; set; }

        public List<FMP_Vertex> Vertices { get; set; } = new List<FMP_Vertex>();

        public static FMP_CollisionVertexData Read(byte[] bytes, int vertexOffset, int vertexCount, int faceIndicesOffset, int faceIndicesCount)
        {
            FMP_CollisionVertexData data = new FMP_CollisionVertexData();

            data.Faces = BitConverter_Ex.ToUInt16Array(bytes, faceIndicesOffset, faceIndicesCount);

            for(int i = 0; i < vertexCount; i++)
            {
                data.Vertices.Add(new FMP_Vertex()
                {
                    Pos = BitConverter_Ex.ToFloat32Array(bytes, vertexOffset + (i * 24), 3),
                    Normal = BitConverter_Ex.ToFloat32Array(bytes, vertexOffset + 12 + (i * 24), 3)
                });
            }

            return data;
        }
    }

    [YAXSerializeAs("Vertex")]
    public class FMP_Vertex
    {
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public float[] Pos { get; set; } //3
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public float[] Normal { get; set; } //3
    }

    [YAXSerializeAs("HvkCollisionData")]
    public class FMP_HvkCollisionData
    {
        [CustomSerialize]
        public int I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [CustomSerialize]
        public byte[] HvkFile { get; set; }

        public static FMP_HvkCollisionData Read(byte[] bytes, int offset)
        {
            int hvkSize = BitConverter.ToInt32(bytes, offset + 4);
            int hvkOffset = BitConverter.ToInt32(bytes, offset + 8);

            return new FMP_HvkCollisionData()
            {
                I_00 = BitConverter.ToInt32(bytes, offset),
                HvkFile = hvkOffset > 0 ? bytes.GetRange(hvkOffset, hvkSize) : null
            };
        }

    }
}
