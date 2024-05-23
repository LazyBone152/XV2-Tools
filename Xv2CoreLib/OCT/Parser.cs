using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.OCT
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        private OCT_File octFile = new OCT_File();

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

        private void Parse()
        {
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int offset = 16;

            if (rawBytes[6] != 24)
                throw new Exception($"Unsupported OCT format: {rawBytes[6]}");

            if (count > 0)
            {
                octFile.OctTableEntries = new List<OCT_TableEntry>();

                for (int i = 0; i < count; i++)
                {
                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset);
                    int subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (28 * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;
                    octFile.OctTableEntries.Add(new OCT_TableEntry() { Index = BitConverter.ToUInt32(rawBytes, offset + 12) });

                    if (subEntryCount > 0)
                    {
                        octFile.OctTableEntries[i].OctSubEntries = new List<OCT_SubEntry>();
                    }

                    for (int a = 0; a < subEntryCount; a++)
                    {
                        octFile.OctTableEntries[i].OctSubEntries.Add(new OCT_SubEntry()
                        {
                            Index = BitConverter.ToInt32(rawBytes, subDataOffset + 4),
                            I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8),
                            I_12 = BitConverter.ToInt32(rawBytes, subDataOffset + 12),
                            STP_Cost = BitConverter.ToInt32(rawBytes, subDataOffset + 16),
                            I_16 = BitConverter.ToInt32(rawBytes, subDataOffset + 20),
                            I_20 = BitConverter.ToInt32(rawBytes, subDataOffset + 24)
                        });

                        subDataOffset += 28;
                    }

                    offset += 16;
                }

            }
        }



    }
}
