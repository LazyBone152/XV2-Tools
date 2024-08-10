using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.IDB
{

    [Flags]
    public enum IdbRaceLock
    {
        HUM = 0x1,
        HUF = 0x2,
        SYM = 0x4,
        SYF = 0x8,
        NMC = 0x10,
        FRI = 0x20,
        MAM = 0x40,
        MAF = 0x80
    }

    public enum LB_Color : ushort
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Purple = 3
    }

    public enum IDB_Type
    {
        Super = 0,
        Ultimate = 1,
        Evasvie = 2,
        Awoken = 5
    }

    [YAXSerializeAs("IDB")]
    public class IDB_File : ISorting
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 1)]
        public int Version { get; set; } = 2;
        //0 = original IDB version (used from 1.00 to 1.17)
        //1 = updated IDB version first used since 1.18
        //2 = updated IDB version first used since 1.22

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "IDB_Entry")]
        public List<IDB_Entry> Entries { get; set; }

        public byte[] SaveToBytes(bool isSkillIdb = false)
        {
            return new Deserializer(this, isSkillIdb).bytes.ToArray();
        }

        public static IDB_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetIdbFile();
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }

        public void SortEntries()
        {
            var sortedEntries = Entries.OrderBy(x => (int)x.Type).ThenBy(x => x.ID).ToList();

            //Copy over sorted entries to preserve original list instance
            for(int i = 0; i < Entries.Count; i++)
            {
                Entries[i] = sortedEntries[i];
            }

            /*
            //Split entries by I_08 (Type), Sort them and then rejoin
            int split = ((int)Entries.Max(x => x.Type)) + 1;
            List<List<IDB_Entry>> splitEntries = new List<List<IDB_Entry>>();

            for(int i = 0; i < split; i++)
            {
                splitEntries.Add(Entries.FindAll(x => x.Type == (IDB_Type)i));
                
                if(splitEntries[i] != null)
                    splitEntries[i].Sort((x, y) => x.SortID - y.SortID);
            }

            Entries.Clear();

            for (int i = 0; i < split; i++)
            {
                if(splitEntries[i] != null)
                    Entries.AddRange(splitEntries[i]);
            }
            */
        }

        public bool DoesSkillExist(int id, IDB_Type skillType)
        {
            if (Entries == null) return false;
            int type = (int)skillType;

            foreach (var entry in Entries)
            {
                if (entry.ID == (ushort)id && entry.Type == type) return true;
            }

            return false;
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public static bool IsIdbMsgFile(string path)
        {
            switch (Path.GetFileName(path))
            {
                case "proper_noun_costume_name_":
                case "proper_noun_accessory_name_":
                case "proper_noun_talisman_name_":
                case "proper_noun_material_name_":
                case "proper_noun_battle_name_":
                case "proper_noun_extra_name_":
                case "proper_noun_gallery_illust_name_":
                case "proper_noun_float_pet_name_":
                case "proper_noun_costume_info_":
                case "proper_noun_accessory_info_":
                case "proper_noun_talisman_info_":
                case "proper_noun_material_info_":
                case "proper_noun_battle_info_":
                case "proper_noun_extra_info_":
                case "proper_noun_gallery_illust_info_":
                case "proper_noun_float_pet_info_":
                    return true;
                default:
                    return false;
            }
        }

        public static string NameMsgFile(string idbName)
        {
            switch (idbName)
            {
                case "costume_bottom_item.idb":
                case "costume_top_item.idb":
                case "costume_shoes_item.idb":
                case "costume_gloves_item.idb":
                    return "proper_noun_costume_name_";
                case "accessory_item.idb":
                    return "proper_noun_accessory_name_";
                case "talisman_item.idb":
                    return "proper_noun_talisman_name_";
                case "material_item.idb":
                    return "proper_noun_material_name_";
                case "battle_item.idb":
                    return "proper_noun_battle_name_";
                case "extra_item.idb":
                    return "proper_noun_extra_name_";
                case "gallery_item.idb":
                    return "proper_noun_gallery_illust_name_";
                case "pet_item.idb":
                    return "proper_noun_float_pet_name_";
                default:
                    throw new Exception(String.Format("IDB \"{0}\" has no name msg file.", idbName));
            }

        }

        public static string InfoMsgFile(string idbName)
        {
            switch (idbName)
            {
                case "costume_bottom_item.idb":
                case "costume_top_item.idb":
                case "costume_shoes_item.idb":
                case "costume_gloves_item.idb":
                    return "proper_noun_costume_info_";
                case "accessory_item.idb":
                    return "proper_noun_accessory_info_";
                case "talisman_item.idb":
                    return "proper_noun_talisman_info_";
                case "material_item.idb":
                    return "proper_noun_material_info_";
                case "battle_item.idb":
                    return "proper_noun_battle_info_";
                case "extra_item.idb":
                    return "proper_noun_extra_info_";
                case "gallery_item.idb":
                    return "proper_noun_gallery_illust_info_";
                case "pet_item.idb":
                    return "proper_noun_float_pet_info_";
                default:
                    throw new Exception(String.Format("IDB \"{0}\" has no info msg file.", idbName));
            }
        }
        
        public static string LimitBurstMsgFile(string idbName)
        {
            switch (idbName)
            {
                case "talisman_item.idb":
                    return "proper_noun_talisman_info_olt_";
                default:
                    throw new Exception(String.Format("IDB \"{0}\" has no Limit Burst msg file.", idbName));
            }
        }

        public static string LimitBurstHudMsgFile(string idbName)
        {
            switch (idbName)
            {
                case "talisman_item.idb":
                    return "quest_btlhud_";
                default:
                    throw new Exception(String.Format("IDB \"{0}\" has no Limit Burst HUD msg file.", idbName));
            }
        }

        public static string SkillNameMsgFile(CUS.CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS.CUS_File.SkillType.Super:
                    return "proper_noun_skill_spa_name_";
                case CUS.CUS_File.SkillType.Ultimate:
                    return "proper_noun_skill_ult_name_";
                case CUS.CUS_File.SkillType.Evasive:
                    return "proper_noun_skill_esc_name_";
                case CUS.CUS_File.SkillType.Awoken:
                    return "proper_noun_skill_met_name_";
            }

            return null;
        }

        public static string SkillInfoMsgFile(CUS.CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS.CUS_File.SkillType.Super:
                    return "proper_noun_skill_spa_info_";
                case CUS.CUS_File.SkillType.Ultimate:
                    return "proper_noun_skill_ult_info_";
                case CUS.CUS_File.SkillType.Evasive:
                    return "proper_noun_skill_esc_info_";
                case CUS.CUS_File.SkillType.Awoken:
                    return "proper_noun_skill_met_info_";
            }

            return null;
        }
    
    }

    [YAXSerializeAs("IDB_Entry")]
    public class IDB_Entry : IInstallable
    {
        public const int OLD_ENTRY_SIZE = 48;
        public const int ENTRY_SIZE_V1 = 52; //New in 1.18
        public const int ENTRY_SIZE_V2 = 64; //New in 1.22

        public const string TALISMAN_NAME_MSG = "msg/proper_noun_talisman_info_";
        public const string TALISMAN_DESCRIPTION_MSG = "msg/proper_noun_talisman_name_";
        public const string TALISMAN_LIMIT_BURST_DESCRIPTION_MSG = "msg/proper_noun_talisman_info_olt_";
        public const string TALISMAN_LIMIT_BURST_DESCRIPTION_HUD_MSG = "msg/quest_btlhud_";

        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return ID; } }
        [YAXDontSerialize]
        public string Index { get { return $"{ID_Binding}_{Type}"; } set { ID_Binding = value.Split('_')[0]; Type = ushort.Parse(value.Split('_')[1]); } }
        [YAXDontSerialize]
        public ushort ID { get { return ushort.Parse(ID_Binding); } set { ID_Binding = value.ToString(); } }

        #endregion


        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string ID_Binding { get; set; } //ushort
        [YAXAttributeFor("Stars")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("MSG_ID")]
        public ushort NameMsgID { get; set; }
        [YAXAttributeFor("Description")]
        [YAXSerializeAs("MSG_ID")]
        public ushort DescMsgID { get; set; }
        [YAXAttributeFor("HowMsgID")]
        [YAXSerializeAs("MSG_ID")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public ushort HowMsgID { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("NEW_I_10")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public ushort NEW_I_10 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ushort Type { get; set; } //ushort
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("BuyPrice")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("SellPrice")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("RaceLock")]
        [YAXSerializeAs("value")]
        public IdbRaceLock RaceLock { get; set; } //int
        [YAXAttributeFor("TPMedals")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("STPMedals")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int NEW_I_32 { get; set; }
        [YAXAttributeFor("NEW_I_36")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int NEW_I_36 { get; set; }
        [YAXAttributeFor("Model")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; } //int32
        [YAXAttributeFor("LimitBurst.EEPK_Effect")]
        [YAXSerializeAs("ID")]
        public ushort I_36 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("LimitBurst.Color")]
        [YAXSerializeAs("value")]
        public LB_Color I_38 { get; set; } = (LB_Color)ushort.MaxValue;
        [YAXAttributeFor("LimitBurst.Description")]
        [YAXSerializeAs("MSG_ID")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID1")]
        public ushort I_42 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID2")]
        public ushort I_44 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID3")]
        public ushort I_46 { get; set; } = ushort.MaxValue;

        //New in 1.18
        [YAXAttributeFor("NEW_I_12")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXHexValue]
        public ushort NEW_I_12 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("NEW_I_14")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXHexValue]
        public ushort NEW_I_14 { get; set; } = ushort.MaxValue;



        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        public List<IBD_Effect> Effects { get; set; } // size 3

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; } //Only for LB Mod Installer

        public static IdbRaceLock GetIdbRaceLock(CUS.CusRaceLock cusRaceLock)
        {
            IdbRaceLock raceLock = 0;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.HUF))
                raceLock = raceLock | IdbRaceLock.HUF;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.HUM))
                raceLock = raceLock | IdbRaceLock.HUM;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.SYF))
                raceLock = raceLock | IdbRaceLock.SYF;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.SYM))
                raceLock = raceLock | IdbRaceLock.SYM;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.FRI))
                raceLock = raceLock | IdbRaceLock.FRI;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.NMC))
                raceLock = raceLock | IdbRaceLock.NMC;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.MAM))
                raceLock = raceLock | IdbRaceLock.MAM;

            if (cusRaceLock.HasFlag(CUS.CusRaceLock.MAF))
                raceLock = raceLock | IdbRaceLock.MAF;

            return raceLock;
        }

        public string GetRaceLockAsString()
        {
            if ((ushort)RaceLock == 255) return null;
            if ((ushort)RaceLock == 0) return null;

            bool first = true;
            List<string> str = new List<string>();
            IdbRaceLock raceLock = (IdbRaceLock)RaceLock;

            if (raceLock.HasFlag(IdbRaceLock.HUM))
            {
                str.Add("HUM");
            }
            if (raceLock.HasFlag(IdbRaceLock.HUF))
            {
                str.Add("HUF");
            }
            if (raceLock.HasFlag(IdbRaceLock.SYM))
            {
                str.Add("SYM");
            }
            if (raceLock.HasFlag(IdbRaceLock.SYF))
            {
                str.Add("SYF");
            }
            if (raceLock.HasFlag(IdbRaceLock.NMC))
            {
                str.Add("NMC");
            }
            if (raceLock.HasFlag(IdbRaceLock.FRI))
            {
                str.Add("FRI");
            }
            if (raceLock.HasFlag(IdbRaceLock.MAM))
            {
                str.Add("MAM");
            }
            if (raceLock.HasFlag(IdbRaceLock.MAF))
            {
                str.Add("MAF");
            }

            StringBuilder str2 = new StringBuilder();
            str2.Append("(");
            foreach(string s in str)
            {
                if (first)
                {
                    str2.Append(String.Format("{0}", s));
                    first = false;
                }
                else
                {
                    str2.Append(String.Format(", {0}", s));
                }
            }
            str2.Append(")");

            return str2.ToString();
        }

        public static IDB_Entry GetDefaultSkillEntry(int skillId = 0, int type = 0, int kiCost = 0, int buyCost = 1000)
        {
            IDB_Entry idbEntry = new IDB_Entry();
            idbEntry.ID = (ushort)skillId;
            idbEntry.Type = (ushort)type;
            idbEntry.NameMsgID = idbEntry.ID;
            idbEntry.DescMsgID = idbEntry.ID;
            idbEntry.I_12 = 32767;
            idbEntry.I_16 = buyCost;

            idbEntry.Effects = new List<IBD_Effect>();
            idbEntry.Effects.Add(new IBD_Effect());
            idbEntry.Effects.Add(new IBD_Effect());
            idbEntry.Effects.Add(new IBD_Effect());

            idbEntry.Effects[0].F_100 = kiCost;

            return idbEntry;
        }

        public bool CanRaceUseItem(SAV.Race race)
        {
            switch (race)
            {
                case SAV.Race.HUM:
                    return RaceLock.HasFlag(IdbRaceLock.HUM);
                case SAV.Race.HUF:
                    return RaceLock.HasFlag(IdbRaceLock.HUF);
                case SAV.Race.SYM:
                    return RaceLock.HasFlag(IdbRaceLock.SYM);
                case SAV.Race.SYF:
                    return RaceLock.HasFlag(IdbRaceLock.SYF);
                case SAV.Race.NMC:
                    return RaceLock.HasFlag(IdbRaceLock.NMC);
                case SAV.Race.MAM:
                    return RaceLock.HasFlag(IdbRaceLock.MAM);
                case SAV.Race.MAF:
                    return RaceLock.HasFlag(IdbRaceLock.MAF);
                default:
                    return RaceLock.HasFlag(IdbRaceLock.FRI);
            }
        }
    }

    [YAXSerializeAs("Effect")]
    public class IBD_Effect
    {
        public const int OLD_ENTRY_SIZE = 224;
        public const int ENTRY_SIZE_V1 = 232;
        public const int ENTRY_SIZE_V2 = 236;

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; } = -1;
        [YAXAttributeFor("ActivationType")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; } = -1;
        [YAXAttributeFor("NumActTimes")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; } = -1;
        [YAXAttributeFor("NEW_I_12")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int NEW_I_12 { get; set; } = 0;
        [YAXAttributeFor("Timer")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; } = -1;
        [YAXAttributeFor("AbilityValues")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0##########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_16 { get; set; } //size 6
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("ActivationChance")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("Multipliers")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0##########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_48 { get; set; } //size 6
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_72 { get; set; } //size 6
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("KiRecovery")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("StaminaRecovery")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("EnemyStaminaEraser")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("F_120")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("GroundSpeed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_124 { get; set; }
        [YAXAttributeFor("AirSpeed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_128 { get; set; }
        [YAXAttributeFor("BoostSpeed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_132 { get; set; }
        [YAXAttributeFor("DashSpeed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_136 { get; set; }
        [YAXAttributeFor("BasicAttack")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_140 { get; set; }
        [YAXAttributeFor("BasicKiBlast")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_144 { get; set; }
        [YAXAttributeFor("StrikeSuper")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_148 { get; set; }
        [YAXAttributeFor("KiSuper")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_152 { get; set; }
        [YAXAttributeFor("F_156")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0##########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_156 { get; set; } //size 17

        //New in 1.18
        [YAXAttributeFor("NEW_I_48")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXHexValue]
        public int NEW_I_48 { get; set; }
        [YAXAttributeFor("NEW_I_52")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXHexValue]
        public int NEW_I_52 { get; set; }

        public IBD_Effect()
        {
            F_16 = new float[6];
            F_48 = new float[6];
            I_72 = new int[6];
            F_156 = new float[17];
        }
    }


}
