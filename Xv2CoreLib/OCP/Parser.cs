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
        private string saveLocation;
        private byte[] rawBytes;
        public OCP_File ocpFile { get; private set; } = new OCP_File();

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
            ocpFile.Version = BitConverter.ToUInt16(rawBytes, 6);
            uint count = BitConverter.ToUInt32(rawBytes, 8);
            int offset = 16;

            if (count > 0)
            {
                ocpFile.TableEntries = new List<OCP_TableEntry>();

                for(int i = 0; i < count; i++)
                {
                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset);
                    int subDataOffset = BitConverter.ToInt32(rawBytes, offset + 4) + (GetSubEntryDataSize() * BitConverter.ToInt32(rawBytes, offset + 8)) + 16;
                    ocpFile.TableEntries.Add(new OCP_TableEntry() { PartnerID = BitConverter.ToInt32(rawBytes, offset + 12) });
                    
                    if(subEntryCount > 0)
                    {
                        ocpFile.TableEntries[i].SubEntries = new List<OCP_SubEntry>();
                    }

                    for (int a = 0; a < subEntryCount; a++)
                    {
                        switch (ocpFile.Version)
                        {
                            case 16:
                                ocpFile.TableEntries[i].SubEntries.Add(new OCP_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset + 4), //Order
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8), //TP_Cost_Toggle
                                    I_12 = BitConverter.ToUInt32(rawBytes, subDataOffset + 12), //TP_Cost
                                    I_16 = BitConverter.ToUInt32(rawBytes, subDataOffset + 16) //StatType
                                });

                                subDataOffset += 20;
                                break;
                            case 20:
                                ocpFile.TableEntries[i].SubEntries.Add(new OCP_SubEntry()
                                {
                                    I_04 = BitConverter.ToInt32(rawBytes, subDataOffset + 4), //Order
                                    I_08 = BitConverter.ToInt32(rawBytes, subDataOffset + 8), //TP_Cost_Toggle
                                    I_12 = BitConverter.ToUInt32(rawBytes, subDataOffset + 12), //TP_Cost
                                    NEW_I_16 = BitConverter.ToUInt32(rawBytes, subDataOffset + 16), //STP_Cost
                                    I_16 = BitConverter.ToUInt32(rawBytes, subDataOffset + 20) //StatType
                                });

                                subDataOffset += 24;
                                break;
                            default:
                                throw new Exception("Unknown OCP version.");
                        }
                    }
                    offset += 16;
                }

            }
        }

        private int GetSubEntryDataSize()
        {
            switch (ocpFile.Version)
            {
                case 16:
                    return 20;
                case 20:
                    return 24;
                default:
                    throw new Exception("Unknown OCP version.");
            }
        }
    }
}
