using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ATA
{
    [YAXSerializeAs("ATA")]
    public class ATA_File
    {
        private List<byte> bytes;

        [YAXAttributeForClass]
        public ushort I_06 { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ATA_Entry")]
        public List<ATA_Entry> AtaEntries { get; set; }

        //Parsing values
        byte[] rawBytes { get; set; }

        public ATA_File (string path, bool _writeXml)
        {
            rawBytes = File.ReadAllBytes(path);
            AtaEntries = new List<ATA_Entry>();
            ParseAta();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ATA_File));
                serializer.SerializeToFile(this, path + ".xml");
            }
        }

        public ATA_File()
        {

        }

        private void ParseAta()
        {
            //Entry offset+count
            int offset = BitConverter.ToInt32(rawBytes, 12) + 16;
            int count = BitConverter.ToInt32(rawBytes, offset);
            offset += BitConverter.ToInt32(rawBytes, offset + 4);

            I_06 = BitConverter.ToUInt16(rawBytes, 6);
            I_08 = BitConverter.ToInt32(rawBytes, 8);
            
            //Entries
            int strOffset = 16;
            
            for(int i = 0; i < count; i++)
            {
                AtaEntries.Add(new ATA_Entry()
                {
                    Name = StringEx.GetString(rawBytes, strOffset),
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
                    I_64 = BitConverter.ToInt32(rawBytes, offset + 64),
                    I_68 = BitConverter.ToInt32(rawBytes, offset + 68),
                    I_72 = BitConverter.ToInt32(rawBytes, offset + 72),
                    I_76 = BitConverter.ToInt32(rawBytes, offset + 76),
                    I_80 = BitConverter.ToInt32(rawBytes, offset + 80),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84),
                    I_88 = BitConverter.ToInt32(rawBytes, offset + 88),
                    I_92 = BitConverter.ToInt32(rawBytes, offset + 92),
                    I_96 = BitConverter.ToInt32(rawBytes, offset + 96),
                    I_100 = BitConverter.ToInt32(rawBytes, offset + 100),
                    I_104 = BitConverter.ToInt32(rawBytes, offset + 104),
                    I_108 = BitConverter.ToInt32(rawBytes, offset + 108),
                    I_112 = BitConverter.ToInt32(rawBytes, offset + 112)
                });
                strOffset += AtaEntries[i].Name.Length + 1;
                offset += 116;
            }
        }

        //Deserialization
        public void WriteFile(string saveLocation)
        {
            int count = (AtaEntries != null) ? AtaEntries.Count() : 0;

            bytes = new List<byte>() { 35, 65, 84, 65, 254, 255 };
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(new byte[4]);

            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(AtaEntries[i].Name));
                bytes.Add(0);
            }


            if(count > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - 16), 12);
                bytes.AddRange(BitConverter.GetBytes(count));
                bytes.AddRange(BitConverter.GetBytes(16));
                bytes.AddRange(new byte[8]);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_44));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_48));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_52));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_56));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_60));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_64));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_68));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_72));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_76));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_80));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_84));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_88));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_92));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_96));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_100));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_104));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_108));
                    bytes.AddRange(BitConverter.GetBytes(AtaEntries[i].I_112));
                }
            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

    }
    
    public struct ATA_Entry
    {
        //entry size = 116
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("value")]
        public int I_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        public int I_112 { get; set; }
    }
}
