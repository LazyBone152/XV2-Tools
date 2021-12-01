using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;
using System.Windows.Data;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Media;

#if SaveEditor
using LB_Save_Editor;
using LB_Save_Editor.ID;
#endif

namespace Xv2CoreLib.SAV
{
    //WARNING: Very sloppy code in here. 
    //I made the mistake of mixing the code of the save editor with the base save file class, and the result was this abomination...
    //The code that relied on the save editor has been separated out and turned into extension methods, but a lot still remians (mostly properties).
    //Fixing this will require way too much time that is better spent elsewhere, so like this it will remain...

    #region EnumsAndOffsets
    public static class Offsets
    {
        //Misc
        public const int XV1_SAVE_SIZE = 210816;
        public const int DECRYPTED_SAVE_SIZE_V1 = 504720;
        public const int ENECRYPTED_SAVE_SIZE_V1 = 504896;
        public const int DECRYPTED_SAVE_SIZE_V10 = 722968;
        public const int ENECRYPTED_SAVE_SIZE_V10 = 723136;
        public const int ENCRYPTED_SAVE_SIZE_V21 = 914080;
        public const int DECRYPTED_SAVE_SIZE_V21 = 913912;


        //Equipment Flags
        public const int TOP_NEW_FLAG = 272;
        public const int BOTTOM_NEW_FLAG = 336;
        public const int GLOVES_NEW_FLAG = 400;
        public const int SHOES_NEW_FLAG = 464;
        public const int ACCESSORY_NEW_FLAG = 528;
        public const int SUPERSOUL_NEW_FLAG = 592;
        public const int MIX_NEW_FLAG = 656;
        public const int IMPORTANT_NEW_FLAG = 720;
        public const int CAPSULE_NEW_FLAG = 784;

        public const int TOP_ACQUIRED_FLAG = 848;
        public const int BOTTOM_ACQUIRED_FLAG = 912;
        public const int GLOVES_ACQUIRED_FLAG = 976;
        public const int SHOES_ACQUIRED_FLAG = 1040;
        public const int ACCESSORY_ACQUIRED_FLAG = 1104;
        public const int SUPERSOUL_ACQUIRED_FLAG = 1168;
        public const int MIX_ACQUIRED_FLAG = 1232;
        public const int IMPORTANT_ACQUIRED_FLAG = 1296;
        public const int CAPSULE_ACQUIRED_FLAG = 1360;

        //Xv1 Hero
        public const int XV1_HERO_OFFSET = 502388;
        public const int XV1_HERO_SIZE = 272;


        //Inventory
        public const int INVENTORY_TOP = 5136;
        public const int INVENTORY_BOTTOM = 9232;
        public const int INVENTORY_GLOVES = 13328;
        public const int INVENTORY_SHOES = 17424;
        public const int INVENTORY_ACCESSORIES = 21520;
        public const int INVENTORY_SUPERSOUL = 25616;
        public const int INVENTORY_MIXITEMS = 29712;
        public const int INVENTORY_IMPORTANTITEMS = 33808;
        public const int INVENTORY_CAPSULES = 37904;
        public const int INVENTORY_QQBANGS = 42000;

        //Battle Items
        public const int BATTLE_ITEMS = 46096;

        //Skills
        public const int SKILLS_SUPER = 46128;
        public const int SKILLS_ULTIMATE = 54320;
        public const int SKILLS_EVASIVE = 62512;
        public const int SKILLS_UNK3 = 70704;
        public const int SKILLS_BLAST = 78896;
        public const int SKILLS_AWOKEN = 87088;

        public const int SUPER_SKILL_COUNT = 1024;
        public const int ULTIMATE_SKILL_COUNT = 1024;
        public const int EVASIVE_SKILL_COUNT = 1024;
        public const int UNK3_SKILL_COUNT = 1024;
        public const int BLAST_SKILL_COUNT = 1024;
        public const int AWOKEN_SKILL_COUNT = 1024;

        //Quests
        public const int QUESTS_TPQ = 97840;
        public const int QUESTS_TMQ = 100912;
        public const int QUESTS_BAQ = 105520;
        public const int QUESTS_TCQ = 108592;
        public const int QUESTS_HLQ = 114736;
        public const int QUESTS_RBQ = 117040;
        public const int QUESTS_CHQ = 119344;
        public const int QUESTS_LEQ = 120880;
        public const int QUESTS_OSQ = 524984;
        public const int QUESTS_PRB = 525944;
        public const int QUESTS_TTQ = 532664;
        public const int QUESTS_TFB = 533176;

        public const int QUESTS_TPQ_COUNT = 128;
        public const int QUESTS_TMQ_COUNT = 192;
        public const int QUESTS_BAQ_COUNT = 128;
        public const int QUESTS_TCQ_COUNT = 256;
        public const int QUESTS_HLQ_COUNT = 96;
        public const int QUESTS_RBQ_COUNT = 96;
        public const int QUESTS_CHQ_COUNT = 64;
        public const int QUESTS_LEQ_COUNT = 128;
        public const int QUESTS_OSQ_COUNT = 39;
        public const int QUESTS_PRB_COUNT = 39;
        public const int QUESTS_TTQ_COUNT = 64;
        public const int QUESTS_TFB_COUNT = 256; //Not 100% sure

        public const int TOKIPEDIA = 532280;
        public const int TOKIPEDIA_COUNT = 12;

        //Character Data
        public const int CAC_START = 95280;
        public const int SYSTEM_FLAGS = 95280;
        public const int CAC_DLC_START = 519816;
        public const int CAC = 124976;
        public const int CAC_PRESET = 125184;
        public const int MENTOR_PROGRESS = 125956;
        public const int PLAYDATA = 126276;

        //Partner Customization
        public const int MENTOR_CUSTOMIZATION = 520096; //47 entries, size 92
        public const int MENTOR_CUSTOMIZATION2 = 524432; //10 entries, size 10
        public const int MENTOR_CUSTOMIZATION3 = 757220; //54 entries, size 44

        public const int MENTOR_COUNT = 33; //Max possible of 40, but only 33 are used. (33 mentors + 1 thats unused but has data, but we will ignore that one)
        public const int MAX_CUSTOM_PARTNERS = 111;

        public const int MENTOR_CUSTOMIZATION_COUNT = 47; //Original 47 slots
        public const int MENTOR_CUSTOMIZATION_COUNT2 = 10; //Extended slots in 1.15
        public const int MENTOR_CUSTOMIZATION_COUNT3 = 54; //Extended slots in 1.17

        //Flags for customization unlocks. 28 bytes per partner.
        public const int MENTOR_CUSTOMIZATION_FLAGS = 504756; // (0 - 46)
        public const int MENTOR_CUSTOMIZATION_FLAGS2 = 506492; // (47 - 56)
        public const int MENTOR_CUSTOMIZATION_FLAGS3 = 722972; // (57 - 110)

        //Sizes
        public const int CAC_SIZE = 50888;
        public const int CAC_DLC_SIZE = 25392;
        public const int CAC_DLC2_SIZE = 19588; //New save data in 1.17
        public const int SYSTEM_FLAGS_COUNT = 1024;

        //Counts
        public const int INVENTORY_TOP_MAX = 512;
        public const int INVENTORY_BOTTOM_MAX = 512;
        public const int INVENTORY_GLOVES_MAX = 512;
        public const int INVENTORY_SHOES_MAX = 512;
        public const int INVENTORY_ACCESSORY_MAX = 512;
        public const int INVENTORY_SUPERSOUL_MAX = 512;
        public const int INVENTORY_MIXITEM_MAX = 512;
        public const int INVENTORY_IMPORTANT_MAX = 512;
        public const int INVENTORY_CAPSULE_MAX = 512;
        public const int INVENTORY_QQBANG_MAX = 512;

        //HC
        public const int HC_SKILL_UNLOCKED_FLAGS = 517480;
        public const int HC_SKILL_UNKNOWN_FLAGS = 517608;
        public const int HC_SKILL_QUANTITY = 517736;
        public const int HC_ITEM_UNLOCKED_FLAGS = 518760;
        public const int HC_ITEM_UNKNOWN_FLAGS = 518776;
        public const int HC_ITEM_QUANTITY = 518792;
        public const int HC_FIGURE_COLLECTION_UNLOCKED_FLAGS = 518920;
        public const int HC_FIGURE_COLLECTION_NEW_FLAGS = 519176;
        public const int HC_FIGURE_COLLECTION_UNKNOWN_FLAGS = 519048;
        public const int HC_FIGURE_COLLECTION_COUNT = 1024; //Number of current ingame figures. Doesn't account for any added by mods.
        public const int HC_GLOBAL_VALUES = 519304;
        public const int HC_ITEM_COUNT = 128;
        public const int HC_SKILL_COUNT = 1024;
        public const int HC_FIGURE_INVENTORY = 507064;
        public const int HC_FIGURE_INVENTORY2 = 726968; //Added in 1.17.01
        public const int HC_FIGURE_INVENTORY_COUNT = 256;
        public const int HC_FIGURE_INVENTORY_COUNT2 = 344; //Added in 1.17.01
        public const int HC_DECK = 517304;

        //New stuff in 1.15
        public const int PARTNER_KEY_FLAGS = 506772; //These need to be set or else the keys dont work (1 - 10)
        public const int MASCOT_FLAGS_OFFSET = 204;
        public const int MASCOT_COUNT = 64; //64 bytes (512 bits)
        public const int ARTWORK_FLAGS_OFFSET = 506228;
        public const int ARTWORK_COUNT = 64; //64 bytes (assumption)

        //New stuff in 1.17
        public const int PARTNER_KEY_FLAGS2 = 724484; //Like the previous keys, these need to be set if the keys are in the inventory. (11 - 15)



    }

    public static class FigureGrowth
    {
        public const int HP = 150;
        public const int ATK = 100;
        public const int DEF = 50;
        public const int SPD = 100;

        public static int CalculateLevelBonus(int level, int min, int max, int rarity)
        {
            int bonus = max - min;
            float increasePerLevel = (float)bonus / (float)(GetMaxLevel(rarity) - 1);

            if (rarity == 3)
            {
                increasePerLevel = increasePerLevel * 1.41955f;
            }

            int ret = (int)(increasePerLevel * (level - 1));


            return Utils.RoundOff(ret);
        }

        public static int GetMaxLevel(int rarity)
        {
            switch (rarity)
            {
                case 0:
                    return 40;
                case 1:
                    return 50;
                case 2:
                    return 60;
                case 3:
                    return 99;
                default:
                    return 1;
            }
        }
    }
    

    public enum SkillType
    {
        Super = 0,
        Ultimate = 1,
        Evasive = 2,
        Blast = 3,
        Awoken = 5,
        All
    }

    public enum EquipmentType
    {
        Top,
        Bottom,
        Gloves,
        Shoes,
        Accessory,
        SuperSoul,
        MixItem,
        ImportantItem,
        Capsule,
        QQBang
    }
    
    public enum QuestType
    {
        Null = -1,
        TPQ = 0,
        TMQ = 1,
        BAQ = 2,
        TCQ = 3,
        HLQ = 4,
        RBQ = 5,
        CHQ = 6,
        LEQ = 7,
        OSQ = 11,
        PRB = 12,
        TTQ,
        TFB,
    }

    [Flags]
    public enum AccountFlags : int
    {
        Unk1 = 1,
        Unk2 = 2,
        Unk3 = 4,
        Unk4 = 8,
        Unk5 = 16,
        Unk6 = 32,
        Level85Unlock = 64, //7
        Level90Unlock = 128, //8
        Level95Unlock = 256, //9
        Level99Unlock = 512, //10
        Unk11 = 1024,
        Unk12 = 2048,
        Unk13 = 4096,
        Unk14 = 8192,
        Unk15 = 16384,
        Unk16 = 16384,
        Unk17 = 32768,
        Unk18 = 65536,
        Unk19 = 131072,
        Unk20 = 262144,
        Unk21 = 524288,
        Unk22 = 1048576,
        Unk23 = 2097152,
        Unk24 = 4194304,
        Unk25 = 8388608,
        Unk26 = 16777216,
        Unk27 = 33554432,
        Unk28 = 67108864,
        Unk29 = 134217728,
        Unk30 = 268435456,
        Unk31 = 536870912,
        Unk32 = 1073741824
    }

    [Flags]
    public enum MentorFlags : int
    {
        FirstContact = 1, //1
        Unk2 = 2,
        Unk3 = 4,
        Unk4 = 8,
        GivenDualSkill = 16,
        Unk6 = 32,
        Unk7 = 64, //7
        Unk8 = 128, //8
        Unk9 = 256, //9
        Unk10 = 512, //10
        Unk11 = 1024,
        Unk12 = 2048,
        Unk13 = 4096,
        Unk14 = 8192,
        Unk15 = 16384,
        Unk16 = 16384,
        Unk17 = 32768,
        Unk18 = 65536,
        Unk19 = 131072,
        Unk20 = 262144,
        Unk21 = 524288,
        Unk22 = 1048576,
        Unk23 = 2097152,
        Unk24 = 4194304,
        Unk25 = 8388608,
        Unk26 = 16777216,
        Unk27 = 33554432,
        Unk28 = 67108864,
        Unk29 = 134217728,
        Unk30 = 268435456,
        Unk31 = 536870912,
        Unk32 = 1073741824
    }
    
    [Flags]
    public enum DeckFlags : byte
    {
        Deck1 = 1,
        Deck2 = 2,
        Deck3 = 4,
        Deck4 = 8,
        Deck5 = 16,
        Deck6 = 32,
        Deck7 = 64,
        Deck8 = 128
    }

    [Flags]
    public enum MentorCustomizationFlags : ushort
    {
        Costume = 1,
        Skill = 2,
        SuperSoul = 4
    }

    [Flags]
    public enum TokipediaFlags : ulong
    {
        None = 0,
        Krillin = 1,
        Tien = 2,
        Yamcha = 4,
        Piccolo = 8,
        Raditz = 16,
        KidGohan = 32,
        Nappa = 64,
        Vegeta = 128,
        Zarbon = 256,
        Dodoria = 512,
        Ginyu = 1024,
        Frieza = 2048,
        Android18 = 4096,
        Cell = 8192,
        LordSlug = 16384,
        MajinBuu = 32768,
        Hercule = 65536,
        AdultGohan = 131072,
        Gotenks = 262144,
        Turles = 524288,
        Broly = 1048576,
        Beerus = 2097152,
        Pan = 4194304,
        Jaco = 8388608,
        Goku = 16777216,
        Whis = 33554432,
        Cooler = 67108864,
        Android16 = 134217728,
        FutureGohan = 268435456,
        Bardock = 536870912,
        Hit = 1073741824,
        Bojack = 2147483648,
        Zamasu = 4294967296,
        Videl = 35184372088832,
        Fuu = 70368744177664,
        Bitmask = 18446744073709551615
    }

    public enum Race
    {
        HUM = 0,
        HUF = 1,
        SYM = 2,
        SYF = 3,
        NMC = 4,
        FRI = 5,
        MAM = 6,
        MAF = 7
    }

    public enum Mentors
    {
        Krillin = 0,
        Tien,
        Yamcha,
        Piccolo,
        Raditz,
        [YAXSerializeAs("Gohan (Kid)")]
        KidGohan,
        Nappa,
        Vegeta,
        Zarbon,
        Dodoria,
        [YAXSerializeAs("Captain Ginyu")]
        CaptainGinyu,
        Frieza,
        [YAXSerializeAs("Android 18")]
        Android18,
        [YAXSerializeAs("Cell (Perfect)")]
        Cell,
        [YAXSerializeAs("Lord Slug")]
        LordSlug,
        [YAXSerializeAs("Majin Buu")]
        MajinBuu,
        Hercule,
        [YAXSerializeAs("Gohan and Videl")]
        GohanAndVidel,
        Gotenks,
        Turles,
        Broly,
        Beerus,
        Pan,
        Jaco,
        Goku,
        Whis,
        Cooler,
        [YAXSerializeAs("Android 16")]
        Android16,
        [YAXSerializeAs("Gohan (Future)")]
        FutureGohan,
        Bardock,
        Hit,
        Bojack,
        Zamasu,
    }

    public enum QQAttribute
    {
        [YAXSerializeAs("-5")]
        Minus5 = 0,
        [YAXSerializeAs("-4")]
        Minus4 = 1,
        [YAXSerializeAs("-3")]
        Minus3 = 2,
        [YAXSerializeAs("-2")]
        Minus2 = 3,
        [YAXSerializeAs("-1")]
        Minus1 = 4,
        [YAXSerializeAs("+0")]
        Zero = 5,
        [YAXSerializeAs("+1")]
        Plus1 = 6,
        [YAXSerializeAs("+2")]
        Plus2 = 7,
        [YAXSerializeAs("+3")]
        Plus3 = 8,
        [YAXSerializeAs("+4")]
        Plus4 = 9,
        [YAXSerializeAs("+5")]
        Plus5 = 10,
        Null = 15
    }

    public enum TrainingLevel
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2,
        Kai = 3,
        God = 4,
        Super = 5
    }

    public enum FriezaFaction
    {
        None = 0,
        Dodoria = 1,
        Zarbon = 2,
        Cooler = 3,
        Frieza = 4
    }

    public enum QuestState
    {
        Locked = 0,
        NotComplete = 1,
        New = 2,
        Cleared = 3
    }

    public enum QuestRank
    {
        NotComplete = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        Z = 6
    }

    public enum QuestWinCondition
    {
        Locked = 0,
        Hidden = 1,
        First = 2,
        Both = 3
    }

    public static class TrainingXpAmount
    {
        //These values are incorrect, I think. Need to research them.
        public const int BEGINNER_MIN = 0;
        public const int BEGINNER_MAX = 112;
        public const int INTERMEDIATE_MIN = 113;
        public const int INTERMEDIATE_MAX = 256;
        public const int ADVANCED_MIN = 257;
        public const int ADVANCED_MAX = 400;
        public const int KAI_MIN = 401;
        public const int KAI_MAX = 704;
        public const int GOD_MIN = 705;
        public const int GOD_MAX = 1008;
        public const int SUPER_MIN = 1009;
        public const int SUPER_MAX = 1232;
    }

#endregion

    [YAXSerializeAs("Xv2SaveFile")]
    public class SAV_File : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static int SAV_SIGNATURE = 1447121699;
        public const ulong VERSION_XOR = 0x7468656265636F6E;

        #region VersionUiProperties
        [YAXDontSerialize]
        public string VersionString
        {
            get
            {
                switch (Version)
                {
                    case 1:
                        return "1.00";
                    case 2:
                        return "1.01";
                    case 3:
                        return "1.02";
                    case 4:
                        return "1.03";
                    case 5:
                        return "1.04.1";
                    case 6:
                        return "1.05";
                    case 7:
                        return "1.06";
                    case 8:
                        return "1.07";
                    case 9:
                        return "1.07";
                    case 10:
                        return "1.08";
                    case 11:
                        return "1.09";
                    case 12:
                        return "1.10";
                    case 13:
                        return "1.11";
                    case 14:
                        return "1.12";
                    case 15:
                        return "1.13";
                    case 16:
                        return "1.14";
                    case 17:
                        return "1.14.1";
                    case 18:
                        return "1.15";
                    case 19:
                        return "1.15.01";
                    case 20:
                        return "1.16";
                    case 21:
                        return "1.16.01";
                    case 22:
                        return "1.17";
                    case 23:
                        return "1.17.01";
                    default:
                        return String.Format("Unknown ({0})", Version);

                }
            }
        }

        [YAXDontSerialize]
        public string VersionToolTip
        {
            get
            {
                switch (Version)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        return "The following data can't be edited on this save version:\n-Hero Colosseum\n-Infinite History\n-Mentor Customization\n-Crystal Raid";
                    case 10:
                        return "The following data can't be edited on this save version:\n-Infinite History\n-Mentor Customization\n-Crystal Raid";
                    case 11:
                    case 12:
                        return "The following data can't be edited on this save version:\n-Crystal Raid";
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                        return "The following data can't be edited on this save version:\n-Artwork\n-Mascots";
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                        return null;
                    default:
                        return "This save version is not supported. It is recommened to update the application (if one is available).";
                }
            }
        }

        [YAXDontSerialize]
        public Brush VersionBrush
        {
            get
            {
                switch (Version)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                        return Brushes.OrangeRed;
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                        return Brushes.Blue;
                    default:
                        return Brushes.Red;

                }
            }
        }


        //Version bools
        [YAXDontSerialize]
        public bool DLC5
        {
            get
            {
                if (Version >= 10) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public bool DLC6
        {
            get
            {
                if (Version >= 11) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public bool DLC7
        {
            get
            {
                if (Version >= 12) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public bool DLC8
        {
            get
            {
                if (Version >= 13) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public bool DLC11
        {
            get
            {
                if (Version >= 18) return true;
                return false;
            }
        }

        #endregion

        [YAXDontSerialize]
        public int FigureInventorySize
        {
            get
            {
                return (Version >= 23) ? Offsets.HC_FIGURE_INVENTORY_COUNT + Offsets.HC_FIGURE_INVENTORY_COUNT2 : Offsets.HC_FIGURE_INVENTORY_COUNT;
            }
        }

        #region FileInfo
        //Bytes
        [YAXAttributeForClass]
        public bool IsEncrypted { get; set; }
        [YAXAttributeFor("BaseFile")]
        [YAXSerializeAs("bytes")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<byte> FileBytes { get; set; }
        [YAXDontSerialize]
        private byte[] _bytes = null;
        [YAXDontSerialize]
        public byte[] bytes
        {
            get
            {
                if (_bytes == null)
                {
                    _bytes = FileBytes.ToArray();
                    return _bytes;
                }
                return _bytes;
            }
            set
            {
                if (value != _bytes)
                {
                    _bytes = value;
                }
            }
        }

#endregion

        //Header
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        public byte Version { get; set; }
        [YAXAttributeFor("SteamID")]
        [YAXSerializeAs("value")]
        public UInt64 SteamID { get; set; }
        [YAXAttributeFor("Zeni")]
        [YAXSerializeAs("value")]
        public UInt32 Zeni { get; set; }
        [YAXAttributeFor("TPMedals")]
        [YAXSerializeAs("value")]
        public UInt32 TPMedals { get; set; }
        [YAXAttributeFor("AccFlags")]
        [YAXSerializeAs("values")]
        public AccountFlags AccFlags { get; set; }

        private ObservableCollection<CaC> _characters = null;
        public ObservableCollection<CaC> Characters //Size 8
        {
            get
            {
                return this._characters;
            }

            set
            {
                if (value != this._characters)
                {
                    this._characters = value;
                    NotifyPropertyChanged("Characters");
                }
            }
        }
        public Inventory Inventory { get; set; }
        public BattleItems BattleItems { get; set; }
        public Skills Skills { get; set; }
        public HeroColosseumGlobal HeroColosseum { get; set; }
        public Xv1Hero Xv1Hero { get; set; }
        public List<MentorCustomizationUnlockFlag> MentorCustomizationUnlockFlags { get; set; }
        public List<MentorCustomizationUnlockFlag> MentorCustomizationUnlockFlags2 { get; set; }
        public List<MentorCustomizationUnlockFlag> MentorCustomizationUnlockFlags3 { get; set; }


        public ObservableCollection<MascotFlag> MascotFlags { get; set; }
        public ObservableCollection<ArtworkFlag> ArtworkFlags { get; set; }

        #region View
        private ListCollectionView _mascotView = null;
        [YAXDontSerialize]
        public ListCollectionView ViewMascotFlags
        {
            get
            {
                if (MascotFlags == null) return null;
                if (_mascotView != null)
                {
                    return _mascotView;
                }
                _mascotView = new ListCollectionView(MascotFlags);
                _mascotView.Filter = new Predicate<object>(MascotContains);
                return _mascotView;
            }
            set
            {
                if (value != _mascotView)
                {
                    _mascotView = value;
                    NotifyPropertyChanged(nameof(ViewMascotFlags));
                }
            }
        }

        private ListCollectionView _artworkView = null;
        [YAXDontSerialize]
        public ListCollectionView ViewArtworkFlags
        {
            get
            {
                if (ArtworkFlags == null) return null;
                if (_artworkView != null)
                {
                    return _artworkView;
                }
                _artworkView = new ListCollectionView(ArtworkFlags);
                _artworkView.Filter = new Predicate<object>(ArtworkContains);
                return _artworkView;
            }
            set
            {
                if (value != _artworkView)
                {
                    _artworkView = value;
                    NotifyPropertyChanged(nameof(ViewArtworkFlags));
                }
            }
        }

        private bool ArtworkContains(object obj)
        {
            ArtworkFlag item = obj as ArtworkFlag;

            if (item != null)
            {
                return item.HasName;
            }
            return false;
        }

        private bool MascotContains(object obj)
        {
            MascotFlag item = obj as MascotFlag;

            if (item != null)
            {
                return item.HasName;
            }
            return false;
        }
        #endregion

        public static SAV_File Load(string path, bool saveXml = true)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            bool encrypted = false;

            //Check is a XV2 save
            switch (rawBytes.Length)
            {
                case Offsets.DECRYPTED_SAVE_SIZE_V1:
                case Offsets.DECRYPTED_SAVE_SIZE_V10:
                    break;
                case Offsets.ENECRYPTED_SAVE_SIZE_V1:
                    encrypted = true;
                    rawBytes = Crypt.DecryptManaged_V1(rawBytes);
                    break;
                case Offsets.ENECRYPTED_SAVE_SIZE_V10:
                    encrypted = true;
                    rawBytes = Crypt.DecryptManaged_V10(rawBytes);
                    break;
                case Offsets.ENCRYPTED_SAVE_SIZE_V21:
                    encrypted = true;
                    rawBytes = Crypt.DecryptManaged_V21(rawBytes);
                    break;
                case Offsets.XV1_SAVE_SIZE:
                    throw new InvalidDataException("DBXV1 saves are not supported.");
                default:
                    throw new InvalidDataException("Unsupported save version. Load failed.");
            }
            
            List<byte> bytes = rawBytes.ToList();

            //Parse the file
            SAV_File savFile = ParseSav(rawBytes, bytes, encrypted);

            //Save xml (if needed)
            if (saveXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(SAV_File));
                serializer.SerializeToFile(savFile, path + ".xml");
            }

            return savFile;
        }

        public void Save(string path, bool writeFile = true)
        {
            var bytes = Write();
            FileBytes = bytes;
            this.bytes = bytes.ToArray();
            if (writeFile == false) return;

            //Encrypt if needed
            byte[] rawBytes = bytes.ToArray();
            if (IsEncrypted)
            {
                switch (rawBytes.Length)
                {
                    case Offsets.DECRYPTED_SAVE_SIZE_V1:
                        rawBytes = Crypt.EncryptManaged_V1(rawBytes);
                        break;
                    case Offsets.DECRYPTED_SAVE_SIZE_V10:
                        rawBytes = Crypt.EncryptManaged_V10(rawBytes);
                        break;
                    case Offsets.DECRYPTED_SAVE_SIZE_V21:
                        rawBytes = Crypt.EncryptManaged_V21(rawBytes);
                        break;
                    default:
                        throw new InvalidDataException("Invalid decrypted save size. Save failed.");
                }
            }

            File.WriteAllBytes(path, rawBytes);
        }

        public void SaveFileBytes()
        {
            //Saves all objects into the byte arrays without actually saving the file to disk
            FileBytes = Write();
            bytes = FileBytes.ToArray();
        }

        public static void SaveXml(string path)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            YAXSerializer serializer = new YAXSerializer(typeof(SAV_File), YAXSerializationOptions.DontSerializeNullObjects);
            var savFile = (SAV_File)serializer.DeserializeFromFile(path);
            var bytes = savFile.Write();

            //Encrypt if needed
            byte[] rawBytes = bytes.ToArray();

            if (savFile.IsEncrypted)
            {
                switch (rawBytes.Length)
                {
                    case Offsets.DECRYPTED_SAVE_SIZE_V1:
                        rawBytes = Crypt.EncryptManaged_V1(rawBytes);
                        break;
                    case Offsets.DECRYPTED_SAVE_SIZE_V10:
                        rawBytes = Crypt.EncryptManaged_V10(rawBytes);
                        break;
                    case Offsets.DECRYPTED_SAVE_SIZE_V21:
                        rawBytes = Crypt.EncryptManaged_V21(rawBytes);
                        break;
                    default:
                        throw new InvalidDataException("Invalid decrypted save size. Save failed.");
                }
            }

            File.WriteAllBytes(saveLocation, rawBytes);
        }

        private static SAV_File ParseSav(byte[] rawBytes, List<byte> bytes, bool _isEncrypted)
        {
            //Check system endianness
            if (!BitConverter.IsLittleEndian)
            {
                throw new Exception("Big Endian systems not supported.");
            }

            //Start
            SAV_File savFile = new SAV_File();
            savFile.IsEncrypted = _isEncrypted;

            //Header
            savFile.Version = rawBytes[5];
            savFile.FileBytes = bytes;
            savFile.SteamID = BitConverter.ToUInt64(rawBytes, 8) ^ VERSION_XOR;
            savFile.Zeni = BitConverter.ToUInt32(rawBytes, 44);
            savFile.TPMedals = BitConverter.ToUInt32(rawBytes, 48);
            savFile.AccFlags = (AccountFlags)BitConverter.ToUInt32(rawBytes, 76);
            
            //CaCs
            savFile.Characters = CaC.ReadAll(bytes, rawBytes, Offsets.CAC, savFile);

            //Inventory
            savFile.Inventory = Inventory.Read(rawBytes, bytes);

            //Battle Items
            savFile.BattleItems = BattleItems.Read(rawBytes);

            //Skills
            savFile.Skills = Skills.Read(rawBytes);

            //HC
            if (savFile.DLC5)
            {
                savFile.HeroColosseum = HeroColosseumGlobal.Read(rawBytes, bytes, savFile.Version);
            }

            //Mentor Customization Flags
            if (savFile.DLC6)
            {
                savFile.MentorCustomizationUnlockFlags = MentorCustomizationUnlockFlag.Read(bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS, Offsets.MENTOR_CUSTOMIZATION_COUNT);
            }

            if(savFile.Version >= 19)
            {
                savFile.MentorCustomizationUnlockFlags2 = MentorCustomizationUnlockFlag.Read(bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS2, Offsets.MENTOR_CUSTOMIZATION_COUNT2);
            }

            if (savFile.Version >= 22)
            {
                savFile.MentorCustomizationUnlockFlags3 = MentorCustomizationUnlockFlag.Read(bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS3, Offsets.MENTOR_CUSTOMIZATION_COUNT3);
            }

            //1.15 stuff
            if (savFile.DLC11)
            {
                savFile.MascotFlags = MascotFlag.LoadAll(bytes);
                savFile.ArtworkFlags = ArtworkFlag.LoadAll(bytes);
            }

            savFile.Xv1Hero = Xv1Hero.Read(rawBytes, bytes);

            return savFile;
        }

        private List<byte> Write()
        {
            if(Version >= 22 && FileBytes.Count != Offsets.DECRYPTED_SAVE_SIZE_V21) throw new InvalidDataException(String.Format("Invalid BaseFile bytes array size. Expected {1} but found {0}. Save failed.", FileBytes.Count, Offsets.DECRYPTED_SAVE_SIZE_V21));
            if (Version >= 10 && Version <= 21 && FileBytes.Count != Offsets.DECRYPTED_SAVE_SIZE_V10) throw new InvalidDataException(String.Format("Invalid BaseFile bytes array size. Expected {1} but found {0}. Save failed.", FileBytes.Count, Offsets.DECRYPTED_SAVE_SIZE_V10));
            if(Version < 10 && FileBytes.Count != Offsets.DECRYPTED_SAVE_SIZE_V1) throw new InvalidDataException(String.Format("Invalid BaseFile bytes array size. Expected {1} but found {0}. Save failed.", FileBytes.Count, Offsets.DECRYPTED_SAVE_SIZE_V1));

            List<byte> bytes = FileBytes;

            //Validate
            bytes = ValidatePartnerKeyFlags(bytes);

            //Header
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SteamID ^ VERSION_XOR), 8);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Zeni), 44);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TPMedals), 48);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((int)AccFlags), 76);

            //Characters
            bytes = CaC.WriteAll(bytes, this);

            //Inventory
            bytes = Inventory.Write(bytes);

            //Battle Items
            bytes = BattleItems.Write(bytes);

            //Skills
            bytes = Skills.Write(bytes);

            //HC
            if (DLC5)
            {
                bytes = HeroColosseum.Write(bytes, Version);
            }

            //Xv1 Hero
            bytes = Xv1Hero.Write(bytes);

            //Mentor Customization Flags
            if (DLC6)
            {
                bytes = MentorCustomizationUnlockFlag.Write(MentorCustomizationUnlockFlags, bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS, Offsets.MENTOR_CUSTOMIZATION_COUNT);
            }

            if(Version >= 19)
            {
                bytes = MentorCustomizationUnlockFlag.Write(MentorCustomizationUnlockFlags2, bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS2, Offsets.MENTOR_CUSTOMIZATION_COUNT2);
            }

            if (Version >= 22)
            {
                bytes = MentorCustomizationUnlockFlag.Write(MentorCustomizationUnlockFlags3, bytes, Offsets.MENTOR_CUSTOMIZATION_FLAGS3, Offsets.MENTOR_CUSTOMIZATION_COUNT3);
            }

            //1.15 stuff
            if (DLC11 && ArtworkFlags != null)
                ArtworkFlag.Write(bytes, ArtworkFlags);

            if (DLC11 && MascotFlags != null)
                MascotFlag.Write(bytes, MascotFlags);

            return bytes;
        }

        private static bool isEncrypted(byte[] rawBytes)
        {
            if (BitConverter.ToInt32(rawBytes, 0) != SAV_SIGNATURE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public void UpdateByteArray()
        {
            bytes = FileBytes.ToArray();
        }
        
        internal void SetAccFlag(AccountFlags flag)
        {
            if (!AccFlags.HasFlag(flag))
            {
                AccFlags = flag | AccFlags;
            }
        }

        public void ValidateLevelFlags()
        {
            int maxLvl = 1;
            foreach (var chara in Characters)
            {
                if (!String.IsNullOrWhiteSpace(chara.Name))
                {
                    if (chara.I_172 > maxLvl) maxLvl = chara.I_172;
                }
            }

            //Set account flags
            if (maxLvl > 95)
            {
                SetAccFlag(AccountFlags.Level85Unlock);
                SetAccFlag(AccountFlags.Level90Unlock);
                SetAccFlag(AccountFlags.Level95Unlock);
                SetAccFlag(AccountFlags.Level99Unlock);
            }
            else if (maxLvl > 90)
            {
                SetAccFlag(AccountFlags.Level85Unlock);
                SetAccFlag(AccountFlags.Level90Unlock);
                SetAccFlag(AccountFlags.Level95Unlock);
            }
            else if (maxLvl > 85)
            {
                SetAccFlag(AccountFlags.Level85Unlock);
                SetAccFlag(AccountFlags.Level90Unlock);
            }
            else if (maxLvl > 80)
            {
                SetAccFlag(AccountFlags.Level85Unlock);
            }
        }
        
        public List<byte> ValidatePartnerKeyFlags(List<byte> bytes)
        {
            //Set the flags if the keys are currently in the inventory
            BitArray flag = new BitArray(bytes.GetRange(Offsets.PARTNER_KEY_FLAGS, 4).ToArray());

            if (Inventory.ImportantItems.Any(x => x.I_00 == 13))
                flag[0] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 14))
                flag[1] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 15))
                flag[2] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 16))
                flag[3] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 17))
                flag[4] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 18))
                flag[5] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 19))
                flag[6] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 20))
                flag[7] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 21))
                flag[8] = true;

            if (Inventory.ImportantItems.Any(x => x.I_00 == 22))
                flag[9] = true;

            int num = Utils.ConvertToInt(flag);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(num), Offsets.PARTNER_KEY_FLAGS);

            //Keys added in 1.17
            if (Version >= 22)
            {
                BitArray flag2 = new BitArray(bytes.GetRange(Offsets.PARTNER_KEY_FLAGS2, 4).ToArray());

                if (Inventory.ImportantItems.Any(x => x.I_00 == 23))
                    flag2[0] = true;

                if (Inventory.ImportantItems.Any(x => x.I_00 == 24))
                    flag2[1] = true;

                if (Inventory.ImportantItems.Any(x => x.I_00 == 25))
                    flag2[2] = true;

                if (Inventory.ImportantItems.Any(x => x.I_00 == 26))
                    flag2[3] = true;

                if (Inventory.ImportantItems.Any(x => x.I_00 == 27))
                    flag2[4] = true;

                int num2 = Utils.ConvertToInt(flag2);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(num2), Offsets.PARTNER_KEY_FLAGS2);
            }

            return bytes;
        }
    }

    #region CaC
    public class CaC : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Race
        [YAXDontSerialize]
        public static Dictionary<Race, string> RaceEnumDictionary = new Dictionary<Race, string>()
        {
            { Race.HUM , "Human Male" },
            { Race.HUF , "Human Female" },
            { Race.SYM , "Saiyan Male" },
            { Race.SYF , "Saiyan Female" },
            { Race.FRI , "Frieza Race" },
            { Race.NMC , "Namekian" },
            { Race.MAM , "Majin Male" },
            { Race.MAF , "Majin Female" },
         };

        [YAXDontSerialize]
        public Dictionary<Race, string> RaceList
        {
            get { return RaceEnumDictionary; }
        }

        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Name))
                {
                    return String.Format("Not Used (Slot {1})", Name, Index + 1);
                }
                else
                {
                    return String.Format("{0} (Slot {1})", Name, Index + 1);
                };
            }
        }
        [YAXDontSerialize]
        public int Index { get; set; }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public byte[] I_00 { get; set; } //Size 20
        [YAXAttributeFor("Race")]
        [YAXSerializeAs("value")]
        public Race I_20 { get; set; }
        [YAXAttributeFor("Voice")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }

        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public byte[] I_54 { get; set; } //Size 14
        private string nameValue = null;
        [YAXAttributeForClass]
        public string Name //68, max length 64
        {
            get
            {
                return this.nameValue;
            }

            set
            {
                if (value != this.nameValue)
                {
                    this.nameValue = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        [YAXAttributeFor("I_156")]
        [YAXSerializeAs("value")]
        public int I_156 { get; set; }
        [YAXAttributeFor("I_160")]
        [YAXSerializeAs("value")]
        public int I_160 { get; set; }
        [YAXAttributeFor("I_164")]
        [YAXSerializeAs("value")]
        public int I_164 { get; set; }
        [YAXAttributeFor("I_168")]
        [YAXSerializeAs("value")]
        public int I_168 { get; set; }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public int I_172 { get; set; }
        private int _xpValue = 0;
        [YAXAttributeFor("Experience")]
        [YAXSerializeAs("value")]
        public int I_176
        {
            get
            {
                return this._xpValue;
            }

            set
            {
                if (value != this._xpValue)
                {
                    this._xpValue = value;
                    NotifyPropertyChanged("I_176");
                }
            }
        }
        private int _attPointsValue = 0;
        [YAXAttributeFor("AttributePoints")]
        [YAXSerializeAs("value")]
        public int I_180
        {
            get
            {
                return this._attPointsValue;
            }

            set
            {
                if (value != this._attPointsValue)
                {
                    this._attPointsValue = value;
                    NotifyPropertyChanged("I_180");
                }
            }
        }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("HEA")]
        public int I_184 { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("KI")]
        public int I_188 { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("ATK")]
        public int I_192 { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("STR")]
        public int I_196 { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("BLA")]
        public int I_200 { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("STM")]
        public int I_204 { get; set; }

        private CaCAppearence _appearence = null;
        public CaCAppearence Appearence
        {
            get
            {
                return this._appearence;
            }

            set
            {
                if (value != this._appearence)
                {
                    this._appearence = value;
                    NotifyPropertyChanged("Appearence");
                }
            }
        }

        public ObservableCollection<CacPreset> Presets { get; set; } //Size 8
        public ObservableCollection<MentorProgress> Mentors { get; set; }
        public PlayData PlayData { get; set; }
        public Quests Quests { get; set; }
        public ObservableCollection<MentorCustomization> MentorCustomizations { get; set; }
        [YAXSerializeAs("HeroColosseum")]
        public HeroColosseum HC { get; set; }
        public ObservableCollection<SystemFlag> SystemFlags { get; set; }

        public static ObservableCollection<CaC> ReadAll(List<byte> bytes, byte[] rawBytes, int offset, SAV_File sav)
        {
            ObservableCollection<CaC> CaCs = new ObservableCollection<CaC>();

            for (int i = 0; i < 8; i++)
            {
                CaCs.Add(Read(bytes, rawBytes, offset, i, sav));
                offset += Offsets.CAC_SIZE;
            }

            return CaCs;
        }

        public static CaC Read(List<byte> bytes, byte[] rawBytes, int offset, int idx, SAV_File sav)
        {
            //Checking if DLC data exists
            ObservableCollection<MentorCustomization> mentorCustomizations = null;
            HeroColosseum heroCol = null;

            if (sav.DLC6)
            {
                mentorCustomizations = MentorCustomization.ReadAll(rawBytes, idx, sav.Version);
            }

            if (sav.DLC5)
            {
                heroCol = HeroColosseum.Read(rawBytes, bytes, idx);
            }

            return new CaC()
            {
                Index = idx,
                I_00 = bytes.GetRange(offset, 20).ToArray(),
                I_20 = (Race)BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_54 = bytes.GetRange(offset + 54, 14).ToArray(),
                Name = StringEx.GetString(bytes, offset + 68, false, StringEx.EncodingType.ASCII),
                I_156 = BitConverter.ToInt32(rawBytes, offset + 156),
                I_160 = BitConverter.ToInt32(rawBytes, offset + 160),
                I_164 = BitConverter.ToInt32(rawBytes, offset + 164),
                I_168 = BitConverter.ToInt32(rawBytes, offset + 168),
                I_172 = BitConverter.ToInt32(rawBytes, offset + 172),
                I_176 = BitConverter.ToInt32(rawBytes, offset + 176),
                I_180 = BitConverter.ToInt32(rawBytes, offset + 180),
                I_184 = BitConverter.ToInt32(rawBytes, offset + 184),
                I_188 = BitConverter.ToInt32(rawBytes, offset + 188),
                I_192 = BitConverter.ToInt32(rawBytes, offset + 192),
                I_196 = BitConverter.ToInt32(rawBytes, offset + 196),
                I_200 = BitConverter.ToInt32(rawBytes, offset + 200),
                I_204 = BitConverter.ToInt32(rawBytes, offset + 204),
                Presets = CacPreset.ReadAll(bytes, rawBytes, idx, 8),
                Mentors = MentorProgress.Read(rawBytes, idx),
                PlayData = PlayData.Read(rawBytes, idx),
                Quests = Quests.Read(rawBytes, idx, sav),
                Appearence = CaCAppearence.Read(rawBytes, idx),
                MentorCustomizations = mentorCustomizations,
                HC = heroCol,
                SystemFlags = SystemFlag.LoadAll(bytes, idx)
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, SAV_File sav)
        {
            if (sav.Characters.Count != 8) throw new InvalidDataException("Expected 8 characters.");

            for (int i = 0; i < 8; i++)
            {
                bytes = sav.Characters[i].Write(bytes, i, sav);
            }

            return bytes;
        }

        public List<byte> Write(List<byte> bytes, int charaIdx, SAV_File sav)
        {
            int offset = Offsets.CAC + (Offsets.CAC_SIZE * charaIdx);

            //0-208
            List<byte> basicInfoBytes = new List<byte>();
            Assertion.AssertArraySize(I_00, 20, "Character", "I_00");
            basicInfoBytes.AddRange(I_00);
            basicInfoBytes.AddRange(BitConverter.GetBytes((int)I_20));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_24));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_28));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_32));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_36));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_38));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_40));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_42));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_44));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_46));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_48));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_50));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_52));
            Assertion.AssertArraySize(I_54, 14, "Character", "I_54");
            basicInfoBytes.AddRange(I_54);
            if (Name.Length > 64) throw new InvalidDataException("Name cannot exceed 64 characters.");
            basicInfoBytes.AddRange(Utils.GetStringBytes(Name, 64));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_132));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_136));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_140));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_144));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_148));
            basicInfoBytes.AddRange(BitConverter.GetBytes(Appearence.I_152));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_156));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_160));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_164));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_168));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_172));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_176));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_180));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_184));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_188));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_192));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_196));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_200));
            basicInfoBytes.AddRange(BitConverter.GetBytes(I_204));

            if (basicInfoBytes.Count != 208) throw new InvalidDataException("Character basicInfo invalid size.");
            bytes = Utils.ReplaceRange(bytes, basicInfoBytes.ToArray(), offset);

            bytes = CacPreset.WriteAll(bytes, Presets, charaIdx);
            bytes = MentorProgress.WriteAll(bytes, Mentors, charaIdx);
            bytes = PlayData.Write(bytes, charaIdx);
            bytes = Quests.Write(bytes, charaIdx, sav);
            if (sav.DLC6)
            {
                bytes = MentorCustomization.WriteAll(bytes, MentorCustomizations, charaIdx, sav.Version);
            }
            if (sav.DLC5)
            {
                bytes = HC.Write(bytes, charaIdx);
            }
            bytes = SystemFlag.Write(bytes, SystemFlags, charaIdx);

            return bytes;
        }

        public int TotalUsedAttributePoints()
        {
            int attributePoints = 0;
            attributePoints += I_180;
            attributePoints += I_184;
            attributePoints += I_188;
            attributePoints += I_192;
            attributePoints += I_196;
            attributePoints += I_200;
            attributePoints += I_204;
            return attributePoints;
        }
        
        private bool HasCompletedQuestForMentor(string mentorName)
        {
            foreach (var quest in Quests.MasterQuests)
            {
                if (quest.SubType == mentorName && quest.I_08 == 3) return true;
            }
            return false;
        }

#if SaveEditor
        public void Init()
        {
            InitMentors();
            InitQuests();
            if(MentorCustomizations != null)
            {
                InitMentorCustomizations();
            }
            InitSysFlags();
            if(HC != null)
            {
                HC.InitQuests();
            }
        }

        public void InitMentors()
        {
            //First, validate the mentor count. It should be at least 33.
            //We wont check if its over 33 as we want to be able to support future dlcs that might add a new mentor.
            while (Mentors.Count < 33)
            {
                Mentors.Add(new MentorProgress());
            }

            //Now we parse all mentor entries and give them names
            for (int i = 0; i < Mentors.Count; i++)
            {
                int charaId = MentorProgress.MentorToCharaId(i);

                if (charaId == -2)
                {
                    Mentors[i].DisplayName = String.Format("None");
                }
                else if (charaId != -1)
                {
                    Mentors[i].DisplayName = GeneralInfo.IdManager.GetCharacterName(charaId);
                }
                else
                {
                    Mentors[i].DisplayName = String.Format("Unknown Mentor ({0})", i);
                }
            }
        }

        public void InitMentorCustomizations()
        {
            if (MentorCustomizations == null) return;

            //Now we parse all mentor entries and give them names
            for (int i = 0; i < MentorCustomizations.Count; i++)
            {
                //IDs
                GeneralInfo.IdManager.AddStatPresetID(MentorCustomizations[i].I_02);
                GeneralInfo.IdManager.AddEquipmentID(MentorCustomizations[i].I_04, EquipmentType.SuperSoul);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_48, SkillType.Super, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_52, SkillType.Super, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_56, SkillType.Super, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_60, SkillType.Super, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_64, SkillType.Ultimate, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_68, SkillType.Ultimate, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_72, SkillType.Evasive, true);
                GeneralInfo.IdManager.AddSkillID(MentorCustomizations[i].I_76, SkillType.Awoken, true);

                //Names
                int charaId = MentorProgress.MentorToCharaId(i);

                if (charaId == -2)
                {
                    MentorCustomizations[i].Name = String.Format("None");
                }
                else if (charaId != -1)
                {
                    MentorCustomizations[i].Name = GeneralInfo.IdManager.GetCharacterName(charaId);
                    MentorCustomizations[i].IsVisible = Visibility.Visible;
                }
                else
                {
                    MentorCustomizations[i].Name = String.Format("Unknown Mentor ({0})", i);
                    MentorCustomizations[i].IsVisible = Visibility.Collapsed;
                }
            }
        }

        public void InitQuests()
        {
            Quests.InitQuests();

            if(HC != null)
            {
                HC.InitQuests();
            }
        }

        public void InitSysFlags()
        {
            foreach (var flag in SystemFlags)
            {
                flag.FlagData = GeneralInfo.IdManager.SysFlags.GetFlag(flag.Index);
            }
        }

        public void ValidateSysFlags()
        {
            if (GeneralInfo.IdManager.SysFlags == null) return;
            if (GeneralInfo.IdManager.SysFlags.Flags == null) return;

            foreach (var flag in GeneralInfo.IdManager.SysFlags.Flags)
            {
                if (flag.FlagType != LB_Save_Editor.ID.SysFlagType.Other)
                {
                    if (IsFlagTrue(flag.FlagType, flag.Conditions1))
                    {
                        SystemFlags[flag.Index].Flag = true;
                    }
                    else if (IsFlagTrue(flag.FlagType, flag.Conditions2))
                    {
                        SystemFlags[flag.Index].Flag = true;
                    }
                    else if (flag.ChangeIfSet)
                    {
                        SystemFlags[flag.Index].Flag = false;
                    }
                }
            }

        }

        public void ValidateMentorFlags()
        {
            foreach (var mentor in Mentors)
            {
                if (!string.IsNullOrWhiteSpace(mentor.DisplayName))
                {
                    bool completedQuest = HasCompletedQuestForMentor(mentor.DisplayName);

                    if (completedQuest)
                    {
                        if (!mentor.Flags.HasFlag(MentorFlags.FirstContact))
                        {
                            mentor.Flags = MentorFlags.FirstContact | mentor.Flags;
                        }
                    }
                }
            }
        }
        
        private bool IsFlagTrue(LB_Save_Editor.ID.SysFlagType flagType, List<string> conditions)
        {
            foreach (string s in conditions)
            {
                List<bool> isComplete = new List<bool>();

                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.TPQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.TPQ, s));
                }
                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.TMQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.TMQ, s));
                }
                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.TCQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.TCQ, s));
                }
                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.HLQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.HLQ, s));
                }
                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.OSQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.OSQ, s));
                }
                if (flagType.HasFlag(LB_Save_Editor.ID.SysFlagType.LEQ))
                {
                    isComplete.Add(Quests.IsComplete(QuestType.LEQ, s));
                }

                if (!isComplete.Contains(true)) return false;
            }

            return true;
        }

#endif
    }

    public class CaCAppearence : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private int _height = 0;
        private int _width = 0;

        [YAXAttributeFor("BodySize")]
        [YAXSerializeAs("Height")]
        public int Height
        {
            get
            {
                return this._height;
            }

            set
            {
                if (value != this._height)
                {
                    this._height = value;
                    NotifyPropertyChanged("Height");
                }
            }
        }
        [YAXAttributeFor("BodySize")]
        [YAXSerializeAs("Width")]
        public int Width
        {
            get
            {
                return this._width;
            }

            set
            {
                if (value != this._width)
                {
                    this._width = value;
                    NotifyPropertyChanged("Width");
                }
            }
        }
        [YAXAttributeFor("BodyShape")]
        [YAXSerializeAs("value")]
        [YAXDontSerialize]
        public int I_28
        {
            get
            {
                return GetBcsBodyFromHeightWidth();
            }
            set
            {
                var ret = BcsBodyToHeightWidth(value);
                if (ret != null)
                {
                    Height = ret[0];
                    Width = ret[1];
                }
            }
        }
        [YAXAttributeFor("FaceBase")]
        [YAXSerializeAs("value")]
        public int I_132 { get; set; }
        [YAXAttributeFor("FaceForehead")]
        [YAXSerializeAs("value")]
        public int I_136 { get; set; }
        [YAXAttributeFor("Eyes")]
        [YAXSerializeAs("value")]
        public int I_140 { get; set; }
        [YAXAttributeFor("Nose")]
        [YAXSerializeAs("value")]
        public int I_144 { get; set; }
        [YAXAttributeFor("Ears")]
        [YAXSerializeAs("value")]
        public int I_148 { get; set; }
        [YAXAttributeFor("Hair")]
        [YAXSerializeAs("value")]
        public int I_152 { get; set; }
        [YAXAttributeFor("SkinColor1")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("SkinColor2")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("SkinColor3")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("SkinColor4")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("HairColor")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("EyeColor")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("MakeupColor1")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("MakeupColor2")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("MakeupColor3")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }

        public static CaCAppearence ReadFromExported(byte[] rawBytes)
        {
            int offset = 29696;

            return new CaCAppearence()
            {
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                I_132 = BitConverter.ToInt32(rawBytes, offset + 132),
                I_136 = BitConverter.ToInt32(rawBytes, offset + 136),
                I_140 = BitConverter.ToInt32(rawBytes, offset + 140),
                I_144 = BitConverter.ToInt32(rawBytes, offset + 144),
                I_148 = BitConverter.ToInt32(rawBytes, offset + 148),
                I_152 = BitConverter.ToInt32(rawBytes, offset + 152)
            };
        }


        public static CaCAppearence Read(byte[] rawBytes, int cacIndex)
        {
            int offset = Offsets.CAC + (Offsets.CAC_SIZE * cacIndex);

            return new CaCAppearence()
            {
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                I_132 = BitConverter.ToInt32(rawBytes, offset + 132),
                I_136 = BitConverter.ToInt32(rawBytes, offset + 136),
                I_140 = BitConverter.ToInt32(rawBytes, offset + 140),
                I_144 = BitConverter.ToInt32(rawBytes, offset + 144),
                I_148 = BitConverter.ToInt32(rawBytes, offset + 148),
                I_152 = BitConverter.ToInt32(rawBytes, offset + 152)
            };
        }

        public static CaCAppearence GetEmpty()
        {
            return new CaCAppearence()
            {

            };
        }

        /// <summary>
        /// Returns an array containing the height [0] and width [1] matching with the bcsBody value. 
        /// </summary>
        private static int[] BcsBodyToHeightWidth(int bcsBody)
        {
            switch (bcsBody)
            {
                case -1:
                    return new int[2] { 0, 0 };
                case 0:
                    return new int[2] { 1, 1 };
                case 1:
                    return new int[2] { 1, 2 };
                case 2:
                    return new int[2] { 1, 3 };
                case 3:
                    return new int[2] { 2, 1 };
                case 4:
                    return new int[2] { 2, 2 };
                case 5:
                    return new int[2] { 2, 3 };
                case 6:
                    return new int[2] { 3, 1 };
                case 7:
                    return new int[2] { 3, 2 };
                case 8:
                    return new int[2] { 3, 3 };
                case 9:
                    return new int[2] { 4, 1 };
                case 10:
                    return new int[2] { 4, 2 };
                case 11:
                    return new int[2] { 4, 3 };
                default:
                    return new int[2] { 1, 1 }; //Default to smallest height/width if bcs body is invalid
            }
        }

        private int GetBcsBodyFromHeightWidth()
        {
            switch (Height)
            {
                case 0:
                    return -1;
                case 1:
                    switch (Width)
                    {
                        case 1:
                            return 0;
                        case 2:
                            return 1;
                        case 3:
                            return 2;
                        default:
                            return 0;
                    }
                case 2:
                    switch (Width)
                    {
                        case 1:
                            return 3;
                        case 2:
                            return 4;
                        case 3:
                            return 5;
                        default:
                            return 3;
                    }
                case 3:
                    switch (Width)
                    {
                        case 1:
                            return 6;
                        case 2:
                            return 7;
                        case 3:
                            return 8;
                        default:
                            return 6;
                    }
                case 4:
                    switch (Width)
                    {
                        case 1:
                            return 9;
                        case 2:
                            return 10;
                        case 3:
                            return 11;
                        default:
                            return 9;
                    }
                default:
                    return 0;
            }
        }

    }

    public class CacPreset : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return "Main";
                    default:
                        return String.Format("Preset {0}", index);
                }
            }
        }
        [YAXDontSerialize]
        private int index { get; set; }

        private int topValue = 0;
        private int bottomValue = 0;
        private int glovesValue = 0;
        private int shoesValue = 0;
        private int accessoryValue = 0;
        private int superSoulValue = 0;

        [YAXAttributeFor("Top")]
        [YAXSerializeAs("value")]
        public int I_00
        {
            get
            {
                return this.topValue;
            }
            set
            {
                if (value != this.topValue)
                {
                    this.topValue = value;
                    NotifyPropertyChanged("I_00");
                }
            }
        }
        [YAXAttributeFor("Bottom")]
        [YAXSerializeAs("value")]
        public int I_04
        {
            get
            {
                return this.bottomValue;
            }
            set
            {
                if (value != this.bottomValue)
                {
                    this.bottomValue = value;
                    NotifyPropertyChanged("I_04");
                }
            }
        }
        [YAXAttributeFor("Gloves")]
        [YAXSerializeAs("value")]
        public int I_08
        {
            get
            {
                return this.glovesValue;
            }
            set
            {
                if (value != this.glovesValue)
                {
                    this.glovesValue = value;
                    NotifyPropertyChanged("I_08");
                }
            }
        }
        [YAXAttributeFor("Shoes")]
        [YAXSerializeAs("value")]
        public int I_12
        {
            get
            {
                return this.shoesValue;
            }
            set
            {
                if (value != this.shoesValue)
                {
                    this.shoesValue = value;
                    NotifyPropertyChanged("I_12");
                }
            }
        }
        [YAXAttributeFor("Accessory")]
        [YAXSerializeAs("value")]
        public int I_16
        {
            get
            {
                return this.accessoryValue;
            }
            set
            {
                if (value != this.accessoryValue)
                {
                    this.accessoryValue = value;
                    NotifyPropertyChanged("I_16");
                }
            }
        }
        [YAXAttributeFor("Talisman")]
        [YAXSerializeAs("value")]
        public int I_20
        {
            get
            {
                return this.superSoulValue;
            }
            set
            {
                if (value != this.superSoulValue)
                {
                    this.superSoulValue = value;
                    NotifyPropertyChanged("I_20");
                }
            }
        }
        [YAXSerializeAs("QQBang")]
        private QQ_Bang qqBangValue = null;
        public QQ_Bang I_24 //int
        {
            get
            {
                return this.qqBangValue;
            }

            set
            {
                if (value != this.qqBangValue)
                {
                    this.qqBangValue = value;
                    NotifyPropertyChanged("I_24");
                }
            }
        }
        [YAXAttributeFor("TopColor1")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; }
        [YAXAttributeFor("TopColor2")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("TopColor3")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }
        [YAXAttributeFor("TopColor4")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("BottomColor1")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("BottomColor2")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("BottomColor3")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("BottomColor4")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("GlovesColor1")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("GlovesColor2")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("GlovesColor3")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("GlovesColor4")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("ShoesColor1")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("ShoesColor2")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
        [YAXAttributeFor("ShoesColor3")]
        [YAXSerializeAs("value")]
        public ushort I_56 { get; set; }
        [YAXAttributeFor("ShoesColor4")]
        [YAXSerializeAs("value")]
        public ushort I_58 { get; set; }

        private int super1 = 0;
        private int super2 = 0;
        private int super3 = 0;
        private int super4 = 0;
        private int ultimate1 = 0;
        private int ultimate2 = 0;
        private int evasive = 0;
        private int awoken = 0;

        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("ID1")]
        public int I_60
        {
            get
            {
                return this.super1;
            }
            set
            {
                if (value != this.super1)
                {
                    this.super1 = value;
                    NotifyPropertyChanged("I_60");
                }
            }
        }
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("ID1")]
        public int I_64
        {
            get
            {
                return this.super2;
            }
            set
            {
                if (value != this.super2)
                {
                    this.super2 = value;
                    NotifyPropertyChanged("I_64");
                }
            }
        }
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("ID1")]
        public int I_68
        {
            get
            {
                return this.super3;
            }
            set
            {
                if (value != this.super3)
                {
                    this.super3 = value;
                    NotifyPropertyChanged("I_68");
                }
            }
        }
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("ID1")]
        public int I_72
        {
            get
            {
                return this.super4;
            }
            set
            {
                if (value != this.super4)
                {
                    this.super4 = value;
                    NotifyPropertyChanged("I_72");
                }
            }
        }
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("ID1")]
        public int I_76
        {
            get
            {
                return this.ultimate1;
            }
            set
            {
                if (value != this.ultimate1)
                {
                    this.ultimate1 = value;
                    NotifyPropertyChanged("I_76");
                }
            }
        }
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("ID1")]
        public int I_80
        {
            get
            {
                return this.ultimate2;
            }
            set
            {
                if (value != this.ultimate2)
                {
                    this.ultimate2 = value;
                    NotifyPropertyChanged("I_80");
                }
            }
        }
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("ID1")]
        public int I_84
        {
            get
            {
                return this.evasive;
            }
            set
            {
                if (value != this.evasive)
                {
                    this.evasive = value;
                    NotifyPropertyChanged("I_84");
                }
            }
        }
        [YAXAttributeFor("BlastSkill")]
        [YAXSerializeAs("ID1")]
        public int I_88 { get; set; }
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("ID1")]
        public int I_92
        {
            get
            {
                return this.awoken;
            }
            set
            {
                if (value != this.awoken)
                {
                    this.awoken = value;
                    NotifyPropertyChanged("I_92");
                }
            }
        }


        public static ObservableCollection<CacPreset> ReadAll(List<byte> bytes, byte[] rawBytes, int charaIdx, int count)
        {
            int offset = Offsets.CAC_PRESET + (Offsets.CAC_SIZE * charaIdx);
            ObservableCollection<CacPreset> presets = new ObservableCollection<CacPreset>();

            for (int i = 0; i < count; i++)
            {
                presets.Add(Read(bytes, rawBytes, offset, i));
                offset += 96;
            }

            return presets;
        }

        private static CacPreset Read(List<byte> bytes, byte[] rawBytes, int offset, int idx)
        {
            return new CacPreset()
            {
                index = idx,
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = QQ_Bang.Parse(BitConverter.ToInt32(rawBytes, offset + 24)),
                I_28 = BitConverter.ToUInt16(rawBytes, offset + 28),
                I_30 = BitConverter.ToUInt16(rawBytes, offset + 30),
                I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                I_54 = BitConverter.ToUInt16(rawBytes, offset + 54),
                I_56 = BitConverter.ToUInt16(rawBytes, offset + 56),
                I_58 = BitConverter.ToUInt16(rawBytes, offset + 58),
                I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
                I_64 = BitConverter.ToInt32(rawBytes, offset + 64),
                I_68 = BitConverter.ToInt32(rawBytes, offset + 68),
                I_72 = BitConverter.ToInt32(rawBytes, offset + 72),
                I_76 = BitConverter.ToInt32(rawBytes, offset + 76),
                I_80 = BitConverter.ToInt32(rawBytes, offset + 80),
                I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                I_88 = BitConverter.ToInt32(rawBytes, offset + 88),
                I_92 = BitConverter.ToInt32(rawBytes, offset + 92),
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, ObservableCollection<CacPreset> presets, int charaIdx)
        {
            if (presets.Count != 8) throw new InvalidDataException("Expected 8 CacPresets.");

            int offset = Offsets.CAC_PRESET + (Offsets.CAC_SIZE * charaIdx);

            //Create temp byte list
            List<byte> presetBytes = new List<byte>();

            for (int i = 0; i < 8; i++)
            {
                presetBytes.AddRange(presets[i].Write());
            }

            if (presetBytes.Count != 768) throw new InvalidDataException("Invalid presetBytes size.");

            bytes = Utils.ReplaceRange(bytes, presetBytes.ToArray(), offset);
            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24.Write()));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_34));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_38));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_42));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_46));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_50));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_54));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_58));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(I_88));
            bytes.AddRange(BitConverter.GetBytes(I_92));

            if (bytes.Count != 96) throw new InvalidDataException("CaCPreset invalid size.");

            return bytes;
        }

    }
    
    public class PlayData : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public static Dictionary<TrainingLevel, string> TrainingLevelEnumDictionary = new Dictionary<TrainingLevel, string>()
        {
            { TrainingLevel.Beginner, "Beginner" },
            { TrainingLevel.Intermediate, "Intermediate" },
            { TrainingLevel.Advanced, "Advanced" },
            { TrainingLevel.Kai, "Kai" },
            { TrainingLevel.God, "God" },
            { TrainingLevel.Super, "Super" },
         };

        [YAXDontSerialize]
        public Dictionary<TrainingLevel, string> TrainingLevelList
        {
            get { return TrainingLevelEnumDictionary; }
        }



        [YAXAttributeFor("CurrentMentor")]
        [YAXSerializeAs("value")]
        public byte I_00 { get; set; } //byte
        [YAXDontSerialize]
        private TrainingLevel trainingLevelValue = TrainingLevel.Beginner;
        [YAXAttributeFor("TrainingLevel")]
        [YAXSerializeAs("value")]
        public TrainingLevel I_01 //byte
        {
            get
            {
                return this.trainingLevelValue;
            }

            set
            {
                if (value != this.trainingLevelValue)
                {
                    this.trainingLevelValue = value;
                    //I_02 = (ushort)GetTrainingXp();
                    NotifyPropertyChanged("I_01");
                    NotifyPropertyChanged("I_02");
                }
            }
        }
        [YAXAttributeFor("TrainingXp")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }

        //Unknowns
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_04 { get; set; } //size 35

        [YAXAttributeFor("ExpertMissionsCleared")]
        [YAXSerializeAs("value")]
        public ushort I_74 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_76 { get; set; } //size 14
        [YAXAttributeFor("ExpertEnemiesDefeated")]
        [YAXSerializeAs("value")]
        public ushort I_104 { get; set; }
        [YAXAttributeFor("HighestCombo")]
        [YAXSerializeAs("value")]
        public ushort I_106 { get; set; }
        [YAXAttributeFor("HighestDamage")]
        [YAXSerializeAs("value")]
        public ushort I_108 { get; set; }
        [YAXAttributeFor("AlliesFreedFromMindControl")]
        [YAXSerializeAs("value")]
        public ushort I_110 { get; set; }
        [YAXAttributeFor("WishesMade")]
        [YAXSerializeAs("value")]
        public ushort I_112 { get; set; }
        [YAXAttributeFor("FoodGivenToBuu")]
        [YAXSerializeAs("value")]
        public ushort I_114 { get; set; }
        [YAXAttributeFor("DefendedDragonBalls")]
        [YAXSerializeAs("value")]
        public ushort I_116 { get; set; }
        [YAXAttributeFor("Saviors")]
        [YAXSerializeAs("value")]
        public ushort I_118 { get; set; }
        [YAXAttributeFor("TrainedWithVegeta")]
        [YAXSerializeAs("value")]
        public ushort I_120 { get; set; }
        [YAXAttributeFor("I_122")]
        [YAXSerializeAs("value")]
        public ushort I_122 { get; set; }
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        public ushort I_124 { get; set; }
        [YAXAttributeFor("I_126")]
        [YAXSerializeAs("value")]
        public ushort I_126 { get; set; }
        [YAXAttributeFor("CapsuleCorpProgress")]
        [YAXSerializeAs("value")]
        public byte I_128 { get; set; }
        [YAXAttributeFor("HerculeProgress")]
        [YAXSerializeAs("value")]
        public byte I_129 { get; set; }
        [YAXAttributeFor("GuruProgress")]
        [YAXSerializeAs("value")]
        public byte I_130 { get; set; }
        [YAXAttributeFor("MajinBuuProgress")]
        [YAXSerializeAs("value")]
        public byte I_131 { get; set; }
        [YAXAttributeFor("FriezaSpaceshipProgress")]
        [YAXSerializeAs("value")]
        public byte I_132 { get; set; }
        [YAXAttributeFor("I_133")]
        [YAXSerializeAs("value")]
        public byte I_133 { get; set; }
        [YAXAttributeFor("I_134")]
        [YAXSerializeAs("value")]
        public byte I_134 { get; set; }
        [YAXAttributeFor("I_135")]
        [YAXSerializeAs("value")]
        public byte I_135 { get; set; }

        private int favCharacterValue = 0;
        private int favSuperValue = 0;
        private int favUltimateValue = 0;
        private int favEvasiveValue = 0;
        private int favFinisherValue = 0;

        [YAXAttributeFor("FavoriteCharacter")]
        [YAXSerializeAs("value")]
        public int I_136
        {
            get
            {
                return this.favCharacterValue;
            }
            set
            {
                if (value != this.favCharacterValue)
                {
                    this.favCharacterValue = value;
                    NotifyPropertyChanged("I_136");
                }
            }
        }
        [YAXAttributeFor("FavoriteSuper")]
        [YAXSerializeAs("value")]
        public int I_140
        {
            get
            {
                return this.favSuperValue;
            }
            set
            {
                if (value != this.favSuperValue)
                {
                    this.favSuperValue = value;
                    NotifyPropertyChanged("I_140");
                }
            }
        }
        [YAXAttributeFor("FavoriteUltimate")]
        [YAXSerializeAs("value")]
        public int I_144
        {
            get
            {
                return this.favUltimateValue;
            }
            set
            {
                if (value != this.favUltimateValue)
                {
                    this.favUltimateValue = value;
                    NotifyPropertyChanged("I_144");
                }
            }
        }
        [YAXAttributeFor("FavoriteEvasive")]
        [YAXSerializeAs("value")]
        public int I_148
        {
            get
            {
                return this.favEvasiveValue;
            }
            set
            {
                if (value != this.favEvasiveValue)
                {
                    this.favEvasiveValue = value;
                    NotifyPropertyChanged("I_148");
                }
            }
        }
        [YAXAttributeFor("FavoriteFinisher")]
        [YAXSerializeAs("value")]
        public int I_152
        {
            get
            {
                return this.favFinisherValue;
            }
            set
            {
                if (value != this.favFinisherValue)
                {
                    this.favFinisherValue = value;
                    NotifyPropertyChanged("I_152");
                }
            }
        }
        [YAXAttributeFor("I_156")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_156 { get; set; } //size 22
        [YAXAttributeFor("FriezaFaction")]
        [YAXSerializeAs("value")]
        public int I_200 { get; set; } //byte
        [YAXAttributeFor("KiSentToAllies")]
        [YAXSerializeAs("value")]
        public int I_204 { get; set; }

        public static PlayData Read(byte[] rawBytes, int charaIdx)
        {
            int offset = Offsets.PLAYDATA + (Offsets.CAC_SIZE * charaIdx);

            return new PlayData()
            {
                I_00 = rawBytes[offset],
                I_01 = (TrainingLevel)rawBytes[offset + 1],
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 4, 35),
                I_74 = BitConverter.ToUInt16(rawBytes, offset + 74),
                I_76 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 76, 14),
                I_104 = BitConverter.ToUInt16(rawBytes, offset + 104),
                I_106 = BitConverter.ToUInt16(rawBytes, offset + 106),
                I_108 = BitConverter.ToUInt16(rawBytes, offset + 108),
                I_110 = BitConverter.ToUInt16(rawBytes, offset + 110),
                I_112 = BitConverter.ToUInt16(rawBytes, offset + 112),
                I_114 = BitConverter.ToUInt16(rawBytes, offset + 114),
                I_116 = BitConverter.ToUInt16(rawBytes, offset + 116),
                I_118 = BitConverter.ToUInt16(rawBytes, offset + 118),
                I_120 = BitConverter.ToUInt16(rawBytes, offset + 120),
                I_122 = BitConverter.ToUInt16(rawBytes, offset + 122),
                I_124 = BitConverter.ToUInt16(rawBytes, offset + 124),
                I_126 = BitConverter.ToUInt16(rawBytes, offset + 126),
                I_128 = rawBytes[offset + 128],
                I_129 = rawBytes[offset + 129],
                I_130 = rawBytes[offset + 130],
                I_131 = rawBytes[offset + 131],
                I_132 = rawBytes[offset + 132],
                I_133 = rawBytes[offset + 133],
                I_134 = rawBytes[offset + 134],
                I_135 = rawBytes[offset + 135],
                I_136 = BitConverter.ToInt32(rawBytes, offset + 136),
                I_140 = BitConverter.ToInt32(rawBytes, offset + 140),
                I_144 = BitConverter.ToInt32(rawBytes, offset + 144),
                I_148 = BitConverter.ToInt32(rawBytes, offset + 148),
                I_152 = BitConverter.ToInt32(rawBytes, offset + 152),
                I_156 = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 156, 22),
                I_200 = BitConverter.ToInt32(rawBytes, offset + 200),
                I_204 = BitConverter.ToInt32(rawBytes, offset + 204),
            };
        }

        public List<byte> Write(List<byte> bytes, int charaIdx)
        {
            int offset = Offsets.PLAYDATA + (Offsets.CAC_SIZE * charaIdx);

            //Write the data into a 208 byte list, and then insert it into bytes
            List<byte> playData = new List<byte>();

            playData.Add(I_00);
            playData.Add((byte)I_01);
            playData.AddRange(BitConverter.GetBytes(I_02));
            Assertion.AssertArraySize(I_04, 35, "PlayData", "I_04");
            playData.AddRange(BitConverter_Ex.GetBytes(I_04));
            playData.AddRange(BitConverter.GetBytes(I_74));
            Assertion.AssertArraySize(I_76, 14, "PlayData", "I_76");
            playData.AddRange(BitConverter_Ex.GetBytes(I_76));
            playData.AddRange(BitConverter.GetBytes(I_104));
            playData.AddRange(BitConverter.GetBytes(I_106));
            playData.AddRange(BitConverter.GetBytes(I_108));
            playData.AddRange(BitConverter.GetBytes(I_110));
            playData.AddRange(BitConverter.GetBytes(I_112));
            playData.AddRange(BitConverter.GetBytes(I_114));
            playData.AddRange(BitConverter.GetBytes(I_116));
            playData.AddRange(BitConverter.GetBytes(I_118));
            playData.AddRange(BitConverter.GetBytes(I_120));
            playData.AddRange(BitConverter.GetBytes(I_122));
            playData.AddRange(BitConverter.GetBytes(I_124));
            playData.AddRange(BitConverter.GetBytes(I_126));
            playData.Add(I_128);
            playData.Add(I_129);
            playData.Add(I_130);
            playData.Add(I_131);
            playData.Add(I_132);
            playData.Add(I_133);
            playData.Add(I_134);
            playData.Add(I_135);
            playData.AddRange(BitConverter.GetBytes(I_136));
            playData.AddRange(BitConverter.GetBytes(I_140));
            playData.AddRange(BitConverter.GetBytes(I_144));
            playData.AddRange(BitConverter.GetBytes(I_148));
            playData.AddRange(BitConverter.GetBytes(I_152));
            Assertion.AssertArraySize(I_156, 22, "PlayData", "I_156");
            playData.AddRange(BitConverter_Ex.GetBytes(I_156));
            playData.AddRange(BitConverter.GetBytes(I_200));
            playData.AddRange(BitConverter.GetBytes(I_204));

            if (playData.Count != 208) throw new Exception("PlayData is wrong size.");
            bytes = Utils.ReplaceRange(bytes, playData.ToArray(), offset);

            return bytes;
        }

        public int GetTrainingXp()
        {
            //Gets the correct amount of training Xp for the current level

            switch (I_01)
            {
                case TrainingLevel.Beginner:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.BEGINNER_MIN, TrainingXpAmount.BEGINNER_MAX);
                case TrainingLevel.Intermediate:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.INTERMEDIATE_MIN, TrainingXpAmount.INTERMEDIATE_MAX);
                case TrainingLevel.Advanced:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.ADVANCED_MIN, TrainingXpAmount.ADVANCED_MAX);
                case TrainingLevel.Kai:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.KAI_MIN, TrainingXpAmount.KAI_MAX);
                case TrainingLevel.God:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.GOD_MIN, TrainingXpAmount.GOD_MAX);
                case TrainingLevel.Super:
                    return GetTrainingXp_2(I_02, TrainingXpAmount.SUPER_MIN, TrainingXpAmount.SUPER_MAX);
                default:
                    return 0;
            }
        }

        private static int GetTrainingXp_2(int _I_02, int min, int max)
        {
            if (_I_02 >= min && _I_02 <= max)
            {
                return _I_02;
            }
            else
            {
                return (_I_02 < min) ? min : max;
            }
        }
    }

#endregion

#region Inventory
    public class Inventory : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public List<EquipmentFlag> TopFlags { get; set; }
        public List<EquipmentFlag> BottomFlags { get; set; }
        public List<EquipmentFlag> GlovesFlags { get; set; }
        public List<EquipmentFlag> ShoesFlags { get; set; }
        public List<EquipmentFlag> AccessoryFlags { get; set; }
        public List<EquipmentFlag> SuperSoulFlags { get; set; }
        public List<EquipmentFlag> MixFlags { get; set; }
        public List<EquipmentFlag> ImportantFlags { get; set; }
        public List<EquipmentFlag> CapsuleFlags { get; set; }

        private ObservableCollection<InventoryItem> _tops = null;
        private ObservableCollection<InventoryItem> _bottoms = null;
        private ObservableCollection<InventoryItem> _gloves = null;
        private ObservableCollection<InventoryItem> _shoes = null;
        private ObservableCollection<InventoryItem> _accessories = null;
        private ObservableCollection<InventoryItem> _superSouls = null;
        private ObservableCollection<InventoryItem> _mixItems = null;
        private ObservableCollection<InventoryItem> _importantItems = null;
        private ObservableCollection<InventoryItem> _capsules = null;
        private ObservableCollection<QQBangItem> _qqBangs = null;

        public ObservableCollection<InventoryItem> Tops
        {
            get
            {
                return this._tops;
            }

            set
            {
                if (value != this._tops)
                {
                    this._tops = value;
                    NotifyPropertyChanged("Tops");
                }
            }
        }
        public ObservableCollection<InventoryItem> Bottoms
        {
            get
            {
                return this._bottoms;
            }

            set
            {
                if (value != this._bottoms)
                {
                    this._bottoms = value;
                    NotifyPropertyChanged("Bottoms");
                }
            }
        }
        public ObservableCollection<InventoryItem> Gloves
        {
            get
            {
                return this._gloves;
            }

            set
            {
                if (value != this._gloves)
                {
                    this._gloves = value;
                    NotifyPropertyChanged("Gloves");
                }
            }
        }
        public ObservableCollection<InventoryItem> Shoes
        {
            get
            {
                return this._shoes;
            }

            set
            {
                if (value != this._shoes)
                {
                    this._shoes = value;
                    NotifyPropertyChanged("Shoes");
                }
            }
        }
        public ObservableCollection<InventoryItem> Accessories
        {
            get
            {
                return this._accessories;
            }

            set
            {
                if (value != this._accessories)
                {
                    this._accessories = value;
                    NotifyPropertyChanged("Accessories");
                }
            }
        }
        public ObservableCollection<InventoryItem> SuperSouls
        {
            get
            {
                return this._superSouls;
            }

            set
            {
                if (value != this._superSouls)
                {
                    this._superSouls = value;
                    NotifyPropertyChanged("SuperSouls");
                }
            }
        }
        public ObservableCollection<InventoryItem> MixItems
        {
            get
            {
                return this._mixItems;
            }

            set
            {
                if (value != this._mixItems)
                {
                    this._mixItems = value;
                    NotifyPropertyChanged("MixItems");
                }
            }
        }
        public ObservableCollection<InventoryItem> ImportantItems
        {
            get
            {
                return this._importantItems;
            }

            set
            {
                if (value != this._importantItems)
                {
                    this._importantItems = value;
                    NotifyPropertyChanged("ImportantItems");
                }
            }
        }
        public ObservableCollection<InventoryItem> Capsules
        {
            get
            {
                return this._capsules;
            }

            set
            {
                if (value != this._capsules)
                {
                    this._capsules = value;
                    NotifyPropertyChanged("Capsules");
                }
            }
        }
        public ObservableCollection<QQBangItem> QQBangs
        {
            get
            {
                return this._qqBangs;
            }

            set
            {
                if (value != this._qqBangs)
                {
                    this._qqBangs = value;
                    NotifyPropertyChanged("QQBangs");
                }
            }
        }


        //Counts
        [YAXDontSerialize]
        public string TopsCount
        {
            get
            {
                if (Tops == null) return String.Format("0/{0}", Offsets.INVENTORY_TOP_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_TOP_MAX, Tops.Count);
            }
        }
        [YAXDontSerialize]
        public string BottomsCount
        {
            get
            {
                if (Bottoms == null) return String.Format("0/{0}", Offsets.INVENTORY_BOTTOM_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_BOTTOM_MAX, Bottoms.Count);
            }
        }
        [YAXDontSerialize]
        public string GlovesCount
        {
            get
            {
                if (Gloves == null) return String.Format("0/{0}", Offsets.INVENTORY_GLOVES_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_GLOVES_MAX, Gloves.Count);
            }
        }
        [YAXDontSerialize]
        public string ShoesCount
        {
            get
            {
                if (Shoes == null) return String.Format("0/{0}", Offsets.INVENTORY_SHOES_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_SHOES_MAX, Shoes.Count);
            }
        }
        [YAXDontSerialize]
        public string AccessoryCount
        {
            get
            {
                if (Accessories == null) return String.Format("0/{0}", Offsets.INVENTORY_ACCESSORY_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_ACCESSORY_MAX, Accessories.Count);
            }
        }
        [YAXDontSerialize]
        public string SuperSoulCount
        {
            get
            {
                if (SuperSouls == null) return String.Format("0/{0}", Offsets.INVENTORY_SUPERSOUL_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_SUPERSOUL_MAX, SuperSouls.Count);
            }
        }
        [YAXDontSerialize]
        public string MixItemCount
        {
            get
            {
                if (MixItems == null) return String.Format("0/{0}", Offsets.INVENTORY_MIXITEM_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_MIXITEM_MAX, MixItems.Count);
            }
        }
        [YAXDontSerialize]
        public string ImportantItemCount
        {
            get
            {
                if (ImportantItems == null) return String.Format("0/{0}", Offsets.INVENTORY_IMPORTANT_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_IMPORTANT_MAX, ImportantItems.Count);
            }
        }
        [YAXDontSerialize]
        public string CapsuleCount
        {
            get
            {
                if (Capsules == null) return String.Format("0/{0}", Offsets.INVENTORY_CAPSULE_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_CAPSULE_MAX, Capsules.Count);
            }
        }
        [YAXDontSerialize]
        public string QQBangCount
        {
            get
            {
                if (QQBangs == null) return String.Format("0/{0}", Offsets.INVENTORY_QQBANG_MAX);
                return String.Format("{1}/{0}", Offsets.INVENTORY_QQBANG_MAX, QQBangs.Count);
            }
        }

        public void UpdateFlags()
        {
            TopFlags = UpdateFlags(TopFlags, Tops);
            BottomFlags = UpdateFlags(BottomFlags, Bottoms);
            GlovesFlags = UpdateFlags(GlovesFlags, Gloves);
            ShoesFlags = UpdateFlags(ShoesFlags, Shoes);
            AccessoryFlags = UpdateFlags(AccessoryFlags, Accessories);
            SuperSoulFlags = UpdateFlags(SuperSoulFlags, SuperSouls);
            MixFlags = UpdateFlags(MixFlags, MixItems);
            ImportantFlags = UpdateFlags(ImportantFlags, ImportantItems);
            CapsuleFlags = UpdateFlags(CapsuleFlags, Capsules);
        }

        private List<EquipmentFlag> UpdateFlags(List<EquipmentFlag> flags, ObservableCollection<InventoryItem> items)
        {
            for(int i = 0; i < flags.Count; i++)
            {
                if(InventoryItem.Exists(items, i))
                {
                    flags[i].Acquired = true;
                }
            }

            return flags;
        }

        public static ObservableCollection<InventoryItem> ValidateItems(ObservableCollection<InventoryItem> items)
        {
            //Remove nulls
            List<InventoryItem> toRemove = new List<InventoryItem>();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].I_00 == -1) toRemove.Add(items[i]);
            }

            for (int a = 0; a < toRemove.Count; a++)
            {
                items.Remove(toRemove[a]);
            }

            return items;
        }

        public static Inventory Read(byte[] rawBytes, List<byte> bytes)
        {
            return new Inventory()
            {
                Tops = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_TOP, Offsets.INVENTORY_TOP_MAX),
                Bottoms = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_BOTTOM, Offsets.INVENTORY_BOTTOM_MAX),
                Gloves = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_GLOVES, Offsets.INVENTORY_GLOVES_MAX),
                Shoes = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_SHOES, Offsets.INVENTORY_SHOES_MAX),
                Accessories = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_ACCESSORIES, Offsets.INVENTORY_ACCESSORY_MAX),
                SuperSouls = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_SUPERSOUL, Offsets.INVENTORY_SUPERSOUL_MAX),
                MixItems = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_MIXITEMS, Offsets.INVENTORY_MIXITEM_MAX),
                ImportantItems = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_IMPORTANTITEMS, Offsets.INVENTORY_IMPORTANT_MAX),
                Capsules = InventoryItem.ReadItems(rawBytes, Offsets.INVENTORY_CAPSULES, Offsets.INVENTORY_CAPSULE_MAX),
                QQBangs = QQBangItem.ReadItems(rawBytes, Offsets.INVENTORY_QQBANGS, Offsets.INVENTORY_QQBANG_MAX),
                TopFlags = EquipmentFlag.Load(bytes, EquipmentType.Top),
                BottomFlags = EquipmentFlag.Load(bytes, EquipmentType.Bottom),
                GlovesFlags = EquipmentFlag.Load(bytes, EquipmentType.Gloves),
                ShoesFlags = EquipmentFlag.Load(bytes, EquipmentType.Shoes),
                AccessoryFlags = EquipmentFlag.Load(bytes, EquipmentType.Accessory),
                SuperSoulFlags = EquipmentFlag.Load(bytes, EquipmentType.SuperSoul),
                MixFlags = EquipmentFlag.Load(bytes, EquipmentType.MixItem),
                ImportantFlags = EquipmentFlag.Load(bytes, EquipmentType.ImportantItem),
                CapsuleFlags = EquipmentFlag.Load(bytes, EquipmentType.Capsule),
            };
        }

        public List<byte> Write(List<byte> bytes)
        {
            Tops = ValidateItems(Tops);
            Bottoms = ValidateItems(Bottoms);
            Gloves = ValidateItems(Gloves);
            Shoes = ValidateItems(Shoes);
            Accessories = ValidateItems(Accessories);
            SuperSouls = ValidateItems(SuperSouls);
            MixItems = ValidateItems(MixItems);
            ImportantItems = ValidateItems(ImportantItems);
            Capsules = ValidateItems(Capsules);

            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Tops, Offsets.INVENTORY_TOP_MAX).ToArray(), Offsets.INVENTORY_TOP);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Bottoms, Offsets.INVENTORY_BOTTOM_MAX).ToArray(), Offsets.INVENTORY_BOTTOM);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Gloves, Offsets.INVENTORY_GLOVES_MAX).ToArray(), Offsets.INVENTORY_GLOVES);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Shoes, Offsets.INVENTORY_SHOES_MAX).ToArray(), Offsets.INVENTORY_SHOES);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Accessories, Offsets.INVENTORY_ACCESSORY_MAX).ToArray(), Offsets.INVENTORY_ACCESSORIES);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(SuperSouls, Offsets.INVENTORY_SUPERSOUL_MAX).ToArray(), Offsets.INVENTORY_SUPERSOUL);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(MixItems, Offsets.INVENTORY_MIXITEM_MAX).ToArray(), Offsets.INVENTORY_MIXITEMS);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(ImportantItems, Offsets.INVENTORY_IMPORTANT_MAX).ToArray(), Offsets.INVENTORY_IMPORTANTITEMS);
            bytes = Utils.ReplaceRange(bytes, InventoryItem.WriteAll(Capsules, Offsets.INVENTORY_CAPSULE_MAX).ToArray(), Offsets.INVENTORY_CAPSULES);
            bytes = Utils.ReplaceRange(bytes, QQBangItem.WriteAll(QQBangs, Offsets.INVENTORY_QQBANG_MAX).ToArray(), Offsets.INVENTORY_QQBANGS);

            bytes = EquipmentFlag.Write(bytes, TopFlags, EquipmentType.Top);
            bytes = EquipmentFlag.Write(bytes, BottomFlags, EquipmentType.Bottom);
            bytes = EquipmentFlag.Write(bytes, GlovesFlags, EquipmentType.Gloves);
            bytes = EquipmentFlag.Write(bytes, ShoesFlags, EquipmentType.Shoes);
            bytes = EquipmentFlag.Write(bytes, AccessoryFlags, EquipmentType.Accessory);
            bytes = EquipmentFlag.Write(bytes, SuperSoulFlags, EquipmentType.SuperSoul);
            bytes = EquipmentFlag.Write(bytes, MixFlags, EquipmentType.MixItem);
            bytes = EquipmentFlag.Write(bytes, ImportantFlags, EquipmentType.ImportantItem);
            bytes = EquipmentFlag.Write(bytes, CapsuleFlags, EquipmentType.Capsule);

            return bytes;
        }
        

        public InventoryItem GetItem(int ID, EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.Top:
                    return Tops.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.Bottom:
                    return Bottoms.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.Gloves:
                    return Gloves.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.Shoes:
                    return Shoes.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.Accessory:
                    return Accessories.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.SuperSoul:
                    return SuperSouls.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.MixItem:
                    return MixItems.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.ImportantItem:
                    return ImportantItems.FirstOrDefault(a => a.I_00 == ID);
                case EquipmentType.Capsule:
                    return Capsules.FirstOrDefault(a => a.I_00 == ID);
                default:
                    return null;
            }
        }

        public static byte GetInventoryTypeValue(EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.Top:
                case EquipmentType.Bottom:
                case EquipmentType.Gloves:
                case EquipmentType.Shoes:
                case EquipmentType.Accessory:
                case EquipmentType.SuperSoul:
                case EquipmentType.MixItem:
                case EquipmentType.ImportantItem:
                case EquipmentType.Capsule:
                case EquipmentType.QQBang:
                    return (byte)equipmentType;
                default:
                    return 255;
            }

        }

        public QQBangItem GetQQBang(string name)
        {
            return QQBangs.FirstOrDefault(a => a.Name == name);
        }

        
        //Events
        public void RegisterEvents()
        {
            Tops.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Bottoms.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Gloves.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Shoes.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Accessories.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            SuperSouls.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            MixItems.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            ImportantItems.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Capsules.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            QQBangs.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }
        
        public void UnregisterEvents()
        {
            Tops.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Bottoms.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Gloves.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Shoes.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Accessories.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            SuperSouls.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            MixItems.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            ImportantItems.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            Capsules.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
            QQBangs.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }
        
        internal void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TopsCount");
            NotifyPropertyChanged("BottomsCount");
            NotifyPropertyChanged("GlovesCount");
            NotifyPropertyChanged("ShoesCount");
            NotifyPropertyChanged("AccessoryCount");
            NotifyPropertyChanged("SuperSoulCount");
            NotifyPropertyChanged("MixItemCount");
            NotifyPropertyChanged("ImportantItemCount");
            NotifyPropertyChanged("CapsuleCount");
            NotifyPropertyChanged("QQBangCount");
        }
        
    }

    public class InventoryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public EquipmentType equipmentType
        {
            get
            {
                switch (I_04)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        return (EquipmentType)I_04;
                    default:
                        return EquipmentType.QQBang;
                }
            }
        }
        private string _nameValue = String.Empty;
        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return this._nameValue;
            }

            set
            {
                if (value != this._nameValue)
                {
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        //int newId = GeneralInfo.IdManager.GetEquipmentID(value, equipmentType);
                        //if(newId != -1)
                        //{
                        this._nameValue = value;
                        HadName = true;
                        //   I_00 = newId;
                        //}
                    }
                    NotifyPropertyChanged("Name");
                }
            }
        }
        public bool HadName = false;

        private int _idValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00
        {
            get
            {
                return this._idValue;
            }

            set
            {
                if (value != this._idValue)
                {
#if SaveEditor
                    if (GeneralInfo.NamesLoaded && HadName)
                    {
                        Name = GeneralInfo.IdManager.TryGetEquipmentName(value, equipmentType);
                    }
#endif
                    this._idValue = value;
                    NotifyPropertyChanged("I_00");

                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public byte I_04 { get; set; }
        private byte _quantityValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Quantity")]
        public byte I_05
        {
            get
            {
                return this._quantityValue;
            }

            set
            {
                if (value != this._quantityValue)
                {
                    this._quantityValue = value;
                    NotifyPropertyChanged("I_05");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        public byte I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_07")]
        public byte I_07 { get; set; }

        public static ObservableCollection<InventoryItem> ReadItems(byte[] rawBytes, int offset, int maxCount)
        {
            ObservableCollection<InventoryItem> items = new ObservableCollection<InventoryItem>();

            for (int i = 0; i < maxCount; i++)
            {
                if (BitConverter.ToInt32(rawBytes, offset + 0) != -1 && rawBytes[offset + 4] != 255)
                {
                    items.Add(ReadItem(rawBytes, offset));
                }
                offset += 8;
            }

            return items;
        }

        public static InventoryItem ReadItem(byte[] rawBytes, int offset)
        {
            return new InventoryItem()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = rawBytes[offset + 4],
                I_05 = rawBytes[offset + 5],
                I_06 = rawBytes[offset + 6],
                I_07 = rawBytes[offset + 7]
            };
        }

        public static List<byte> WriteAll(ObservableCollection<InventoryItem> inventory, int count)
        {
            List<byte> bytes = new List<byte>();

            //Create skill list
            for (int i = 0; i < inventory.Count; i++)
            {
                bytes.AddRange(inventory[i].Write());
            }

            //Finish the list by filling in empty slots
            int emptySlots = count - inventory.Count;

            for (int i = 0; i < emptySlots; i++)
            {
                bytes.AddRange(NullEntry());
            }

            if (bytes.Count / 8 != count) throw new InvalidDataException("Invalid inventory data size!");
            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.Add(I_04);
            bytes.Add(I_05);
            bytes.Add(I_06);
            bytes.Add(I_07);

            if (bytes.Count != 8) throw new InvalidDataException("InventoryItem is supposed to be 8 bytes.");
            return bytes;
        }

        public static List<byte> NullEntry()
        {
            return new List<byte> { 255, 255, 255, 255, 255, 0, 0, 0 };
        }

        public static bool Exists(ObservableCollection<InventoryItem> items, int ID)
        {
            return items.Any(a => a.I_00 == ID);
        }

    }

    public class QQBangItem : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public string Name { get
            {
                if (QQBang != null) return QQBang.ToString();
                return "Unknown QQ Bang";
            } }

        private QQ_Bang _qqBangValue = null;
        public QQ_Bang QQBang
        {
            get
            {
                return this._qqBangValue;
            }

            set
            {
                if (value != this._qqBangValue)
                {
                    this._qqBangValue = value;
                    NotifyPropertyChanged("QQBang");
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public byte I_04 { get; set; }
        private byte _quantityValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Quantity")]
        public byte I_05
        {
            get
            {
                return this._quantityValue;
            }

            set
            {
                if (value != this._quantityValue)
                {
                    this._quantityValue = value;
                    NotifyPropertyChanged("I_05");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        public byte I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_07")]
        public byte I_07 { get; set; }

        public static ObservableCollection<QQBangItem> ReadItems(byte[] rawBytes, int offset, int maxCount)
        {
            ObservableCollection<QQBangItem> items = new ObservableCollection<QQBangItem>();

            for (int i = 0; i < maxCount; i++)
            {
                if (BitConverter.ToInt32(rawBytes, offset + 0) != -1 && rawBytes[offset + 4] != 255)
                {
                    items.Add(ReadItem(rawBytes, offset));
                }
                offset += 8;
            }

            return items;
        }

        public static QQBangItem ReadItem(byte[] rawBytes, int offset)
        {

            return new QQBangItem()
            {
                QQBang = QQ_Bang.Parse(rawBytes, offset),
                I_04 = rawBytes[offset + 4],
                I_05 = rawBytes[offset + 5],
                I_06 = rawBytes[offset + 6],
                I_07 = rawBytes[offset + 7]
            };
        }

        public static List<byte> WriteAll(ObservableCollection<QQBangItem> inventory, int count)
        {
            List<byte> bytes = new List<byte>();

            //Create skill list
            for (int i = 0; i < inventory.Count; i++)
            {
                bytes.AddRange(inventory[i].Write());
            }

            //Finish the list by filling in empty slots
            int emptySlots = count - inventory.Count;

            for (int i = 0; i < emptySlots; i++)
            {
                bytes.AddRange(NullEntry());
            }

            if (bytes.Count / 8 != count) throw new InvalidDataException("Invalid inventory (QQBang) data size!");
            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(QQBang.Write()));
            bytes.Add(I_04);
            bytes.Add(I_05);
            bytes.Add(I_06);
            bytes.Add(I_07);

            if (bytes.Count != 8) throw new InvalidDataException("QQBangItem is supposed to be 8 bytes.");
            return bytes;
        }

        public static List<byte> NullEntry()
        {
            return new List<byte> { 255, 255, 255, 255, 255, 0, 0, 0 };
        }



    }

    public class QQ_Bang
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("HEA")]
        public int I_00_a { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("KI")]
        public int I_00_b { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("STM")]
        public int I_01_a { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ATK")]
        public int I_01_b { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("STR")]
        public int I_02_a { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("BLA")]
        public int I_02_b { get; set; }

        public static QQ_Bang Parse(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return new QQ_Bang()
            {
                I_00_a = Int4Converter.ToInt4(bytes[0])[0] - 5,
                I_00_b = Int4Converter.ToInt4(bytes[0])[1] - 5,
                I_01_a = Int4Converter.ToInt4(bytes[1])[0] - 5,
                I_01_b = Int4Converter.ToInt4(bytes[1])[1] - 5,
                I_02_a = Int4Converter.ToInt4(bytes[2])[0] - 5,
                I_02_b = Int4Converter.ToInt4(bytes[2])[1] - 5,
            };
        }

        public static QQ_Bang Parse(byte[] rawBytes, int offset)
        {
            if (BitConverter.ToInt32(rawBytes, offset) == -1) return new QQ_Bang() { I_00_a = -1, I_00_b = -1, I_01_a = -1, I_01_b = -1, I_02_a = -1, I_02_b = -1, };

            return new QQ_Bang()
            {
                I_00_a = Int4Converter.ToInt4(rawBytes[offset + 0])[0] - 5,
                I_00_b = Int4Converter.ToInt4(rawBytes[offset + 0])[1] - 5,
                I_01_a = Int4Converter.ToInt4(rawBytes[offset + 1])[0] - 5,
                I_01_b = Int4Converter.ToInt4(rawBytes[offset + 1])[1] - 5,
                I_02_a = Int4Converter.ToInt4(rawBytes[offset + 2])[0] - 5,
                I_02_b = Int4Converter.ToInt4(rawBytes[offset + 2])[1] - 5,
            };
        }

        public int Write()
        {
            if (IsNull()) return -1;

            byte _I_00 = Int4Converter.GetByte((byte)(I_00_a + 5), (byte)(I_00_b + 5));
            byte _I_01 = Int4Converter.GetByte((byte)(I_01_a + 5), (byte)(I_01_b + 5));
            byte _I_02 = Int4Converter.GetByte((byte)(I_02_a + 5), (byte)(I_02_b + 5));

            return BitConverter.ToInt32(new byte[4] { _I_00, _I_01, _I_02, 0 }, 0);
        }

        public override string ToString()
        {
            if (IsNull()) return "---";

            string _I_00_a = (I_00_a >= 0) ? String.Format("+{0}", I_00_a) : I_00_a.ToString();
            string _I_00_b = (I_00_b >= 0) ? String.Format("+{0}", I_00_b) : I_00_b.ToString();
            string _I_01_a = (I_01_a >= 0) ? String.Format("+{0}", I_01_a) : I_01_a.ToString();
            string _I_01_b = (I_01_b >= 0) ? String.Format("+{0}", I_01_b) : I_01_b.ToString();
            string _I_02_a = (I_02_a >= 0) ? String.Format("+{0}", I_02_a) : I_02_a.ToString();
            string _I_02_b = (I_02_b >= 0) ? String.Format("+{0}", I_02_b) : I_02_b.ToString();

            return String.Format("{6} {0} {1} {2} {3} {4} {5}", _I_00_a, _I_00_b, _I_01_a, _I_01_b, _I_02_a, _I_02_b, GetLevel());
        }

        public string GetLevel()
        {
            int totalPositive = 0;

            if (I_00_a > 0) totalPositive += I_00_a;
            if (I_00_b > 0) totalPositive += I_00_b;
            if (I_01_a > 0) totalPositive += I_01_a;
            if (I_01_b > 0) totalPositive += I_01_b;
            if (I_02_a > 0) totalPositive += I_02_a;
            if (I_02_b > 0) totalPositive += I_02_b;

            if (totalPositive >= 0 && totalPositive <= 3)
            {
                return "Lv1";
            }
            else if (totalPositive >= 4 && totalPositive <= 6)
            {
                return "Lv2";
            }
            else if (totalPositive >= 7 && totalPositive <= 10)
            {
                return "Lv3";
            }
            else if (totalPositive >= 11 && totalPositive <= 14)
            {
                return "Lv4";
            }
            else if (totalPositive >= 15)
            {
                return "Lv5";
            }
            else
            {
                return "Unknown Level";
            }

        }

        public bool IsNull()
        {
            //If any of the values are outside of the usual QQ Bang range then it is considered null.
            if (I_00_a < -5 || I_00_a > 5) return true;
            if (I_00_b < -5 || I_00_b > 5) return true;
            if (I_01_a < -5 || I_01_a > 5) return true;
            if (I_01_b < -5 || I_01_b > 5) return true;
            if (I_02_a < -5 || I_02_a > 5) return true;
            if (I_02_b < -5 || I_02_b > 5) return true;
            return false;
        }

        public static QQ_Bang GetNullQQBang()
        {
            return new QQ_Bang()
            {
                I_00_a = -6,
                I_00_b = -6,
                I_01_a = -6,
                I_01_b = -6,
                I_02_a = -6,
                I_02_b = -6
            };
        }

        public QQ_Bang Clone()
        {
            return new QQ_Bang()
            {
                I_00_a = I_00_a,
                I_00_b = I_00_b,
                I_01_a = I_01_a,
                I_01_b = I_01_b,
                I_02_a = I_02_a,
                I_02_b = I_02_b
            };
        }

    }

    public class BattleItems
    {
        public InventoryItem BattleItem1 { get; set; }
        public InventoryItem BattleItem2 { get; set; }
        public InventoryItem BattleItem3 { get; set; }
        public InventoryItem BattleItem4 { get; set; }

        public static BattleItems Read(byte[] bytes)
        {
            return new BattleItems()
            {
                BattleItem1 = InventoryItem.ReadItem(bytes, Offsets.BATTLE_ITEMS + 0),
                BattleItem2 = InventoryItem.ReadItem(bytes, Offsets.BATTLE_ITEMS + 8),
                BattleItem3 = InventoryItem.ReadItem(bytes, Offsets.BATTLE_ITEMS + 16),
                BattleItem4 = InventoryItem.ReadItem(bytes, Offsets.BATTLE_ITEMS + 24)
            };
        }

        public List<byte> Write(List<byte> bytes)
        {
            //Validate propeties
            BattleItem1 = ValidatePropeties(BattleItem1);
            BattleItem2 = ValidatePropeties(BattleItem2);
            BattleItem3 = ValidatePropeties(BattleItem3);
            BattleItem4 = ValidatePropeties(BattleItem4);

            //Write
            bytes = Utils.ReplaceRange(bytes, BattleItem1.Write().ToArray(), Offsets.BATTLE_ITEMS + 0);
            bytes = Utils.ReplaceRange(bytes, BattleItem2.Write().ToArray(), Offsets.BATTLE_ITEMS + 8);
            bytes = Utils.ReplaceRange(bytes, BattleItem3.Write().ToArray(), Offsets.BATTLE_ITEMS + 16);
            bytes = Utils.ReplaceRange(bytes, BattleItem4.Write().ToArray(), Offsets.BATTLE_ITEMS + 24);
            return bytes;
        }

        private InventoryItem ValidatePropeties(InventoryItem item)
        {
            //Ensures the properties are in line with a BattleItem
            if (item.I_00 == -1) item.I_05 = 0; //Set quantity to 0 if ID is -1 (empty slot)
            if (item.I_00 != -1) item.I_05 = 1; //Set quantity to 1 if ID is not -1 (the slot is not empty)
            if (item.I_00 == -1) item.I_04 = 255; //Set type to none if ID is -1 (empty slot)
            return item;
        }
    }

    public class Skills : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Filter options
        private string _superFilter = null;
        public string SuperFilter
        {
            get
            {
                return this._superFilter;
            }

            set
            {
                if (value != this._superFilter)
                {
                    this._superFilter = value;
                    NotifyPropertyChanged("SuperFilter");
                }
            }
        }
        private string _ultimateFilter = null;
        public string UltimateFilter
        {
            get
            {
                return this._ultimateFilter;
            }

            set
            {
                if (value != this._ultimateFilter)
                {
                    this._ultimateFilter = value;
                    NotifyPropertyChanged("UltimateFilter");
                }
            }
        }
        private string _evasiveFilter = null;
        public string EvasiveFilter
        {
            get
            {
                return this._evasiveFilter;
            }

            set
            {
                if (value != this._evasiveFilter)
                {
                    this._evasiveFilter = value;
                    NotifyPropertyChanged("EvasiveFilter");
                }
            }
        }
        private string _awokenFilter = null;
        public string AwokenFilter
        {
            get
            {
                return this._awokenFilter;
            }

            set
            {
                if (value != this._awokenFilter)
                {
                    this._awokenFilter = value;
                    NotifyPropertyChanged("AwokenFilter");
                }
            }
        }



        //Filtered Views
        //Filtered views
        private ListCollectionView _viewSuper = null;
        [YAXDontSerialize]
        public ListCollectionView ViewSuper
        {
            get
            {
                if (_viewSuper != null)
                {
                    return _viewSuper;
                }
                _viewSuper = new ListCollectionView(SuperSkills);
                _viewSuper.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
                _viewSuper.Filter = new Predicate<object>(SuperFilterCheck);
                return _viewSuper;
            }
            set
            {
                if (value != _viewSuper)
                {
                    _viewSuper = value;
                    NotifyPropertyChanged("ViewSuper");
                }
            }
        }

        private ListCollectionView _viewUltimate = null;
        [YAXDontSerialize]
        public ListCollectionView ViewUltimate
        {
            get
            {
                if (_viewUltimate != null)
                {
                    return _viewUltimate;
                }
                _viewUltimate = new ListCollectionView(UltimateSkills);
                _viewUltimate.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
                _viewUltimate.Filter = new Predicate<object>(UltimateFilterCheck);
                return _viewUltimate;
            }
            set
            {
                if (value != _viewUltimate)
                {
                    _viewUltimate = value;
                    NotifyPropertyChanged("ViewUltimate");
                }
            }
        }

        private ListCollectionView _viewEvasive = null;
        [YAXDontSerialize]
        public ListCollectionView ViewEvasive
        {
            get
            {
                if (_viewEvasive != null)
                {
                    return _viewEvasive;
                }
                _viewEvasive = new ListCollectionView(EvasiveSkills);
                _viewEvasive.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
                _viewEvasive.Filter = new Predicate<object>(EvasiveFilterCheck);
                return _viewEvasive;
            }
            set
            {
                if (value != _viewEvasive)
                {
                    _viewEvasive = value;
                    NotifyPropertyChanged("ViewEvasive");
                }
            }
        }

        private ListCollectionView _viewAwoken = null;
        [YAXDontSerialize]
        public ListCollectionView ViewAwoken
        {
            get
            {
                if (_viewAwoken != null)
                {
                    return _viewAwoken;
                }
                _viewAwoken = new ListCollectionView(AwokenSkills);
                _viewAwoken.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
                _viewAwoken.Filter = new Predicate<object>(AwokenFilterCheck);
                return _viewAwoken;
            }
            set
            {
                if (value != _viewAwoken)
                {
                    _viewAwoken = value;
                    NotifyPropertyChanged("ViewAwoken");
                }
            }
        }



        private ObservableCollection<Skill> _superSkills = null;
        private ObservableCollection<Skill> _ultimateSkills = null;
        private ObservableCollection<Skill> _evasiveSkills = null;
        private ObservableCollection<Skill> _unk3Skills = null;
        private ObservableCollection<Skill> _blastSkills = null;
        private ObservableCollection<Skill> _awokenSkills = null;

        public ObservableCollection<Skill> SuperSkills
        {
            get
            {
                return this._superSkills;
            }

            set
            {
                if (value != this._superSkills)
                {
                    this._superSkills = value;
                    NotifyPropertyChanged("SuperSkills");
                    NotifyPropertyChanged("ViewSuper");
                }
            }
        }
        public ObservableCollection<Skill> UltimateSkills
        {
            get
            {
                return this._ultimateSkills;
            }

            set
            {
                if (value != this._ultimateSkills)
                {
                    this._ultimateSkills = value;
                    NotifyPropertyChanged("UltimateSkills");
                    NotifyPropertyChanged("ViewUltimate");
                }
            }
        }
        public ObservableCollection<Skill> EvasiveSkills
        {
            get
            {
                return this._evasiveSkills;
            }

            set
            {
                if (value != this._evasiveSkills)
                {
                    this._evasiveSkills = value;
                    NotifyPropertyChanged("EvasivesSkills");
                    NotifyPropertyChanged("ViewEvasive");
                }
            }
        }
        public ObservableCollection<Skill> Unk3Skills
        {
            get
            {
                return this._unk3Skills;
            }

            set
            {
                if (value != this._unk3Skills)
                {
                    this._unk3Skills = value;
                    NotifyPropertyChanged("Unk3Skills");
                }
            }
        }
        public ObservableCollection<Skill> BlastSkills
        {
            get
            {
                return this._blastSkills;
            }

            set
            {
                if (value != this._blastSkills)
                {
                    this._blastSkills = value;
                    NotifyPropertyChanged("BlastSkills");
                }
            }
        }
        public ObservableCollection<Skill> AwokenSkills
        {
            get
            {
                return this._awokenSkills;
            }

            set
            {
                if (value != this._awokenSkills)
                {
                    this._awokenSkills = value;
                    NotifyPropertyChanged("AwokenSkills");
                    NotifyPropertyChanged("ViewAwoken");
                }
            }
        }


        public static Skills Read(byte[] rawBytes)
        {
            return new Skills()
            {
                SuperSkills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_SUPER, 1024),
                UltimateSkills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_ULTIMATE, 1024),
                EvasiveSkills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_EVASIVE, 1024),
                Unk3Skills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_UNK3, 1024),
                BlastSkills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_BLAST, 1024),
                AwokenSkills = Skill.ReadSkills(rawBytes, Offsets.SKILLS_AWOKEN, 1024),
            };
        }

        public List<byte> Write(List<byte> bytes)
        {
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(SuperSkills).ToArray(), Offsets.SKILLS_SUPER);
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(UltimateSkills).ToArray(), Offsets.SKILLS_ULTIMATE);
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(EvasiveSkills).ToArray(), Offsets.SKILLS_EVASIVE);
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(Unk3Skills).ToArray(), Offsets.SKILLS_UNK3);
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(BlastSkills).ToArray(), Offsets.SKILLS_BLAST);
            bytes = Utils.ReplaceRange(bytes, Skill.WriteAll(AwokenSkills).ToArray(), Offsets.SKILLS_AWOKEN);

            return bytes;
        }

        
        //Filter methods
        public bool SuperFilterCheck(object skill)
        {
            if (String.IsNullOrWhiteSpace(SuperFilter)) return true;
            var _quest = skill as Skill;

            if (_quest != null)
            {
                if (_quest.Name.Contains(SuperFilter)) return true;
            }

            return false;
        }

        public void UpdateSuperFilter()
        {
            _viewSuper = new ListCollectionView(SuperSkills);
            _viewSuper.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
            _viewSuper.Filter = new Predicate<object>(SuperFilterCheck);
            NotifyPropertyChanged("ViewSuper");
        }

        public bool UltimateFilterCheck(object skill)
        {
            if (String.IsNullOrWhiteSpace(UltimateFilter)) return true;
            var _quest = skill as Skill;

            if (_quest != null)
            {
                if (_quest.Name.Contains(UltimateFilter)) return true;
            }

            return false;
        }

        public void UpdateUltimateFilter()
        {
            _viewUltimate = new ListCollectionView(UltimateSkills);
            _viewUltimate.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
            _viewUltimate.Filter = new Predicate<object>(UltimateFilterCheck);
            NotifyPropertyChanged("ViewUltimate");
        }

        public bool EvasiveFilterCheck(object skill)
        {
            if (String.IsNullOrWhiteSpace(EvasiveFilter)) return true;
            var _quest = skill as Skill;

            if (_quest != null)
            {
                if (_quest.Name.Contains(EvasiveFilter)) return true;
            }

            return false;
        }

        public void UpdateEvasiveFilter()
        {
            _viewEvasive = new ListCollectionView(EvasiveSkills);
            _viewEvasive.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
            _viewEvasive.Filter = new Predicate<object>(EvasiveFilterCheck);
            NotifyPropertyChanged("ViewEvasive");
        }

        public bool AwokenFilterCheck(object skill)
        {
            if (String.IsNullOrWhiteSpace(AwokenFilter)) return true;
            var _quest = skill as Skill;

            if (_quest != null)
            {
                if (_quest.Name.Contains(AwokenFilter)) return true;
            }

            return false;
        }

        public void UpdateAwokenFilter()
        {
            _viewAwoken = new ListCollectionView(AwokenSkills);
            _viewAwoken.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
            _viewAwoken.Filter = new Predicate<object>(AwokenFilterCheck);
            NotifyPropertyChanged("ViewAwoken");
        }


    }

    public class Skill : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public string Name { get; set; }

        private bool _unlocked = false;
        [YAXAttributeForClass]
        [YAXSerializeAs("Unlocked")]
        public bool I_00
        {
            get
            {
                return this._unlocked;
            }

            set
            {
                if (value != this._unlocked)
                {
                    this._unlocked = value;
                    NotifyPropertyChanged("I_00");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID1")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public SkillType I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public ushort I_06 { get; set; }

        public static ObservableCollection<Skill> ReadSkills(byte[] rawBytes, int offset, int maxCount)
        {
            ObservableCollection<Skill> skills = new ObservableCollection<Skill>();

            for (int i = 0; i < maxCount; i++)
            {
                if (BitConverter.ToInt32(rawBytes, offset + 0) != -1 && rawBytes[offset + 4] != 255)
                {
                    skills.Add(Read(rawBytes, offset));
                }
                offset += 8;
            }

            return skills;
        }

        private static Skill Read(byte[] rawBytes, int offset)
        {
            return new Skill()
            {
                I_00 = BitConverter.ToBoolean(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = (SkillType)BitConverter.ToUInt16(rawBytes, offset + 4),
                I_06 = BitConverter.ToUInt16(rawBytes, offset + 6)
            };
        }

        public static List<byte> WriteAll(ObservableCollection<Skill> quests, int count = 1024)
        {
            List<byte> bytes = new List<byte>();

            //Create skill list
            for (int i = 0; i < quests.Count; i++)
            {
                bytes.AddRange(quests[i].Write());
            }

            //Finish the list by filling in empty slots
            int emptySlots = count - quests.Count;

            for (int i = 0; i < emptySlots; i++)
            {
                bytes.AddRange(NullEntry());
            }

            if (bytes.Count / 8 != count) throw new InvalidDataException("Invalid skill data size!");
            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(I_00)));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));

            if (bytes.Count != 8) throw new InvalidDataException("Skill is supposed to be 8 bytes.");
            return bytes;
        }

        public static List<byte> NullEntry()
        {
            return new List<byte> { 255, 255, 255, 255, 255, 255, 255, 255 };
        }
    }
#endregion

#region Quests
    public class Quests : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Filtered views
        private ListCollectionView _viewTcq = null;
        [YAXDontSerialize]
        public ListCollectionView ViewTcq
        {
            get
            {
                if (_viewTcq != null)
                {
                    return _viewTcq;
                }
                _viewTcq = new ListCollectionView(MasterQuests);
                _viewTcq.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
                _viewTcq.Filter = new Predicate<object>(Contains);
                return _viewTcq;
            }
            set
            {
                if (value != _viewTcq)
                {
                    _viewTcq = value;
                    NotifyPropertyChanged("ViewTcq");
                }
            }
        }


        private ObservableCollection<Quest> _timePatrols = null;
        private ObservableCollection<Quest> _parallelQuests = null;
        private ObservableCollection<Quest> _masterQuests = null;
        private ObservableCollection<Quest> _timeRiftQuests = null;
        private ObservableCollection<Quest> _expertMissions = null;
        private ObservableCollection<Quest> _raids = null;
        private ObservableCollection<Quest> _elderKaiQuests = null;
        private ObservableCollection<Quest> _friezaSeige = null;
        private ObservableCollection<Quest> _infiniteHistory = null;
        private ObservableCollection<Quest> _prbQuests = null;



        public ObservableCollection<Quest> TimePatrols
        {
            get
            {
                return this._timePatrols;
            }

            set
            {
                if (value != this._timePatrols)
                {
                    this._timePatrols = value;
                    NotifyPropertyChanged("TimePatrols");
                }
            }
        }
        public ObservableCollection<Quest> ParallelQuests
        {
            get
            {
                return this._parallelQuests;
            }

            set
            {
                if (value != this._parallelQuests)
                {
                    this._parallelQuests = value;
                    NotifyPropertyChanged("ParallelQuests");
                }
            }
        }
        public ObservableCollection<Quest> MasterQuests
        {
            get
            {
                return this._masterQuests;
            }

            set
            {
                if (value != this._masterQuests)
                {
                    this._masterQuests = value;
                    NotifyPropertyChanged("MasterQuests");
                    NotifyPropertyChanged("ViewTcq");
                }
            }
        }
        public ObservableCollection<Quest> TimeRiftQuests
        {
            get
            {
                return this._timeRiftQuests;
            }

            set
            {
                if (value != this._timeRiftQuests)
                {
                    this._timeRiftQuests = value;
                    NotifyPropertyChanged("TimeRiftQuests");
                }
            }
        }
        public ObservableCollection<Quest> ExpertMissions
        {
            get
            {
                return this._expertMissions;
            }

            set
            {
                if (value != this._expertMissions)
                {
                    this._expertMissions = value;
                    NotifyPropertyChanged("ExpertMissions");
                }
            }
        }
        public ObservableCollection<Quest> Raids
        {
            get
            {
                return this._raids;
            }

            set
            {
                if (value != this._raids)
                {
                    this._raids = value;
                    NotifyPropertyChanged("Raids");
                }
            }
        }
        public ObservableCollection<Quest> ElderKaiQuests
        {
            get
            {
                return this._elderKaiQuests;
            }

            set
            {
                if (value != this._elderKaiQuests)
                {
                    this._elderKaiQuests = value;
                    NotifyPropertyChanged("ElderKaiQuests");
                }
            }
        }
        public ObservableCollection<Quest> FriezaSeige
        {
            get
            {
                return this._friezaSeige;
            }

            set
            {
                if (value != this._friezaSeige)
                {
                    this._friezaSeige = value;
                    NotifyPropertyChanged("FriezaSeige");
                }
            }
        }
        public ObservableCollection<Quest> InfiniteHistory
        {
            get
            {
                return this._infiniteHistory;
            }

            set
            {
                if (value != this._infiniteHistory)
                {
                    this._infiniteHistory = value;
                    NotifyPropertyChanged("InfiniteHistory");
                }
            }
        }
        public ObservableCollection<Quest> PlayerRaidBattles
        {
            get
            {
                return this._prbQuests;
            }

            set
            {
                if (value != this._prbQuests)
                {
                    this._prbQuests = value;
                    NotifyPropertyChanged("PlayerRaidBattles");
                }
            }
        }

        public List<byte> Write(List<byte> bytes, int charaIdx, SAV_File sav)
        {
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(TimePatrols, 128, "TimePatrols").ToArray(), Offsets.QUESTS_TPQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(ParallelQuests, 192, "ParallelQuests").ToArray(), Offsets.QUESTS_TMQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(TimeRiftQuests, 128, "TimeRiftQuests").ToArray(), Offsets.QUESTS_BAQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(MasterQuests, 256, "MasterQuests").ToArray(), Offsets.QUESTS_TCQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(ExpertMissions, 96, "ExpertMissions").ToArray(), Offsets.QUESTS_HLQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(Raids, 96, "Raids").ToArray(), Offsets.QUESTS_RBQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(ElderKaiQuests, 64, "ElderKaiQuests").ToArray(), Offsets.QUESTS_CHQ + (Offsets.CAC_SIZE * charaIdx));
            bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(FriezaSeige, 128, "FriezaSeige").ToArray(), Offsets.QUESTS_LEQ + (Offsets.CAC_SIZE * charaIdx));
            if (sav.DLC6)
            {
                bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(InfiniteHistory, Offsets.QUESTS_OSQ_COUNT, "InfiniteHistory").ToArray(), Offsets.QUESTS_OSQ + (Offsets.CAC_DLC_SIZE * charaIdx));
                bytes = Utils.ReplaceRange(bytes, TokipediaProgress.Write(charaIdx, InfiniteHistory).ToArray(), Offsets.TOKIPEDIA + (Offsets.CAC_DLC_SIZE * charaIdx));
            }
            if (sav.DLC8)
            {
                bytes = Utils.ReplaceRange(bytes, Quest.WriteAll(PlayerRaidBattles, Offsets.QUESTS_PRB_COUNT, "PlayerRaidBattles").ToArray(), Offsets.QUESTS_PRB + (Offsets.CAC_DLC_SIZE * charaIdx));
            }

            return bytes;
        }

        public static Quests Read(byte[] rawBytes, int charaIdx, SAV_File sav)
        {
            //Check if save file has DLC data
            ObservableCollection<Quest> inf = null;
            ObservableCollection<Quest> prb = null;
            if (sav.DLC6)
            {
                inf = Quest.ReadAll(rawBytes, Offsets.QUESTS_OSQ + (Offsets.CAC_DLC_SIZE * charaIdx), Offsets.QUESTS_OSQ_COUNT, charaIdx, true);
            }
            if (sav.DLC8)
            {
                prb = Quest.ReadAll(rawBytes, Offsets.QUESTS_PRB + (Offsets.CAC_DLC_SIZE * charaIdx), Offsets.QUESTS_PRB_COUNT, charaIdx, false);
            }

            return new Quests()
            {
                TimePatrols = Quest.ReadAll(rawBytes, Offsets.QUESTS_TPQ + (Offsets.CAC_SIZE * charaIdx), 128, charaIdx),
                ParallelQuests = Quest.ReadAll(rawBytes, Offsets.QUESTS_TMQ + (Offsets.CAC_SIZE * charaIdx), 192, charaIdx),
                TimeRiftQuests = Quest.ReadAll(rawBytes, Offsets.QUESTS_BAQ + (Offsets.CAC_SIZE * charaIdx), 128, charaIdx),
                MasterQuests = Quest.ReadAll(rawBytes, Offsets.QUESTS_TCQ + (Offsets.CAC_SIZE * charaIdx), 256, charaIdx),
                ExpertMissions = Quest.ReadAll(rawBytes, Offsets.QUESTS_HLQ + (Offsets.CAC_SIZE * charaIdx), 96, charaIdx),
                Raids = Quest.ReadAll(rawBytes, Offsets.QUESTS_RBQ + (Offsets.CAC_SIZE * charaIdx), 96, charaIdx),
                ElderKaiQuests = Quest.ReadAll(rawBytes, Offsets.QUESTS_CHQ + (Offsets.CAC_SIZE * charaIdx), 64, charaIdx),
                FriezaSeige = Quest.ReadAll(rawBytes, Offsets.QUESTS_LEQ + (Offsets.CAC_SIZE * charaIdx), 128, charaIdx),
                InfiniteHistory = inf,
                PlayerRaidBattles = prb
            };
        }
        
        public bool Contains(object quest)
        {
            var _quest = quest as Quest;

            if (_quest != null)
            {
                return _quest.HasName;
            }

            return false;
        }

        public bool IsComplete(QuestType type, string ID)
        {
            ObservableCollection<Quest> quests;

            switch (type)
            {
                case QuestType.TMQ:
                    quests = ParallelQuests;
                    break;
                case QuestType.TPQ:
                    quests = TimePatrols;
                    break;
                case QuestType.TCQ:
                    quests = MasterQuests;
                    break;
                case QuestType.HLQ:
                    quests = ExpertMissions;
                    break;
                case QuestType.OSQ:
                    if (InfiniteHistory == null) return false;
                    quests = InfiniteHistory;
                    break;
                case QuestType.BAQ:
                    quests = TimeRiftQuests;
                    break;
                case QuestType.RBQ:
                    quests = Raids;
                    break;
                case QuestType.LEQ:
                    quests = FriezaSeige;
                    break;
                case QuestType.CHQ:
                    quests = ElderKaiQuests;
                    break;
                case QuestType.PRB:
                    if (PlayerRaidBattles == null) return false;
                    quests = PlayerRaidBattles;
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Quest type {0} is invalid.", type));
            }

            foreach (var quest in quests)
            {
                if (quest.StrID == ID)
                {
                    if (quest.I_08 == 3)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class Quest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [YAXDontSerialize]
        public string StrID { get; set; } //string id from qxd / folder name
        [YAXDontSerialize]
        public bool HasName { get; set; }
        [YAXDontSerialize]
        public string SubType { get; set; }
        [YAXDontSerialize]
        public int SortedID { get; set; }
        [YAXDontSerialize]
        public string DisplayName { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public QuestType I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public int I_04 { get; set; }
        private int _stateValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("State")]
        public int I_08
        {
            get
            {
                return this._stateValue;
            }

            set
            {
                if (value != this._stateValue)
                {
                    this._stateValue = value;
                    NotifyPropertyChanged("I_08");
                }
            }
        }
        private int _rankValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Rank")]
        public int I_12
        {
            get
            {
                return this._rankValue;
            }

            set
            {
                if (value != this._rankValue)
                {
                    this._rankValue = value;
                    NotifyPropertyChanged("I_12");
                }
            }
        }
        private int _winConditionValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("WinCondition")]
        public int I_16
        {
            get
            {
                return this._winConditionValue;
            }

            set
            {
                if (value != this._winConditionValue)
                {
                    this._winConditionValue = value;
                    NotifyPropertyChanged("I_16");
                }
            }
        }
        private int _scoreValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Score")]
        public int I_20
        {
            get
            {
                return this._scoreValue;
            }

            set
            {
                if (value != this._scoreValue)
                {
                    this._scoreValue = value;
                    NotifyPropertyChanged("I_20");
                }
            }
        }

        private TokipediaProgress _tokipedia = null;
        [YAXDontSerializeIfNull]
        public TokipediaProgress Tokipedia
        {
            get
            {
                return this._tokipedia;
            }

            set
            {
                if (value != this._tokipedia)
                {
                    this._tokipedia = value;
                    NotifyPropertyChanged("Tokipedia");
                    NotifyPropertyChanged("TokipediaCompletion");
                }
            }
        }
        [YAXDontSerialize]
        public string TokipediaCompletion
        {
            get
            {
                if (Tokipedia != null)
                {
                    return Tokipedia.ToString();
                }
                else
                {
                    return "---";
                }
            }
        }

        public static ObservableCollection<Quest> ReadAll(byte[] rawBytes, int offset, int maxCount, int charaIdx, bool tokipedia = false)
        {
            ObservableCollection<Quest> quests = new ObservableCollection<Quest>();

            for (int i = 0; i < maxCount; i++)
            {
                if (BitConverter.ToInt32(rawBytes, offset) == -1) break;

                TokipediaProgress tokipediaProgress = null;
                if (tokipedia == true)
                {
                    tokipediaProgress = TokipediaProgress.Read(rawBytes, Offsets.TOKIPEDIA + (8 * i) + (Offsets.CAC_DLC_SIZE * charaIdx));
                }

                quests.Add(Read(rawBytes, offset, tokipediaProgress));
                offset += 24;
            }

            return quests;
        }

        public static Quest Read(byte[] rawBytes, int offset, TokipediaProgress tokipedia = null)
        {
            return new Quest()
            {
                I_00 = (QuestType)BitConverter.ToInt32(rawBytes, offset),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                Tokipedia = tokipedia
            };
        }

        public static List<byte> WriteAll(ObservableCollection<Quest> _quests, int count, string questType)
        {
            //Create a new collection of quests so that the original isn't modified
            List<Quest> quests = new List<Quest>();

            for (int i = 0; i < _quests.Count; i++)
            {
                quests.Add(new Quest()
                {
                    I_00 = _quests[i].I_00,
                    I_04 = _quests[i].I_04,
                    I_08 = _quests[i].I_08,
                    I_12 = _quests[i].I_12,
                    I_16 = _quests[i].I_16,
                    I_20 = _quests[i].I_20
                });
            }

            //Add padding entries if required
            if (quests.Count < count)
            {
                while (quests.Count != count)
                {
                    quests.Add(GetNull());
                }
            }

            if (quests.Count != count) throw new InvalidDataException(String.Format("Invalid quest count for quest type {0}. Expected {1} but found {2}.", questType, count, quests.Count));
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(quests[i].Write());
            }

            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes((int)I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));

            return bytes;
        }

        public void MaxScore()
        {
            I_20 = 999999;
        }

        public void MaxRank()
        {
            I_12 = 6;
        }

        public void SetComplete()
        {
            I_08 = 3;
        }

        public static Quest GetNull()
        {
            return new Quest()
            {
                I_00 = QuestType.Null,
                I_04 = -1,
                I_08 = -1,
                I_12 = -1,
                I_16 = -1,
                I_20 = -1
            };
        }

        public void TokipediaChanged()
        {
            NotifyPropertyChanged("TokipediaCompletion");
        }
    }

    public class TokipediaProgress
    {
#if SaveEditor
        [YAXDontSerialize]
        public LB_Save_Editor.ID.TokipediaEntry TokipediaEntry { get; set; }
        
        public override string ToString()
        {
            if (TokipediaEntry == null) return "---";
            return String.Format("{0:0.0}%", GetTokipediaCompletion());
        }

        public float GetTokipediaCompletion()
        {
            //The percentage caluclated here is not equal to the one shown ingame, as the game treats branches and alts differently (alts get less percentage).

            if (TokipediaEntry == null) return 0;
            //Count how many of the bit flags required by TokipediaEntry are set
            ulong setBitsValue = (ulong)I_00;

            ulong combinedRequirement = (ulong)TokipediaEntry.AlternatePaths + (ulong)TokipediaEntry.BranchingPaths; //Joining both paths togher into a single value for easier comparison
            ulong bitTotal = combinedRequirement & setBitsValue; //Copying all bits that belong to a path into a new value
            int totalRequirementsMeet = Utils.GetSetBitCount((long)bitTotal);

            //Now check if any bits that dont belong to any of the paths are set
            ulong nonreqBits = combinedRequirement ^ (ulong)TokipediaFlags.Bitmask;
            if ((nonreqBits & setBitsValue) != 0) totalRequirementsMeet++;

            //MessageBox.Show(String.Format("Reqs Meet: {0}\nAlt Paths: {1}\nBranch Paths: {2}\nID: {3}\nnonReqBits: {4}", totalRequirementsMeet, TokipediaEntry.AlternatePaths, TokipediaEntry.BranchingPaths, TokipediaEntry.ID, nonreqBits));

            int totalPaths = Utils.GetSetBitCount((long)combinedRequirement) + 1; //For 100% the game requires all paths to be completed + one non-path completed.
            return (100f / totalPaths) * totalRequirementsMeet;
        }
        
#endif
        [YAXAttributeForClass]
        [YAXSerializeAs("Flags")]
        public TokipediaFlags I_00 { get; set; }

        public static TokipediaProgress Read(byte[] rawBytes, int offset)
        {
            return new TokipediaProgress()
            {
                I_00 = (TokipediaFlags)BitConverter.ToUInt64(rawBytes, offset)
            };
        }

        public static List<byte> Write(int charaIdx, ObservableCollection<Quest> quests)
        {
            List<byte> newBytes = new List<byte>();

            //Write entries
            int i = 0;
            foreach (var e in quests)
            {
                newBytes.AddRange(BitConverter.GetBytes((ulong)e.Tokipedia.I_00));
                if (i == Offsets.TOKIPEDIA_COUNT) break;
                i++;
            }

            //Add padding for remaining entries
            if (quests.Count < 48)
            {
                newBytes.AddRange(new byte[(Offsets.TOKIPEDIA_COUNT - quests.Count) * 8]);
            }

            //Validate size
            int expectedSize = Offsets.TOKIPEDIA_COUNT * 8;
            if (newBytes.Count != expectedSize)
            {
                throw new Exception(String.Format("Tokipedia data is an invalid size.\nSize = {0}\nExpected = {1}", newBytes.Count, expectedSize));
            }

            return newBytes;
        }
        
        public static int TokipediaFlagToCharaId(TokipediaFlags flag)
        {
            switch (flag)
            {
                case TokipediaFlags.Krillin:
                    return 10;
                case TokipediaFlags.LordSlug:
                    return 121;
                case TokipediaFlags.MajinBuu:
                    return 35;
                case TokipediaFlags.Nappa:
                    return 15;
                case TokipediaFlags.Pan:
                    return 42;
                case TokipediaFlags.Piccolo:
                    return 9;
                case TokipediaFlags.Raditz:
                    return 13;
                case TokipediaFlags.Tien:
                    return 12;
                case TokipediaFlags.Turles:
                    return 87;
                case TokipediaFlags.Vegeta:
                    return 16;
                case TokipediaFlags.Videl:
                    return 34;
                case TokipediaFlags.Whis:
                    return 65;
                case TokipediaFlags.Yamcha:
                    return 11;
                case TokipediaFlags.Zamasu:
                    return 138;
                case TokipediaFlags.Zarbon:
                    return 122;
                case TokipediaFlags.AdultGohan:
                    return 8;
                case TokipediaFlags.Android16:
                    return 86;
                case TokipediaFlags.Android18:
                    return 30;
                case TokipediaFlags.Bardock:
                    return 1;
                case TokipediaFlags.Beerus:
                    return 41;
                case TokipediaFlags.Bojack:
                    return 140;
                case TokipediaFlags.Broly:
                    return 40;
                case TokipediaFlags.Cell:
                    return 31;
                case TokipediaFlags.Cooler:
                    return 85;
                case TokipediaFlags.Dodoria:
                    return 123;
                case TokipediaFlags.Frieza:
                    return 23;
                case TokipediaFlags.FutureGohan:
                    return 86;
                case TokipediaFlags.Fuu:
                    return 150;
                case TokipediaFlags.Ginyu:
                    return 22;
                case TokipediaFlags.Goku:
                    return 0;
                case TokipediaFlags.Gotenks:
                    return 38;
                case TokipediaFlags.Hercule:
                    return 52;
                case TokipediaFlags.Hit:
                    return 126;
                case TokipediaFlags.Jaco:
                    return 67;
                case TokipediaFlags.KidGohan:
                    return 6;
                default:
                    return 0;
            }
        }
    }

#endregion

#region Mentors
    public class MentorCustomization
    {
        [YAXDontSerialize]
        public Visibility IsVisible { get; set; }
        [YAXDontSerialize]
        public string Name { get; set; }

        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("StatType")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("SuperSoul")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("HasFlags")]
        [YAXSerializeAs("values")]
        public MentorCustomizationFlags I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("I_26")]
        [YAXSerializeAs("value")]
        public ushort I_26 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }
        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("I_38")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("I_42")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("I_46")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("ID2")]
        public int I_48 { get; set; }
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("ID2")]
        public int I_52 { get; set; }
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("ID2")]
        public int I_56 { get; set; }
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("ID2")]
        public int I_60 { get; set; }
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("ID2")]
        public int I_64 { get; set; }
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("ID2")]
        public int I_68 { get; set; }
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("ID2")]
        public int I_72 { get; set; }
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("ID2")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }

        [YAXDontSerialize]
        public bool CostumeEnabled
        {
            get
            {
                return I_06.HasFlag(MentorCustomizationFlags.Costume);
            }
            set
            {
                if (value == true)
                {
                    if (!I_06.HasFlag(MentorCustomizationFlags.Costume)) I_06 = I_06 | MentorCustomizationFlags.Costume;
                }
                else
                {
                    I_06 = I_06 & ~MentorCustomizationFlags.Costume;
                }
            }
        }
        [YAXDontSerialize]
        public bool SkillEnabled
        {
            get
            {
                return I_06.HasFlag(MentorCustomizationFlags.Skill);
            }
            set
            {
                if (value == true)
                {
                    if (!I_06.HasFlag(MentorCustomizationFlags.Skill)) I_06 = I_06 | MentorCustomizationFlags.Skill;
                }
                else
                {
                    I_06 = I_06 & ~MentorCustomizationFlags.Skill;
                }
            }
        }
        [YAXDontSerialize]
        public bool SuperSoulEnabled
        {
            get
            {
                return I_06.HasFlag(MentorCustomizationFlags.SuperSoul);
            }
            set
            {
                if (value == true)
                {
                    if (!I_06.HasFlag(MentorCustomizationFlags.SuperSoul)) I_06 = I_06 | MentorCustomizationFlags.SuperSoul;
                }
                else
                {
                    I_06 = I_06 & ~MentorCustomizationFlags.SuperSoul;
                }
            }
        }

        public static ObservableCollection<MentorCustomization> ReadAll(byte[] rawBytes, int cacIdx, int version)
        {
            ObservableCollection<MentorCustomization> mentors = new ObservableCollection<MentorCustomization>();

            //Original partners
            for (int i = 0; i < Offsets.MENTOR_CUSTOMIZATION_COUNT; i++)
            {
                mentors.Add(Read(rawBytes, i, cacIdx));
            }
            
            //Partners added in 1.15
            if(version  >= 19)
            {
                for (int i = 0; i < Offsets.MENTOR_CUSTOMIZATION_COUNT2; i++)
                {
                    mentors.Add(Read(rawBytes, i + Offsets.MENTOR_CUSTOMIZATION_COUNT, cacIdx));
                }
            }
            
            //Partners added in 1.17
            if (version >= 22)
            {
                for (int i = 0; i < Offsets.MENTOR_CUSTOMIZATION_COUNT3; i++)
                {
                    mentors.Add(Read(rawBytes, i + Offsets.MENTOR_CUSTOMIZATION_COUNT + Offsets.MENTOR_CUSTOMIZATION_COUNT2, cacIdx));
                }
            }

            return mentors;
        }

        public static MentorCustomization Read(byte[] rawBytes, int mentorIdx, int cacIdx)
        {
            int offset;

            if(mentorIdx <= 46)
            {
                offset = Offsets.MENTOR_CUSTOMIZATION + (92 * mentorIdx) + (Offsets.CAC_DLC_SIZE * cacIdx);
                return Read_Large(rawBytes, offset, mentorIdx);
            }
            else if(mentorIdx >= 47 && mentorIdx <= 56)
            {
                offset = Offsets.MENTOR_CUSTOMIZATION2 + (44 * (mentorIdx - 47)) + (Offsets.CAC_DLC_SIZE * cacIdx);
                return Read_Small(rawBytes, offset, mentorIdx);
            }
            else if(mentorIdx >= 57 && mentorIdx <= 110)
            {
                offset = Offsets.MENTOR_CUSTOMIZATION3 + (44 * (mentorIdx - 57)) + (Offsets.CAC_DLC2_SIZE * cacIdx);
                return Read_Small(rawBytes, offset, mentorIdx);
            }
            else
            {
                throw new InvalidDataException("Invalid Partner ID - cannot go above 110!");
            }

        }

        public static List<byte> WriteAll(List<byte> bytes, ObservableCollection<MentorCustomization> mentors, int cacIdx, int version)
        {
            if (mentors.Count > Offsets.MAX_CUSTOM_PARTNERS) throw new Exception("Invalid amount of MentorCustomizations.");

            int offset1 = Offsets.MENTOR_CUSTOMIZATION + (Offsets.CAC_DLC_SIZE * cacIdx);
            int offset2 = Offsets.MENTOR_CUSTOMIZATION2 + (Offsets.CAC_DLC_SIZE * cacIdx);
            int offset3 = Offsets.MENTOR_CUSTOMIZATION3 + (Offsets.CAC_DLC2_SIZE * cacIdx);

            foreach(var partner in mentors)
            {
                if(partner.Index >= 0 && partner.Index <= 46)
                {
                    //Original partner list
                    var ret = partner.Write_Large();
                    bytes = Utils.ReplaceRange(bytes, ret.ToArray(), offset1);
                    offset1 += 92;
                }
                else if (partner.Index >= 47 && partner.Index <= 56 && version >= 19)
                {
                    //Expanded partner list in 1.15 (10 partners)
                    var ret = partner.Write_Small();
                    bytes = Utils.ReplaceRange(bytes, ret.ToArray(), offset2);
                    offset2 += 44;
                }
                else if (partner.Index >= 57 && partner.Index <= 110 && version >= 22)
                {
                    //Expanded partner list in 1.17 (54 partners)
                    var ret = partner.Write_Small();
                    bytes = Utils.ReplaceRange(bytes, ret.ToArray(), offset3);
                    offset3 += 44;
                }

            }

            return bytes;
        }

        public List<byte> Write_Large()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_22));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_26));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_34));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_38));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_42));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_46));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(I_88));

            if (bytes.Count != 92) throw new Exception("MentorCustomization is the wrong size.");
            return bytes;
        }

        public List<byte> Write_Small()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_22));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_26));

            //Skills
            bytes.AddRange(BitConverter.GetBytes((ushort)I_48));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_52));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_56));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_60));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_64));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_68));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_72));
            bytes.AddRange(BitConverter.GetBytes((ushort)I_76));

            if (bytes.Count != 44) throw new Exception("MentorCustomization is the wrong size (small version).");
            return bytes;
        }


        //New format in 1.15:
        public static MentorCustomization Read_Large(byte[] rawBytes, int offset, int partnerIdx)
        {
            return new MentorCustomization()
            {
                Index = partnerIdx,
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                I_06 = (MentorCustomizationFlags)BitConverter.ToUInt16(rawBytes, offset + 6),
                I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                I_26 = BitConverter.ToUInt16(rawBytes, offset + 26),
                I_28 = BitConverter.ToUInt16(rawBytes, offset + 28),
                I_30 = BitConverter.ToUInt16(rawBytes, offset + 30),
                I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
                I_64 = BitConverter.ToInt32(rawBytes, offset + 64),
                I_68 = BitConverter.ToInt32(rawBytes, offset + 68),
                I_72 = BitConverter.ToInt32(rawBytes, offset + 72),
                I_76 = BitConverter.ToInt32(rawBytes, offset + 76),
                I_80 = BitConverter.ToInt32(rawBytes, offset + 80),
                I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                I_88 = BitConverter.ToInt32(rawBytes, offset + 88),
            };
        }

        public static MentorCustomization Read_Small(byte[] rawBytes, int offset, int partnerIdx)
        {
            return new MentorCustomization()
            {
                Index = partnerIdx,
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                I_06 = (MentorCustomizationFlags)BitConverter.ToUInt16(rawBytes, offset + 6),
                I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                I_26 = BitConverter.ToUInt16(rawBytes, offset + 26),

                //Skills
                I_48 = BitConverter.ToUInt16(rawBytes, offset + 28),
                I_52 = BitConverter.ToUInt16(rawBytes, offset + 30),
                I_56 = BitConverter.ToUInt16(rawBytes, offset + 32),
                I_60 = BitConverter.ToUInt16(rawBytes, offset + 34),
                I_64 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_68 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_72 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_76 = BitConverter.ToUInt16(rawBytes, offset + 42)
            };
        }



    }

    [YAXSerializeAs("Mentor")]
    public class MentorProgress : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public string DisplayName { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public Mentors Mentor { get; set; }
        [YAXAttributeForClass]
        public MentorFlags Flags { get; set; }
        private ushort _I_00_value = 0;
        private ushort _I_02_value = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Friendship")]
        public ushort I_00
        {
            get
            {
                return this._I_00_value;
            }

            set
            {
                if (value != this._I_00_value)
                {
                    this._I_00_value = value;
                    NotifyPropertyChanged("I_00");
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("DualAttack")]
        public ushort I_02  // Dual Attack guage: 0 - 4000 (1000 per segement)
        {
            get
            {
                return this._I_02_value;
            }

            set
            {
                if (value != this._I_02_value)
                {
                    this._I_02_value = value;
                    NotifyPropertyChanged("I_02");
                }
            }
        }

        public static ObservableCollection<MentorProgress> Read(byte[] rawBytes, int charaIdx)
        {
            int offset = Offsets.MENTOR_PROGRESS + (Offsets.CAC_SIZE * charaIdx);
            ObservableCollection<MentorProgress> mentors = new ObservableCollection<MentorProgress>();

            for (int i = 0; i < Offsets.MENTOR_COUNT; i++)
            {
                mentors.Add(new MentorProgress()
                {
                    Flags = (MentorFlags)BitConverter.ToInt32(rawBytes, offset + 4),
                    I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                    I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                    Mentor = (Mentors)i
                });

                offset += 8;
            }

            return mentors;
        }

        public static List<byte> WriteAll(List<byte> bytes, ObservableCollection<MentorProgress> _mentors, int charaIdx)
        {
            //Create new list so that the original isn't modified
            List<MentorProgress> mentors = new List<MentorProgress>();

            foreach (var m in _mentors)
            {
                mentors.Add(new MentorProgress()
                {
                    Flags = m.Flags,
                    I_00 = m.I_00,
                    I_02 = m.I_02
                });
            }

            int count = 40; //There is only space for 40 mentors in the save file
            int offset = Offsets.MENTOR_PROGRESS + (Offsets.CAC_SIZE * charaIdx);

            //Add null mentors to complete list
            if (mentors.Count != count)
            {
                while (mentors.Count != count) //Add padding entries
                {
                    mentors.Add(new MentorProgress());
                }
            }


            if (mentors.Count != count) throw new InvalidDataException(String.Format("Invalid mentor count. Expected {0} but found {1}.", count, mentors.Count));

            //Create temp byte list for mentors
            List<byte> mentorBytes = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                mentorBytes.AddRange(BitConverter.GetBytes(mentors[i].I_00));
                mentorBytes.AddRange(BitConverter.GetBytes(mentors[i].I_02));
                mentorBytes.AddRange(BitConverter.GetBytes((int)mentors[i].Flags));
            }

            if (mentorBytes.Count / 8 != count) throw new InvalidDataException("Mentor list is wrong size.");

            //Insert temp byte list into save file bytes
            bytes = Utils.ReplaceRange(bytes, mentorBytes.ToArray(), offset);

            return bytes;
        }

        public static int MentorToCharaId(int mentorIndex)
        {
            switch (mentorIndex)
            {
                case 0: // Krillin
                    return 10;
                case 1: //Tien
                    return 12;
                case 2: //yamcha
                    return 11;
                case 3: //Piccolo
                    return 9;
                case 4: //Raditz
                    return 13;
                case 5: //Gohan (Kid)
                    return 6;
                case 6: //Nappa
                    return 15;
                case 7: //Vegeta
                    return 16;
                case 8: //Zarbon
                    return 122;
                case 9: //Dodoria
                    return 123;
                case 10: //Captain Ginyu
                    return 22;
                case 11: //Frieza
                    return 23;
                case 12: //ANdroid 18
                    return 30;
                case 13: //Cell
                    return 31;
                case 14: //Lord Slug
                    return 121;
                case 15: //Majin Buu
                    return 35;
                case 16: //Hercule
                    return 52;
                case 17: //Gohan/Videl
                    return 8; //Only return gohans ID
                case 18: //Gotenks
                    return 38;
                case 19: //Turles
                    return 87;
                case 20: //Broly
                    return 40;
                case 21: //Beerus
                    return 41;
                case 22: //Pan
                    return 42;
                case 23: //Jaco
                    return 67;
                case 24: //Goku
                    return 0;
                case 25: //Whis
                    return 65;
                case 26: //Cooler
                    return 85;
                case 27: //Android 16
                    return 83;
                case 28: //Gohan (Future)
                    return 86;
                case 29: //Bardock
                    return 1;
                case 30: //Hit
                    return 126;
                case 31: //Bojack
                    return 140;
                case 32: //Zamasu
                    return 138;
                case 45: //Videl
                    return 34;
                case 46: //Fu
                    return 150;
                case 47:
                    return 2; //Goku SS4
                case 48:
                    return 17; //Vegeta SS4
                case 49:
                    return 26; //Trunks
                case 50:
                    return 141; //SSB Vegeto
                case 51:
                    return 154; //SSB Gogeta
                case 52:
                    return 139; //Goku Black Rose
                case 53:
                    return 148; //Android 17 (DB Super)
                case 54:
                    return 58; //Jenemba
                case 55:
                    return 147; //Tapion
                case 56:
                    return 155; //Broly (FSS)
                case 57:
                    return 4; //Goku GT
                case 58:
                    return 50; //Omega Shenron
                case 59:
                    return 144; //Super Buu
                case 60:
                    return 149; //Jiren
                case 61:
                    return 152; //Kefla
                case 255:
                    return -2; //No mentor
                default:
                    return -1;
            }
        }

        public override string ToString()
        {
            if (DisplayName != null) return DisplayName;
            return base.ToString();
        }

        public static MentorProgress GetNull()
        {
            return new MentorProgress()
            {

            };
        }
    }

#endregion

#region HeroColosseum
    public class MentorCustomizationUnlockFlag
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public bool Flag { get; set; }

        public static List<byte> Write(List<MentorCustomizationUnlockFlag> mentorFlags, List<byte> bytes, int offset, int count)
        {
            //Create bool list
            List<bool> flags = new List<bool>();

            foreach (var flag in mentorFlags)
            {
                flags.Add(flag.Flag);
            }

            BitArray bitFlags = new BitArray(flags.ToArray());
            byte[] flagBytes = Utils.ConvertToByteArray(bitFlags, 28 * count);

            if (flagBytes.Length != 28 * count) throw new InvalidDataException("MentorCustomizationUnlockFlag Collection is an invalid size.");

            bytes = Utils.ReplaceRange(bytes, flagBytes, offset);
            return bytes;
        }

        public static List<MentorCustomizationUnlockFlag> Read(List<byte> bytes, int offset, int count)
        {
            List<MentorCustomizationUnlockFlag> mentorFlags = new List<MentorCustomizationUnlockFlag>();
            BitArray flags = new BitArray(bytes.GetRange(offset, 28 * count).ToArray());

            int i = 0;
            foreach(bool flag in flags)
            {
                mentorFlags.Add(new MentorCustomizationUnlockFlag()
                {
                    Flag = flag,
                    Index = i
                });
                i++;
            }

            return mentorFlags;
        }

    }

    public class HeroColosseum : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //General values
        private int _masterXp = 0;
        private int _numOfFiguresDefeated = 0;
        private int _maxDamageFrom1Atk = 0;
        private byte _level = 0;

        [YAXAttributeFor("MasterXp")]
        [YAXSerializeAs("value")]
        public int MasterXp
        {
            get
            {
                return this._masterXp;
            }

            set
            {
                if (value != this._masterXp)
                {
                    this._masterXp = value;
                    NotifyPropertyChanged("MasterXp");
                }
            }
        }
        [YAXAttributeFor("NumFiguresDefeated")]
        [YAXSerializeAs("value")]
        public int NumFiguresDefeated
        {
            get
            {
                return this._numOfFiguresDefeated;
            }

            set
            {
                if (value != this._numOfFiguresDefeated)
                {
                    this._numOfFiguresDefeated = value;
                    NotifyPropertyChanged("NumFiguresDefeated");
                }
            }
        }
        [YAXAttributeFor("MaxDamageFrom1Attack")]
        [YAXSerializeAs("value")]
        public int MaxDamageFrom1Attack
        {
            get
            {
                return this._maxDamageFrom1Atk;
            }

            set
            {
                if (value != this._maxDamageFrom1Atk)
                {
                    this._maxDamageFrom1Atk = value;
                    NotifyPropertyChanged("MaxDamageFrom1Attack");
                }
            }
        }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public byte Level
        {
            get
            {
                return this._level;
            }

            set
            {
                if (value != this._level)
                {
                    this._level = value;
                    NotifyPropertyChanged("Level");
                }
            }
        }

        //Filtered views
        private ListCollectionView _viewStoryBattles = null;
        [YAXDontSerialize]
        public ListCollectionView ViewStoryBattles
        {
            get
            {
                if (_viewStoryBattles != null)
                {
                    return _viewStoryBattles;
                }
                _viewStoryBattles = new ListCollectionView(StoryBattles);
                _viewStoryBattles.Filter = new Predicate<object>(Contains);
                return _viewStoryBattles;
            }
            set
            {
                if (value != _viewStoryBattles)
                {
                    _viewStoryBattles = value;
                    NotifyPropertyChanged("ViewStoryBattles");
                }
            }
        }
        private ListCollectionView _viewFreeBattles = null;
        [YAXDontSerialize]
        public ListCollectionView ViewFreeBattles
        {
            get
            {
                if (_viewFreeBattles != null)
                {
                    return _viewFreeBattles;
                }
                _viewFreeBattles = new ListCollectionView(FreeBattles);
                _viewFreeBattles.Filter = new Predicate<object>(Contains);
                return _viewFreeBattles;
            }
            set
            {
                if (value != _viewFreeBattles)
                {
                    _viewFreeBattles = value;
                    NotifyPropertyChanged("ViewFreeBattles");
                }
            }
        }

        //Collections
        private ObservableCollection<HCQuest> _storyBattles = null;
        private ObservableCollection<HCQuest> _freeBattles = null;

        public ObservableCollection<HCQuest> StoryBattles
        {
            get
            {
                return this._storyBattles;
            }

            set
            {
                if (value != this._storyBattles)
                {
                    this._storyBattles = value;
                    NotifyPropertyChanged("StoryBattles");
                    NotifyPropertyChanged("ViewStoryBattles");
                }
            }
        }
        public ObservableCollection<HCQuest> FreeBattles
        {
            get
            {
                return this._freeBattles;
            }

            set
            {
                if (value != this._freeBattles)
                {
                    this._freeBattles = value;
                    NotifyPropertyChanged("FreeBattles");
                    NotifyPropertyChanged("ViewFreeBattles");
                }
            }
        }

        public static HeroColosseum Read(byte[] rawBytes, List<byte> bytes, int cacIdx)
        {
            int add = (Offsets.CAC_DLC_SIZE * cacIdx);

            return new HeroColosseum()
            {
                MasterXp = BitConverter.ToInt32(rawBytes, 535448 + add),
                NumFiguresDefeated = BitConverter.ToInt32(rawBytes, 535452 + add),
                MaxDamageFrom1Attack = BitConverter.ToInt32(rawBytes, 535468 + add),
                Level = rawBytes[544689 + add],
                StoryBattles = HCQuest.ReadAll(rawBytes, Offsets.QUESTS_TTQ + add, Offsets.QUESTS_TTQ_COUNT),
                FreeBattles = HCQuest.ReadAll(rawBytes, Offsets.QUESTS_TFB + add, Offsets.QUESTS_TFB_COUNT),
            };
        }

        public List<byte> Write(List<byte> bytes, int cacIdx)
        {
            int add = (Offsets.CAC_DLC_SIZE * cacIdx);

            //General values
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(MasterXp), 535448 + add);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(NumFiguresDefeated), 535452 + add);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(MaxDamageFrom1Attack), 535468 + add);
            bytes[544689 + add] = Level;

            //Collections
            bytes = Utils.ReplaceRange(bytes, HCQuest.WriteAll(StoryBattles, Offsets.QUESTS_TTQ_COUNT).ToArray(), Offsets.QUESTS_TTQ + add);
            bytes = Utils.ReplaceRange(bytes, HCQuest.WriteAll(FreeBattles, Offsets.QUESTS_TFB_COUNT).ToArray(), Offsets.QUESTS_TFB + add);

            return bytes;
        }

        public bool Contains(object quest)
        {
            var ttlQuest = quest as HCQuest;

            if (ttlQuest != null)
            {
                return !ttlQuest.Hidden;
            }

            return false;
        }
        
    }

    public class HCQuest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ushort _masterXp = 0;
        private ushort _figureXp = 0;

        [YAXDontSerialize]
        public int SortedID { get; set; }
        private bool _hidden = false;
        [YAXDontSerialize]
        public bool Hidden
        {
            get
            {
                return this._hidden;
            }

            set
            {
                if (value != this._hidden)
                {
                    this._hidden = value;
                    NotifyPropertyChanged("Hidden");
                }
            }
        }
        [YAXDontSerialize]
        public string DisplayName { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("MasterXp")]
        public ushort I_00
        {
            get
            {
                return this._masterXp;
            }

            set
            {
                if (value != this._masterXp)
                {
                    this._masterXp = value;
                    NotifyPropertyChanged("I_00");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("FigureXp")]
        public ushort I_02
        {
            get
            {
                return this._figureXp;
            }

            set
            {
                if (value != this._figureXp)
                {
                    this._figureXp = value;
                    NotifyPropertyChanged("I_02");
                }
            }
        }
        private byte _rankValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Rank")]
        public byte I_04
        {
            get
            {
                return this._rankValue;
            }

            set
            {
                if (value != this._rankValue)
                {
                    this._rankValue = value;
                    NotifyPropertyChanged("I_04");
                }
            }
        }
        private byte _stateValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("State")]
        public byte I_05
        {
            get
            {
                return this._stateValue;
            }

            set
            {
                if (value != this._stateValue)
                {
                    this._stateValue = value;
                    NotifyPropertyChanged("I_05");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        public byte I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_07")]
        public byte I_07 { get; set; }

        public static HCQuest Read(byte[] rawBytes, int offset)
        {
            return new HCQuest()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = rawBytes[offset + 4],
                I_05 = rawBytes[offset + 5],
                I_06 = rawBytes[offset + 6],
                I_07 = rawBytes[offset + 7]
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.Add(I_04);
            bytes.Add(I_05);
            bytes.Add(I_06);
            bytes.Add(I_07);

            if (bytes.Count != 8) throw new InvalidDataException("HCQuest is an invalid size.");
            return bytes;
        }

        public static ObservableCollection<HCQuest> ReadAll(byte[] rawBytes, int offset, int count)
        {
            ObservableCollection<HCQuest> quests = new ObservableCollection<HCQuest>();

            for (int i = 0; i < count; i++)
            {
                quests.Add(Read(rawBytes, offset));
                offset += 8;
            }

            return quests;
        }

        public static List<byte> WriteAll(ObservableCollection<HCQuest> quests, int count)
        {
            //Add padding entries if needed
            if (quests.Count != count)
            {
                while (quests.Count != count)
                {
                    quests.Add(new HCQuest());
                }
            }

            //Write data
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(quests[i].Write());
            }

            if (bytes.Count != 8 * count) throw new InvalidDataException("HCQuest collection is an invalid size.");
            return bytes;
        }



    }

    public class HeroColosseumGlobal : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //General values
        private int _figureXp = 0;
        private ushort _extrafigureSlots = 0;
        private byte _extraDeckSlots = 0;

        [YAXAttributeFor("FigureXp")]
        [YAXSerializeAs("value")]
        public int I_04
        {
            get
            {
                return this._figureXp;
            }

            set
            {
                if (value != this._figureXp)
                {
                    this._figureXp = value;
                    NotifyPropertyChanged("I_04");
                }
            }
        }
        [YAXAttributeFor("ExtraFigureSlots")]
        [YAXSerializeAs("value")]
        public ushort I_10
        {
            get
            {
                return this._extrafigureSlots;
            }

            set
            {
                if (value != this._extrafigureSlots)
                {
                    this._extrafigureSlots = value;
                    NotifyPropertyChanged("I_10");
                }
            }
        }
        [YAXAttributeFor("ExtraDeckSlots")]
        [YAXSerializeAs("value")]
        public byte I_11
        {
            get
            {
                return this._extraDeckSlots;
            }

            set
            {
                if (value != this._extraDeckSlots)
                {
                    this._extraDeckSlots = value;
                    NotifyPropertyChanged("I_11");
                }
            }
        }

#if SaveEditor
        //Filtered views
        private ListCollectionView _viewFigureCollection = null;
        [YAXDontSerialize]
        public ListCollectionView ViewFigureCollection
        {
            get
            {
                if (OwnedFigureCollection == null) return null;
                if (_viewFigureCollection != null)
                {
                    return _viewFigureCollection;
                }
                _viewFigureCollection = new ListCollectionView(OwnedFigureCollection);
                _viewFigureCollection.Filter = new Predicate<object>(FigureContains);
                return _viewFigureCollection;
            }
            set
            {
                if (value != _viewFigureCollection)
                {
                    _viewFigureCollection = value;
                    NotifyPropertyChanged("ViewFigureCollection");
                }
            }
        }
        private ListCollectionView _viewSkills = null;
        [YAXDontSerialize]
        public ListCollectionView ViewSkills
        {
            get
            {
                if (Skills == null) return null;
                if (_viewSkills != null)
                {
                    return _viewSkills;
                }
                _viewSkills = new ListCollectionView(Skills);
                _viewSkills.Filter = new Predicate<object>(ItemContains);
                return _viewSkills;
            }
            set
            {
                if (value != _viewSkills)
                {
                    _viewSkills = value;
                    NotifyPropertyChanged("ViewSkills");
                }
            }
        }
        private ListCollectionView _viewItems = null;
        [YAXDontSerialize]
        public ListCollectionView ViewItems
        {
            get
            {
                if (Items == null) return null;
                if (_viewItems != null)
                {
                    return _viewItems;
                }
                _viewItems = new ListCollectionView(Items);
                _viewItems.Filter = new Predicate<object>(ItemContains);
                return _viewItems;
            }
            set
            {
                if (value != _viewItems)
                {
                    _viewItems = value;
                    NotifyPropertyChanged("ViewItems");
                }
            }
        }


        private bool FigureContains(object obj)
        {
            FigureCollectionItem fig = obj as FigureCollectionItem;

            if (fig != null)
            {
                if (fig.FigureData == null) return false;
                return fig.FigureData.ValidPlayerFigure;
            }
            return false;
        }

        private bool ItemContains(object obj)
        {
            HCItem item = obj as HCItem;

            if (item != null)
            {
                return item.HasName;
            }
            return false;
        }
#endif

        //Collections
        private ObservableCollection<HCItem> _items = null;
        private ObservableCollection<HCItem> _skills = null;
        private ObservableCollection<FigureCollectionItem> _figureCollection = null;
        private ObservableCollection<Figure> _figures = null;
        private ObservableCollection<Deck> _decks = null;

        public ObservableCollection<HCItem> Items
        {
            get
            {
                return this._items;
            }

            set
            {
                if (value != this._items)
                {
                    this._items = value;
                    NotifyPropertyChanged("Items");
                    NotifyPropertyChanged("ViewItems");
                }
            }
        }
        public ObservableCollection<HCItem> Skills
        {
            get
            {
                return this._skills;
            }

            set
            {
                if (value != this._skills)
                {
                    this._skills = value;
                    NotifyPropertyChanged("Skills");
                    NotifyPropertyChanged("ViewSkills");
                }
            }
        }
        public ObservableCollection<FigureCollectionItem> OwnedFigureCollection
        {
            get
            {
                return this._figureCollection;
            }

            set
            {
                if (value != this._figureCollection)
                {
                    this._figureCollection = value;
                    NotifyPropertyChanged("OwnedFigureCollection");
                    NotifyPropertyChanged("ViewFigureCollection");
                }
            }
        }
        public ObservableCollection<Figure> Figures
        {
            get
            {
                return this._figures;
            }

            set
            {
                if (value != this._figures)
                {
                    this._figures = value;
                    NotifyPropertyChanged("Figures");
                    NotifyPropertyChanged("ViewFigures");
                }
            }
        }
        public ObservableCollection<Deck> Decks
        {
            get
            {
                return this._decks;
            }

            set
            {
                if (value != this._decks)
                {
                    this._decks = value;
                    NotifyPropertyChanged("Decks");
                }
            }
        }

        //Counts
        [YAXDontSerialize]
        public string FigureCount
        {
            get
            {
                if (Figures == null) return String.Format("0/{0}", 100 + I_10);
                return String.Format("{0}/{1}", Figures.Count, 100 + I_10);
            }
        }
        
        public static HeroColosseumGlobal Read(byte[] rawBytes, List<byte> bytes, int version)
        {
            HeroColosseumGlobal hc = new HeroColosseumGlobal();

            //General values
            hc.I_04 = BitConverter.ToInt32(rawBytes, Offsets.HC_GLOBAL_VALUES + 4);
            hc.I_11 = rawBytes[Offsets.HC_GLOBAL_VALUES + 11];

            if(version >= 23)
            {
                //1.17.01
                hc.I_10 = BitConverter.ToUInt16(rawBytes, Offsets.HC_GLOBAL_VALUES + 76);
            }
            else
            {
                hc.I_10 = rawBytes[Offsets.HC_GLOBAL_VALUES + 10];
            }

            //Collections
            hc.Items = HCItem.Read(rawBytes, bytes, Offsets.HC_ITEM_UNLOCKED_FLAGS, Offsets.HC_ITEM_UNKNOWN_FLAGS, Offsets.HC_ITEM_QUANTITY, Offsets.HC_ITEM_COUNT);
            hc.Skills = HCItem.Read(rawBytes, bytes, Offsets.HC_SKILL_UNLOCKED_FLAGS, Offsets.HC_SKILL_UNKNOWN_FLAGS, Offsets.HC_SKILL_QUANTITY, Offsets.HC_SKILL_COUNT);
            hc.OwnedFigureCollection = FigureCollectionItem.Read(rawBytes, bytes);
            hc.Figures = Figure.ReadAll(rawBytes, version);
            hc.Decks = Deck.ReadAll(rawBytes);

            return hc;
        }

        public List<byte> Write(List<byte> bytes, int version)
        {
            //General values
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(I_04), Offsets.HC_GLOBAL_VALUES + 4);
            bytes[Offsets.HC_GLOBAL_VALUES + 11] = I_11;

            //Collections
            bytes = Utils.ReplaceRange(bytes, HCItem.Write(Items, Offsets.HC_ITEM_COUNT).ToArray(), Offsets.HC_ITEM_UNLOCKED_FLAGS);
            bytes = Utils.ReplaceRange(bytes, HCItem.Write(Skills, Offsets.HC_SKILL_COUNT).ToArray(), Offsets.HC_SKILL_UNLOCKED_FLAGS);
            bytes = Utils.ReplaceRange(bytes, FigureCollectionItem.Write(OwnedFigureCollection).ToArray(), Offsets.HC_FIGURE_COLLECTION_UNLOCKED_FLAGS);
            bytes = Utils.ReplaceRange(bytes, Figure.WriteFigureInventory1(Figures).ToArray(), Offsets.HC_FIGURE_INVENTORY);
            bytes = Utils.ReplaceRange(bytes, Deck.WriteAll(Decks).ToArray(), Offsets.HC_DECK);

            if (version >= 23)
            {
                //1.17.01
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(I_10), Offsets.HC_GLOBAL_VALUES + 76);
                bytes = Utils.ReplaceRange(bytes, Figure.WriteFigureInventory2(Figures).ToArray(), Offsets.HC_FIGURE_INVENTORY2);
            }
            else
            {
                bytes[Offsets.HC_GLOBAL_VALUES + 10] = (byte)I_10;
            }

            return bytes;
        }

        public void InitEvents()
        {
            Figures.CollectionChanged += new NotifyCollectionChangedEventHandler(OnFiguresCollectionChanged);
        }

        private void OnFiguresCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("FigureCount");
            NotifyPropertyChanged("CompositeFigureCollection");
        }
        
        public void InitGuids()
        {
            //Set a guid on each figure
            foreach (var figure in Figures)
            {
                figure.FigureGuid = Guid.NewGuid();
            }

            //Link the deck index with the guid
            foreach (var deck in Decks)
            {
                deck.Figure1_Guid = GetFigureGuid(deck.Figure1);
                deck.Figure2_Guid = GetFigureGuid(deck.Figure2);
                deck.Figure3_Guid = GetFigureGuid(deck.Figure3);
                deck.Figure4_Guid = GetFigureGuid(deck.Figure4);
                deck.Figure5_Guid = GetFigureGuid(deck.Figure5);
            }
        }

        public void SaveGuids()
        {
            foreach (var deck in Decks)
            {
                deck.Figure1 = (ushort)GetFigureIndex(deck.Figure1_Guid);
                deck.Figure2 = (ushort)GetFigureIndex(deck.Figure2_Guid);
                deck.Figure3 = (ushort)GetFigureIndex(deck.Figure3_Guid);
                deck.Figure4 = (ushort)GetFigureIndex(deck.Figure4_Guid);
                deck.Figure5 = (ushort)GetFigureIndex(deck.Figure5_Guid);
            }
        }

        private int GetFigureIndex(Guid guid)
        {
            for (int i = 0; i < Figures.Count; i++)
            {
                if (Figures[i].FigureGuid == guid) return i;
            }

            return 65476;
        }

        private Guid GetFigureGuid(int index)
        {
            foreach (var figure in Figures)
            {
                if (figure.Index == index) return figure.FigureGuid;
            }
            return new Guid();
        }

        public void SetDeckFlags()
        {
            //Manually set all DeckFlags

            foreach (var figure in Figures)
            {
                DeckFlags flags = 0;
                if (Decks[0].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck1;
                if (Decks[1].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck2;
                if (Decks[2].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck3;
                if (Decks[3].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck4;
                if (Decks[4].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck5;
                if (Decks[5].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck6;
                if (Decks[6].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck7;
                if (Decks[7].IsInDeck(figure.FigureGuid)) flags = flags | DeckFlags.Deck8;
                figure.I_30 = flags;
            }

        }

    }

    public class HCItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private byte _quantity = 0;
        private bool _unlocked = false;
        private bool _unknown = false;
        private string _name = null;
        private bool _hasName = false;

        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public byte Quantity
        {
            get
            {
                return this._quantity;
            }

            set
            {
                if (value != this._quantity)
                {
                    this._quantity = value;
                    NotifyPropertyChanged("Quantity");
                }
            }
        }

        [YAXAttributeForClass]
        public bool Unlocked
        {
            get
            {
                return this._unlocked;
            }

            set
            {
                if (value != this._unlocked)
                {
                    this._unlocked = value;
                    NotifyPropertyChanged("Unlocked");
                }
            }
        }

        [YAXAttributeForClass]
        public bool Unknown
        {
            get
            {
                return this._unknown;
            }

            set
            {
                if (value != this._unknown)
                {
                    this._unknown = value;
                    NotifyPropertyChanged("Unknown");
                }
            }
        }

#if SaveEditor
        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXDontSerialize]
        public bool HasName
        {
            get
            {
                return this._hasName;
            }

            set
            {
                if (value != this._hasName)
                {
                    this._hasName = value;
                    NotifyPropertyChanged("HasName");
                }
            }
        }
        [YAXDontSerialize]
        public LB_Save_Editor.ID.HCItem ItemData { get; set; }
        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                if (ItemData == null) return Name;
                if (ItemData.CanPlayerUse) return Name;
                return String.Format("{0}*", Name);
            }
        }
#endif

        [YAXDontSerialize]
        public int SortId
        {
            get
            {
                return Index;
            }
        }


        public static ObservableCollection<HCItem> Read(byte[] rawBytes, List<byte> bytes, int flag1Offset, int flag2Offset, int quantityOffset, int count)
        {
            BitArray flag1 = new BitArray(bytes.GetRange(flag1Offset, count / 8).ToArray());
            BitArray flag2 = new BitArray(bytes.GetRange(flag2Offset, count / 8).ToArray());
            ObservableCollection<HCItem> items = new ObservableCollection<HCItem>();

            for (int i = 0; i < count; i++)
            {
                items.Add(new HCItem()
                {
                    Quantity = rawBytes[quantityOffset + i],
                    Unknown = flag2[i],
                    Unlocked = flag1[i],
                    Index = i
                });
            }

            return items;
        }

        public static List<byte> Write(ObservableCollection<HCItem> items, int count)
        {
            List<byte> bytes = new List<byte>();

            //Create boolean lists
            List<bool> flag1 = new List<bool>();
            List<bool> flag2 = new List<bool>();

            foreach (var item in items)
            {
                flag1.Add(item.Unlocked);
                flag2.Add(item.Unknown);
            }

            BitArray flag1Bits = new BitArray(flag1.ToArray());
            BitArray flag2Bits = new BitArray(flag2.ToArray());

            //Write data
            bytes.AddRange(Utils.ConvertToByteArray(flag1Bits, count / 8));
            bytes.AddRange(Utils.ConvertToByteArray(flag2Bits, count / 8));

            foreach (var item in items)
            {
                if (item.Quantity > 99) item.Quantity = 99;
                if (item.Quantity < 0) item.Quantity = 0;
                bytes.Add(item.Quantity);
            }

            //Validate size and return bytes
            int expectedSize = count + (count / 8 * 2);
            if (bytes.Count != expectedSize) throw new InvalidDataException("HCItem collection is an invalid size.");
            return bytes;
        }

    }

    public class FigureCollectionItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

#if SaveEditor
        public LB_Save_Editor.ID.Figure FigureData { get; set; }

        [YAXDontSerialize]
        public string Rarity
        {
            get
            {
                if (FigureData != null)
                {
                    switch (FigureData.I_05)
                    {
                        case 0:
                            return "N";
                        case 1:
                            return "R";
                        case 2:
                            return "SR";
                        case 3:
                            return "UR";
                        default:
                            return "Unknown";
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }

#endif

        private bool _unlocked = false;
        private bool _new = false;
        private bool _unknown = false;
        private string _name = null;
        private bool _hidden = false;

        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public bool Unlocked
        {
            get
            {
                return this._unlocked;
            }

            set
            {
                if (value != this._unlocked)
                {
                    this._unlocked = value;
                    NotifyPropertyChanged("Unlocked");
                }
            }
        }
        [YAXAttributeForClass]
        public bool New
        {
            get
            {
                return this._new;
            }

            set
            {
                if (value != this._new)
                {
                    this._new = value;
                    NotifyPropertyChanged("New");
                }
            }
        }
        [YAXAttributeForClass]
        public bool Unknown
        {
            get
            {
                return this._unknown;
            }

            set
            {
                if (value != this._unknown)
                {
                    this._unknown = value;
                    NotifyPropertyChanged("Unknown");
                }
            }
        }

        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        [YAXDontSerialize]
        public bool Hidden
        {
            get
            {
                return this._hidden;
            }

            set
            {
                if (value != this._hidden)
                {
                    this._hidden = value;
                    NotifyPropertyChanged("Hidden");
                }
            }
        }

        [YAXDontSerialize]
        public int SortId
        {
            get
            {
                return Index + 1;
            }
        }

        
        public static ObservableCollection<FigureCollectionItem> Read(byte[] rawBytes, List<byte> bytes)
        {
            ObservableCollection<FigureCollectionItem> figures = new ObservableCollection<FigureCollectionItem>();

            BitArray unlockedFlags = new BitArray(bytes.GetRange(Offsets.HC_FIGURE_COLLECTION_UNLOCKED_FLAGS, Offsets.HC_FIGURE_COLLECTION_COUNT / 8).ToArray());
            BitArray unknownFlags = new BitArray(bytes.GetRange(Offsets.HC_FIGURE_COLLECTION_UNKNOWN_FLAGS, Offsets.HC_FIGURE_COLLECTION_COUNT / 8).ToArray());
            BitArray newFlags = new BitArray(bytes.GetRange(Offsets.HC_FIGURE_COLLECTION_NEW_FLAGS, Offsets.HC_FIGURE_COLLECTION_COUNT / 8).ToArray());

            for (int i = 0; i < Offsets.HC_FIGURE_COLLECTION_COUNT; i++)
            {
                figures.Add(new FigureCollectionItem()
                {
                    Index = i,
                    New = newFlags[i],
                    Unknown = unknownFlags[i],
                    Unlocked = unlockedFlags[i]
                });
            }

            return figures;
        }

        public static List<byte> Write(ObservableCollection<FigureCollectionItem> figures)
        {
            List<byte> bytes = new List<byte>();

            //Create boolean lists
            List<bool> unlockedFlags = new List<bool>();
            List<bool> unknownFlags = new List<bool>();
            List<bool> newFlags = new List<bool>();

            foreach (var figure in figures)
            {
                unlockedFlags.Add(figure.Unlocked);
                unknownFlags.Add(figure.Unknown);
                newFlags.Add(figure.New);
            }

            //Add null entries to bring the count up to 1024
            while (unlockedFlags.Count != Offsets.HC_FIGURE_COLLECTION_COUNT)
            {
                unlockedFlags.Add(false);
                unknownFlags.Add(false);
                newFlags.Add(false);
            }

            BitArray unlockedBits = new BitArray(unlockedFlags.ToArray());
            BitArray unknownBits = new BitArray(unknownFlags.ToArray());
            BitArray newBits = new BitArray(newFlags.ToArray());

            //Write data
            bytes.AddRange(Utils.ConvertToByteArray(unlockedBits, Offsets.HC_FIGURE_COLLECTION_COUNT / 8));
            bytes.AddRange(Utils.ConvertToByteArray(unknownBits, Offsets.HC_FIGURE_COLLECTION_COUNT / 8));
            bytes.AddRange(Utils.ConvertToByteArray(newBits, Offsets.HC_FIGURE_COLLECTION_COUNT / 8));

            //Validate size and return bytes
            int expectedSize = Offsets.HC_FIGURE_COLLECTION_COUNT / 8 * 3;
            if (bytes.Count != expectedSize) throw new InvalidDataException("FigureItemCollection list is an invalid size.");
            return bytes;

        }

    }

    public class Figure : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

#if SaveEditor
        [YAXDontSerializeIfNull]
        public LB_Save_Editor.ID.Figure FigureData { get; set; }

        
        [YAXDontSerialize]
        public bool UseSkill2
        {
            get
            {
                if (FigureData == null) return true;
                if (FigureData.I_44 >= 2) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public bool UseSkill3
        {
            get
            {
                if (FigureData == null) return true;
                if (FigureData.I_44 >= 3) return true;
                return false;
            }
        }

        
        [YAXDontSerialize]
        public int HP
        {
            get
            {
                if (FigureData == null) return -1;
                return FigureData.I_08 + FigureGrowth.CalculateLevelBonus(I_34, FigureData.I_08, FigureData.I_12, FigureData.I_05) + (FigureGrowth.HP * I_20);
            }
        }
        [YAXDontSerialize]
        public int ATK
        {
            get
            {
                if (FigureData == null) return -1;
                return FigureData.I_16 + FigureGrowth.CalculateLevelBonus(I_34, FigureData.I_16, FigureData.I_20, FigureData.I_05) + (FigureGrowth.ATK * I_21);
            }
        }
        [YAXDontSerialize]
        public int DEF
        {
            get
            {
                if (FigureData == null) return -1;
                return FigureData.I_24 + FigureGrowth.CalculateLevelBonus(I_34, FigureData.I_24, FigureData.I_28, FigureData.I_05) + (FigureGrowth.DEF * I_22);
            }
        }
        [YAXDontSerialize]
        public int SPD
        {
            get
            {
                if (FigureData == null) return -1;
                return FigureData.I_32 + FigureGrowth.CalculateLevelBonus(I_34, FigureData.I_32, FigureData.I_36, FigureData.I_05) + (FigureGrowth.SPD * I_23);
            }
        }
        [YAXDontSerialize]
        public string Rarity
        {
            get
            {
                if (FigureData != null)
                {
                    switch (FigureData.I_05)
                    {
                        case 0:
                            return "N";
                        case 1:
                            return "R";
                        case 2:
                            return "SR";
                        case 3:
                            return "UR";
                        default:
                            return "Unknown";
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }
        private int _maxLevelValue = -1;
        [YAXDontSerialize]
        public int MaxLevel
        {
            get
            {
                if (GeneralInfo.HC_IgnoreFigureLevelLimits)
                {
                    return 255;
                }
                else
                {
                    if (_maxLevelValue == -1 && FigureData != null)
                    {
                        _maxLevelValue = FigureGrowth.GetMaxLevel(FigureData.I_05);
                    }
                    return _maxLevelValue;
                }
            }
        }

#endif

        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                return String.Format("No. {2} {1} (Lvl {0}) ", I_34, Name, SortId);
            }
        }
        private Guid _figureGuid = Guid.NewGuid();
        [YAXDontSerialize]
        public Guid FigureGuid
        {
            get
            {
                return this._figureGuid;
            }

            set
            {
                if (value != this._figureGuid)
                {
                    this._figureGuid = value;
                    NotifyPropertyChanged("FigureGuid");
                }
            }
        }
        [YAXDontSerialize]
        public bool InDeck
        {
            get
            {
                return ((int)I_30 != 0);
            }
        }
        
        private byte _I_16_value = 0;
        private byte _I_17_value = 0;
        private byte _I_18_value = 0;
        private byte _I_19_value = 0;
        private byte _I_20_value = 0;
        private byte _I_21_value = 0;
        private byte _I_22_value = 0;
        private byte _I_23_value = 0;
        private ushort _I_24_value = 0;
        private ushort _I_26_value = 0;
        private ushort _I_28_value = 0;
        private DeckFlags _I_30_value = 0;
        private ushort _I_32_value = 0;
        private byte _I_34_value = 0;
        private byte _I_35_value = 0;
        private string _name = null;
        private bool _hidden = false;

        public byte _hp_prev_value = 0;
        public byte _atk_prev_value = 0;
        public byte _def_prev_value = 0;
        public byte _spd_prev_value = 0;


        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXDontSerialize]
        public bool Hidden
        {
            get
            {
                return this._hidden;
            }

            set
            {
                if (value != this._hidden)
                {
                    this._hidden = value;
                    NotifyPropertyChanged("Hidden");
                }
            }
        }
        [YAXDontSerialize]
        public int SortId
        {
            get
            {
                return I_32 + 1;
            }
        }

        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("TimeAcquired")]
        [YAXSerializeAs("value")]
        public DateTime TimeAcquired { get; set; }
        [YAXAttributeFor("AcquiredIndex")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public byte I_16
        {
            get
            {
                return this._I_16_value;
            }

            set
            {
                if (value != this._I_16_value)
                {
                    this._I_16_value = value;
                    NotifyPropertyChanged("I_16");
                }
            }
        }
        [YAXAttributeFor("Combinations")]
        [YAXSerializeAs("Total")]
        public byte I_17
        {
            get
            {
                return this._I_17_value;
            }

            set
            {
                if (value != this._I_17_value)
                {
                    this._I_17_value = value;
                    NotifyPropertyChanged("I_17");
                }
            }
        }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public byte I_18
        {
            get
            {
                return this._I_18_value;
            }

            set
            {
                if (value != this._I_18_value)
                {
                    this._I_18_value = value;
                    NotifyPropertyChanged("I_18");
                }
            }
        }
        [YAXAttributeFor("I_19")]
        [YAXSerializeAs("value")]
        public byte I_19
        {
            get
            {
                return this._I_19_value;
            }

            set
            {
                if (value != this._I_19_value)
                {
                    this._I_19_value = value;
                    NotifyPropertyChanged("I_19");
                }
            }
        }
        [YAXAttributeFor("Combinations")]
        [YAXSerializeAs("HP")]
        public byte I_20
        {
            get
            {
                return this._I_20_value;
            }

            set
            {
                if (value != this._I_20_value)
                {
                    this._I_20_value = value;
                    NotifyPropertyChanged("I_20");
                    NotifyPropertyChanged("HP");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXAttributeFor("Combinations")]
        [YAXSerializeAs("ATK")]
        public byte I_21
        {
            get
            {
                return this._I_21_value;
            }

            set
            {
                if (value != this._I_21_value)
                {
                    this._I_21_value = value;
                    NotifyPropertyChanged("I_21");
                    NotifyPropertyChanged("ATK");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXAttributeFor("Combinations")]
        [YAXSerializeAs("DEF")]
        public byte I_22
        {
            get
            {
                return this._I_22_value;
            }

            set
            {
                if (value != this._I_22_value)
                {
                    this._I_22_value = value;
                    NotifyPropertyChanged("I_22");
                    NotifyPropertyChanged("DEF");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXAttributeFor("Combinations")]
        [YAXSerializeAs("SPD")]
        public byte I_23
        {
            get
            {
                return this._I_23_value;
            }

            set
            {
                if (value != this._I_23_value)
                {
                    this._I_23_value = value;
                    NotifyPropertyChanged("I_23");
                    NotifyPropertyChanged("SPD");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXAttributeFor("Skill_1")]
        [YAXSerializeAs("ID")]
        public ushort I_24
        {
            get
            {
                return this._I_24_value;
            }

            set
            {
                if (value != this._I_24_value)
                {
                    this._I_24_value = value;
                    NotifyPropertyChanged("I_24");
                }
            }
        }
        [YAXAttributeFor("Skill_2")]
        [YAXSerializeAs("ID")]
        public ushort I_26
        {
            get
            {
                return this._I_26_value;
            }

            set
            {
                if (value != this._I_26_value)
                {
                    this._I_26_value = value;
                    NotifyPropertyChanged("I_26");
                }
            }
        }
        [YAXAttributeFor("Skill_3")]
        [YAXSerializeAs("ID")]
        public ushort I_28
        {
            get
            {
                return this._I_28_value;
            }

            set
            {
                if (value != this._I_28_value)
                {
                    this._I_28_value = value;
                    NotifyPropertyChanged("I_28");
                }
            }
        }
        [YAXAttributeFor("DeckFlags")]
        [YAXSerializeAs("value")]
        public DeckFlags I_30 //byte
        {
            get
            {
                return this._I_30_value;
            }

            set
            {
                if (value != this._I_30_value)
                {
                    this._I_30_value = value;
                    NotifyPropertyChanged("I_30");
                }
            }
        }
        [YAXAttributeFor("I_31")]
        [YAXSerializeAs("value")]
        public byte I_31 { get; set; }
        [YAXAttributeFor("FigureID")]
        [YAXSerializeAs("value")]
        public ushort I_32
        {
            get
            {
                return this._I_32_value;
            }

            set
            {
                if (value != this._I_32_value)
                {
                    this._I_32_value = value;
                    NotifyPropertyChanged("I_32");
                }
            }
        }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public byte I_34
        {
            get
            {
                return this._I_34_value;
            }

            set
            {
                if (value != this._I_34_value)
                {
                    this._I_34_value = value;
                    NotifyPropertyChanged("I_34");
                    NotifyPropertyChanged("HP");
                    NotifyPropertyChanged("ATK");
                    NotifyPropertyChanged("DEF");
                    NotifyPropertyChanged("SPD");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXAttributeFor("Locked")]
        [YAXSerializeAs("value")]
        public byte I_35 //byte
        {
            get
            {
                return this._I_35_value;
            }

            set
            {
                if (value != this._I_35_value)
                {
                    this._I_35_value = value;
                    NotifyPropertyChanged("I_35");
                }
            }
        }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }

        public static ObservableCollection<Figure> ReadAll(byte[] rawBytes, int version)
        {
            ObservableCollection<Figure> figures = new ObservableCollection<Figure>();

            int offset = Offsets.HC_FIGURE_INVENTORY;

            //Base figures
            for (int i = 0; i < Offsets.HC_FIGURE_INVENTORY_COUNT; i++)
            {
                if (BitConverter.ToInt64(rawBytes, offset) == 0) break;
                
                var figure = Figure.Read(rawBytes, offset, i);

                if(figure != null)
                    figures.Add(figure); 

                offset += 40;
            }

            //Extra figures added in 1.17.01
            if(version >= 23)
            {
                offset = Offsets.HC_FIGURE_INVENTORY2;

                for (int i = 0; i < Offsets.HC_FIGURE_INVENTORY_COUNT2; i++)
                {
                    if (BitConverter.ToInt64(rawBytes, offset) == 0) break;

                    var figure = Figure.Read(rawBytes, offset, i);

                    if (figure != null)
                        figures.Add(figure);

                    offset += 40;
                }
            }

            return figures;
        }

        public static List<byte> WriteFigureInventory1(ObservableCollection<Figure> figures)
        {
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < figures.Count; i++)
            {
                if (i >= Offsets.HC_FIGURE_INVENTORY_COUNT) break;

                bytes.AddRange(figures[i].Write());
            }

            while (bytes.Count != Offsets.HC_FIGURE_INVENTORY_COUNT * 40)
            {
                bytes.AddRange(new byte[40]);
                if (bytes.Count > Offsets.HC_FIGURE_INVENTORY_COUNT * 40) break;
            }

            if (bytes.Count != Offsets.HC_FIGURE_INVENTORY_COUNT * 40) throw new InvalidDataException("Figure collection is an invalid size.");
            return bytes;
        }

        public static List<byte> WriteFigureInventory2(ObservableCollection<Figure> figures)
        {
            List<byte> bytes = new List<byte>();

            for (int i = Offsets.HC_FIGURE_INVENTORY_COUNT; i < figures.Count; i++)
            {
                bytes.AddRange(figures[i].Write());
            }

            while (bytes.Count != Offsets.HC_FIGURE_INVENTORY_COUNT2 * 40)
            {
                bytes.AddRange(new byte[40]);
                if (bytes.Count > Offsets.HC_FIGURE_INVENTORY_COUNT2 * 40) break;
            }

            if (bytes.Count != Offsets.HC_FIGURE_INVENTORY_COUNT2 * 40) throw new InvalidDataException("Figure collection is an invalid size.");
            return bytes;
        }


        public static Figure Read(byte[] rawBytes, int offset, int index)
        {
            DateTime time;
            try
            {
                time = TimeHelper.ToDateTime(BitConverter.ToUInt64(rawBytes, offset + 0));
            }
            catch
            {
                return null;
            }

            return new Figure()
            {
                Index = index,
                TimeAcquired = time,
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = rawBytes[offset + 16],
                I_17 = rawBytes[offset + 17],
                I_18 = rawBytes[offset + 18],
                I_19 = rawBytes[offset + 19],
                I_20 = rawBytes[offset + 20],
                I_21 = rawBytes[offset + 21],
                I_22 = rawBytes[offset + 22],
                I_23 = rawBytes[offset + 23],
                I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                I_26 = BitConverter.ToUInt16(rawBytes, offset + 26),
                I_28 = BitConverter.ToUInt16(rawBytes, offset + 28),
                I_30 = (DeckFlags)rawBytes[offset + 30],
                I_31 = rawBytes[offset + 31],
                I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                I_34 = rawBytes[offset + 34],
                I_35 = rawBytes[offset + 35],
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                _hp_prev_value = rawBytes[offset + 20],
                _atk_prev_value = rawBytes[offset + 21],
                _def_prev_value = rawBytes[offset + 22],
                _spd_prev_value = rawBytes[offset + 23],

            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            //Automatically set the level limit break flag if level is greater than 60
            if (I_34 > 60 && I_16 == 0)
            {
                I_16 = 1;
            }

            bytes.AddRange(BitConverter.GetBytes(TimeHelper.ToUInt64(TimeAcquired)));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.Add(I_16);
            bytes.Add(I_17);
            bytes.Add(I_18);
            bytes.Add(I_19);
            bytes.Add(I_20);
            bytes.Add(I_21);
            bytes.Add(I_22);
            bytes.Add(I_23);
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_26));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.Add((byte)I_30);
            bytes.Add(I_31);
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.Add(I_34);
            bytes.Add(I_35);
            bytes.AddRange(BitConverter.GetBytes(I_36));

            if (bytes.Count != 40) throw new InvalidDataException("Figure is an invalid size.");
            return bytes;
        }

        public void ResetCombinations()
        {
            I_20 = _hp_prev_value;
            I_21 = _atk_prev_value;
            I_22 = _def_prev_value;
            I_23 = _spd_prev_value;
        }

        public void SaveCombinations()
        {
            _hp_prev_value = I_20;
            _atk_prev_value = I_21;
            _def_prev_value = I_22;
            _spd_prev_value = I_23;
        }

        public static Figure DefaultValues()
        {
            return new Figure()
            {
                TimeAcquired = DateTime.UtcNow,
                I_24 = ushort.MaxValue,
                I_26 = ushort.MaxValue,
                I_28 = ushort.MaxValue,
                I_32 = ushort.MaxValue,
                I_34 = 1
            };
        }
    }

    public class Deck : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ushort _figure1 = 0;
        private ushort _figure2 = 0;
        private ushort _figure3 = 0;
        private ushort _figure4 = 0;
        private ushort _figure5 = 0;
        private Guid _figure1Guid = new Guid();
        private Guid _figure2Guid = new Guid();
        private Guid _figure3Guid = new Guid();
        private Guid _figure4Guid = new Guid();
        private Guid _figure5Guid = new Guid();

        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return String.Format("Deck {0}", (Index + 1).ToString());
            }
        }
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Figure1")]
        [YAXSerializeAs("Index")]
        public ushort Figure1
        {
            get
            {
                return this._figure1;
            }

            set
            {
                if (value != this._figure1)
                {
                    this._figure1 = value;
                    NotifyPropertyChanged("Figure1");
                }
            }
        }
        [YAXAttributeFor("Figure2")]
        [YAXSerializeAs("Index")]
        public ushort Figure2
        {
            get
            {
                return this._figure2;
            }

            set
            {
                if (value != this._figure2)
                {
                    this._figure2 = value;
                    NotifyPropertyChanged("Figure2");
                }
            }
        }
        [YAXAttributeFor("Figure3")]
        [YAXSerializeAs("Index")]
        public ushort Figure3
        {
            get
            {
                return this._figure3;
            }

            set
            {
                if (value != this._figure3)
                {
                    this._figure3 = value;
                    NotifyPropertyChanged("Figure3");
                }
            }
        }
        [YAXAttributeFor("Figure4")]
        [YAXSerializeAs("Index")]
        public ushort Figure4
        {
            get
            {
                return this._figure4;
            }

            set
            {
                if (value != this._figure4)
                {
                    this._figure4 = value;
                    NotifyPropertyChanged("Figure4");
                }
            }
        }
        [YAXAttributeFor("Figure5")]
        [YAXSerializeAs("Index")]
        public ushort Figure5
        {
            get
            {
                return this._figure5;
            }

            set
            {
                if (value != this._figure5)
                {
                    this._figure5 = value;
                    NotifyPropertyChanged("Figure5");
                }
            }
        }

        [YAXDontSerialize]
        public Guid Figure1_Guid
        {
            get
            {
                return this._figure1Guid;
            }

            set
            {
                if (value != this._figure1Guid)
                {
                    this._figure1Guid = value;
                    NotifyPropertyChanged("Figure1_Guid");
                }
            }
        }
        [YAXDontSerialize]
        public Guid Figure2_Guid
        {
            get
            {
                return this._figure2Guid;
            }

            set
            {
                if (value != this._figure2Guid)
                {
                    this._figure2Guid = value;
                    NotifyPropertyChanged("Figure2_Guid");
                }
            }
        }
        [YAXDontSerialize]
        public Guid Figure3_Guid
        {
            get
            {
                return this._figure3Guid;
            }

            set
            {
                if (value != this._figure3Guid)
                {
                    this._figure3Guid = value;
                    NotifyPropertyChanged("Figure3_Guid");
                }
            }
        }
        [YAXDontSerialize]
        public Guid Figure4_Guid
        {
            get
            {
                return this._figure4Guid;
            }

            set
            {
                if (value != this._figure4Guid)
                {
                    this._figure4Guid = value;
                    NotifyPropertyChanged("Figure4_Guid");
                }
            }
        }
        [YAXDontSerialize]
        public Guid Figure5_Guid
        {
            get
            {
                return this._figure5Guid;
            }

            set
            {
                if (value != this._figure5Guid)
                {
                    this._figure5Guid = value;
                    NotifyPropertyChanged("Figure5_Guid");
                }
            }
        }

        public static ObservableCollection<Deck> ReadAll(byte[] rawBytes)
        {
            int offset = Offsets.HC_DECK;
            ObservableCollection<Deck> decks = new ObservableCollection<Deck>();

            for (int i = 0; i < 8; i++)
            {
                decks.Add(Deck.Read(rawBytes, offset, i));
                offset += 10;
            }

            return decks;
        }

        public static List<byte> WriteAll(ObservableCollection<Deck> decks)
        {
            if (decks.Count != 8) throw new InvalidDataException("Expected 8 decks.");
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < decks.Count; i++)
            {
                bytes.AddRange(decks[i].Write());
            }

            if (bytes.Count != 10 * 8) throw new InvalidDataException("Deck collection is an invalid size.");
            return bytes;
        }

        public static Deck Read(byte[] rawBytes, int offset, int i)
        {
            return new Deck()
            {
                Index = i,
                Figure1 = BitConverter.ToUInt16(rawBytes, offset + 0),
                Figure2 = BitConverter.ToUInt16(rawBytes, offset + 2),
                Figure3 = BitConverter.ToUInt16(rawBytes, offset + 4),
                Figure4 = BitConverter.ToUInt16(rawBytes, offset + 6),
                Figure5 = BitConverter.ToUInt16(rawBytes, offset + 8),
            };
        }

        public List<byte> Write()
        {
            ValidateIds();
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(Figure1));
            bytes.AddRange(BitConverter.GetBytes(Figure2));
            bytes.AddRange(BitConverter.GetBytes(Figure3));
            bytes.AddRange(BitConverter.GetBytes(Figure4));
            bytes.AddRange(BitConverter.GetBytes(Figure5));

            if (bytes.Count != 10) throw new InvalidDataException("Deck is an invalid size.");
            return bytes;
        }

        public bool IsInDeck(Guid guid)
        {
            if (Figure1_Guid == guid || Figure2_Guid == guid || Figure3_Guid == guid || Figure4_Guid == guid || Figure5_Guid == guid)
            {
                return true;
            }
            return false;
        }

        private void ValidateIds()
        {
            if (Figure1 == ushort.MaxValue) Figure1 = 65476;
            if (Figure2 == ushort.MaxValue) Figure2 = 65476;
            if (Figure3 == ushort.MaxValue) Figure3 = 65476;
            if (Figure4 == ushort.MaxValue) Figure4 = 65476;
            if (Figure5 == ushort.MaxValue) Figure5 = 65476;
        }
    }
#endregion

    public class SystemFlag
    {
        #if SaveEditor
        [YAXDontSerialize]
        public LB_Save_Editor.ID.SysFlag FlagData { get; set; }
        #endif

        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public bool Flag { get; set; }

        public static ObservableCollection<SystemFlag> LoadAll(List<byte> bytes, int cacIdx)
        {
            int offset = Offsets.SYSTEM_FLAGS + (Offsets.CAC_SIZE * cacIdx);

            BitArray flags = new BitArray(bytes.GetRange(offset, Offsets.SYSTEM_FLAGS_COUNT).ToArray());

            ObservableCollection<SystemFlag> systemFlags = new ObservableCollection<SystemFlag>();

            for (int i = 0; i < flags.Count; i++)
            {
                systemFlags.Add(new SystemFlag()
                {
                    Index = i,
                    Flag = flags[i]
                });
            }

            if (systemFlags.Count != Offsets.SYSTEM_FLAGS_COUNT * 8) throw new InvalidDataException("SystemFlags size is invalid.");
            return systemFlags;
        }

        public static List<byte> Write(List<byte> bytes, ObservableCollection<SystemFlag> sysFlags, int cacIdx)
        {
            int offset = Offsets.SYSTEM_FLAGS + (Offsets.CAC_SIZE * cacIdx);

            //Create bool list
            List<bool> flags = new List<bool>();

            foreach (var flag in sysFlags)
            {
                flags.Add(flag.Flag);
            }

            BitArray bitFlags = new BitArray(flags.ToArray());
            byte[] flagBytes = Utils.ConvertToByteArray(bitFlags, Offsets.SYSTEM_FLAGS_COUNT);

            if (flagBytes.Length != Offsets.SYSTEM_FLAGS_COUNT) throw new InvalidDataException("SysFlags Collection is an invalid size.");

            bytes = Utils.ReplaceRange(bytes, flagBytes, offset);

            return bytes;
        }
    }

#region Xv1Hero
    //XV1 Hero
    public class Xv1Hero : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [YAXDontSerialize]
        public bool ImportEnabled
        {
            get
            {
                if (HeroType == 1) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public Visibility Preset2Enabled
        {
            get
            {
                if (HeroType == 2) return Visibility.Visible;
                return Visibility.Hidden;
            }
        }
        [YAXDontSerialize]
        public Visibility Preset3Enabled
        {
            get
            {
                if (HeroType == 3) return Visibility.Visible;
                return Visibility.Hidden;
            }
        }
        [YAXDontSerialize]
        public Visibility Preset2or3Enabled
        {
            get
            {
                if (HeroType == 3 || HeroType == 2) return Visibility.Visible;
                return Visibility.Hidden;
            }
        }


        private ushort _heroType = 0;
        private ushort _selectedXv1Import = 0;
        private ushort _selectedPreset2 = 0;
        private ushort _selectedPreset3 = 0;
        private Xv1HeroImport _importedHero0 = null;

        [YAXAttributeForClass]
        public ushort HeroType
        {
            get
            {
                return this._heroType;
            }

            set
            {
                if (value != this._heroType)
                {
                    this._heroType = value;
                    NotifyPropertyChanged("HeroType");
                    NotifyPropertyChanged("ImportEnabled");
                    NotifyPropertyChanged("Preset2Enabled");
                    NotifyPropertyChanged("Preset3Enabled");
                    NotifyPropertyChanged("Preset2or3Enabled");
                }
            }
        }
        [YAXDontSerialize]
        public ushort SelectedCharacter
        {
            get
            {
                switch (HeroType)
                {
                    case 1:
                        return 0; //Always put the imported hero at index 0
                    case 2:
                        return _selectedPreset2;
                    case 3:
                        return _selectedPreset3;
                }
                return 0;
            } 
        }

        [YAXAttributeForClass]
        public ushort SelectedImport
        {
            get
            {
                return this._selectedXv1Import;
            }

            set
            {
                if (value != this._selectedXv1Import)
                {
                    this._selectedXv1Import = value;
                    NotifyPropertyChanged("SelectedImport");
                }
            }
        }
        [YAXAttributeForClass]
        public ushort SelectedPreset2
        {
            get
            {
                return this._selectedPreset2;
            }

            set
            {
                if (value != this._selectedPreset2)
                {
                    this._selectedPreset2 = value;
                    NotifyPropertyChanged("SelectedPreset2");
                }
            }
        }
        [YAXAttributeForClass]
        public ushort SelectedPreset3
        {
            get
            {
                return this._selectedPreset3;
            }

            set
            {
                if (value != this._selectedPreset3)
                {
                    this._selectedPreset3 = value;
                    NotifyPropertyChanged("SelectedPreset3");
                }
            }
        }

        public Xv1HeroImport ImportedHero0
        {
            get
            {
                return this._importedHero0;
            }

            set
            {
                if (value != this._importedHero0)
                {
                    this._importedHero0 = value;
                    NotifyPropertyChanged("ImportedHero0");
                }
            }
        }

        public static Xv1Hero Read(byte[] rawBytes, List<byte> bytes)
        {
            Xv1Hero hero = new Xv1Hero();

            hero.HeroType = BitConverter.ToUInt16(rawBytes, 68);

            switch (hero.HeroType)
            {
                case 1:
                    hero.SelectedImport = BitConverter.ToUInt16(rawBytes, 70);
                    break;
                case 2:
                    hero.SelectedPreset2 = BitConverter.ToUInt16(rawBytes, 70);
                    break;
                case 3:
                    hero.SelectedPreset3 = BitConverter.ToUInt16(rawBytes, 70);
                    break;
            }
            
            hero.ImportedHero0 = Xv1HeroImport.Read(rawBytes, bytes, hero.SelectedImport);

            return hero;
        }

        public List<byte> Write(List<byte> bytes)
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(HeroType), 68);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SelectedCharacter), 70);
            bytes = ImportedHero0.Write(bytes, 0);
            return bytes;
        }
        
    }

    public class Xv1HeroImport : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name = null;
        private Race _race = Race.HUM;

        [YAXDontSerialize]
        public string RaceName
        {
            get
            {
                string name = "";
                CaC.RaceEnumDictionary.TryGetValue(Race, out name);
                return name;
            }
        }

        [YAXAttributeFor("Race")]
        [YAXSerializeAs("value")]
        public Race Race
        {
            get
            {
                return this._race;
            }

            set
            {
                if (value != this._race)
                {
                    this._race = value;
                    NotifyPropertyChanged("Race");
                }
            }
        }
        [YAXAttributeFor("Voice")]
        [YAXSerializeAs("value")]
        public int Voice { get; set; }
        [YAXAttributeFor("BodySize")]
        [YAXSerializeAs("value")]
        public int BodySize { get; set; }
        [YAXAttributeFor("SkinColor1")]
        [YAXSerializeAs("value")]
        public ushort SkinColor1 { get; set; }
        [YAXAttributeFor("SkinColor2")]
        [YAXSerializeAs("value")]
        public ushort SkinColor2 { get; set; }
        [YAXAttributeFor("SkinColor3")]
        [YAXSerializeAs("value")]
        public ushort SkinColor3 { get; set; }
        [YAXAttributeFor("SkinColor4")]
        [YAXSerializeAs("value")]
        public ushort SkinColor4 { get; set; }
        [YAXAttributeFor("HairColor")]
        [YAXSerializeAs("value")]
        public ushort HairColor { get; set; }
        [YAXAttributeFor("EyeColor")]
        [YAXSerializeAs("value")]
        public ushort EyeColor { get; set; }
        [YAXAttributeFor("MakeupColor1")]
        [YAXSerializeAs("value")]
        public ushort MakeupColor1 { get; set; }
        [YAXAttributeFor("MakeupColor2")]
        [YAXSerializeAs("value")]
        public ushort MakeupColor2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXAttributeFor("FaceBase")]
        [YAXSerializeAs("value")]
        public int FaceBase { get; set; }
        [YAXAttributeFor("FaceForehead")]
        [YAXSerializeAs("value")]
        public int FaceForehead { get; set; }
        [YAXAttributeFor("Eyes")]
        [YAXSerializeAs("value")]
        public int Eyes { get; set; }
        [YAXAttributeFor("Nose")]
        [YAXSerializeAs("value")]
        public int Nose { get; set; }
        [YAXAttributeFor("Ears")]
        [YAXSerializeAs("value")]
        public int Ears { get; set; }
        [YAXAttributeFor("Hair")]
        [YAXSerializeAs("value")]
        public int Hair { get; set; }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public int Level { get; set; }
        [YAXAttributeFor("Experience")]
        [YAXSerializeAs("value")]
        public int Experience { get; set; }
        [YAXAttributeFor("AttributePoints")]
        [YAXSerializeAs("value")]
        public int AttributePoints { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("HEA")]
        public int HEA { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("KI")]
        public int KI { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("ATK")]
        public int ATK { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("STR")]
        public int STR { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("BLA")]
        public int BLA { get; set; }
        [YAXAttributeFor("Stats")]
        [YAXSerializeAs("STM")]
        public int STM { get; set; }
        [YAXAttributeFor("Top")]
        [YAXSerializeAs("value")]
        public int Top { get; set; }
        [YAXAttributeFor("Bottom")]
        [YAXSerializeAs("value")]
        public int Bottom { get; set; }
        [YAXAttributeFor("Gloves")]
        [YAXSerializeAs("value")]
        public int Gloves { get; set; }
        [YAXAttributeFor("Shoes")]
        [YAXSerializeAs("value")]
        public int Shoes { get; set; }
        [YAXAttributeFor("Accessory")]
        [YAXSerializeAs("value")]
        public int Accessory { get; set; }
        [YAXAttributeFor("SuperSoul")]
        [YAXSerializeAs("value")]
        public int SuperSoul { get; set; }
        [YAXAttributeFor("TopColor1")]
        [YAXSerializeAs("value")]
        public ushort TopColor1 { get; set; }
        [YAXAttributeFor("TopColor2")]
        [YAXSerializeAs("value")]
        public ushort TopColor2 { get; set; }
        [YAXAttributeFor("TopColor3")]
        [YAXSerializeAs("value")]
        public ushort TopColor3 { get; set; }
        [YAXAttributeFor("TopColor4")]
        [YAXSerializeAs("value")]
        public ushort TopColor4 { get; set; }
        [YAXAttributeFor("BottomColor1")]
        [YAXSerializeAs("value")]
        public ushort BottomColor1 { get; set; }
        [YAXAttributeFor("BottomColor2")]
        [YAXSerializeAs("value")]
        public ushort BottomColor2 { get; set; }
        [YAXAttributeFor("BottomColor3")]
        [YAXSerializeAs("value")]
        public ushort BottomColor3 { get; set; }
        [YAXAttributeFor("BottomColor4")]
        [YAXSerializeAs("value")]
        public ushort BottomColor4 { get; set; }
        [YAXAttributeFor("GlovesColor1")]
        [YAXSerializeAs("value")]
        public ushort GlovesColor1 { get; set; }
        [YAXAttributeFor("GlovesColor2")]
        [YAXSerializeAs("value")]
        public ushort GlovesColor2 { get; set; }
        [YAXAttributeFor("GlovesColor3")]
        [YAXSerializeAs("value")]
        public ushort GlovesColor3 { get; set; }
        [YAXAttributeFor("GlovesColor4")]
        [YAXSerializeAs("value")]
        public ushort GlovesColor4 { get; set; }
        [YAXAttributeFor("BootsColor1")]
        [YAXSerializeAs("value")]
        public ushort BootsColor1 { get; set; }
        [YAXAttributeFor("BootsColor2")]
        [YAXSerializeAs("value")]
        public ushort BootsColor2 { get; set; }
        [YAXAttributeFor("BootsColor3")]
        [YAXSerializeAs("value")]
        public ushort BootsColor3 { get; set; }
        [YAXAttributeFor("BootsColor4")]
        [YAXSerializeAs("value")]
        public ushort BootsColor4 { get; set; }
        [YAXAttributeFor("SuperSkill1")]
        [YAXSerializeAs("value")]
        public int Super1 { get; set; }
        [YAXAttributeFor("SuperSkill2")]
        [YAXSerializeAs("value")]
        public int Super2 { get; set; }
        [YAXAttributeFor("SuperSkill3")]
        [YAXSerializeAs("value")]
        public int Super3 { get; set; }
        [YAXAttributeFor("SuperSkill4")]
        [YAXSerializeAs("value")]
        public int Super4 { get; set; }
        [YAXAttributeFor("UltimateSkill1")]
        [YAXSerializeAs("value")]
        public int Ultimate1 { get; set; }
        [YAXAttributeFor("UltimateSkill2")]
        [YAXSerializeAs("value")]
        public int Ultimate2 { get; set; }
        [YAXAttributeFor("EvasiveSkill")]
        [YAXSerializeAs("value")]
        public int Evasive { get; set; }
        [YAXAttributeFor("BlastSkill")]
        [YAXSerializeAs("value")]
        public int Blast { get; set; }
        [YAXAttributeFor("AwokenSkill")]
        [YAXSerializeAs("value")]
        public int Awoken { get; set; }

        public static Xv1HeroImport Read(byte[] rawBytes, List<byte> bytes, int index)
        {
            int offset = Offsets.XV1_HERO_SIZE * index;

            return new Xv1HeroImport()
            {
                Race = (Race)BitConverter.ToInt32(rawBytes, offset + 502388),
                Voice = BitConverter.ToInt32(rawBytes, offset + 502392),
                BodySize = BitConverter.ToInt32(rawBytes, offset + 502396),
                SkinColor1 = BitConverter.ToUInt16(rawBytes, offset + 502404),
                SkinColor2 = BitConverter.ToUInt16(rawBytes, offset + 502406),
                SkinColor3 = BitConverter.ToUInt16(rawBytes, offset + 502408),
                SkinColor4 = BitConverter.ToUInt16(rawBytes, offset + 502410),
                HairColor = BitConverter.ToUInt16(rawBytes, offset + 502412),
                EyeColor = BitConverter.ToUInt16(rawBytes, offset + 502414),
                MakeupColor1 = BitConverter.ToUInt16(rawBytes, offset + 502416),
                MakeupColor2 = BitConverter.ToUInt16(rawBytes, offset + 502418),
                Name = StringEx.GetString(bytes, offset + 502420, false, StringEx.EncodingType.ASCII),
                FaceBase = BitConverter.ToInt32(rawBytes, offset + 502484),
                FaceForehead = BitConverter.ToInt32(rawBytes, offset + 502488),
                Eyes = BitConverter.ToInt32(rawBytes, offset + 502492),
                Nose = BitConverter.ToInt32(rawBytes, offset + 502496),
                Ears = BitConverter.ToInt32(rawBytes, offset + 502500),
                Hair = BitConverter.ToInt32(rawBytes, offset + 502504),
                Level = BitConverter.ToInt32(rawBytes, offset + 502524),
                Experience = BitConverter.ToInt32(rawBytes, offset + 502528),
                AttributePoints = BitConverter.ToInt32(rawBytes, offset + 502532),
                HEA = BitConverter.ToInt32(rawBytes, offset + 502536),
                KI = BitConverter.ToInt32(rawBytes, offset + 502540),
                ATK = BitConverter.ToInt32(rawBytes, offset + 502544),
                STR = BitConverter.ToInt32(rawBytes, offset + 502548),
                BLA = BitConverter.ToInt32(rawBytes, offset + 502552),
                STM = BitConverter.ToInt32(rawBytes, offset + 502556),
                Top = BitConverter.ToInt32(rawBytes, offset + 502560),
                Bottom = BitConverter.ToInt32(rawBytes, offset + 502564),
                Gloves = BitConverter.ToInt32(rawBytes, offset + 502568),
                Shoes = BitConverter.ToInt32(rawBytes, offset + 502572),
                Accessory = BitConverter.ToInt32(rawBytes, offset + 502576),
                SuperSoul = BitConverter.ToInt32(rawBytes, offset + 502580),
                TopColor1 = BitConverter.ToUInt16(rawBytes, offset + 502584),
                TopColor2 = BitConverter.ToUInt16(rawBytes, offset + 502586),
                TopColor3 = BitConverter.ToUInt16(rawBytes, offset + 502588),
                TopColor4 = BitConverter.ToUInt16(rawBytes, offset + 502590),
                BottomColor1 = BitConverter.ToUInt16(rawBytes, offset + 502592),
                BottomColor2 = BitConverter.ToUInt16(rawBytes, offset + 502594),
                BottomColor3 = BitConverter.ToUInt16(rawBytes, offset + 502596),
                BottomColor4 = BitConverter.ToUInt16(rawBytes, offset + 502598),
                GlovesColor1 = BitConverter.ToUInt16(rawBytes, offset + 502600),
                GlovesColor2 = BitConverter.ToUInt16(rawBytes, offset + 502602),
                GlovesColor3 = BitConverter.ToUInt16(rawBytes, offset + 502604),
                GlovesColor4 = BitConverter.ToUInt16(rawBytes, offset + 502606),
                BootsColor1 = BitConverter.ToUInt16(rawBytes, offset + 502608),
                BootsColor2 = BitConverter.ToUInt16(rawBytes, offset + 502610),
                BootsColor3 = BitConverter.ToUInt16(rawBytes, offset + 502612),
                BootsColor4 = BitConverter.ToUInt16(rawBytes, offset + 502614),
                Super1 = BitConverter.ToInt32(rawBytes, offset + 502616),
                Super2 = BitConverter.ToInt32(rawBytes, offset + 502620),
                Super3 = BitConverter.ToInt32(rawBytes, offset + 502624),
                Super4 = BitConverter.ToInt32(rawBytes, offset + 502628),
                Ultimate1 = BitConverter.ToInt32(rawBytes, offset + 502632),
                Ultimate2 = BitConverter.ToInt32(rawBytes, offset + 502636),
                Evasive = BitConverter.ToInt32(rawBytes, offset + 502640),
                Blast = BitConverter.ToInt32(rawBytes, offset + 502644),
                Awoken = BitConverter.ToInt32(rawBytes, offset + 502648),


            };
        }

        public List<byte> Write(List<byte> bytes, int index)
        {
            if (Name.Length > 64) throw new Exception("Xv1Hero Name length is greater than the maximum allowed size of 64.");

            int offset = Offsets.XV1_HERO_SIZE * index;

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((int)Race), offset + 502388);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Voice), offset + 502392);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BodySize), offset + 502396);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SkinColor1), offset + 502404);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SkinColor2), offset + 502406);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SkinColor3), offset + 502408);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SkinColor4), offset + 502410);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(HairColor), offset + 502412);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(EyeColor), offset + 502414);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(MakeupColor1), offset + 502416);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(MakeupColor2), offset + 502418);
            bytes = Utils.ReplaceRange(bytes, Utils.GetStringBytes(Name, 64).ToArray(), offset + 502420);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(FaceBase), offset + 502484);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(FaceForehead), offset + 502488);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Eyes), offset + 502492);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Nose), offset + 502496);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Ears), offset + 502500);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Hair), offset + 502504);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Level), offset + 502524);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Experience), offset + 502528);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(AttributePoints), offset + 502532);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(HEA), offset + 502536);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(KI), offset + 502540);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(ATK), offset + 502544);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(STR), offset + 502548);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BLA), offset + 502552);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(STM), offset + 502556);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Top), offset + 502560);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Bottom), offset + 502564);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Gloves), offset + 502568);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Shoes), offset + 502572);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Accessory), offset + 502576);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(SuperSoul), offset + 502580);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TopColor1), offset + 502584);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TopColor2), offset + 502586);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TopColor3), offset + 502588);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(TopColor4), offset + 502590);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BottomColor1), offset + 502592);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BottomColor2), offset + 502594);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BottomColor3), offset + 502596);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BottomColor4), offset + 502598);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(GlovesColor1), offset + 502600);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(GlovesColor2), offset + 502602);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(GlovesColor3), offset + 502604);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(GlovesColor4), offset + 502606);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BootsColor1), offset + 502608);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BootsColor2), offset + 502610);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BootsColor3), offset + 502612);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(BootsColor4), offset + 502614);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Super1), offset + 502616);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Super2), offset + 502620);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Super3), offset + 502624);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Super4), offset + 502628);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Ultimate1), offset + 502632);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Ultimate2), offset + 502636);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Evasive), offset + 502640);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Blast), offset + 502644);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Awoken), offset + 502648);

            return bytes;
        }

        public static Xv1HeroImport ConvertToXv1HeroImport(CaC cac)
        {
            return new Xv1HeroImport()
            {
                Race = cac.I_20,
                Voice = cac.I_24,
                BodySize = cac.Appearence.I_28,
                SkinColor1 = cac.Appearence.I_36,
                SkinColor2 = cac.Appearence.I_38,
                SkinColor3 = cac.Appearence.I_40,
                SkinColor4 = cac.Appearence.I_42,
                HairColor = cac.Appearence.I_44,
                EyeColor = cac.Appearence.I_46,
                MakeupColor1 = cac.Appearence.I_48,
                MakeupColor2 = cac.Appearence.I_50,
                Name = cac.Name,
                FaceBase = cac.Appearence.I_132,
                FaceForehead = cac.Appearence.I_136,
                Eyes = cac.Appearence.I_140,
                Nose = cac.Appearence.I_144,
                Ears = cac.Appearence.I_148,
                Hair = cac.Appearence.I_152,
                Level = cac.I_172,
                Experience = cac.I_176,
                AttributePoints = cac.I_180,
                HEA = cac.I_184,
                KI = cac.I_188,
                ATK = cac.I_192,
                STR = cac.I_196,
                BLA = cac.I_200,
                STM = cac.I_204,
                Top = cac.Presets[0].I_00,
                Bottom = cac.Presets[0].I_04,
                Gloves = cac.Presets[0].I_08,
                Shoes = cac.Presets[0].I_12,
                Accessory = cac.Presets[0].I_16,
                SuperSoul = cac.Presets[0].I_20,
                TopColor1 = cac.Presets[0].I_28,
                TopColor2 = cac.Presets[0].I_30,
                TopColor3 = cac.Presets[0].I_32,
                TopColor4 = cac.Presets[0].I_34,
                BottomColor1 = cac.Presets[0].I_36,
                BottomColor2 = cac.Presets[0].I_38,
                BottomColor3 = cac.Presets[0].I_40,
                BottomColor4 = cac.Presets[0].I_42,
                GlovesColor1 = cac.Presets[0].I_44,
                GlovesColor2 = cac.Presets[0].I_46,
                GlovesColor3 = cac.Presets[0].I_48,
                GlovesColor4 = cac.Presets[0].I_50,
                BootsColor1 = cac.Presets[0].I_52,
                BootsColor2 = cac.Presets[0].I_54,
                BootsColor3 = cac.Presets[0].I_56,
                BootsColor4 = cac.Presets[0].I_58,
                Super1 = cac.Presets[0].I_60,
                Super2 = cac.Presets[0].I_64,
                Super3 = cac.Presets[0].I_68,
                Super4 = cac.Presets[0].I_72,
                Ultimate1 = cac.Presets[0].I_76,
                Ultimate2 = cac.Presets[0].I_80,
                Evasive = cac.Presets[0].I_84,
                Blast = cac.Presets[0].I_88,
                Awoken = cac.Presets[0].I_92
            };
        }

#if SaveEditor
        public static Xv1HeroImport ConvertToXv1HeroImport(Xv1SavFile.CaC cac)
        {
            return new Xv1HeroImport()
            {
                Race = cac.Race,
                Voice = cac.Voice,
                BodySize = cac.BodySize,
                Name = cac.Name,
                FaceBase = cac.FaceBase,
                FaceForehead = cac.FaceForehead,
                Accessory = cac.Accessory,
                ATK = cac.ATK,
                AttributePoints = cac.AttributePoints,
                BLA = cac.BLA,
                Ears = cac.Ears,
                Evasive = cac.EvasiveSkill,
                Experience = cac.Experience,
                KI = cac.KI,
                BootsColor1 = cac.ShoesColor1,
                BootsColor2 = cac.ShoesColor2,
                BootsColor3 = cac.ShoesColor3,
                BootsColor4 = cac.ShoesColor4,
                Top = cac.Top,
                TopColor1 = cac.TopColor1,
                TopColor2 = cac.TopColor2,
                TopColor3 = cac.TopColor3,
                TopColor4 = cac.TopColor4,
                STM = cac.STM,
                Shoes = cac.Shoes,
                Bottom = cac.Bottom,
                BottomColor1 = cac.BottomColor1,
                BottomColor2 = cac.BottomColor2,
                BottomColor3 = cac.BottomColor3,
                BottomColor4 = cac.BottomColor4,
                Eyes = cac.Eyes,
                Gloves = cac.Gloves,
                GlovesColor1 = cac.GlovesColor1,
                GlovesColor2 = cac.GlovesColor2,
                GlovesColor3 = cac.GlovesColor3,
                GlovesColor4 = cac.GlovesColor4,
                Hair = cac.Hair,
                HEA = cac.HEA,
                Level = cac.Level,
                Nose = cac.Nose,
                STR = cac.STR,
                Super1 = cac.SuperSkill1,
                Super2 = cac.SuperSkill2,
                Super3 = cac.SuperSkill3,
                Super4 = cac.SuperSkill4,
                Ultimate1 = cac.UltimateSkill1,
                Ultimate2 = cac.UltimateSkill2,
                Awoken = -1,
                Blast = -1,
                SuperSoul = -1, 
                SkinColor1 = cac.SkinColor1,
                SkinColor2 = cac.SkinColor2,
                SkinColor3 = cac.SkinColor3,
                SkinColor4 = cac.SkinColor4,
                HairColor = cac.HairColor,
                EyeColor = cac.EyeColor,
                MakeupColor1 = cac.MakeupColor1,
                MakeupColor2 = cac.MakeupColor2
            };

        }
#endif
    }

#endregion

    //Equipment Flags

    public class EquipmentFlag
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public bool New { get; set; }
        [YAXAttributeForClass]
        public bool Acquired { get; set; }

        public static List<EquipmentFlag> Load(List<byte> bytes, EquipmentType equipType)
        {
            int newOffset = 0;
            int acquireOffset = 0;

            switch (equipType)
            {
                case EquipmentType.Top:
                    newOffset = Offsets.TOP_NEW_FLAG;
                    acquireOffset = Offsets.TOP_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Bottom:
                    newOffset = Offsets.BOTTOM_NEW_FLAG;
                    acquireOffset = Offsets.BOTTOM_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Gloves:
                    newOffset = Offsets.GLOVES_NEW_FLAG;
                    acquireOffset = Offsets.GLOVES_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Shoes:
                    newOffset = Offsets.SHOES_NEW_FLAG;
                    acquireOffset = Offsets.SHOES_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Accessory:
                    newOffset = Offsets.ACCESSORY_NEW_FLAG;
                    acquireOffset = Offsets.ACCESSORY_ACQUIRED_FLAG;
                    break;
                case EquipmentType.SuperSoul:
                    newOffset = Offsets.SUPERSOUL_NEW_FLAG;
                    acquireOffset = Offsets.SUPERSOUL_ACQUIRED_FLAG;
                    break;
                case EquipmentType.MixItem:
                    newOffset = Offsets.MIX_NEW_FLAG;
                    acquireOffset = Offsets.MIX_ACQUIRED_FLAG;
                    break;
                case EquipmentType.ImportantItem:
                    newOffset = Offsets.IMPORTANT_NEW_FLAG;
                    acquireOffset = Offsets.IMPORTANT_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Capsule:
                    newOffset = Offsets.CAPSULE_NEW_FLAG;
                    acquireOffset = Offsets.CAPSULE_ACQUIRED_FLAG;
                    break;
            }

            List<EquipmentFlag> flags = new List<EquipmentFlag>();
            BitArray newFlags = new BitArray(bytes.GetRange(newOffset, 64).ToArray());
            BitArray acquireFlags = new BitArray(bytes.GetRange(acquireOffset, 64).ToArray());

            for(int i = 0; i < 512; i++)
            {
                flags.Add(new EquipmentFlag()
                {
                    Index = i,
                    New = !newFlags[i],
                    Acquired = acquireFlags[i]
                });
            }

            return flags;
        }

        public static List<byte> Write(List<byte> bytes, List<EquipmentFlag> flags, EquipmentType equipType)
        {
            int newOffset = 0;
            int acquireOffset = 0;

            switch (equipType)
            {
                case EquipmentType.Top:
                    newOffset = Offsets.TOP_NEW_FLAG;
                    acquireOffset = Offsets.TOP_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Bottom:
                    newOffset = Offsets.BOTTOM_NEW_FLAG;
                    acquireOffset = Offsets.BOTTOM_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Gloves:
                    newOffset = Offsets.GLOVES_NEW_FLAG;
                    acquireOffset = Offsets.GLOVES_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Shoes:
                    newOffset = Offsets.SHOES_NEW_FLAG;
                    acquireOffset = Offsets.SHOES_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Accessory:
                    newOffset = Offsets.ACCESSORY_NEW_FLAG;
                    acquireOffset = Offsets.ACCESSORY_ACQUIRED_FLAG;
                    break;
                case EquipmentType.SuperSoul:
                    newOffset = Offsets.SUPERSOUL_NEW_FLAG;
                    acquireOffset = Offsets.SUPERSOUL_ACQUIRED_FLAG;
                    break;
                case EquipmentType.MixItem:
                    newOffset = Offsets.MIX_NEW_FLAG;
                    acquireOffset = Offsets.MIX_ACQUIRED_FLAG;
                    break;
                case EquipmentType.ImportantItem:
                    newOffset = Offsets.IMPORTANT_NEW_FLAG;
                    acquireOffset = Offsets.IMPORTANT_ACQUIRED_FLAG;
                    break;
                case EquipmentType.Capsule:
                    newOffset = Offsets.CAPSULE_NEW_FLAG;
                    acquireOffset = Offsets.CAPSULE_ACQUIRED_FLAG;
                    break;
                default:
                    throw new Exception("Error in UpdateFlags(). Invalid Type.");
            }

            //Create bool list
            List<bool> newBools = new List<bool>();
            List<bool> acquireBools = new List<bool>();

            foreach (var flag in flags)
            {
                newBools.Add(!flag.New);
                acquireBools.Add(flag.Acquired);
            }

            BitArray newBitFlags = new BitArray(newBools.ToArray());
            BitArray acquireBitFlags = new BitArray(acquireBools.ToArray());

            byte[] newFlagBytes = Utils.ConvertToByteArray(newBitFlags, 64);
            byte[] acquireFlagBytes = Utils.ConvertToByteArray(acquireBitFlags, 64);

            if (newFlagBytes.Length != 64) throw new InvalidDataException("Equipment New Flags is an invalid size.");
            if (acquireFlagBytes.Length != 64) throw new InvalidDataException("Equipment Acquire Flags is an invalid size.");

            bytes = Utils.ReplaceRange(bytes, newFlagBytes, newOffset);
            bytes = Utils.ReplaceRange(bytes, acquireFlagBytes, acquireOffset);

            return bytes;
        }

    }

    //New 1.15 stuff:
    public class MascotFlag : INotifyPropertyChanged
    {
        #region NotifyPropChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region View
        [YAXDontSerialize]
        public int DisplayID { get { return Index + 1; } }
        [YAXDontSerialize]
        public string Name { get; set; }
        [YAXDontSerialize]
        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }
        #endregion

        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public bool Acquired
        {
            get
            {
                return this._acquired;
            }

            set
            {
                if (value != this._acquired)
                {
                    this._acquired = value;
                    NotifyPropertyChanged(nameof(Acquired));
                }
            }
        }

        private bool _acquired = false;

        public static ObservableCollection<MascotFlag> LoadAll(List<byte> bytes)
        {
            ObservableCollection<MascotFlag> flags = new ObservableCollection<MascotFlag>();
            BitArray bits = new BitArray(bytes.GetRange(Offsets.MASCOT_FLAGS_OFFSET, Offsets.MASCOT_COUNT).ToArray());

            for (int i = 0; i < bits.Count; i++)
                flags.Add(new MascotFlag() { Index = i, Acquired = bits[i] });

            return flags;
        }

        public static List<byte> Write(List<byte> bytes, ObservableCollection<MascotFlag> mascotFlags)
        {
            int offset = Offsets.MASCOT_FLAGS_OFFSET;

            //Create bool list
            List<bool> flags = new List<bool>();

            foreach (var flag in mascotFlags)
            {
                flags.Add(flag.Acquired);
            }

            BitArray bitFlags = new BitArray(flags.ToArray());
            byte[] flagBytes = Utils.ConvertToByteArray(bitFlags, Offsets.MASCOT_COUNT);

            if (flagBytes.Length != Offsets.MASCOT_COUNT) throw new InvalidDataException("MascotFlags Collection is an invalid size.");

            bytes = Utils.ReplaceRange(bytes, flagBytes, offset);

            return bytes;
        }
    }

    public class ArtworkFlag : INotifyPropertyChanged
    {
        #region NotifyPropChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region View
        [YAXDontSerialize]
        public int DisplayID { get { return Index + 1; } }
        [YAXDontSerialize]
        public string Name { get; set; }
        [YAXDontSerialize]
        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }
        #endregion

        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public bool Acquired
        {
            get
            {
                return this._acquired;
            }

            set
            {
                if (value != this._acquired)
                {
                    this._acquired = value;
                    NotifyPropertyChanged(nameof(Acquired));
                }
            }
        }

        private bool _acquired = false;

        public static ObservableCollection<ArtworkFlag> LoadAll(List<byte> bytes)
        {
            ObservableCollection<ArtworkFlag> flags = new ObservableCollection<ArtworkFlag>();
            BitArray bits = new BitArray(bytes.GetRange(Offsets.ARTWORK_FLAGS_OFFSET, Offsets.ARTWORK_COUNT).ToArray());

            for (int i = 0; i < bits.Count; i++)
                flags.Add(new ArtworkFlag() { Index = i, Acquired = bits[i] });

            return flags;
        }

        public static List<byte> Write(List<byte> bytes, ObservableCollection<ArtworkFlag> artworkFlags)
        {
            int offset = Offsets.ARTWORK_FLAGS_OFFSET;

            //Create bool list
            List<bool> flags = new List<bool>();

            foreach (var flag in artworkFlags)
            {
                flags.Add(flag.Acquired);
            }

            BitArray bitFlags = new BitArray(flags.ToArray());
            byte[] flagBytes = Utils.ConvertToByteArray(bitFlags, Offsets.ARTWORK_COUNT);

            if (flagBytes.Length != Offsets.ARTWORK_COUNT) throw new InvalidDataException("ArtworkFlags Collection is an invalid size.");

            bytes = Utils.ReplaceRange(bytes, flagBytes, offset);

            return bytes;
        }

    }
}
