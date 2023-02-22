using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.CMS;
using Xv2CoreLib.Eternity;
using YAXLib;

namespace Xv2CoreLib.CUS
{

    [YAXSerializeAs("CUS")]
    public class CUS_File : ISorting
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (byte)0)]
        public byte Version { get; set; } = 1;

        public List<Skillset> Skillsets { get; set; }
        public List<Skill> SuperSkills { get; set; }
        public List<Skill> UltimateSkills { get; set; }
        public List<Skill> EvasiveSkills { get; set; }
        public List<Skill> UnkSkills { get; set; }
        public List<Skill> BlastSkills { get; set; }
        public List<Skill> AwokenSkills { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            Skillsets?.Sort((x, y) => x.SortID - y.SortID);
            SuperSkills?.Sort((x, y) => x.SortID - y.SortID);
            UltimateSkills?.Sort((x, y) => x.SortID - y.SortID);
            EvasiveSkills?.Sort((x, y) => x.SortID - y.SortID);
            BlastSkills?.Sort((x, y) => x.SortID - y.SortID);
            AwokenSkills?.Sort((x, y) => x.SortID - y.SortID);
        }

        public static CUS_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetCusFile();
        }

        public int GetIndexOf(int CharacterID, int CostumeID, int ModelPreset)
        {
            for (int i = 0; i < Skillsets.Count(); i++)
            {
                if (int.Parse(Skillsets[i].I_00) == CharacterID && Skillsets[i].I_04 == CostumeID && Skillsets[i].I_26 == (ushort)ModelPreset)
                {
                    return i;
                }
            }

            return -1;
        }

        public string GetNameMsgPath(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Super:
                    return @"msg/proper_noun_skill_spa_name_";
                case SkillType.Ultimate:
                    return @"msg/proper_noun_skill_ult_name_";
                case SkillType.Evasive:
                    return @"msg/proper_noun_skill_esc_name_";
                case SkillType.Awoken:
                    return @"msg/proper_noun_skill_met_name_";
                default:
                    return null;
            }
        }

        public string GetInfoMsgPath(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Super:
                    return @"msg/proper_noun_skill_spa_info_";
                case SkillType.Ultimate:
                    return @"msg/proper_noun_skill_ult_info_";
                case SkillType.Evasive:
                    return @"msg/proper_noun_skill_esc_info_";
                case SkillType.Awoken:
                    return @"msg/proper_noun_skill_met_info_";
                default:
                    return null;
            }
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public bool SkillExists(int id1, SkillType type)
        {
            List<Skill> skills = null;

            switch (type)
            {
                case SkillType.Super:
                    skills = SuperSkills;
                    break;
                case SkillType.Ultimate:
                    skills = UltimateSkills;
                    break;
                case SkillType.Evasive:
                    skills = EvasiveSkills;
                    break;
                case SkillType.Blast:
                    skills = BlastSkills;
                    break;
                case SkillType.Awoken:
                    skills = AwokenSkills;
                    break;
            }

            foreach (var skill in skills)
            {
                if (skill.ID1 == id1) return true;
            }

            return false;
        }

        public int GetSkillIndex(ushort id2, SkillType skillType)
        {
            List<Skill> skills = GetSkills(skillType);

            for(int i = 0; i < skills.Count; i++)
            {
                if (skills[i].ID2 == id2) return i;
            }

            return -1;
        }

        public List<Skill> GetSkills(SkillType type)
        {
            switch (type)
            {
                case SkillType.Super:
                    return SuperSkills;
                case SkillType.Ultimate:
                    return UltimateSkills;
                case SkillType.Evasive:
                    return EvasiveSkills;
                case SkillType.Blast:
                    return BlastSkills;
                case SkillType.Awoken:
                    return AwokenSkills;
                default:
                    throw new Exception($"Invalid argument. SkillType = {type} is invalid.");
            }
        }

        public Skill GetSkill(int id2, SkillType type)
        {
            List<Skill> skills = GetSkills(type);
            return skills?.FirstOrDefault(x => x.ID2 == id2);
        }

        #region Helper

        /// <summary>
        /// Checks of a PUP entry range is used in the CUS.
        /// </summary>
        /// <param name="cusId1Exclusions">Exclude the specified CUS Skills from the check.</param>
        /// <returns></returns>
        public bool IsPupEntryUsed(int pupId, int pupCount, params int[] cusId1Exclusions)
        {
            if (IsPupEntryUsed(SuperSkills, pupId, pupCount, cusId1Exclusions)) return true;
            if (IsPupEntryUsed(UltimateSkills, pupId, pupCount, cusId1Exclusions)) return true;
            if (IsPupEntryUsed(EvasiveSkills, pupId, pupCount, cusId1Exclusions)) return true;
            if (IsPupEntryUsed(BlastSkills, pupId, pupCount, cusId1Exclusions)) return true;
            if (IsPupEntryUsed(AwokenSkills, pupId, pupCount, cusId1Exclusions)) return true;

            return false;
        }

        private bool IsPupEntryUsed(List<Skill> skills, int pupId, int pupCount, params int[] cusId1Exclusions)
        {
            foreach (var skill in skills.Where(x => !cusId1Exclusions.Contains(x.ID1)))
            {
                if (skill.PUP == ushort.MaxValue) continue;
                int min = skill.PUP;
                int max = skill.NumTransformations;

                for (int i = pupId; i <= pupId + pupCount; i++)
                {
                    if (i >= min && i <= max) return true;
                }
            }

            return false;
        }

        public static int ConvertToID1(int ID2, SkillType type)
        {
            switch (type)
            {
                case SkillType.Ultimate:
                    return ID2 + 5000;
                case SkillType.Evasive:
                    return ID2 + 10000;
                case SkillType.Blast:
                    return ID2 + 20000;
                case SkillType.Awoken:
                    return ID2 + 25000;
                case SkillType.Super:
                default:
                    return ID2;
            }

        }

        public static int ConvertToID2(int ID1, SkillType type)
        {
            switch (type)
            {
                case SkillType.Ultimate:
                    return ID1 - 5000;
                case SkillType.Evasive:
                    return ID1 - 10000;
                case SkillType.Blast:
                    return ID1 - 20000;
                case SkillType.Awoken:
                    return ID1 - 25000;
                case SkillType.Super:
                default:
                    return ID1;
            }

        }

        public static SkillType GetSkillTypeFromID1(int ID1)
        {
            if (ID1 > 30000) return SkillType.NotSet;
            if (ID1 > 25000) return SkillType.Awoken;
            if (ID1 > 20000) return SkillType.Blast;
            if (ID1 > 10000) return SkillType.Evasive;
            if (ID1 > 5000) return SkillType.Ultimate;
            if (ID1 > 0) return SkillType.Super;

            return SkillType.NotSet;
        }

        public bool IsSkillIdRangeUsed(CMS_Entry cmsEntry, SkillType skillType, List<int> assignedIds = null)
        {
            int id = cmsEntry.ID * 10;

            List<Skill> skills = GetSkills(skillType);

            for (int i = 0; i < 10; i++)
            {
                if (!skills.Any(x => x.ID2 == id + i) || assignedIds?.Any(x => x == id + i) == true) return false;
            }

            return true;
        }

        /// <summary>
        /// Assigns a new skill ID (ID2), parented to the assigned CMS entry. If no free IDs are available, -1 will be returned.
        /// </summary>
        public int AssignNewSkillId(CMS_Entry cmsEntry, SkillType skillType, List<int> assignedIds = null)
        {
            int id = cmsEntry.ID * 10;
            List<Skill> skills = GetSkills(skillType);

            int min = id;
            int max = id + 10;

            for (int i = 0; i < 10; i++)
            {
                if(assignedIds == null)
                {
                    if (skills.FirstOrDefault(x => x.ID2 == id) == null) return id;
                }
                else
                {
                    if (skills.FirstOrDefault(x => x.ID2 == id) == null && !assignedIds.Contains(id) == true) return id;
                }
                id++;
            }

            return -1;
        }

        public static byte GetCusVersion(int skillSectionOffset, int skillCount, int nextSectionOffset)
        {
            switch ((nextSectionOffset - skillSectionOffset) / skillCount)
            {
                case 68:
                    return 0;
                case 72:
                    return 1;
                default:
                    throw new InvalidDataException("CUS file version not supported.");
            }
        }

        public static string GetSkillDir(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Super:
                    return "skill/SPA";
                case SkillType.Ultimate:
                    return "skill/ULT";
                case SkillType.Evasive:
                    return "skill/ESC";
                case SkillType.Blast:
                    return "skill/BLT";
                case SkillType.Awoken:
                    return "skill/MET";
                default:
                    return null;
            }
        }
        #endregion

        public enum SkillType
        {
            Super = 0,
            Ultimate = 1,
            Evasive = 2,
            Blast = 3,
            Awoken = 5,
            NotSet = -1
        }
    }

    public class Skillset : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return String.Format("{0}_{1}_{2}", I_00, I_04, I_26);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 3)
                {
                    I_00 = split[0];
                    I_04 = int.Parse(split[1]);
                    I_26 = ushort.Parse(split[2]);
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Character ID")]
        public string I_00 { get; set; } //int32
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume Index")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Model Preset")]
        public ushort I_26 { get; set; }
        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("ID1")]
        public ushort I_08 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("ID1")]
        public ushort I_10 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("ID1")]
        public ushort I_12 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("ID1")]
        public ushort I_14 { get; set; } //uint16
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("ID1")]
        public ushort I_16 { get; set; } //uint16
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("ID1")]
        public ushort I_18 { get; set; } //uint16
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("ID1")]
        public ushort I_20 { get; set; } //uint16
        [YAXAttributeFor("BlastType")]
        [YAXSerializeAs("ID1")]
        public ushort I_22 { get; set; } //uint16
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("ID1")]
        public ushort I_24 { get; set; } //uint16
    }

    public class Skill : IInstallable
    {
        [Flags]
        public enum FilesLoadedFlags : ushort
        {
            Unk0 = 0x1,
            CamEan = 0x2,
            Eepk = 0x4,
            BsaAndShotBdm = 0x8,
            CharaSE = 0x10,
            CharaVOX = 0x20,
            Unk6 = 0x40,
            Unk7 = 0x80,
            ShotEepk = 0x100,
            Bas = 0x200,
            Bdm = 0x400,
            CharaSpecificEan = 0x800,
            CharaSpecificCamEan = 0x1000,
            AfterBac = 0x2000,
            AfterBcm = 0x4000,
            Unk15 = 0x8000
        }

        [Flags]
        public enum Type : byte
        {
            Flag0 = 1,
            Flag1 = 0x2,
            Flag2 = 0x4,
            Flag3 = 0x8,
            BAC = 0x10,
            BCM = 0x20,
            EAN = 0x40,
            Flag7 = 0x80
        }


        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return ID1; } }
        [YAXDontSerialize]
        public string Index
        {
            get { return ID1.ToString(); }
            set { ID1 = ushort.Parse(value); }
        }

        [YAXDontSerialize]
        public Type FilesLoadedFlags2
        {
            get
            {
                return (Type)I_13;
            }
            set
            {
                if ((byte)value != I_13)
                {
                    I_13 = (byte)value;
                }
            }
        }


        [YAXDontSerialize]
        public bool HasEanPath { get { return !(EanPath == "NULL" || string.IsNullOrWhiteSpace(EanPath)); } }
        [YAXDontSerialize]
        public bool HasCamEanPath { get { return !(CamEanPath == "NULL" || string.IsNullOrWhiteSpace(CamEanPath)); } }
        [YAXDontSerialize]
        public bool HasEepkPath { get { return !(EepkPath == "NULL" || string.IsNullOrWhiteSpace(EepkPath)); } }
        [YAXDontSerialize]
        public bool HasSeAcbPath { get { return !(SePath == "NULL" || string.IsNullOrWhiteSpace(SePath)); } }
        [YAXDontSerialize]
        public bool HasVoxAcbPath { get { return !(VoxPath == "NULL" || string.IsNullOrWhiteSpace(VoxPath)); } }
        [YAXDontSerialize]
        public bool HasAfterBacPath { get { return !(AfterBacPath == "NULL" || string.IsNullOrWhiteSpace(AfterBacPath)); } }
        [YAXDontSerialize]
        public bool HasAfterBcmPath { get { return !(AfterBcmPath == "NULL" || string.IsNullOrWhiteSpace(AfterBcmPath)); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ShortName")]
        public string ShortName { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID1")]
        public ushort ID1 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public ushort ID2 { get; set; } //uint16
        [YAXAttributeFor("Race_Lock")]
        [YAXSerializeAs("value")]
        public CusRaceLock I_12 { get; set; }
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte I_13 { get; set; }
        [YAXAttributeFor("FilesLoaded")]
        [YAXSerializeAs("Flags")]
        public FilesLoadedFlags FilesLoadedFlags1 { get; set; } //uint16
        [YAXAttributeFor("PartSet")]
        [YAXSerializeAs("value")]
        public short I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("Path")]
        public string EanPath { get; set; }
        [YAXAttributeFor("CAM_EAN")]
        [YAXSerializeAs("Path")]
        public string CamEanPath { get; set; }
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Path")]
        public string EepkPath { get; set; }
        [YAXAttributeFor("ACB_SE")]
        [YAXSerializeAs("Path")]
        public string SePath { get; set; }
        [YAXAttributeFor("ACB_VOX")]
        [YAXSerializeAs("Path")]
        public string VoxPath { get; set; }
        [YAXAttributeFor("AFTER_BAC")]
        [YAXSerializeAs("Path")]
        public string AfterBacPath { get; set; }
        [YAXAttributeFor("AFTER_BCM")]
        [YAXSerializeAs("Path")]
        public string AfterBcmPath { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
        [YAXAttributeFor("PUP")]
        [YAXSerializeAs("ID")]
        public ushort PUP { get; set; } //ushort
        [YAXAttributeFor("CUS_Aura")]
        [YAXSerializeAs("value")]
        public short CusAura { get; set; }
        [YAXAttributeFor("TransformCharaSwap")]
        [YAXSerializeAs("Chara_ID")]
        public ushort CharaSwapId { get; set; } //ushort
        [YAXAttributeFor("Skillset_Change")]
        [YAXSerializeAs("ModelPreset")]
        public short I_62 { get; set; }
        [YAXAttributeFor("Num_Of_Transforms")]
        [YAXSerializeAs("value")]
        public ushort NumTransformations { get; set; }
        [YAXAttributeFor("I_66")]
        [YAXSerializeAs("value")]
        public ushort I_66 { get; set; }

        [YAXHexValue]
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (uint)0xffffff00)]
        public uint I_68 { get; set; } = 0xffffff00;


        #region Installer
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; }

        //Prebaked Auras:
        //We keep track of these values as they were upon load so that the prebaked auras can be properly updated when saving.
        //The original sequence can be reused if the CusAuras haven't changed, but if they have, then it needs to be uninstalled.
        public short OriginalCusAura = -1;
        public ushort OriginalNumTrans = 1;

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CusAuraData")]
        public List<CusAuraData> CusAuras { get; set; } = new List<CusAuraData>();
        #endregion
    }

    [Flags]
    public enum CusRaceLock : byte
    {
        HUM = 1,
        SYM = 0x2,
        NMC = 0x4,
        FRI = 0x8,
        MAM = 0x10,
        HUF = 0x20,
        SYF = 0x40,
        MAF = 0x80
    }
}
