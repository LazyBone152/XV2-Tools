using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TTC
{
    public enum TtcEventListType
    {
        TTC_VERSUS_LIST = 0, /* Player faces this char */
        TTC_MINOR_DAMAGE_LIST = 1, /* This char has been damaged (minor damage) */
        TTC_MAJOR_DAMAGE_LIST = 2, /* This char has been damaged (major damage) */
        TTC_PLAYER_KO_ENEMY_LIST = 6, /* This char has ko'd the player */
        TTC_STRONG_ATTACK_DAMAGED_LIST = 7, /* This char has taken damage */
        TTC_POWER_UP_LIST = 8, /* This char is about to power up */
        TTC_START_TALK_LIST = 0xa, /* Start talk (ally) */
        TTC_PLAYER_DAMAGED_LIST = 0xb, /* The player is an ally of this character, and has been damaged by an enemy*/
        TTC_LITTLE_TIME_LIST = 0xc, /* The player is an ally and there is little time */
        TTC_PLAYER_ALLY_KILLED_ENEMY_LIST = 0xd, /* The player is an ally and has killed an anemy */
        TTC_CHALLENGE_LIST = 0xe, /* The player challenges this char. ? */
        TTC_KO_LIST = 0xf, /* This char has been ko'd */
        TTC_ENTERING_LIST = 0x10, /* This char (an enemy?) enters the quest */
        TTC_MASTER_VERSUS_LIST = 0x12, /* This char is the master of the player, and is going to face him */
        TTC_PLAYER_KO_ALLY_LIST = 0x13, /* This char is an ally of the player, and the player has been ko'd by an enemy */
        TTC_FIGHT_SERIOUSLY_LIST = 0x15, /* This char is going to fight seriously from now on */

        TTC_BIGGER_EVENT_P1 = 0x16
    };

    public enum TtcEventCondition
    {
        TTC_DEFAULT_CONDITION = 0,
        TTC_TO_HUMAN_CAC_CONDITION = 1,
        TTC_TO_SAIYAN_CAC_CONDITION = 2,
        TTC_TO_NAMEKIAN_CAC_CONDITION = 3,
        TTC_TO_FREEZER_CAC_CONDITION = 4,
        TTC_TO_MAJIN_CAC_CONDITION = 5,
        TTC_TEACHER_CONDITION = 6,
        TTC_EVENT_CONDITION_MAX = 7
    };

    [YAXSerializeAs("TTC")]
    public class TTC_File
    {
        private const uint TTC_SIGNATURE = 0x43545423;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<TTC_Entry> Entries { get; set; } = new List<TTC_Entry>();

        #region SaveLoad

        public static TTC_File Parse(string path, bool writeXml)
        {
            TTC_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TTC_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static TTC_File Parse(byte[] bytes)
        {
            if (BitConverter.ToUInt32(bytes, 0) != TTC_SIGNATURE) throw new InvalidDataException("TTC signature not found!");
            TTC_File ttc = new TTC_File();

            int count = BitConverter.ToInt32(bytes, 8);
            int offset = BitConverter.ToInt32(bytes, 12) + 16;

            //Parse ttc entries
            for(int i = 0; i < count; i++)
            {
                TTC_Entry entry = new TTC_Entry();
                entry.CmsID = BitConverter.ToInt32(bytes, offset + (16 * i) + 12);

                int numLists = BitConverter.ToInt32(bytes, offset + (16 * i) + 0);
                int dataStart = BitConverter.ToInt32(bytes, offset + (16 * i) + 4) + offset;
                int startIndex = BitConverter.ToInt32(bytes, offset + (16 * i) + 8);

                for(int a = 0; a < numLists; a++)
                {
                    TTC_EventList eventList = new TTC_EventList();
                    eventList.Type = (TtcEventListType)BitConverter.ToInt32(bytes, dataStart + (16 * (startIndex + a)) + 12);

                    int numEvents = BitConverter.ToInt32(bytes, dataStart + (16 * (startIndex + a)) + 0);
                    int eventDataStart = BitConverter.ToInt32(bytes, dataStart + (16 * (startIndex + a)) + 4) + offset;
                    int eventStartIndex = BitConverter.ToInt32(bytes, dataStart + (16 * (startIndex + a)) + 8);

                    for(int b = 0; b < numEvents; b++)
                    {
                        TTC_Event _event = new TTC_Event();
                        

                        _event.Costume = BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 4);
                        _event.Transformation = BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 8);
                        _event.Condition = (TtcEventCondition)BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 16);

                        int nameIdx = BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 20);

                        if (nameIdx != -1 && nameIdx < offset)
                            _event.Name = StringEx.GetString(bytes, nameIdx + 16, false);

                        //validation
                        if (BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 0) != entry.CmsID)
                            throw new InvalidDataException("Warning: CMS ID mismatch!");

                        if (BitConverter.ToInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 12) != (int)eventList.Type)
                            throw new InvalidDataException("Warning: EventList Type mismatch!");

                        if (BitConverter.ToUInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 24) != 0xFFFFFFFF)
                            throw new InvalidDataException("Warning: unk_24 is not 0xFFFFFFFF!");

                        if (BitConverter.ToUInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 28) != 0xFFFFFFFF)
                            throw new InvalidDataException("Warning: unk_28 is not 0xFFFFFFFF!");

                        if (BitConverter.ToUInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 32) != 0xFFFFFFFF)
                            throw new InvalidDataException("Warning: unk_32 is not 0xFFFFFFFF!");

                        if (BitConverter.ToUInt32(bytes, eventDataStart + (40 * (eventStartIndex + b)) + 36) != 0xFFFFFFFF)
                            throw new InvalidDataException("Warning: unk_36 is not 0xFFFFFFFF!");

                        eventList.Events.Add(_event);
                    }

                    entry.EventLists.Add(eventList);
                }

                ttc.Entries.Add(entry);
            }

            return ttc;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .ttc file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(TTC_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (TTC_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the TTC_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            List<byte> ttcEntryBytes = new List<byte>();
            List<string> strings = new List<string>();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)TTC_SIGNATURE)); //signature
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //endianness
            bytes.AddRange(BitConverter.GetBytes((ushort)24));
            bytes.AddRange(BitConverter.GetBytes((uint)Entries.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)0)); //string size

            //TTC Entries
            int dataStartOffsetCalc = 16 * Entries.Count; //Relative to TTC Entry list
            int currentIdx = 0;

            foreach(var entry in Entries)
            {
                ttcEntryBytes.AddRange(BitConverter.GetBytes((entry.EventLists != null) ? entry.EventLists.Count : 0));
                ttcEntryBytes.AddRange(BitConverter.GetBytes(dataStartOffsetCalc));
                ttcEntryBytes.AddRange(BitConverter.GetBytes(currentIdx));
                ttcEntryBytes.AddRange(BitConverter.GetBytes(entry.CmsID));

                currentIdx += (entry.EventLists != null) ? entry.EventLists.Count : 0;
            }


            //TTCEventLists
            currentIdx = 0;
            int dataStartOffset2Calc = dataStartOffsetCalc; //Relative to TTC Entry list as well
            
            foreach (var entry in Entries.Where(x => x.EventLists != null))
                dataStartOffset2Calc += 16 * entry.EventLists.Count;

            foreach (var entry in Entries.Where(x => x.EventLists != null))
            {
                foreach(var list in entry.EventLists)
                {
                    ttcEntryBytes.AddRange(BitConverter.GetBytes((list.Events != null) ? list.Events.Count : 0));
                    ttcEntryBytes.AddRange(BitConverter.GetBytes(dataStartOffset2Calc));
                    ttcEntryBytes.AddRange(BitConverter.GetBytes(currentIdx));
                    ttcEntryBytes.AddRange(BitConverter.GetBytes((int)list.Type));

                    currentIdx += (list.Events != null) ? list.Events.Count : 0;
                }
            }

            //TTC Event
            foreach (var entry in Entries.Where(x => x.EventLists != null))
            {
                foreach (var list in entry.EventLists.Where(x => x.Events != null))
                {
                    foreach(var _event in list.Events)
                    {
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(entry.CmsID));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes((int)list.Type));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes((int)_event.Condition));

                        if(!string.IsNullOrWhiteSpace(_event.Name))
                            ttcEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Name)));
                        else
                            ttcEntryBytes.AddRange(BitConverter.GetBytes((uint)0));

                        //These values are always 0xFFFFFFFF
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
                        ttcEntryBytes.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
                    }
                }
            }

            //strings
            foreach(var str in strings)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(str));
                bytes.Add(0);
            }

            //finalize
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - 16), 12);
            bytes.AddRange(ttcEntryBytes);

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        //util
        private static int AddString(List<string> strList, string str)
        {
            //Add string
            if (!strList.Contains(str))
                strList.Add(str);

            //Calc offset
            int idx = strList.IndexOf(str);
            int totalPrecedingLength = 0;

            for (int i = 0; i < idx; i++)
                totalPrecedingLength += strList[i].Length + 1;

            return totalPrecedingLength;
        }

        #endregion


        internal static string EventListTypeToString(TtcEventListType type)
        {
            switch (type)
            {
                case TtcEventListType.TTC_VERSUS_LIST:
                    return "versus";
                case TtcEventListType.TTC_MINOR_DAMAGE_LIST:
                    return "minor_damage";
                case TtcEventListType.TTC_MAJOR_DAMAGE_LIST:
                    return "major_damage";
                case TtcEventListType.TTC_PLAYER_KO_ENEMY_LIST:
                    return "player_ko_enemy";
                case TtcEventListType.TTC_STRONG_ATTACK_DAMAGED_LIST:
                    return "strong_attack_damaged";
                case TtcEventListType.TTC_POWER_UP_LIST:
                    return "power_up";
                case TtcEventListType.TTC_START_TALK_LIST:
                    return "start_talk";
                case TtcEventListType.TTC_PLAYER_DAMAGED_LIST:
                    return "player_damaged";
                case TtcEventListType.TTC_LITTLE_TIME_LIST:
                    return "little_time";
                case TtcEventListType.TTC_PLAYER_ALLY_KILLED_ENEMY_LIST:
                    return "player_ally_killed_enemy";
                case TtcEventListType.TTC_CHALLENGE_LIST:
                    return "challenge";
                case TtcEventListType.TTC_KO_LIST:
                    return "ko";
                case TtcEventListType.TTC_ENTERING_LIST:
                    return "entering";
                case TtcEventListType.TTC_MASTER_VERSUS_LIST:
                    return "master_versus";
                case TtcEventListType.TTC_PLAYER_KO_ALLY_LIST:
                    return "player_ko_ally";
                case TtcEventListType.TTC_FIGHT_SERIOUSLY_LIST:
                    return "fight_seriously";
                default:
                    throw new InvalidDataException($"Unknown type: {type}");
            }
        }

        internal static TtcEventListType StringToEventListType(string typeStr)
        {
            switch (typeStr)
            {
                case "versus":
                    return TtcEventListType.TTC_VERSUS_LIST;
                case "minor_damage":
                    return TtcEventListType.TTC_MINOR_DAMAGE_LIST;
                case "major_damage":
                    return TtcEventListType.TTC_MAJOR_DAMAGE_LIST;
                case "player_ko_enemy":
                    return TtcEventListType.TTC_PLAYER_KO_ENEMY_LIST;
                case "strong_attack_damaged":
                    return TtcEventListType.TTC_STRONG_ATTACK_DAMAGED_LIST;
                case "power_up":
                    return TtcEventListType.TTC_POWER_UP_LIST;
                case "start_talk":
                    return TtcEventListType.TTC_START_TALK_LIST;
                case "player_damaged":
                    return TtcEventListType.TTC_PLAYER_DAMAGED_LIST;
                case "little_time":
                    return TtcEventListType.TTC_LITTLE_TIME_LIST;
                case "player_ally_killed_enemy":
                    return TtcEventListType.TTC_PLAYER_ALLY_KILLED_ENEMY_LIST;
                case "challenge":
                    return TtcEventListType.TTC_CHALLENGE_LIST;
                case "ko":
                    return TtcEventListType.TTC_KO_LIST;
                case "entering":
                    return TtcEventListType.TTC_ENTERING_LIST;
                case "master_versus":
                    return TtcEventListType.TTC_MASTER_VERSUS_LIST;
                case "player_ko_ally":
                    return TtcEventListType.TTC_PLAYER_KO_ALLY_LIST;
                case "fight_seriously":
                    return TtcEventListType.TTC_FIGHT_SERIOUSLY_LIST;
                default:
                    throw new InvalidDataException($"Unknown type: {typeStr}");
            }

        }

        internal static string ConditionToString(TtcEventCondition condition)
        {
            switch (condition)
            {
                case TtcEventCondition.TTC_DEFAULT_CONDITION:
                    return "default";
                case TtcEventCondition.TTC_TO_SAIYAN_CAC_CONDITION:
                    return "to_saiyan_cac";
                case TtcEventCondition.TTC_TO_NAMEKIAN_CAC_CONDITION:
                    return "to_namekian_cac";
                case TtcEventCondition.TTC_TO_MAJIN_CAC_CONDITION:
                    return "to_majin_cac";
                case TtcEventCondition.TTC_TO_HUMAN_CAC_CONDITION:
                    return "to_human_cac";
                case TtcEventCondition.TTC_TO_FREEZER_CAC_CONDITION:
                    return "to_freezer_cac";
                case TtcEventCondition.TTC_TEACHER_CONDITION:
                    return "teacher";
                default:
                    throw new InvalidDataException($"Unknown condition: {condition}");
            }

        }

        internal static TtcEventCondition StringToCondition(string conditionStr)
        {
            switch (conditionStr)
            {
                case "default":
                    return TtcEventCondition.TTC_DEFAULT_CONDITION;
                case "to_saiyan_cac":
                    return TtcEventCondition.TTC_TO_SAIYAN_CAC_CONDITION;
                case "to_namekian_cac":
                    return TtcEventCondition.TTC_TO_NAMEKIAN_CAC_CONDITION;
                case "to_majin_cac":
                    return TtcEventCondition.TTC_TO_MAJIN_CAC_CONDITION;
                case "to_human_cac":
                    return TtcEventCondition.TTC_TO_HUMAN_CAC_CONDITION;
                case "to_freezer_cac":
                    return TtcEventCondition.TTC_TO_FREEZER_CAC_CONDITION;
                case "teacher":
                    return TtcEventCondition.TTC_TEACHER_CONDITION;
                default:
                    throw new InvalidDataException($"Unknown condition: {conditionStr}");
            }

        }
    }

    [YAXSerializeAs("Entry")]
    public class TTC_Entry : IInstallable
    {
        #region NonSerialized
        [YAXDontSerialize]
        public int CmsID { set { I_12 = value.ToString(); } get { return int.Parse(I_12); } }

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CmsID; } }
        [YAXDontSerialize]
        public string Index { get { return I_12; } set { I_12 = value; } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Cms_ID")]
        public string I_12 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EventList")]
        public List<TTC_EventList> EventLists { get; set; } = new List<TTC_EventList>();
    }

    [YAXSerializeAs("EventList")]
    public class TTC_EventList
    {
        #region NonSerialized
        [YAXDontSerialize]
        public TtcEventListType Type { get { return TTC_File.StringToEventListType(TypeStr); } set { TypeStr = TTC_File.EventListTypeToString(value); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string TypeStr { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Event")]
        public List<TTC_Event> Events { get; set; } = new List<TTC_Event>();
    }

    [YAXSerializeAs("Event")]
    public class TTC_Event
    {
        #region NonSerialized
        [YAXDontSerialize]
        public TtcEventCondition Condition { get { return TTC_File.StringToCondition(ConditionStr); } set { ConditionStr = TTC_File.ConditionToString(value); } }
        #endregion

        [YAXAttributeForClass]
        public string ConditionStr { get; set; } //16

        [YAXAttributeFor("Costume")]
        [YAXSerializeAs("value")]
        public int Costume { get; set; } //4
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public int Transformation { get; set; } //8
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; } //20


    }

}
