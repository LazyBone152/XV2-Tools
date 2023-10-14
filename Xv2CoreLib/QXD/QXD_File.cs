using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QXD
{
    [Flags]
    public enum QxdUpdate
    {
        UPDATE_ANY = 0,
        UPDATE_FIRST_RAID = 1,
        UPDATE_DLC1 = 2,
        UPDATE_DLC2 = 4,
        UPDATE_DLC3 = 8,
        UPDATE_DLC4 = 0x10,
        UPDATE_LEGEND_PATROL = 0x20,
        UPDATE_DLC5 = 0x40,
        UPDATE_HERO_COLOSSEUM = 0x80,
        UPDATE_DLC6 = 0x100,
        UPDATE_DLC7 = 0x200,
        UPDATE_DLC8 = 0x400,
        UPDATE_DLC9 = 0x800,
        UPDATE_DLC10 = 0x1000,
        UPDATE_DEVELOPER = 0x10000000,
    };

    [Flags]
    public enum QxdDlc
    {
        None = 0,
        DLC1 = 1,
        DLC2 = 2,
        DLC3 = 4,
        DLC4 = 8,
        Legend_Patrol = 0x10,
        DLC5 = 0x20,
        DLC6 = 0x40,
        DLC7 = 0x80,
        DLC8 = 0x100,
        DLC9 = 0x200,
        DLC10 = 0x400
    };

    public enum QxdItemType
    {
        ITEM_TOP = 0,
        ITEM_BOTTOM = 1,
        ITEM_GLOVES = 2,
        ITEM_SHOES = 3,
        ITEM_ACCESSORY = 4,
        ITEM_SUPERSOUL = 5,
        ITEM_MATERIAL = 6, // material_item.idb
        ITEM_EXTRA = 7,
        ITEM_BATTLE = 8, // battle_item.idb
        ITEM_COLLECTION = 10, // Qxd collection
        ITEM_TTL_OBJECT = 100,
        ITEM_TTL_SKILL = 101, // I guess id is one from those tdb files
        ITEM_TTL_FIGURE = 102, // I guess id is one from those tdb files
                               // There is a 999 referenced in a collection in some .qxd, but the collection isn't used in any item reward
    };

    public enum QxdSkillType
    {
        SKILL_SUPER,
        SKILL_ULTIMATE,
        SKILL_EVASIVE,
        SKILL_BLAST,
        SKILL_AWAKEN
    };

    [YAXSerializeAs("QXD")]
    public class QXD_File
    {
        //header and end of file data
        [YAXAttributeForClass]
        public int I_40 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("UnkFloats")]
        [YAXAttributeForClass]
        [YAXFormat("0.0###########")]
        public float[] EndFloats { get; set; }
        //end

        public List<Quest_Data> Quests { get; set; }
        [YAXSerializeAs("NormalCharacters")]
        public List<Quest_Characters> Characters1 { get; set; }
        [YAXSerializeAs("SpecialCharacters")]
        public List<Quest_Characters> Characters2 { get; set; }
        [YAXDontSerializeIfNull]
        public List<QXD_CollectionEntry> Collections { get; set; }

        public void SortEntries()
        {
            if (Quests != null)
                Quests.Sort((x, y) => x.SortID - y.SortID);
            if (Collections != null)
                Collections.Sort((x, y) => x.SortID - y.SortID);
        }

        public Quest_Data GetQuest(string qxdQuestName)
        {
            foreach (var quest in Quests)
            {
                if (quest.Name == qxdQuestName) return quest;
            }
            return null;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static QXD_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetQxdFile();
        }

        public List<string> GetAllUsedCollectionIds()
        {
            List<string> ids = new List<string>();
            if (Collections == null) return ids;

            foreach (var collection in Collections)
            {
                if (!ids.Contains(collection.I_00))
                    ids.Add(collection.I_00);
            }

            return ids;
        }

        public List<string> GetAllUsedCharacterIds()
        {
            List<string> ids = new List<string>();
            if (Characters1 == null) return ids;

            foreach (var character in Characters1)
            {
                if (!ids.Contains(character.Index))
                    ids.Add(character.Index);
            }

            if (Characters2 == null) return ids;

            foreach (var character in Characters2)
            {
                if (!ids.Contains(character.Index))
                    ids.Add(character.Index);
            }

            return ids;
        }

        /// <summary>
        /// Gets the quests to write to binary, adding dummy placeholder entries to fill gaps.
        /// </summary>
        public List<Quest_Data> GetQuestsToWrite()
        {
            List<Quest_Data> quests = new List<Quest_Data>();

            for (int i = 0; i < Quests.Count; i++)
                quests.Add(Quests[i]);

            quests.Sort((x, y) => x.SortID - y.SortID);

            int count = quests.Max(x => x.SortID) + 1;
            int prevID = -1;

            for (int i = 0; i < count; i++)
            {
                //Check if there is an ID gap, and fill any with a dummy quest to maintain ID consistency
                if (prevID + 1 != quests[i].SortID)
                {
                    quests.Insert(i, Quest_Data.CreatePlaceholderQuest(i));
                }

                prevID = quests[i].SortID;
            }

            return quests;
        }
    }

    [YAXSerializeAs("Quest")]
    public class Quest_Data : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Episode")]
        public int I_20 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("SubType")]
        public int I_24 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("NumPlayers")]
        public int I_28 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_40")]
        public short I_40 { get; set; }
        [YAXSerializeAs("ID")]
        [YAXAttributeFor("Parent_Quest")]
        public short I_42 { get; set; }
        [YAXSerializeAs("ID")]
        [YAXAttributeFor("Unlock_Requirement")]
        public int I_64 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_44")]
        public short I_44 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_46")]
        public short I_46 { get; set; }
        [YAXAttributeFor("Time_Limit")]
        [YAXSerializeAs("seconds")]
        public short I_104 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Difficulty_Menu")]
        public short I_106 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Start Stage")]
        public short I_108 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Start Demo")]
        public short I_110 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Xp_Reward")]
        public int I_120 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Ult Xp Reward")]
        public int I_124 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Fail Xp Reward")]
        public int I_128 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Zeni Reward")]
        public int I_132 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Ult Zeni Reward")]
        public int I_136 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Fail Zeni Reward")]
        public int I_140 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("TP_Medals_Once")]
        public int I_144 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("TP_Medals")]
        public int I_148 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("TP_Medals_Special")]
        public int I_152 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Resistance_Points")]
        public int I_156 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_192")]
        public int I_192 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Flags")]
        public int I_248 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_252")]
        public int I_252 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Update_Requirement")]
        public QxdUpdate I_256 { get; set; } //Int
        [YAXSerializeAs("value")]
        [YAXAttributeFor("DLC_Flag")]
        public QxdDlc I_260 { get; set; } //Int
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_264")]
        public int I_264 { get; set; }
        [YAXSerializeAs("No Enemy")]
        [YAXAttributeFor("Music")]
        public short I_268 { get; set; }
        [YAXSerializeAs("Enemy_Near")]
        [YAXAttributeFor("Music")]
        public short I_270 { get; set; }
        [YAXSerializeAs("Battle")]
        [YAXAttributeFor("Music")]
        public short I_272 { get; set; }
        [YAXSerializeAs("Ult_Finish")]
        [YAXAttributeFor("Music")]
        public short I_274 { get; set; }
        [YAXAttributeFor("F_276")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_276 { get; set; }
        [YAXAttributeFor("I_280")]
        [YAXSerializeAs("value")]
        public int I_280 { get; set; }

        //New in 1.21:
        [YAXAttributeFor("NEW_I_108")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int NEW_I_108 { get; set; }
        [YAXAttributeFor("NEW_I_112")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int NEW_I_112 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("StageIDs")]
        [YAXAttributeFor("Stage Portrait")]
        public List<short> StageDisplay { get; set; } //array size = 16
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("values")]
        [YAXAttributeFor("I_48")]
        public int[] I_48 { get; set; } //array of size 4
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("values")]
        [YAXAttributeFor("I_68")]
        public int[] I_68 { get; set; } //array of size 5
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("values")]
        [YAXAttributeFor("I_232")]
        public short[] I_232 { get; set; } //array of size 10


        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("MsgEntries")]
        [YAXSerializeAs("entries")]
        public List<string> MsgFiles { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("Qed_Files")]
        [YAXSerializeAs("names")]
        public List<string> QedFiles { get; set; }
        [YAXDontSerializeIfNull]
        public List<UnkNum1> UnknownNum1 { get; set; }
        [YAXDontSerializeIfNull]
        public List<UnkNum2> UnknownNum2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("EquipmentRewards")]
        public List<EquipmentReward> EquipReward { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SkillRewards")]
        public List<SkillReward> Skill_Reward { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("CharacterUnlocks")]
        public List<CharaUnlock> Chara_Unlock { get; set; }
        [YAXSerializeAs("EnemyPortraits")]
        public List<EnemyPortrait> EnemyPortraitDisplay { get; set; } //6 entries

        public bool IsEmpty()
        {
            if (Name.Split('_')[0] == "empty") return true;
            return false;
        }

        public bool IsPlaceholder()
        {
            return Name.Contains("dummyQ");
        }

        public static Quest_Data CreatePlaceholderQuest(int id)
        {
            Quest_Data dummyQuest = new Quest_Data()
            {
                Name = $"dummyQ_{id}",
                Index = id.ToString(),
                I_248 = -1,
                MsgFiles = new List<string>()
                {
                    "dummy",
                    "dummy",
                    "dummy",
                    "dummy",
                    "dummy",
                    "dummy",
                },
                EnemyPortraitDisplay = new List<EnemyPortrait>()
                {
                    new EnemyPortrait(),
                    new EnemyPortrait(),
                    new EnemyPortrait(),
                    new EnemyPortrait(),
                    new EnemyPortrait(),
                    new EnemyPortrait(),
                },
                StageDisplay = new short[16].ToList(),
                I_48 = new int[4],
                I_68 = new int[5],
                I_232 = new short[8],
                F_276 = 1f
            };

            dummyQuest.I_232[6] = -1;
            dummyQuest.I_232[7] = -1;

            if (dummyQuest.Name.Length > 16)
                throw new Exception("Dummy quest name is too long!");

            return dummyQuest;
        }
    }

    public class EnemyPortrait
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Chara_ID")]
        public short CharaID { get; set; } = -1;
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public short CostumeIndex { get; set; } = -1;
        [YAXAttributeForClass]
        [YAXSerializeAs("Transformation")]
        public short State { get; set; } = -1;
    }

    public class UnkNum1
    {
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        public short[] I_00 { get; set; }
    }

    public class UnkNum2
    {
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        public short[] I_00 { get; set; }
    }

    public class EquipmentReward
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public QxdItemType I_00 { get; set; } //Int
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Condition")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_12")]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flags")]
        public int I_16 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_20")]
        public int I_20 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Chance")]
        [YAXFormat(".0###########")]
        public float F_24 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_28")]
        public int I_28 { get; set; }
    }

    public class SkillReward
    {
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public QxdSkillType I_00 { get; set; } //Int
        [YAXSerializeAs("ID2")]
        [YAXAttributeForClass]
        public int I_04 { get; set; }
        [YAXSerializeAs("Condition")]
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXSerializeAs("I_12")]
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXSerializeAs("Chance")]
        [YAXAttributeForClass]
        [YAXFormat(".0###########")]
        public float F_16 { get; set; }
    }

    public class CharaUnlock
    {
        [YAXSerializeAs("CMS_Name")]
        [YAXAttributeForClass]
        public string ShortName { get; set; }
        [YAXSerializeAs("Costume")]
        [YAXAttributeForClass]
        public short CostumeIndex { get; set; }
        [YAXSerializeAs("I_06")]
        [YAXAttributeForClass]
        public short I_06 { get; set; }
    }

    [YAXSerializeAs("Character")]
    public class Quest_Characters : IInstallable
    {

        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXAttributeForClass]
        [YAXSerializeAs("Character")]
        public string CharaShortName { get; set; }
        [YAXAttributeFor("Costume_Index")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Stamina_Armour")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Basic Melee")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Ki Blast")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Strike Super")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Ki Super")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Basic Melee Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Ki Blast Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Strike Super Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Ki Super Damage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("F_72")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("Air Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("Boost Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("AI_Table")]
        [YAXSerializeAs("ID")]
        public int I_84 { get; set; } //Int32

        [YAXSerializeAs("value")]
        [YAXAttributeFor("Transformation")]
        public short I_120 { get; set; }
        [YAXAttributeFor("Super Soul")]
        [YAXSerializeAs("value")]
        public ushort I_122 { get; set; } //ushort
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_106")]
        [YAXSerializeAs("values")]
        public short[] I_106 { get; set; }

        //New in 1.21+
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)0)]
        public ushort I_124 { get; set; }
        [YAXAttributeFor("I_126")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)0)]
        public ushort I_126 { get; set; }

        [BindingSubClass]
        [YAXSerializeAs("Skills")]
        public Skills _Skills { get; set; }
    }

    [BindingSubClass]
    public class Skills
    {
        //All values uint16

        [YAXAttributeFor("Super_1")]
        [YAXSerializeAs("ID2")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("Super_2")]
        [YAXSerializeAs("ID2")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Super_3")]
        [YAXSerializeAs("ID2")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Super_4")]
        [YAXSerializeAs("ID2")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("Ultimate_1")]
        [YAXSerializeAs("ID2")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("Ultimate_2")]
        [YAXSerializeAs("ID2")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Evasive")]
        [YAXSerializeAs("ID2")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Blast_Type")]
        [YAXSerializeAs("ID2")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("Awoken")]
        [YAXSerializeAs("ID2")]
        public ushort I_16 { get; set; }
    }

    public class QXD_CollectionEntry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return string.Format("{0}_{1}_{2}_{3}_{4}_{5}", I_00, (ushort)I_02, I_04, I_06, I_08, I_10);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 6)
                {
                    I_00 = split[0];
                    I_02 = (QxdItemType)ushort.Parse(split[1]);
                    I_04 = ushort.Parse(split[2]);
                    I_06 = ushort.Parse(split[3]);
                    I_08 = ushort.Parse(split[4]);
                    I_10 = ushort.Parse(split[5]);
                }
            }
        }


        [YAXAttributeForClass]
        [YAXSerializeAs("Collection_ID")]
        [BindingAutoId]
        public string I_00 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("Item_Type")]
        public QxdItemType I_02 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("Item_ID")]
        public ushort I_04 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        public ushort I_06 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("I_08")]
        public ushort I_08 { get; set; } //uint16
        [YAXAttributeForClass]
        [YAXSerializeAs("I_10")]
        public ushort I_10 { get; set; } //uint16

        public static QXD_CollectionEntry Read(byte[] bytes, int offset)
        {
            return new QXD_CollectionEntry()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset + 0).ToString(),
                I_02 = (QxdItemType)BitConverter.ToUInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                I_08 = BitConverter.ToUInt16(bytes, offset + 8),
                I_10 = BitConverter.ToUInt16(bytes, offset + 10)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(ushort.Parse(I_00)));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));

            if (bytes.Count != 12) throw new Exception("QXD_CollectionEntry must be 12 bytes.");
            return bytes.ToArray();
        }

    }

}
