using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.CUS
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        CUS_File cusFile = new CUS_File();
        

        public Parser(string location, bool _writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            Parse();
            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CUS_File));
                serializer.SerializeToFile(cusFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            if(rawBytes != null)
            {
                Parse();
            }
            else
            {
                cusFile = null;
            }
        }

        public CUS_File GetCusFile() 
        {
            return cusFile;
        }
        
        void Parse() 
        {
            //counts
            int skillsetCount = BitConverter.ToInt32(rawBytes, 8);
            int superCount = BitConverter.ToInt32(rawBytes, 16);
            int ultimateCount = BitConverter.ToInt32(rawBytes, 20);
            int evasiveCount = BitConverter.ToInt32(rawBytes, 24);
            int unkCount = BitConverter.ToInt32(rawBytes, 28);
            int blastCount = BitConverter.ToInt32(rawBytes, 32);
            int awokenCount = BitConverter.ToInt32(rawBytes, 36);

            //offsets
            int skillsetOffset = BitConverter.ToInt32(rawBytes, 12);
            int superOffset = BitConverter.ToInt32(rawBytes, 40);
            int ultimateOffset = BitConverter.ToInt32(rawBytes, 44);
            int evasiveOffset = BitConverter.ToInt32(rawBytes, 48);
            int unkOffset = BitConverter.ToInt32(rawBytes, 52);
            int blastOffset = BitConverter.ToInt32(rawBytes, 56);
            int awokenOffset = BitConverter.ToInt32(rawBytes, 60);

            if(skillsetCount > 0)
            {
                cusFile.Skillsets = new List<Skillset>();
                for (int i = 0; i < skillsetCount; i++)
                {
                    cusFile.Skillsets.Add(GetSkillset(skillsetOffset));
                    skillsetOffset += 32;
                }
            }

            cusFile.SuperSkills = GetSkillEntries(superCount, superOffset);
            cusFile.UltimateSkills = GetSkillEntries(ultimateCount, ultimateOffset);
            cusFile.EvasiveSkills = GetSkillEntries(evasiveCount, evasiveOffset);
            cusFile.UnkSkills = GetSkillEntries(unkCount, unkOffset);
            cusFile.BlastSkills = GetSkillEntries(blastCount, blastOffset);
            cusFile.AwokenSkills = GetSkillEntries(awokenCount, awokenOffset);
            

        }

        private Skillset GetSkillset(int offset)
        {
            return new Skillset()
            {
                I_00 = BitConverter.ToInt32(rawBytes, offset + 0).ToString(),
                I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                I_26 = BitConverter.ToUInt16(rawBytes, offset + 26)
            };
        }

        private List<Skill> GetSkillEntries(int count, int offset) 
        {
            var skillEntries = new List<Skill>();

            for (int i = 0; i < count; i++)
            {
                skillEntries.Add(new Skill()
                {
                    ShortName = StringEx.GetString(rawBytes, offset, false, StringEx.EncodingType.ASCII, 4),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    ID1 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    ID2 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = (Skill.RaceLock)rawBytes[offset + 12],
                    I_13 = rawBytes[offset + 13],
                    FilesLoadedFlags1 = (Skill.FilesLoadedFlags)BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    EanPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 20), false),
                    CamEanPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24), false),
                    EepkPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 28), false),
                    SePath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32), false),
                    VoxPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 36), false),
                    AfterBacPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40), false),
                    AfterBcmPath = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 44), false),
                    I_48 = BitConverter.ToUInt16(rawBytes, offset + 48),
                    I_50 = BitConverter.ToUInt16(rawBytes, offset + 50),
                    I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                    I_54 = BitConverter.ToUInt16(rawBytes, offset + 54),
                    PUP = BitConverter.ToUInt16(rawBytes, offset + 56),
                    CusAura = BitConverter.ToInt16(rawBytes, offset + 58),
                    CharaSwapId = BitConverter.ToUInt16(rawBytes, offset + 60),
                    I_62 = BitConverter.ToInt16(rawBytes, offset + 62),
                    NumTransformations = BitConverter.ToUInt16(rawBytes, offset + 64),
                    I_66 = BitConverter.ToUInt16(rawBytes, offset + 66)
                });

                offset += 68;
            }

            return skillEntries;
        }
    }
}
