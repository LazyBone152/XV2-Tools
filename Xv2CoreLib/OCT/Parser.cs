using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.OCS;
using YAXLib;

namespace Xv2CoreLib.OCT
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        public OCT_File octFile = new OCT_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCT_File));
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
            int offset = 16;

            if (count > 0)
            {
                octFile.OctTableEntries = new List<OCT_TableEntry>();

                for (int i = 0; i < count; i++)
                {
                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset);
                    int subDataOffset;

                    if (octFile.Version >= 24)
                        subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (28 * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;
                    else
                        subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (24 * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;

                    octFile.OctTableEntries.Add(new OCT_TableEntry() { PartnerID = BitConverter.ToInt32(rawBytes, offset + 12) });

                    if (subEntryCount > 0)
                    {
                        octFile.OctTableEntries[i].SubEntries = new List<OCT_SubEntry>();
                    }

                    for (int a = 0; a < subEntryCount; a++)
                    {
                        switch (octFile.Version)
                        {
                            case 20:
                                octFile.OctTableEntries[i].SubEntries.Add(new OCT_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset + 4),
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8),
                                    I_12 = BitConverter.ToInt32(rawBytes, subDataOffset + 12),
                                    I_16 = BitConverter.ToInt32(rawBytes, subDataOffset + 16),
                                    I_20 = BitConverter.ToInt32(rawBytes, subDataOffset + 20)
                                });

                                subDataOffset += 24;
                                break;
                            case 24:
                                octFile.OctTableEntries[i].SubEntries.Add(new OCT_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset + 4),
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8),
                                    I_12 = BitConverter.ToInt32(rawBytes, subDataOffset + 12),
                                    STP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset + 16),
                                    I_16 = BitConverter.ToInt32(rawBytes, subDataOffset + 20),
                                    I_20 = BitConverter.ToInt32(rawBytes, subDataOffset + 24)
                                });

                                subDataOffset += 28;
                                break;
                            default:
                                throw new Exception("Unknown OCT version.");
                        }
                    }

                    offset += 16;
                }

            }
        }



    }
}
