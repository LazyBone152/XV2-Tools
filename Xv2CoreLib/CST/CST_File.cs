using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.CST
{
    [Flags]
    public enum CstDlcVer : uint
    {
        DLC_Def = 1,
        DLC_Gkb = 2,
        DLC_1 = 4, //Super Pack 1
        DLC_2 = 8, //Super Pack 2
        DLC_3 = 16, //Super Pack 3
        DLC_4 = 32, //Super Pack 4
        DLC_5 = 64, //Extra Pack 1
        DLC_6 = 128, //Extra Pack 2
        DLC_7 = 256, //Extra Pack 3
        DLC_8 = 512, //Extra Pack 4
        DLC_9 = 1024, //Ultra Pack 1
        DLC_10 = 2048, //Ultra Pack 2
        Ver_Day1 = 4096,
        Ver_TU4 = 65536,
        UD7 = 524288,
        PRB = 0x10000000,
        EL0 = 0x20000000,
        DLC12 = 0x40000000, //Legendary Pack 1
        DLC13 = 0x80000000, //Legendary Pack 2
    }

    [Flags]
    public enum CstDlcVer2 : uint
    {
        DLC14 = 1,
    }

    [YAXSerializeAs("CST")]
    public class CST_File
    {
        private const uint CST_SIGNATURE = 0x54534323;
        private const uint CST_HEADER_SIZE = 0x10;
        public const string CST_PATH = "system/chara_select_table.cst";
        public const string CST_PRB_PATH = "system/chara_select_table_prb.cst";

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

            if (bytes.Length != CST_HEADER_SIZE + (CST_CharaCostumeSlot.CST_ENTRY_SIZE * numCostumes))
                throw new InvalidDataException("CST_File.Load: Invalid file size!");

            //Parse the file
            CST_File cstFile = new CST_File();

            for(int i = 0; i < numCostumes; i++)
            {
                var slot = CST_CharaCostumeSlot.Read(bytes, (int)(CST_HEADER_SIZE + (CST_CharaCostumeSlot.CST_ENTRY_SIZE * i)));

                if(slot.CostumeSlotID == 0)
                {
                    //New slot
                    cstFile.CharaSlots.Add(new CST_CharaSlot(slot));
                }
                else
                {
                    //New costume for last slot

                    if(cstFile.CharaSlots.Count - 1 >= 0)
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
            foreach(var slot in CharaSlots)
            {
                slot.OrderCostumeSlotIDs();

                foreach(var costume in slot.CharaCostumeSlots)
                {
                    bytes.AddRange(costume.Write());
                }
            }

            //Validate size
            if (bytes.Count != CST_HEADER_SIZE + (CST_CharaCostumeSlot.CST_ENTRY_SIZE * numCostumes))
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

            foreach(var slot in CharaSlots)
            {
                count += slot.CharaCostumeSlots.Count;
            }

            return count;
        }
        #endregion

        public Eternity.CharaCostumeSlot GetEntry(string installID)
        {
            foreach(var slot in CharaSlots)
            {
                foreach(var costume in slot.CharaCostumeSlots)
                {
                    if (costume.InstallID == installID) return costume.ConvertToEternityFile();
                }
            }

            return null;
        }
    }

    [YAXSerializeAs("CharaSlot")]
    public class CST_CharaSlot
    {
        public CST_CharaSlot() { }

        public CST_CharaSlot(CST_CharaCostumeSlot slot)
        {
            CharaCostumeSlots.Add(slot);
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharaCostumeSlot")]
        public List<CST_CharaCostumeSlot> CharaCostumeSlots { get; set; } = new List<CST_CharaCostumeSlot>();

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
    }

    [YAXSerializeAs("CharaCostumeSlot")]
    public class CST_CharaCostumeSlot
    {
        public const uint CST_ENTRY_SIZE = 0x28;

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

        public static CST_CharaCostumeSlot Read(byte[] bytes, int offset)
        {
            return new CST_CharaCostumeSlot()
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
        }
    
        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(CostumeSlotID));
            bytes.AddRange(Utils.GetStringBytes(CharaCode, 4));
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

            if (bytes.Count != CST_ENTRY_SIZE)
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
                DLC = DlcFlag1,
                Costume = Costume
            };
        }
    };

}
