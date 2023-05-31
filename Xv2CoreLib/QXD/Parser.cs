using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.QXD
{
    //Messy old code.

    public class Parser
    {
        QXD_File qxd_File = new QXD_File();
        byte[] rawBytes;
        string saveLocation;
        bool writeXml = false;

        //offsets
        int chara1Offset;
        int chara2Offset;
        int questsOffset;
        int lastSectionOffset;
        int floatSectionOffset;

        //counts
        int chara1Count;
        int chara2Count;
        int questsCount;
        int lastSectionCount;

        public Parser(string location, bool _writeXml)
        {
            writeXml = _writeXml;
            rawBytes = File.ReadAllBytes(location);
            saveLocation = location;
            chara1Offset = BitConverter.ToInt32(rawBytes, 20);
            chara2Offset = BitConverter.ToInt32(rawBytes, 28);
            questsOffset = BitConverter.ToInt32(rawBytes, 12);
            lastSectionOffset = BitConverter.ToInt32(rawBytes, 36);
            floatSectionOffset = BitConverter.ToInt32(rawBytes, 44);
            chara1Count = BitConverter.ToInt32(rawBytes, 16);
            chara2Count = BitConverter.ToInt32(rawBytes, 24);
            questsCount = BitConverter.ToInt32(rawBytes, 8);
            lastSectionCount = BitConverter.ToInt32(rawBytes, 32);
            qxd_File.Characters1 = new List<Quest_Characters>();
            qxd_File.Characters2 = new List<Quest_Characters>();
            qxd_File.Quests = new List<Quest_Data>();
            if (lastSectionCount > 0)
            {
                qxd_File.Collections = new List<QXD_CollectionEntry>();
            }
            ParseCharacters();
            ParseUnknownData();
            ParseQuestData();
            ParseEndFloats();
            if (writeXml == true)
            {
                WriteXmlFile();
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            chara1Offset = BitConverter.ToInt32(rawBytes, 20);
            chara2Offset = BitConverter.ToInt32(rawBytes, 28);
            questsOffset = BitConverter.ToInt32(rawBytes, 12);
            lastSectionOffset = BitConverter.ToInt32(rawBytes, 36);
            floatSectionOffset = BitConverter.ToInt32(rawBytes, 44);
            chara1Count = BitConverter.ToInt32(rawBytes, 16);
            chara2Count = BitConverter.ToInt32(rawBytes, 24);
            questsCount = BitConverter.ToInt32(rawBytes, 8);
            lastSectionCount = BitConverter.ToInt32(rawBytes, 32);
            qxd_File.Characters1 = new List<Quest_Characters>();
            qxd_File.Characters2 = new List<Quest_Characters>();
            qxd_File.Quests = new List<Quest_Data>();
            if (lastSectionCount > 0)
            {
                qxd_File.Collections = new List<QXD_CollectionEntry>();
            }
            ParseCharacters();
            ParseUnknownData();
            ParseQuestData();
            ParseEndFloats();
        }


        public QXD_File GetQxdFile()
        {
            return qxd_File;
        }

        private void ParseCharacters()
        {
            int offset = chara1Offset;

            for (int i = 0; i < chara1Count; i++)
            {
                qxd_File.Characters1.Add(new Quest_Characters()
                {
                    Index = BitConverter.ToInt32(rawBytes, offset).ToString(),
                    CharaShortName = StringEx.GetString(rawBytes, offset + 4, false, StringEx.EncodingType.ASCII, 3),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                    F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                    F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                    F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                    F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                    F_76 = BitConverter.ToSingle(rawBytes, offset + 76),
                    F_80 = BitConverter.ToSingle(rawBytes, offset + 80),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                    _Skills = new Skills
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 88),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 90),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 92),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 94),
                        I_08 = BitConverter.ToUInt16(rawBytes, offset + 96),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 98),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 100),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 102),
                        I_16 = BitConverter.ToUInt16(rawBytes, offset + 104),
                    },
                    I_106 = BitConverter_Ex.ToInt16Array(rawBytes, offset + 106, 7),
                    I_120 = BitConverter.ToInt16(rawBytes, offset + 120),
                    I_122 = BitConverter.ToUInt16(rawBytes, offset + 122)
                });
                offset += 124;
            }

            offset = chara2Offset;

            for (int i = 0; i < chara2Count; i++)
            {
                qxd_File.Characters2.Add(new Quest_Characters()
                {
                    Index = BitConverter.ToInt32(rawBytes, offset).ToString(),
                    CharaShortName = StringEx.GetString(rawBytes, offset + 4, false, StringEx.EncodingType.ASCII),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                    F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                    F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                    F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                    F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                    F_76 = BitConverter.ToSingle(rawBytes, offset + 76),
                    F_80 = BitConverter.ToSingle(rawBytes, offset + 80),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                    _Skills = new Skills
                    {
                        I_00 = BitConverter.ToUInt16(rawBytes, offset + 88),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 90),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 92),
                        I_06 = BitConverter.ToUInt16(rawBytes, offset + 94),
                        I_08 = BitConverter.ToUInt16(rawBytes, offset + 96),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 98),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 100),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 102),
                        I_16 = BitConverter.ToUInt16(rawBytes, offset + 104),
                    },
                    I_106 = BitConverter_Ex.ToInt16Array(rawBytes, offset + 106, 7),
                    I_120 = BitConverter.ToInt16(rawBytes, offset + 120),
                    I_122 = BitConverter.ToUInt16(rawBytes, offset + 122)
                });
                offset += 124;
            }
        }

        private void ParseUnknownData()
        {
            int offset = lastSectionOffset;
            for (int i = 0; i < lastSectionCount; i++)
            {
                qxd_File.Collections.Add(QXD_CollectionEntry.Read(rawBytes, offset));
                offset += 12;
            }
        }

        private void ParseQuestData()
        {
            int offset = questsOffset;

            for (int i = 0; i < questsCount; i++)
            {
                //counts & offsets
                int msgEntryCount = BitConverter.ToInt32(rawBytes, offset + 32);
                int equipRewardCount = BitConverter.ToInt32(rawBytes, offset + 160);
                int skillRewardCount = BitConverter.ToInt32(rawBytes, offset + 168);
                int unk1count = BitConverter.ToInt32(rawBytes, offset + 88);
                int unk2Count = BitConverter.ToInt32(rawBytes, offset + 96);
                int charaUnlockCount = BitConverter.ToInt32(rawBytes, offset + 176);
                int stagePortraitCount = BitConverter.ToInt32(rawBytes, offset + 184);
                int qedFileCount = BitConverter.ToInt32(rawBytes, offset + 112);
                int msgEntryOffset = BitConverter.ToInt32(rawBytes, offset + 36);
                int equipRewardOffset = BitConverter.ToInt32(rawBytes, offset + 164);
                int skillRewardOffset = BitConverter.ToInt32(rawBytes, offset + 172);
                int unk1Offset = BitConverter.ToInt32(rawBytes, offset + 92);
                int unk2Offset = BitConverter.ToInt32(rawBytes, offset + 100);
                int charaUnlockOffset = BitConverter.ToInt32(rawBytes, offset + 180);
                int stagePortraitOffset = BitConverter.ToInt32(rawBytes, offset + 188);
                int qedFileOffset = BitConverter.ToInt32(rawBytes, offset + 116);

                qxd_File.Quests.Add(new Quest_Data());
                qxd_File.Quests[i].Name = StringEx.GetString(rawBytes, offset, false, StringEx.EncodingType.ASCII, 16);

                qxd_File.Quests[i].Index = BitConverter.ToInt32(rawBytes, offset + 16).ToString();
                qxd_File.Quests[i].I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
                qxd_File.Quests[i].I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
                qxd_File.Quests[i].I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
                qxd_File.Quests[i].I_40 = BitConverter.ToInt16(rawBytes, offset + 40);
                qxd_File.Quests[i].I_42 = BitConverter.ToInt16(rawBytes, offset + 42);
                qxd_File.Quests[i].I_44 = BitConverter.ToInt16(rawBytes, offset + 44);
                qxd_File.Quests[i].I_46 = BitConverter.ToInt16(rawBytes, offset + 46);
                qxd_File.Quests[i].I_64 = BitConverter.ToInt32(rawBytes, offset + 64);
                qxd_File.Quests[i].I_104 = BitConverter.ToInt16(rawBytes, offset + 104);
                qxd_File.Quests[i].I_106 = BitConverter.ToInt16(rawBytes, offset + 106);
                qxd_File.Quests[i].I_108 = BitConverter.ToInt16(rawBytes, offset + 108);
                qxd_File.Quests[i].I_110 = BitConverter.ToInt16(rawBytes, offset + 110);
                qxd_File.Quests[i].I_120 = BitConverter.ToInt32(rawBytes, offset + 120);
                qxd_File.Quests[i].I_124 = BitConverter.ToInt32(rawBytes, offset + 124);
                qxd_File.Quests[i].I_128 = BitConverter.ToInt32(rawBytes, offset + 128);
                qxd_File.Quests[i].I_132 = BitConverter.ToInt32(rawBytes, offset + 132);
                qxd_File.Quests[i].I_136 = BitConverter.ToInt32(rawBytes, offset + 136);
                qxd_File.Quests[i].I_140 = BitConverter.ToInt32(rawBytes, offset + 140);
                qxd_File.Quests[i].I_144 = BitConverter.ToInt32(rawBytes, offset + 144);
                qxd_File.Quests[i].I_148 = BitConverter.ToInt32(rawBytes, offset + 148);
                qxd_File.Quests[i].I_152 = BitConverter.ToInt32(rawBytes, offset + 152);
                qxd_File.Quests[i].I_156 = BitConverter.ToInt32(rawBytes, offset + 156);
                qxd_File.Quests[i].I_192 = BitConverter.ToInt32(rawBytes, offset + 192);
                qxd_File.Quests[i].I_248 = BitConverter.ToInt32(rawBytes, offset + 248);
                qxd_File.Quests[i].I_252 = BitConverter.ToInt32(rawBytes, offset + 252);
                qxd_File.Quests[i].I_256 = (QxdUpdate)BitConverter.ToInt32(rawBytes, offset + 256);
                qxd_File.Quests[i].I_260 = (QxdDlc)BitConverter.ToInt32(rawBytes, offset + 260);
                qxd_File.Quests[i].I_264 = BitConverter.ToInt32(rawBytes, offset + 264);
                qxd_File.Quests[i].I_268 = BitConverter.ToInt16(rawBytes, offset + 268);
                qxd_File.Quests[i].I_270 = BitConverter.ToInt16(rawBytes, offset + 270);
                qxd_File.Quests[i].I_272 = BitConverter.ToInt16(rawBytes, offset + 272);
                qxd_File.Quests[i].I_274 = BitConverter.ToInt16(rawBytes, offset + 274);
                qxd_File.Quests[i].F_276 = BitConverter.ToSingle(rawBytes, offset + 276);
                qxd_File.Quests[i].I_280 = BitConverter.ToInt32(rawBytes, offset + 280);


                qxd_File.Quests[i].I_48 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 48, 4);
                qxd_File.Quests[i].I_68 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 68, 5);

                qxd_File.Quests[i].I_232 = BitConverter_Ex.ToInt16Array(rawBytes, offset + 232, 8);

                if (unk1count > 0)
                {
                    int tempOffset = unk1Offset;
                    qxd_File.Quests[i].UnknownNum1 = new List<UnkNum1>();
                    for (int a = 0; a < unk1count; a++)
                    {

                        qxd_File.Quests[i].UnknownNum1.Add(new UnkNum1()
                        {
                            I_00 = BitConverter_Ex.ToInt16Array(rawBytes, tempOffset, 16)
                        });

                        tempOffset += 32;
                    }
                }
                if (unk2Count > 0)
                {
                    int tempOffset = unk2Offset;
                    qxd_File.Quests[i].UnknownNum2 = new List<UnkNum2>();
                    for (int a = 0; a < unk2Count; a++)
                    {
                        qxd_File.Quests[i].UnknownNum2.Add(new UnkNum2()
                        {
                            I_00 = BitConverter_Ex.ToInt16Array(rawBytes, tempOffset, 16)
                        });

                        tempOffset += 32;
                    }
                }

                if (equipRewardCount > 0)
                {
                    qxd_File.Quests[i].EquipReward = new List<EquipmentReward>();
                    int equipOffset = equipRewardOffset;
                    for (int b = 0; b < equipRewardCount; b++)
                    {
                        qxd_File.Quests[i].EquipReward.Add(new EquipmentReward()
                        {
                            I_00 = (QxdItemType)BitConverter.ToInt32(rawBytes, equipOffset),
                            I_04 = BitConverter.ToInt32(rawBytes, equipOffset + 4),
                            I_08 = BitConverter.ToInt32(rawBytes, equipOffset + 8),
                            I_12 = BitConverter.ToInt32(rawBytes, equipOffset + 12),
                            I_16 = BitConverter.ToInt32(rawBytes, equipOffset + 16),
                            I_20 = BitConverter.ToInt16(rawBytes, equipOffset + 20),
                            F_24 = BitConverter.ToSingle(rawBytes, equipOffset + 24),
                            I_28 = BitConverter.ToInt32(rawBytes, equipOffset + 28)
                        });

                        equipOffset += 32;
                    }
                }

                if (skillRewardCount > 0)
                {
                    qxd_File.Quests[i].Skill_Reward = new List<SkillReward>();
                    int skillOffset = skillRewardOffset;
                    for (int b = 0; b < skillRewardCount; b++)
                    {
                        qxd_File.Quests[i].Skill_Reward.Add(new SkillReward()
                        {
                            I_00 = (QxdSkillType)BitConverter.ToInt32(rawBytes, skillOffset + 0),
                            I_04 = BitConverter.ToInt32(rawBytes, skillOffset + 4),
                            I_08 = BitConverter.ToInt32(rawBytes, skillOffset + 8),
                            I_12 = BitConverter.ToInt32(rawBytes, skillOffset + 12),
                            F_16 = BitConverter.ToSingle(rawBytes, skillOffset + 16)
                        });
                        skillOffset += 20;
                    }
                }



                if (charaUnlockCount > 0)
                {
                    qxd_File.Quests[i].Chara_Unlock = new List<CharaUnlock>();
                    int charaOffset = charaUnlockOffset;
                    for (int b = 0; b < charaUnlockCount; b++)
                    {
                        qxd_File.Quests[i].Chara_Unlock.Add(new CharaUnlock()
                        {
                            ShortName = StringEx.GetString(rawBytes, charaOffset, false, StringEx.EncodingType.ASCII, 3),
                            CostumeIndex = BitConverter.ToInt16(rawBytes, charaOffset + 4),
                            I_06 = BitConverter.ToInt16(rawBytes, charaOffset + 6)
                        });
                        charaOffset += 8;
                    }
                }

                int portraitOffset = 196;
                qxd_File.Quests[i].EnemyPortraitDisplay = new List<EnemyPortrait>();
                for (int b = 0; b < 6; b++)
                {
                    qxd_File.Quests[i].EnemyPortraitDisplay.Add(new EnemyPortrait());
                    qxd_File.Quests[i].EnemyPortraitDisplay[b].CharaID = BitConverter.ToInt16(rawBytes, offset + portraitOffset);
                    qxd_File.Quests[i].EnemyPortraitDisplay[b].CostumeIndex = BitConverter.ToInt16(rawBytes, offset + portraitOffset + 2);
                    qxd_File.Quests[i].EnemyPortraitDisplay[b].State = BitConverter.ToInt16(rawBytes, offset + portraitOffset + 4);
                    portraitOffset += 6;
                }

                if (stagePortraitCount > 0)
                {
                    qxd_File.Quests[i].StageDisplay = new List<short>();
                    for (int j = 0; j < 32; j += 2)
                    {
                        qxd_File.Quests[i].StageDisplay.Add(BitConverter.ToInt16(rawBytes, stagePortraitOffset + j));
                    }

                }

                if (msgEntryCount > 0)
                {
                    qxd_File.Quests[i].MsgFiles = new List<string>();
                    int msgOffset = msgEntryOffset;
                    for (int j = 0; j < msgEntryCount; j++)
                    {
                        qxd_File.Quests[i].MsgFiles.Add(StringEx.GetString(rawBytes, msgOffset, false, StringEx.EncodingType.ASCII, 32));
                        msgOffset += 32;
                    }
                }

                if (qedFileCount > 0)
                {
                    qxd_File.Quests[i].QedFiles = new List<string>();
                    int qedOffset = qedFileOffset;
                    for (int j = 0; j < qedFileCount; j++)
                    {
                        qxd_File.Quests[i].QedFiles.Add(StringEx.GetString(rawBytes, qedOffset, false, StringEx.EncodingType.ASCII, 32));
                        qedOffset += 32;
                    }
                }
                offset += 284;
            }

            //NEW: Remove all dummy entries on parse. (They will be added back in as needed when writing back to binary)
            for (int i = qxd_File.Quests.Count - 1; i >= 0; i--)
            {
                if (qxd_File.Quests[i].IsPlaceholder())
                {
                    qxd_File.Quests.RemoveAt(i);
                }
            }
        }

        private void ParseEndFloats()
        {
            qxd_File.EndFloats = new float[8];

            for (int i = 0; i < 32; i += 4)
            {
                qxd_File.EndFloats[i / 4] = BitConverter.ToSingle(rawBytes, floatSectionOffset + i);
            }

            qxd_File.I_40 = BitConverter.ToInt32(rawBytes, 40);
        }

        private void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QXD_File));
            serializer.SerializeToFile(qxd_File, saveLocation + ".xml");
        }

        private void DisplayUsedDialogueIDs()
        {
            List<int> ids = new List<int>();

            for (int i = 0; i < qxd_File.Quests.Count(); i++)
            {
                bool skipThisPass = false;
                for (int a = 0; a < ids.Count(); a++)
                {
                    if (ids[a] == qxd_File.Quests[i].I_20)
                    {
                        skipThisPass = true;
                    }
                }
                if (skipThisPass == false)
                {
                    ids.Add(qxd_File.Quests[i].I_20);
                }
            }

            for (int i = 0; i < ids.Count(); i++)
            {
                Console.WriteLine(ids[i]);
            }

            Console.ReadLine();
        }


    }
}
