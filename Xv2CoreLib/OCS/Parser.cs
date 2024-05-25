using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    public class Parser
    {
        private string saveLocation;
        private byte[] rawBytes;
        public OCS_File ocsFile { get; private set; } = new OCS_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCS_File));
                serializer.SerializeToFile(ocsFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;
            Parse();
        }

        private void Parse()
        {
            ocsFile.Version = BitConverter.ToUInt16(rawBytes, 6);
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int firstTableOffset = 16;
            int secondTableOffset = (int)(count * 16) + 16;

            if (count > 0)
            {
                ocsFile.Partners = new List<OCS_Partner>();

                for(int i = 0; i < count; i++)
                {
                    int subTableCount = BitConverter.ToInt32(rawBytes, firstTableOffset);
                    int subTableOffset = BitConverter.ToInt32(rawBytes, firstTableOffset + 4) + (16 * BitConverter.ToInt32(rawBytes, firstTableOffset + 8)) + 16;
                    ocsFile.Partners.Add(new OCS_Partner() { Index = BitConverter.ToInt32(rawBytes, firstTableOffset + 12) });
                    
                    if(subTableCount > 0)
                    {
                        ocsFile.Partners[i].SkillTypes = new List<OCS_SkillTypeGroup>();
                    }

                    for(int a = 0; a < subTableCount; a++)
                    {
                        int subEntryCount2 = BitConverter.ToInt32(rawBytes, subTableOffset);
                        int subDataOffset2 = BitConverter.ToInt32(rawBytes, subTableOffset + 4) + (GetSkillDataSize() * BitConverter.ToInt32(rawBytes, subTableOffset + 8)) + 16;
                        ocsFile.Partners[i].SkillTypes.Add(new OCS_SkillTypeGroup() { Skill_Type = (OCS_SkillTypeGroup.SkillType)BitConverter.ToInt32(rawBytes, subTableOffset + 12) });

                        if (subEntryCount2 > 0)
                        {
                            ocsFile.Partners[i].SkillTypes[a].Skills = new List<OCS_Skill>();
                        }

                        for (int s = 0; s < subEntryCount2; s++)
                        {
                            if(BitConverter.ToInt32(rawBytes, subDataOffset2 + GetSkillTypeOffset()) != (int)ocsFile.Partners[i].SkillTypes[a].Skill_Type)
                            {
                                throw new Exception("Skill type mismatch.");
                            }

                            switch(ocsFile.Version)
                            {
                                case 16:
                                    ocsFile.Partners[i].SkillTypes[a].Skills.Add(new OCS_Skill()
                                    {
                                        EntryID = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                        TP_Cost_Toggle = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                        TP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                        SkillID2 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20)
                                    });

                                    subDataOffset2 += 24;
                                    break;
                                case 20:
                                    ocsFile.Partners[i].SkillTypes[a].Skills.Add(new OCS_Skill()
                                    {
                                        EntryID = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                        TP_Cost_Toggle = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                        TP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                        SkillID2 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20),
                                        DLC_Flag = BitConverter.ToInt32(rawBytes, subDataOffset2 + 24)
                                    });

                                    subDataOffset2 += 28;
                                    break;
                                case 28:
                                    ocsFile.Partners[i].SkillTypes[a].Skills.Add(new OCS_Skill()
                                    {
                                        EntryID = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                        TP_Cost_Toggle = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                        TP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                        STP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 16),
                                        SkillID2 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 24),
                                        DLC_Flag = BitConverter.ToInt32(rawBytes, subDataOffset2 + 28),
                                        NEW_I_32 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 32)
                                    });

                                    subDataOffset2 += 36;
                                    break;
                                default:
                                    throw new Exception("Unknown OCS version.");
                            }
                            
                        }

                        subTableOffset += 16;

                    }

                    firstTableOffset += 16;
                }
                
            }
        }

        private int GetSkillDataSize()
        {
            switch (ocsFile.Version)
            {
                case 16:
                    return 24;
                case 20:
                    return 28;
                case 28:
                    return 36;
                default:
                    throw new Exception("Unknown OCS version.");
            }
        }

        private int GetSkillTypeOffset()
        {
            switch (ocsFile.Version)
            {
                case 16:
                case 20:
                    return 16;
                case 28:
                    return 20;
                default:
                    throw new Exception("Unknown OCS version.");
            }
        }


    }
}
