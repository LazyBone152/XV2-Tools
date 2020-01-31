using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.UTF;
using YAXLib;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.Resource;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;

namespace Xv2CoreLib.ACB_NEW
{

    [YAXSerializeAs("ACB")]
    public class ACB_File
    {
        //Supported Versions
        public static Version _1_30_0_0 = new Version("1.30.0.0"); //SDBH
        public static Version _1_29_2_0 = new Version("1.29.2.0"); //SDBH
        public static Version _1_29_0_0 = new Version("1.29.0.0"); //SDBH
        public static Version _1_28_2_0 = new Version("1.28.2.0"); //SDBH, identical to 1.27.7.0
        public static Version _1_27_7_0 = new Version("1.27.7.0"); //Used in XV2
        public static Version _1_27_2_0 = new Version("1.27.2.0"); //Used in XV2
        public static Version _1_23_1_0 = new Version("1.23.1.0"); //Unknown game
        public static Version _1_22_4_0 = new Version("1.22.4.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public static Version _1_22_0_0 = new Version("1.22.0.0"); //Not observed, just a guess
        public static Version _1_21_1_0 = new Version("1.21.1.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public static Version _1_6_0_0 = new Version("1.6.0.0"); //Used in 1 file in XV1. Missing a lot of data.
        public static Version _0_81_1_0 = new Version("0.81.1.0"); //Used in MGS3 3D. Uses CPK AWB files instead of AFS2. (wont support)

        public static Version[] SupportedVersions = new Version[] 
        {
            _1_21_1_0,
            _1_22_0_0,
            _1_22_4_0,
            _1_27_2_0,
            _1_27_7_0,
            _1_28_2_0,
            _1_29_0_0,
            _1_29_2_0,
            _1_30_0_0
        };

        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }

        [YAXDontSerialize]
        public Version Version = null;
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        public string _versionStrProp
        {
            get
            {
                return (Version != null) ? Version.ToString() : "0.0.0.0";
            }
            set
            {
                Version = new Version(value);
            }
        }

        [YAXAttributeFor("Volume")]
        [YAXSerializeAs("value")]
        public float AcbVolume { get; set; }
        [YAXAttributeFor("GUID")]
        [YAXSerializeAs("value")]
        public Guid GUID { get; set; }

        [YAXAttributeFor("FileIdentifier")]
        [YAXSerializeAs("value")]
        public uint FileIdentifier { get; set; }
        [YAXAttributeFor("Size")]
        [YAXSerializeAs("value")]
        public uint Size { get; set; }
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte Type { get; set; }
        [YAXAttributeFor("Target")]
        [YAXSerializeAs("value")]
        public byte Target { get; set; }
        [YAXAttributeFor("AcfMd5Hash")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] AcfMd5Hash { get; set; }
        [YAXAttributeFor("CategoryExtension")]
        [YAXSerializeAs("value")]
        public byte CategoryExtension { get; set; }
        [YAXAttributeFor("VersionString")]
        [YAXSerializeAs("value")]
        public string VersionString { get; set; } //Maybe calculate this based on Version?
        [YAXAttributeFor("CueLimitWorkTable")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] CueLimitWorkTable { get; set; }
        [YAXAttributeFor("NumCueLimitListWorks")]
        [YAXSerializeAs("value")]
        public ushort NumCueLimitListWorks { get; set; }
        [YAXAttributeFor("NumCueLimitNodeWorks")]
        [YAXSerializeAs("value")]
        public ushort NumCueLimitNodeWorks { get; set; }
        [YAXAttributeFor("CharacterEncodingType")]
        [YAXSerializeAs("value")]
        public byte CharacterEncodingType { get; set; }
        [YAXAttributeFor("CuePriorityType")]
        [YAXSerializeAs("value")]
        public byte CuePriorityType { get; set; }
        [YAXAttributeFor("NumCueLimit")]
        [YAXSerializeAs("value")]
        public ushort NumCueLimit { get; set; }
        [YAXAttributeFor("StreamAwbTocWork")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] StreamAwbTocWork { get; set; }

        public List<ACB_Cue> Cues { get; set; } = new List<ACB_Cue>();
        public List<ACB_Waveform> Waveforms { get; set; } = new List<ACB_Waveform>();
        public List<ACB_Aisac> Aisacs { get; set; } = new List<ACB_Aisac>();
        public List<ACB_Graph> Graphs { get; set; } = new List<ACB_Graph>();
        public List<ACB_GlobalAisacReference> GlobalAisacReferences { get; set; } = new List<ACB_GlobalAisacReference>();
        public List<ACB_Synth> Synths { get; set; } = new List<ACB_Synth>();
        public ACB_CommandTables CommandTables { get; set; }
        public List<ACB_Track> Tracks { get; set; } = new List<ACB_Track>();
        public List<ACB_Sequence> Sequences { get; set; } = new List<ACB_Sequence>();
        public List<ACB_AutoModulation> AutoModulations { get; set; } = new List<ACB_AutoModulation>();
        public List<ACB_Track> ActionTracks { get; set; } = new List<ACB_Track>();

        public AFS2_File AudioTracks { get; set; } = AFS2_File.CreateNewAwbFile();

        public UTF_File StringValueTable { get; set; } // No need to parse this
        public UTF_File OutsideLinkTable { get; set; } // No need to parse this
        public UTF_File AcfReferenceTable { get; set; } // No need to parse this
        public UTF_File SoundGeneratorTable { get; set; } // No need to parse this

        #region LoadFunctions
        public static ACB_File Load(string path, bool writeXml = false)
        {
            //Check for external awb file
            byte[] awbFile = null;
            string awbPath = string.Format("{0}/{1}.awb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

            if (File.Exists(awbPath))
            {
                awbFile = File.ReadAllBytes(awbPath);
            }


            var file = Load(File.ReadAllBytes(path), awbFile);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ACB_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static ACB_File Load(byte[] acbBytes, byte[] awbBytes)
        {
            return Load(UTF_File.LoadUtfTable(acbBytes, acbBytes.Length), awbBytes);
        }

        public static ACB_File Load(UTF_File utfFile, byte[] awbBytes)
        {
            ACB_File acbFile = new ACB_File();

            acbFile.Version = BigEndianConverter.UIntToVersion(utfFile.GetValue<uint>("Version", TypeFlag.UInt32, 0), true);

            //if (acbFile.Version < _1_21_1_0 || acbFile.Version > _1_30_0_0)
            //    throw new Exception(string.Format("ACB_File.Load: Unsupported ACB Version: {0}.\nLoad failed.", acbFile.Version));

            acbFile.AcfMd5Hash = utfFile.GetData("AcfMd5Hash", 0);
            acbFile.CategoryExtension = utfFile.GetValue<byte>("CategoryExtension", TypeFlag.UInt8, 0);
            acbFile.CharacterEncodingType = utfFile.GetValue<byte>("CharacterEncodingType", TypeFlag.UInt8, 0);
            acbFile.FileIdentifier = utfFile.GetValue<uint>("FileIdentifier", TypeFlag.UInt32, 0);
            acbFile.GUID = new Guid(utfFile.GetData("AcbGuid", 0));
            acbFile.Name = utfFile.GetValue<string>("Name", TypeFlag.String, 0);
            acbFile.NumCueLimitListWorks = utfFile.GetValue<ushort>("NumCueLimitListWorks", TypeFlag.UInt16, 0);
            acbFile.NumCueLimitNodeWorks = utfFile.GetValue<ushort>("NumCueLimitNodeWorks", TypeFlag.UInt16, 0);
            acbFile.Size = utfFile.GetValue<uint>("Size", TypeFlag.UInt32, 0);
            acbFile.Target = utfFile.GetValue<byte>("Target", TypeFlag.UInt8, 0);
            acbFile.Type = utfFile.GetValue<byte>("Type", TypeFlag.UInt8, 0);
            acbFile.VersionString = utfFile.GetValue<string>("VersionString", TypeFlag.String, 0);
            acbFile.AcbVolume = utfFile.GetValue<float>("AcbVolume", TypeFlag.Single, 0);
            acbFile.CueLimitWorkTable = utfFile.GetData("CueLimitWorkTable", 0);
            acbFile.AcfReferenceTable = utfFile.GetColumnTable("AcfReferenceTable", true);
            acbFile.OutsideLinkTable = utfFile.GetColumnTable("OutsideLinkTable", true);
            acbFile.StringValueTable = utfFile.GetColumnTable("StringValueTable", true);
            acbFile.StreamAwbTocWork = utfFile.GetData("StreamAwbTocWork", 0);

            if (acbFile.Version >= ACB_File._1_27_2_0)
            {
                acbFile.NumCueLimit = utfFile.GetValue<ushort>("NumCueLimit", TypeFlag.UInt16, 0);
                acbFile.CuePriorityType = utfFile.GetValue<byte>("CuePriorityType", TypeFlag.UInt8, 0);
            }

            if (acbFile.Version >= ACB_File._1_30_0_0)
            {
                acbFile.SoundGeneratorTable = utfFile.GetColumnTable("SoundGeneratorTable", true);
            }

            //Parse tables
            acbFile.Cues = ACB_Cue.Load(utfFile.GetColumnTable("CueTable", true), utfFile.GetColumnTable("CueNameTable", true), acbFile.Version);
            acbFile.Synths = ACB_Synth.Load(utfFile.GetColumnTable("SynthTable", false), acbFile.Version);
            acbFile.Sequences = ACB_Sequence.Load(utfFile.GetColumnTable("SequenceTable", false), acbFile.Version);
            acbFile.Tracks = ACB_Track.Load(utfFile.GetColumnTable("TrackTable", false), acbFile.Version, acbFile.Name);
            acbFile.ActionTracks = ACB_Track.Load(utfFile.GetColumnTable("ActionTrackTable", false), acbFile.Version, acbFile.Name);
            acbFile.GlobalAisacReferences = ACB_GlobalAisacReference.Load(utfFile.GetColumnTable("GlobalAisacReferenceTable", false), acbFile.Version);
            acbFile.Waveforms = ACB_Waveform.Load(utfFile.GetColumnTable("WaveformTable", false), utfFile.GetColumnTable("WaveformExtensionDataTable", false), acbFile.Version);
            acbFile.Aisacs = ACB_Aisac.Load(utfFile.GetColumnTable("AisacTable", false), utfFile.GetColumnTable("AisacControlNameTable", false), acbFile.Version);
            acbFile.Graphs = ACB_Graph.Load(utfFile.GetColumnTable("GraphTable", false), acbFile.Version);
            acbFile.AutoModulations = ACB_AutoModulation.Load(utfFile.GetColumnTable("AutoModulationTable", false), acbFile.Version);
            acbFile.CommandTables = ACB_CommandTables.Load(utfFile, acbFile.Version, acbFile);

            //AWB
            var internalAwbFile = utfFile.GetColumnAfs2File("AwbFile");
            acbFile.LoadAwbFile(internalAwbFile, false);

            if(awbBytes != null)
                acbFile.LoadAwbFile(AFS2_File.LoadAfs2File(awbBytes), true);

            //Finalization
            acbFile.SetTableRoot();

            return acbFile;
        }
        
        public static ACB_File LoadXml(string path, bool saveBinary = false)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ACB_File), YAXSerializationOptions.DontSerializeNullObjects);
            ACB_File acbFile = (ACB_File)serializer.DeserializeFromFile(path);
            acbFile.SetTableRoot();

            if (saveBinary)
            {
                string savePath = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)));
                acbFile.Save(savePath);
            }

            return acbFile;
        }

        public static ACB_File NewXv2Acb()
        {
            ACB_File acbFile = new ACB_File();
            acbFile.Version = _1_27_7_0;
            acbFile.CommandTables = new ACB_CommandTables();
            acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.SequenceCommand, Root = acbFile } };
            acbFile.SetTableRoot();
            return acbFile;
        }
        #endregion

        #region SaveFunctions
        /// <summary>
        /// Save the acb + awb.
        /// </summary>
        /// <param name="path">The save location, minus the extension.</param>
        public void Save(string path)
        {
            AFS2_File externalAwb = GenerateAwbFile(true);
            byte[] awbHeader = null;
            byte[] awbBytes = null;

            if (externalAwb != null)
                awbBytes = externalAwb.WriteAfs2File(out awbHeader);

            //Calculate hash
            byte[] streamAwbHash = null;

            if(awbBytes != null)
            {
                using(MD5Cng md5 = new MD5Cng())
                {
                    streamAwbHash = md5.ComputeHash(awbBytes);
                }
            }

            UTF_File utfFile = WriteToTable(awbHeader, streamAwbHash, Path.GetFileName(path));

            utfFile.Save(path + ".acb");

            if(awbBytes != null)
                File.WriteAllBytes(path + ".awb", awbBytes);
        }

        public UTF_File WriteToTable(byte[] streamAwbHeader, byte[] streamAwbHash, string name)
        {
            UTF_File utfFile = CreateDefaultHeaderTable();
            AFS2_File internalAwb = GenerateAwbFile(false);
            Name = name;

            //Header
            utfFile.TableName = "Header";
            utfFile.EncodingType = _EncodingType.UTF8;
            utfFile.DefaultRowCount = 1;

            //Create default tables
            UTF_File cueTable = CreateDefaultCueTable();
            UTF_File cueNameTable = CreateDefaultCueNameTable();
            UTF_File aisacTable = CreateDefaultAisacTable();
            UTF_File globalAisacRefTable = CreateDefaultGlobalAisacReferenceTable();
            UTF_File aisacControlNameTable = CreateDefaultAisacControlNameTable();
            UTF_File synthTable = CreateDefaultSynthTable();
            UTF_File sequenceTable = CreateDefaultSequenceTable();
            UTF_File waveformTable = CreateDefaultWaveformTable();
            UTF_File trackTable = CreateDefaultTrackTable(false);
            UTF_File actionTrackTable = CreateDefaultTrackTable(true);
            UTF_File graphTable = CreateDefaultGraphTable();
            UTF_File autoModTable = CreateDefaultAutoModulationTable();
            UTF_File waveformExtensionTable = CreateDefaultWaveformExtensionTable();

            //Reordering
            SortActionTracks(); //Sort them to be sequential, as in the UTF file ActionTracks are specified with a StartIndex and Count
            SortGlobalAisacRefs();

            //Fill tables
            ACB_Cue.WriteToTable(Cues, Version, cueTable, cueNameTable);
            ACB_Aisac.WriteToTable(Aisacs, Version, aisacTable, aisacControlNameTable);
            ACB_GlobalAisacReference.WriteToTable(GlobalAisacReferences, Version, globalAisacRefTable);
            ACB_Synth.WriteToTable(Synths, Version, synthTable);
            ACB_Sequence.WriteToTable(Sequences, Version, sequenceTable);
            ACB_Waveform.WriteToTable(Waveforms, Version, waveformTable, waveformExtensionTable);
            ACB_Track.WriteToTable(Tracks, Version, trackTable, Name);
            ACB_Track.WriteToTable(ActionTracks, Version, actionTrackTable, Name);
            ACB_Graph.WriteToTable(Graphs, Version, graphTable);
            ACB_AutoModulation.WriteToTable(AutoModulations, Version, autoModTable);

            //Columns
            utfFile.AddValue("FileIdentifier", TypeFlag.UInt32, 0, FileIdentifier.ToString());
            utfFile.AddValue("Size", TypeFlag.UInt32, 0, Size.ToString());
            utfFile.AddValue("Version", TypeFlag.UInt32, 0, BigEndianConverter.VersionToUInt(Version, true).ToString());
            utfFile.AddValue("Type", TypeFlag.UInt8, 0, Type.ToString());
            utfFile.AddValue("Target", TypeFlag.UInt8, 0, Target.ToString());
            utfFile.AddData("AcfMd5Hash", 0, AcfMd5Hash);
            utfFile.AddValue("CategoryExtension", TypeFlag.UInt8, 0, CategoryExtension.ToString());
            utfFile.AddData("CueTable", 0, cueTable.Write());
            utfFile.AddData("CueNameTable", 0, cueNameTable.Write());
            utfFile.AddData("WaveformTable", 0, waveformTable.Write());
            utfFile.AddData("AisacTable", 0, aisacTable.Write());
            utfFile.AddData("GraphTable", 0, graphTable.Write());
            utfFile.AddData("GlobalAisacReferenceTable", 0, globalAisacRefTable.Write());
            utfFile.AddData("AisacNameTable", 0, null); //Unknown
            utfFile.AddData("SynthTable", 0, synthTable.Write());

            if(Version >= _1_29_0_0)
                utfFile.AddData("SeqCommandTable", 0, CommandTables.WriteToTable(CommandTableType.SequenceCommand).Write());
            else
                utfFile.AddData("CommandTable", 0, CommandTables.WriteToTable(CommandTableType.SequenceCommand).Write());


            utfFile.AddData("TrackTable", 0, trackTable.Write());
            utfFile.AddData("SequenceTable", 0, sequenceTable.Write());
            utfFile.AddData("AisacControlNameTable", 0, aisacControlNameTable.Write());
            utfFile.AddData("AutoModulationTable", 0, autoModTable.Write());
            utfFile.AddData("StreamAwbTocWorkOld", 0, null);

            if(internalAwb != null)
                utfFile.AddData("AwbFile", 0, internalAwb.WriteAfs2File());
            else
                utfFile.AddData("AwbFile", 0, null);

            utfFile.AddValue("VersionString", TypeFlag.String, 0, VersionString);
            utfFile.AddData("CueLimitWorkTable", 0, null); // Unknown
            utfFile.AddValue("NumCueLimitListWorks", TypeFlag.UInt16, 0, NumCueLimitListWorks.ToString());
            utfFile.AddValue("NumCueLimitNodeWorks", TypeFlag.UInt16, 0, NumCueLimitNodeWorks.ToString());
            utfFile.AddData("AcbGuid", 0, GUID.ToByteArray());
            
            if (Version >= _1_27_2_0)
            {
                UTF_File streamAwbHashTable = CreateDefaultStreamAwbHashTable();
                streamAwbHashTable.AddValue("Name", TypeFlag.String, 0, name);
                streamAwbHashTable.AddData("Hash", 0, streamAwbHash);
                utfFile.AddData("StreamAwbHash", 0, streamAwbHashTable.Write());
            }
            else
            {
                utfFile.AddData("StreamAwbHash", 0, streamAwbHash);
            }

            utfFile.AddData("StreamAwbTocWork_Old", 0, null);
            utfFile.AddValue("AcbVolume", TypeFlag.Single, 0, AcbVolume.ToString());
            utfFile.AddData("StringValueTable", 0, (StringValueTable != null) ? StringValueTable.Write() : null);
            utfFile.AddData("OutsideLinkTable", 0, (OutsideLinkTable != null) ? OutsideLinkTable.Write() : null);
            utfFile.AddData("BlockSequenceTable", 0, null); //Not implemented
            utfFile.AddData("BlockTable", 0, null); //Not implemented
            utfFile.AddValue("Name", TypeFlag.String, 0, name);
            utfFile.AddValue("CharacterEncodingType", TypeFlag.UInt8, 0, CharacterEncodingType.ToString());
            utfFile.AddData("EventTable", 0, null); //Not implemented
            utfFile.AddData("ActionTrackTable", 0, actionTrackTable.Write());
            utfFile.AddData("AcfReferenceTable", 0, (AcfReferenceTable != null) ? AcfReferenceTable.Write() : null);

            if(Version >= _1_22_0_0)
            {
                utfFile.AddData("WaveformExtensionDataTable", 0, waveformExtensionTable.Write());
            }
            else
            {
                utfFile.AddValue("R21", TypeFlag.UInt8, 0, "0");
            }

            if (Version >= _1_23_1_0)
            {
                utfFile.AddData("BeatSyncInfoTable", 0, null); //Not implemented
                utfFile.AddValue("CuePriorityType", TypeFlag.UInt8, 0, CuePriorityType.ToString());
                utfFile.AddValue("NumCueLimit", TypeFlag.UInt16, 0, NumCueLimit.ToString());
            }
            else
            {
                utfFile.AddValue("R20", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R19", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R18", TypeFlag.UInt8, 0, "0");
            }

            if (Version >= _1_29_0_0)
            {
                utfFile.AddData("TrackCommandTable", 0, CommandTables.WriteToTable(CommandTableType.TrackCommand).Write());
                utfFile.AddData("SynthCommandTable", 0, CommandTables.WriteToTable(CommandTableType.SynthCommand).Write());
                utfFile.AddData("TrackEventTable", 0, CommandTables.WriteToTable(CommandTableType.TrackEvent).Write());
                utfFile.AddData("SeqParameterPalletTable", 0, null);
                utfFile.AddData("TrackParameterPalletTable", 0, null);
                utfFile.AddData("SynthParameterPalletTable", 0, null);
            }
            else
            {
                utfFile.AddValue("R17", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R16", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R15", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R14", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R13", TypeFlag.UInt8, 0, "0");
                utfFile.AddValue("R12", TypeFlag.UInt8, 0, "0");
            }

            if (Version >= _1_30_0_0)
            {
                utfFile.AddData("SoundGeneratorTable", 0, SoundGeneratorTable.Write());
            }
            else
            {
                utfFile.AddValue("R11", TypeFlag.UInt8, 0, "0");
            }

            //Reserved columns
            utfFile.AddValue("R10", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R9", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R8", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R7", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R6", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R5", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R4", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R3", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R2", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R1", TypeFlag.UInt8, 0, "0");
            utfFile.AddValue("R0", TypeFlag.UInt8, 0, "0");


            utfFile.AddData("PaddingArea", 0, null);
            utfFile.AddData("StreamAwbTocWork", 0, StreamAwbTocWork); //Not sure what this is...

            if (Version >= _1_27_2_0)
            {
                UTF_File streamAwbHeaderTable = CreateDefaultStreamAwbHeaderTable();
                streamAwbHeaderTable.AddData("Header", 0, streamAwbHeader);
                utfFile.AddData("StreamAwbAfs2Header", 0, streamAwbHeaderTable.Write());
            }
            else
            {
                utfFile.AddData("StreamAwbAfs2Header", 0, streamAwbHeader);
            }

            return utfFile;
        }

        #endregion

        #region InitFunctions
        private void LoadAwbFile(AFS2_File afs2File, bool streaming)
        {
            if (afs2File == null) return;
            if (afs2File.EmbeddedFiles == null) return;

            foreach (var entry in afs2File.EmbeddedFiles)
            {
                int oldId = entry.AwbId;
                int newId = AudioTracks.AddEntry(entry, true);

                if (oldId != newId)
                    WaveformAwbIdRefactor(oldId, newId, streaming);
            }
        }

        public void SetTableRoot()
        {
            CommandTables.Root = this;
            foreach (var commandTable in CommandTables.CommandTables)
            {
                commandTable.Root = this;
            }
        }
        #endregion

        #region AddFunctions
        public ACB_Cue AddCue(string name, ReferenceType type)
        {
            ACB_Cue newCue = new ACB_Cue();
            newCue.ID = (uint)GetFreeCueId();
            newCue.Name = name;
            newCue.ReferenceType = type;
            newCue.ReferenceIndex = ushort.MaxValue;
            
            if(type == ReferenceType.Sequence)
            {
                newCue.ReferenceIndex = (ushort)Sequences.Count;
                Sequences.Add(new ACB_Sequence());
            }
            else if(type == ReferenceType.Synth)
            {
                newCue.ReferenceIndex = (ushort)Synths.Count;
                Synths.Add(new ACB_Synth());
            }
            //Dont add Waveform entry until a track is added

            Cues.Add(newCue);
            return newCue;
        }

        #endregion

        #region RefactorFunctions
        private void WaveformAwbIdRefactor(int oldAwbId, int newAwbId, bool stream)
        {
            if (Waveforms == null) return;

            foreach(var waveform in Waveforms.Where(w => w.Streaming == stream && w.AwbId == oldAwbId))
            {
                waveform.AwbId = (ushort)newAwbId;
            }
        }

        private void SortActionTracks()
        {
            List<ACB_Track> newActionTracks = new List<ACB_Track>();

            //Synths
            foreach(var synth in Synths)
            {
                synth.ActionTracks.Sort();
                ushort startIndex = CheckTableSequence(synth.ActionTracks, newActionTracks, ActionTracks);

                //If the correct sequence of entries doesn't exist, then we must add them
                if(startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newActionTracks.Count;

                    for (int i = 0; i < synth.ActionTracks.Count; i++)
                    {
                        newActionTracks.Add(ActionTracks[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < synth.ActionTracks.Count; i++)
                {
                    synth.ActionTracks[i] = (ushort)(startIndex + i);
                }
            }

            //Sequences
            foreach (var sequence in Sequences)
            {
                sequence.ActionTracks.Sort();
                ushort startIndex = CheckTableSequence(sequence.ActionTracks, newActionTracks, ActionTracks);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newActionTracks.Count;

                    for (int i = 0; i < sequence.ActionTracks.Count; i++)
                    {
                        newActionTracks.Add(ActionTracks[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < sequence.ActionTracks.Count; i++)
                {
                    sequence.ActionTracks[i] = (ushort)(startIndex + i);
                }
            }

        }
        
        private void SortGlobalAisacRefs()
        {
            List<ACB_GlobalAisacReference> newAisacRefs = new List<ACB_GlobalAisacReference>();

            //Synths
            foreach (var synth in Synths)
            {
                synth.GlobalAisacRefs.Sort();
                ushort startIndex = CheckTableSequence(synth.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < synth.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < synth.GlobalAisacRefs.Count; i++)
                {
                    synth.GlobalAisacRefs[i] = (ushort)(startIndex + i);
                }
            }

            //Sequences
            foreach (var sequence in Sequences)
            {
                sequence.GlobalAisacRefs.Sort();
                ushort startIndex = CheckTableSequence(sequence.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < sequence.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < sequence.GlobalAisacRefs.Count; i++)
                {
                    sequence.GlobalAisacRefs[i] = (ushort)(startIndex + i);
                }
            }

            //Tracks
            foreach (var track in Tracks)
            {
                track.GlobalAisacRefs.Sort();
                ushort startIndex = CheckTableSequence(track.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                {
                    track.GlobalAisacRefs[i] = (ushort)(startIndex + i);
                }
            }

            //ActionTracks
            foreach (var track in ActionTracks)
            {
                track.GlobalAisacRefs.Sort();
                ushort startIndex = CheckTableSequence(track.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[i]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                {
                    track.GlobalAisacRefs[i] = (ushort)(startIndex + i);
                }
            }
        }

        private ushort CheckTableSequence<T>(List<ushort> idx, List<T> newList, List<T> mainList) where T : AcbTableBase
        {
            if (idx.Count == 0) return ushort.MaxValue;

            for (int i = 0; i < newList.Count; i++)
            {
                if (newList[i] == mainList[idx[0]])
                {
                    for (int a = 1; a < idx.Count; a++)
                    {
                        if ((i + a) >= newList.Count) break;
                        if (a >= ActionTracks.Count) break;
                        if (newList[i + a] != mainList[a]) break;

                        if (idx.Count == a - 1) return (ushort)i; //Reached end of idx list, and no breaks have happened
                    }
                }
            }

            return ushort.MaxValue;
        }

        #endregion

        #region MiscPrivateFunctions
        private AFS2_File GenerateAwbFile(bool streamingAwb)
        {
            //Get list of tracks to include
            List<int> tracks = new List<int>();

            foreach (var waveforms in Waveforms.Where(w => w.Streaming == streamingAwb && w.AwbId != ushort.MaxValue))
                tracks.Add(waveforms.AwbId);

            //If there are no tracks then dont create a awb file.
            if (tracks.Count == 0)
                return null;

            //Create the AWB file
            AFS2_File awbFile = AFS2_File.CreateNewAwbFile();

            foreach(var track in AudioTracks.EmbeddedFiles)
            {
                if (tracks.Contains(track.AwbId))
                {
                    awbFile.EmbeddedFiles.Add(track);
                }
            }

            return awbFile;
        }

        #endregion

        #region DefaultTableFunctions
        internal UTF_File CreateDefaultHeaderTable()
        {
            UTF_File utfFile = new UTF_File("Header");

            //Columns
            utfFile.Columns.Add(new UTF_Column("FileIdentifier", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("Size", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("Version", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("Type", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("Target", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("AcfMd5Hash", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("CategoryExtension", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("CueTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("CueNameTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("WaveformTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AisacTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("GraphTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacReferenceTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AisacNameTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("SynthTable", TypeFlag.Data));

            if (Version >= _1_29_0_0)
                utfFile.Columns.Add(new UTF_Column("SeqCommandTable", TypeFlag.Data));
            else
                utfFile.Columns.Add(new UTF_Column("CommandTable", TypeFlag.Data));

            utfFile.Columns.Add(new UTF_Column("TrackTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("SequenceTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AisacControlNameTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AutoModulationTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("StreamAwbTocWorkOld", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AwbFile", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("VersionString", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("CueLimitWorkTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("NumCueLimitListWorks", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("NumCueLimitNodeWorks", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("AcbGuid", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("StreamAwbHash", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("StreamAwbTocWork_Old", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AcbVolume", TypeFlag.Single));
            utfFile.Columns.Add(new UTF_Column("StringValueTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("OutsideLinkTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("BlockSequenceTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("BlockTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("Name", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("CharacterEncodingType", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("EventTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("ActionTrackTable", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("AcfReferenceTable", TypeFlag.Data));


            if (Version >= _1_22_0_0)
                utfFile.Columns.Add(new UTF_Column("WaveformExtensionDataTable", TypeFlag.Data));
            else
                utfFile.Columns.Add(new UTF_Column("R21", TypeFlag.UInt8));

            if (Version >= _1_23_1_0)
            {
                utfFile.Columns.Add(new UTF_Column("BeatSyncInfoTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("CuePriorityType", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("NumCueLimit", TypeFlag.UInt16));
            }
            else
            {
                utfFile.Columns.Add(new UTF_Column("R20", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R19", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R18", TypeFlag.UInt8));
            }

            if (Version >= _1_29_0_0)
            {
                utfFile.Columns.Add(new UTF_Column("TrackCommandTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("SynthCommandTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("TrackEventTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("SeqParameterPalletTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("TrackParameterPalletTable", TypeFlag.Data));
                utfFile.Columns.Add(new UTF_Column("SynthParameterPalletTable", TypeFlag.Data));
            }
            else
            {
                utfFile.Columns.Add(new UTF_Column("R17", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R16", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R15", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R14", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R13", TypeFlag.UInt8));
                utfFile.Columns.Add(new UTF_Column("R12", TypeFlag.UInt8));
            }

            if (Version >= _1_30_0_0)
                utfFile.Columns.Add(new UTF_Column("SoundGeneratorTable", TypeFlag.Data)); //R11
            else
                utfFile.Columns.Add(new UTF_Column("R11", TypeFlag.UInt8));

            utfFile.Columns.Add(new UTF_Column("R10", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R9", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R8", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R7", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R6", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R5", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R4", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R3", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R2", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R1", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("R0", TypeFlag.UInt8));

            utfFile.Columns.Add(new UTF_Column("PaddingArea", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("StreamAwbTocWork", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("StreamAwbAfs2Header", TypeFlag.Data));

            return utfFile;
        }

        internal UTF_File CreateDefaultCueTable()
        {
            UTF_File utfFile = new UTF_File("Cue");

            //Columns
            utfFile.Columns.Add(new UTF_Column("CueId", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("ReferenceType", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("ReferenceIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("UserData", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("Worksize", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("AisacControlMap", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("Length", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("NumAisacControlMaps", TypeFlag.UInt8));

            if (Version >= _1_22_0_0)
                utfFile.Columns.Add(new UTF_Column("HeaderVisibility", TypeFlag.UInt8));

            return utfFile;
        }

        internal UTF_File CreateDefaultCueNameTable()
        {
            UTF_File utfFile = new UTF_File("CueName");

            //Columns
            utfFile.Columns.Add(new UTF_Column("CueName", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("CueIndex", TypeFlag.UInt16));

            return utfFile;
        }

        internal UTF_File CreateDefaultWaveformTable()
        {
            UTF_File utfFile = new UTF_File("Waveform");

            //Columns
            if (Version >= _1_27_2_0)
                utfFile.Columns.Add(new UTF_Column("MemoryAwbId", TypeFlag.UInt16));
            else
                utfFile.Columns.Add(new UTF_Column("Id", TypeFlag.UInt16));


            utfFile.Columns.Add(new UTF_Column("EncodeType", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("Streaming", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("NumChannels", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("LoopFlag", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("SamplingRate", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("NumSamples", TypeFlag.UInt32));

            if (Version >= _1_22_0_0)
                utfFile.Columns.Add(new UTF_Column("ExtensionData", TypeFlag.UInt16));
            else
                utfFile.Columns.Add(new UTF_Column("ExtensionData", TypeFlag.Data));

            if (Version >= _1_27_2_0)
            {
                utfFile.Columns.Add(new UTF_Column("StreamAwbPortNo", TypeFlag.UInt16));
                utfFile.Columns.Add(new UTF_Column("StreamAwbId", TypeFlag.UInt16));
            }

            return utfFile;
        }

        internal UTF_File CreateDefaultAisacTable()
        {
            UTF_File utfFile = new UTF_File("Aisac");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Id", TypeFlag.Int16),
                    new UTF_Column("Type", TypeFlag.UInt8),
                    new UTF_Column("ControlId", TypeFlag.UInt16),
                    new UTF_Column("RandomRange", TypeFlag.Single),
                    new UTF_Column("AutoModulationIndex", TypeFlag.UInt16),
                    new UTF_Column("GraphIndexes", TypeFlag.Data),
                    new UTF_Column("DefaultControlFlag", TypeFlag.UInt8),
                    new UTF_Column("DefaultControl", TypeFlag.Single),
                    new UTF_Column("GraphBitFlag", TypeFlag.UInt8)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultGraphTable()
        {
            UTF_File utfFile = new UTF_File("Graph");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Type", TypeFlag.UInt16),
                    new UTF_Column("Controls", TypeFlag.Data),
                    new UTF_Column("Destinations", TypeFlag.Data),
                    new UTF_Column("Curve", TypeFlag.Data),
                    new UTF_Column("ControlWorkArea", TypeFlag.Single),
                    new UTF_Column("DestinationWorkArea", TypeFlag.Single)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultGlobalAisacReferenceTable()
        {
            UTF_File utfFile = new UTF_File("GlobalAisacReference");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Name", TypeFlag.String)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultAisacControlNameTable()
        {
            UTF_File utfFile = new UTF_File("AisacControlName");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("AisacControlId", TypeFlag.UInt16),
                    new UTF_Column("AisacControlName", TypeFlag.String)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultSynthTable()
        {
            UTF_File utfFile = new UTF_File("Synth");

            //Columns
            utfFile.Columns.Add(new UTF_Column("Type", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("VoiceLimitGroupName", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("CommandIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ReferenceItems", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("LocalAisacs", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ControlWorkArea1", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ControlWorkArea2", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("TrackValues", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("ParameterPallet", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ActionTrackStartIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("NumActionTracks", TypeFlag.UInt16));

            return utfFile;
        }
        
        internal UTF_File CreateDefaultSequenceTable()
        {
            UTF_File utfFile = new UTF_File("Sequence");

            //Columns
            utfFile.Columns.Add(new UTF_Column("PlaybackRatio", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("NumTracks", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("TrackIndex", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("CommandIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("LocalAisacs", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ParameterPallet", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ActionTrackStartIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("NumActionTracks", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("TrackValues", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("Type", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("ControlWorkArea1", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ControlWorkArea2", TypeFlag.UInt16));

            return utfFile;
        }

        internal UTF_File CreateDefaultTrackTable(bool actionTrack)
        {
            UTF_File utfFile = new UTF_File((actionTrack) ? "ActionTrack" : "Track");

            //Columns
            utfFile.Columns.Add(new UTF_Column("EventIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("CommandIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("LocalAisacs", TypeFlag.Data));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("ParameterPallet", TypeFlag.UInt16));
            utfFile.Columns.Add(new UTF_Column("TargetType", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("TargetName", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("TargetId", TypeFlag.UInt32));
            utfFile.Columns.Add(new UTF_Column("TargetAcbName", TypeFlag.String));
            utfFile.Columns.Add(new UTF_Column("Scope", TypeFlag.UInt8));
            utfFile.Columns.Add(new UTF_Column("TargetTrackNo", TypeFlag.UInt16));

            return utfFile;
        }

        internal UTF_File CreateDefaultAutoModulationTable()
        {
            UTF_File utfFile = new UTF_File("AutoModulation");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Type", TypeFlag.UInt8),
                    new UTF_Column("TriggerType", TypeFlag.UInt8),
                    new UTF_Column("Time", TypeFlag.UInt32),
                    new UTF_Column("Key", TypeFlag.UInt32)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultWaveformExtensionTable()
        {
            UTF_File utfFile = new UTF_File("WaveformExtensionData");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("LoopStart", TypeFlag.UInt32),
                    new UTF_Column("LoopEnd", TypeFlag.UInt32)
                };

            return utfFile;
        }
        
        internal UTF_File CreateDefaultStreamAwbHashTable()
        {
            UTF_File utfFile = new UTF_File("StreamAwb");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Name", TypeFlag.String),
                    new UTF_Column("Hash", TypeFlag.Data)
                };

            return utfFile;
        }

        internal UTF_File CreateDefaultStreamAwbHeaderTable()
        {
            UTF_File utfFile = new UTF_File("StreamAwbHeader");

            utfFile.Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Header", TypeFlag.Data)
                };

            return utfFile;
        }

        #endregion

        public AFS2_AudioFile GetAfs2Entry(int id)
        {
            return AudioTracks.GetEntry((ushort)id);
        }

        public int GetFreeCueId()
        {
            int id = 0;

            while (Cues.Where(c => c.ID != id).Count() > 0)
                id++;

            return id;
        }
    }

    [YAXSerializeAs("Cue")]
    public class ACB_Cue : AcbTableBase
    {
        [YAXAttributeForClass]
        public uint ID { get; set; } //CueID (uint)
        [YAXAttributeForClass]
        public string Name { get; set; } //CueName
        [YAXAttributeFor("UserData")]
        [YAXSerializeAs("value")]
        public string UserData { get; set; }
        [YAXAttributeFor("Worksize")]
        [YAXSerializeAs("value")]
        public ushort Worksize { get; set; }
        [YAXAttributeFor("AisacControlMap")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] AisacControlMap { get; set; }
        [YAXAttributeFor("ReferenceType")]
        [YAXSerializeAs("value")]
        public ReferenceType ReferenceType { get; set; }
        [YAXAttributeFor("ReferenceIndex")]
        [YAXSerializeAs("value")]
        public ushort ReferenceIndex { get; set; }
        [YAXAttributeFor("Length")]
        [YAXSerializeAs("value")]
        public uint Length { get; set; }
        [YAXAttributeFor("NumAisacControlMaps")]
        [YAXSerializeAs("value")]
        public uint NumAisacControlMaps { get; set; }
        [YAXAttributeFor("HeaderVisibility")]
        [YAXSerializeAs("value")]
        public byte HeaderVisibility { get; set; }
        
        public static List<ACB_Cue> Load(UTF_File cueTable, UTF_File nameTable, Version ParseVersion)
        {
            List<ACB_Cue> cues = new List<ACB_Cue>();

            for(int i = 0; i < cueTable.DefaultRowCount; i++)
            {
                cues.Add(Load(cueTable, i, nameTable, ParseVersion));
            }

            return cues;
        }

        public static ACB_Cue Load(UTF_File cueTable, int index, UTF_File nameTable, Version ParseVersion)
        {
            ACB_Cue cue = new ACB_Cue();

            //Cue data
            cue.ID = cueTable.GetValue<uint>("CueId", TypeFlag.UInt32, index);
            cue.UserData = cueTable.GetValue<string>("UserData", TypeFlag.String, index);
            cue.Worksize = cueTable.GetValue<ushort>("Worksize", TypeFlag.UInt16, index);
            cue.AisacControlMap = cueTable.GetData("AisacControlMap", index);
            cue.Length = cueTable.GetValue<uint>("Length", TypeFlag.UInt32, index);
            cue.NumAisacControlMaps = cueTable.GetValue<byte>("NumAisacControlMaps", TypeFlag.UInt8, index);

            //Name
            int nameIdx = nameTable.IndexOfRow("CueIndex", index.ToString());

            if (nameIdx != -1)
            {
                cue.Name = nameTable.GetValue<string>("CueName", TypeFlag.String, nameIdx);
            }
            else
            {
                //Cue has no name
                cue.Name = string.Empty;
            }

            if (ParseVersion >= ACB_File._1_22_0_0)
            {
                cue.HeaderVisibility = cueTable.GetValue<byte>("HeaderVisibility", TypeFlag.UInt8, index);
            }

            //ReferenceItem
            cue.ReferenceType = (ReferenceType)cueTable.GetValue<byte>("ReferenceType", TypeFlag.UInt8, index);
            cue.ReferenceIndex = cueTable.GetValue<ushort>("ReferenceIndex", TypeFlag.UInt16, index);

            return cue;
        }

        public static void WriteToTable(IList<ACB_Cue> cues, Version ParseVersion, UTF_File utfTable, UTF_File nameTable)
        {
            for(int i = 0; i < cues.Count; i++)
            {
                cues[i].WriteToTable(utfTable, i, ParseVersion, nameTable);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, UTF_File nameTable)
        {
            utfTable.AddValue("CueId", TypeFlag.UInt32, index, ID.ToString());
            utfTable.AddValue("UserData", TypeFlag.String, index, UserData.ToString());
            utfTable.AddValue("Worksize", TypeFlag.UInt16, index, Worksize.ToString());
            utfTable.AddData("AisacControlMap", index, AisacControlMap);
            utfTable.AddValue("Length", TypeFlag.UInt32, index, Length.ToString());
            utfTable.AddValue("NumAisacControlMaps", TypeFlag.UInt8, index, NumAisacControlMaps.ToString());
            
            if (ParseVersion >= ACB_File._1_22_0_0)
                utfTable.AddValue("HeaderVisibility", TypeFlag.UInt8, index, HeaderVisibility.ToString());
            
            //ReferenceItem
            utfTable.AddValue("ReferenceType", TypeFlag.UInt8, index, ((byte)ReferenceType).ToString());
            utfTable.AddValue("ReferenceIndex", TypeFlag.UInt16, index, ReferenceIndex.ToString());

            //Name
            if (string.IsNullOrWhiteSpace(Name))
                Name = "cue_" + index;
            
            int newRowIdx = nameTable.RowCount();

            nameTable.AddValue("CueName", TypeFlag.String, newRowIdx, Name);
            nameTable.AddValue("CueIndex", TypeFlag.UInt16, newRowIdx, index.ToString());
        }
    }

    [YAXSerializeAs("Synth")]
    public class ACB_Synth : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte Type { get; set; }
        [YAXAttributeFor("VoiceLimitGroupName")]
        [YAXSerializeAs("value")]
        public string VoiceLimitGroupName { get; set; }
        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("TrackValues")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] TrackValues { get; set; }

        //ReferenceItems
        [YAXAttributeFor("ReferenceType")]
        [YAXSerializeAs("value")]
        public ReferenceType ReferenceType { get; set; }
        [YAXAttributeFor("ReferenceIndex")]
        [YAXSerializeAs("value")]
        public ushort ReferenceIndex { get; set; } = ushort.MaxValue;

        //CommandTable
        [YAXAttributeFor("CommandIndex")]
        [YAXSerializeAs("value")]
        public ushort CommandIndex { get; set; } = ushort.MaxValue;

        //LocalAisac
        [YAXAttributeFor("LocalAisac")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<ushort> LocalAisac { get; set; } = new List<ushort>();

        //GlobalAisac
        [YAXAttributeFor("GlobalAisacRefs")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<ushort> GlobalAisacRefs { get; set; } = new List<ushort>();

        //ActionTrack
        [YAXAttributeFor("ActionTracks")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<ushort> ActionTracks { get; set; } = new List<ushort>();

        public static List<ACB_Synth> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_Synth> rows = new List<ACB_Synth>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i));
            }

            return rows;
        }

        public static ACB_Synth Load(UTF_File synthTable, int index)
        {
            ACB_Synth synth = new ACB_Synth();
            synth.Index = index;
            synth.Type = synthTable.GetValue<byte>("Type", TypeFlag.UInt8, index);
            synth.VoiceLimitGroupName = synthTable.GetValue<string>("VoiceLimitGroupName", TypeFlag.String, index);
            synth.TrackValues = synthTable.GetData("TrackValues", index);
            synth.ParameterPallet = synthTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index);
            synth.Type = synthTable.GetValue<byte>("Type", TypeFlag.UInt8, index);

            //Attached data
            ushort[] referenceItems = BigEndianConverter.ToUInt16Array(synthTable.GetData("ReferenceItems", index));
            synth.CommandIndex = synthTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);
            synth.LocalAisac = BigEndianConverter.ToUInt16Array(synthTable.GetData("LocalAisacs", index)).ToList();
            var globalAisacStartIndex = synthTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
            var globalAisacNumRefs = synthTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);
            
            for (int i = 0; i < globalAisacNumRefs; i++)
            {
                synth.GlobalAisacRefs.Add((ushort)(globalAisacStartIndex + i));
            }

            int numActionTracks = synthTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, index);
            int actionTracksStartIndex = synthTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, index);

            for(int i = 0; i < numActionTracks; i++)
            {
                synth.ActionTracks.Add((ushort)(actionTracksStartIndex + i));
            }

            //ReferenceItems
            if (referenceItems.Length != 0 && referenceItems.Length != 2)
            {
                throw new Exception(string.Format("Synth: Invalid ReferenceItems size.\nSize = {0}\nExpected = 0 or 2", referenceItems.Length));
            }

            if (referenceItems.Length == 2)
            {
                synth.ReferenceType = (ReferenceType)referenceItems[0];
                synth.ReferenceIndex = referenceItems[1];
            }
            else
            {
                synth.ReferenceType = ReferenceType.Nothing;
                synth.ReferenceIndex = ushort.MaxValue;
            }

            return synth;
        }

        public static void WriteToTable(IList<ACB_Synth> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            ushort actionTrackIndex = (ActionTracks.Count > 0) ? ActionTracks[0] : ushort.MaxValue;
            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0] : ushort.MaxValue;
            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString());
            utfTable.AddValue("VoiceLimitGroupName", TypeFlag.String, index, VoiceLimitGroupName.ToString());
            utfTable.AddValue("ControlWorkArea1", TypeFlag.UInt16, index, index.ToString());
            utfTable.AddValue("ControlWorkArea2", TypeFlag.UInt16, index, index.ToString());
            utfTable.AddData("TrackValues", index, TrackValues);
            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString());
            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString());
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString());
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString());
            utfTable.AddValue("NumActionTracks", TypeFlag.UInt16, index, ActionTracks.Count.ToString());
            utfTable.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, index, actionTrackIndex.ToString());

            if (ReferenceType != ReferenceType.Nothing)
            {
                ushort[] refItems = new ushort[2] { (ushort)ReferenceType, ReferenceIndex };
                utfTable.AddData("ReferenceItems", index, BigEndianConverter.GetBytes(refItems));
            }
            else
            {
                utfTable.AddData("ReferenceItems", index, null);
            }
            
            if(LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(LocalAisac.ToArray()));
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null);
            }
        }

    }

    [YAXSerializeAs("Sequence")]
    public class ACB_Sequence : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public SequenceType Type { get; set; }
        [YAXAttributeFor("PlaybackRatio")]
        [YAXSerializeAs("value")]
        public ushort PlaybackRatio { get; set; } = 100;
        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;

        //Tracks
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Track")]
        public ObservableCollection<ACB_SequenceTrack> Tracks { get; set; } = new ObservableCollection<ACB_SequenceTrack>();

        //CommandIndex
        [YAXAttributeFor("CommandIndex")]
        [YAXSerializeAs("value")]
        public ushort CommandIndex { get; set; } = ushort.MaxValue;

        //LocalAisac
        [YAXAttributeFor("LocalAisac")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<ushort> LocalAisac { get; set; } = new List<ushort>();

        //GlobalAisac
        [YAXAttributeFor("GlobalAisacRefs")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<ushort> GlobalAisacRefs { get; set; } = new List<ushort>();

        //ActionTrack
        [YAXAttributeFor("ActionTracks")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<ushort> ActionTracks { get; set; } = new List<ushort>();

        public static List<ACB_Sequence> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_Sequence> rows = new List<ACB_Sequence>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i));
            }

            return rows;
        }

        public static ACB_Sequence Load(UTF_File sequenceTable, int index)
        {
            ACB_Sequence sequence = new ACB_Sequence();
            sequence.Index = index;

            //Sequence data
            sequence.ParameterPallet = sequenceTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index);
            sequence.Type = (SequenceType)sequenceTable.GetValue<byte>("Type", TypeFlag.UInt8, index);
            sequence.PlaybackRatio = sequenceTable.GetValue<ushort>("PlaybackRatio", TypeFlag.UInt16, index);

            //Attached data
            //sequence.NumTracks = sequenceTable.GetValue<ushort>("NumTracks", TypeFlag.UInt16, index); //Redundant value...
            sequence.CommandIndex = sequenceTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);
            sequence.LocalAisac = BigEndianConverter.ToUInt16Array(sequenceTable.GetData("LocalAisacs", index)).ToList();

            var globalAisacStartIndex = sequenceTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
            var globalAisacNumRefs = sequenceTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);

            for (int i = 0; i < globalAisacNumRefs; i++)
            {
                sequence.GlobalAisacRefs.Add((ushort)(globalAisacStartIndex + i));
            }

            int numActionTracks = sequenceTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, index);
            int actionTracksStartIndex = sequenceTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, index);

            for (int i = 0; i < numActionTracks; i++)
            {
                sequence.ActionTracks.Add((ushort)(actionTracksStartIndex + i));
            }

            //Tracks
            var trackValues = BigEndianConverter.ToUInt16Array(sequenceTable.GetData("TrackValues", index)).ToList();
            var tracks = BigEndianConverter.ToUInt16Array(sequenceTable.GetData("TrackIndex", index)).ToList();
            sequence.Tracks = new ObservableCollection<ACB_SequenceTrack>();
            
            //Add TrackValues to match Tracks, if they were not in the acb file (needed for non-Random/RandomNoRepeat types, as they dont have or need the values)
            while (trackValues.Count < tracks.Count)
            {
                trackValues.Add((ushort)(100 / tracks.Count));
            }

            //Delete TrackValues remainder (will be added in on rebuild, if needed)
            if (trackValues.Count == tracks.Count + 1 && trackValues.Count > 0)
            {
                trackValues.RemoveAt(trackValues.Count - 1);
            }

            if (trackValues.Count > tracks.Count)
            {
                throw new Exception("ACB_Sequence.Load: TrackValues count is greater than Tracks count. Load failed.");
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                sequence.Tracks.Add(new ACB_SequenceTrack() { Index = tracks[i], Percentage = trackValues[i] });
            }
            
            
            return sequence;
        }

        public static void WriteToTable(IList<ACB_Sequence> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            //Create TrackIndex and TrackValue arrays
            List<ushort> trackValues = new List<ushort>();
            List<ushort> trackIndexes = new List<ushort>();

            int total = 0;
            foreach (var track in Tracks)
            {
                trackValues.Add(track.Percentage);
                trackIndexes.Add(track.Index);
                total += track.Percentage;
            }

            //Add remainder value to TrackValues IF they do not all add up to 100.
            if (total < 100)
                trackValues.Add((ushort)(100 - total));

            //Write columns
            ushort actionTrackIndex = (ActionTracks.Count > 0) ? ActionTracks[0] : ushort.MaxValue;
            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0] : ushort.MaxValue;

            utfTable.AddValue("PlaybackRatio", TypeFlag.UInt16, index, PlaybackRatio.ToString());
            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString());
            utfTable.AddValue("ControlWorkArea1", TypeFlag.UInt16, index, index.ToString());
            utfTable.AddValue("ControlWorkArea2", TypeFlag.UInt16, index, index.ToString());

            if(trackValues.Count > 0 && (Type == SequenceType.Random || Type == SequenceType.RandomNoRepeat))
            {
                utfTable.AddData("TrackValues", index, BigEndianConverter.GetBytes(trackValues.ToArray()));
            }
            else
            {
                utfTable.AddData("TrackValues", index, null);
            }

            utfTable.AddValue("Type", TypeFlag.UInt8, index, ((byte)Type).ToString());
            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString());
            
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString());
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString());
            utfTable.AddValue("NumActionTracks", TypeFlag.UInt16, index, ActionTracks.Count.ToString());
            utfTable.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, index, actionTrackIndex.ToString());

            if (trackIndexes.Count > 0)
            {
                utfTable.AddData("TrackIndex", index, BigEndianConverter.GetBytes(trackIndexes.ToArray()));
                utfTable.AddValue("NumTracks", TypeFlag.UInt16, index, Tracks.Count.ToString());
            }
            else
            {
                utfTable.AddData("TrackIndex", index, null);
                utfTable.AddValue("NumTracks", TypeFlag.UInt16, index, "0");
            }

            if (LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(LocalAisac.ToArray()));
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null);
            }
        }

    }

    [YAXSerializeAs("Track")]
    public class ACB_SequenceTrack
    {
        [YAXAttributeForClass]
        public ushort Index { get; set; } //TrackIndex
        [YAXAttributeForClass]
        public ushort Percentage { get; set; } //TrackValues
    }

    [YAXSerializeAs("Track")]
    public class ACB_Track : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; }
        [YAXAttributeFor("TargetType")]
        [YAXSerializeAs("value")]
        public byte TargetType { get; set; }
        [YAXAttributeFor("TargetName")]
        [YAXSerializeAs("value")]
        public string TargetName { get; set; }
        [YAXAttributeFor("TargetId")]
        [YAXSerializeAs("value")]
        public uint TargetId { get; set; }
        [YAXAttributeFor("TargetAcbName")]
        [YAXSerializeAs("value")]
        public string TargetAcbName { get; set; }
        [YAXAttributeFor("TargetSelf")]
        [YAXSerializeAs("value")]
        public bool TargetSelf { get; set; } //When true, set TargetAcbName to current Acb Name
        [YAXAttributeFor("Scope")]
        [YAXSerializeAs("value")]
        public byte Scope { get; set; }
        [YAXAttributeFor("TargetTrackNo")]
        [YAXSerializeAs("value")]
        public ushort TargetTrackNo { get; set; }

        //Commands
        [YAXAttributeFor("EventIndex")]
        [YAXSerializeAs("value")]
        public ushort EventIndex { get; set; }
        [YAXAttributeFor("CommandIndex")]
        [YAXSerializeAs("value")]
        public ushort CommandIndex { get; set; }

        //LocalAisac
        [YAXAttributeFor("LocalAisac")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<ushort> LocalAisac { get; set; }

        //GlobalAisac
        [YAXAttributeFor("GlobalAisacRefs")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<ushort> GlobalAisacRefs { get; set; } = new List<ushort>();

        public static List<ACB_Track> Load(UTF_File table, Version ParseVersion, string currentAcbName)
        {
            List<ACB_Track> rows = new List<ACB_Track>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, currentAcbName));
            }

            return rows;
        }

        public static ACB_Track Load(UTF_File trackTable, int index, string currentAcbName)
        {
            ACB_Track track = new ACB_Track();
            track.Index = index;

            //Track values
            track.ParameterPallet = trackTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index);
            track.TargetType = trackTable.GetValue<byte>("TargetType", TypeFlag.UInt8, index);
            track.TargetName = trackTable.GetValue<string>("TargetName", TypeFlag.String, index);
            track.TargetId = trackTable.GetValue<uint>("TargetId", TypeFlag.UInt32, index);
            track.TargetAcbName = trackTable.GetValue<string>("TargetAcbName", TypeFlag.String, index);
            track.Scope = trackTable.GetValue<byte>("Scope", TypeFlag.UInt8, index);
            track.TargetTrackNo = trackTable.GetValue<ushort>("TargetTrackNo", TypeFlag.UInt16, index);

            track.EventIndex = trackTable.GetValue<ushort>("EventIndex", TypeFlag.UInt16, index);
            track.CommandIndex = trackTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);
            track.LocalAisac = BigEndianConverter.ToUInt16Array(trackTable.GetData("LocalAisacs", index)).ToList();

            var globalAisacStartIndex = trackTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
            var globalAisacNumRefs = trackTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);
            
            for (int i = 0; i < globalAisacNumRefs; i++)
            {
                track.GlobalAisacRefs.Add((ushort)(globalAisacStartIndex + i));
            }

            if (currentAcbName == track.TargetAcbName)
                track.TargetSelf = true;

            return track;
        }

        public static void WriteToTable(IList<ACB_Track> entries, Version ParseVersion, UTF_File utfTable, string currentAcbName)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion, currentAcbName);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, string currentAcbName)
        {
            if (TargetSelf)
                TargetAcbName = currentAcbName;

            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0] : ushort.MaxValue;

            utfTable.AddValue("TargetType", TypeFlag.UInt8, index, TargetType.ToString());
            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString());
            utfTable.AddValue("TargetName", TypeFlag.String, index, TargetName);
            utfTable.AddValue("TargetId", TypeFlag.UInt32, index, TargetId.ToString());
            utfTable.AddValue("TargetAcbName", TypeFlag.String, index, TargetAcbName.ToString());
            utfTable.AddValue("Scope", TypeFlag.UInt8, index, Scope.ToString());
            utfTable.AddValue("TargetTrackNo", TypeFlag.UInt16, index, TargetTrackNo.ToString());

            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString());
            utfTable.AddValue("EventIndex", TypeFlag.UInt16, index, EventIndex.ToString());
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString());
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString());
            
            if (LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(LocalAisac.ToArray()));
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null);
            }
        }

    }

    [YAXSerializeAs("GlobalAisacReference")]
    public class ACB_GlobalAisacReference : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        public static List<ACB_GlobalAisacReference> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_GlobalAisacReference> rows = new List<ACB_GlobalAisacReference>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i));
            }

            return rows;
        }

        public static ACB_GlobalAisacReference Load(UTF_File globalAisacTable, int index)
        {
            ACB_GlobalAisacReference aisac = new ACB_GlobalAisacReference();
            aisac.Index = index;
            aisac.Name = globalAisacTable.GetValue<string>("Name", TypeFlag.String, index);

            return aisac;
        }

        public static void WriteToTable(IList<ACB_GlobalAisacReference> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            utfTable.AddValue("Name", TypeFlag.String, index, Name.ToString());
        }

    }

    [YAXSerializeAs("Waveform")]
    public class ACB_Waveform : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("EncodeType")]
        [YAXSerializeAs("value")]
        public EncodeType EncodeType { get; set; }
        [YAXAttributeFor("Streaming")]
        [YAXSerializeAs("value")]
        public bool Streaming { get; set; }
        [YAXAttributeFor("AwbId")]
        [YAXSerializeAs("value")]
        public ushort AwbId { get; set; } //Tracks will be stored in a seperate section
        [YAXAttributeFor("NumChannels")]
        [YAXSerializeAs("value")]
        public byte NumChannels { get; set; }
        [YAXAttributeFor("LoopFlag")]
        [YAXSerializeAs("value")]
        public bool LoopFlag { get; set; }
        [YAXAttributeFor("SamplingRate")]
        [YAXSerializeAs("value")]
        public ushort SamplingRate { get; set; }
        [YAXAttributeFor("NumSamples")]
        [YAXSerializeAs("value")]
        public uint NumSamples { get; set; }
        [YAXAttributeFor("StreamAwbPortNo")]
        [YAXSerializeAs("value")]
        public ushort StreamAwbPortNo { get; set; }

        //Extension Data (read from WaveformExtensionDataTable)
        [YAXAttributeFor("ExtensionData")]
        [YAXSerializeAs("LoopStart")]
        public uint LoopStart { get; set; }
        [YAXAttributeFor("ExtensionData")]
        [YAXSerializeAs("LoopEnd")]
        public uint LoopEnd { get; set; }

        public static List<ACB_Waveform> Load(UTF_File table, UTF_File waveformTable, Version ParseVersion)
        {
            List<ACB_Waveform> rows = new List<ACB_Waveform>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, waveformTable, ParseVersion));
            }

            return rows;
        }

        public static ACB_Waveform Load(UTF_File waveformTable, int index, UTF_File waveformExtensionTable, Version ParseVersion)
        {
            ACB_Waveform waveform = new ACB_Waveform();
            waveform.Index = index;

            waveform.EncodeType = (EncodeType)waveformTable.GetValue<byte>("EncodeType", TypeFlag.UInt8, index);
            waveform.Streaming = Convert.ToBoolean(waveformTable.GetValue<byte>("Streaming", TypeFlag.UInt8, index));
            waveform.NumChannels = waveformTable.GetValue<byte>("NumChannels", TypeFlag.UInt8, index);
            waveform.LoopFlag = Convert.ToBoolean(waveformTable.GetValue<byte>("LoopFlag", TypeFlag.UInt8, index));
            waveform.SamplingRate = waveformTable.GetValue<ushort>("SamplingRate", TypeFlag.UInt16, index);
            waveform.NumSamples = waveformTable.GetValue<uint>("NumSamples", TypeFlag.UInt32, index);

            if (ParseVersion >= ACB_File._1_27_2_0)
            {
                waveform.AwbId = (waveform.Streaming) ? waveformTable.GetValue<ushort>("StreamAwbId", TypeFlag.UInt16, index) : waveformTable.GetValue<ushort>("MemoryAwbId", TypeFlag.UInt16, index);
                waveform.StreamAwbPortNo = waveformTable.GetValue<ushort>("StreamAwbPortNo", TypeFlag.UInt16, index);
            }
            else
            {
                //Older ACB ver. Just has a "Id" column.
                waveform.AwbId = waveformTable.GetValue<ushort>("Id", TypeFlag.UInt16, index);
            }

            if (ParseVersion >= ACB_File._1_22_4_0)
            {
                ushort extensionIndex = waveformTable.GetValue<ushort>("ExtensionData", TypeFlag.UInt16, index);

                if(extensionIndex != ushort.MaxValue)
                {
                    waveform.LoopFlag = true;
                    waveform.LoopStart = waveformExtensionTable.GetValue<uint>("LoopStart", TypeFlag.UInt32, extensionIndex);
                    waveform.LoopEnd = waveformExtensionTable.GetValue<uint>("LoopEnd", TypeFlag.UInt32, extensionIndex);
                }
                else
                {
                    waveform.LoopFlag = false;
                }
            }
            else
            {
                //Older ACB ver where ExtensionData is of type Data.
                //I haven't found any instances where this is not null, so its structure is unknown... assume 2 x uint16
                ushort[] loopData = BigEndianConverter.ToUInt16Array(waveformTable.GetData("ExtensionData", index));

                if (loopData.Length == 2)
                {
                    waveform.LoopFlag = true;
                    waveform.LoopStart = loopData[0];
                    waveform.LoopEnd = loopData[1];
                }
                else
                {
                    waveform.LoopFlag = false;
                }
            }

            return waveform;
        }

        public static void WriteToTable(IList<ACB_Waveform> entries, Version ParseVersion, UTF_File utfTable, UTF_File waveformExtensionTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion, waveformExtensionTable);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, UTF_File waveformExtensionTable)
        {
            utfTable.AddValue("EncodeType", TypeFlag.UInt8, index, ((byte)EncodeType).ToString());
            utfTable.AddValue("Streaming", TypeFlag.UInt8, index, Convert.ToByte(Streaming).ToString());
            utfTable.AddValue("NumChannels", TypeFlag.UInt8, index, NumChannels.ToString());
            utfTable.AddValue("LoopFlag", TypeFlag.UInt8, index, Convert.ToByte(LoopFlag).ToString());
            utfTable.AddValue("SamplingRate", TypeFlag.UInt16, index, SamplingRate.ToString());
            utfTable.AddValue("NumSamples", TypeFlag.UInt32, index, NumSamples.ToString());

            if (ParseVersion >= ACB_File._1_27_2_0)
            {
                if (Streaming)
                {
                    utfTable.AddValue("StreamAwbId", TypeFlag.UInt16, index, AwbId.ToString());
                    utfTable.AddValue("MemoryAwbId", TypeFlag.UInt16, index, ushort.MaxValue.ToString());

                    if(StreamAwbPortNo != ushort.MaxValue)
                        utfTable.AddValue("StreamAwbPortNo", TypeFlag.UInt16, index, StreamAwbPortNo.ToString());
                    else
                        utfTable.AddValue("StreamAwbPortNo", TypeFlag.UInt16, index, "0");
                }
                else
                {
                    utfTable.AddValue("StreamAwbId", TypeFlag.UInt16, index, ushort.MaxValue.ToString());
                    utfTable.AddValue("MemoryAwbId", TypeFlag.UInt16, index, AwbId.ToString());
                    utfTable.AddValue("StreamAwbPortNo", TypeFlag.UInt16, index, ushort.MaxValue.ToString());
                }
            }
            else
            {
                //Older ACB ver. Just has a "Id" column.
                utfTable.AddValue("Id", TypeFlag.UInt16, index, AwbId.ToString());
            }

            //ExtensionData
            ushort extensionIndex = ushort.MaxValue;
            byte[] loopBytes = null;

            if (LoopFlag)
            {
                if (ParseVersion >= ACB_File._1_22_4_0)
                {
                    int newIdx = waveformExtensionTable.RowCount();
                    waveformExtensionTable.AddValue("LoopStart", TypeFlag.UInt32, newIdx, LoopStart.ToString());
                    waveformExtensionTable.AddValue("LoopEnd", TypeFlag.UInt32, newIdx, LoopEnd.ToString());
                    extensionIndex = (ushort)newIdx;
                }
                else
                {
                    loopBytes = BigEndianConverter.GetBytes(new ushort[2] { (ushort)LoopStart, (ushort)LoopEnd });
                }
            }

            if (ParseVersion >= ACB_File._1_22_4_0)
            {
                utfTable.AddValue("ExtensionData", TypeFlag.UInt16, index, extensionIndex.ToString());
            }
            else
            {
                utfTable.AddData("ExtensionData", index, loopBytes);
            }

        }

    }

    [YAXSerializeAs("Aisac")]
    public class ACB_Aisac : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Id")]
        [YAXSerializeAs("value")]
        public short Id { get; set; }
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte Type { get; set; }
        [YAXAttributeFor("ControlId")]
        [YAXSerializeAs("value")]
        public ushort ControlId { get; set; }
        [YAXAttributeFor("ControlName")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string ControlName { get; set; } //Optional name
        [YAXAttributeFor("RandomRange")]
        [YAXSerializeAs("value")]
        public float RandomRange { get; set; }
        [YAXAttributeFor("DefaultControlFlag")]
        [YAXSerializeAs("value")]
        public byte DefaultControlFlag { get; set; }
        [YAXAttributeFor("DefaultControl")]
        [YAXSerializeAs("value")]
        public float DefaultControl { get; set; }
        [YAXAttributeFor("GraphBitFlag")]
        [YAXSerializeAs("value")]
        public byte GraphBitFlag { get; set; }

        
        [YAXAttributeFor("AutoModulationIndex")]
        [YAXSerializeAs("value")]
        public ushort AutoModulationIndex { get; set; }
        
        [YAXAttributeFor("GraphIndexes")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<ushort> GraphIndexes { get; set; }

        public static List<ACB_Aisac> Load(UTF_File table, UTF_File aisacNameTable, Version ParseVersion)
        {
            List<ACB_Aisac> rows = new List<ACB_Aisac>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, aisacNameTable));
            }

            return rows;
        }

        public static ACB_Aisac Load(UTF_File aisacTable, int index, UTF_File aisacNameTable)
        {
            ACB_Aisac aisac = new ACB_Aisac();
            aisac.Index = index;
            aisac.Id = aisacTable.GetValue<short>("Id", TypeFlag.Int16, index);
            aisac.Type = aisacTable.GetValue<byte>("Type", TypeFlag.UInt8, index);
            aisac.ControlId = aisacTable.GetValue<ushort>("ControlId", TypeFlag.UInt16, index);
            aisac.RandomRange = aisacTable.GetValue<float>("RandomRange", TypeFlag.Single, index);
            aisac.DefaultControlFlag = aisacTable.GetValue<byte>("DefaultControlFlag", TypeFlag.UInt8, index);
            aisac.DefaultControl = aisacTable.GetValue<float>("DefaultControl", TypeFlag.Single, index);
            aisac.GraphBitFlag = aisacTable.GetValue<byte>("GraphBitFlag", TypeFlag.UInt8, index);
            aisac.AutoModulationIndex = aisacTable.GetValue<ushort>("AutoModulationIndex", TypeFlag.UInt16, index);
            aisac.GraphIndexes = BigEndianConverter.ToUInt16Array(aisacTable.GetData("GraphIndexes", index)).ToList();

            //Get ControlName if it is defined
            if(aisacNameTable != null)
            {
                int nameIdx = aisacNameTable.IndexOfRow("AisacControlId", aisac.ControlId.ToString());

                if(nameIdx != -1)
                {
                    aisac.ControlName = aisacNameTable.GetValue<string>("AisacControlName", TypeFlag.String, nameIdx);
                }
            }

            return aisac;
        }

        public static void WriteToTable(IList<ACB_Aisac> entries, Version ParseVersion, UTF_File utfTable, UTF_File nameTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion, nameTable);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, UTF_File nameTable)
        {
            utfTable.AddValue("Id", TypeFlag.Int16, index, Id.ToString());
            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString());
            utfTable.AddValue("ControlId", TypeFlag.UInt16, index, ControlId.ToString());
            utfTable.AddValue("RandomRange", TypeFlag.Single, index, RandomRange.ToString());
            utfTable.AddValue("DefaultControlFlag", TypeFlag.UInt8, index, DefaultControlFlag.ToString());
            utfTable.AddValue("DefaultControl", TypeFlag.Single, index, DefaultControl.ToString());
            utfTable.AddValue("GraphBitFlag", TypeFlag.UInt8, index, GraphBitFlag.ToString());
            utfTable.AddValue("AutoModulationIndex", TypeFlag.UInt16, index, AutoModulationIndex.ToString());

            if (GraphIndexes != null)
            {
                utfTable.AddData("GraphIndexes", index, BigEndianConverter.GetBytes(GraphIndexes.ToArray()));
            }
            else
            {
                utfTable.AddData("GraphIndexes", index, null);
            }

            if (!string.IsNullOrWhiteSpace(ControlName))
            {
                int newRow = nameTable.RowCount();
                nameTable.AddValue("AisacControlId", TypeFlag.UInt16, newRow, ControlId.ToString());
                nameTable.AddValue("AisacControlName", TypeFlag.String, newRow, ControlName);
            }
        }

    }

    [YAXSerializeAs("Graph")]
    public class ACB_Graph : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public ushort Type { get; set; }
        [YAXAttributeFor("Controls")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] Controls { get; set; }
        [YAXAttributeFor("Destinations")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] Destinations { get; set; }
        [YAXAttributeFor("Curve")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] Curve { get; set; }
        [YAXAttributeFor("ControlWorkArea")]
        [YAXSerializeAs("value")]
        public float ControlWorkArea { get; set; }
        [YAXAttributeFor("DestinationWorkArea")]
        [YAXSerializeAs("value")]
        public float DestinationWorkArea { get; set; }

        public static List<ACB_Graph> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_Graph> rows = new List<ACB_Graph>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i));
            }

            return rows;
        }

        public static ACB_Graph Load(UTF_File graphTable, int index)
        {
            ACB_Graph graph = new ACB_Graph();
            graph.Index = index;
            graph.Type = graphTable.GetValue<ushort>("Type", TypeFlag.UInt16, index);
            graph.Controls = graphTable.GetData("Controls", index);
            graph.Destinations = graphTable.GetData("Destinations", index);
            graph.Curve = graphTable.GetData("Curve", index);
            graph.ControlWorkArea = graphTable.GetValue<float>("ControlWorkArea", TypeFlag.Single, index);
            graph.DestinationWorkArea = graphTable.GetValue<float>("DestinationWorkArea", TypeFlag.Single, index);

            return graph;
        }

        public static void WriteToTable(IList<ACB_Graph> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            utfTable.AddValue("Type", TypeFlag.UInt16, index, Type.ToString());
            utfTable.AddData("Controls", index, Controls);
            utfTable.AddData("Destinations", index, Destinations);
            utfTable.AddData("Curve", index, Curve);
            utfTable.AddValue("ControlWorkArea", TypeFlag.Single, index, ControlWorkArea.ToString());
            utfTable.AddValue("DestinationWorkArea", TypeFlag.Single, index, DestinationWorkArea.ToString());
        }

    }

    [YAXSerializeAs("AutoModulation")]
    public class ACB_AutoModulation : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public byte Type { get; set; }
        [YAXAttributeForClass]
        public byte TriggerType { get; set; }
        [YAXAttributeForClass]
        public uint Time { get; set; }
        [YAXAttributeForClass]
        public uint Key { get; set; }

        public static List<ACB_AutoModulation> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_AutoModulation> rows = new List<ACB_AutoModulation>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i));
            }

            return rows;
        }

        public static ACB_AutoModulation Load(UTF_File autoModTable, int index)
        {
            ACB_AutoModulation autoMod = new ACB_AutoModulation();
            autoMod.Index = index;
            autoMod.Key = autoModTable.GetValue<uint>("Key", TypeFlag.UInt32, index);
            autoMod.Time = autoModTable.GetValue<uint>("Time", TypeFlag.UInt32, index);
            autoMod.Type = autoModTable.GetValue<byte>("Type", TypeFlag.UInt8, index);
            autoMod.TriggerType = autoModTable.GetValue<byte>("TriggerType", TypeFlag.UInt8, index);

            return autoMod;
        }

        public static void WriteToTable(IList<ACB_AutoModulation> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString());
            utfTable.AddValue("TriggerType", TypeFlag.UInt8, index, TriggerType.ToString());
            utfTable.AddValue("Time", TypeFlag.UInt32, index, Time.ToString());
            utfTable.AddValue("Key", TypeFlag.UInt32, index, Key.ToString());
        }
    }

    #region Commands
    public class ACB_CommandTables
    {
        public ACB_File Root = null;

        [YAXComment("Below ver 1.29.0.0: Always use SequenceCommand (just named CommandTable in the ACB file). Later versions separated the command tables.")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CommandTable")]
        public List<ACB_CommandTable> CommandTables { get; set; } = new List<ACB_CommandTable>();

        //todo: manager methods
        public int AddCommand(ACB_CommandGroup command, CommandTableType type)
        {
            var table = GetCommandTable(type);
            table.CommandGroups.Add(command);
            return table.CommandGroups.Count - 1;
        }

        public void SetCommand(int index, CommandTableType type, ACB_CommandGroup command)
        {
            var table = GetCommandTable(type);

            if (index >= table.CommandGroups.Count)
                throw new IndexOutOfRangeException(string.Format("ACB_CommandTables.SetCommand: cannot set command at index {0} on {1} because it is out of range.", index, type));

            table.CommandGroups[index] = command;
        }

        public ACB_CommandGroup GetCommand(int index, CommandTableType type)
        {
            var table = GetCommandTable(type);

            if (index >= table.CommandGroups.Count)
                throw new IndexOutOfRangeException(string.Format("ACB_CommandTables.GetCommand: cannot get command at index {0} on {1} because it is out of range.", index, type));

            return table.CommandGroups[index];
        }

        public ACB_CommandTable GetCommandTable(CommandTableType type)
        {
            //If on old ACB version, always return SequenceCommandTable
            if(Root.Version < ACB_File._1_29_0_0)
                type = CommandTableType.SequenceCommand;

            //Find table, create it if needed, and return it
            var table = CommandTables.FirstOrDefault(p => p.Type == type);
            if(table == null)
            {
                table = new ACB_CommandTable() { Type = type };
                CommandTables.Add(table);
            }
            return table;
        }

        public int GetCommandIndex(ACB_CommandGroup command, CommandTableType type)
        {
            var table = GetCommandTable(type);
            return table.CommandGroups.IndexOf(command);
        }

        public static ACB_CommandTables Load(UTF_File utfFile, Version ParseVersion, ACB_File acbFile)
        {
            ACB_CommandTables commandTables = new ACB_CommandTables();
            commandTables.Root = acbFile;

            if(ParseVersion >= ACB_File._1_29_0_0)
            {
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("SeqCommandTable", true), CommandTableType.SequenceCommand));
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("TrackCommandTable", true), CommandTableType.TrackCommand));
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("SynthCommandTable", true), CommandTableType.SynthCommand));
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("TrackEventTable", true), CommandTableType.TrackEvent));
            }
            else
            {
                //Just CommandTable
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("CommandTable", true), CommandTableType.SequenceCommand));
            }

            return commandTables;
        }

        public UTF_File WriteToTable(CommandTableType type)
        {
            foreach(var table in CommandTables)
            {
                if (table.Type == type) return table.WriteToTable();
            }

            return ACB_CommandTable.CreateEmptyTable(Root.Version, type);
        }
    }

    public class ACB_CommandTable
    {
        public ACB_File Root = null; //Will have to set this manually after loading from XML

        [YAXAttributeForClass]
        public CommandTableType Type { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CommandGroup")]
        public List<ACB_CommandGroup> CommandGroups { get; set; } = new List<ACB_CommandGroup>();

        public static ACB_CommandTable Load(UTF_File commandTable, CommandTableType type)
        {
            ACB_CommandTable table = new ACB_CommandTable();
            table.Type = type;
            if (commandTable == null) return table;

            int numRows = commandTable.DefaultRowCount;

            for(int i = 0; i < numRows; i++)
            {
                table.CommandGroups.Add(ACB_CommandGroup.Load(commandTable, i));
            }

            return table;
        }

        public UTF_File WriteToTable()
        {
            UTF_File utfTable = CreateEmptyTable(Root.Version, Type);

            for(int i = 0; i < CommandGroups.Count; i++)
            {
                utfTable.AddData("Command", i, CommandGroups[i].GetBytes());
            }

            return utfTable;
        }

        public static UTF_File CreateEmptyTable(Version acbVersion, CommandTableType type)
        {
            UTF_File utfTable = new UTF_File();
            utfTable.EncodingType = _EncodingType.UTF8;
            utfTable.Columns.Add(new UTF_Column("Command", TypeFlag.Data));

            if (acbVersion >= ACB_File._1_29_0_0)
            {
                switch (type)
                {
                    case CommandTableType.SequenceCommand:
                        utfTable.TableName = "SequenceCommand";
                        break;
                    case CommandTableType.SynthCommand:
                        utfTable.TableName = "SynthCommand";
                        break;
                    case CommandTableType.TrackCommand:
                        utfTable.TableName = "TrackCommand";
                        break;
                    case CommandTableType.TrackEvent:
                        utfTable.TableName = "TrackEvent";
                        break;
                }
            }
            else
            {
                utfTable.TableName = "Command";
            }

            return utfTable;
        }
        
    }

    [YAXSerializeAs("CommandGroup")]
    public class ACB_CommandGroup : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        public ObservableCollection<ACB_Command> Commands { get; set; } = new ObservableCollection<ACB_Command>();

        public static ACB_CommandGroup Load(UTF_File commandTable, int index)
        {
            ACB_CommandGroup commandGroup = new ACB_CommandGroup();
            commandGroup.Index = index;

            byte[] commandBytes = commandTable.GetData("Command", index);

            //Parse command bytes. If there are 3 or more bytes left, then there is another command to parse.
            int offset = 0;

            while (((commandBytes.Length) - offset) >= 3)
            {
                ACB_Command command = new ACB_Command();
                command.I_00 = commandBytes[offset + 0];
                command.CommandType = (CommandType)commandBytes[offset + 1];
                command.Parameters = commandBytes.GetRange(offset + 3, commandBytes[offset + 2]).ToList();
                commandGroup.Commands.Add(command);

                offset += 3 + command.NumParameters;
            }

            return commandGroup;
        }

        public static byte[] GetBytes(ACB_CommandGroup commands)
        {
            List<byte> bytes = new List<byte>();

            foreach (var command in commands.Commands)
            {
                bytes.Add(command.I_00);
                bytes.Add((byte)command.CommandType);
                bytes.Add(command.NumParameters);

                if (command.Parameters != null)
                {
                    bytes.AddRange(command.Parameters);
                }
            }

            return bytes.ToArray();
        }
        
        public byte[] GetBytes()
        {
            return GetBytes(this);
        }
    }

    [YAXSerializeAs("Command")]
    public class ACB_Command : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        
        [YAXDontSerialize]
        public string DisplayName { get { return "Entry"; } }

        [YAXAttributeForClass]
        public byte I_00 { get; set; }
        [YAXAttributeForClass]
        public CommandType CommandType
        {
            get
            {
                return this._commandType;
            }

            set
            {
                if (value != this._commandType)
                {
                    this._commandType = value;
                    NotifyPropertyChanged("CommandType");
                }
            }
        }
        private CommandType _commandType = 0;

        [YAXDontSerialize]
        public byte NumParameters { get { return (byte)Parameters.Count; } }

        //Data, depending on CommandType
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        public List<byte> Parameters { get; set; } = new List<byte>(); 
        
        #region ParameterProperties
        [YAXDontSerialize]
        public ushort Param1 { get { return GetParam(0); } set { SetParam(0, value); } }
        [YAXDontSerialize]
        public ushort Param2 { get { return GetParam(1); } set { SetParam(1, value); } }
        [YAXDontSerialize]
        public ushort Param3 { get { return GetParam(2); } set { SetParam(2, value); } }
        [YAXDontSerialize]
        public ushort Param4 { get { return GetParam(3); } set { SetParam(3, value); } }


        private ushort GetParam(int idx)
        {
            if (Parameters == null) Parameters = new List<byte>();
            int adjustedIndex = idx * 2;

            //Expand Parameters if needed
            while (Parameters.Count - 1 < adjustedIndex + 2)
            {
                Parameters.Add(0);
            }

            return BigEndianConverter.ReadUInt16(Parameters, adjustedIndex);
        }

        private void SetParam(int idx, ushort value)
        {
            if (Parameters == null) Parameters = new List<byte>();
            int adjustedIndex = idx * 2;

            //Expand Parameters if needed
            while (Parameters.Count - 1 < adjustedIndex + 2)
            {
                Parameters.Add(0);
            }

            Utils.ReplaceRange(Parameters, BigEndianConverter.GetBytes(value), adjustedIndex);
        }
        #endregion
        
    }

    #endregion

    //Enums
    public enum ReferenceType : byte
    {
        Waveform = 1,
        Synth = 2,
        Sequence = 3,
        BlockSequence = 8,
        Nothing = 255
    }

    public enum EncodeType : byte
    {
        ADX = 0,
        HCA = 2,
        HCA_ALT = 6,
        VAG = 7,
        ATRAC3 = 8,
        BCWAV = 9,
        ATRAC9 = 11,
        NINTENDO_DSP = 13,
        None = 255
    }

    public enum CommandType
    {
        Null = 0, //No parameters. Does nothing.
        Action_Play = 188,//No parameters. Uses target information from ActionTrack.
        Action_Stop = 189,//No parameters. Uses target information from ActionTrack.
        ReferenceItem = 208, //4 parameters (2 uint16s). Identical to ReferenceType/ReferenceIndex on Cue and ReferenceItems on Sequence/Synth.
        Unk209 = 209, //4 parameters (2 uint16s). First value could be ReferenceType, but 2nd value cant be ReferenceIndex (too high)
    }

    public enum SequenceType : byte
    {
        Polyphonic = 0,
        Sequential = 1,
        Shuffle = 2,
        Random = 3,
        RandomNoRepeat = 4,
        Switch = 5,
        ComboSequential = 6
    }
    
    public enum CommandTableType
    {
        SequenceCommand, //In older ACB versions this is the only command table and was used for everything (was just called "CommandTable")
        SynthCommand,
        TrackCommand,
        TrackEvent
    }

    public class AcbTableBase
    {
        /// <summary>
        /// The ID for this instance. Used in copying operations.
        /// </summary>
        public Guid InstanceGuid = Guid.NewGuid();
    }
}
