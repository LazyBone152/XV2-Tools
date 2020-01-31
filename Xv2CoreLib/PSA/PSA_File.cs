using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PSA
{
    [YAXSerializeAs("PSA")]
    public class PSA_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PSA_Entry")]
        public List<PSA_Entry> PsaEntries { get; set; }

        //Parsing
        private byte[] rawBytes { get; set; }

        public PSA_File(string path, bool _writeXml)
        {
            rawBytes = File.ReadAllBytes(path);
            PsaEntries = new List<PSA_Entry>();
            ParsePsa();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PSA_File));
                serializer.SerializeToFile(this, path + ".xml");
            }
        }

        public PSA_File()
        {

        }

        private void ParsePsa()
        {
            int offset = (8 * BitConverter.ToInt32(rawBytes, 8)) + 48;
            int count = BitConverter.ToInt32(rawBytes, 8);

            if(rawBytes.Count() != offset + (24 * count))
            {
                Console.WriteLine(String.Format("File size does not match the expected size. (expected = {0}, actual {1})", offset + (24 * count), rawBytes.Count()));
                Utils.WaitForInputThenQuit();
            }

            for(int i = 0; i < count; i++)
            {
                PsaEntries.Add(new PSA_Entry()
                {
                    F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                });
                offset += 24;
            }

        }

        public void WriteFile(string path)
        {
            List<byte> bytes = new List<byte>() { 35, 80, 83, 65, 254, 255, 16, 0 };
            int count = (PsaEntries != null) ? PsaEntries.Count() : 0;

            if (count > 0)
            {
                bytes.AddRange(BitConverter.GetBytes(count));
                bytes.AddRange(new byte[(8 * count) + 36]);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_00));
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(PsaEntries[i].F_20));
                }
            }
            else
            {
                bytes.AddRange(new byte[8]);
            }

            File.WriteAllBytes(path, bytes.ToArray());
        }


    }

    public class PSA_Entry
    {
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Basic Attacks")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Strike Super")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Ki Super")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
    }
}
