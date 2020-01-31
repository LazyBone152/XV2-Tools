using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.PSC
{
    [YAXSerializeAs("PSC")]
    public class PSC_File
    {
        public const ushort PSC_HEADER_SIZE = 20;
        public const uint PSC_SIGNATURE = 0x43535023;

        [YAXDontSerialize]
        public int NumPscEntries
        {
            get
            {
                if (Configurations == null) return 0;
                if (Configurations.Count > 0) return (Configurations[0].PscEntries != null) ? Configurations[0].PscEntries.Count : 0;
                return 0;
            }
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Configuration")]
        public List<PSC_Configuration> Configurations { get; set; } = new List<PSC_Configuration>();

        public static PSC_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);

            PSC_File file = Load(rawBytes);

            //Write Xml
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(PSC_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static PSC_File Load(byte[] bytes)
        {
            //First, validate that the psc file is from version 1.13 or greater
            if(BitConverter.ToInt16(bytes, 6) != PSC_HEADER_SIZE)
            {
                throw new InvalidDataException("PSC files from game versions prior to 1.13 are not supported.");
            }

            PSC_File pscFile = new PSC_File();

            int numEntries = BitConverter.ToInt32(bytes, 8);
            int numConfigurations = BitConverter.ToInt32(bytes, 16);
            int currentSpecPos = PSC_HEADER_SIZE + ((12 * numEntries) * numConfigurations);

            for(int i = 0; i < numConfigurations; i++)
            {
                PSC_Configuration config = new PSC_Configuration();
                config.Index = i;

                int pscEntryOffset = PSC_HEADER_SIZE + ((12 * numEntries) * i);

                for(int a = 0; a < numEntries; a++)
                {
                    PSC_Entry pscEntry = new PSC_Entry();
                    pscEntry.Index = BitConverter.ToInt32(bytes, pscEntryOffset + 0).ToString();
                    int numSpec = BitConverter.ToInt32(bytes, pscEntryOffset + 4);

                    for(int s = 0; s < numSpec; s++)
                    {
                        pscEntry.PscSpecEntries.Add(PSC_SpecEntry.Read(bytes, currentSpecPos));
                        currentSpecPos += 196;
                    }

                    config.PscEntries.Add(pscEntry);
                    pscEntryOffset += 12;
                }

                pscFile.Configurations.Add(config);
            }

            return pscFile;
        }
        
        public static void Deserialize(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(PSC_File), YAXSerializationOptions.DontSerializeNullObjects);
            Write((PSC_File)serializer.DeserializeFromFile(xmlPath), path);
        }

        public static void Write(PSC_File file, string path)
        {
            byte[] bytes = file.SaveToBytes();

            //Saving
            File.WriteAllBytes(path, bytes.ToArray());
        }


        public byte[] SaveToBytes()
        {
            List<byte> bytes = new List<byte>();

            //Validate
            Configurations.Sort((x, y) => x.Index - y.Index);
            ValidateConfigurationsIndex();
            ValidateConfigurationsEntryCount();

            //Count
            int entryCount = NumPscEntries;
            int numConfigs = Configurations.Count;

            //Header (20 bytes)
            bytes.AddRange(BitConverter.GetBytes(PSC_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(PSC_HEADER_SIZE));
            bytes.AddRange(BitConverter.GetBytes(entryCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(numConfigs));

            //PSC Entries
            for(int i = 0; i < numConfigs; i++)
            {
                for(int a = 0; a < NumPscEntries; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(Configurations[i].PscEntries[a].Index)));
                    bytes.AddRange(BitConverter.GetBytes((Configurations[i].PscEntries[a].PscSpecEntries != null) ? Configurations[i].PscEntries[a].PscSpecEntries.Count : 0));
                    bytes.AddRange(new byte[4]);
                }
            }

            //Write Spec Entries
            for (int i = 0; i < numConfigs; i++)
            {
                for (int a = 0; a < NumPscEntries; a++)
                {
                    int specCount = (Configurations[i].PscEntries[a].PscSpecEntries != null) ? Configurations[i].PscEntries[a].PscSpecEntries.Count : 0;

                    for (int s = 0; s < specCount; s++)
                    {
                        bytes.AddRange(Configurations[i].PscEntries[a].PscSpecEntries[s].Write());
                    }
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Ensure that each configuration has the same amount of PSC Entries. If an entry does not exist, then it will be copied over.
        /// </summary>
        /// <returns></returns>
        private void ValidateConfigurationsEntryCount()
        {
            for(int i = 0; i < Configurations.Count; i++)
            {
                for(int a = 0; a < Configurations.Count; a++)
                {
                    Configurations[i].AddIfMissing(Configurations[a].PscEntries);
                }
            }
        }

        private void ValidateConfigurationsIndex()
        {
            for(int i = 0; i < Configurations.Count; i++)
            {
                if (Configurations[i].Index != i) throw new Exception(String.Format("PSC_File.Configuration Index value is invalid. Declared index was {0}, but the expected index is {1}.", Configurations[i].Index, i));
            }
        }
        
        public PSC_Configuration GetConfiguration(int index)
        {
            if (Configurations.Count - 1 >= index)
                return Configurations[index];
            else
                throw new Exception(string.Format("No PSC_Configuration exists at index {0}", index));
        }

    }

    [YAXSerializeAs("Configuration")]
    public class PSC_Configuration
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PSC_Entry")]
        public List<PSC_Entry> PscEntries { get; set; } = new List<PSC_Entry>();

        public void AddIfMissing(List<PSC_Entry> pscEntries)
        {
            foreach (var pscEntry in pscEntries)
            {
                if (PscEntries.FindIndex(m => m.Index == pscEntry.Index) == -1)
                {
                    //If entry doesn't exist, add it
                    PscEntries.Add(pscEntry);
                }
                else
                {
                    //Entry exists... so now ensure that the child spec entries exist as well
                    var entry = PscEntries.FirstOrDefault(m => m.Index == pscEntry.Index);
                    entry.AddIfMissing(pscEntry.PscSpecEntries);
                }
            }
        }

        public PSC_Entry GetPscEntry(string charaID)
        {
            if (PscEntries.Any(e => e.Index == charaID))
            {
                return PscEntries.FirstOrDefault(e => e.Index == charaID);
            }
            else
            {
                PSC_Entry pscEntry = new PSC_Entry() { Index = charaID };
                PscEntries.Add(pscEntry);
                return pscEntry;
            }
        }
    }

    public class PSC_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Chara_ID")]
        public string Index { get; set; } //int32, offset 0

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PscSpecEntry")]
        public List<PSC_SpecEntry> PscSpecEntries { get; set; } = new List<PSC_SpecEntry>();

        public void AddIfMissing(List<PSC_SpecEntry> specEntries)
        {
            foreach (var specEntry in specEntries)
            {
                if (PscSpecEntries.FindIndex(m => m.Index == specEntry.Index) == -1)
                {
                    PscSpecEntries.Add(specEntry);
                }
            }
        }

    }

    public class PSC_SpecEntry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        [BindingAutoId]
        public string Index { get; set; } //Int32, offset 0
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume2")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Camera_Position")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Ki")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Ki_Recharge")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("Stamina")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Stamina_Recharge_Move")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Stamina_Recharge_Air")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Stamina_Recharge_Ground")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Stamina_Drain_Rate_1")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Stamina_Drain_Rate_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("F_72")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("Basic_Atk")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("Basic_Ki_Atk")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("Strike_Atk")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_84 { get; set; }
        [YAXAttributeFor("Super_Ki_Blast_Atk")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("Basic_Atk_Defense")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_92 { get; set; }
        [YAXAttributeFor("Basic_Ki_Atk_Defense")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("Strike_Atk_Defense")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("Super Ki Blast Defense")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("Ground_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("Air_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("Boosting_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("Dash_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("F_124")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_124 { get; set; }
        [YAXAttributeFor("Reinforcement_Skill_Duration")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_128 { get; set; }
        [YAXAttributeFor("F_132")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_132 { get; set; }
        [YAXAttributeFor("Revival_HP_Amount")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_136 { get; set; }
        [YAXAttributeFor("F_140")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_140 { get; set; }
        [YAXAttributeFor("Reviving_Speed")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_144 { get; set; }
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
        [YAXAttributeFor("Super_Soul")]
        [YAXSerializeAs("talisman")]
        public int I_180 { get; set; }
        [YAXAttributeFor("I_184")]
        [YAXSerializeAs("value")]
        public int I_184 { get; set; }
        [YAXAttributeFor("I_188")]
        [YAXSerializeAs("value")]
        public int I_188 { get; set; }
        [YAXAttributeFor("F_192")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_192 { get; set; }

        public static PSC_SpecEntry Read(byte[] bytes, int offset)
        {
            return new PSC_SpecEntry()
            {
                Index = BitConverter.ToInt32(bytes, offset + 0).ToString(),
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                I_08 = BitConverter.ToInt32(bytes, offset + 8),
                I_12 = BitConverter.ToInt32(bytes, offset + 12),
                I_16 = BitConverter.ToInt32(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                I_36 = BitConverter.ToInt32(bytes, offset + 36),
                I_40 = BitConverter.ToInt32(bytes, offset + 40),
                I_44 = BitConverter.ToInt32(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
                F_64 = BitConverter.ToSingle(bytes, offset + 64),
                F_68 = BitConverter.ToSingle(bytes, offset + 68),
                F_72 = BitConverter.ToSingle(bytes, offset + 72),
                F_76 = BitConverter.ToSingle(bytes, offset + 76),
                F_80 = BitConverter.ToSingle(bytes, offset + 80),
                F_84 = BitConverter.ToSingle(bytes, offset + 84),
                F_88 = BitConverter.ToSingle(bytes, offset + 88),
                F_92 = BitConverter.ToSingle(bytes, offset + 92),
                F_96 = BitConverter.ToSingle(bytes, offset + 96),
                F_100 = BitConverter.ToSingle(bytes, offset + 100),
                F_104 = BitConverter.ToSingle(bytes, offset + 104),
                F_108 = BitConverter.ToSingle(bytes, offset + 108),
                F_112 = BitConverter.ToSingle(bytes, offset + 112),
                F_116 = BitConverter.ToSingle(bytes, offset + 116),
                F_120 = BitConverter.ToSingle(bytes, offset + 120),
                F_124 = BitConverter.ToSingle(bytes, offset + 124),
                F_128 = BitConverter.ToSingle(bytes, offset + 128),
                F_132 = BitConverter.ToSingle(bytes, offset + 132),
                F_136 = BitConverter.ToSingle(bytes, offset + 136),
                F_140 = BitConverter.ToSingle(bytes, offset + 140),
                F_144 = BitConverter.ToSingle(bytes, offset + 144),
                I_148 = BitConverter.ToInt32(bytes, offset + 148),
                I_152 = BitConverter.ToInt32(bytes, offset + 152),
                I_156 = BitConverter.ToInt32(bytes, offset + 156),
                I_160 = BitConverter.ToInt32(bytes, offset + 160),
                I_164 = BitConverter.ToInt32(bytes, offset + 164),
                I_168 = BitConverter.ToInt32(bytes, offset + 168),
                I_172 = BitConverter.ToInt32(bytes, offset + 172),
                I_176 = BitConverter.ToInt32(bytes, offset + 176),
                I_180 = BitConverter.ToInt32(bytes, offset + 180),
                I_184 = BitConverter.ToInt32(bytes, offset + 184),
                I_188 = BitConverter.ToInt32(bytes, offset + 188),
                F_192 = BitConverter.ToSingle(bytes, offset + 192)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            //Write values
            bytes.AddRange(BitConverter.GetBytes(int.Parse(Index)));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));
            bytes.AddRange(BitConverter.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(F_72));
            bytes.AddRange(BitConverter.GetBytes(F_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(F_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(F_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(F_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));
            bytes.AddRange(BitConverter.GetBytes(F_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));
            bytes.AddRange(BitConverter.GetBytes(F_120));
            bytes.AddRange(BitConverter.GetBytes(F_124));
            bytes.AddRange(BitConverter.GetBytes(F_128));
            bytes.AddRange(BitConverter.GetBytes(F_132));
            bytes.AddRange(BitConverter.GetBytes(F_136));
            bytes.AddRange(BitConverter.GetBytes(F_140));
            bytes.AddRange(BitConverter.GetBytes(F_144));
            bytes.AddRange(BitConverter.GetBytes(I_148));
            bytes.AddRange(BitConverter.GetBytes(I_152));
            bytes.AddRange(BitConverter.GetBytes(I_156));
            bytes.AddRange(BitConverter.GetBytes(I_160));
            bytes.AddRange(BitConverter.GetBytes(I_164));
            bytes.AddRange(BitConverter.GetBytes(I_168));
            bytes.AddRange(BitConverter.GetBytes(I_172));
            bytes.AddRange(BitConverter.GetBytes(I_176));
            bytes.AddRange(BitConverter.GetBytes(I_180));
            bytes.AddRange(BitConverter.GetBytes(I_184));
            bytes.AddRange(BitConverter.GetBytes(I_188));
            bytes.AddRange(BitConverter.GetBytes(F_192));

            //Validate and return
            if (bytes.Count != 196) throw new InvalidDataException("PSC_SpecEntry must be 196 bytes.");
            return bytes.ToArray();
        }
    }

}
