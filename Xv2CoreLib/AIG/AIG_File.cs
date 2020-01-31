using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.AIG
{
    [YAXSerializeAs("AIG")]
    public class AIG_File
    {
        private List<byte> bytes;

        [YAXAttributeForClass]
        public ushort I_06 { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AIG_Entry")]
        public List<AIG_Entry> AigEntries { get; set; }

        //Parsing values
        byte[] rawBytes { get; set; }

        public AIG_File (string path, bool _writeXml)
        {
            rawBytes = File.ReadAllBytes(path);
            AigEntries = new List<AIG_Entry>();
            ParseAig();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AIG_File));
                serializer.SerializeToFile(this, path + ".xml");
            }
        }

        public AIG_File()
        {

        }

        private void ParseAig()
        {
            //Entry offset+count
            int offset = BitConverter.ToInt32(rawBytes, 12) + 16;
            int count = BitConverter.ToInt32(rawBytes, offset);
            offset += BitConverter.ToInt32(rawBytes, offset + 4);

            I_06 = BitConverter.ToUInt16(rawBytes, 6);
            I_08 = BitConverter.ToInt32(rawBytes, 8);
            
            //Entries
            int strOffset = 16;
            List<byte> _bytes = rawBytes.ToList();
            
            for(int i = 0; i < count; i++)
            {
                AigEntries.Add(new AIG_Entry()
                {
                    Name = Utils.GetString(_bytes, strOffset),
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToUInt16(rawBytes, offset + 28),
                    I_30 = BitConverter.ToUInt16(rawBytes, offset + 30),
                    I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                    I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                });
                strOffset += AigEntries[i].Name.Length + 1;
                offset += 44;
            }
        }

        //Deserialization
        public void WriteFile(string saveLocation)
        {
            int count = (AigEntries != null) ? AigEntries.Count() : 0;

            bytes = new List<byte>() { 35, 65, 73, 71, 254, 255 };
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(new byte[4]);

            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(AigEntries[i].Name));
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
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_30));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_34));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes(AigEntries[i].I_40));
                }
            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

    }
    
    public struct AIG_Entry
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public ushort I_28 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }
        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
    }
}
