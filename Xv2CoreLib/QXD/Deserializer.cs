using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.QXD
{
    public class Deserializer
    {
        QXD_File qxd_File;
        string saveLocation;
        public List<byte> bytes = new List<byte>() { 35, 81, 88, 68, 254, 255, 48, 0, 0, 0, 0, 0, 48, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QXD_File), YAXSerializationOptions.DontSerializeNullObjects);
            qxd_File = (QXD_File)serializer.DeserializeFromFile(location);
            Validation();
            WriteQuestData();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(QXD_File _qxdFile, string _saveLocation)
        {
            saveLocation = _saveLocation;
            qxd_File = _qxdFile;
            WriteQuestData();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(QXD_File _qxdFile)
        {
            qxd_File = _qxdFile;
            Validation();
            WriteQuestData();
        }

        private void Validation()
        {
            for (int i = 0; i < qxd_File.Quests.Count; i++)
            {
                if (qxd_File.Quests[i].Name.Length > 16)
                    throw new InvalidDataException(string.Format("Quest name exceeds max length of 16 ({0})", qxd_File.Quests[i].Name));

                if (qxd_File.Quests[i].I_48.Length != 4)
                    throw new InvalidDataException(string.Format("Quest I_48 is an invalid size. Must have 4 values. (on quest: {0}", qxd_File.Quests[i].Name));

                if (qxd_File.Quests[i].I_68.Length != 5)
                    throw new InvalidDataException(string.Format("Quest I_68 is an invalid size. Must have 5 values. (on quest: {0}", qxd_File.Quests[i].Name));

                if (qxd_File.Quests[i].I_232.Length != 8)
                    throw new InvalidDataException(string.Format("Quest I_232 is an invalid size. Must have 8 values. (on quest: {0}", qxd_File.Quests[i].Name));

                if (qxd_File.Quests[i].StageDisplay.Count != 16)
                    throw new InvalidDataException(string.Format("Quest Stage_Portraits is an invalid size. Must have 16 values. (on quest: {0}", qxd_File.Quests[i].Name));

                if (qxd_File.Quests[i].UnknownNum1 != null)
                {
                    foreach (UnkNum1 unk1 in qxd_File.Quests[i].UnknownNum1)
                    {
                        if (unk1.I_00.Length != 16)
                            throw new InvalidDataException(string.Format("Unk1 values is an invalid size. Must have 16 values. (on quest: {0}", qxd_File.Quests[i].Name));
                    }
                }

                if (qxd_File.Quests[i].UnknownNum2 != null)
                {
                    foreach (UnkNum2 unk2 in qxd_File.Quests[i].UnknownNum2)
                    {
                        if (unk2.I_00.Length != 16)
                            throw new InvalidDataException(string.Format("Unk2 values is an invalid size. Must have 16 values. (on quest: {0}", qxd_File.Quests[i].Name));
                    }
                }
            }

            for (int i = 0; i < qxd_File.Characters1.Count; i++)
            {
                if (qxd_File.Characters1[i].I_106.Length != 7)
                    throw new InvalidDataException(string.Format("QXD NormalCharacter I_106 is an invalid size. Must have 7 values. (ID: {0}", qxd_File.Characters1[i].SortID));
            }

            for (int i = 0; i < qxd_File.Characters2.Count; i++)
            {
                if (qxd_File.Characters2[i].I_106.Length != 7)
                    throw new InvalidDataException(string.Format("QXD SpecialCharacter I_106 is an invalid size. Must have 7 values. (ID: {0}", qxd_File.Characters2[i].SortID));
            }
        }

        private void WriteQuestData()
        {
            List<Quest_Data> quests = qxd_File.GetQuestsToWrite();

            //writing the count
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(quests.Count), 8);

            //counts
            int totalQuests = quests.Count();
            int totalCharacters1 = 0;
            int totalCharacters2 = 0;
            int totalUnknownDatas = 0;
            if (qxd_File.Characters1 != null)
            {
                totalCharacters1 = qxd_File.Characters1.Count;
            }
            if (qxd_File.Characters2 != null)
            {
                totalCharacters2 = qxd_File.Characters2.Count;
            }
            if (qxd_File.Collections != null)
            {
                totalUnknownDatas = qxd_File.Collections.Count;
            }

            //offsets (where to write the offsets)
            List<int> MsgEntryOffsets = new List<int>();
            List<int> UnkNum1Offsets = new List<int>();
            List<int> UnkNum2Offsets = new List<int>();
            List<int> QbtFilesOffsets = new List<int>();
            List<int> EquipRewardOffsets = new List<int>();
            List<int> SkillRewardOffsets = new List<int>();
            List<int> CharaUnlockOffsets = new List<int>();
            List<int> StageDisplayOffsets = new List<int>();

            for (int i = 0; i < totalQuests; i++)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(quests[i].Name));

                for (int a = 0; a < 16 - quests[i].Name.Count(); a++)
                {
                    bytes.Add(0);
                }

                bytes.AddRange(BitConverter.GetBytes(int.Parse(quests[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_20));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_24));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_28));

                //MsgEntries
                if (quests[i].MsgFiles != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].MsgFiles.Count()));
                    MsgEntryOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0, });
                    MsgEntryOffsets.Add(0);
                }

                //some shorts
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_40));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_42));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_44));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_46));
                bytes.AddRange(BitConverter_Ex.GetBytes(quests[i].I_48));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_64));
                bytes.AddRange(BitConverter_Ex.GetBytes(quests[i].I_68));

                //UnkNum1
                if (quests[i].UnknownNum1 != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].UnknownNum1.Count()));
                    UnkNum1Offsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    UnkNum1Offsets.Add(0);
                }

                //UnkNum2
                if (quests[i].UnknownNum2 != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].UnknownNum2.Count()));
                    UnkNum2Offsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    UnkNum2Offsets.Add(0);
                }


                //more values
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_104));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_106));
                bytes.AddRange(BitConverter.GetBytes(quests[i].NEW_I_108));
                bytes.AddRange(BitConverter.GetBytes(quests[i].NEW_I_112));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_108));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_110));

                //QBT
                if (quests[i].QedFiles != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].QedFiles.Count()));
                    QbtFilesOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    QbtFilesOffsets.Add(0);
                }


                //more values
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_120));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_124));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_128));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_132));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_136));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_140));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_144));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_148));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_152));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_156));


                //Equipment Reward
                if (quests[i].EquipReward != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward.Count()));
                    EquipRewardOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    EquipRewardOffsets.Add(0);
                }

                //Skill Reward
                if (quests[i].Skill_Reward != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].Skill_Reward.Count()));
                    SkillRewardOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    SkillRewardOffsets.Add(0);
                }

                //Chara Unlock
                if (quests[i].Chara_Unlock != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(quests[i].Chara_Unlock.Count()));
                    CharaUnlockOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    CharaUnlockOffsets.Add(0);
                }

                //Stage Portrait
                if (quests[i].StageDisplay != null)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)1));
                    StageDisplayOffsets.Add(bytes.Count());
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0 });
                }
                else
                {
                    bytes.AddRange(new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 });
                    StageDisplayOffsets.Add(0);
                }

                //random value
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_192));

                //Enemy Portrait
                InvalidCollectionCheck("EnemyPortraits", 6, quests[i].EnemyPortraitDisplay.Count(), quests[i].Name);
                int addedOffset = 0;
                for (int a = 0; a < 6; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes((short)quests[i].EnemyPortraitDisplay[a].CharaID));
                    bytes.AddRange(BitConverter.GetBytes((short)quests[i].EnemyPortraitDisplay[a].CostumeIndex));
                    bytes.AddRange(BitConverter.GetBytes((short)quests[i].EnemyPortraitDisplay[a].State));
                    addedOffset += 6;
                }

                //collection of unknown values
                bytes.AddRange(BitConverter_Ex.GetBytes(quests[i].I_232));

                //5 Int32s
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_248));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_252));
                bytes.AddRange(BitConverter.GetBytes((int)quests[i].I_256));
                bytes.AddRange(BitConverter.GetBytes((int)quests[i].I_260));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_264));


                //Music Values
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_268));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_270));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_272));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_274));
                bytes.AddRange(BitConverter.GetBytes(quests[i].F_276));
                bytes.AddRange(BitConverter.GetBytes(quests[i].I_280));


                //end main Quest Data, Next: MSG entries

            }

            //MSG section
            for (int i = 0; i < totalQuests; i++)
            {
                if (MsgEntryOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), MsgEntryOffsets[i]);
                    for (int a = 0; a < quests[i].MsgFiles.Count(); a++)
                    {
                        bytes.AddRange(Encoding.ASCII.GetBytes(quests[i].MsgFiles[a]));
                        int remainingBytes = 32 - quests[i].MsgFiles[a].Count();
                        for (int b = 0; b < remainingBytes; b++)
                        {
                            bytes.Add(0);
                        }
                    }
                }
            }

            //UnkNum1 section
            for (int i = 0; i < totalQuests; i++)
            {
                if (UnkNum1Offsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), UnkNum1Offsets[i]);

                    for (int e = 0; e < quests[i].UnknownNum1.Count(); e++)
                    {
                        InvalidCollectionCheck("UnkNum1", 16, quests[i].UnknownNum1[e].I_00.Count(), quests[i].Name);
                        for (int a = 0; a < quests[i].UnknownNum1[e].I_00.Count(); a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(quests[i].UnknownNum1[e].I_00[a]));
                        }
                    }
                }
            }

            //UnkNum2 section
            for (int i = 0; i < totalQuests; i++)
            {
                if (UnkNum2Offsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), UnkNum2Offsets[i]);

                    for (int a = 0; a < quests[i].UnknownNum2.Count(); a++)
                    {
                        InvalidCollectionCheck("UnkNum2", 16, quests[i].UnknownNum2[a].I_00.Count(), quests[i].Name);
                        for (int b = 0; b < quests[i].UnknownNum2[a].I_00.Count(); b++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(quests[i].UnknownNum2[a].I_00[b]));
                        }
                    }
                }
            }

            //QBT section
            for (int i = 0; i < totalQuests; i++)
            {
                if (QbtFilesOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), QbtFilesOffsets[i]);

                    for (int a = 0; a < quests[i].QedFiles.Count(); a++)
                    {
                        ErrorStringToLongCheck(quests[i].QedFiles[a], 31);
                        bytes.AddRange(Encoding.ASCII.GetBytes(quests[i].QedFiles[a]));
                        int remainingBytes = 32 - quests[i].QedFiles[a].Count();

                        for (int b = 0; b < remainingBytes; b++)
                        {
                            bytes.Add(0);
                        }
                    }
                }
            }

            //Equipment Reward section
            for (int i = 0; i < totalQuests; i++)
            {
                if (EquipRewardOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), EquipRewardOffsets[i]);

                    for (int a = 0; a < quests[i].EquipReward.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes((int)quests[i].EquipReward[a].I_00));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_04));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_08));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_12));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_16));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_20));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].F_24));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].EquipReward[a].I_28));
                    }
                }
            }

            //Skill Reward Section
            for (int i = 0; i < totalQuests; i++)
            {
                if (SkillRewardOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), SkillRewardOffsets[i]);

                    for (int a = 0; a < quests[i].Skill_Reward.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes((int)quests[i].Skill_Reward[a].I_00));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].Skill_Reward[a].I_04));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].Skill_Reward[a].I_08));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].Skill_Reward[a].I_12));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].Skill_Reward[a].F_16));
                    }
                }
            }

            //Chara Unlock Section
            for (int i = 0; i < totalQuests; i++)
            {
                if (CharaUnlockOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), CharaUnlockOffsets[i]);

                    for (int a = 0; a < quests[i].Chara_Unlock.Count(); a++)
                    {
                        ErrorStringToLongCheck(quests[i].Chara_Unlock[a].ShortName, 3);

                        bytes.AddRange(Encoding.ASCII.GetBytes(quests[i].Chara_Unlock[a].ShortName));
                        int remainingBytes = 4 - quests[i].Chara_Unlock[a].ShortName.Count();

                        for (int b = 0; b < remainingBytes; b++)
                        {
                            bytes.Add(0);
                        }

                        bytes.AddRange(BitConverter.GetBytes(quests[i].Chara_Unlock[a].CostumeIndex));
                        bytes.AddRange(BitConverter.GetBytes(quests[i].Chara_Unlock[a].I_06));

                    }
                }
            }

            //Stage Portrait Section
            for (int i = 0; i < totalQuests; i++)
            {
                if (StageDisplayOffsets[i] > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), StageDisplayOffsets[i]);

                    for (int a = 0; a < quests[i].StageDisplay.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(quests[i].StageDisplay[a]));
                    }
                }
            }

            WriteCharacterData();
        }

        private void WriteCharacterData()
        {
            //setting header info for Characters1
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 20);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qxd_File.Characters1.Count()), 16);

            for (int i = 0; i < qxd_File.Characters1.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(qxd_File.Characters1[i].Index)));

                bytes.AddRange(Encoding.ASCII.GetBytes(qxd_File.Characters1[i].CharaShortName));
                int remainingBytes = 4 - qxd_File.Characters1[i].CharaShortName.Count();
                for (int b = 0; b < remainingBytes; b++)
                {
                    bytes.Add(0);
                }

                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_16));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_40));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_44));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_48));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_52));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_56));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_60));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_64));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_68));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_72));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_76));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].F_80));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_84));


                //Skills
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_00));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_02));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_04));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_06));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_08));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_10));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_12));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_14));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i]._Skills.I_16));
                bytes.AddRange(BitConverter_Ex.GetBytes(qxd_File.Characters1[i].I_106));

                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_120));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_122));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_124));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters1[i].I_126));

            }

            //CHARACTERS2 (duplicated code)
            //setting header info for Characters2
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 28);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qxd_File.Characters2.Count()), 24);

            for (int i = 0; i < qxd_File.Characters2.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(qxd_File.Characters2[i].Index)));

                bytes.AddRange(Encoding.ASCII.GetBytes(qxd_File.Characters2[i].CharaShortName));
                int remainingBytes = 4 - qxd_File.Characters2[i].CharaShortName.Count();
                for (int b = 0; b < remainingBytes; b++)
                {
                    bytes.Add(0);
                }

                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_16));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_20));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_24));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_28));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_32));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_36));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_40));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_44));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_48));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_52));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_56));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_60));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_64));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_68));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_72));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_76));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].F_80));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_84));



                //Skills
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_00));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_02));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_04));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_06));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_08));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_10));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_12));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_14));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i]._Skills.I_16));
                bytes.AddRange(BitConverter_Ex.GetBytes(qxd_File.Characters2[i].I_106));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_120));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_122));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_124));
                bytes.AddRange(BitConverter.GetBytes(qxd_File.Characters2[i].I_126));

            }

            WriteUnknownData();
        }

        private void WriteUnknownData()
        {
            if (qxd_File.Collections != null)
            {
                //setting header info
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 36);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qxd_File.Collections.Count), 32);

                for (int i = 0; i < qxd_File.Collections.Count; i++)
                {
                    bytes.AddRange(qxd_File.Collections[i].Write());
                }
            }
            else
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 36);
            }

            //more header info
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 44);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qxd_File.I_40), 40);

            for (int i = 0; i < 8; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(qxd_File.EndFloats[i]));
            }


        }

        private void InvalidCollectionCheck(string collection, int limit, int actual, string questID)
        {
            if (actual > limit || actual < limit)
            {
                Console.WriteLine("Error! (on Quest ID " + questID + ")");
                Console.WriteLine("\nThe collection " + collection + " can only contain " + limit + " values");
                Console.WriteLine("It currently has " + actual);
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private void ErrorStringToLongCheck(string input, int maxSize)
        {
            if (input.Count() > maxSize)
            {
                Console.WriteLine("Error! The string \"" + input + "\" exceeds the maximum length of " + maxSize);
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

    }
}
