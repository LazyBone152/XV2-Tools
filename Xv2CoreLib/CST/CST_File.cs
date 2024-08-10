using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.CST
{
    [Flags]
    public enum CstDlcVer : uint
    {
        DLC_Def = 0x1,
        DLC_Gkb = 0x2,
        DLC_1 = 0x4, //Super Pack 1
        DLC_2 = 0x8, //Super Pack 2
        DLC_3 = 0x10, //Super Pack 3
        DLC_4 = 0x20, //Super Pack 4
        DLC_5 = 0x40, //Extra Pack 1
        DLC_6 = 0x80, //Extra Pack 2
        DLC_7 = 0x100, //Extra Pack 3
        DLC_8 = 0x200, //Extra Pack 4
        DLC_9 = 0x400, //Ultra Pack 1
        DLC_10 = 0x800, //Ultra Pack 2
        Ver_Day1 = 0x1000,
        Ver_TU4 = 0x10000,
        UD7 = 0x80000,
        PRB = 0x10000000,
        EL0 = 0x20000000,
        DLC12 = 0x40000000, //Legendary Pack 1
        DLC13 = 0x80000000, //Legendary Pack 2
    }

    [Flags]
    public enum CstDlcVer2 : uint
    {
        DLC14 = 0x1, //Conton City Vote Pack
        DLC15 = 0x10 //Hero of Justice Pack 1
    }

    [YAXSerializeAs("CST")]
    public class CST_File
    {
        private const uint CST_SIGNATURE = 0x54534323;
        private const uint CST_HEADER_SIZE = 0x10;
        public const string CST_PATH = "system/chara_select_table.cst";
        public const string CST_PRB_PATH = "system/chara_select_table_prb.cst";

        public const int CST_ENTRY_SIZE_1 = 0x28; //Ver 1 (original)
        public const int CST_ENTRY_SIZE_2 = 0x30; //Ver 2
        public const int CST_ENTRY_SIZE_3 = 0x34; //Ver 3

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1)]
        public int Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharaSlot")]
        public List<CST_CharaSlot> CharaSlots { get; set; } = new List<CST_CharaSlot>();

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(CST_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(CST_File), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (CST_File)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion

        #region LoadSave
        public static CST_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static CST_File Load(byte[] bytes)
        {
            //Validate the CST file.
            if (BitConverter.ToUInt32(bytes, 0) != CST_SIGNATURE)
                throw new InvalidDataException("CST_File.Load: Signature not found!");

            if (BitConverter.ToUInt32(bytes, 4) != CST_HEADER_SIZE)
                throw new InvalidDataException("CST_File.Load: Header size incorrect!");


            uint numSlots = BitConverter.ToUInt32(bytes, 8);
            uint numCostumes = BitConverter.ToUInt32(bytes, 12);
            int entrySize = (int)((bytes.Length - CST_HEADER_SIZE) / numCostumes);

            CST_File cstFile = new CST_File();
            cstFile.Version = EntrySizeToVersion(entrySize);

            //Parse the file
            for (int i = 0; i < numCostumes; i++)
            {
                var slot = CST_CharaCostumeSlot.Read(bytes, (int)(CST_HEADER_SIZE + (entrySize * i)), cstFile.Version);

                if (slot.CostumeSlotID == 0)
                {
                    //New slot
                    cstFile.CharaSlots.Add(new CST_CharaSlot(slot));
                }
                else
                {
                    //New costume for last slot

                    if (cstFile.CharaSlots.Count - 1 >= 0)
                    {
                        cstFile.CharaSlots[cstFile.CharaSlots.Count - 1].CharaCostumeSlots.Add(slot);
                    }
                    else
                    {
                        throw new InvalidDataException("CST_File.Load: Slot data was in an unexpected order.");
                    }
                }
            }

            return cstFile;
        }

        public byte[] SaveToBytes()
        {
            List<byte> bytes = new List<byte>();

            //Remove empty slots
            CharaSlots.RemoveAll(x => x.IsEmpty());

            int numCostumes = GetCostumeNum();

            //Header
            bytes.AddRange(BitConverter.GetBytes(CST_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(CST_HEADER_SIZE));
            bytes.AddRange(BitConverter.GetBytes(CharaSlots.Count));
            bytes.AddRange(BitConverter.GetBytes(numCostumes));

            //Write entries
            foreach (var slot in CharaSlots)
            {
                slot.OrderCostumeSlotIDs();

                foreach (var costume in slot.CharaCostumeSlots)
                {
                    bytes.AddRange(costume.Write(Version));
                }
            }

            //Validate size
            if (bytes.Count != CST_HEADER_SIZE + (VersionToEntrySize(Version) * numCostumes))
                throw new InvalidDataException("CST_File.SaveToBytes: Invalid file size!");

            return bytes.ToArray();
        }

        public void SaveFile(string path)
        {
            byte[] bytes = SaveToBytes();
            File.WriteAllBytes(path, bytes);
        }

        private int GetCostumeNum()
        {
            int count = 0;

            foreach (var slot in CharaSlots)
            {
                count += slot.CharaCostumeSlots.Count;
            }

            return count;
        }

        public static int VersionToEntrySize(int version)
        {
            switch (version)
            {
                case 1:
                    return CST_ENTRY_SIZE_1;
                case 2:
                    return CST_ENTRY_SIZE_2;
                case 3:
                    return CST_ENTRY_SIZE_3;
                default:
                    throw new InvalidDataException($"CST: This CST version is not supported (Version: {version}).");
            }
        }

        public static int EntrySizeToVersion(int entrySize)
        {
            switch (entrySize)
            {
                case CST_ENTRY_SIZE_1:
                    return 1;
                case CST_ENTRY_SIZE_2:
                    return 2;
                case CST_ENTRY_SIZE_3:
                    return 3;
                default:
                    throw new InvalidDataException($"CST: This CST version is not supported (EntrySize: {entrySize}).");
            }
        }
        #endregion

        #region Install

        public CST_CharaSlot GetCharaSlotFromInstallID(string installID)
        {
            foreach (var charaSlot in CharaSlots)
            {
                if (charaSlot.CharaCostumeSlots.FirstOrDefault(x => x.InstallID == installID) != null) return charaSlot;
            }

            return null;
        }

        public void InstallEntries(List<CST_CharaSlot> installSlots, List<string> installIDs)
        {
            if (installSlots == null) return;

            foreach (var installSlot in installSlots)
            {
                CST_CharaSlot charaSlot = GetCharaSlotFromInstallID(installSlot.InstallID);

                //Sorting
                int sortBefore = GetIndexOfSlot(installSlot.SortBefore);
                int sortAfter = GetIndexAfter(installSlot.SortAfter);

                //Sorting priority. Before -> After
                int sortIndex = sortBefore != -1 ? sortBefore : sortAfter;

                if (charaSlot == null)
                {
                    charaSlot = new CST_CharaSlot();

                    if (sortIndex != -1)
                        CharaSlots.Insert(sortIndex, charaSlot);
                    else
                        CharaSlots.Add(charaSlot);
                }

                foreach (var installCostume in installSlot.CharaCostumeSlots)
                {
                    int slotIdx = charaSlot.IndexOfSlot(installCostume.InstallID);

                    if (slotIdx == -1)
                    {
                        //Costume slot doesn't exist
                        installIDs.Add(installCostume.InstallID);
                        charaSlot.CharaCostumeSlots.Add(installCostume);
                    }
                    else
                    {
                        //Costume slot already exists
                        installIDs.Add(installCostume.InstallID);
                        charaSlot.CharaCostumeSlots[slotIdx] = installCostume;
                    }
                }

            }
        }

        public void UninstallEntries(List<string> installIDs, CST_File cpkCstFile)
        {
            foreach (var charaSlot in CharaSlots)
            {
                for (int i = charaSlot.CharaCostumeSlots.Count - 1; i >= 0; i--)
                {
                    CST_CharaCostumeSlot existing = cpkCstFile != null ? cpkCstFile.GetEntry(charaSlot.CharaCostumeSlots[i].InstallID) : null;

                    if (installIDs.Contains(charaSlot.CharaCostumeSlots[i].InstallID))
                    {
                        if (existing != null)
                        {
                            //Restore default entry from the CST
                            charaSlot.CharaCostumeSlots[i] = existing;
                        }
                        else
                        {
                            charaSlot.CharaCostumeSlots.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }

            RemoveEmptySlots();
        }

        private void RemoveEmptySlots()
        {
            for (int i = CharaSlots.Count - 1; i >= 0; i--)
            {
                if (CharaSlots[i].CharaCostumeSlots == null)
                {
                    CharaSlots.RemoveAt(i);
                    continue;
                }
                if (CharaSlots[i].CharaCostumeSlots.Count == 0)
                {
                    CharaSlots.RemoveAt(i);
                    continue;
                }
            }
        }

        private int GetIndexOfSlot(string installID)
        {
            CST_CharaSlot slot = GetCharaSlotFromInstallID(installID);

            if (slot != null)
                return CharaSlots.IndexOf(slot);

            return -1;
        }

        private int GetIndexAfter(string installID)
        {
            CST_CharaSlot slot = GetCharaSlotFromInstallID(installID);

            if (slot != null)
                return CharaSlots.IndexOf(slot) + 1;

            return -1;
        }


        #endregion

        public Eternity.CharaSlotsFile ConvertToPatcherSlotsFile()
        {
            Eternity.CharaSlotsFile patcherSlots = new Eternity.CharaSlotsFile();

            foreach (var slot in CharaSlots)
            {
                patcherSlots.CharaSlots.Add(new Eternity.CharaSlot(slot));
            }

            return patcherSlots;
        }

        public CST_CharaCostumeSlot GetEntry(string installID)
        {
            foreach (var slot in CharaSlots)
            {
                foreach (var costume in slot.CharaCostumeSlots)
                {
                    if (costume.InstallID == installID) return costume;
                }
            }

            return null;
        }
    }

    [YAXSerializeAs("CharaSlot")]
    public class CST_CharaSlot
    {
        [YAXDontSerializeIfNull]
        [YAXAttributeForClass]
        public string InstallID { get; set; }
        [YAXDontSerializeIfNull]
        [YAXAttributeForClass]
        public string SortBefore { get; set; }
        [YAXDontSerializeIfNull]
        [YAXAttributeForClass]
        public string SortAfter { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharaCostumeSlot")]
        public List<CST_CharaCostumeSlot> CharaCostumeSlots { get; set; } = new List<CST_CharaCostumeSlot>();


        public CST_CharaSlot() { }

        public CST_CharaSlot(CST_CharaCostumeSlot slot)
        {
            //InstallID = slot.InstallID;
            CharaCostumeSlots.Add(slot);
        }

        public CST_CharaSlot(Eternity.CharaSlot charaSlot)
        {
            InstallID = charaSlot.InstallID;
            SortBefore = charaSlot.SortBefore;
            SortAfter = charaSlot.SortAfter;

            foreach (var costumeSlot in charaSlot.CostumeSlots)
            {
                CharaCostumeSlots.Add(new CST_CharaCostumeSlot(costumeSlot));
            }
        }

        public void OrderCostumeSlotIDs()
        {
            for (int i = 0; i < CharaCostumeSlots.Count; i++)
                CharaCostumeSlots[i].CostumeSlotID = i;
        }

        public bool IsEmpty()
        {
            if (CharaCostumeSlots == null) return true;
            if (CharaCostumeSlots.Count == 0) return true;
            return false;
        }

        public int IndexOfSlot(string installID)
        {
            return CharaCostumeSlots.IndexOf(CharaCostumeSlots.FirstOrDefault(x => x.InstallID == installID));
        }

    }

    [YAXSerializeAs("CharaCostumeSlot")]
    public class CST_CharaCostumeSlot
    {
        [YAXDontSerialize]
        public string InstallID { get { return $"{CharaCode}_{Costume}_{Preset}"; } }

        //Copied from eternity source

        [YAXDontSerialize]
        public int CostumeSlotID { get; set; } // 0 
        [YAXAttributeForClass]
        public string CharaCode { get; set; } // 4
        [YAXAttributeForClass]
        public ushort Costume { get; set; } // 8
        [YAXAttributeForClass]
        public ushort Preset { get; set; } // A
        [YAXAttributeFor("UnlockIndex")]
        [YAXSerializeAs("value")]
        public ushort UnlockIndex { get; set; } // C
        [YAXAttributeFor("flag_gk2")]
        [YAXSerializeAs("value")]
        public ushort flag_gk2 { get; set; } // E
        [YAXAttributeFor("CssVoice1")]
        [YAXSerializeAs("value")]
        public ushort CssVoice1 { get; set; } //0x10
        [YAXAttributeFor("CssVoice2")]
        [YAXSerializeAs("value")]
        public ushort CssVoice2 { get; set; } //0x12
        [YAXAttributeFor("DlcFlag1")]
        [YAXSerializeAs("value")]
        public CstDlcVer DlcFlag1 { get; set; } // 0x14
        [YAXAttributeFor("DlcFlag2")]
        [YAXSerializeAs("value")]
        public CstDlcVer2 DlcFlag2 { get; set; } // 0x18 - Added in game v 1.16
        [YAXAttributeFor("IsCustomCostume")]
        [YAXSerializeAs("value")]
        public int IsCustomCostume { get; set; } // 0x1C
        [YAXAttributeFor("CacIndex")]
        [YAXSerializeAs("value")]
        public int CacIndex { get; set; } // 0x20 - Added in game v 1.10
        [YAXAttributeFor("var_type_after_TU9_order")]
        [YAXSerializeAs("value")]
        public int var_type_after_TU9_order { get; set; } // 0x24 - Added in game 1.14. Whatever this shit is, is -1 in all chars, except TU0 ant TU1 where it is 0 and 1 respectively.

        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int I_44 { get; set; }
        [YAXAttributeFor("flag_cgk2")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public ushort flag_cgk2 { get; set; } // 0x30 - Added in game v 1.22. Adds the "Ultra Supervillain" to the name in CSS
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public ushort I_50 { get; set; }

        public CST_CharaCostumeSlot() { }

        public CST_CharaCostumeSlot(Eternity.CharaCostumeSlot slot)
        {
            CharaCode = slot.CharaCode;
            Costume = (ushort)slot.Costume;
            Preset = (ushort)slot.Preset;
            UnlockIndex = (ushort)slot.UnlockIndex;
            flag_gk2 = (ushort)(slot.flag_gk2 == true ? 1 : 0);
            CssVoice1 = slot.CssVoice1 < 0 ? ushort.MaxValue : (ushort)slot.CssVoice1;
            CssVoice2 = slot.CssVoice2 < 0 ? ushort.MaxValue : (ushort)slot.CssVoice2;
            DlcFlag1 = slot.DLC_Flag1;
            DlcFlag2 = slot.DLC_Flag2;
            flag_cgk2 = (ushort)(slot.flag_cgk2 == true ? 1 : 0);
        }

        public static CST_CharaCostumeSlot Read(byte[] bytes, int offset, int version)
        {
            CST_CharaCostumeSlot entry = new CST_CharaCostumeSlot()
            {
                CostumeSlotID = BitConverter.ToInt32(bytes, offset + 0x0),
                CharaCode = StringEx.GetString(bytes, offset + 0x4, false, StringEx.EncodingType.ASCII, 4),
                Costume = BitConverter.ToUInt16(bytes, offset + 0x8),
                Preset = BitConverter.ToUInt16(bytes, offset + 0xA),
                UnlockIndex = BitConverter.ToUInt16(bytes, offset + 0xC),
                flag_gk2 = BitConverter.ToUInt16(bytes, offset + 0xE),
                CssVoice1 = BitConverter.ToUInt16(bytes, offset + 0x10),
                CssVoice2 = BitConverter.ToUInt16(bytes, offset + 0x12),
                DlcFlag1 = (CstDlcVer)BitConverter.ToUInt32(bytes, offset + 0x14),
                DlcFlag2 = (CstDlcVer2)BitConverter.ToUInt32(bytes, offset + 0x18),
                IsCustomCostume = BitConverter.ToInt32(bytes, offset + 0x1c),
                CacIndex = BitConverter.ToInt32(bytes, offset + 0x20),
                var_type_after_TU9_order = BitConverter.ToInt32(bytes, offset + 0x24)
            };

            if(version >= 2)
            {
                entry.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                entry.I_44 = BitConverter.ToInt32(bytes, offset + 44);
            }

            if(version >= 3)
            {
                entry.flag_cgk2 = BitConverter.ToUInt16(bytes, offset + 0x30);
                entry.I_50 = BitConverter.ToUInt16(bytes, offset + 0x32);
            }

            return entry;
        }

        public byte[] Write(int version)
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(CostumeSlotID));
            bytes.AddRange(Utils.GetStringBytes(CharaCode, 4, 4));
            bytes.AddRange(BitConverter.GetBytes(Costume));
            bytes.AddRange(BitConverter.GetBytes(Preset));
            bytes.AddRange(BitConverter.GetBytes(UnlockIndex));
            bytes.AddRange(BitConverter.GetBytes(flag_gk2));
            bytes.AddRange(BitConverter.GetBytes(CssVoice1));
            bytes.AddRange(BitConverter.GetBytes(CssVoice2));
            bytes.AddRange(BitConverter.GetBytes((uint)DlcFlag1));
            bytes.AddRange(BitConverter.GetBytes((uint)DlcFlag2));
            bytes.AddRange(BitConverter.GetBytes(IsCustomCostume));
            bytes.AddRange(BitConverter.GetBytes(CacIndex));
            bytes.AddRange(BitConverter.GetBytes(var_type_after_TU9_order));

            if(version >= 2)
            {
                bytes.AddRange(BitConverter.GetBytes(I_40));
                bytes.AddRange(BitConverter.GetBytes(I_44));
            }

            if(version >= 3)
            {
                bytes.AddRange(BitConverter.GetBytes(flag_cgk2));
                bytes.AddRange(BitConverter.GetBytes(I_50));
            }

            if (bytes.Count != CST_File.VersionToEntrySize(version))
                throw new InvalidDataException($"CST_Entry.Write: Invalid entry size!");

            return bytes.ToArray();
        }

        public Eternity.CharaCostumeSlot ConvertToEternityFile()
        {
            return new Eternity.CharaCostumeSlot()
            {
                UnlockIndex = UnlockIndex,
                CharaCode = CharaCode,
                Preset = Preset,
                flag_gk2 = (flag_gk2 == 0) ? false : true,
                CssVoice1 = CssVoice1,
                CssVoice2 = CssVoice2,
                DLC_Flag1 = DlcFlag1,
                Costume = Costume,
                flag_cgk2 = (flag_cgk2 == 0) ? false : true
            };
        }
    };

}
