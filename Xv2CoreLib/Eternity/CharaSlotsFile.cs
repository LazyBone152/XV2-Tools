using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.Eternity
{
    [Flags]
    public enum CstDlcVer
    {
        DLC_Def = 1,
        DLC_Gkb = 2,
        DLC_1 = 4,
        DLC_2 = 8,
        DLC_3 = 16,
        DLC_4 = 32,
        DLC_5 = 64,
        DLC_6 = 128,
        DLC_7 = 256,
        DLC_8 = 512,
        DLC_9 = 1024,
        DLC_10 = 2048,
        Ver_Day1 = 4096,
        Ver_TU4 = 65536,
        UD7 = 524288,
        PRB = 0x10000000,
        EL0 = 0x20000000
    }

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
                    if (parameters.Length != 8) throw new InvalidDataException($"Invalid number of CharaSlot parameters. Expected 8, found {parameters.Length}.");

                    costumeSlot.CharaCode = parameters[0];
                    costumeSlot.Costume = int.Parse(parameters[1]);
                    costumeSlot.Preset = int.Parse(parameters[2]);
                    costumeSlot.UnlockIndex = int.Parse(parameters[3]);
                    costumeSlot.flag_gk2 = (parameters[4] == "1") ? true : false;
                    costumeSlot.CssVoice1 = int.Parse(parameters[5]);
                    costumeSlot.CssVoice2 = int.Parse(parameters[6]);
                    costumeSlot.DLC = (CstDlcVer)int.Parse(parameters[7]);

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
                    strBuilder.Append(costume.CostumeBinding).Append(",");
                    strBuilder.Append(costume.PresetBinding).Append(",");
                    strBuilder.Append(costume.UnlockIndex).Append(",");
                    strBuilder.Append((costume.flag_gk2) ? 1 : 0).Append(",");
                    strBuilder.Append(costume.CssVoice1).Append(",");
                    strBuilder.Append(costume.CssVoice2).Append(",");
                    strBuilder.Append((int)costume.DLC);

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

        #region Install
        //Installing is limited to new slots. Modifications to existing slots are not allowed.
        //If attempting to install into a slot that already exists, an error will be raised.

        private bool SlotExists(string installID)
        {
            foreach (var charaSlot in CharaSlots)
            {
                if (charaSlot.CostumeSlots.FirstOrDefault(x => x.InstallID == installID) != null) return true;
            }

            return false;
        }

        public CharaSlot GetCharaSlotFromInstallID(string installID)
        {
            foreach(var charaSlot in CharaSlots)
            {
                if (charaSlot.CostumeSlots.FirstOrDefault(x => x.InstallID == installID) != null) return charaSlot;
            }

            return null;
        }

        /// <returns> Bool indicating if the installation successfull. If it is false, then there was an ID conflict. </returns>
        public bool InstallEntries(List<CharaSlot> installSlots, List<string> installIDs)
        {
            if (installSlots == null) return true;

            foreach(var installSlot in installSlots)
            {
                CharaSlot charaSlot = GetCharaSlotFromInstallID(installSlot.InstallID);

                if (charaSlot == null)
                {
                    charaSlot = new CharaSlot();
                    CharaSlots.Add(charaSlot);
                }

                foreach(var installCostume in installSlot.CostumeSlots)
                {
                    if (!SlotExists(installCostume.InstallID))
                    {
                        installIDs.Add(installCostume.InstallID);
                        charaSlot.CostumeSlots.Add(installCostume);
                    }
                    else
                    {
                        return false;
                    }
                }

            }

            return true;
        }

        public void UninstallEntries(List<string> installIDs)
        {
            foreach(var charaSlot in CharaSlots)
            {
                for (int i = charaSlot.CostumeSlots.Count - 1; i >= 0; i--)
                {
                    if (installIDs.Contains(charaSlot.CostumeSlots[i].InstallID))
                    {
                        charaSlot.CostumeSlots.RemoveAt(i);
                        continue;
                    }
                }
            }

            RemoveEmptySlots();
        }

        private void RemoveEmptySlots()
        {
            for (int i = CharaSlots.Count - 1; i >= 0; i--)
            {
                if(CharaSlots[i].CostumeSlots == null)
                {
                    CharaSlots.RemoveAt(i);
                    continue;
                }
                if (CharaSlots[i].CostumeSlots.Count == 0)
                {
                    CharaSlots.RemoveAt(i);
                    continue;
                }
            }
        }
        #endregion
    }

    public class CharaSlot
    {
        [YAXDontSerializeIfNull]
        [YAXAttributeForClass]
        public string InstallID { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharaCostumeSlot")]
        [BindingSubList]
        public List<CharaCostumeSlot> CostumeSlots { get; set; } = new List<CharaCostumeSlot>();
    }

    public class CharaCostumeSlot
    {
        [YAXDontSerialize]
        public string InstallID { get { return $"{CharaCode}_{CostumeBinding}_{PresetBinding}"; } }
        [YAXDontSerialize]
        public int Costume
        {
            get
            {
                return int.Parse(CostumeBinding);
            }
            set
            {
                CostumeBinding = value.ToString();
            }
        }
        [YAXDontSerialize]
        public int Preset
        {
            get
            {
                return int.Parse(PresetBinding);
            }
            set
            {
                PresetBinding = value.ToString();
            }
        }


        //Serialized values
        [YAXAttributeForClass]
        public string CharaCode { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public string CostumeBinding { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Preset")]
        public string PresetBinding { get; set; }
        [YAXAttributeForClass]
        public int UnlockIndex { get; set; }
        [YAXAttributeForClass]
        public bool flag_gk2 { get; set; }
        [YAXAttributeForClass]
        public int CssVoice1 { get; set; }
        [YAXAttributeForClass]
        public int CssVoice2 { get; set; }
        [YAXAttributeForClass]
        public CstDlcVer DLC { get; set; }
    }
}
