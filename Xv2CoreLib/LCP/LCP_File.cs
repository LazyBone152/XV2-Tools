using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.LCP
{
    [YAXSerializeAs("LCP")]
    public class LCP_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "LCP_Entry")]
        public List<LCP_Entry> LcpEntries { get; set; }


        //Parsing
        private byte[] rawBytes { get; set; }

        public LCP_File(string path, bool _writeXml)
        {
            rawBytes = File.ReadAllBytes(path);
            LcpEntries = new List<LCP_Entry>();
            ParseLcp();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(LCP_File));
                serializer.SerializeToFile(this, path + ".xml");
            }
        }

        public LCP_File()
        {

        }

        private void ParseLcp()
        {
            int offset = (8 * BitConverter.ToInt32(rawBytes, 8)) + 24;
            int count = BitConverter.ToInt32(rawBytes, 8);

            if (rawBytes.Count() != offset + (128 * count))
            {
                Console.WriteLine(String.Format("File size does not match the expected size. (expected = {0}, actual {1})", offset + (128 * count), rawBytes.Count()));
                Utils.WaitForInputThenQuit();
            }


            for (int i = 0; i < count; i++)
            {
                LcpEntries.Add(new LCP_Entry()
                {
                    Index = i + 1,
                    F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                    F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                    F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                    F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                    F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                    F_76 = BitConverter.ToSingle(rawBytes, offset + 76),
                    F_80 = BitConverter.ToSingle(rawBytes, offset + 80),
                    F_84 = BitConverter.ToSingle(rawBytes, offset + 84),
                    F_88 = BitConverter.ToSingle(rawBytes, offset + 88),
                    F_92 = BitConverter.ToSingle(rawBytes, offset + 92),
                    F_96 = BitConverter.ToSingle(rawBytes, offset + 96),
                    F_100 = BitConverter.ToSingle(rawBytes, offset + 100),
                    F_104 = BitConverter.ToSingle(rawBytes, offset + 104),
                    F_108 = BitConverter.ToSingle(rawBytes, offset + 108),
                    F_112 = BitConverter.ToSingle(rawBytes, offset + 112),
                    F_116 = BitConverter.ToSingle(rawBytes, offset + 116),
                    F_120 = BitConverter.ToSingle(rawBytes, offset + 120),
                    F_124 = BitConverter.ToSingle(rawBytes, offset + 124)
                });

                offset += 128;
            }

        }

        public void WriteFile(string path)
        {
            ValidateLevels();
            List<byte> bytes = new List<byte>() { 35, 76, 67, 80, 254, 255, 16, 0 };
            int count = (LcpEntries != null) ? LcpEntries.Count() : 0;

            if(count > 0)
            {
                bytes.AddRange(BitConverter.GetBytes(count));
                bytes.AddRange(new byte[(8 * count) + 12]);

                for(int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_00));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_04));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_08));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_44));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_48));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_52));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_56));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_60));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_64));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_68));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_72));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_76));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_80));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_84));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_88));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_92));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_96));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_100));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_104));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_108));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_112));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_116));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_120));
                    bytes.AddRange(BitConverter.GetBytes(LcpEntries[i].F_124));
                }
            }
            else
            {
                bytes.AddRange(new byte[8]);
            }

            File.WriteAllBytes(path, bytes.ToArray());
        }

        private void ValidateLevels()
        {
            int level = 1;
            int count = (LcpEntries != null) ? LcpEntries.Count() : 0;

            for(int i = 0; i < count; i++)
            {
                if(LcpEntries[i].Index != level)
                {
                    Console.WriteLine(String.Format("Level=\"{0}\" is invalid ({1} was expected). Levels must be consecutive, with the first entry starting at 1.", LcpEntries[i].Index, level));
                    Utils.WaitForInputThenQuit();
                }
                level++;
            }
        }

    }

    public class LCP_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Level")]
        public int Index { get; set; }
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Ki_Recharge")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Stamina_Recharge")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Basic Melee")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Ki Blast")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0#########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Strike Super")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0#########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Ki Super")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0#########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Basic Melee")]
        [YAXSerializeAs("Modifer")]
        [YAXFormat("0.0#########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Ki Blast")]
        [YAXSerializeAs("Modifer")]
        [YAXFormat("0.0#########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Strike Super")]
        [YAXSerializeAs("Modifer")]
        [YAXFormat("0.0#########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Ki Super")]
        [YAXSerializeAs("Modifer")]
        [YAXFormat("0.0#########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Ground Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Air Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Boost Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("Dash Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("F_76")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("F_80")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("F_84")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_84 { get; set; }
        [YAXAttributeFor("F_88")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("F_92")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_92 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("F_100")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("F_108")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("F_112")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("F_116")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("F_120")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("F_124")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_124 { get; set; }
    }
}
