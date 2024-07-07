using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCP
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        public OCP_File ocpFile = new OCP_File();

        public Parser(string path, bool _writeXml)
        {
            saveLocation = path;
            rawBytes = File.ReadAllBytes(path);

            Parse();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(OCP_File));
                serializer.SerializeToFile(ocpFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] bytes)
        {
            rawBytes = bytes;

            Parse();
        }

        private void Parse()
        {
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int offset = 16;

            if (count > 0)
            {
                ocpFile.TableEntries = new List<OCP_TableEntry>();

                for(int i = 0; i < count; i++)
                {
                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset);
                    int subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (20 * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;
                    ocpFile.TableEntries.Add(new OCP_TableEntry() { PartnerID = BitConverter.ToInt32(rawBytes, offset + 12) });
                    
                    if(subEntryCount > 0)
                    {
                        ocpFile.TableEntries[i].SubEntries = new List<OCP_SubEntry>();
                    }

                    for (int a = 0; a < subEntryCount; a++)
                    {
                        ocpFile.TableEntries[i].SubEntries.Add(new OCP_SubEntry()
                        {
                            I_04 = BitConverter.ToInt32(rawBytes, subDataOffset + 4),
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
