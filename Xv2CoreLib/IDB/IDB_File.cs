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
    public enum RaceLock
    {
        HUM = 1,
        HUF = 2,
        SYM = 4,
        SYF = 8,
        NMC = 16,
        FRI = 32,
        MAM = 64,
        MAF = 128
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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "IDB_Entry")]
        public List<IDB_Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
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

            Entries = (List<IDB_Entry>)Entries.OrderBy(x => (int)x.Type).ThenByDescending(x => x.ID);

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
                if (entry.ID == (ushort)id && entry.Type == (IDB_Type)type) return true;
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

    }

    [YAXSerializeAs("IDB_Entry")]
    public class IDB_Entry : IInstallable
    {
        public const string TALISMAN_NAME_MSG = "msg/proper_noun_talisman_info_";
        public const string TALISMAN_DESCRIPTION_MSG = "msg/proper_noun_talisman_name_";
        public const string TALISMAN_LIMIT_BURST_DESCRIPTION_MSG = "msg/proper_noun_talisman_info_olt_";
        public const string TALISMAN_LIMIT_BURST_DESCRIPTION_HUD_MSG = "msg/quest_btlhud_";

        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return ID; } }
        [YAXDontSerialize]
        public string Index { get { return $"{ID_Binding}_{Type}"; } set { ID_Binding = value.Split('_')[0]; Type = (IDB_Type)ushort.Parse(value.Split('_')[1]); } }
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
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public IDB_Type Type { get; set; } //ushort
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("BuyPrice")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("SellPrice")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("RaceLock")]
        [YAXSerializeAs("value")]
        public RaceLock RaceLock { get; set; } //int
        [YAXAttributeFor("TPMedals")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("Model")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; } //int32
        [YAXAttributeFor("LimitBurst.EEPK_Effect")]
        [YAXSerializeAs("ID")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("LimitBurst.Color")]
        [YAXSerializeAs("value")]
        public LB_Color I_38 { get; set; }
        [YAXAttributeFor("LimitBurst.Description")]
        [YAXSerializeAs("MSG_ID")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID1")]
        public ushort I_42 { get; set; } //ushort
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID2")]
        public ushort I_44 { get; set; } //ushort
        [YAXAttributeFor("LimitBurst.Talisman")]
        [YAXSerializeAs("ID3")]
        public ushort I_46 { get; set; } //ushort

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        public List<IBD_Effect> Effects { get; set; } // size 3

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; } //Only for LB Mod Installer

        public string GetRaceLockAsString()
        {
            if ((ushort)RaceLock == 255) return null;
            if ((ushort)RaceLock == 0) return null;

            bool first = true;
            List<string> str = new List<string>();
            RaceLock raceLock = (RaceLock)RaceLock;

            if (raceLock.HasFlag(RaceLock.HUM))
            {
                str.Add("HUM");
            }
            if (raceLock.HasFlag(RaceLock.HUF))
            {
                str.Add("HUF");
            }
            if (raceLock.HasFlag(RaceLock.SYM))
            {
                str.Add("SYM");
            }
            if (raceLock.HasFlag(RaceLock.SYF))
            {
                str.Add("SYF");
            }
            if (raceLock.HasFlag(RaceLock.NMC))
            {
                str.Add("NMC");
            }
            if (raceLock.HasFlag(RaceLock.FRI))
            {
                str.Add("FRI");
            }
            if (raceLock.HasFlag(RaceLock.MAM))
            {
                str.Add("MAM");
            }
            if (raceLock.HasFlag(RaceLock.MAF))
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
    }

    [YAXSerializeAs("Effect")]
    public class IBD_Effect
    {
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("ActivationType")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("NumActTimes")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Timer")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
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
        public float[] F_156 { get; set; } //size 13

    }


}
