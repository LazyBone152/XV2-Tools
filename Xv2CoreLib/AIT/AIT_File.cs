using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.AIT
{
    public enum AIT_Version
    {
        XV1 = 40,
        XV2_BASE = 148,
        XV2_DLC5 = 196,
        XV2_DLC6 = 208,
        XV2_DLC16 = 232
    }

    [YAXSerializeAs("AIT")]
    public class AIT_File
    {
        //AIT_File values
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public AIT_Version AitType { get; set; }
        [YAXAttributeForClass]
        public uint I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AIT_Entry")]
        public List<AIT_Entry> AIT_Entries { get; set; }

        #region LoadSave
        public static AIT_File Parse(string path, bool writeXml)
        {
            AIT_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AIT_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static AIT_File Parse(byte[] bytes)
        {
            AIT_File ait = new AIT_File();
            if (ait.AIT_Entries == null) ait.AIT_Entries = new List<AIT_Entry>();
            int count = BitConverter.ToInt32(bytes, 8);
            ait.I_12 = BitConverter.ToUInt32(bytes, 12);
            ait.AitType = GetAitType(bytes);

            int offset = 16;

            for (int i = 0; i < count; i++)
            {
                AIT_Entry entry = new AIT_Entry()
                {
                    I_00 = BitConverter.ToInt32(bytes, offset + 0).ToString(),
                    I_04 = BitConverter.ToInt32(bytes, offset + 4),
                    I_08 = BitConverter.ToInt32(bytes, offset + 8),
                    I_12 = BitConverter.ToInt32(bytes, offset + 12),
                    I_16 = BitConverter.ToInt32(bytes, offset + 16),
                    I_20 = BitConverter.ToInt32(bytes, offset + 20),
                    I_24 = BitConverter.ToInt32(bytes, offset + 24),
                    I_28 = BitConverter.ToInt32(bytes, offset + 28),
                    I_32 = BitConverter.ToInt32(bytes, offset + 32),
                    I_36 = BitConverter.ToInt32(bytes, offset + 36),
                };

                if (ait.AitType >= AIT_Version.XV2_BASE)
                {
                    entry.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                    entry.I_44 = BitConverter.ToInt32(bytes, offset + 44);
                    entry.I_48 = BitConverter.ToInt32(bytes, offset + 48);
                    entry.I_52 = BitConverter.ToInt32(bytes, offset + 52);
                    entry.I_56 = BitConverter.ToInt32(bytes, offset + 56);
                    entry.I_60 = BitConverter.ToInt32(bytes, offset + 60);
                    entry.I_64 = BitConverter.ToInt32(bytes, offset + 64);
                    entry.I_68 = BitConverter.ToInt32(bytes, offset + 68);
                    entry.I_72 = BitConverter.ToInt32(bytes, offset + 72);
                    entry.I_76 = BitConverter.ToInt32(bytes, offset + 76);
                    entry.I_80 = BitConverter.ToInt32(bytes, offset + 80);
                    entry.I_84 = BitConverter.ToInt32(bytes, offset + 84);
                    entry.I_88 = BitConverter.ToInt32(bytes, offset + 88);
                    entry.I_92 = BitConverter.ToInt32(bytes, offset + 92);
                    entry.I_96 = BitConverter.ToInt32(bytes, offset + 96);
                    entry.I_100 = BitConverter.ToInt32(bytes, offset + 100);
                    entry.I_104 = BitConverter.ToInt32(bytes, offset + 104);
                    entry.I_108 = BitConverter.ToInt32(bytes, offset + 108);
                    entry.I_112 = BitConverter.ToInt32(bytes, offset + 112);
                    entry.I_116 = BitConverter.ToInt32(bytes, offset + 116);
                    entry.I_120 = BitConverter.ToInt32(bytes, offset + 120);
                    entry.I_124 = BitConverter.ToInt32(bytes, offset + 124);
                    entry.I_128 = BitConverter.ToInt32(bytes, offset + 128);
                    entry.I_132 = BitConverter.ToInt32(bytes, offset + 132);
                    entry.I_136 = BitConverter.ToInt32(bytes, offset + 136);
                    entry.I_140 = BitConverter.ToInt32(bytes, offset + 140);
                    entry.I_144 = BitConverter.ToInt32(bytes, offset + 144);
                }

                if (ait.AitType >= AIT_Version.XV2_DLC5)
                {
                    entry.I_148 = BitConverter.ToInt32(bytes, offset + 148);
                    entry.I_152 = BitConverter.ToInt32(bytes, offset + 152);
                    entry.I_156 = BitConverter.ToInt32(bytes, offset + 156);
                    entry.I_160 = BitConverter.ToInt32(bytes, offset + 160);
                    entry.I_164 = BitConverter.ToInt32(bytes, offset + 164);
                    entry.I_168 = BitConverter.ToInt32(bytes, offset + 168);
                    entry.I_172 = BitConverter.ToInt32(bytes, offset + 172);
                    entry.I_176 = BitConverter.ToInt32(bytes, offset + 176);
                    entry.I_180 = BitConverter.ToInt32(bytes, offset + 180);
                    entry.I_184 = BitConverter.ToInt32(bytes, offset + 184);
                    entry.I_188 = BitConverter.ToInt32(bytes, offset + 188);
                    entry.I_192 = BitConverter.ToInt32(bytes, offset + 192);
                }

                if (ait.AitType >= AIT_Version.XV2_DLC6)
                {
                    entry.I_196 = BitConverter.ToInt32(bytes, offset + 196);
                    entry.I_200 = BitConverter.ToInt32(bytes, offset + 200);
                    entry.I_204 = BitConverter.ToInt32(bytes, offset + 204);
                }

                if (ait.AitType >= AIT_Version.XV2_DLC16)
                {
                    entry.I_208 = BitConverter.ToInt32(bytes, offset + 208);
                    entry.I_212 = BitConverter.ToInt32(bytes, offset + 212);
                    entry.I_216 = BitConverter.ToInt32(bytes, offset + 216);
                    entry.I_220 = BitConverter.ToInt32(bytes, offset + 220);
                    entry.I_224 = BitConverter.ToInt32(bytes, offset + 224);
                    entry.I_228 = BitConverter.ToInt32(bytes, offset + 228);
                }

                ait.AIT_Entries.Add(entry);
                offset += (int)ait.AitType;
            }

            return ait;
        }

        private static AIT_Version GetAitType(byte[] bytes)
        {
            int count = BitConverter.ToInt32(bytes, 8);
            int entrySize = (bytes.Length - 16) / count;

            switch (entrySize)
            {
                case 40:
                    return AIT_Version.XV1;
                case 148:
                    return AIT_Version.XV2_BASE;
                case 196:
                    return AIT_Version.XV2_DLC5;
                case 208:
                    return AIT_Version.XV2_DLC6;
                case 232:
                    return AIT_Version.XV2_DLC16;
                default:
                    throw new Exception(String.Format("Unknown AitType. (Entry size = {0}).\nCannot parse this type of ait!", entrySize));
            }
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .ait file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(AIT_File), YAXSerializationOptions.DontSerializeNullObjects);
            var aitFile = (AIT_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, aitFile.Write());
        }

        /// <summary>
        /// Save the AIT_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (AIT_Entries == null) AIT_Entries = new List<AIT_Entry>();

            int count = (AIT_Entries != null) ? AIT_Entries.Count : 0;

            List<byte> bytes = new List<byte>() { 35, 65, 73, 84, 254, 255, 16, 0 };
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(I_12));

            //Validate version
            if (AitType != AIT_Version.XV1 && AitType != AIT_Version.XV2_BASE && AitType != AIT_Version.XV2_DLC5 && AitType != AIT_Version.XV2_DLC6 && AitType != AIT_Version.XV2_DLC16)
                throw new Exception(String.Format("Unknown AitType encountered = {0}. Cannot continue.", AitType));

            //Write entries
            if (AIT_Entries != null)
            {
                foreach (var e in AIT_Entries)
                {
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(e.I_00)));
                    bytes.AddRange(BitConverter.GetBytes(e.I_04));
                    bytes.AddRange(BitConverter.GetBytes(e.I_08));
                    bytes.AddRange(BitConverter.GetBytes(e.I_12));
                    bytes.AddRange(BitConverter.GetBytes(e.I_16));
                    bytes.AddRange(BitConverter.GetBytes(e.I_20));
                    bytes.AddRange(BitConverter.GetBytes(e.I_24));
                    bytes.AddRange(BitConverter.GetBytes(e.I_28));
                    bytes.AddRange(BitConverter.GetBytes(e.I_32));
                    bytes.AddRange(BitConverter.GetBytes(e.I_36));

                    if(AitType >= AIT_Version.XV2_BASE)
                    {
                        bytes.AddRange(BitConverter.GetBytes(e.I_40));
                        bytes.AddRange(BitConverter.GetBytes(e.I_44));
                        bytes.AddRange(BitConverter.GetBytes(e.I_48));
                        bytes.AddRange(BitConverter.GetBytes(e.I_52));
                        bytes.AddRange(BitConverter.GetBytes(e.I_56));
                        bytes.AddRange(BitConverter.GetBytes(e.I_60));
                        bytes.AddRange(BitConverter.GetBytes(e.I_64));
                        bytes.AddRange(BitConverter.GetBytes(e.I_68));
                        bytes.AddRange(BitConverter.GetBytes(e.I_72));
                        bytes.AddRange(BitConverter.GetBytes(e.I_76));
                        bytes.AddRange(BitConverter.GetBytes(e.I_80));
                        bytes.AddRange(BitConverter.GetBytes(e.I_84));
                        bytes.AddRange(BitConverter.GetBytes(e.I_88));
                        bytes.AddRange(BitConverter.GetBytes(e.I_92));
                        bytes.AddRange(BitConverter.GetBytes(e.I_96));
                        bytes.AddRange(BitConverter.GetBytes(e.I_100));
                        bytes.AddRange(BitConverter.GetBytes(e.I_104));
                        bytes.AddRange(BitConverter.GetBytes(e.I_108));
                        bytes.AddRange(BitConverter.GetBytes(e.I_112));
                        bytes.AddRange(BitConverter.GetBytes(e.I_116));
                        bytes.AddRange(BitConverter.GetBytes(e.I_120));
                        bytes.AddRange(BitConverter.GetBytes(e.I_124));
                        bytes.AddRange(BitConverter.GetBytes(e.I_128));
                        bytes.AddRange(BitConverter.GetBytes(e.I_132));
                        bytes.AddRange(BitConverter.GetBytes(e.I_136));
                        bytes.AddRange(BitConverter.GetBytes(e.I_140));
                        bytes.AddRange(BitConverter.GetBytes(e.I_144));
                    }

                    if(AitType >= AIT_Version.XV2_DLC5)
                    {
                        bytes.AddRange(BitConverter.GetBytes(e.I_148));
                        bytes.AddRange(BitConverter.GetBytes(e.I_152));
                        bytes.AddRange(BitConverter.GetBytes(e.I_156));
                        bytes.AddRange(BitConverter.GetBytes(e.I_160));
                        bytes.AddRange(BitConverter.GetBytes(e.I_164));
                        bytes.AddRange(BitConverter.GetBytes(e.I_168));
                        bytes.AddRange(BitConverter.GetBytes(e.I_172));
                        bytes.AddRange(BitConverter.GetBytes(e.I_176));
                        bytes.AddRange(BitConverter.GetBytes(e.I_180));
                        bytes.AddRange(BitConverter.GetBytes(e.I_184));
                        bytes.AddRange(BitConverter.GetBytes(e.I_188));
                        bytes.AddRange(BitConverter.GetBytes(e.I_192));
                    }

                    if(AitType >= AIT_Version.XV2_DLC6)
                    {
                        bytes.AddRange(BitConverter.GetBytes(e.I_196));
                        bytes.AddRange(BitConverter.GetBytes(e.I_200));
                        bytes.AddRange(BitConverter.GetBytes(e.I_204));
                    }

                    if (AitType >= AIT_Version.XV2_DLC16)
                    {
                        bytes.AddRange(BitConverter.GetBytes(e.I_208));
                        bytes.AddRange(BitConverter.GetBytes(e.I_212));
                        bytes.AddRange(BitConverter.GetBytes(e.I_216));
                        bytes.AddRange(BitConverter.GetBytes(e.I_220));
                        bytes.AddRange(BitConverter.GetBytes(e.I_224));
                        bytes.AddRange(BitConverter.GetBytes(e.I_228));
                    }
                }
            }

            return bytes.ToArray();
        }
        public byte[] SaveToBytes()
        {
            return Write();
        }
        #endregion

    }

    public class AIT_Entry : IInstallable
    {
        #region NonSerialized

        //interface
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return $"{I_00}";
            }
            set
            {
                I_00 = value.ToString();
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string I_00 { get; set; } //Int32
        [YAXAttributeFor("BAI_Entry")]
        [YAXSerializeAs("ID")]
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
        [YAXAttributeFor("I_116")]
        [YAXSerializeAs("value")]
        public int I_116 { get; set; }
        [YAXAttributeFor("I_120")]
        [YAXSerializeAs("value")]
        public int I_120 { get; set; }
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        public int I_124 { get; set; }
        [YAXAttributeFor("I_128")]
        [YAXSerializeAs("value")]
        public int I_128 { get; set; }
        [YAXAttributeFor("I_132")]
        [YAXSerializeAs("value")]
        public int I_132 { get; set; }
        [YAXAttributeFor("I_136")]
        [YAXSerializeAs("value")]
        public int I_136 { get; set; }
        [YAXAttributeFor("I_140")]
        [YAXSerializeAs("value")]
        public int I_140 { get; set; }
        [YAXAttributeFor("I_144")]
        [YAXSerializeAs("value")]
        public int I_144 { get; set; }
        [YAXAttributeFor("I_148")]
        [YAXSerializeAs("value")]
        public int I_148 { get; set; }
        [YAXAttributeFor("I_152")]
        [YAXSerializeAs("value")]
        public int I_152 { get; set; }
        [YAXAttributeFor("I_156")]
        [YAXSerializeAs("value")]
        public int I_156 { get; set; }
        [YAXAttributeFor("I_160")]
        [YAXSerializeAs("value")]
        public int I_160 { get; set; }
        [YAXAttributeFor("I_164")]
        [YAXSerializeAs("value")]
        public int I_164 { get; set; }
        [YAXAttributeFor("I_168")]
        [YAXSerializeAs("value")]
        public int I_168 { get; set; }
        [YAXAttributeFor("I_172")]
        [YAXSerializeAs("value")]
        public int I_172 { get; set; }
        [YAXAttributeFor("I_176")]
        [YAXSerializeAs("value")]
        public int I_176 { get; set; }
        [YAXAttributeFor("I_180")]
        [YAXSerializeAs("value")]
        public int I_180 { get; set; }
        [YAXAttributeFor("I_184")]
        [YAXSerializeAs("value")]
        public int I_184 { get; set; }
        [YAXAttributeFor("I_188")]
        [YAXSerializeAs("value")]
        public int I_188 { get; set; }
        [YAXAttributeFor("I_192")]
        [YAXSerializeAs("value")]
        public int I_192 { get; set; }
        [YAXAttributeFor("I_196")]
        [YAXSerializeAs("value")]
        public int I_196 { get; set; }
        [YAXAttributeFor("I_200")]
        [YAXSerializeAs("value")]
        public int I_200 { get; set; }
        [YAXAttributeFor("I_204")]
        [YAXSerializeAs("value")]
        public int I_204 { get; set; }
        [YAXAttributeFor("I_208")]
        [YAXSerializeAs("value")]
        public int I_208 { get; set; }
        [YAXAttributeFor("I_212")]
        [YAXSerializeAs("value")]
        public int I_212 { get; set; }
        [YAXAttributeFor("I_216")]
        [YAXSerializeAs("value")]
        public int I_216 { get; set; }
        [YAXAttributeFor("I_220")]
        [YAXSerializeAs("value")]
        public int I_220 { get; set; }
        [YAXAttributeFor("I_224")]
        [YAXSerializeAs("value")]
        public int I_224 { get; set; }
        [YAXAttributeFor("I_228")]
        [YAXSerializeAs("value")]
        public int I_228 { get; set; }
    }
}
