using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CUS
{

    [YAXSerializeAs("CUS")]
    public class CUS_File : ISorting
    {

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
            Skillsets.Sort((x, y) => x.SortID - y.SortID);
            SuperSkills.Sort((x, y) => x.SortID - y.SortID);
            UltimateSkills.Sort((x, y) => x.SortID - y.SortID);
            EvasiveSkills.Sort((x, y) => x.SortID - y.SortID);
            BlastSkills.Sort((x, y) => x.SortID - y.SortID);
            AwokenSkills.Sort((x, y) => x.SortID - y.SortID);
        }

        public static CUS_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetCusFile();
        }

        public int GetIndexOf(int CharacterID, int CostumeID, int ModelPreset)
        {
            for(int i = 0; i < Skillsets.Count(); i++)
            {
                if(int.Parse(Skillsets[i].I_00) == CharacterID && Skillsets[i].I_04 == CostumeID && Skillsets[i].I_26 == (ushort)ModelPreset)
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

        /// <summary>
        /// (Deprecrated?) Converts a Skill ShortName into it's actual ID, if it is installed in the system. If it is not installed, then -1 will be returned. If "SkillID" is already an ID, then it will simply be returned.
        /// ShortNames should be formatted like this "#GOK" (minus the quotes)
        /// </summary>
        public string GetRealSkillId(string SkillID, SkillType _SkillType)
        {
            //Obsolete function
            if(SkillID[0] == '#')
            {
                SkillID = SkillID.Remove(0, 1);
            }
            else
            {
                return SkillID;
            }

            List<Skill> SkillList = null;
            switch (_SkillType)
            {
                case SkillType.Super:
                    SkillList = SuperSkills;
                    break;
                case SkillType.Ultimate:
                    SkillList = UltimateSkills;
                    break;
                case SkillType.Evasive:
                    SkillList = EvasiveSkills;
                    break;
                case SkillType.Blast:
                    SkillList = BlastSkills;
                    break;
                case SkillType.Awoken:
                    SkillList = AwokenSkills;
                    break;
            }

            foreach(var e in SkillList)
            {
                if(e.Str_00 == SkillID)
                {
                    return e.Index.ToString();
                }
            }


            return (UInt16.MaxValue).ToString();
        }

        public enum SkillType
        {
            Super = 0,
            Ultimate = 1,
            Evasive = 2,
            Blast = 3,
            Awoken = 5,
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }
        
        public bool SkillExists(string id1, SkillType type)
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

            foreach(var skill in skills)
            {
                if (skill.Index == id1) return true;
            }

            return false;
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
            foreach(var skill in skills.Where(x => !cusId1Exclusions.Contains(x.ID1)))
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
                    return ID2 = 25000;
                case SkillType.Super:
                default:
                    return ID2;
            }

        }
        #endregion
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
                return String.Format("{0}_{1}_{2}",I_00, I_04, I_26);
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
        public string I_08 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("ID1")]
        public string I_10 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("ID1")]
        public string I_12 { get; set; } //uint16
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("ID1")]
        public string I_14 { get; set; } //uint16
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("ID1")]
        public string I_16 { get; set; } //uint16
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("ID1")]
        public string I_18 { get; set; } //uint16
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("ID1")]
        public string I_20 { get; set; } //uint16
        [YAXAttributeFor("BlastType")]
        [YAXSerializeAs("ID1")]
        public string I_22 { get; set; } //uint16
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("ID1")]
        public string I_24 { get; set; } //uint16
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
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public string ShortName { get { return Str_00; } set { Str_00 = value; } }
        [YAXDontSerialize]
        public Type FilesLoadedFlags2
        {
            get
            {
                return (Type)I_13;
            }
            set
            {
                if((byte)value != I_13)
                {
                    I_13 = (byte)value;
                }
            }
        }
        [YAXDontSerialize]
        public int ID1 { get { return ushort.Parse(Index); } set { Index = value.ToString(); } }
        [YAXDontSerialize]
        public ushort ID2 { get { return ushort.Parse(I_10); } set { I_10 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort PUP { get { return ushort.Parse(I_56); } set { I_56 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort CharaSwapId { get { return ushort.Parse(I_60); } set { I_60 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort NumTransformations { get { return I_64; } set { I_64 = value; } }

        //Paths
        [YAXDontSerialize]
        public string EanPath { get { return Str_20; } set { Str_20 = value; } }
        [YAXDontSerialize]
        public string CamEanPath { get { return Str_24; } set { Str_24 = value; } }
        [YAXDontSerialize]
        public string EepkPath { get { return Str_28; } set { Str_28 = value; } }
        [YAXDontSerialize]
        public string SeAcbPath { get { return Str_32; } set { Str_32 = value; } }
        [YAXDontSerialize]
        public string VoxAcbPath { get { return Str_36; } set { Str_36 = value; } }
        [YAXDontSerialize]
        public string AfterBacPath { get { return Str_40; } set { Str_40 = value; } }
        [YAXDontSerialize]
        public string AfterBcmPath { get { return Str_44; } set { Str_44 = value; } }
        [YAXDontSerialize]
        public bool HasEanPath { get { return !(Str_20 == "NULL" || string.IsNullOrWhiteSpace(Str_20)); } }
        [YAXDontSerialize]
        public bool HasCamEanPath { get { return !(Str_24 == "NULL" || string.IsNullOrWhiteSpace(Str_24)); } }
        [YAXDontSerialize]
        public bool HasEepkPath { get { return !(Str_28 == "NULL" || string.IsNullOrWhiteSpace(Str_28)); } }
        [YAXDontSerialize]
        public bool HasSeAcbPath { get { return !(Str_32 == "NULL" || string.IsNullOrWhiteSpace(Str_32)); } }
        [YAXDontSerialize]
        public bool HasVoxAcbPath { get { return !(Str_36 == "NULL" || string.IsNullOrWhiteSpace(Str_36)); } }
        [YAXDontSerialize]
        public bool HasAfterBacPath { get { return !(Str_40 == "NULL" || string.IsNullOrWhiteSpace(Str_40)); } }
        [YAXDontSerialize]
        public bool HasAfterBcmPath { get { return !(Str_44 == "NULL" || string.IsNullOrWhiteSpace(Str_44)); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ShortName")]
        public string Str_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID1")]
        public string Index { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public string I_10 { get; set; } //uint16
        [YAXAttributeFor("Race_Lock")]
        [YAXSerializeAs("value")]
        public byte I_12 { get; set; }
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte I_13 { get; set; }
        [YAXAttributeFor("FilesLoaded")]
        [YAXSerializeAs("Flags")]
        public FilesLoadedFlags I_14 { get; set; } //uint16
        [YAXAttributeFor("PartSet")]
        [YAXSerializeAs("value")]
        public short I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_20 { get; set; }
        [YAXAttributeFor("CAM_EAN")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_24 { get; set; }
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_28 { get; set; }
        [YAXAttributeFor("ACB_SE")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_32 { get; set; }
        [YAXAttributeFor("ACB_VOX")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_36 { get; set; }
        [YAXAttributeFor("AFTER_BAC")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_40 { get; set; }
        [YAXAttributeFor("AFTER_BCM")]
        [YAXSerializeAs("Path")]
        [BindingString]
        public string Str_44 { get; set; }
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
        public string I_56 { get; set; } //ushort
        [YAXAttributeFor("CUS_Aura")]
        [YAXSerializeAs("value")]
        public short I_58 { get; set; }
        [YAXAttributeFor("TransformCharaSwap")]
        [YAXSerializeAs("Chara_ID")]
        public string I_60 { get; set; } //ushort
        [YAXAttributeFor("Skillset_Change")]
        [YAXSerializeAs("ModelPreset")]
        public short I_62 { get; set; }
        [YAXAttributeFor("Num_Of_Transforms")]
        [YAXSerializeAs("value")]
        public ushort I_64 { get; set; }
        [YAXAttributeFor("I_66")]
        [YAXSerializeAs("value")]
        public ushort I_66 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; }
    }

}
