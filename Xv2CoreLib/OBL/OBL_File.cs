using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.OBL
{
    public enum Type : uint
    {
        None = 0,
        Main = 1,
        AnimeMusicPack1 = 2,
        AnimeMusicPack2 = 3,
        BothMusicPacks = 5
    }

    public enum DLC_Flag : uint
    {
        None = uint.MaxValue,
        AnimeMusicPack1 = 0x13,
        AnimeMusicPack2 = 0x14,
    }

    public enum SelectionType : uint
    {
        Normal = 0,
        AnimeMusicPackRandom = 1
    }

    [YAXSerializeAs("OBL")]
    public class OBL_File
    {
        public List<OBL_Cue> LobbyCues { get; set; }
        public List<OBL_Cue> BattleCues { get; set; }
        public List<OBL_Cue> QuestCues { get; set; }
        public List<OBL_Cue> HeroColosseumCues { get; set; }

        #region LoadSave
        public static OBL_File Parse(string path, bool writeXml)
        {
            OBL_File oblFile = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OBL_File));
                serializer.SerializeToFile(oblFile, path + ".xml");
            }

            return oblFile;
        }

        public static OBL_File Parse(byte[] bytes)
        {
            OBL_File oblFile = new OBL_File();

            int lobbyCount = BitConverter.ToInt32(bytes, 8);
            int battleCount = BitConverter.ToInt32(bytes, 12);
            int questCount = BitConverter.ToInt32(bytes, 16);
            int hcCount = BitConverter.ToInt32(bytes, 20);

            //Validate the file size
            if (32 + (20 * lobbyCount) + (20 * battleCount) + (20 * questCount) + (20 * hcCount) != bytes.Length)
                throw new InvalidDataException("OBL parse failed. File size validation failed!");

            //Calculate offsets
            int lobbyOffset = 32;
            int battleOffset = lobbyOffset + (20 * lobbyCount);
            int questOffset = battleOffset + (20 * battleCount);
            int hcOffset = questOffset + (20 * questCount);

            //Read data
            oblFile.LobbyCues = OBL_Cue.ReadAll(bytes, lobbyOffset, lobbyCount);
            oblFile.BattleCues = OBL_Cue.ReadAll(bytes, battleOffset, battleCount);
            oblFile.QuestCues = OBL_Cue.ReadAll(bytes, questOffset, questCount);
            oblFile.HeroColosseumCues = OBL_Cue.ReadAll(bytes, hcOffset, hcCount);

            return oblFile;
        }
        
        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .obl file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(OBL_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (OBL_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the OBL_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)1279414051)); //signature
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //endianness
            bytes.AddRange(BitConverter.GetBytes((ushort)32)); //header size
            bytes.AddRange(BitConverter.GetBytes((uint)LobbyCues.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)BattleCues.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)QuestCues.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)HeroColosseumCues.Count));
            bytes.AddRange(new byte[8]); //Padding?

            //Entries
            bytes.AddRange(OBL_Cue.WriteAll(LobbyCues));
            bytes.AddRange(OBL_Cue.WriteAll(BattleCues));
            bytes.AddRange(OBL_Cue.WriteAll(QuestCues));
            bytes.AddRange(OBL_Cue.WriteAll(HeroColosseumCues));
            
            //Validate the file size
            if (32 + (20 * LobbyCues.Count) + (20 * BattleCues.Count) + (20 * QuestCues.Count) + (20 * HeroColosseumCues.Count) != bytes.Count)
                throw new InvalidDataException("OBL rebuild failed. File size validation failed!");

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        #endregion

        /// <summary>
        /// Remove an entry from all 4 sections with Cue Id and MsgNameId equal to the specified value.
        /// </summary>
        public void RemoveEntry(int cueId)
        {
            LobbyCues.RemoveAll(x => x.Index == $"{cueId}_{cueId}");
            BattleCues.RemoveAll(x => x.Index == $"{cueId}_{cueId}");
            QuestCues.RemoveAll(x => x.Index == $"{cueId}_{cueId}");
            HeroColosseumCues.RemoveAll(x => x.Index == $"{cueId}_{cueId}");
        }

        /// <summary>
        /// Add an entry to all sections with Cue Id and MsgNameId set to the specified value, and default values for the remaining parameters.
        /// </summary>
        public void AddEntry(int cueId)
        {
            LobbyCues.Add(new OBL_Cue() { CueID = cueId, MsgNameID = cueId, DlcFlag = DLC_Flag.None, SelectionType = SelectionType.Normal, Type = Type.Main });
            BattleCues.Add(new OBL_Cue() { CueID = cueId, MsgNameID = cueId, DlcFlag = DLC_Flag.None, SelectionType = SelectionType.Normal, Type = Type.Main });
            QuestCues.Add(new OBL_Cue() { CueID = cueId, MsgNameID = cueId, DlcFlag = DLC_Flag.None, SelectionType = SelectionType.Normal, Type = Type.Main });
            HeroColosseumCues.Add(new OBL_Cue() { CueID = cueId, MsgNameID = cueId, DlcFlag = DLC_Flag.None, SelectionType = SelectionType.Normal, Type = Type.Main });
        }

        public bool IsCueAndMessageIdUsed(int id)
        {
            if (LobbyCues.Any(x => x.Index == $"{id}_{id}")) return true;
            if (BattleCues.Any(x => x.Index == $"{id}_{id}")) return true;
            if (QuestCues.Any(x => x.Index == $"{id}_{id}")) return true;
            if (HeroColosseumCues.Any(x => x.Index == $"{id}_{id}")) return true;
            return false;
        }
    }

    [YAXSerializeAs("Cue")]
    public class OBL_Cue : IInstallable
    {
        #region WrapperProps
        [YAXDontSerialize]
        public Type Type { get { return I_00; } set { I_00 = value; } }
        [YAXDontSerialize]
        public int CueID { get { return I_04; } set { I_04 = value; } }
        [YAXDontSerialize]
        public int MsgNameID { get { return I_08; } set { I_08 = value; } }
        [YAXDontSerialize]
        public DLC_Flag DlcFlag { get { return I_12; } set { I_12 = value; } }
        [YAXDontSerialize]
        public SelectionType SelectionType { get { return I_16; } set { I_16 = value; } }
        #endregion

        #region IInstallable
        [YAXDontSerialize]
        public int SortID { get { return 0; } } //No sorting needed, but IInstallable requires this
        [YAXDontSerialize]
        public string Index { get { return $"{I_04}_{I_08}"; } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public Type I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("CueID")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("MsgNameID")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("DLC")]
        public DLC_Flag I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SelectionType")]
        public SelectionType I_16 { get; set; }

        public static byte[] WriteAll(List<OBL_Cue> cues)
        {
            List<byte> bytes = new List<byte>();

            foreach (var cue in cues)
                bytes.AddRange(cue.Write());

            return bytes.ToArray();
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes((uint)I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes((uint)I_12));
            bytes.AddRange(BitConverter.GetBytes((uint)I_16));

            return bytes.ToArray();
        }

        public static List<OBL_Cue> ReadAll(byte[] bytes, int offset, int count)
        {
            List<OBL_Cue> cues = new List<OBL_Cue>();

            for(int i = 0; i < count; i++)
                cues.Add(Read(bytes, offset + (20 * i)));

            return cues;
        }

        public static OBL_Cue Read(byte[] bytes, int offset)
        {
            return new OBL_Cue()
            {
                I_00 = (Type)BitConverter.ToUInt32(bytes, offset + 0),
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                I_08 = BitConverter.ToInt32(bytes, offset + 8),
                I_12 = (DLC_Flag)BitConverter.ToUInt32(bytes, offset + 12),
                I_16 = (SelectionType)BitConverter.ToUInt32(bytes, offset + 16),
            };
        }

    }
}