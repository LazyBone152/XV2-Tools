using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.TDB
{
    [Flags]
    public enum TriggerSquares
    {
        BackRight = 1,
        BackMiddle = 2,
        BackLeft = 4,
        FrontRight = 8,
        FrontMiddle = 16,
        FrontLeft = 32
    }

    public enum TdbType
    {
        TtlEnemyDataList,
        TtlMasterLevelTable,
        TTLItemList,
        TtlFigureDataList,
        TtlFigurePoseList,
        TTL_skill_list
    }

    [YAXSerializeAs("TDB")]
    public class TDB_File
    {
        public const int SIGNATURE = 1111774243;

        [YAXAttributeForClass]
        public ushort I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TdbType")]
        public TdbType _TdbType { get; set; }

        [YAXDontSerializeIfNull]
        public List<TtlEnemyDataList1> EnemyFigures { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlEnemyDataList2> EnemySets { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlMasterLevelTable> MasterLevels { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlItemList> Items { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlItemList> Skills { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlFigureDataList1> Figures { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlFigureDataList2> LevelingXpRequirements { get; set; }
        [YAXDontSerializeIfNull]
        public List<TtlFigurePoseList> FigurePoseList { get; set; }
        [YAXDontSerializeIfNull]
        public List<TTL_skill_list> PosingSkills { get; set; }
        [YAXDontSerializeIfNull]
        public List<TTL_skill_list> CharacterSkills { get; set; }

        public static TDB_File Parse(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            var file = Parse(rawBytes, GetTdbFileType(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TDB_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static TDB_File Parse(byte[] rawBytes, TdbType tdbType)
        {
            int count1 = BitConverter.ToInt32(rawBytes, 8);
            int offset1 = BitConverter.ToInt32(rawBytes, 12);
            int count2 = BitConverter.ToInt32(rawBytes, 16);
            int offset2 = BitConverter.ToInt32(rawBytes, 20);

            TDB_File tdbFile = new TDB_File();
            tdbFile.I_06 = BitConverter.ToUInt16(rawBytes, 6);
            tdbFile._TdbType = tdbType;

            switch (tdbType)
            {
                case TdbType.TtlEnemyDataList:
                    tdbFile.EnemyFigures = TtlEnemyDataList1.ReadAll(rawBytes, offset1, count1);
                    tdbFile.EnemySets = TtlEnemyDataList2.ReadAll(rawBytes, offset2, count2);
                    break;
                case TdbType.TtlMasterLevelTable:
                    tdbFile.MasterLevels = TtlMasterLevelTable.ReadAll(rawBytes, offset1, count1);
                    break;
                case TdbType.TTLItemList:
                    bool oldVersion = ((offset2 - 32) / count1 == 44) ? true : false;
                    tdbFile.Items = TtlItemList.ReadAll(rawBytes, offset1, count1, oldVersion);
                    tdbFile.Skills = TtlItemList.ReadAll(rawBytes, offset2, count2, oldVersion);
                    break;
                case TdbType.TtlFigureDataList:
                    tdbFile.Figures = TtlFigureDataList1.ReadAll(rawBytes, offset1, count1);
                    tdbFile.LevelingXpRequirements = TtlFigureDataList2.ReadAll(rawBytes, offset2, count2);
                    break;
                case TdbType.TtlFigurePoseList:
                    tdbFile.FigurePoseList = TtlFigurePoseList.ReadAll(rawBytes, offset1, count1);
                    break;
                case TdbType.TTL_skill_list:
                    tdbFile.PosingSkills = TTL_skill_list.ReadAll(rawBytes, offset1, count1);
                    tdbFile.CharacterSkills = TTL_skill_list.ReadAll(rawBytes, offset2, count2);
                    break;
            }

            return tdbFile;
        }

        public static void WriteFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(TDB_File), YAXSerializationOptions.DontSerializeNullObjects);
            var tdbFile = (TDB_File)serializer.DeserializeFromFile(xmlPath);
            var bytes = tdbFile.Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();
            
            //Header
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(new byte[24]); //Fill these in later


            //Section 1
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 12);
            switch (_TdbType)
            {
                case TdbType.TtlEnemyDataList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(EnemyFigures.Count), 8);
                    bytes = TtlEnemyDataList1.WriteAll(bytes, EnemyFigures);
                    break;
                case TdbType.TtlMasterLevelTable:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(MasterLevels.Count), 8);
                    bytes = TtlMasterLevelTable.WriteAll(bytes, MasterLevels);
                    break;
                case TdbType.TTLItemList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Items.Count), 8);
                    bytes = TtlItemList.WriteAll(bytes, Items);
                    break;
                case TdbType.TtlFigureDataList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Figures.Count), 8);
                    bytes = TtlFigureDataList1.WriteAll(bytes, Figures);
                    break;
                case TdbType.TtlFigurePoseList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(FigurePoseList.Count), 8);
                    bytes = TtlFigurePoseList.WriteAll(bytes, FigurePoseList);
                    break;
                case TdbType.TTL_skill_list:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(PosingSkills.Count), 8);
                    bytes = TTL_skill_list.WriteAll(bytes, PosingSkills);
                    break;
            }


            //Section 2
            switch (_TdbType)
            {
                case TdbType.TtlEnemyDataList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(EnemySets.Count), 16);
                    bytes = TtlEnemyDataList2.WriteAll(bytes, EnemySets);
                    break;
                case TdbType.TTLItemList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(Skills.Count), 16);
                    bytes = TtlItemList.WriteAll(bytes, Skills);
                    break;
                case TdbType.TtlFigureDataList:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(LevelingXpRequirements.Count), 16);
                    bytes = TtlFigureDataList2.WriteAll(bytes, LevelingXpRequirements);
                    break;
                case TdbType.TTL_skill_list:
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CharacterSkills.Count), 16);
                    bytes = TTL_skill_list.WriteAll(bytes, CharacterSkills);
                    break;
            }

            return bytes;
        }
        
        public static TdbType GetTdbFileType(string path)
        {
            try
            {
                return (TdbType)Enum.Parse(typeof(TdbType), Path.GetFileNameWithoutExtension(path));
            }
            catch
            {
                throw new Exception(String.Format("\"{0}\" is not a valid TdbType.", Path.GetFileNameWithoutExtension(path)));
            }
        }
        

        public int ExperienceForLevel(int level)
        {
            if (MasterLevels == null || _TdbType != TdbType.TtlMasterLevelTable) throw new InvalidDataException("TdbType is not TtlMasterLevelTable.");
            int experience = 0;

            for (int i = 0; i < MasterLevels.Count; i++)
            {
                if (level < MasterLevels[i].I_00)
                {
                    break;
                }
                else if (level == MasterLevels[i].I_00)
                {
                    experience = MasterLevels[i].I_08;
                    break;
                }
                else
                {
                    experience = MasterLevels[i].I_08;
                }
            }

            return experience;
        }

        public int ExperienceForNextLevel(int level)
        {
            if (MasterLevels == null || _TdbType != TdbType.TtlMasterLevelTable) throw new InvalidDataException("TdbType is not TtlMasterLevelTable.");

            int experience = 0;

            for (int i = 0; i < MasterLevels.Count; i++)
            {
                if (level < MasterLevels[i].I_00)
                {
                    break;
                }
                else if (level == MasterLevels[i].I_00)
                {
                    experience += MasterLevels[i].AbsoluteXpForNextLevel;
                    break;
                }
                else
                {
                    experience += MasterLevels[i].AbsoluteXpForNextLevel;
                }
            }

            return experience;
        }

        public bool IsMaxLevel(int level)
        {
            if (MasterLevels == null || _TdbType != TdbType.TtlMasterLevelTable) throw new InvalidDataException("TdbType is not TtlMasterLevelTable.");

            if (MasterLevels.Count == 0) return false;
            if (MasterLevels[MasterLevels.Count - 1].I_00 == level) return true;
            return false;
        }

        public int CalculateExperienceRequired(int experience, int level)
        {
            if (MasterLevels == null || _TdbType != TdbType.TtlMasterLevelTable) throw new InvalidDataException("TdbType is not TtlMasterLevelTable.");

            int xpNeeded = ExperienceForLevel(level);
            int xpNeededForNextLevel = ExperienceForNextLevel(level);

            if (experience >= xpNeeded && experience < xpNeededForNextLevel)
            {
                return experience;
            }
            else
            {
                return xpNeeded;
            }
        }

        public TtlFigurePoseList GetPose(int ID)
        {
            if (_TdbType != TdbType.TtlFigurePoseList) throw new Exception("This function is not valid for this TdpType.");

            foreach(var pose in FigurePoseList)
            {
                if (pose.I_00 == ID) return pose;
            }

            return null;
        }

        public TtlItemList GetSkillItemEntry(int skillID)
        {
            if (_TdbType != TdbType.TTLItemList) throw new Exception("This function is not valid for this TdpType.");

            foreach (var skill in Skills)
            {
                if (skill.I_06 == skillID) return skill;
            }

            return null;
        }

    }

    [YAXSerializeAs("EnemyFigure")]
    public class TtlEnemyDataList1
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00 { get; set; }
        [YAXAttributeFor("FigureData")]
        [YAXSerializeAs("ID")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Level")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Skill_1")]
        [YAXSerializeAs("ID")]
        public int I_12 { get; set; }
        [YAXAttributeFor("Skill_2")]
        [YAXSerializeAs("ID")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Skill_3")]
        [YAXSerializeAs("ID")]
        public int I_20 { get; set; }

        public static List<TtlEnemyDataList1> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlEnemyDataList1> enemyData = new List<TtlEnemyDataList1>();

            for(int i = 0; i < count; i++)
            {
                enemyData.Add(Read(rawBytes, offset));
                offset += 24;
            }

            return enemyData;
        }

        public static TtlEnemyDataList1 Read(byte[] rawBytes, int offset)
        {
            return new TtlEnemyDataList1()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlEnemyDataList1> enemyData)
        {
            for(int i = 0; i < enemyData.Count; i++)
            {
                bytes.AddRange(enemyData[i].Write());
            }

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

            if (bytes.Count != 24) throw new Exception("EnemyDataList1 is an invalid size.");
            return bytes;
        }

    }

    [YAXSerializeAs("EnemySet")]
    public class TtlEnemyDataList2
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00 { get; set; }
        [YAXAttributeFor("Master_Level")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Master_Health")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Figure_1")]
        [YAXSerializeAs("ID")]
        public int I_12 { get; set; }
        [YAXAttributeFor("Figure_1")]
        [YAXSerializeAs("ChanceToPlace")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Figure_2")]
        [YAXSerializeAs("ID")]
        public int I_20 { get; set; }
        [YAXAttributeFor("Figure_2")]
        [YAXSerializeAs("ChanceToPlace")]
        public int I_24 { get; set; }
        [YAXAttributeFor("Figure_3")]
        [YAXSerializeAs("ID")]
        public int I_28 { get; set; }
        [YAXAttributeFor("Figure_3")]
        [YAXSerializeAs("ChanceToPlace")]
        public int I_32 { get; set; }
        [YAXAttributeFor("Figure_4")]
        [YAXSerializeAs("ID")]
        public int I_36 { get; set; }
        [YAXAttributeFor("Figure_4")]
        [YAXSerializeAs("ChanceToPlace")]
        public int I_40 { get; set; }
        [YAXAttributeFor("Figure_5")]
        [YAXSerializeAs("ID")]
        public int I_44 { get; set; }
        [YAXAttributeFor("Figure_5")]
        [YAXSerializeAs("ChanceToPlace")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }

        public static List<TtlEnemyDataList2> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlEnemyDataList2> enemyData = new List<TtlEnemyDataList2>();

            for (int i = 0; i < count; i++)
            {
                enemyData.Add(Read(rawBytes, offset));
                offset += 64;
            }

            return enemyData;
        }

        public static TtlEnemyDataList2 Read(byte[] rawBytes, int offset)
        {
            return new TtlEnemyDataList2()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlEnemyDataList2> enemyData)
        {
            for (int i = 0; i < enemyData.Count; i++)
            {
                bytes.AddRange(enemyData[i].Write());
            }

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
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_60));

            if (bytes.Count != 64) throw new Exception("EnemyDataList2 is an invalid size.");
            return bytes;
        }

    }

    [YAXSerializeAs("MasterLevel")]
    public class TtlMasterLevelTable
    {
        [YAXDontSerialize]
        public int AbsoluteXpForNextLevel
        {
            get
            {
                return I_04 + I_08;
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Level")]
        public int I_00 { get; set; }
        [YAXAttributeFor("RelativeXpForNextLevel")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("XpForThisLevel")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }

        public static List<TtlMasterLevelTable> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlMasterLevelTable> masterTable = new List<TtlMasterLevelTable>();

            for (int i = 0; i < count; i++)
            {
                masterTable.Add(Read(rawBytes, offset));
                offset += 20;
            }

            return masterTable;
        }

        public static TtlMasterLevelTable Read(byte[] rawBytes, int offset)
        {
            return new TtlMasterLevelTable()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16)
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlMasterLevelTable> masterLevelTable)
        {
            for (int i = 0; i < masterLevelTable.Count; i++)
            {
                bytes.AddRange(masterLevelTable[i].Write());
            }

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

            if (bytes.Count != 20) throw new Exception("TtlMasterLevelTable is an invalid size.");
            return bytes;
        }




    }

    [YAXSerializeAs("Item")]
    public class TtlItemList
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("Stars")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
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
        [YAXAttributeFor("UnlockRequirement")]
        [YAXSerializeAs("QuestID")]
        public string Str_16 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("I_38")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("TPMedalCost")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("I_42")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; } //ushort
        [YAXAttributeFor("I_46")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; } //ushort

        public static List<TtlItemList> ReadAll(byte[] rawBytes, int offset, int count, bool oldVersion)
        {
            List<TtlItemList> items = new List<TtlItemList>();

            for (int i = 0; i < count; i++)
            {
                items.Add(Read(rawBytes, offset, oldVersion));
                offset += (oldVersion) ? 44 : 48;
            }

            return items;
        }

        public static TtlItemList Read(byte[] rawBytes, int offset, bool oldVersion)
        {
            int _I_44 = (oldVersion) ? 0 : BitConverter.ToUInt16(rawBytes, offset + 44);
            int _I_46 = (oldVersion) ? 0 : BitConverter.ToUInt16(rawBytes, offset + 46);

            return new TtlItemList()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                Str_16 = StringEx.GetString(rawBytes, offset + 16, maxSize: 16),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = (ushort)_I_44,
                I_46 = (ushort)_I_46
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlItemList> enemyData, bool oldVersion = false)
        {
            for (int i = 0; i < enemyData.Count; i++)
            {
                bytes.AddRange(enemyData[i].Write(oldVersion));
            }

            return bytes;
        }

        public List<byte> Write(bool oldVersion)
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(Utils.GetStringBytes(Str_16, 16));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_38));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_42));

            if (!oldVersion)
            {
                bytes.AddRange(BitConverter.GetBytes(I_44));
                bytes.AddRange(BitConverter.GetBytes(I_46));
            }

            if (bytes.Count != 48 && !oldVersion) throw new Exception("TtlItemList is an invalid size.");
            if (bytes.Count != 44 && oldVersion) throw new Exception("TtlItemList is an invalid size.");
            return bytes;
        }

    }

    [YAXSerializeAs("Figure")]
    public class TtlFigureDataList1
    {
        [YAXDontSerialize]
        public int MaxCombinations
        {
            get
            {
                switch (I_05)
                {
                    case 0:
                        return 4;
                    case 1:
                        return 7;
                    case 2:
                        return 9;
                    default:
                        return 10;
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("Pose_ID")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public byte I_04 { get; set; }
        [YAXAttributeFor("Rarity")]
        [YAXSerializeAs("value")]
        public byte I_05 { get; set; }
        [YAXAttributeFor("Limit_Broken_Total")]
        [YAXSerializeAs("value")]
        public byte I_06 { get; set; }
        [YAXAttributeFor("Limit_Breaks_Avialable")]
        [YAXSerializeAs("value")]
        public byte I_07 { get; set; }
        [YAXAttributeFor("HP")]
        [YAXSerializeAs("Min")]
        public int I_08 { get; set; }
        [YAXAttributeFor("HP")]
        [YAXSerializeAs("Max")]
        public int I_12 { get; set; }
        [YAXAttributeFor("ATK")]
        [YAXSerializeAs("Min")]
        public int I_16 { get; set; }
        [YAXAttributeFor("ATK")]
        [YAXSerializeAs("Max")]
        public int I_20 { get; set; }
        [YAXAttributeFor("DEF")]
        [YAXSerializeAs("Min")]
        public int I_24 { get; set; }
        [YAXAttributeFor("DEF")]
        [YAXSerializeAs("Max")]
        public int I_28 { get; set; }
        [YAXAttributeFor("SPD")]
        [YAXSerializeAs("Min")]
        public int I_32 { get; set; }
        [YAXAttributeFor("SPD")]
        [YAXSerializeAs("Max")]
        public int I_36 { get; set; }
        [YAXAttributeFor("Posing_Skill")]
        [YAXSerializeAs("ID")]
        public int I_40 { get; set; }
        [YAXAttributeFor("Skill_Count")]
        [YAXSerializeAs("value")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("I_46")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("Limit_Break_Figure_ID")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("Alternate_Limit_Break_Figure_ID")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_1")]
        [YAXSerializeAs("ItemID")]
        public ushort I_56 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_1")]
        [YAXSerializeAs("Amount")]
        public ushort I_58 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_2")]
        [YAXSerializeAs("ItemID")]
        public ushort I_60 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_2")]
        [YAXSerializeAs("Amount")]
        public ushort I_62 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_3")]
        [YAXSerializeAs("ItemID")]
        public ushort I_64 { get; set; }
        [YAXAttributeFor("Generic_Limit_Break_Chip_3")]
        [YAXSerializeAs("Amount")]
        public ushort I_66 { get; set; }
        [YAXAttributeFor("Special_Limit_Break_Chip_1")]
        [YAXSerializeAs("ItemID")]
        public ushort I_68 { get; set; }
        [YAXAttributeFor("Special_Limit_Break_Chip_1")]
        [YAXSerializeAs("Amount")]
        public ushort I_70 { get; set; }
        [YAXAttributeFor("Special_Limit_Break_Chip_2")]
        [YAXSerializeAs("ItemID")]
        public ushort I_72 { get; set; }
        [YAXAttributeFor("Special_Limit_Break_Chip_2")]
        [YAXSerializeAs("Amount")]
        public ushort I_74 { get; set; }

        public static List<TtlFigureDataList1> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlFigureDataList1> enemyData = new List<TtlFigureDataList1>();

            for (int i = 0; i < count; i++)
            {
                enemyData.Add(Read(rawBytes, offset));
                offset += 76;
            }

            return enemyData;
        }

        public static TtlFigureDataList1 Read(byte[] rawBytes, int offset)
        {
            return new TtlFigureDataList1()
            {
                I_00 = BitConverter.ToUInt16(rawBytes, offset + 0),
                I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                I_04 = rawBytes[offset + 4],
                I_05 = rawBytes[offset + 5],
                I_06 = rawBytes[offset + 6],
                I_07 = rawBytes[offset + 7],
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                I_56 = BitConverter.ToUInt16(rawBytes, offset + 56),
                I_58 = BitConverter.ToUInt16(rawBytes, offset + 58),
                I_60 = BitConverter.ToUInt16(rawBytes, offset + 60),
                I_62 = BitConverter.ToUInt16(rawBytes, offset + 62),
                I_64 = BitConverter.ToUInt16(rawBytes, offset + 64),
                I_66 = BitConverter.ToUInt16(rawBytes, offset + 66),
                I_68 = BitConverter.ToUInt16(rawBytes, offset + 68),
                I_70 = BitConverter.ToUInt16(rawBytes, offset + 70),
                I_72 = BitConverter.ToUInt16(rawBytes, offset + 72),
                I_74 = BitConverter.ToUInt16(rawBytes, offset + 74),
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlFigureDataList1> enemyData)
        {
            for (int i = 0; i < enemyData.Count; i++)
            {
                bytes.AddRange(enemyData[i].Write());
            }

            return bytes;
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
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_46));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_58));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_62));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_66));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_70));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_74));

            if (bytes.Count != 76) throw new Exception("TtlFigureDataList1 is an invalid size.");
            return bytes;
        }

        public string GetRarity()
        {
            switch (I_05)
            {
                case 0:
                    return "N";
                case 1:
                    return "R";
                case 2:
                    return "SR";
                default:
                    return "UR";

            }
        }
        
    }

    [YAXSerializeAs("LevelingXpRequirement")]
    public class TtlFigureDataList2
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Level")]
        public int I_16 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("N")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("R")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SR")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UR")]
        public int I_12 { get; set; }

        public static List<TtlFigureDataList2> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlFigureDataList2> masterTable = new List<TtlFigureDataList2>();

            for (int i = 0; i < count; i++)
            {
                masterTable.Add(Read(rawBytes, offset, i));
                offset += 20;
            }

            return masterTable;
        }

        public static TtlFigureDataList2 Read(byte[] rawBytes, int offset, int index)
        {
            return new TtlFigureDataList2()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16)
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlFigureDataList2> masterLevelTable)
        {
            for (int i = 0; i < masterLevelTable.Count; i++)
            {
                bytes.AddRange(masterLevelTable[i].Write());
            }

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

            if (bytes.Count != 20) throw new Exception("TtlFigureDataList2 is an invalid size.");
            return bytes;
        }

    }

    [YAXSerializeAs("FigurePose")]
    public class TtlFigurePoseList
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Character")]
        [YAXSerializeAs("ID")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Character")]
        [YAXSerializeAs("Code")]
        public string Str_12 { get; set; }
        [YAXAttributeFor("Character")]
        [YAXSerializeAs("Costume")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public byte I_20 { get; set; }
        [YAXAttributeFor("I_21")]
        [YAXSerializeAs("value")]
        public byte I_21 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public byte I_22 { get; set; }
        [YAXAttributeFor("I_23")]
        [YAXSerializeAs("value")]
        public byte I_23 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public ushort I_36 { get; set; }
        [YAXAttributeFor("Super_Skill_1")]
        [YAXSerializeAs("ID2")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("Super_Skill_2")]
        [YAXSerializeAs("ID2")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("Super_Skill_3")]
        [YAXSerializeAs("ID2")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("Super_Skill_4")]
        [YAXSerializeAs("ID2")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("Ultimate_Skill_1")]
        [YAXSerializeAs("ID2")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("Ultimate_Skill_2")]
        [YAXSerializeAs("ID2")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("Evasive_Skill")]
        [YAXSerializeAs("ID2")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("Blast_Skill")]
        [YAXSerializeAs("ID2")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("Awoken_Skill")]
        [YAXSerializeAs("ID2")]
        public ushort I_54 { get; set; }

        public static List<TtlFigurePoseList> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TtlFigurePoseList> masterTable = new List<TtlFigurePoseList>();

            for (int i = 0; i < count; i++)
            {
                masterTable.Add(Read(rawBytes, offset));
                offset += 56;
            }

            return masterTable;
        }

        public static TtlFigurePoseList Read(byte[] rawBytes, int offset)
        {
            return new TtlFigurePoseList()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                Str_12 = StringEx.GetString(rawBytes, offset + 12, maxSize: 4),
                I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = rawBytes[offset + 20],
                I_21 = rawBytes[offset + 21],
                I_22 = rawBytes[offset + 22],
                I_23 = rawBytes[offset + 23],
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                I_38 = BitConverter.ToUInt16(rawBytes, offset + 38),
                I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                I_54 = BitConverter.ToUInt16(rawBytes, offset + 54)
            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TtlFigurePoseList> masterLevelTable)
        {
            for (int i = 0; i < masterLevelTable.Count; i++)
            {
                bytes.AddRange(masterLevelTable[i].Write());
            }

            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(Utils.GetStringBytes(Str_12, 4));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.Add(I_20);
            bytes.Add(I_21);
            bytes.Add(I_22);
            bytes.Add(I_23);
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
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

            if (bytes.Count != 56) throw new Exception("TtlFigurePoseList is an invalid size.");
            return bytes;
        }

    }

    [YAXSerializeAs("Skill")]
    public class TTL_skill_list
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Sub_ID")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("TriggerSquares")]
        [YAXSerializeAs("values")]
        public TriggerSquares I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("ATK")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("F_88")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("DEF")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("SPD")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("DamageDealt")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("DamageReceived")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("UltimateAttackDamage")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("UltimateAttackGauge")]
        [YAXSerializeAs("value")]
        public int I_120 { get; set; }
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        public int I_124 { get; set; }
        [YAXAttributeFor("I_128")]
        [YAXSerializeAs("value")]
        public int I_128 { get; set; }
        [YAXAttributeFor("I_132")]
        [YAXSerializeAs("value")]
        public int I_132 { get; set; }
        [YAXAttributeFor("I_136")]
        [YAXSerializeAs("value")]
        public int I_136 { get; set; }
        [YAXAttributeFor("I_140")]
        [YAXSerializeAs("value")]
        public int I_140 { get; set; }
        [YAXAttributeFor("I_144")]
        [YAXSerializeAs("value")]
        public int I_144 { get; set; }
        [YAXAttributeFor("I_148")]
        [YAXSerializeAs("value")]
        public int I_148 { get; set; }
        [YAXAttributeFor("I_152")]
        [YAXSerializeAs("value")]
        public int I_152 { get; set; }

        public static List<TTL_skill_list> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<TTL_skill_list> enemyData = new List<TTL_skill_list>();

            for (int i = 0; i < count; i++)
            {
                enemyData.Add(Read(rawBytes, offset));
                offset += 156;
            }

            return enemyData;
        }

        public static TTL_skill_list Read(byte[] rawBytes, int offset)
        {
            return new TTL_skill_list()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                I_16 = (TriggerSquares)BitConverter.ToInt32(rawBytes, offset + 16),
                I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
                I_64 = BitConverter.ToInt32(rawBytes, offset + 64),
                I_68 = BitConverter.ToInt32(rawBytes, offset + 68),
                I_72 = BitConverter.ToInt32(rawBytes, offset + 72),
                I_76 = BitConverter.ToInt32(rawBytes, offset + 76),
                F_80 = BitConverter.ToSingle(rawBytes, offset + 80),
                I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                F_88 = BitConverter.ToSingle(rawBytes, offset + 88),
                I_92 = BitConverter.ToInt32(rawBytes, offset + 92),
                F_96 = BitConverter.ToSingle(rawBytes, offset + 96),
                I_100 = BitConverter.ToInt32(rawBytes, offset + 100),
                F_104 = BitConverter.ToSingle(rawBytes, offset + 104),
                F_108 = BitConverter.ToSingle(rawBytes, offset + 108),
                F_112 = BitConverter.ToSingle(rawBytes, offset + 112),
                F_116 = BitConverter.ToSingle(rawBytes, offset + 116),
                I_120 = BitConverter.ToInt32(rawBytes, offset + 120),
                I_124 = BitConverter.ToInt32(rawBytes, offset + 124),
                I_128 = BitConverter.ToInt32(rawBytes, offset + 128),
                I_132 = BitConverter.ToInt32(rawBytes, offset + 132),
                I_136 = BitConverter.ToInt32(rawBytes, offset + 136),
                I_140 = BitConverter.ToInt32(rawBytes, offset + 140),
                I_144 = BitConverter.ToInt32(rawBytes, offset + 144),
                I_148 = BitConverter.ToInt32(rawBytes, offset + 148),
                I_152 = BitConverter.ToInt32(rawBytes, offset + 152)

            };
        }

        public static List<byte> WriteAll(List<byte> bytes, List<TTL_skill_list> enemyData)
        {
            for (int i = 0; i < enemyData.Count; i++)
            {
                bytes.AddRange(enemyData[i].Write());
            }

            return bytes;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes((int)I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(I_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(I_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_56));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(I_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(I_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));
            bytes.AddRange(BitConverter.GetBytes(F_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));
            bytes.AddRange(BitConverter.GetBytes(I_120));
            bytes.AddRange(BitConverter.GetBytes(I_124));
            bytes.AddRange(BitConverter.GetBytes(I_128));
            bytes.AddRange(BitConverter.GetBytes(I_132));
            bytes.AddRange(BitConverter.GetBytes(I_136));
            bytes.AddRange(BitConverter.GetBytes(I_140));
            bytes.AddRange(BitConverter.GetBytes(I_144));
            bytes.AddRange(BitConverter.GetBytes(I_148));
            bytes.AddRange(BitConverter.GetBytes(I_152));

            if (bytes.Count != 156) throw new Exception("TTL_skill_list is an invalid size.");
            return bytes;
        }

    }



}
