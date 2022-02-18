using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCO
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        private OCO_File octFile = new OCO_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCO_File));
                serializer.SerializeToFile(octFile, saveLocation + ".xml");
            }
        }

        private void Parse()
        {
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int offset = 16;

            if (count > 0)
            {
                octFile.TableEntries = new List<OCO_TableEntry>();

                for(int i = 0; i < count; i++)
                {
                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset);
                    int subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (20 * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;
                    octFile.TableEntries.Add(new OCO_TableEntry() { Index = BitConverter.ToInt32(rawBytes, offset + 12) });
                    
                    if(subEntryCount > 0)
                    {
                        octFile.TableEntries[i].SubEntries = new List<OCO_SubEntry>();
                    }

                    for (int a = 0; a < subEntryCount; a++)
                    {
                        octFile.TableEntries[i].SubEntries.Add(new OCO_SubEntry()
                        {
                            I_04 = BitConverter.ToUInt32(rawBytes, subDataOffset + 4),
                            I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8),
                            I_12 = BitConverter.ToUInt32(rawBytes, subDataOffset + 12),
                            I_16 = BitConverter.ToUInt32(rawBytes, subDataOffset + 16)
                        });

                        subDataOffset += 20;
                    }

                    offset += 16;
                }
                
            }
        }



    }
}
