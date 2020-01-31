using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.AIT
{
    [YAXSerializeAs("AIT")]
    public class AIT_File
    {
        //AIT_File values
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string AitType { get; set; }
        [YAXAttributeForClass]
        public uint I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AIT_Entry")]
        public List<AIT_Entry> AIT_Entries { get; set; }

        //Parsing values
        string SaveLocation { get; set; }
        private byte[] rawBytes;

        //Desierlization values
        private List<byte> bytes { get; set; }
        

        public AIT_File(string path, bool _writeXml)
        {
            SaveLocation = path;
            rawBytes = File.ReadAllBytes(path);
            
            AIT_Entries = new List<AIT_Entry>();
            ParseAit();

            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AIT_File));
                serializer.SerializeToFile(this, SaveLocation + ".xml");
            }
        }
        
        public AIT_File()
        {

        }

        //Parsing
        private void ParseAit()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            I_12 = BitConverter.ToUInt32(rawBytes, 12);
            AitType = GetAitType();

            switch (AitType)
            {
                case "XV1":
                    ParseXv1Base(count);
                    break;
                case "XV2_BASE":
                    ParseXv2Base(count);
                    break;
                case "XV2_DLC5":
                    ParseXv2Dlc5(count);
                    break;
                case "XV2_DLC6":
                    ParseXv2Dlc6(count);
                    break;
            }

        }

        private void ParseXv1Base(int count)
        {
            int offset = 16;

            for (int i = 0; i < count; i++)
            {
                AIT_Entries.Add(new AIT_Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36)
                });
                offset += 40;
            }

        }

        private void ParseXv2Base(int count)
        {
            int offset = 16;

            for (int i = 0; i < count; i++)
            {
                AIT_Entries.Add(new AIT_Entry()
                {
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
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40).ToString(),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44).ToString(),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48).ToString(),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52).ToString(),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56).ToString(),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60).ToString(),
                    I_64 = BitConverter.ToInt32(rawBytes, offset + 64).ToString(),
                    I_68 = BitConverter.ToInt32(rawBytes, offset + 68).ToString(),
                    I_72 = BitConverter.ToInt32(rawBytes, offset + 72).ToString(),
                    I_76 = BitConverter.ToInt32(rawBytes, offset + 76).ToString(),
                    I_80 = BitConverter.ToInt32(rawBytes, offset + 80).ToString(),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84).ToString(),
                    I_88 = BitConverter.ToInt32(rawBytes, offset + 88).ToString(),
                    I_92 = BitConverter.ToInt32(rawBytes, offset + 92).ToString(),
                    I_96 = BitConverter.ToInt32(rawBytes, offset + 96).ToString(),
                    I_100 = BitConverter.ToInt32(rawBytes, offset + 100).ToString(),
                    I_104 = BitConverter.ToInt32(rawBytes, offset + 104).ToString(),
                    I_108 = BitConverter.ToInt32(rawBytes, offset + 108).ToString(),
                    I_112 = BitConverter.ToInt32(rawBytes, offset + 112).ToString(),
                    I_116 = BitConverter.ToInt32(rawBytes, offset + 116).ToString(),
                    I_120 = BitConverter.ToInt32(rawBytes, offset + 120).ToString(),
                    I_124 = BitConverter.ToInt32(rawBytes, offset + 124).ToString(),
                    I_128 = BitConverter.ToInt32(rawBytes, offset + 128).ToString(),
                    I_132 = BitConverter.ToInt32(rawBytes, offset + 132).ToString(),
                    I_136 = BitConverter.ToInt32(rawBytes, offset + 136).ToString(),
                    I_140 = BitConverter.ToInt32(rawBytes, offset + 140).ToString(),
                    I_144 = BitConverter.ToInt32(rawBytes, offset + 144).ToString()
                });
                offset += 148;
            }

        }
        
        private void ParseXv2Dlc5(int count)
        {
            int offset = 16;

            for (int i = 0; i < count; i++)
            {
                AIT_Entries.Add(new AIT_Entry()
                {
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
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40).ToString(),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44).ToString(),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48).ToString(),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52).ToString(),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56).ToString(),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60).ToString(),
                    I_64 = BitConverter.ToInt32(rawBytes, offset + 64).ToString(),
                    I_68 = BitConverter.ToInt32(rawBytes, offset + 68).ToString(),
                    I_72 = BitConverter.ToInt32(rawBytes, offset + 72).ToString(),
                    I_76 = BitConverter.ToInt32(rawBytes, offset + 76).ToString(),
                    I_80 = BitConverter.ToInt32(rawBytes, offset + 80).ToString(),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84).ToString(),
                    I_88 = BitConverter.ToInt32(rawBytes, offset + 88).ToString(),
                    I_92 = BitConverter.ToInt32(rawBytes, offset + 92).ToString(),
                    I_96 = BitConverter.ToInt32(rawBytes, offset + 96).ToString(),
                    I_100 = BitConverter.ToInt32(rawBytes, offset + 100).ToString(),
                    I_104 = BitConverter.ToInt32(rawBytes, offset + 104).ToString(),
                    I_108 = BitConverter.ToInt32(rawBytes, offset + 108).ToString(),
                    I_112 = BitConverter.ToInt32(rawBytes, offset + 112).ToString(),
                    I_116 = BitConverter.ToInt32(rawBytes, offset + 116).ToString(),
                    I_120 = BitConverter.ToInt32(rawBytes, offset + 120).ToString(),
                    I_124 = BitConverter.ToInt32(rawBytes, offset + 124).ToString(),
                    I_128 = BitConverter.ToInt32(rawBytes, offset + 128).ToString(),
                    I_132 = BitConverter.ToInt32(rawBytes, offset + 132).ToString(),
                    I_136 = BitConverter.ToInt32(rawBytes, offset + 136).ToString(),
                    I_140 = BitConverter.ToInt32(rawBytes, offset + 140).ToString(),
                    I_144 = BitConverter.ToInt32(rawBytes, offset + 144).ToString(),
                    I_148 = BitConverter.ToInt32(rawBytes, offset + 148).ToString(),
                    I_152 = BitConverter.ToInt32(rawBytes, offset + 152).ToString(),
                    I_156 = BitConverter.ToInt32(rawBytes, offset + 156).ToString(),
                    I_160 = BitConverter.ToInt32(rawBytes, offset + 160).ToString(),
                    I_164 = BitConverter.ToInt32(rawBytes, offset + 164).ToString(),
                    I_168 = BitConverter.ToInt32(rawBytes, offset + 168).ToString(),
                    I_172 = BitConverter.ToInt32(rawBytes, offset + 172).ToString(),
                    I_176 = BitConverter.ToInt32(rawBytes, offset + 176).ToString(),
                    I_180 = BitConverter.ToInt32(rawBytes, offset + 180).ToString(),
                    I_184 = BitConverter.ToInt32(rawBytes, offset + 184).ToString(),
                    I_188 = BitConverter.ToInt32(rawBytes, offset + 188).ToString(),
                    I_192 = BitConverter.ToInt32(rawBytes, offset + 192).ToString()
                });
                offset += 196;
            }

        }

        private void ParseXv2Dlc6(int count)
        {
            int offset = 16;

            for (int i = 0; i < count; i++)
            {
                AIT_Entries.Add(new AIT_Entry()
                {
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
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40).ToString(),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44).ToString(),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48).ToString(),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52).ToString(),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56).ToString(),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60).ToString(),
                    I_64 = BitConverter.ToInt32(rawBytes, offset + 64).ToString(),
                    I_68 = BitConverter.ToInt32(rawBytes, offset + 68).ToString(),
                    I_72 = BitConverter.ToInt32(rawBytes, offset + 72).ToString(),
                    I_76 = BitConverter.ToInt32(rawBytes, offset + 76).ToString(),
                    I_80 = BitConverter.ToInt32(rawBytes, offset + 80).ToString(),
                    I_84 = BitConverter.ToInt32(rawBytes, offset + 84).ToString(),
                    I_88 = BitConverter.ToInt32(rawBytes, offset + 88).ToString(),
                    I_92 = BitConverter.ToInt32(rawBytes, offset + 92).ToString(),
                    I_96 = BitConverter.ToInt32(rawBytes, offset + 96).ToString(),
                    I_100 = BitConverter.ToInt32(rawBytes, offset + 100).ToString(),
                    I_104 = BitConverter.ToInt32(rawBytes, offset + 104).ToString(),
                    I_108 = BitConverter.ToInt32(rawBytes, offset + 108).ToString(),
                    I_112 = BitConverter.ToInt32(rawBytes, offset + 112).ToString(),
                    I_116 = BitConverter.ToInt32(rawBytes, offset + 116).ToString(),
                    I_120 = BitConverter.ToInt32(rawBytes, offset + 120).ToString(),
                    I_124 = BitConverter.ToInt32(rawBytes, offset + 124).ToString(),
                    I_128 = BitConverter.ToInt32(rawBytes, offset + 128).ToString(),
                    I_132 = BitConverter.ToInt32(rawBytes, offset + 132).ToString(),
                    I_136 = BitConverter.ToInt32(rawBytes, offset + 136).ToString(),
                    I_140 = BitConverter.ToInt32(rawBytes, offset + 140).ToString(),
                    I_144 = BitConverter.ToInt32(rawBytes, offset + 144).ToString(),
                    I_148 = BitConverter.ToInt32(rawBytes, offset + 148).ToString(),
                    I_152 = BitConverter.ToInt32(rawBytes, offset + 152).ToString(),
                    I_156 = BitConverter.ToInt32(rawBytes, offset + 156).ToString(),
                    I_160 = BitConverter.ToInt32(rawBytes, offset + 160).ToString(),
                    I_164 = BitConverter.ToInt32(rawBytes, offset + 164).ToString(),
                    I_168 = BitConverter.ToInt32(rawBytes, offset + 168).ToString(),
                    I_172 = BitConverter.ToInt32(rawBytes, offset + 172).ToString(),
                    I_176 = BitConverter.ToInt32(rawBytes, offset + 176).ToString(),
                    I_180 = BitConverter.ToInt32(rawBytes, offset + 180).ToString(),
                    I_184 = BitConverter.ToInt32(rawBytes, offset + 184).ToString(),
                    I_188 = BitConverter.ToInt32(rawBytes, offset + 188).ToString(),
                    I_192 = BitConverter.ToInt32(rawBytes, offset + 192).ToString(),
                    I_196 = BitConverter.ToInt32(rawBytes, offset + 196).ToString(),
                    I_200 = BitConverter.ToInt32(rawBytes, offset + 200).ToString(),
                    I_204 = BitConverter.ToInt32(rawBytes, offset + 204).ToString()
                });
                offset += 208;
            }
            
        }

        private string GetAitType()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int entrySize = (rawBytes.Count() - 16) / count;

            switch (entrySize)
            {
                case 40:
                    return "XV1";
                case 148:
                    return "XV2_BASE";
                case 196:
                    return "XV2_DLC5";
                case 208:
                    return "XV2_DLC6";
                default:
                    Console.WriteLine(String.Format("Unknown AitType. (Entry size = {0}).\nCannot parse this type of ait!", entrySize));
                    Utils.WaitForInputThenQuit();
                    return null;
            }
        }

        //Deserialization
        public void WriteFile(string saveLocation)
        {
            int count = (AIT_Entries != null) ? AIT_Entries.Count() : 0;

            bytes = new List<byte>() { 35, 65, 73, 84, 254, 255, 16, 0 };
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(I_12));

            switch (AitType)
            {
                case "XV1":
                    WriteXv1Base(AIT_Entries);
                    break;
                case "XV2_BASE":
                    WriteXv2Base(AIT_Entries);
                    break;
                case "XV2_DLC5":
                    WriteXv2Dlc5(AIT_Entries);
                    break;
                case "XV2_DLC6":
                    WriteXv2Dlc6(AIT_Entries);
                    break;
                default:
                    Console.WriteLine(String.Format("Unknown AitType encountered = {0}. Cannot continue.", AitType));
                    Utils.WaitForInputThenQuit();
                    break;
            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());

        }

        private void WriteXv1Base(List<AIT_Entry> entries)
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_00));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_04));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_08));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_12));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_16));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_20));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_24));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_28));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_32));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_36));
                }
            }
        }

        private void WriteXv2Base(List<AIT_Entry> entries)
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_00));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_04));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_08));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_12));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_16));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_20));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_24));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_28));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_32));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_36));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_40)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_44)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_48)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_52)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_56)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_60)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_64)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_68)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_72)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_76)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_80)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_84)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_88)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_92)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_96)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_100)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_104)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_108)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_112)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_116)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_120)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_124)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_128)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_132)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_136)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_140)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_144)));
                }
            }
        }

        private void WriteXv2Dlc6(List<AIT_Entry> entries)
        {
            if(entries != null)
            {
                foreach(var e in entries)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_00));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_04));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_08));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_12));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_16));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_20));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_24));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_28));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_32));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_36));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_40)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_44)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_48)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_52)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_56)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_60)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_64)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_68)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_72)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_76)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_80)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_84)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_88)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_92)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_96)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_100)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_104)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_108)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_112)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_116)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_120)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_124)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_128)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_132)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_136)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_140)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_144)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_148)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_152)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_156)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_160)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_164)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_168)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_172)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_176)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_180)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_184)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_188)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_192)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_196)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_200)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_204)));
                }
            }
        }

        private void WriteXv2Dlc5(List<AIT_Entry> entries)
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_00));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_04));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_08));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_12));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_16));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_20));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_24));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_28));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_32));
                    bytes.AddRange(BitConverter.GetBytes((int)e.I_36));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_40)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_44)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_48)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_52)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_56)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_60)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_64)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_68)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_72)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_76)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_80)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_84)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_88)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_92)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_96)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_100)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_104)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_108)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_112)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_116)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_120)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_124)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_128)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_132)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_136)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_140)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_144)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_148)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_152)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_156)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_160)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_164)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_168)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_172)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_176)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_180)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_184)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_188)));
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_192)));
                }
            }
        }





    }

    public class AIT_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [YAXDontSerializeIfNull]
        public int? I_00 { get; set; }
        [YAXAttributeFor("BAI_Entry")]
        [YAXSerializeAs("ID")]
        [YAXDontSerializeIfNull]
        public int? I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public int? I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_92 { get; set; }
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_112 { get; set; }
        [YAXAttributeFor("I_116")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_116 { get; set; }
        [YAXAttributeFor("I_120")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_120 { get; set; }
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_124 { get; set; }
        [YAXAttributeFor("I_128")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_128 { get; set; }
        [YAXAttributeFor("I_132")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_132 { get; set; }
        [YAXAttributeFor("I_136")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_136 { get; set; }
        [YAXAttributeFor("I_140")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_140 { get; set; }
        [YAXAttributeFor("I_144")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_144 { get; set; }
        [YAXAttributeFor("I_148")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_148 { get; set; }
        [YAXAttributeFor("I_152")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_152 { get; set; }
        [YAXAttributeFor("I_156")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_156 { get; set; }
        [YAXAttributeFor("I_160")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_160 { get; set; }
        [YAXAttributeFor("I_164")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_164 { get; set; }
        [YAXAttributeFor("I_168")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_168 { get; set; }
        [YAXAttributeFor("I_172")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_172 { get; set; }
        [YAXAttributeFor("I_176")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_176 { get; set; }
        [YAXAttributeFor("I_180")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_180 { get; set; }
        [YAXAttributeFor("I_184")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_184 { get; set; }
        [YAXAttributeFor("I_188")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_188 { get; set; }
        [YAXAttributeFor("I_192")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_192 { get; set; }
        [YAXAttributeFor("I_196")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_196 { get; set; }
        [YAXAttributeFor("I_200")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_200 { get; set; }
        [YAXAttributeFor("I_204")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string I_204 { get; set; }
    }
}
