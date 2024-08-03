using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xv2CoreLib.CST;
using YAXLib;

namespace Xv2CoreLib.Eternity
{

    public class CharaSlotsFile
    {
        public const string FILE_NAME_BIN = "XV2P_SLOTS.x2s";
        public const string FILE_NAME_XML = "XV2P_SLOTS.x2s.xml";

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharaSlot")]
        [BindingSubList]
        public List<CharaSlot> CharaSlots { get; set; } = new List<CharaSlot>();

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(CharaSlotsFile));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(CharaSlotsFile), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (CharaSlotsFile)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion

        #region LoadSave
        public static CharaSlotsFile Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static CharaSlotsFile Load(byte[] bytes)
        {
            string rawText = Encoding.ASCII.GetString(bytes);

            CharaSlotsFile charaFile = new CharaSlotsFile();

            rawText = rawText.Replace("{", "");
            rawText = rawText.Replace("[", "");
            rawText = rawText.Replace(" ", "");

            string[] charaSlotsText = rawText.Split('}');


            foreach(var slot in charaSlotsText)
            {
                if (string.IsNullOrWhiteSpace(slot)) continue;

                string[] costumeSlotsText = slot.Split(']');
                CharaSlot charaSlot = new CharaSlot();

                foreach(var costume in costumeSlotsText)
                {
                    if (string.IsNullOrWhiteSpace(costume)) continue;

                    CharaCostumeSlot costumeSlot = new CharaCostumeSlot();
                    
                    string[] parameters = costume.Split(',');
                    if (parameters.Length != 10) throw new InvalidDataException($"Invalid number of CharaSlot parameters. Expected 10, found {parameters.Length}.");

                    costumeSlot.CharaCode = parameters[0];
                    costumeSlot.Costume = int.Parse(parameters[1]);
                    costumeSlot.Preset = int.Parse(parameters[2]);
                    costumeSlot.UnlockIndex = int.Parse(parameters[3]);
                    costumeSlot.flag_gk2 = (parameters[4] == "1") ? true : false;
                    costumeSlot.CssVoice1 = int.Parse(parameters[5]);
                    costumeSlot.CssVoice2 = int.Parse(parameters[6]);
                    costumeSlot.DLC_Flag1 = (CstDlcVer)uint.Parse(parameters[7]);
                    costumeSlot.DLC_Flag2 = (CstDlcVer2)uint.Parse(parameters[8]);
                    costumeSlot.flag_cgk2 = (parameters[9] == "1") ? true : false;

                    charaSlot.CostumeSlots.Add(costumeSlot);
                }

                charaFile.CharaSlots.Add(charaSlot);
            }

            return charaFile;
        }

        public byte[] SaveToBytes()
        {
            StringBuilder strBuilder = new StringBuilder();

            foreach(var chara in CharaSlots)
            {
                strBuilder.Append("{");

                foreach(var costume in chara.CostumeSlots)
                {
                    strBuilder.Append("[");

                    strBuilder.Append(costume.CharaCode).Append(",");
                    strBuilder.Append(costume.Costume).Append(",");
                    strBuilder.Append(costume.Preset).Append(",");
                    strBuilder.Append(costume.UnlockIndex).Append(",");
                    strBuilder.Append((costume.flag_gk2) ? 1 : 0).Append(",");
                    strBuilder.Append(costume.CssVoice1).Append(",");
                    strBuilder.Append(costume.CssVoice2).Append(",");
                    strBuilder.Append((uint)costume.DLC_Flag1).Append(",");
                    strBuilder.Append((uint)costume.DLC_Flag2).Append(",");
                    strBuilder.Append((costume.flag_cgk2) ? 1 : 0);

                    strBuilder.Append("]");
                }

                strBuilder.Append("}");
            }

            return Encoding.ASCII.GetBytes(strBuilder.ToString());
        }

        public void SaveFile(string path)
        {
            byte[] bytes = SaveToBytes();
            File.WriteAllBytes(path, bytes);
        }

        #endregion

        public bool SlotExists(string charCode, int costume)
        {
            foreach(var slot in CharaSlots)
            {
                if (slot.CostumeSlots.FirstOrDefault(x => x.CharaCode == charCode && x.Costume == costume) != null) return true;
            }

            return false;
        }
        
        public CST_File ConvertToCst()
        {
            CST_File cstFile = new CST_File();

            foreach(var charaSlot in CharaSlots)
            {
                cstFile.CharaSlots.Add(new CST_CharaSlot(charaSlot));
            }

            return cstFile;
        }
    }

    public class CharaSlot
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
        [BindingSubList]
        public List<CharaCostumeSlot> CostumeSlots { get; set; } = new List<CharaCostumeSlot>();

        public CharaSlot() { }

        public CharaSlot(CST_CharaSlot charaSlots)
        {
            InstallID = charaSlots.InstallID;
            SortBefore = charaSlots.SortBefore;
            SortAfter = charaSlots.SortAfter;

            foreach(var slot in charaSlots.CharaCostumeSlots)
            {
                CostumeSlots.Add(new CharaCostumeSlot(slot));
            }
        }

        public int IndexOfSlot(string installID)
        {
            return CostumeSlots.IndexOf(CostumeSlots.FirstOrDefault(x => x.InstallID == installID));
        }
    }

    public class CharaCostumeSlot
    {
        [YAXDontSerialize]
        public string InstallID { get { return $"{CharaCode}_{Costume}_{Preset}"; } }

        //Serialized values
        [YAXAttributeForClass]
        public string CharaCode { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public int Costume { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Preset")]
        public int Preset { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int UnlockIndex { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = false)]
        public bool flag_gk2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("CssVoice1")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int CssVoice1 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("CssVoice2")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = -1)]
        public int CssVoice2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("DLC")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (CstDlcVer)0)]
        public CstDlcVer DLC_Flag1 { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (CstDlcVer2)0)]
        public CstDlcVer2 DLC_Flag2 { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = false)]
        public bool flag_cgk2 { get; set; }

        public CharaCostumeSlot() { }

        public CharaCostumeSlot(CST_CharaCostumeSlot slot)
        {
            CharaCode = slot.CharaCode;
            Costume = slot.Costume;
            Preset = slot.Preset;
            UnlockIndex = slot.UnlockIndex;
            flag_gk2 = slot.flag_gk2 > 0;
            CssVoice1 = slot.CssVoice1 == ushort.MaxValue ? -1 : slot.CssVoice1;
            CssVoice2 = slot.CssVoice2 == ushort.MaxValue ? -1 : slot.CssVoice2;
            DLC_Flag1 = slot.DlcFlag1;
            DLC_Flag2 = slot.DlcFlag2;
            flag_cgk2 = slot.flag_cgk2 > 0;
        }
    }
}
