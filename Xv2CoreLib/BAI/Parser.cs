using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAI
{
    public class Parser
    {
        public List<int> UsedValues = new List<int>();

        string saveLocation { get; set; }
        public BAI_File baiFile { get; private set; } = new BAI_File();
        byte[] rawBytes { get; set; }


        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);

            ParseBai();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BAI_File));
                serializer.SerializeToFile(baiFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            ParseBai();
        }

        public void ParseBai()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);

            if(count > 0)
            {
                baiFile.Entries = new List<BAI_Entry>();

                for(int i = 0; i < count; i++)
                {
                    baiFile.Entries.Add(new BAI_Entry());
                    baiFile.Entries[i].I_00 = BitConverter.ToUInt32(rawBytes, offset + 0);
                    baiFile.Entries[i].I_04 = BitConverter.ToInt32(rawBytes, offset + 4);
                    baiFile.Entries[i].I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
                    baiFile.Entries[i].I_12 = BitConverter.ToInt32(rawBytes, offset + 12);

                    int subEntryCount = BitConverter.ToInt32(rawBytes, offset + 16);
                    int subEntryOffset = BitConverter.ToInt32(rawBytes, offset + 20);

                    if(subEntryCount > 0)
                    {
                        baiFile.Entries[i].SubEntries = new List<BAI_SubEntry>();
                        for(int a = 0; a < subEntryCount; a++)
                        {
                            baiFile.Entries[i].SubEntries.Add(new BAI_SubEntry()
                            {
                                Name = Utils.GetString(rawBytes.ToList(), subEntryOffset, 8),
                                I_08 = BitConverter_Ex.ToBooleanFromInt32(rawBytes, subEntryOffset + 8),
                                I_12 = (BAI_SubEntry.ActivationConditionTarget)BitConverter.ToInt32(rawBytes, subEntryOffset + 12),
                                I_16 = (BAI_SubEntry.ActivationConditionTarget)BitConverter.ToInt32(rawBytes, subEntryOffset + 16),
                                I_20 = (BAI_SubEntry.ActivationConditionTarget)BitConverter.ToInt32(rawBytes, subEntryOffset + 20),
                                I_24 = BitConverter.ToInt32(rawBytes, subEntryOffset + 24),
                                I_28 = BitConverter.ToInt32(rawBytes, subEntryOffset + 28),
                                I_32 = BitConverter.ToInt32(rawBytes, subEntryOffset + 32),
                                I_36 = BitConverter.ToInt32(rawBytes, subEntryOffset + 36),
                                I_40 = BitConverter.ToInt32(rawBytes, subEntryOffset + 40),
                                I_44 = BitConverter.ToInt32(rawBytes, subEntryOffset + 44),
                                I_48 = BitConverter.ToInt32(rawBytes, subEntryOffset + 48),
                                I_52 = BitConverter.ToInt32(rawBytes, subEntryOffset + 52),
                                I_56 = BitConverter.ToInt32(rawBytes, subEntryOffset + 56),
                                I_60 = BitConverter.ToInt32(rawBytes, subEntryOffset + 60),
                                I_64 = BitConverter.ToInt32(rawBytes, subEntryOffset + 64),
                                F_68 = BitConverter.ToSingle(rawBytes, subEntryOffset + 68),
                                F_72 = BitConverter.ToSingle(rawBytes, subEntryOffset + 72),
                                F_76 = BitConverter.ToSingle(rawBytes, subEntryOffset + 76),
                                F_80 = BitConverter.ToSingle(rawBytes, subEntryOffset + 80),
                            });
                            if(BitConverter.ToInt32(rawBytes, subEntryOffset + 56) == 10)
                            {
                                UsedValues.Add(BitConverter.ToInt32(rawBytes, subEntryOffset + 60));
                            }

                            subEntryOffset += 84;
                        }

                    }

                    offset += 24;
                }
            }

        }

    }
}
