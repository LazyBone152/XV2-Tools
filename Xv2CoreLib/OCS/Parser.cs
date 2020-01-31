using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        List<byte> bytes;
        private OCS_File octFile = new OCS_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);
            bytes = rawBytes.ToList();

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCS_File));
                serializer.SerializeToFile(octFile, saveLocation + ".xml");
            }
        }

        private void Parse()
        {
            octFile.Version = BitConverter.ToUInt16(rawBytes, 6);
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int firstTableOffset = 16;
            int secondTableOffset = (int)(count * 16) + 16;

            if (count > 0)
            {
                octFile.TableEntries = new List<OCS_TableEntry>();

                for(int i = 0; i < count; i++)
                {
                    int subTableCount = BitConverter.ToInt32(rawBytes, firstTableOffset);
                    int subTableOffset = BitConverter.ToInt32(rawBytes, firstTableOffset + 4) + (16 * BitConverter.ToInt32(rawBytes, firstTableOffset + 8)) + 16;
                    octFile.TableEntries.Add(new OCS_TableEntry() { Index = BitConverter.ToInt32(rawBytes, firstTableOffset + 12) });
                    
                    if(subTableCount > 0)
                    {
                        octFile.TableEntries[i].SubEntries = new List<OCS_SubTableEntry>();
                    }

                    for(int a = 0; a < subTableCount; a++)
                    {
                        int subEntryCount2 = BitConverter.ToInt32(rawBytes, subTableOffset);
                        int subDataOffset2 = BitConverter.ToInt32(rawBytes, subTableOffset + 4) + (GetSkillDataSize() * BitConverter.ToInt32(rawBytes, subTableOffset + 8)) + 16;
                        octFile.TableEntries[i].SubEntries.Add(new OCS_SubTableEntry() { Skill_Type = (OCS_SubTableEntry.SkillType)BitConverter.ToInt32(rawBytes, subTableOffset + 12) });

                        if (subEntryCount2 > 0)
                        {
                            octFile.TableEntries[i].SubEntries[a].SubEntries = new List<OCS_SubEntry>();
                        }

                        for (int s = 0; s < subEntryCount2; s++)
                        {
                            if(BitConverter.ToInt32(rawBytes, subDataOffset2 + 16) != (int)octFile.TableEntries[i].SubEntries[a].Skill_Type)
                            {
                                throw new Exception("Skill type mismatch.");
                            }

                            if(octFile.Version == 16)
                            {
                                octFile.TableEntries[i].SubEntries[a].SubEntries.Add(new OCS_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                    I_12 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                    I_20 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20)
                                });

                                subDataOffset2 += 24;
                            }
                            else if (octFile.Version == 20)
                            {
                                octFile.TableEntries[i].SubEntries[a].SubEntries.Add(new OCS_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 4),
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 8),
                                    I_12 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 12),
                                    I_20 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 20),
                                    I_24 = BitConverter.ToInt32(rawBytes, subDataOffset2 + 24)
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
