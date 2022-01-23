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
        public OCS_File octFile { get; private set; } = new OCS_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCS_File));
                serializer.SerializeToFile(octFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;
            Parse();
        }

        private void Parse()
        {
            octFile.Version = BitConverter.ToUInt16(rawBytes, 6);
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int firstTableOffset = 16;
            int secondTableOffset = (int)(count * 16) + 16;

            if (count > 0)
            {
                octFile.Partners = new List<OCS_Partner>();

                for(int i = 0; i < count; i++)
                {
                    int subTableCount = BitConverter.ToInt32(rawBytes, firstTableOffset);
                    int subTableOffset = BitConverter.ToInt32(rawBytes, firstTableOffset + 4) + (16 * BitConverter.ToInt32(rawBytes, firstTableOffset + 8)) + 16;
                    octFile.Partners.Add(new OCS_Partner() { Index = BitConverter.ToInt32(rawBytes, firstTableOffset + 12) });
                    
                    if(subTableCount > 0)
                    {
                        octFile.Partners[i].SkillTypes = new List<OCS_SkillTypeGroup>();
                    }

                    for(int a = 0; a < subTableCount; a++)
                    {
                        int subEntryCount2 = BitConverter.ToInt32(rawBytes, subTableOffset);
                        int subDataOffset2 = BitConverter.ToInt32(rawBytes, subTableOffset + 4) + (GetSkillDataSize() * BitConverter.ToInt32(rawBytes, subTableOffset + 8)) + 16;
                        octFile.Partners[i].SkillTypes.Add(new OCS_SkillTypeGroup() { Skill_Type = (OCS_SkillTypeGroup.SkillType)BitConverter.ToInt32(rawBytes, subTableOffset + 12) });

                        if (subEntryCount2 > 0)
                        {
                            octFile.Partners[i].SkillTypes[a].Skills = new List<OCS_Skill>();
                        }

                        for (int s = 0; s < subEntryCount2; s++)
                        {
                            if(BitConverter.ToInt32(rawBytes, subDataOffset2 + 16) != (int)octFile.Partners[i].SkillTypes[a].Skill_Type)
                            {
                                throw new Exception("Skill type mismatch.");
                            }

                            if(octFile.Version == 16)
                            {
                                octFile.Partners[i].SkillTypes[a].Skills.Add(new OCS_Skill()
                                {
                                    EntryID = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                    TP_Cost_Toggle = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                    TP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                    SkillID2 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20)
                                });

                                subDataOffset2 += 24;
                            }
                            else if (octFile.Version == 20)
                            {
                                octFile.Partners[i].SkillTypes[a].Skills.Add(new OCS_Skill()
                                {
                                    EntryID = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                    TP_Cost_Toggle = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                    TP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                    SkillID2 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20),
                                    DLC_Flag =BitConverter.ToInt32(rawBytes, subDataOffset2 + 24)
                                });
                                subDataOffset2 += 28;
                            }
                            else
                            {
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
            switch (octFile.Version)
            {
                case 16:
                    return 24;
                case 20:
                    return 28;
                default:
                    throw new Exception("Unknown OCS version.");
            }
        }


    }
}
