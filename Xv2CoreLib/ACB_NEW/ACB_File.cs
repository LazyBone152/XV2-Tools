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
using Xv2CoreLib.Resource.UndoRedo;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Collections;
using Xv2CoreLib.HCA;
using System.Windows;
using Xv2CoreLib.CPK;

namespace Xv2CoreLib.ACB_NEW
{
    public enum SaveFormat
    {
        Default,
        MusicPackage //Same as Default, except different extension (.musicpackage) and no external awb (all streaming tracks are stored internally). Used by LB Mod Installer for installing music/CssVoices.
    }

    public enum MusicPackageType : int
    {
        BGM_NewOption = 0,
        BGM_Direct = 1,
        CSS_Voice = 2
    }


    [YAXSerializeAs("ACB")]
    [Serializable]
    public class ACB_File
    {
        #region Constants
        public const string MUSIC_PACKAGE_EXTENSION = ".musicpackage";

        //Clipboard Constants
        public const string CLIPBOARD_ACB_CUES = "ACB_CUES";
        public const string CLIPBOARD_ACB_TRACK = "ACB_TRACK";
        public const string CLIPBOARD_ACB_ACTION = "ACB_ACTION";

        //Global Aisacs
        public const string GLOBAL_AISAC_3DVOL_DEF = "3Dvol_def";

        //Known ACB Versions
        public readonly static Version _1_30_0_0 = new Version("1.30.0.0"); //SDBH
        public readonly static Version _1_29_2_0 = new Version("1.29.2.0"); //SDBH
        public readonly static Version _1_29_0_0 = new Version("1.29.0.0"); //SDBH
        public readonly static Version _1_28_2_0 = new Version("1.28.2.0"); //SDBH, identical to 1.27.7.0
        public readonly static Version _1_27_7_0 = new Version("1.27.7.0"); //Used in XV2
        public readonly static Version _1_27_2_0 = new Version("1.27.2.0"); //Used in XV2
        public readonly static Version _1_23_1_0 = new Version("1.23.1.0"); //Unknown game
        public readonly static Version _1_22_4_0 = new Version("1.22.4.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public readonly static Version _1_22_0_0 = new Version("1.22.0.0"); //Not observed, just a guess
        public readonly static Version _1_21_1_0 = new Version("1.21.1.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public readonly static Version _1_16_0_0 = new Version("1.16.0.0");
        public readonly static Version _1_14_0_0 = new Version("1.14.0.0");
        public readonly static Version _1_12_0_0 = new Version("1.12.0.0");
        public readonly static Version _1_7_0_0 = new Version("1.7.0.0");
        public readonly static Version _1_6_0_0 = new Version("1.6.0.0"); //Used in 1 file in XV1. Missing a lot of data.
        public readonly static Version _1_3_0_0 = new Version("1.3.0.0");
        public readonly static Version _1_0_0_0 = new Version("1.0.0.0");
        public readonly static Version _0_81_8_0 = new Version("0.81.8.0"); //Used in MGS3 3D. First acb version that has BlockSequenceTable and BlockTable.
        public readonly static Version _0_81_1_0 = new Version("0.81.1.0"); //Used in MGS3 3D. Uses CPK AWB files instead of AFS2.

        //As of the DBXV2 1.16 update, any ACBs of this version or greater are silent without a "VolumeBus" command on the Sequence/Synth.
        public readonly static Version VolumeBusRequiredVersion = new Version("1.24.0.0");
        #endregion

        [YAXDontSerialize]
        public string VersionToolTip
        {
            get
            {
                string tooltip = "This is the version of the ACB file. Depending on the version, some features may not be available.\n";

                if (Version > _1_30_0_0)
                {
                    tooltip += "\n-This is a newer, unknown ACB version. Compatibility is not guaranteed.";
                }

                if (Version >= _1_16_0_0 && Version <= _1_30_0_0)
                {
                    tooltip += "\n-This version should be very stable and support most/all features.";
                }

                if (Version > _1_6_0_0 && Version < _1_16_0_0)
                {
                    tooltip += "\n-This version is mostly known. Some features are not supported.";
                }

                if (Version <= _1_6_0_0 && Version >= _1_0_0_0)
                {
                    tooltip += "\n-This is an older version and may have issues.";
                }

                if (Version < _1_0_0_0)
                {
                    tooltip += "\n-This is quite an ACB old version. Compatibility is not guaranteed.";
                }

                //list unsupported features
                if (!AcbFormatHelper.Instance.IsActionsEnabled(Version))
                {
                    tooltip += "\n-This version does not support Actions.";
                }

                if (!AcbFormatHelper.Instance.IsSequenceTypeEnabled(Version))
                {
                    tooltip += "\n-This version does not support editing sequence data (random weights and type).";
                }

                return tooltip;
            }
        }


        [YAXDontSerialize]
        public bool TableValidationFailed { get; private set; }
        [YAXDontSerialize]
        public bool ExternalAwbError { get; private set; }

        #region SaveSettings
        [YAXDontSerialize]
        public bool CleanTablesOnSave = true;
        [YAXDontSerialize]
        public SaveFormat SaveFormat { get; set; } = SaveFormat.Default;
        [YAXDontSerialize]
        public MusicPackageType MusicPackageType { get; set; } = MusicPackageType.BGM_NewOption;
        /// <summary>
        /// Save the ACB in a way that Eternity Audio Tool and associated tools can still load it, ommiting some features.
        /// </summary>
        [YAXDontSerialize]
        public bool EternityCompatibility = true;
        #endregion


        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }

        [YAXDontSerialize]
        public Version Version = null; //If manually changed, then call SetCommandTableVersion().
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
        public float AcbVolume { get; set; } = 1.0f;
        [YAXAttributeFor("GUID")]
        [YAXSerializeAs("value")]
        public Guid GUID { get; set; } = Guid.NewGuid();

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
        [YAXAttributeFor("CharacterEncodingType")]
        [YAXSerializeAs("value")]
        public byte CharacterEncodingType { get; set; }
        [YAXAttributeFor("CuePriorityType")]
        [YAXSerializeAs("value")]
        public byte CuePriorityType { get; set; } = 255;
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
        public List<ACB_StringValue> StringValues { get; set; } = new List<ACB_StringValue>();
        public List<ACB_SequenceBlock> BlockSequences { get; set; } = new List<ACB_SequenceBlock>();
        public List<ACB_Block> Blocks { get; set; } = new List<ACB_Block>();


        /// <summary>
        /// Used to store the CPK Header when AWB is CPK format, for saving back to CPK. This is needed as currently CPKs cannot be created from scratch. 
        /// </summary>
        [YAXDontSerializeIfNull]
        public UTF_File CpkHeader { get; set; }
        public bool IsAwbCpk { get; set; } = false;
        /// <summary>
        /// When true, the external AWB file is saved with "_streamfiles.awb" at the end. This is how older AWBs are named.
        /// </summary>
        public bool IsUsingAltAwbPath { get; set; } = false;
        public AFS2_File AudioTracks { get; set; } = AFS2_File.CreateNewAwbFile();
        
        public UTF_File OutsideLinkTable { get; set; } // No need to parse this
        public UTF_File AcfReferenceTable { get; set; } // No need to parse this
        public UTF_File SoundGeneratorTable { get; set; } // No need to parse this

        #region Settings
        public bool ReuseTrackCommand = false;
        public bool ReuseSequenceCommand = false;
        public bool AllowSharedAwbEntries = true;

        #endregion

        #region LoadFunctions
        public static ACB_File Load(string path, bool writeXml = false)
        {
            //Check for external awb file
            byte[] awbFile = null;
            string awbPath = string.Format("{0}/{1}.awb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            string altAwbPath = string.Format("{0}/{1}_streamfiles.awb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            bool altPath = false;
            bool isMusicPackage = Path.GetExtension(path).ToLower() == MUSIC_PACKAGE_EXTENSION;

            if (File.Exists(awbPath) && !isMusicPackage)
            {
                awbFile = File.ReadAllBytes(awbPath);
            }
            else if (File.Exists(altAwbPath) && !isMusicPackage)
            {
                awbFile = File.ReadAllBytes(altAwbPath);
                altPath = true;
            }


            var file = Load(File.ReadAllBytes(path), awbFile, writeXml, isMusicPackage, altPath);

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ACB_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static ACB_File Load(byte[] acbBytes, byte[] awbBytes, bool loadUnknownCommands = false, bool isMusicPackage = false, bool altAwbPath = false)
        {
            return Load(UTF_File.LoadUtfTable(acbBytes, acbBytes.Length), awbBytes, loadUnknownCommands, isMusicPackage, true, altAwbPath);
        }

        public static ACB_File Load(UTF_File utfFile, byte[] awbBytes, bool loadUnknownCommands = false, bool isMusicPackage = false, bool eternityCompatibility = true, bool altAwbPath = false)
        {
            if (!AcbFormatHelper.Instance.AcbFormatHelperMain.ForceLoad)
            {
                //Analyze the ACB version. This maps out what columns and tables exist in this ACB version, so that they can be read and saved properly.
                AcbFormatHelper.Instance.ParseFile(utfFile);
            }

            AcbFormatHelperTable header = AcbFormatHelper.Instance.AcbFormatHelperMain.Header;

            //Parse acb
            ACB_File acbFile = new ACB_File();
            acbFile.IsUsingAltAwbPath = altAwbPath;
            acbFile.EternityCompatibility = eternityCompatibility;
            acbFile.ValidateTables(utfFile);

            if (!utfFile.ColumnExists("Version")) throw new InvalidDataException($"Could not find the \"Version\" column. Load failed.");
            acbFile.Version = BigEndianConverter.UIntToVersion(utfFile.GetValue<uint>("Version", TypeFlag.UInt32, 0), true);

            acbFile.AcfMd5Hash = utfFile.GetData("AcfMd5Hash", 0, header, acbFile.Version);
            acbFile.CategoryExtension = utfFile.GetValue<byte>("CategoryExtension", TypeFlag.UInt8, 0, header, acbFile.Version);
            acbFile.FileIdentifier = utfFile.GetValue<uint>("FileIdentifier", TypeFlag.UInt32, 0, header, acbFile.Version);
            acbFile.Size = utfFile.GetValue<uint>("Size", TypeFlag.UInt32, 0, header, acbFile.Version);
            acbFile.Target = utfFile.GetValue<byte>("Target", TypeFlag.UInt8, 0, header, acbFile.Version);
            acbFile.Type = utfFile.GetValue<byte>("Type", TypeFlag.UInt8, 0, header, acbFile.Version);
            acbFile.VersionString = utfFile.GetValue<string>("VersionString", TypeFlag.String, 0, header, acbFile.Version);
            acbFile.AcbVolume = utfFile.GetValue<float>("AcbVolume", TypeFlag.Single, 0, header, acbFile.Version);
            acbFile.StreamAwbTocWork = utfFile.GetData("StreamAwbTocWork", 0, header, acbFile.Version);
            acbFile.Name = utfFile.GetValue<string>("Name", TypeFlag.String, 0, header, acbFile.Version);
            acbFile.CharacterEncodingType = utfFile.GetValue<byte>("CharacterEncodingType", TypeFlag.UInt8, 0, header, acbFile.Version);
            acbFile.NumCueLimit = utfFile.GetValue<ushort>("NumCueLimit", TypeFlag.UInt16, 0, header, acbFile.Version);
            acbFile.CuePriorityType = utfFile.GetValue<byte>("CuePriorityType", TypeFlag.UInt8, 0, header, acbFile.Version);

            acbFile.GUID = new Guid(utfFile.GetData("AcbGuid", 0, header, acbFile.Version));
            acbFile.OutsideLinkTable = utfFile.GetColumnTable("OutsideLinkTable", true, header, acbFile.Version);

            //Tables
            acbFile.AcfReferenceTable = utfFile.GetColumnTable("AcfReferenceTable", true, header, acbFile.Version);
            acbFile.ActionTracks = ACB_Track.Load(utfFile.GetColumnTable("ActionTrackTable", false, header, acbFile.Version), acbFile.Version, acbFile.Name, true);
            acbFile.SoundGeneratorTable = utfFile.GetColumnTable("SoundGeneratorTable", true, header, acbFile.Version);
            acbFile.Cues = ACB_Cue.Load(utfFile.GetColumnTable("CueTable", true), utfFile.GetColumnTable("CueNameTable", true), acbFile.Version);
            acbFile.Synths = ACB_Synth.Load(utfFile.GetColumnTable("SynthTable", false), acbFile.Version);
            acbFile.Sequences = ACB_Sequence.Load(utfFile.GetColumnTable("SequenceTable", false), acbFile.Version);
            acbFile.Tracks = ACB_Track.Load(utfFile.GetColumnTable("TrackTable", false), acbFile.Version, acbFile.Name, false);
            acbFile.GlobalAisacReferences = ACB_GlobalAisacReference.Load(utfFile.GetColumnTable("GlobalAisacReferenceTable", false, header, acbFile.Version), acbFile.Version);
            acbFile.Waveforms = ACB_Waveform.Load(utfFile.GetColumnTable("WaveformTable", false), utfFile.GetColumnTable("WaveformExtensionDataTable", false, header, acbFile.Version), acbFile.Version);
            acbFile.Aisacs = ACB_Aisac.Load(utfFile.GetColumnTable("AisacTable", false, header, acbFile.Version), utfFile.GetColumnTable("AisacControlNameTable", false), acbFile.Version);
            acbFile.Graphs = ACB_Graph.Load(utfFile.GetColumnTable("GraphTable", false, header, acbFile.Version), acbFile.Version);
            acbFile.AutoModulations = ACB_AutoModulation.Load(utfFile.GetColumnTable("AutoModulationTable", false, header, acbFile.Version), acbFile.Version);
            acbFile.CommandTables = ACB_CommandTables.Load(utfFile, acbFile, loadUnknownCommands);
            acbFile.StringValues = ACB_StringValue.Load(utfFile.GetColumnTable("StringValueTable", false, header, acbFile.Version), acbFile.Version);

            //If no StringValueTable exists, then use default table. (this is required for some commands)
            if (acbFile.StringValues.Count == 0)
                acbFile.StringValues = ACB_StringValue.DefaultStringTable();

            //AWB
            //Check for both a CPK or AFS2 based AWB
            AWB_CPK internalCpkAwb = utfFile.GetColumnCpkFile("AwbFile");
            AFS2_File internalAfs2Awb = utfFile.GetColumnAfs2File("AwbFile");

            if(internalCpkAwb != null)
            {
                acbFile.IsAwbCpk = true;
                acbFile.LoadAwbFile(internalCpkAwb, false);
            }
            else
            {
                acbFile.LoadAwbFile(internalAfs2Awb, false);
            }

            if(awbBytes != null)
            {
                if(BitConverter.ToInt32(awbBytes, 0) == AFS2_File.AFS2_SIGNATURE)
                {
                    acbFile.LoadAwbFile(AFS2_File.LoadAfs2File(awbBytes), true);
                }
                else if (BitConverter.ToInt32(awbBytes, 0) == AWB_CPK.CPK_SIGNATURE)
                {
                    acbFile.IsAwbCpk = true;
                    acbFile.LoadAwbFile(AWB_CPK.Load(awbBytes), true);
                }
            }

            if (awbBytes == null && acbFile.Waveforms.Any(x => x.Streaming) && !isMusicPackage)
                acbFile.ExternalAwbError = true;

            //MusicPackage values
            if (utfFile.ColumnExists("MusicPackageType"))
            {
                acbFile.MusicPackageType = (MusicPackageType)utfFile.GetValue<int>("MusicPackageType", TypeFlag.Int32, 0);
            }

            //Finalization
            acbFile.SetCommandTableVersion();
            acbFile.LinkTableGuids();
            acbFile.SaveFormat = (isMusicPackage) ? SaveFormat.MusicPackage : SaveFormat.Default;

            return acbFile;
        }
        
        public static ACB_File LoadXml(string path, bool saveBinary = false)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ACB_File), YAXSerializationOptions.DontSerializeNullObjects);
            ACB_File acbFile = (ACB_File)serializer.DeserializeFromFile(path);
            acbFile.SetCommandTableVersion();
            acbFile.InitializeValues();

            if (saveBinary)
            {
                string savePath = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)));
                acbFile.Save(savePath, false); //No full rebuild for XML - keep the indexes and tables completely unchanged
            }

            return acbFile;
        }

        public static ACB_File NewXv2Acb()
        {
            ACB_File acbFile = new ACB_File();
            acbFile.Version = _1_27_7_0;
            //acbFile.Version = _1_22_4_0;
            //acbFile.Version = new Version("1.24.1.0");
            acbFile.CommandTables = new ACB_CommandTables();
            acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.SequenceCommand } };
            acbFile.SetCommandTableVersion();
            acbFile.VersionString = @"&#xA;ACB Format/PC Ver.1.27.07 Build:&#xA;";
            acbFile.StringValues = ACB_StringValue.DefaultStringTable();
            acbFile.AcfMd5Hash = new byte[] { 103, 60, 108, 22, 196, 140, 254, 73, 5, 182, 132, 96, 160, 88, 222, 176 };
            return acbFile;
        }

        public static ACB_File NewAcb(Version version, byte[] acfHash = null)
        {
            ACB_File acbFile = new ACB_File();
            acbFile.Version = version;
            acbFile.CommandTables = new ACB_CommandTables();
            
            if(AcbFormatHelper.Instance.AcbFormatHelperMain.Header.ColumnExists("CommandTable", TypeFlag.Data, version))
            {
                acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.SequenceCommand } };
            }
            else
            {
                //Newer CommandTable configuration
                acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.SequenceCommand } };
                acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.SynthCommand } };
                acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.TrackCommand } };
                acbFile.CommandTables.CommandTables = new List<ACB_CommandTable>() { new ACB_CommandTable() { Type = CommandTableType.TrackEvent } };
            }

            acbFile.SetCommandTableVersion();
            acbFile.VersionString = $@"&#xA;ACB Format/PC Ver.{version.Major}.{version.Minor}.{version.Build.ToString("D2")} Build:&#xA;";
            acbFile.StringValues = ACB_StringValue.DefaultStringTable();
            acbFile.AcfMd5Hash = (acfHash != null) ? acfHash : new byte[] { 103, 60, 108, 22, 196, 140, 254, 73, 5, 182, 132, 96, 160, 88, 222, 176 }; //Default DBXV2 ACF
            return acbFile;
        }

        public static ACB_File LoadFromClipboard()
        {
            if (!Clipboard.ContainsData(CLIPBOARD_ACB_CUES)) return null;
            ACB_File tempAcb = (ACB_File)Clipboard.GetData(CLIPBOARD_ACB_CUES);
            tempAcb.SetCommandTableVersion();
            //tempAcb.LinkTableGuids();
            return tempAcb;
        }

        private void InitializeValues()
        {
            //Call when loading from xml.

            if (Cues == null) Cues = new List<ACB_Cue>();
            if (Sequences == null) Sequences = new List<ACB_Sequence>();
            if (Synths == null) Synths = new List<ACB_Synth>();
            if (Waveforms == null) Waveforms = new List<ACB_Waveform>();
            if (Tracks == null) Tracks = new List<ACB_Track>();
            if (ActionTracks == null) ActionTracks = new List<ACB_Track>();
            if (Aisacs == null) Aisacs = new List<ACB_Aisac>();
            if (Graphs == null) Graphs = new List<ACB_Graph>();
            if (AutoModulations == null) AutoModulations = new List<ACB_AutoModulation>();
            if (GlobalAisacReferences == null) GlobalAisacReferences = new List<ACB_GlobalAisacReference>();

            foreach (var entry in Cues)
                entry.Initialize();

            foreach (var entry in Sequences)
                entry.Initialize();

            foreach (var entry in Synths)
                entry.Initialize();

            foreach (var entry in Tracks)
                entry.Initialize();

            foreach (var entry in Aisacs)
                entry.Initialize();

            foreach (var entry in ActionTracks)
                entry.Initialize();

            foreach(var commandTable in CommandTables.CommandTables)
            {
                if (commandTable.CommandGroups == null) commandTable.CommandGroups = new List<ACB_CommandGroup>();

                foreach(var commandGroup in commandTable.CommandGroups)
                {
                    if (commandGroup.Commands == null) commandGroup.Commands = AsyncObservableCollection<ACB_Command>.Create();
                }
            }

        }
        
        private void ValidateTables(UTF_File utfFile)
        {
            if (utfFile.ColumnTableExists("BlockSequenceTable", true) || utfFile.ColumnTableExists("BlockTable", true) || utfFile.ColumnTableExists("EventTable", true) 
                || utfFile.ColumnTableExists("AisacNameTable", true) || utfFile.ColumnTableExists("BeatSyncInfoTable", true)
                || utfFile.ColumnTableExists("SeqParameterPalletTable", true) || utfFile.ColumnTableExists("TrackParameterPalletTable", true) || utfFile.ColumnTableExists("SynthParameterPalletTable", true))
                TableValidationFailed = true;
        }

        private void LoadAwbFile(AFS2_File afs2File, bool streaming)
        {
            if (afs2File == null) return;
            if (afs2File.Entries == null) return;

            foreach (var entry in afs2File.Entries.Where(x => !x.BytesIsNullOrEmpty()))
            {
                int oldId = entry.AwbId;
                int newId = AudioTracks.AddEntry(entry, true);

                if (oldId != newId)
                    WaveformAwbIdRefactor(oldId, newId, streaming);
            }
        }

        private void LoadAwbFile(AWB_CPK cpkFile, bool streaming)
        {
            if (cpkFile == null) return;
            if (cpkFile.Entries == null) return;

            //Store header so it can be converted back into a cpk file easily on save
            if (CpkHeader == null)
                CpkHeader = cpkFile.CPK_Header;

            AFS2_File afs2File = new AFS2_File(cpkFile);
            LoadAwbFile(afs2File, streaming);
        }

        public void SetCommandTableVersion()
        {
            if (CommandTables != null)
                CommandTables.AcbVersion = Version;
        }

        #endregion

        #region SaveFunctions
        /// <summary>
        /// Save the acb + awb.
        /// </summary>
        /// <param name="path">The save location, minus the extension.</param>
        /// <param name="fullRebuild">Determines if table indexes will be dynamicly re-linked upon saving, using the TableGuid/InstanceGuid linkage.\n\nRecommended: Disabled for XMLs, enabled for all other uses.</param>
        public void Save(string path, bool fullRebuild = true)
        {
            List<IUndoRedo> undos = null;

            //Clean tables up
            if (fullRebuild && CleanTablesOnSave)
            {
                undos = CleanUpTables();
            }

            SortCues();
            TrimSequenceTrackPercentages();
            if (fullRebuild)
            {
                LinkTableIndexes();
            }


            AFS2_File externalAwb = GenerateAwbFile(true);
            byte[] awbHeader = null;
            byte[] awbBytes = null;

            if (externalAwb != null)
            {
                if (IsAwbCpk)
                {
                    awbBytes = new AWB_CPK(externalAwb, CpkHeader).Write();
                }
                else
                {
                    awbBytes = externalAwb.WriteAfs2File(out awbHeader);
                }
            }

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

            if(SaveFormat == SaveFormat.Default)
                utfFile.Save(path + ".acb");
            else if (SaveFormat == SaveFormat.MusicPackage)
                utfFile.Save(path + MUSIC_PACKAGE_EXTENSION);

            if (awbBytes != null)
                File.WriteAllBytes((IsUsingAltAwbPath) ? path + "_streamfiles.awb" : path + ".awb", awbBytes);
            

            if(undos != null)
            {
                CompositeUndo undo = new CompositeUndo(undos, "");
                undo.Undo();
            }
        }

        public void SaveToClipboard()
        {
            TrimSequenceTrackPercentages();
            LinkTableIndexes();
            Clipboard.SetData(CLIPBOARD_ACB_CUES, this);
        }

        public UTF_File WriteToTable(byte[] streamAwbHeader, byte[] streamAwbHash, string name)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.AcbFormatHelperMain.Header;

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
            UTF_File trackTable = CreateDefaultTrackTable();
            UTF_File actionTrackTable = CreateDefaultActionTrackTable();
            UTF_File graphTable = CreateDefaultGraphTable();
            UTF_File autoModTable = CreateDefaultAutoModulationTable();
            UTF_File waveformExtensionTable = CreateDefaultWaveformExtensionTable();
            UTF_File stringValueTable = CreateDefaultStringValueTable();

            //Reordering
            SortActionTracks(); //Sort them to be sequential, as in the UTF file ActionTracks are specified with a StartIndex and Count
            SortGlobalAisacRefs();

            //Fill tables
            ACB_Cue.WriteToTable(Cues, Version, cueTable, cueNameTable);
            ACB_Aisac.WriteToTable(Aisacs, Version, aisacTable, aisacControlNameTable);
            ACB_GlobalAisacReference.WriteToTable(GlobalAisacReferences, Version, globalAisacRefTable);
            ACB_Synth.WriteToTable(Synths, Version, synthTable);
            ACB_Sequence.WriteToTable(Sequences, Version, sequenceTable);
            ACB_Waveform.WriteToTable(Waveforms, Version, waveformTable, waveformExtensionTable, EternityCompatibility);
            ACB_Track.WriteToTable(Tracks, Version, trackTable, Name, false);
            ACB_Track.WriteToTable(ActionTracks, Version, actionTrackTable, Name, true);
            ACB_Graph.WriteToTable(Graphs, Version, graphTable);
            ACB_AutoModulation.WriteToTable(AutoModulations, Version, autoModTable);
            ACB_StringValue.WriteToTable(StringValues, Version, stringValueTable);

            //Columns
            utfFile.AddValue("FileIdentifier", TypeFlag.UInt32, 0, FileIdentifier.ToString(), tableHelper, Version);
            utfFile.AddValue("Size", TypeFlag.UInt32, 0, Size.ToString(), tableHelper, Version);
            utfFile.AddValue("Version", TypeFlag.UInt32, 0, BigEndianConverter.VersionToUInt(Version, true).ToString());
            utfFile.AddValue("VersionString", TypeFlag.String, 0, VersionString, tableHelper, Version);
            utfFile.AddValue("Type", TypeFlag.UInt8, 0, Type.ToString(), tableHelper, Version);
            utfFile.AddValue("Target", TypeFlag.UInt8, 0, Target.ToString(), tableHelper, Version);
            utfFile.AddData("AcbGuid", 0, GUID.ToByteArray(), tableHelper, Version);
            utfFile.AddData("AcfMd5Hash", 0, AcfMd5Hash, tableHelper, Version);
            utfFile.AddValue("CategoryExtension", TypeFlag.UInt8, 0, CategoryExtension.ToString(), tableHelper, Version);
            utfFile.AddData("CueTable", 0, (Cues.Count > 0) ? cueTable.Write() : null);
            utfFile.AddData("CueNameTable", 0, (Cues.Count > 0) ? cueNameTable.Write() : null);
            utfFile.AddData("WaveformTable", 0, (Waveforms.Count > 0) ? waveformTable.Write() : null);
            utfFile.AddData("AisacTable", 0, (Aisacs.Count > 0) ? aisacTable.Write() : null, tableHelper, Version);
            utfFile.AddData("GraphTable", 0, (Graphs.Count > 0) ? graphTable.Write() : null, tableHelper, Version);
            utfFile.AddData("GlobalAisacReferenceTable", 0, (GlobalAisacReferences.Count > 0) ? globalAisacRefTable.Write() : null, tableHelper, Version);
            utfFile.AddData("AisacNameTable", 0, null, tableHelper, Version); //Unknown
            utfFile.AddData("SynthTable", 0, (Synths.Count > 0) ? synthTable.Write() : null);
            utfFile.AddData("TrackTable", 0, (Tracks.Count > 0) ? trackTable.Write() : null);
            utfFile.AddData("SequenceTable", 0, (Sequences.Count > 0) ? sequenceTable.Write() : null);
            utfFile.AddData("AisacControlNameTable", 0, (Aisacs.Count > 0) ? aisacControlNameTable.Write() : null, tableHelper, Version);
            utfFile.AddData("AutoModulationTable", 0, (AutoModulations.Count > 0) ? autoModTable.Write() : null, tableHelper, Version);
            utfFile.AddData("StreamAwbTocWorkOld", 0, null, tableHelper, Version);
            utfFile.AddData("StreamAwbTocWork_Old", 0, null, tableHelper, Version);
            utfFile.AddValue("AcbVolume", TypeFlag.Single, 0, AcbVolume.ToString(), tableHelper, Version);
            utfFile.AddData("StringValueTable", 0, (StringValues.Count > 0) ? stringValueTable.Write() : null, tableHelper, Version);
            utfFile.AddData("OutsideLinkTable", 0, (OutsideLinkTable != null) ? OutsideLinkTable.Write() : null, tableHelper, Version);
            utfFile.AddData("BlockSequenceTable", 0, null, tableHelper, Version); //Not implemented
            utfFile.AddData("BlockTable", 0, null, tableHelper, Version); //Not implemented

            if (tableHelper.ColumnExists("CommandTable", TypeFlag.Data, Version))
            {
                utfFile.AddData("CommandTable", 0, CommandTables.WriteToTable(CommandTableType.SequenceCommand).Write());
            }
            else
            {
                if (!tableHelper.ColumnExists("SeqCommandTable", TypeFlag.Data, Version) || !tableHelper.ColumnExists("TrackCommandTable", TypeFlag.Data, Version) ||
                    !tableHelper.ColumnExists("SynthCommandTable", TypeFlag.Data, Version) || !tableHelper.ColumnExists("TrackEventTable", TypeFlag.Data, Version))
                    throw new InvalidDataException("ACB_File.WriteToTable: Unknown command table configuration.");

                utfFile.AddData("SeqCommandTable", 0, CommandTables.WriteToTable(CommandTableType.SequenceCommand).Write());
                utfFile.AddData("TrackCommandTable", 0, CommandTables.WriteToTable(CommandTableType.TrackCommand).Write());
                utfFile.AddData("SynthCommandTable", 0, CommandTables.WriteToTable(CommandTableType.SynthCommand).Write());
                utfFile.AddData("TrackEventTable", 0, CommandTables.WriteToTable(CommandTableType.TrackEvent).Write());
            }

            if (IsAwbCpk)
            {
                utfFile.AddData("AwbFile", 0, (internalAwb != null) ? new AWB_CPK(internalAwb, CpkHeader).Write() : null);
            }
            else
            {
                utfFile.AddData("AwbFile", 0, (internalAwb != null) ? internalAwb.WriteAfs2File() : null);
            }

            
            //CueLimitWorkTable is populated by null bytes (?) according to the 2 following values. The logic behind them is unknown, but idealy they should be equal to the amount of cues. The Command for "CueLimit" requires this to function correctly.
            //-Header = 8 bytes
            //-List Entry = 48 bytes
            //-Node Entry = 16 bytes
            //Not all ACBs actually have this table, but it should be safe to generate it even when its not needed.
            utfFile.AddData("CueLimitWorkTable", 0, new byte[8 + (64 * Cues.Count)], tableHelper, Version); 
            utfFile.AddValue("NumCueLimitListWorks", TypeFlag.UInt16, 0, Cues.Count.ToString(), tableHelper, Version);
            utfFile.AddValue("NumCueLimitNodeWorks", TypeFlag.UInt16, 0, Cues.Count.ToString(), tableHelper, Version);

            //External AWB hash
            var hashHelper = AcbFormatHelper.Instance.GetTableHelper("StreamAwbHash");

            if (hashHelper.DoesExist(Version) && streamAwbHash != null)
            {
                //UTF Table
                UTF_File streamAwbHashTable = CreateDefaultStreamAwbHashTable();
                streamAwbHashTable.AddValue("Name", TypeFlag.String, 0, (IsUsingAltAwbPath) ? $"{name}_streamfiles" : name, hashHelper, Version);
                streamAwbHashTable.AddData("Hash", 0, streamAwbHash);
                utfFile.AddData("StreamAwbHash", 0, streamAwbHashTable.Write());
            }
            else if(tableHelper.ColumnExists("StreamAwbHash", TypeFlag.Data, Version))
            {
                //Just a byte array
                utfFile.AddData("StreamAwbHash", 0, (streamAwbHash != null) ? streamAwbHash : new byte[16]);
            }
            

            utfFile.AddValue("Name", TypeFlag.String, 0, name, tableHelper, Version);
            utfFile.AddValue("CharacterEncodingType", TypeFlag.UInt8, 0, CharacterEncodingType.ToString(), tableHelper, Version);
            utfFile.AddData("EventTable", 0, null, tableHelper, Version); //Not implemented
            utfFile.AddData("ActionTrackTable", 0, (ActionTracks.Count > 0) ? actionTrackTable.Write() : null, tableHelper, Version);
            utfFile.AddData("AcfReferenceTable", 0, (AcfReferenceTable != null) ? AcfReferenceTable.Write() : null, tableHelper, Version);
            utfFile.AddData("WaveformExtensionDataTable", 0, (waveformExtensionTable.IsValid() && !EternityCompatibility) ? waveformExtensionTable.Write() : null, tableHelper, Version);
            utfFile.AddData("BeatSyncInfoTable", 0, null, tableHelper, Version); //Not implemented
            utfFile.AddValue("CuePriorityType", TypeFlag.UInt8, 0, CuePriorityType.ToString(), tableHelper, Version);
            utfFile.AddValue("NumCueLimit", TypeFlag.UInt16, 0, NumCueLimit.ToString(), tableHelper, Version);
            utfFile.AddData("SeqParameterPalletTable", 0, null, tableHelper, Version);
            utfFile.AddData("TrackParameterPalletTable", 0, null, tableHelper, Version);
            utfFile.AddData("SynthParameterPalletTable", 0, null, tableHelper, Version);
            utfFile.AddData("SoundGeneratorTable", 0, (SoundGeneratorTable != null) ? SoundGeneratorTable.Write() : null, tableHelper, Version);
            utfFile.AddData("PaddingArea", 0, null, tableHelper, Version);
            utfFile.AddData("StreamAwbTocWork", 0, StreamAwbTocWork, tableHelper, Version); //Not sure what this is...

            //External AWB header
            var headerHelper = AcbFormatHelper.Instance.GetTableHelper("StreamAwbAfs2Header");

            if (headerHelper.DoesExist(Version) && streamAwbHeader != null && !IsAwbCpk)
            {
                //UTF Table
                UTF_File streamAwbHeaderTable = CreateDefaultStreamAwbHeaderTable();
                streamAwbHeaderTable.AddData("Header", 0, streamAwbHeader);
                utfFile.AddData("StreamAwbAfs2Header", 0, streamAwbHeaderTable.Write());
            }
            else if (tableHelper.ColumnExists("StreamAwbAfs2Header", TypeFlag.Data, Version))
            {
                //Just a byte array
                utfFile.AddData("StreamAwbAfs2Header", 0, streamAwbHeader);
            }

            //MusicPackage values
            if(SaveFormat == SaveFormat.MusicPackage)
            {
                utfFile.AddValue("MusicPackageType", TypeFlag.Int32, 0, ((int)MusicPackageType).ToString());
            }

            //Now set all of the "R_" columns
            foreach(var column in utfFile.Columns.Where(x => x.Rows.Count == 0))
            {
                if(column.Name[0] == 'R')
                {
                    column.AddValue("0", 0);
                }
                else
                {
                    throw new InvalidDataException($"ACB_File.WriteToTable: row count mismatch");
                }
            }

            return utfFile;
        }
        
        public byte[] SaveMusicPackageToBytes()
        {
            TrimSequenceTrackPercentages();
            LinkTableIndexes();
            UTF_File utfFile = WriteToTable(null, null, Name);
            return utfFile.Write(true);
        }
        #endregion

        #region AddFunctions
        /// <summary>
        /// Add a new cue with no tracks.
        /// </summary>
        /// <returns></returns>
        public List<IUndoRedo> AddCue(string name, ReferenceType type, out ACB_Cue newCue)
        {
            return AddCue(name, GetFreeCueId(), type, out newCue);
        }

        private List<IUndoRedo> AddCue(string name, int id, ReferenceType type, out ACB_Cue newCue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            newCue = new ACB_Cue();
            newCue.ID = (uint)id;
            newCue.Name = name;
            newCue.ReferenceType = type;
            newCue.ReferenceIndex = new AcbTableReference(ushort.MaxValue);
            newCue.HeaderVisibility = 1;

            if (type == ReferenceType.Sequence)
            {
                var seq = new ACB_Sequence();
                Sequences.Add(seq);
                newCue.ReferenceIndex.TableGuid = seq.InstanceGuid;
                undos.Add(new UndoableListAdd<ACB_Sequence>(Sequences, seq, ""));

                if (ReuseSequenceCommand)
                {
                    //Reuse an existing command
                    var existingSequence = Sequences.FirstOrDefault(x => !x.CommandIndex.IsNull);

                    if(existingSequence != null)
                    {
                        seq.CommandIndex = new AcbTableReference(existingSequence.CommandIndex.TableGuid);
                    }
                }
                else
                {
                    //Add default volume bus command
                    undos.AddRange(AddVolumeBus(seq, CommandTableType.SequenceCommand));
                }
            }
            else if (type == ReferenceType.Synth)
            {
                var synth = new ACB_Synth();
                synth.ReferenceItems[0].ReferenceType = ReferenceType.Waveform;
                Synths.Add(synth);
                newCue.ReferenceIndex.TableGuid = synth.InstanceGuid;
                undos.Add(new UndoableListAdd<ACB_Synth>(Synths, synth, ""));
                undos.AddRange(AddVolumeBus(synth, CommandTableType.SynthCommand));
            }
            //Dont add Waveform entry until a track is added

            //If cue with same ID already exists, delete it
            var existingCue = Cues.FirstOrDefault(x => x.ID == id);

            if (existingCue != null)
            {
                undos.Add(new UndoableListRemove<ACB_Cue>(Cues, existingCue));
                Cues.Remove(existingCue);
            }

            Cues.Add(newCue);
            undos.Add(new UndoableListAdd<ACB_Cue>(Cues, newCue, ""));

            return undos;
        }

        /// <summary>
        /// Add a new cue with 1 track.
        /// </summary>
        /// <returns></returns>
        public List<IUndoRedo> AddCue(string name, ReferenceType type, byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType, out ACB_Cue newCue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            
            undos.AddRange(AddCue(name, type, out newCue));
            undos.AddRange(AddTrackToCue(newCue, trackBytes, streaming, loop, encodeType));
            
            return undos;
        }

        public List<IUndoRedo> AddCue(string name, int id, ReferenceType type, byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType, out ACB_Cue newCue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.AddRange(AddCue(name, id, type, out newCue));
            undos.AddRange(AddTrackToCue(newCue, trackBytes, streaming, loop, encodeType));

            return undos;
        }

        //Add Action
        /// <summary>
        /// Add a new empty action to the specified cue.
        /// </summary>
        /// <returns></returns>
        public List<IUndoRedo> AddActionToCue(ACB_Cue cue, CopiedAction copiedAction = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            switch (cue.ReferenceType)
            {
                case ReferenceType.Sequence:
                    undos.AddRange(AddAction_IActionTrack(GetSequence(cue.ReferenceIndex.TableGuid, false), copiedAction));
                    break;
                case ReferenceType.Synth:
                    undos.AddRange(AddAction_IActionTrack(GetSynth(cue.ReferenceIndex.TableGuid, false), copiedAction));
                    break;
                case ReferenceType.Waveform:
                    throw new InvalidOperationException($"ACB_File.AddActionToCue: ReferenceType {cue.ReferenceType} does not support actions.");
                default:
                    throw new InvalidOperationException($"ACB_File.AddActionToCue: ReferenceType {cue.ReferenceType} not supported.");
            }

            return undos;
        }

        private List<IUndoRedo> AddAction_IActionTrack(IActionTrack action, CopiedAction copiedAction = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            Guid newActionGuid;
            undos.AddRange(AddAction(out newActionGuid, copiedAction));
            AcbTableReference actionTableReference = new AcbTableReference();
            actionTableReference.TableGuid = newActionGuid;
            action.ActionTracks.Add(actionTableReference);
            undos.Add(new UndoableListAdd<AcbTableReference>(action.ActionTracks, actionTableReference));

            return undos;
        }

        private List<IUndoRedo> AddAction(out Guid actionGuid, CopiedAction copiedAction = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ACB_Track action = (copiedAction != null) ? copiedAction.Track : new ACB_Track();
            ACB_CommandGroup commandGroup = (copiedAction != null) ? copiedAction.Commands : new ACB_CommandGroup();
            
            if(copiedAction == null)
            {
                //Set default action values
                action.CommandIndex.TableGuid = commandGroup.InstanceGuid;
                action.TargetType = TargetType.SpecificAcb;
                action.TargetAcbName = Name;
                action.TargetSelf = true;
            }

            //Finalize
            ActionTracks.Add(action);
            undos.Add(new UndoableListAdd<ACB_Track>(ActionTracks, action));
            undos.AddRange(CommandTables.AddCommand(commandGroup, CommandTableType.TrackEvent));

            actionGuid = action.InstanceGuid;
            return undos;
        }

        //Add Track
        public List<IUndoRedo> AddTrackToCue(ACB_Cue cue, byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.AddRange(AddTrack_IRefItems(cue, trackBytes, streaming, loop, encodeType));
            undos.AddRange(UpdateCueLength(cue));
            return undos;
        }
        
        private List<IUndoRedo> AddWaveform(byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType, out Guid waveformGuid)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var waveform = new ACB_Waveform();
            var trackMetadata = new TrackMetadata(trackBytes);
            undos.AddRange(AudioTracks.AddEntry(trackBytes, AllowSharedAwbEntries, out ushort awbId));

            //Create waveform
            waveform.AwbId = awbId;
            waveform.Streaming = streaming;
            waveform.StreamAwbPortNo = (streaming) ? (ushort)0 : ushort.MaxValue;
            waveform.EncodeType = encodeType;
            waveform.LoopFlag = Convert.ToByte(loop);
            waveform.NumChannels = trackMetadata.Channels;
            waveform.SamplingRate = (ushort)trackMetadata.SampleRate;
            waveform.NumSamples = trackMetadata.NumSamples;

            if(encodeType == EncodeType.HCA)
            {
                HcaMetadata hcaMetadata = new HcaMetadata(trackBytes);
                waveform.LoopStart = (loop) ? hcaMetadata.LoopStart : 0;
                waveform.LoopEnd = (loop) ? hcaMetadata.LoopEnd : 0;
            }

            Waveforms.Add(waveform);
            undos.Add(new UndoableListAdd<ACB_Waveform>(Waveforms, waveform));

            waveformGuid = waveform.InstanceGuid;
            return undos;
        }
        
        private List<IUndoRedo> AddTrackToSynth(ACB_Synth synth, byte[] hca, bool streaming, bool loop, EncodeType encodeType)
        {
            if (synth.ReferenceItems[0].ReferenceIndex.IsNull)
            {
                //Replace this one
                return AddTrack_IRefItems(synth.ReferenceItems[0], hca, streaming, loop, encodeType);
            }
            else
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                //Add new
                ACB_ReferenceItem refItem = new ACB_ReferenceItem();
                synth.ReferenceItems.Add(refItem);
                undos.Add(new UndoableListAdd<ACB_ReferenceItem>(synth.ReferenceItems, refItem));

                undos.AddRange(AddTrack_IRefItems(refItem, hca, streaming, loop, encodeType));
                return undos;
            }
        }

        private List<IUndoRedo> AddTrack_IRefItems(IReferenceType refItem, byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            switch (refItem.ReferenceType)
            {
                case ReferenceType.Waveform:
                    if (refItem.ReferenceIndex.IsNull)
                    {
                        undos.AddRange(AddWaveform(trackBytes, streaming, loop, encodeType, out refItem.ReferenceIndex._tableGuid));
                    }
                    else
                        throw new InvalidDataException($"ACB_File.AddTrackToCue: ReferenceType is {refItem.ReferenceType} - cannot add multiple tracks.");
                    break;
                case ReferenceType.Synth:
                    undos.AddRange(AddTrackToSynth(GetSynth(refItem.ReferenceIndex.TableGuid, false), trackBytes, streaming, loop, encodeType));
                    break;
                case ReferenceType.Sequence:
                    undos.AddRange(AddTrackToSequence(GetSequence(refItem.ReferenceIndex.TableGuid, false), trackBytes, streaming, loop, encodeType));
                    break;
            }

            return undos;
        }
        
        private List<IUndoRedo> AddTrackToSequence(ACB_Sequence sequence, byte[] hca, bool streaming, bool loop, EncodeType encodeType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ACB_SequenceTrack newTrack = new ACB_SequenceTrack();
            ACB_Track track = new ACB_Track();
            newTrack.Index.TableGuid = track.InstanceGuid;
            newTrack.Percentage = (ushort)(100 / (sequence.Tracks.Count + 1));

            if (ReuseTrackCommand)
            {
                //Set CommandIndex to that of another Track in the ACB
                var existingTrack = Tracks.FirstOrDefault(x => !x.CommandIndex.IsNull);

                if(existingTrack != null)
                {
                    track.CommandIndex = new AcbTableReference(Tracks[0].CommandIndex.TableGuid);
                }
            }

            //Create command
            ACB_Command command = new ACB_Command();
            command.CommandType = CommandType.ReferenceItem;
            command.Parameters = new byte[4].ToList();
            command.ReferenceType = ReferenceType.Synth;
            command.ReferenceIndex = new AcbTableReference();

            //Create synth
            ACB_Synth synth = new ACB_Synth();
            synth.ReferenceItems[0].ReferenceType = ReferenceType.Waveform;
            command.ReferenceIndex.TableGuid = synth.InstanceGuid;
            Synths.Add(synth);
            undos.Add(new UndoableListAdd<ACB_Synth>(Synths, synth));
            undos.AddRange(AddTrackToSynth(synth, hca, streaming, loop, encodeType));

            ACB_CommandGroup commandGroup = new ACB_CommandGroup();
            commandGroup.Commands.Add(command);
            commandGroup.Commands.Add(new ACB_Command()); //IMPROTANT! Game will crash without this!

            undos.AddRange(CommandTables.AddCommand(commandGroup, CommandTableType.TrackEvent));

            //Create track
            track.TargetId.TableIndex = uint.MaxValue;
            track.EventIndex.TableGuid = commandGroup.InstanceGuid;

            Tracks.Add(track);
            undos.Add(new UndoableListAdd<ACB_Track>(Tracks, track));

            //Add track to sequence
            sequence.Tracks.Add(newTrack);
            undos.Add(new UndoableListAdd<ACB_SequenceTrack>(sequence.Tracks, newTrack));

            return undos;
        }
        
        //Replace Track
        /// <summary>
        /// Replaces track on cue. Expects just one waveform on cue. Will reuse existing tables.
        /// </summary>
        public void ReplaceTrackOnCue(ACB_Cue cue, byte[] trackBytes, bool streaming, EncodeType encodeType)
        {
            var waveforms = GetWaveformsFromCue(cue);
            if (waveforms.Count != 1) throw new InvalidDataException($"ACB_File.ReplaceTrackOnCue: Unexpected number of Waveforms ({waveforms.Count}).");

            ReplaceTrackOnWaveform(waveforms[0], trackBytes, streaming, encodeType);
            UpdateCueLength(cue);
        }

        public List<IUndoRedo> ReplaceTrackOnWaveform(ACB_Waveform waveform, byte[] trackBytes, bool streaming, EncodeType encodeType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.AddRange(AudioTracks.AddEntry(trackBytes, true, out ushort newAwbId));

            var trackMetadata = new TrackMetadata(trackBytes);

            ushort oldAwbId = waveform.AwbId;
            ushort newStreamAwbPortNo = (streaming) ? (ushort)0 : ushort.MaxValue;

            bool hasLoop = false;
            uint loopStart = 0;
            uint loopEnd = 0;

            if(encodeType == EncodeType.HCA)
            {
                var hcaMeta = new HcaMetadata(trackBytes);
                hasLoop = hcaMeta.HasLoopData;
                loopStart = hcaMeta.LoopStartSamples;
                loopEnd = hcaMeta.LoopEndSamples;

            }

            undos.Add(new UndoableProperty<ACB_Waveform>("AwbId", waveform, waveform.AwbId, newAwbId));
            undos.Add(new UndoableProperty<ACB_Waveform>("Streaming", waveform, waveform.Streaming, streaming));
            undos.Add(new UndoableProperty<ACB_Waveform>("StreamAwbPortNo", waveform, waveform.StreamAwbPortNo, newStreamAwbPortNo));
            undos.Add(new UndoableProperty<ACB_Waveform>("EncodeType", waveform, waveform.EncodeType, encodeType));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopFlag", waveform, waveform.LoopFlag, hasLoop));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopStart", waveform, waveform.LoopStart, loopStart));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopEnd", waveform, waveform.AwbId, loopEnd));
            undos.Add(new UndoableProperty<ACB_Waveform>("NumChannels", waveform, waveform.NumChannels, trackMetadata.Channels));
            undos.Add(new UndoableProperty<ACB_Waveform>("SamplingRate", waveform, waveform.SamplingRate, (ushort)trackMetadata.SampleRate));
            undos.Add(new UndoableProperty<ACB_Waveform>("NumSamples", waveform, waveform.SamplingRate, (uint)trackMetadata.NumSamples));

            waveform.AwbId = newAwbId;
            waveform.Streaming = streaming;
            waveform.StreamAwbPortNo = newStreamAwbPortNo;
            waveform.EncodeType = EncodeType.HCA;
            waveform.LoopFlag = Convert.ToByte(hasLoop);
            waveform.LoopStart = loopStart;
            waveform.LoopEnd = loopEnd;
            waveform.NumChannels = trackMetadata.Channels;
            waveform.SamplingRate = (ushort)trackMetadata.SampleRate;
            waveform.NumSamples = (uint)trackMetadata.NumSamples;

            //Remove previous AWB entry if nothing else uses it.
            if(Waveforms.All(x=> x.AwbId != oldAwbId))
            {
                var entry = AudioTracks.Entries.FirstOrDefault(x => x.AwbId == oldAwbId);
                if(entry != null)
                {
                    undos.Add(new UndoableListRemove<AFS2_Entry>(AudioTracks.Entries, entry, AudioTracks.Entries.IndexOf(entry)));
                    AudioTracks.Entries.Remove(entry);
                }
            }
            
            waveform.UpdateProperties();
            undos.Add(new UndoActionDelegate(waveform, "UpdateProperties", true));

            return undos;
        }
        
        //Loop
        public List<IUndoRedo> EditLoopOnWaveform(ACB_Waveform waveform, bool loop, int startMs, int endMs)
        {
            if (waveform.EncodeType != EncodeType.HCA && waveform.EncodeType != EncodeType.HCA_ALT)
                throw new InvalidOperationException("Can only edit loop on HCA encoded tracks.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            var awbEntry = GetAfs2Entry(waveform.AwbId);
            if (awbEntry == null) return undos;

            double startSeconds = startMs / 1000.0;
            double endSeconds = endMs / 1000.0;

            byte[] originalFile = awbEntry.bytes.DeepCopy();
            awbEntry.bytes = HcaMetadata.SetLoop(awbEntry.bytes, loop, startSeconds, endSeconds);
            
            HcaMetadata metadata = new HcaMetadata(awbEntry.bytes);

            undos.Add(new UndoableProperty<AFS2_Entry>("bytes", awbEntry, originalFile, awbEntry.bytes.DeepCopy()));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopStart", waveform, waveform.LoopStart, metadata.LoopStartSamples));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopEnd", waveform, waveform.LoopEnd, metadata.LoopEndSamples));
            undos.Add(new UndoableProperty<ACB_Waveform>("LoopFlag", waveform, waveform.LoopFlag, metadata.HasLoopData));

            //Update waveform
            waveform.LoopStart = metadata.LoopStartSamples;
            waveform.LoopEnd = metadata.LoopEndSamples;
            waveform.LoopFlag = Convert.ToByte(metadata.HasLoopData);

            return undos;
        }
        
        //VolumeBus 
        public List<IUndoRedo> AddVolumeBus(ICommandIndex commandIndex, CommandTableType commandType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (Version < VolumeBusRequiredVersion) return undos; //VolumeBus not required

            ACB_CommandGroup command = null;

            if (!commandIndex.CommandIndex.IsNull)
            {
                command = CommandTables.GetCommand(commandIndex.CommandIndex.TableGuid, commandType);
            }

            if(command == null)
            {
                //No command on this Sequence/Synth. Need to create new one.
                command = new ACB_CommandGroup();
                undos.Add(new UndoableProperty<AcbTableReference>(nameof(AcbTableReference.TableGuid), commandIndex.CommandIndex, commandIndex.CommandIndex.TableGuid, command.InstanceGuid));
                commandIndex.CommandIndex.TableGuid = command.InstanceGuid;
                undos.AddRange(CommandTables.AddCommand(command, commandType));
            }

            //Check if volume bus command already exists, and end here if it does.
            if (command.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeBus) != null) return undos;

            //Check if StringValueTable exists, and create if it needed.
            if (StringValues == null || StringValues?.Count == 0)
            {
                var newStringValues = ACB_StringValue.DefaultStringTable();
                undos.Add(new UndoableProperty<ACB_File>(nameof(StringValues), this, StringValues, newStringValues));
                StringValues = ACB_StringValue.DefaultStringTable();
            }

            //Add the command
            ACB_Command volumeBusCommand = new ACB_Command(CommandType.VolumeBus) { ReferenceIndex = new AcbTableReference(StringValues[0].InstanceGuid), Param2 = 10000 };

            undos.Add(new UndoableListAdd<ACB_Command>(command.Commands, volumeBusCommand));
            command.Commands.Add(volumeBusCommand);

            return undos;
        }
        
        /// <summary>
        /// Adds a VolumeBus command to every valid cue (only if one doesn't already exist).
        /// </summary>
        public List<IUndoRedo> AddVolumeBusToCues()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var cue in Cues)
            {
                undos.AddRange(AddVolumeBusToCue(cue));
            }

            return undos;
        }

        public List<IUndoRedo> AddVolumeBusToCue(ACB_Cue cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            bool hasTracks = true;
            ICommandIndex commandIndex = null;
            CommandTableType commandType = CommandTableType.SequenceCommand;

            switch (cue.ReferenceType)
            {
                case ReferenceType.Synth:
                    ACB_Synth synth = GetSynth(cue.ReferenceIndex.TableGuid);
                    commandIndex = synth;
                    hasTracks = synth.ReferenceItems.Count > 0;
                    commandType = CommandTableType.SynthCommand;
                    break;
                case ReferenceType.Sequence:
                    ACB_Sequence sequence = GetSequence(cue.ReferenceIndex.TableGuid);
                    commandIndex = sequence;
                    hasTracks = sequence.Tracks.Count > 0;
                    commandType = CommandTableType.SequenceCommand;
                    break;
            }

            if (commandIndex != null && hasTracks)
            {
                undos.AddRange(AddVolumeBus(commandIndex, commandType));
            }

            return undos;
        }
        #endregion

        #region MiscFunctions
        public List<IUndoRedo> UpdateCueLength(ACB_Cue cue)
        {
            uint length = 0;

            foreach(var waveform in GetWaveformsFromCue(cue))
            {
                var awbEntry = GetAfs2Entry(waveform.AwbId, true);
                TrackMetadata trackInfo = (awbEntry != null) ? awbEntry.HcaInfo : new TrackMetadata();

                if (trackInfo.DurationSeconds * 1000 > length)
                    length = (uint)(trackInfo.DurationSeconds * 1000);
            }

            uint originalLength = cue.Length;
            cue.Length = length;

            return new List<IUndoRedo>() { new UndoableProperty<ACB_Cue>("Length", cue, originalLength, length) };
        }
        
        /// <summary>
        /// Checks if the specified cue can add any more tracks.
        /// </summary>
        public bool CanAddTrack(ACB_Cue cue)
        {
            if(cue.ReferenceType == ReferenceType.Sequence || cue.ReferenceType == ReferenceType.Synth)
            {
                return true;
            }
            else if(cue.ReferenceType == ReferenceType.Waveform)
            {
                return cue.ReferenceIndex.IsNull;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the specified cue can add any actions.
        /// </summary>
        public bool CanAddAction(ACB_Cue cue)
        {
            return cue.ReferenceType == ReferenceType.Sequence || cue.ReferenceType == ReferenceType.Synth;
        }
        
        public List<IUndoRedo> RandomizeCueIds()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            int startCueId = Random.Range(5000, int.MaxValue / 2);

            for (int i = 0; i < Cues.Count; i++)
            {
                uint newId = (uint)(startCueId + i);
                undos.Add(new UndoableProperty<ACB_Cue>(nameof(ACB_Cue.ID), Cues[i], Cues[i].ID, newId));
                Cues[i].ID = newId;
            }

            return undos;
        }
        
        //3Dvol_def
        public List<IUndoRedo> Add3dDefToCue(ACB_Cue cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (DoesCueHave3dDef(cue)) return undos;

            IGlobalAisacRef globalAisacHolder;

            switch (cue.ReferenceType)
            {
                case ReferenceType.Sequence:
                    globalAisacHolder = GetSequence(cue.ReferenceIndex.TableGuid);
                    break;
                case ReferenceType.Synth:
                    globalAisacHolder = GetSynth(cue.ReferenceIndex.TableGuid);
                    break;
                default:
                    return undos;
            }


            ACB_GlobalAisacReference globalAisacRef = new ACB_GlobalAisacReference() { Name = GLOBAL_AISAC_3DVOL_DEF, InstanceGuid = Guid.NewGuid() };
            GlobalAisacReferences.Add(globalAisacRef);
            undos.Add(new UndoableListAdd<ACB_GlobalAisacReference>(GlobalAisacReferences, globalAisacRef));

            AcbTableReference _3dRef = new AcbTableReference() { TableGuid = globalAisacRef.InstanceGuid };

            globalAisacHolder.GlobalAisacRefs.Add(_3dRef);
            undos.Add(new UndoableListAdd<AcbTableReference>(globalAisacHolder.GlobalAisacRefs, _3dRef));

            return undos;
        }

        public List<IUndoRedo> Remove3dDefFromCue(ACB_Cue cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            
            IGlobalAisacRef globalAisacHolder;

            switch (cue.ReferenceType)
            {
                case ReferenceType.Sequence:
                    globalAisacHolder = GetSequence(cue.ReferenceIndex.TableGuid);
                    break;
                case ReferenceType.Synth:
                    globalAisacHolder = GetSynth(cue.ReferenceIndex.TableGuid);
                    break;
                default:
                    return undos;
            }
            
            foreach (var aisac in globalAisacHolder.GlobalAisacRefs)
            {
                var aisacRef = GetGlobalAisacReference(aisac.TableGuid);
                if (aisacRef != null)
                {
                    if (aisacRef.Name == GLOBAL_AISAC_3DVOL_DEF)
                    {
                        undos.Add(new UndoableListRemove<AcbTableReference>(globalAisacHolder.GlobalAisacRefs, aisac));
                        globalAisacHolder.GlobalAisacRefs.Remove(aisac);
                        break;
                    }
                }
            }

            return undos;
        }

        public bool DoesCueHave3dDef(ACB_Cue cue)
        {
            IGlobalAisacRef globalAisacHolder;

            switch (cue.ReferenceType)
            {
                case ReferenceType.Sequence:
                    globalAisacHolder = GetSequence(cue.ReferenceIndex.TableGuid);
                    break;
                case ReferenceType.Synth:
                    globalAisacHolder = GetSynth(cue.ReferenceIndex.TableGuid);
                    break;
                default:
                    return false;
            }

            foreach(var aisac in globalAisacHolder.GlobalAisacRefs)
            {
                var aisacRef = GetGlobalAisacReference(aisac.TableGuid);
                if(aisacRef != null)
                {
                    if (aisacRef.Name == GLOBAL_AISAC_3DVOL_DEF) return true;
                }
            }

            return false;
        }
        
        #endregion

        #region CopyFunctions
        public List<IUndoRedo> CopyCue(int cueId, ACB_File copyAcb)
        {
            //For when you dont care about the new cue ID
            int id;
            return CopyCue(cueId, copyAcb, out id);
        }

        public List<IUndoRedo> CopyCue(int cueId, ACB_File copyAcb, out int newCueId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Get and copy the cue
            ACB_Cue cue = copyAcb.GetCue(cueId);
            if (cue == null)
            {
                newCueId = -1;
                return undos;
            }

            //Add volumeBus command if required by version (bug-fix)
            copyAcb.AddVolumeBusToCue(cue);

            cue = cue.Copy();

            //Calculate new cueID if needed
            if (Cues.Any(x => x.ID == cue.ID))
                cue.ID = (uint)GetFreeCueId();

            //Copy referenced tables (if needed)
            undos.AddRange(CopyReferenceItems(cue, copyAcb));

            //Add to acb
            Cues.Add(cue);
            undos.Add(new UndoableListAdd<ACB_Cue>(Cues, cue));

            newCueId = (int)cue.ID;
            return undos;
        }

        private List<IUndoRedo> CopyReferenceItems(IReferenceItems referenceItem, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            
            foreach(var item in referenceItem.ReferenceItems)
            {
                undos.AddRange(CopyReferenceItems(item, copyAcb));
            }

            return undos;
        }

        private List<IUndoRedo> CopyReferenceItems(IReferenceType referenceItem, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (referenceItem.ReferenceIndex.IsNull) return undos;

            switch (referenceItem.ReferenceType)
            {
                case ReferenceType.Waveform:
                    undos.AddRange(CopyWaveform(copyAcb.GetWaveform(referenceItem.ReferenceIndex.TableGuid, false), copyAcb));
                    break;
                case ReferenceType.Synth:
                    undos.AddRange(CopySynth(copyAcb.GetSynth(referenceItem.ReferenceIndex.TableGuid, false), copyAcb));
                    break;
                case ReferenceType.Sequence:
                    undos.AddRange(CopySequence(copyAcb.GetSequence(referenceItem.ReferenceIndex.TableGuid, false), copyAcb));
                    break;
                default:
                    throw new InvalidDataException($"ACB_File.CopyReferenceItems: Encountered a non-supported ReferenceType ({referenceItem.ReferenceType}). Copy failed.");
            }

            return undos;
        }

        private List<IUndoRedo> CopyWaveform(ACB_Waveform waveform, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (GetWaveform(waveform.InstanceGuid) == null)
            {
                waveform = waveform.Copy();
                Waveforms.Add(waveform);
                undos.Add(new UndoableListAdd<ACB_Waveform>(Waveforms, waveform));

                if(waveform.AwbId != ushort.MaxValue)
                {
                    ushort awbId;
                    undos.AddRange(CopyAwbEntry(copyAcb.GetAfs2Entry(waveform.AwbId, false), copyAcb, out awbId));
                    waveform.AwbId = awbId;
                }
                
            }

            return undos;
        }

        private List<IUndoRedo> CopySynth(ACB_Synth synth, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (GetSynth(synth.InstanceGuid) == null)
            {
                synth = synth.Copy();

                //ReferenceItems
                undos.AddRange(CopyReferenceItems(synth, copyAcb));

                //Commnad
                if (!synth.CommandIndex.IsNull)
                    undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(synth.CommandIndex.TableGuid, CommandTableType.SynthCommand, false), copyAcb, CommandTableType.SynthCommand));

                undos.AddRange(CopyLocalAisacs(synth, copyAcb));
                undos.AddRange(CopyGlobalAisacs(synth, copyAcb));
                undos.AddRange(CopyActionTracks(synth, copyAcb));

                Synths.Add(synth);
                undos.Add(new UndoableListAdd<ACB_Synth>(Synths, synth));
            }

            return undos;
        }
        
        private List<IUndoRedo> CopySequence(ACB_Sequence sequence, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (GetSequence(sequence.InstanceGuid) == null)
            {
                sequence = sequence.Copy();

                //Commnad
                if (!sequence.CommandIndex.IsNull)
                    undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(sequence.CommandIndex.TableGuid, CommandTableType.SequenceCommand, false), copyAcb, CommandTableType.SequenceCommand));
                
                undos.AddRange(CopyLocalAisacs(sequence, copyAcb));
                undos.AddRange(CopyGlobalAisacs(sequence, copyAcb));
                undos.AddRange(CopyActionTracks(sequence, copyAcb));

                //Tracks
                foreach(var track in sequence.Tracks)
                {
                    if (!track.Index.IsNull)
                        undos.AddRange(CopyTrack(copyAcb.GetTrack(track.Index.TableGuid, false), copyAcb));
                }

                Sequences.Add(sequence);
                undos.Add(new UndoableListAdd<ACB_Sequence>(Sequences, sequence));
            }

            return undos;
        }

        private List<IUndoRedo> CopyAwbEntry(AFS2_Entry awbEntry, ACB_File copyAcb, out ushort newAwbId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ushort id = AudioTracks.GetExistingEntry(awbEntry);
            if (id == ushort.MaxValue)
            {
                //Awb entry doesn't exist, adding it..
                awbEntry = awbEntry.Copy();
                id = (ushort)AudioTracks.NextID();
                awbEntry.AwbId = id;
                AudioTracks.Entries.Add(awbEntry);
                undos.Add(new UndoableListAdd<AFS2_Entry>(AudioTracks.Entries, awbEntry));
            }

            newAwbId = id;
            return undos;
        }

        private List<IUndoRedo> CopyCommand(ACB_CommandGroup command, ACB_File copyAcb, CommandTableType commandTableType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(CommandTables.GetCommand(command.InstanceGuid, commandTableType) == null)
            {
                command = command.Copy();

                //Copy referenced tables
                foreach(var cmd in command.Commands)
                {
                    switch (cmd.CommandType)
                    {
                        case CommandType.ReferenceItem:
                        case CommandType.ReferenceItem2:
                            undos.AddRange(CopyReferenceItems(cmd, copyAcb));
                            break;
                        case CommandType.GlobalAisacReference:
                            undos.AddRange(CopyGlobalAisac(copyAcb, cmd.ReferenceIndex));
                            break;
                        case CommandType.VolumeBus:
                        case CommandType.Bus:
                            undos.AddRange(CopyStringValue(cmd.ReferenceIndex, copyAcb));
                            break;
                    }
                }

                undos.AddRange(CommandTables.AddCommand(command, commandTableType));
            }

            return undos;
        }
        
        private List<IUndoRedo> CopyTrack(ACB_Track track, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(GetTrack(track.InstanceGuid) == null)
            {
                track = track.Copy();

                undos.AddRange(CopyLocalAisacs(track, copyAcb));
                undos.AddRange(CopyGlobalAisacs(track, copyAcb));

                if(!track.CommandIndex.IsNull)
                    undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(track.CommandIndex.TableGuid, CommandTableType.TrackCommand, false), copyAcb, CommandTableType.TrackCommand));

                if (!track.EventIndex.IsNull)
                    undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(track.EventIndex.TableGuid, CommandTableType.TrackEvent, false), copyAcb, CommandTableType.TrackEvent));

                Tracks.Add(track);
                undos.Add(new UndoableListAdd<ACB_Track>(Tracks, track));

            }

            return undos;
        }

        private List<IUndoRedo> CopyGraphs(IGraphIndexes graphs, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var graph in graphs.GraphIndexes)
            {
                if(GetGraph(graph.TableGuid) == null && !graph.IsNull)
                {
                    var graphTable = copyAcb.GetGraph(graph.TableGuid, false).Copy();
                    Graphs.Add(graphTable);
                    undos.Add(new UndoableListAdd<ACB_Graph>(Graphs, graphTable));
                }
            }

            return undos;
        }

        private List<IUndoRedo> CopyAutoModulation(IAutoModulationIndex autoMod, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (autoMod.AutoModulationIndex.IsNull) return undos;

            if (GetAutoModulation(autoMod.AutoModulationIndex.TableGuid) == null)
            {
                var newAutoMod = copyAcb.GetAutoModulation(autoMod.AutoModulationIndex.TableGuid).Copy();
                AutoModulations.Add(newAutoMod);
                undos.Add(new UndoableListAdd<ACB_AutoModulation>(AutoModulations, newAutoMod));
            }

            return undos;
        }

        private List<IUndoRedo> CopyLocalAisacs(ILocalAisac localAisacs, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var aisac in localAisacs.LocalAisac)
            {
                if (GetAisac(aisac.TableGuid) == null && !aisac.IsNull)
                {
                    var aisacTable = copyAcb.GetAisac(aisac.TableGuid, false).Copy();

                    if (aisacTable.GraphIndexes.Count > 0)
                        undos.AddRange(CopyGraphs(aisacTable, copyAcb));

                    if (!aisacTable.AutoModulationIndex.IsNull)
                        undos.AddRange(CopyAutoModulation(aisacTable, copyAcb));

                    Aisacs.Add(aisacTable);
                    undos.Add(new UndoableListAdd<ACB_Aisac>(Aisacs, aisacTable));
                }
            }

            return undos;
        }

        private List<IUndoRedo> CopyGlobalAisacs(IGlobalAisacRef globalAisacs, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var aisac in globalAisacs.GlobalAisacRefs)
            {
                undos.AddRange(CopyGlobalAisac(copyAcb, aisac));
            }

            return undos;
        }

        private List<IUndoRedo> CopyGlobalAisac(ACB_File copyAcb, AcbTableReference aisac)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (GetGlobalAisacReference(aisac.TableGuid) == null && !aisac.IsNull)
            {
                var aisacTable = copyAcb.GetGlobalAisacReference(aisac.TableGuid, false).Copy();
                GlobalAisacReferences.Add(aisacTable);
                undos.Add(new UndoableListAdd<ACB_GlobalAisacReference>(GlobalAisacReferences, aisacTable));
            }

            return undos;
        }

        private List<IUndoRedo> CopyActionTracks(IActionTrack actionTracks, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var action in actionTracks.ActionTracks)
            {
                if (GetActionTrack(action.TableGuid) == null && !action.IsNull)
                {
                    var actionTable = copyAcb.GetActionTrack(action.TableGuid, false).Copy();

                    undos.AddRange(CopyLocalAisacs(actionTable, copyAcb));
                    undos.AddRange(CopyGlobalAisacs(actionTable, copyAcb));

                    if (!actionTable.CommandIndex.IsNull)
                        undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(actionTable.CommandIndex.TableGuid, CommandTableType.TrackEvent, false), copyAcb, CommandTableType.TrackEvent));

                    if (!actionTable.EventIndex.IsNull)
                        undos.AddRange(CopyCommand(copyAcb.CommandTables.GetCommand(actionTable.EventIndex.TableGuid, CommandTableType.TrackEvent, false), copyAcb, CommandTableType.TrackEvent));

                    ActionTracks.Add(actionTable);
                    undos.Add(new UndoableListAdd<ACB_Track>(ActionTracks, actionTable));
                }
            }

            return undos;
        }

        private List<IUndoRedo> CopyStringValue(AcbTableReference tableRef, ACB_File copyAcb)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var strValue = copyAcb.GetStringValue(tableRef.TableGuid);

            if(strValue != null)
            {
                var strValueInThisAcb = GetStringValue(strValue.StringValue);

                if(strValueInThisAcb != null)
                {
                    //Set the GUID
                    tableRef.TableGuid = strValueInThisAcb.InstanceGuid;
                }
                else
                {
                    //Add StringValue
                    StringValues.Add(strValue);
                    undos.Add(new UndoableListAdd<ACB_StringValue>(StringValues, strValue));
                }
            }

            return undos;
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
                synth.ActionTracks.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(synth.ActionTracks, newActionTracks, ActionTracks);

                //If the correct sequence of entries doesn't exist, then we must add them
                if(startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newActionTracks.Count;

                    for (int i = 0; i < synth.ActionTracks.Count; i++)
                    {
                        newActionTracks.Add(ActionTracks[synth.ActionTracks[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < synth.ActionTracks.Count; i++)
                {
                    synth.ActionTracks[i].TableIndex = (ushort)(startIndex + i);
                }
            }

            //Sequences
            foreach (var sequence in Sequences)
            {
                sequence.ActionTracks.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(sequence.ActionTracks, newActionTracks, ActionTracks);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newActionTracks.Count;

                    for (int i = 0; i < sequence.ActionTracks.Count; i++)
                    {
                        newActionTracks.Add(ActionTracks[sequence.ActionTracks[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < sequence.ActionTracks.Count; i++)
                {
                    sequence.ActionTracks[i].TableIndex = (ushort)(startIndex + i);
                }
            }

        }
        
        private void SortGlobalAisacRefs()
        {
            List<ACB_GlobalAisacReference> newAisacRefs = new List<ACB_GlobalAisacReference>();

            //Synths
            foreach (var synth in Synths)
            {
                synth.GlobalAisacRefs.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(synth.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < synth.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[synth.GlobalAisacRefs[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < synth.GlobalAisacRefs.Count; i++)
                {
                    synth.GlobalAisacRefs[i].TableIndex = (ushort)(startIndex + i);
                }
            }

            //Sequences
            foreach (var sequence in Sequences)
            {
                sequence.GlobalAisacRefs.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(sequence.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < sequence.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[sequence.GlobalAisacRefs[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < sequence.GlobalAisacRefs.Count; i++)
                {
                    sequence.GlobalAisacRefs[i].TableIndex = (ushort)(startIndex + i);
                }
            }

            //Tracks
            foreach (var track in Tracks)
            {
                track.GlobalAisacRefs.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(track.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[track.GlobalAisacRefs[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                {
                    track.GlobalAisacRefs[i].TableIndex = (ushort)(startIndex + i);
                }
            }

            //ActionTracks
            foreach (var track in ActionTracks)
            {
                track.GlobalAisacRefs.Sort((x, y) => x.TableIndex_Int - y.TableIndex_Int);
                ushort startIndex = CheckTableSequence(track.GlobalAisacRefs, newAisacRefs, GlobalAisacReferences);

                //If the correct sequence of entries doesn't exist, then we must add them
                if (startIndex == ushort.MaxValue)
                {
                    startIndex = (ushort)newAisacRefs.Count;

                    for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                    {
                        newAisacRefs.Add(GlobalAisacReferences[track.GlobalAisacRefs[i].TableIndex_Int]);
                    }
                }

                //Set new indexes
                for (int i = 0; i < track.GlobalAisacRefs.Count; i++)
                {
                    track.GlobalAisacRefs[i].TableIndex = (ushort)(startIndex + i);
                }
            }
        }
        
        private ushort CheckTableSequence<T>(List<AcbTableReference> idx, List<T> newList, List<T> mainList) where T : AcbTableBase
        {
            if (idx.Count == 0) return ushort.MaxValue;

            for (int i = 0; i < newList.Count; i++)
            {
                if(idx[0].TableIndex_Int >= 0 && idx[0].TableIndex_Int <= mainList.Count - 1)
                {
                    if (newList[i] == mainList[idx[0].TableIndex_Int])
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
            }

            return ushort.MaxValue;
        }

        /// <summary>
        /// Creates new InstanceGuids for all tables and updates all references.
        /// </summary>
        internal void NewInstanceGuids()
        {
            NewInstanceGuids_EachTable(Cues);
            NewInstanceGuids_EachTable(Waveforms);
            NewInstanceGuids_EachTable(Aisacs);
            NewInstanceGuids_EachTable(Graphs);
            NewInstanceGuids_EachTable(GlobalAisacReferences);
            NewInstanceGuids_EachTable(Synths);
            NewInstanceGuids_EachTable(Tracks);
            NewInstanceGuids_EachTable(Sequences);
            NewInstanceGuids_EachTable(AutoModulations);
            NewInstanceGuids_EachTable(ActionTracks);

            foreach (var commandTable in CommandTables.CommandTables)
                NewInstanceGuids_EachTable(commandTable.CommandGroups);
        }

        private void NewInstanceGuids_EachTable<T>(IList<T> table) where T : AcbTableBase
        {
            foreach(var entry in table)
            {
                Guid oldGuid = entry.InstanceGuid;
                entry.InstanceGuid = Guid.NewGuid();
                InstanceGuidRefactor(oldGuid, entry.InstanceGuid);
            }
        }

        /// <summary>
        /// Changes all references to the specified InstanceGuid with the new GUID (does not change the actual InstanceGuid on AcbTableBase, just references via AcbTableReference).
        /// </summary>
        /// <returns></returns>
        public void InstanceGuidRefactor(Guid oldGuid, Guid newGuid)
        {
            InstanceGuidRefactor_Reflection(Cues, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(Sequences, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(Synths, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(Waveforms, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(Tracks, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(ActionTracks, oldGuid, newGuid);
            InstanceGuidRefactor_Reflection(Aisacs, oldGuid, newGuid);
            //InstanceGuidRefactor_Reflection(GlobalAisacReferences, oldGuid, newGuid);
            //InstanceGuidRefactor_Reflection(AutoModulations, oldGuid, newGuid);
            //InstanceGuidRefactor_Reflection(Graphs, oldGuid, newGuid);

            foreach (var commandTable in CommandTables.CommandTables)
                foreach (var commandGroup in commandTable.CommandGroups)
                    InstanceGuidRefactor_Reflection(commandGroup.Commands, oldGuid, newGuid);
            
        }

        private void InstanceGuidRefactor_Reflection<T>(IList<T> entries, Guid oldGuid, Guid newGuid) where T : class
        {
            foreach (var entry in entries)
            {
                InstanceGuidRefactor_ReflectionRecursive(entry, oldGuid, newGuid);
            }
        }

        private void InstanceGuidRefactor_ReflectionRecursive<T>(T entry, Guid oldGuid, Guid newGuid)
        {
            if (entry == null) return;
            var props = entry.GetType().GetProperties();

            foreach (var prop in props.Where(x => x.Name != "InstanceGuid"))
            {
                if (prop.PropertyType == typeof(Guid))
                {
                    if ((Guid)prop.GetValue(entry) == oldGuid)
                    {
                        prop.SetValue(entry, newGuid);
                    }
                }
                else if (prop.PropertyType == typeof(AcbTableReference))
                {
                    InstanceGuidRefactor_ReflectionRecursive(prop.GetValue(entry) as AcbTableReference, oldGuid, newGuid);
                }
                else if (prop.PropertyType == typeof(List<AcbTableReference>))
                {
                    InstanceGuidRefactor_Reflection(prop.GetValue(entry) as List<AcbTableReference>, oldGuid, newGuid);
                }
                else if (prop.PropertyType == typeof(AsyncObservableCollection<ACB_SequenceTrack>))
                {
                    InstanceGuidRefactor_Reflection(prop.GetValue(entry) as AsyncObservableCollection<ACB_SequenceTrack>, oldGuid, newGuid);
                }
                else if (prop.PropertyType == typeof(List<ACB_ReferenceItem>))
                {
                    InstanceGuidRefactor_Reflection(prop.GetValue(entry) as List<ACB_ReferenceItem>, oldGuid, newGuid);
                }
            }
            
        }

        #endregion

        #region MiscPrivateFunctions
        private AFS2_File GenerateAwbFile(bool streamingAwb)
        {
            //Get list of tracks to include
            List<int> tracks = new List<int>();

            if(SaveFormat == SaveFormat.Default)
            {
                foreach (var waveforms in Waveforms.Where(w => w.Streaming == streamingAwb && w.AwbId != ushort.MaxValue))
                    tracks.Add(waveforms.AwbId);
            }
            else if(SaveFormat == SaveFormat.MusicPackage && !streamingAwb)
            {
                //All tracks are stored internally, ignoring the streaming flag.
                foreach (var waveforms in Waveforms.Where(w => w.AwbId != ushort.MaxValue))
                    tracks.Add(waveforms.AwbId);
            }

            //If there are no tracks then dont create a awb file.
            if (tracks.Count == 0)
                return null;

            //Create the AWB file
            AFS2_File awbFile = AFS2_File.CreateNewAwbFile();

            foreach(var track in AudioTracks.Entries)
            {
                if (tracks.Contains(track.AwbId))
                {
                    awbFile.Entries.Add(track);
                }
            }

            //For EAT compatibility
            awbFile.PadWithNullEntries();
            return awbFile;
        }
        
        /// <summary>
        /// Removes unused tables. (Except Cues and Awb Entries)
        /// </summary>
        /// <returns></returns>
        public List<IUndoRedo> CleanUpTables()
        {
            //Operation must be undoable to preserve any deletes in the undo history
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //We must loop over them all multiple times to ensure every unused table is caught, as some unusued tables that get removed later on may reference an earlier one
            for (int i = 0; i < 11; i++)
            {
                bool deleted = false;

                undos.AddRange(CleanUpTable(Sequences, ref deleted));
                undos.AddRange(CleanUpTable(Synths, ref deleted));
                undos.AddRange(CleanUpTable(Waveforms, ref deleted));
                undos.AddRange(CleanUpTable(Tracks, ref deleted));
                undos.AddRange(CleanUpTable(ActionTracks, ref deleted));
                undos.AddRange(CleanUpTable(Aisacs, ref deleted));
                undos.AddRange(CleanUpTable(GlobalAisacReferences, ref deleted));
                undos.AddRange(CleanUpTable(Graphs, ref deleted));
                undos.AddRange(CleanUpTable(AutoModulations, ref deleted));
                //StringValues dont get deleted (some commands require them and they are small and harmless by themselves)

                foreach (var commandTable in CommandTables.CommandTables)
                    undos.AddRange(CleanUpTable(commandTable.CommandGroups, ref deleted));

                //If no table was deleted in this loop, end here
                if (!deleted)
                    break;
            }
            
            return undos;
        }

        private List<IUndoRedo> CleanUpTable<T>(IList<T> entries, ref bool deleted) where T : AcbTableBase
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if(!IsTableUsed(entries[i].InstanceGuid))
                {
                    T original = entries[i];
                    entries.Remove(entries[i]);
                    undos.Add(new UndoableListRemove<T>(entries, original, i));
                    deleted = true;
                }
            }

            return undos;
        }
        
        /// <summary>
        /// Ensure that all ACB_SequenceTrack Percentage values do not exceed 100.
        /// </summary>
        private void TrimSequenceTrackPercentages()
        {
            foreach (var sequence in Sequences)
            {
                sequence.AdjustTrackPercentage();
            }

            foreach (var sequence in BlockSequences)
            {
                sequence.AdjustTrackPercentage();
            }
        }
        
        private void SortCues()
        {
            Cues.Sort((x, y) => (int)x.ID - (int)y.ID);
        }

        #endregion

        #region TableReferenceLinking
        /// <summary>
        /// Link the AcbTableReferences with the InstanceGuid of its matching TableIndex. (Call this once, after loading)
        /// </summary>
        private void LinkTableGuids()
        {
            LinkRefItemsGuid(Cues);

            LinkMultiRefItemsGuid(Synths);
            LinkCommandIndexGuid(Synths, CommandTableType.SynthCommand);
            LinkLocalAisacGuid(Synths);
            LinkGlobalAisacGuid(Synths);
            LinkActionTrackGuid(Synths);

            LinkTrackGuid(Sequences);
            LinkCommandIndexGuid(Sequences, CommandTableType.SequenceCommand);
            LinkLocalAisacGuid(Sequences);
            LinkGlobalAisacGuid(Sequences);
            LinkActionTrackGuid(Sequences);

            LinkLocalAisacGuid(Tracks);
            LinkGlobalAisacGuid(Tracks);
            LinkEventIndexGuid(Tracks, CommandTableType.TrackEvent);
            LinkCommandIndexGuid(Tracks, CommandTableType.TrackCommand);

            LinkLocalAisacGuid(ActionTracks);
            LinkGlobalAisacGuid(ActionTracks);
            LinkEventIndexGuid(ActionTracks, CommandTableType.TrackEvent);
            LinkCommandIndexGuid(ActionTracks, CommandTableType.TrackEvent);
            LinkTargetIdGuid(ActionTracks);

            LinkAutoModulationIndexGuid(Aisacs);
            LinkGraphIndexesGuid(Aisacs);

            CommandTables.LinkGuids(this);
        }

        /// <summary>
        /// Link the AcbTableReferences with the updated table index of its matching InstanceGuid. (Call this once, right before saving)
        /// </summary>
        private void LinkTableIndexes()
        {
            LinkRefItemsIndex(Cues);

            LinkMultiRefItemsIndex(Synths);
            LinkCommandIndexIndex(Synths, CommandTableType.SynthCommand);
            LinkLocalAisacIndex(Synths);
            LinkGlobalAisacIndex(Synths);
            LinkActionTrackIndex(Synths);

            LinkTrackIndex(Sequences);
            LinkCommandIndexIndex(Sequences, CommandTableType.SequenceCommand);
            LinkLocalAisacIndex(Sequences);
            LinkGlobalAisacIndex(Sequences);
            LinkActionTrackIndex(Sequences);

            LinkLocalAisacIndex(Tracks);
            LinkGlobalAisacIndex(Tracks);
            LinkEventIndexIndex(Tracks, CommandTableType.TrackEvent);
            LinkCommandIndexIndex(Tracks, CommandTableType.TrackCommand);

            LinkLocalAisacIndex(ActionTracks);
            LinkGlobalAisacIndex(ActionTracks);
            LinkEventIndexIndex(ActionTracks, CommandTableType.TrackEvent);
            LinkCommandIndexIndex(ActionTracks, CommandTableType.TrackEvent);
            LinkActionTargetIdIndex(ActionTracks);

            LinkAutoModulationIndexIndex(Aisacs);
            LinkGraphIndexesIndex(Aisacs);
            CommandTables.LinkIndexes(this);
        }

        //Guid
        private void LinkMultiRefItemsGuid<T>(IList<T> table) where T : IReferenceItems
        {
            foreach (var entry in table)
            {
                LinkRefItemsGuid(entry.ReferenceItems);
            }
        }

        private void LinkRefItemsGuid<T>(IList<T> table) where T : IReferenceType
        {
            foreach(var entry in table)
            {
                switch (entry.ReferenceType)
                {
                    case ReferenceType.Waveform:
                        entry.ReferenceIndex.TableGuid = GetTableGuid(entry.ReferenceIndex.TableIndex_Int, Waveforms);
                        break;
                    case ReferenceType.Sequence:
                        entry.ReferenceIndex.TableGuid = GetTableGuid(entry.ReferenceIndex.TableIndex_Int, Sequences);
                        break;
                    case ReferenceType.Synth:
                        entry.ReferenceIndex.TableGuid = GetTableGuid(entry.ReferenceIndex.TableIndex_Int, Synths);
                        break;
                }
            }
        }

        private void LinkLocalAisacGuid<T>(IList<T> table) where T : ILocalAisac
        {
            foreach (var entry in table)
            {
                foreach (var aisac in entry.LocalAisac)
                    aisac.TableGuid = GetTableGuid(aisac.TableIndex_Int, Aisacs);

                RemoveUnlinkedReferences(entry.LocalAisac);
            }
        }

        private void LinkGlobalAisacGuid<T>(IList<T> table) where T : IGlobalAisacRef
        {
            foreach (var entry in table)
            {
                foreach (var aisac in entry.GlobalAisacRefs)
                    aisac.TableGuid = GetTableGuid(aisac.TableIndex_Int, GlobalAisacReferences);

                RemoveUnlinkedReferences(entry.GlobalAisacRefs);
            }
        }

        private void LinkCommandIndexGuid<T>(IList<T> table, CommandTableType commandTableType) where T : ICommandIndex
        {
            foreach (var entry in table)
            {
                entry.CommandIndex.TableGuid = CommandTables.GetCommandGuid(entry.CommandIndex.TableIndex_Int, commandTableType);
            }
        }

        private void LinkEventIndexGuid<T>(IList<T> table, CommandTableType commandTableType) where T : IEventIndex
        {
            foreach (var entry in table)
            {
                entry.EventIndex.TableGuid = CommandTables.GetCommandGuid(entry.EventIndex.TableIndex_Int, commandTableType);
            }
        }

        private void LinkActionTrackGuid<T>(IList<T> table) where T : IActionTrack
        {
            foreach (var entry in table)
            {
                foreach (var track in entry.ActionTracks)
                    track.TableGuid = GetTableGuid(track.TableIndex_Int, ActionTracks);
            }
        }

        private void LinkTrackGuid<T>(IList<T> table) where T : ITrack
        {
            foreach (var entry in table)
            {
                foreach (var track in entry.Tracks)
                {
                    track.Index.TableGuid = GetTableGuid(track.Index.TableIndex_Int, Tracks);
                }

                //Remove unlinked tracks
                for (int i = entry.Tracks.Count - 1; i >= 0; i--)
                {
                    if (entry.Tracks[i].Index.IsNull)
                        entry.Tracks.RemoveAt(i);
                }
            }
        }

        private void LinkAutoModulationIndexGuid<T>(IList<T> table) where T : IAutoModulationIndex
        {
            foreach (var entry in table)
                entry.AutoModulationIndex.TableGuid = GetTableGuid(entry.AutoModulationIndex.TableIndex_Int, AutoModulations);
        }

        private void LinkGraphIndexesGuid<T>(IList<T> table) where T : IGraphIndexes
        {
            foreach (var entry in table)
            {
                foreach (var graph in entry.GraphIndexes)
                    graph.TableGuid = GetTableGuid(graph.TableIndex_Int, Graphs);

                RemoveUnlinkedReferences(entry.GraphIndexes);
            }
        }

        private void LinkTargetIdGuid<T>(IList<T> table) where T : ITargetId
        {
            foreach (var entry in table)
            {
                if(entry.TargetSelf && entry.TargetType == TargetType.SpecificAcb && entry.TargetId.TableIndex != uint.MaxValue)
                {
                    entry.TargetId.TableGuid = GetCueGuid(entry.TargetId.TableIndex_Int);
                }
            }
        }

        private void RemoveUnlinkedReferences(IList<AcbTableReference> references)
        {
            for (int i = references.Count - 1; i >= 0; i--)
            {
                if (references[i].IsNull)
                    references.RemoveAt(i);
            }
        }

        //Index
        private void LinkMultiRefItemsIndex<T>(IList<T> table) where T : IReferenceItems
        {
            foreach (var entry in table)
            {
                LinkRefItemsIndex(entry.ReferenceItems);
            }
        }

        private void LinkRefItemsIndex<T>(IList<T> table) where T : IReferenceType
        {
            foreach (var entry in table)
            {
                switch (entry.ReferenceType)
                {
                    case ReferenceType.Waveform:
                        entry.ReferenceIndex.TableIndex = (ushort)GetTableIndex(entry.ReferenceIndex.TableGuid, Waveforms);
                        break;
                    case ReferenceType.Sequence:
                        entry.ReferenceIndex.TableIndex = (ushort)GetTableIndex(entry.ReferenceIndex.TableGuid, Sequences);
                        break;
                    case ReferenceType.Synth:
                        entry.ReferenceIndex.TableIndex = (ushort)GetTableIndex(entry.ReferenceIndex.TableGuid, Synths);
                        break;
                }
            }
        }

        private void LinkLocalAisacIndex<T>(IList<T> table) where T : ILocalAisac
        {
            foreach (var entry in table)
            {
                foreach (var aisac in entry.LocalAisac)
                    aisac.TableIndex = GetTableIndex(aisac.TableGuid, Aisacs);
            }
        }

        private void LinkGlobalAisacIndex<T>(IList<T> table) where T : IGlobalAisacRef
        {
            foreach (var entry in table)
            {
                foreach (var aisac in entry.GlobalAisacRefs)
                    aisac.TableIndex = GetTableIndex(aisac.TableGuid, GlobalAisacReferences);
            }
        }

        private void LinkCommandIndexIndex<T>(IList<T> table, CommandTableType commandTableType) where T : ICommandIndex
        {
            foreach (var entry in table)
            {
                entry.CommandIndex.TableIndex = CommandTables.GetCommandIndex(entry.CommandIndex.TableGuid, commandTableType);
            }
        }

        private void LinkEventIndexIndex<T>(IList<T> table, CommandTableType commandTableType) where T : IEventIndex
        {
            foreach (var entry in table)
            {
                entry.EventIndex.TableIndex = CommandTables.GetCommandIndex(entry.EventIndex.TableGuid, commandTableType);
            }
        }

        private void LinkActionTrackIndex<T>(IList<T> table) where T : IActionTrack
        {
            foreach (var entry in table)
            {
                foreach (var track in entry.ActionTracks)
                    track.TableIndex = GetTableIndex(track.TableGuid, ActionTracks);
            }
        }

        private void LinkTrackIndex<T>(IList<T> table) where T : ITrack
        {
            foreach (var entry in table)
            {
                foreach (var track in entry.Tracks)
                    track.Index.TableIndex = GetTableIndex(track.Index.TableGuid, Tracks);
            }
        }

        private void LinkAutoModulationIndexIndex<T>(IList<T> table) where T : IAutoModulationIndex
        {
            foreach (var entry in table)
                entry.AutoModulationIndex.TableIndex = GetTableIndex(entry.AutoModulationIndex.TableGuid, AutoModulations);
        }

        private void LinkGraphIndexesIndex<T>(IList<T> table) where T : IGraphIndexes
        {
            foreach (var entry in table)
            {
                foreach (var graph in entry.GraphIndexes)
                    graph.TableIndex = GetTableIndex(graph.TableGuid, Graphs);
            }
        }

        private void LinkActionTargetIdIndex(IList<ACB_Track> table)
        {
            foreach (var entry in table)
            {
                if(entry.TargetType == TargetType.SpecificAcb)
                    entry.TargetId.TableIndex = GetCueId(entry.TargetId.TableGuid);
            }
        }


        //Helper
        internal uint GetCueId(Guid cueGuid)
        {
            var cue = Cues.FirstOrDefault(x => x.InstanceGuid == cueGuid);
            return (cue != null) ? cue.ID : uint.MaxValue;
        }

        internal Guid GetCueGuid(int cueId)
        {
            var cue = Cues.FirstOrDefault(x => x.ID == cueId);
            return (cue != null) ? cue.InstanceGuid : Guid.Empty;
        }

        internal Guid GetTableGuid<T>(int index, IList<T> table) where T : AcbTableBase
        {
            if (index >= ushort.MaxValue) return Guid.Empty;
            if (index >= table.Count || index < 0) return Guid.Empty;
            //if (index >= table.Count || index < 0) throw new ArgumentOutOfRangeException($"ACB_File.GetTableGuid: Cannot find entry at index {index}. Value is out of range.");
            return table[index].InstanceGuid;
        }

        internal ushort GetTableIndex<T>(Guid guid, IList<T> table) where T : AcbTableBase
        {
            var entry = table.FirstOrDefault(x => x.InstanceGuid == guid);
            return (entry != null) ? (ushort)table.IndexOf(entry) : ushort.MaxValue;
            //if (entry == null) throw new ArgumentOutOfRangeException($"ACB_File.GetTableIndex: Cannot find entry with guid {guid}.");
        }
        
        #endregion

        #region DefaultTableFunctions
        internal UTF_File CreateDefaultHeaderTable()
        {
            UTF_File utfFile = AcbFormatHelper.Instance.AcbFormatHelperMain.Header.CreateTable(Version, "Header");

            //Added for MusicPackage
            if(SaveFormat == SaveFormat.MusicPackage && !utfFile.ColumnExists("MusicPackageType"))
                utfFile.Columns.Add(new UTF_Column("MusicPackageType", TypeFlag.Int32));

            return utfFile;
        }

        internal UTF_File CreateDefaultCueTable()
        {
            return AcbFormatHelper.Instance.CreateTable("CueTable", "Cue", Version);
        }

        internal UTF_File CreateDefaultCueNameTable()
        {
            return AcbFormatHelper.Instance.CreateTable("CueNameTable", "CueName", Version);
        }

        internal UTF_File CreateDefaultWaveformTable()
        {
            return AcbFormatHelper.Instance.CreateTable("WaveformTable", "Waveform", Version);
        }

        internal UTF_File CreateDefaultAisacTable()
        {
            return AcbFormatHelper.Instance.CreateTable("AisacTable", "Aisac", Version);
        }

        internal UTF_File CreateDefaultGraphTable()
        {
            return AcbFormatHelper.Instance.CreateTable("GraphTable", "Graph", Version);
        }

        internal UTF_File CreateDefaultGlobalAisacReferenceTable()
        {
            return AcbFormatHelper.Instance.CreateTable("GlobalAisacReferenceTable", "GlobalAisacReference", Version);
        }

        internal UTF_File CreateDefaultAisacControlNameTable()
        {
            return AcbFormatHelper.Instance.CreateTable("AisacControlNameTable", "AisacControlName", Version);
        }

        internal UTF_File CreateDefaultSynthTable()
        {
            return AcbFormatHelper.Instance.CreateTable("SynthTable", "Synth", Version);
        }
        
        internal UTF_File CreateDefaultSequenceTable()
        {
            return AcbFormatHelper.Instance.CreateTable("SequenceTable", "Sequence", Version);
        }

        internal UTF_File CreateDefaultTrackTable()
        {
            return AcbFormatHelper.Instance.CreateTable("TrackTable", "Track", Version);
        }

        internal UTF_File CreateDefaultActionTrackTable()
        {
            return AcbFormatHelper.Instance.CreateTable("ActionTrackTable", "ActionTrack", Version);
        }

        internal UTF_File CreateDefaultAutoModulationTable()
        {
            return AcbFormatHelper.Instance.CreateTable("AutoModulationTable", "AutoModulation", Version);
        }

        internal UTF_File CreateDefaultWaveformExtensionTable()
        {
            return AcbFormatHelper.Instance.CreateTable("WaveformExtensionDataTable", "WaveformExtensionData", Version);
        }
        
        internal UTF_File CreateDefaultStreamAwbHashTable()
        {
            return AcbFormatHelper.Instance.CreateTable("StreamAwbHash", "StreamAwb", Version);
        }

        internal UTF_File CreateDefaultStreamAwbHeaderTable()
        {
            return AcbFormatHelper.Instance.CreateTable("StreamAwbAfs2Header", "StreamAwb", Version);
        }

        internal UTF_File CreateDefaultStringValueTable()
        {
            return AcbFormatHelper.Instance.CreateTable("StringValueTable", "Strings", Version);
        }

        #endregion

        #region GetHelpers
        public ACB_Cue GetCue(string name) { return Cues.FirstOrDefault(x => x.Name == name); }
        public ACB_Cue GetCue(int cueId) { return Cues.FirstOrDefault(x=>x.ID == (uint)cueId); }
        public ACB_Cue GetCue(Guid guid, bool allowNull = true) { return GetTable(guid, Cues, allowNull); }
        public ACB_Sequence GetSequence(Guid guid, bool allowNull = true) { return GetTable(guid, Sequences, allowNull); }
        public ACB_Synth GetSynth(Guid guid, bool allowNull = true) { return GetTable(guid, Synths, allowNull); }
        public ACB_Waveform GetWaveform(Guid guid, bool allowNull = true) { return GetTable(guid, Waveforms, allowNull); }
        public ACB_Track GetTrack(Guid guid, bool allowNull = true) { return GetTable(guid, Tracks, allowNull); }
        public ACB_Track GetActionTrack(Guid guid, bool allowNull = true) { return GetTable(guid, ActionTracks, allowNull); }
        public ACB_Aisac GetAisac(Guid guid, bool allowNull = true) { return GetTable(guid, Aisacs, allowNull); }
        public ACB_GlobalAisacReference GetGlobalAisacReference(Guid guid, bool allowNull = true) { return GetTable(guid, GlobalAisacReferences, allowNull); }
        public ACB_AutoModulation GetAutoModulation(Guid guid, bool allowNull = true) { return GetTable(guid, AutoModulations, allowNull); }
        public ACB_Graph GetGraph(Guid guid, bool allowNull = true) { return GetTable(guid, Graphs, allowNull); }
        public ACB_StringValue GetStringValue(Guid guid, bool allowNull = true) { return GetTable(guid, StringValues, allowNull); }
        public ACB_StringValue GetStringValue(string stringValue) { return StringValues.FirstOrDefault(x => x.StringValue == stringValue); }
        public ACB_GlobalAisacReference GetGlobalAisacReference(string globalAisacValue) { return GlobalAisacReferences.FirstOrDefault(x => x.Name == globalAisacValue); }

        public AFS2_Entry GetAfs2Entry(int id, bool allowNull = true)
        {
            var entry = AudioTracks.GetEntry((ushort)id);
            if (entry == null && !allowNull)
                throw new InvalidDataException(string.Format("ACB_File.GetAfs2Entry: Could not find the AWB entry with ID {0}.", id));

            return entry;
        }
        
        /// <summary>
        /// Check if a table is referenced by any other table.
        /// </summary>
        /// <param name="oldGuid">The guid of the table to check for.</param>
        /// <returns></returns>
        public bool IsTableUsed(Guid tableGuid)
        {
            if (IsTableUsed_Reflection(Cues, tableGuid)) return true;
            if (IsTableUsed_Reflection(Sequences, tableGuid)) return true;
            if (IsTableUsed_Reflection(Synths, tableGuid)) return true;
            if (IsTableUsed_Reflection(Waveforms, tableGuid)) return true;
            if (IsTableUsed_Reflection(Tracks, tableGuid)) return true;
            if (IsTableUsed_Reflection(ActionTracks, tableGuid)) return true;
            if (IsTableUsed_Reflection(Aisacs, tableGuid)) return true;

            foreach (var commandTable in CommandTables.CommandTables)
                foreach(var commandGroup in commandTable.CommandGroups)
                    if (IsTableUsed_Reflection(commandGroup.Commands, tableGuid)) return true;

            return false;
        }
        
        private bool IsTableUsed_Reflection<T>(IList<T> entries, Guid tableGuid) where T : class
        {
            foreach(var entry in entries)
            {
                if (IsTableUsed_ReflectionRecursive(entry, tableGuid))
                    return true;
            }
            return false;
        }
        
        private bool IsTableUsed_ReflectionRecursive<T>(T entry, Guid tableGuid)
        {
            if (entry == null) return false;
            var props = entry.GetType().GetProperties();
            
            foreach(var prop in props.Where(x => x.Name != "InstanceGuid"))
            {
                if(prop.PropertyType == typeof(Guid))
                {
                    if ((Guid)prop.GetValue(entry) == tableGuid) return true;
                }
                else if(prop.PropertyType == typeof(AcbTableReference))
                {
                    if (IsTableUsed_ReflectionRecursive(prop.GetValue(entry) as AcbTableReference, tableGuid)) return true;
                }
                else if (prop.PropertyType == typeof(List<AcbTableReference>))
                {
                    if (IsTableUsed_Reflection(prop.GetValue(entry) as List<AcbTableReference>, tableGuid)) return true;
                }
                else if (prop.PropertyType == typeof(AsyncObservableCollection<ACB_SequenceTrack>))
                {
                    if (IsTableUsed_Reflection(prop.GetValue(entry) as AsyncObservableCollection<ACB_SequenceTrack>, tableGuid)) return true;
                }
                else if (prop.PropertyType == typeof(List<ACB_ReferenceItem>))
                {
                    if (IsTableUsed_Reflection(prop.GetValue(entry) as List<ACB_ReferenceItem>, tableGuid)) return true;
                }
            }

            return false;
        }

        public T GetTable<T>(Guid guid, IList<T> tables, bool allowNull) where T : AcbTableBase
        {
            var result = tables.FirstOrDefault(x => x.InstanceGuid == guid);

            if (result == null && !allowNull)
                throw new InvalidDataException(string.Format("ACB_File.GetTable: Could not find the {0} table with the specified GUID.", typeof(T)));

            return result;
        }


        //GetWaveformFromCue
        public List<ACB_Waveform> GetWaveformsFromCue(ACB_Cue cue)
        {
            List<ACB_Waveform> waveforms = new List<ACB_Waveform>();
            if (cue.ReferenceIndex.IsNull) return waveforms;

            if(cue.ReferenceType == ReferenceType.Waveform)
            {
                waveforms.Add(GetWaveform(cue.ReferenceIndex.TableGuid));
            }
            else if(cue.ReferenceType == ReferenceType.Synth)
            {
                waveforms.AddRange(GetWaveformsFromSynth(GetSynth(cue.ReferenceIndex.TableGuid)));
            }
            else if (cue.ReferenceType == ReferenceType.Sequence)
            {
                waveforms.AddRange(GetWaveformsFromSequence(GetSequence(cue.ReferenceIndex.TableGuid)));
            }

            return waveforms;
        }

        private List<ACB_Waveform> GetWaveformsFromSequence(ACB_Sequence sequence)
        {
            List<ACB_Waveform> waveforms = new List<ACB_Waveform>();

            foreach(var track in sequence.Tracks)
                if(!track.Index.IsNull)
                    waveforms.AddRange(GetWaveformsFromTrack(GetTrack(track.Index.TableGuid)));

            return waveforms;
        }

        private List<ACB_Waveform> GetWaveformsFromSynth(ACB_Synth synth)
        {
            List<ACB_Waveform> waveforms = new List<ACB_Waveform>();

            foreach(var refItem in synth.ReferenceItems)
            {
                waveforms.AddRange(GetWaveformsFromReferenceItem(refItem));
            }

            return waveforms;
        }

        public List<ACB_Waveform> GetWaveformsFromReferenceItem(ACB_ReferenceItem refItem)
        {
            List<ACB_Waveform> waveforms = new List<ACB_Waveform>();
            if (refItem.ReferenceIndex.IsNull) return waveforms;

                switch (refItem.ReferenceType)
                {
                    case ReferenceType.Waveform:
                        waveforms.Add(GetWaveform(refItem.ReferenceIndex.TableGuid));
                        break;
                    case ReferenceType.Sequence:
                        waveforms.AddRange(GetWaveformsFromSequence(GetSequence(refItem.ReferenceIndex.TableGuid)));
                        break;
                    case ReferenceType.Synth:
                        waveforms.AddRange(GetWaveformsFromSynth(GetSynth(refItem.ReferenceIndex.TableGuid)));
                        break;
                }

            return waveforms;
        }

        public List<ACB_Waveform> GetWaveformsFromTrack(ACB_Track track)
        {
            List<ACB_Waveform> waveforms = new List<ACB_Waveform>();

            if (!track.EventIndex.IsNull)
            {
                var command = CommandTables.GetCommand(track.EventIndex.TableGuid, CommandTableType.TrackEvent);
                if(command != null)
                {
                    foreach(var cmd in command.Commands.Where(x => (x.CommandType == CommandType.ReferenceItem || x.CommandType == CommandType.ReferenceItem2) && x.ReferenceIndex != null))
                    {
                        switch (cmd.ReferenceType)
                        {
                            case ReferenceType.Waveform:
                                waveforms.Add(GetWaveform(cmd.ReferenceIndex.TableGuid));
                                break;
                            case ReferenceType.Sequence:
                                waveforms.AddRange(GetWaveformsFromSequence(GetSequence(cmd.ReferenceIndex.TableGuid)));
                                break;
                            case ReferenceType.Synth:
                                waveforms.AddRange(GetWaveformsFromSynth(GetSynth(cmd.ReferenceIndex.TableGuid)));
                                break;
                        }
                    }
                }
            }

            return waveforms;
        }
        #endregion

        public int GetFreeCueId()
        {
            int id = 0;
            while (Cues.Exists(c => c.ID == id) && id < int.MaxValue)
                id++;
            return id;
        }
    
        
    }

    [YAXSerializeAs("Cue")]
    [Serializable]
    public class ACB_Cue : AcbTableBase, IReferenceType
    {

        #region Undoable
        [YAXDontSerialize]
        public string UndoableName
        {
            get
            {
                return Name;
            }
            set
            {
                if (Name != value)
                {
                    string oldValue = Name;
                    Name = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Cue>("Name", this, oldValue, value, "Cue Name"));
                    NotifyPropertyChanged("UndoableName");
                }
            }
        }

        #endregion

        [YAXAttributeForClass]
        public uint ID { get; set; } //CueID (uint)
        private string _cueName = string.Empty;
        [YAXAttributeForClass]
        public string Name { get { return _cueName; } set { _cueName = value; NotifyPropertyChanged("UndoableName"); } }
        [YAXAttributeFor("UserData")]
        [YAXSerializeAs("value")]
        public string UserData { get; set; } = string.Empty;
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
        [YAXSerializeAs("ReferenceIndex")]
        public AcbTableReference ReferenceIndex { get; set; } = new AcbTableReference(); //ushort
        [YAXAttributeFor("Length")]
        [YAXSerializeAs("value")]
        public uint Length { get; set; }
        [YAXAttributeFor("NumAisacControlMaps")]
        [YAXSerializeAs("value")]
        public uint NumAisacControlMaps { get; set; }
        [YAXAttributeFor("HeaderVisibility")]
        [YAXSerializeAs("value")]
        public byte HeaderVisibility { get; set; }

        public void Initialize()
        {
            if (ReferenceIndex == null) ReferenceIndex = new AcbTableReference();
        }

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("CueTable");

            //Cue data
            cue.ID = cueTable.GetValue<uint>("CueId", TypeFlag.UInt32, index, tableHelper, ParseVersion);
            cue.UserData = cueTable.GetValue<string>("UserData", TypeFlag.String, index, tableHelper, ParseVersion);
            cue.Worksize = cueTable.GetValue<ushort>("Worksize", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            cue.AisacControlMap = cueTable.GetData("AisacControlMap", index, tableHelper, ParseVersion);
            cue.Length = cueTable.GetValue<uint>("Length", TypeFlag.UInt32, index, tableHelper, ParseVersion);
            cue.NumAisacControlMaps = cueTable.GetValue<byte>("NumAisacControlMaps", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            cue.HeaderVisibility = cueTable.GetValue<byte>("HeaderVisibility", TypeFlag.UInt8, index, tableHelper, ParseVersion);

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

            //ReferenceItem
            cue.ReferenceType = (ReferenceType)cueTable.GetValue<byte>("ReferenceType", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            cue.ReferenceIndex = new AcbTableReference(cueTable.GetValue<ushort>("ReferenceIndex", TypeFlag.UInt16, index, tableHelper, ParseVersion));

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("CueTable");

            utfTable.AddValue("CueId", TypeFlag.UInt32, index, ID.ToString());
            utfTable.AddValue("UserData", TypeFlag.String, index, UserData, tableHelper, ParseVersion);
            utfTable.AddValue("Worksize", TypeFlag.UInt16, index, Worksize.ToString(), tableHelper, ParseVersion);
            utfTable.AddData("AisacControlMap", index, AisacControlMap, tableHelper, ParseVersion);
            utfTable.AddValue("Length", TypeFlag.UInt32, index, Length.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("NumAisacControlMaps", TypeFlag.UInt8, index, NumAisacControlMaps.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("HeaderVisibility", TypeFlag.UInt8, index, HeaderVisibility.ToString(), tableHelper, ParseVersion);
            
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
    [Serializable]
    public class ACB_Synth : AcbTableBase, IReferenceItems, ICommandIndex, ILocalAisac, IGlobalAisacRef, IActionTrack
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public byte Type { get; set; }
        [YAXAttributeFor("VoiceLimitGroupName")]
        [YAXSerializeAs("value")]
        public string VoiceLimitGroupName { get; set; } = string.Empty;
        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("TrackValues")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public byte[] TrackValues { get; set; }

        //ReferenceItems
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ReferenceItem")]
        public List<ACB_ReferenceItem> ReferenceItems { get; set; } = new List<ACB_ReferenceItem>();

        //CommandTable
        [YAXSerializeAs("CommandIndex")]
        public AcbTableReference CommandIndex { get; set; } = new AcbTableReference(); //ushort

        //LocalAisac
        [YAXSerializeAs("LocalAisac")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> LocalAisac { get; set; } = new List<AcbTableReference>(); //ushort

        //GlobalAisac
        [YAXSerializeAs("GlobalAisacRefs")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> GlobalAisacRefs { get; set; } = new List<AcbTableReference>(); //ushort

        //ActionTrack
        [YAXSerializeAs("ActionTracks")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> ActionTracks { get; set; } = new List<AcbTableReference>(); //ushort

        public ACB_Synth()
        {
            ReferenceItems.Add(new ACB_ReferenceItem((ushort)ReferenceType.Nothing, ushort.MaxValue));
        }

        public void Initialize()
        {
            if (ReferenceItems == null) ReferenceItems = new List<ACB_ReferenceItem>();
            if (ActionTracks == null) ActionTracks = new List<AcbTableReference>();
            if (CommandIndex == null) CommandIndex = new AcbTableReference();
            if (LocalAisac == null) LocalAisac = new List<AcbTableReference>();
            if (GlobalAisacRefs == null) GlobalAisacRefs = new List<AcbTableReference>();
        }

        public static List<ACB_Synth> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_Synth> rows = new List<ACB_Synth>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_Synth Load(UTF_File synthTable, int index, Version ParseVersion)
        {
            ACB_Synth synth = new ACB_Synth();
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("SynthTable");

            synth.Index = index;
            synth.Type = synthTable.GetValue<byte>("Type", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            synth.VoiceLimitGroupName = synthTable.GetValue<string>("VoiceLimitGroupName", TypeFlag.String, index, tableHelper, ParseVersion);
            synth.Type = synthTable.GetValue<byte>("Type", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            synth.ParameterPallet = synthTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            synth.TrackValues = synthTable.GetData("TrackValues", index, tableHelper, ParseVersion);

            //Attached data
            ushort[] referenceItems = BigEndianConverter.ToUInt16Array(synthTable.GetData("ReferenceItems", index));
            synth.CommandIndex.TableIndex = synthTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);


            if (tableHelper.ColumnExists("LocalAisacs", TypeFlag.Data, ParseVersion))
            {
                synth.LocalAisac = AcbTableReference.FromArray(BigEndianConverter.ToUInt16Array(synthTable.GetData("LocalAisacs", index)));
            }

            if (tableHelper.ColumnExists("GlobalAisacStartIndex", TypeFlag.UInt16, ParseVersion))
            {
                var globalAisacStartIndex = synthTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
                var globalAisacNumRefs = synthTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);

                for (int i = 0; i < globalAisacNumRefs; i++)
                {
                    synth.GlobalAisacRefs.Add(new AcbTableReference(globalAisacStartIndex + i));
                }
            }

            if(tableHelper.ColumnExists("NumActionTracks", TypeFlag.UInt16, ParseVersion))
            {
                int numActionTracks = synthTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, index);
                int actionTracksStartIndex = synthTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, index);

                for (int i = 0; i < numActionTracks; i++)
                {
                    synth.ActionTracks.Add(new AcbTableReference(actionTracksStartIndex + i));
                }
            }

            //ReferenceItems
            synth.ReferenceItems = ACB_ReferenceItem.Load(referenceItems);

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("SynthTable");

            ushort actionTrackIndex = (ActionTracks.Count > 0) ? ActionTracks[0].TableIndex_Ushort : ushort.MaxValue;
            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0].TableIndex_Ushort : ushort.MaxValue;

            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("VoiceLimitGroupName", TypeFlag.String, index, VoiceLimitGroupName, tableHelper, ParseVersion);
            utfTable.AddValue("ControlWorkArea1", TypeFlag.UInt16, index, index.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ControlWorkArea2", TypeFlag.UInt16, index, index.ToString(), tableHelper, ParseVersion);
            utfTable.AddData("TrackValues", index, TrackValues, tableHelper, ParseVersion);
            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, index, actionTrackIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("NumActionTracks", TypeFlag.UInt16, index, ActionTracks.Count.ToString(), tableHelper, ParseVersion);

            byte[] referenceItems = ACB_ReferenceItem.Write(ReferenceItems);
            utfTable.AddData("ReferenceItems", index, (referenceItems.Length > 0) ? referenceItems : null, tableHelper, ParseVersion);
            
            if(LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(AcbTableReference.ToArray(LocalAisac)), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null, tableHelper, ParseVersion);
            }
        }


    }

    [YAXSerializeAs("Sequence")]
    [Serializable]
    public class ACB_Sequence : AcbTableBase, ITrack, ICommandIndex, ILocalAisac, IGlobalAisacRef, IActionTrack
    {

        #region Undoable
        [YAXDontSerialize]
        public SequenceType UndoableSequenceType
        {
            get
            {
                return Type;
            }
            set
            {
                if (Type != value)
                {
                    SequenceType oldValue = Type;
                    Type = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Sequence>("Type", this, oldValue, value, "Sequence Type"));
                    NotifyPropertyChanged(nameof(UndoableSequenceType));
                }
            }
        }

        #endregion

        [YAXAttributeForClass]
        public int Index { get; set; }

        private SequenceType _sequenceType = SequenceType.Polyphonic;
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public SequenceType Type { get { return _sequenceType; } set { _sequenceType = value; NotifyPropertyChanged(nameof(UndoableSequenceType)); } }
        [YAXAttributeFor("PlaybackRatio")]
        [YAXSerializeAs("value")]
        public ushort PlaybackRatio { get; set; } = 100;
        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("Tempo")]
        [YAXSerializeAs("value")]
        public ushort Tempo { get; set; } //only in old versions

        //Tracks
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Track")]
        public AsyncObservableCollection<ACB_SequenceTrack> Tracks { get; set; } = new AsyncObservableCollection<ACB_SequenceTrack>();

        //CommandIndex
        [YAXSerializeAs("CommandIndex")]
        public AcbTableReference CommandIndex { get; set; } = new AcbTableReference(); //ushort

        //LocalAisac
        [YAXSerializeAs("LocalAisac")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> LocalAisac { get; set; } = new List<AcbTableReference>(); //ushort

        //GlobalAisac
        [YAXSerializeAs("GlobalAisacRefs")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> GlobalAisacRefs { get; set; } = new List<AcbTableReference>(); //ushort

        //ActionTrack
        [YAXSerializeAs("ActionTracks")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> ActionTracks { get; set; } = new List<AcbTableReference>(); //ushort
        
        public void Initialize()
        {
            if (Tracks == null) Tracks = AsyncObservableCollection<ACB_SequenceTrack>.Create();
            if (ActionTracks == null) ActionTracks = new List<AcbTableReference>();
            if (CommandIndex == null) CommandIndex = new AcbTableReference();
            if (LocalAisac == null) LocalAisac = new List<AcbTableReference>();
            if (GlobalAisacRefs == null) GlobalAisacRefs = new List<AcbTableReference>();
        }

        public static List<ACB_Sequence> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_Sequence> rows = new List<ACB_Sequence>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_Sequence Load(UTF_File sequenceTable, int index, Version ParseVersion)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("SequenceTable");

            ACB_Sequence sequence = new ACB_Sequence();

            //Values
            sequence.Index = index;
            sequence.CommandIndex.TableIndex = sequenceTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);
            sequence.Tempo = sequenceTable.GetValue<ushort>("Tempo", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            sequence.ParameterPallet = sequenceTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            sequence.PlaybackRatio = sequenceTable.GetValue<ushort>("PlaybackRatio", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            sequence.Type = (SequenceType)sequenceTable.GetValue<byte>("Type", TypeFlag.UInt8, index, tableHelper, ParseVersion);

            //References
            if (tableHelper.ColumnExists("LocalAisacs", TypeFlag.Data, ParseVersion))
            {
                sequence.LocalAisac = AcbTableReference.FromArray(BigEndianConverter.ToUInt16Array(sequenceTable.GetData("LocalAisacs", index)));
            }

            if(tableHelper.ColumnExists("GlobalAisacStartIndex", TypeFlag.UInt16, ParseVersion))
            {
                var globalAisacStartIndex = sequenceTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
                var globalAisacNumRefs = sequenceTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);

                for (int i = 0; i < globalAisacNumRefs; i++)
                {
                    sequence.GlobalAisacRefs.Add(new AcbTableReference(globalAisacStartIndex + i));
                }
            }

            if (tableHelper.ColumnExists("NumActionTracks", TypeFlag.UInt16, ParseVersion))
            {
                int numActionTracks = sequenceTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, index);
                int actionTracksStartIndex = sequenceTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, index);

                for (int i = 0; i < numActionTracks; i++)
                {
                    sequence.ActionTracks.Add(new AcbTableReference(actionTracksStartIndex + i));
                }
            }

            //Tracks
            var tracks = BigEndianConverter.ToUInt16Array(sequenceTable.GetData("TrackIndex", index)).ToList();
            List<ushort> trackValues = new ushort[tracks.Count].ToList();
            sequence.Tracks = AsyncObservableCollection<ACB_SequenceTrack>.Create();
            
            if(tableHelper.ColumnExists("TrackValues", TypeFlag.Data, ParseVersion))
            {
                trackValues = BigEndianConverter.ToUInt16Array(sequenceTable.GetData("TrackValues", index)).ToList();

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
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                sequence.Tracks.Add(new ACB_SequenceTrack() { Index = new AcbTableReference(tracks[i]), Percentage = trackValues[i] });
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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("SequenceTable");

            //Create TrackIndex and TrackValue arrays
            List<ushort> trackValues = new List<ushort>();
            List<ushort> trackIndexes = new List<ushort>();

            int total = 0;
            foreach (var track in Tracks)
            {
                trackValues.Add(track.Percentage);
                trackIndexes.Add(track.Index.TableIndex_Ushort);
                total += track.Percentage;
            }

            //Add remainder value to TrackValues IF they do not all add up to 100.
            if (total < 100)
                trackValues.Add((ushort)(100 - total));

            //Write columns
            ushort actionTrackIndex = (ActionTracks.Count > 0) ? ActionTracks[0].TableIndex_Ushort : ushort.MaxValue;
            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0].TableIndex_Ushort : ushort.MaxValue;

            utfTable.AddValue("Tempo", TypeFlag.UInt16, index, Tempo.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("PlaybackRatio", TypeFlag.UInt16, index, PlaybackRatio.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("Type", TypeFlag.UInt8, index, ((byte)Type).ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ControlWorkArea1", TypeFlag.UInt16, index, index.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ControlWorkArea2", TypeFlag.UInt16, index, index.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, index, actionTrackIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("NumActionTracks", TypeFlag.UInt16, index, ActionTracks.Count.ToString(), tableHelper, ParseVersion);

            if (trackValues.Count > 0 && (Type == SequenceType.Random || Type == SequenceType.RandomNoRepeat))
            {
                utfTable.AddData("TrackValues", index, BigEndianConverter.GetBytes(trackValues.ToArray()), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("TrackValues", index, null, tableHelper, ParseVersion);
            }

            if (trackIndexes.Count > 0)
            {
                utfTable.AddData("TrackIndex", index, BigEndianConverter.GetBytes(trackIndexes.ToArray()), tableHelper, ParseVersion);
                utfTable.AddValue("NumTracks", TypeFlag.UInt16, index, Tracks.Count.ToString(), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("TrackIndex", index, null, tableHelper, ParseVersion);
                utfTable.AddValue("NumTracks", TypeFlag.UInt16, index, "0", tableHelper, ParseVersion);
            }

            if (LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(AcbTableReference.ToArray(LocalAisac)), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null, tableHelper, ParseVersion);
            }
        }


        public void AdjustTrackPercentage()
        {
            if (Tracks == null) return;

            while (TrackPercentageExceeds100())
            {
                foreach (var track in Tracks)
                {
                    track.Percentage = (ushort)(track.Percentage * 0.8);
                }
            }
        }

        private bool TrackPercentageExceeds100()
        {
            int total = 0;

            foreach (var track in Tracks)
                total += track.Percentage;

            return total > 100;
        }
    }

    [YAXSerializeAs("Track")]
    [Serializable]
    public class ACB_SequenceTrack : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


        [YAXAttributeForClass]
        public AcbTableReference Index { get; set; } = new AcbTableReference(); //TrackIndex, ushort
        private ushort _percentage = 0;
        [YAXAttributeForClass]
        public ushort Percentage
        {
            get
            {
                return _percentage;
            }
            set
            {
                _percentage = value;
                NotifyPropertyChanged("Percentage");
                NotifyPropertyChanged("UndoablePercentage");
            }
        }

        #region Undoables
        [YAXDontSerialize]
        public ushort UndoablePercentage
        {
            get
            {
                return Percentage;
            }
            set
            {
                if(Percentage != value)
                {
                    ushort oldValue = Percentage;
                    Percentage = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_SequenceTrack>("Percentage", this, oldValue, value, "SeqTrack Percentage"));
                    NotifyPropertyChanged("UndoablePercentage");
                }
            }
        }

        #endregion

    }

    [Serializable]
    public class ACB_SequenceBlock : AcbTableBase, ICommandIndex, ILocalAisac, ITrack, IGlobalAisacRef, IBlock
    {
        
        #region Undoable
        [YAXDontSerialize]
        public SequenceType UndoableSequenceType
        {
            get
            {
                return Type;
            }
            set
            {
                if (Type != value)
                {
                    SequenceType oldValue = Type;
                    Type = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_SequenceBlock>(nameof(Type), this, oldValue, value, "SequenceBlock Type"));
                    NotifyPropertyChanged(nameof(UndoableSequenceType));
                }
            }
        }

        #endregion

        [YAXAttributeForClass]
        public int Index { get; set; }

        private SequenceType _sequenceType = SequenceType.Polyphonic;
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public SequenceType Type { get { return _sequenceType; } set { _sequenceType = value; NotifyPropertyChanged(nameof(UndoableSequenceType)); } }
        [YAXAttributeFor("PlaybackRatio")]
        [YAXSerializeAs("value")]
        public ushort PlaybackRatio { get; set; } = 100;
        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("Tempo")]
        [YAXSerializeAs("value")]
        public ushort Tempo { get; set; } //only in old versions

        //CommandIndex
        [YAXSerializeAs("CommandIndex")]
        public AcbTableReference CommandIndex { get; set; } = new AcbTableReference(); //ushort

        //LocalAisac
        [YAXSerializeAs("LocalAisac")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> LocalAisac { get; set; } = new List<AcbTableReference>(); //ushort

        //GlobalAisac
        [YAXSerializeAs("GlobalAisacRefs")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> GlobalAisacRefs { get; set; } = new List<AcbTableReference>(); //ushort

        //Tracks
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Track")]
        public AsyncObservableCollection<ACB_SequenceTrack> Tracks { get; set; } = new AsyncObservableCollection<ACB_SequenceTrack>();

        //Blocks
        [YAXSerializeAs("Blocks")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> Blocks { get; set; } = new List<AcbTableReference>();

        public void Initialize()
        {
            if (Tracks == null) Tracks = AsyncObservableCollection<ACB_SequenceTrack>.Create();
            if (CommandIndex == null) CommandIndex = new AcbTableReference();
            if (LocalAisac == null) LocalAisac = new List<AcbTableReference>();
            if (GlobalAisacRefs == null) GlobalAisacRefs = new List<AcbTableReference>();
            if (Blocks == null) Blocks = new List<AcbTableReference>();
        }

        public static List<ACB_SequenceBlock> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_SequenceBlock> rows = new List<ACB_SequenceBlock>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_SequenceBlock Load(UTF_File table, int index, Version ParseVersion)
        {
            ACB_SequenceBlock sequenceBlock = new ACB_SequenceBlock();
            sequenceBlock.Index = index;

            sequenceBlock.CommandIndex.TableIndex = table.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);

            if (ParseVersion < ACB_File._1_7_0_0)
            {
                sequenceBlock.Tempo = table.GetValue<ushort>("Tempo", TypeFlag.UInt16, index);
            }

            if (ParseVersion > ACB_File._1_3_0_0)
            {
                sequenceBlock.LocalAisac = AcbTableReference.FromArray(BigEndianConverter.ToUInt16Array(table.GetData("LocalAisacs", index)));

                var globalAisacStartIndex = table.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
                var globalAisacNumRefs = table.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);

                for (int i = 0; i < globalAisacNumRefs; i++)
                {
                    sequenceBlock.GlobalAisacRefs.Add(new AcbTableReference(globalAisacStartIndex + i));
                }
            }

            if (ParseVersion >= ACB_File._1_7_0_0)
            {
                sequenceBlock.ParameterPallet = table.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index);
                sequenceBlock.PlaybackRatio = table.GetValue<ushort>("PlaybackRatio", TypeFlag.UInt16, index);
            }

            if (ParseVersion > ACB_File._1_12_0_0)
            {
                sequenceBlock.Type = (SequenceType)table.GetValue<byte>("Type", TypeFlag.UInt8, index);
            }

            //Tracks
            var tracks = BigEndianConverter.ToUInt16Array(table.GetData("TrackIndex", index)).ToList();
            List<ushort> trackValues = new ushort[tracks.Count].ToList();
            sequenceBlock.Tracks = AsyncObservableCollection<ACB_SequenceTrack>.Create();

            if (ParseVersion > ACB_File._1_12_0_0)
            {
                trackValues = BigEndianConverter.ToUInt16Array(table.GetData("TrackValues", index)).ToList();

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
                    throw new Exception("ACB_SequenceBlock.Load: TrackValues count is greater than Tracks count. Load failed.");
                }
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                sequenceBlock.Tracks.Add(new ACB_SequenceTrack() { Index = new AcbTableReference(tracks[i]), Percentage = trackValues[i] });
            }

            //Blocks
            var blocks = BigEndianConverter.ToUInt16Array(table.GetData("BlockIndex", index)).ToList();

            foreach(var block in blocks)
            {
                sequenceBlock.Blocks.Add(new AcbTableReference(block));
            }

            return sequenceBlock;
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
                trackIndexes.Add(track.Index.TableIndex_Ushort);
                total += track.Percentage;
            }

            //Add remainder value to TrackValues IF they do not all add up to 100.
            if (total < 100)
                trackValues.Add((ushort)(100 - total));

            //Write columns
            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0].TableIndex_Ushort : ushort.MaxValue;

            if (ParseVersion < ACB_File._1_7_0_0)
            {
                utfTable.AddValue("Tempo", TypeFlag.UInt16, index, Tempo.ToString());
            }

            if (ParseVersion >= ACB_File._1_7_0_0)
            {
                utfTable.AddValue("PlaybackRatio", TypeFlag.UInt16, index, PlaybackRatio.ToString());
                utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString());
            }

            if (ParseVersion > ACB_File._1_12_0_0)
            {
                if (trackValues.Count > 0 && (Type == SequenceType.Random || Type == SequenceType.RandomNoRepeat))
                {
                    utfTable.AddData("TrackValues", index, BigEndianConverter.GetBytes(trackValues.ToArray()));
                }
                else
                {
                    utfTable.AddData("TrackValues", index, null);
                }
            }

            if (ParseVersion > ACB_File._1_12_0_0)
            {
                utfTable.AddValue("Type", TypeFlag.UInt8, index, ((byte)Type).ToString());
                utfTable.AddValue("ControlWorkArea1", TypeFlag.UInt16, index, index.ToString());
                utfTable.AddValue("ControlWorkArea2", TypeFlag.UInt16, index, index.ToString());
            }

            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString());

            if (ParseVersion > ACB_File._1_3_0_0)
            {
                utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString());
                utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString());
            }

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

            if (ParseVersion > ACB_File._1_3_0_0)
            {
                if (LocalAisac != null)
                {
                    utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(AcbTableReference.ToArray(LocalAisac)));
                }
                else
                {
                    utfTable.AddData("LocalAisacs", index, null);
                }
            }

            //Blocks
            ushort[] blocks = new ushort[Blocks.Count];

            for(int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = Blocks[i].TableIndex_Ushort;
            }

            utfTable.AddData("BlockIndex", index, BigEndianConverter.GetBytes(blocks.ToArray()));
            utfTable.AddValue("BlockIndex", TypeFlag.UInt16, index, blocks.Length.ToString());
        }


        public void AdjustTrackPercentage()
        {
            if (Tracks == null) return;

            while (TrackPercentageExceeds100())
            {
                foreach (var track in Tracks)
                {
                    track.Percentage = (ushort)(track.Percentage * 0.8);
                }
            }
        }

        private bool TrackPercentageExceeds100()
        {
            int total = 0;

            foreach (var track in Tracks)
                total += track.Percentage;

            return total > 100;
        }

    }

    [Serializable]
    public class ACB_Block : AcbTableBase, ITrack, ICommandIndex, IActionTrack, IBlock
    {

        //CommandIndex
        [YAXSerializeAs("CommandIndex")]
        public AcbTableReference CommandIndex { get; set; } = new AcbTableReference(); //ushort

        //ActionTrack
        [YAXSerializeAs("ActionTracks")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> ActionTracks { get; set; } = new List<AcbTableReference>(); //ushort

        //Tracks
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Track")]
        public AsyncObservableCollection<ACB_SequenceTrack> Tracks { get; set; } = new AsyncObservableCollection<ACB_SequenceTrack>();

        //Blocks
        [YAXSerializeAs("Blocks")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> Blocks { get; set; } = new List<AcbTableReference>();


    }

    [YAXSerializeAs("Track")]
    [Serializable]
    public class ACB_Track : AcbTableBase, IEventIndex, ICommandIndex, ILocalAisac, IGlobalAisacRef, ITargetId
    {
        #region Undoable
        [YAXDontSerialize]
        public int UndoableTargetType
        {
            get
            {
                return (int)TargetType;
            }
            set
            {
                if ((int)TargetType != value)
                {
                    TargetType oldValue = TargetType;
                    TargetType = (TargetType)value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Track>("TargetType", this, oldValue, TargetType, "Target Type"));
                    NotifyPropertyChanged("UndoableTargetType");
                }
            }
        }

        [YAXDontSerialize]
        public string UndoableTargetAcbName
        {
            get
            {
                return TargetAcbName;
            }
            set
            {
                if (TargetAcbName != value)
                {
                    string oldValue = TargetAcbName;
                    TargetAcbName = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Track>("TargetAcbName", this, oldValue, value, "Target Acb Name"));
                    NotifyPropertyChanged("UndoableTargetAcbName");
                }
            }
        }

        [YAXDontSerialize]
        public bool UndoableTargetSelf
        {
            get
            {
                return TargetSelf;
            }
            set
            {
                if (TargetSelf != value)
                {
                    bool oldValue = TargetSelf;
                    TargetSelf = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Track>("TargetSelf", this, oldValue, value, "Target Self"));
                    NotifyPropertyChanged("UndoableTargetSelf");
                }
            }
        }
        #endregion

        //Values
        private TargetType _targetType = TargetType.AnyAcb;
        private string _targetAcbName = string.Empty;
        private bool _targetSelf = false;

        //Properties
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("ParameterPallet")]
        [YAXSerializeAs("value")]
        public ushort ParameterPallet { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("TargetType")]
        [YAXSerializeAs("value")]
        public TargetType TargetType { get { return _targetType; } set { _targetType = value; NotifyPropertyChanged(nameof(UndoableTargetType)); } }
        [YAXAttributeFor("TargetName")]
        [YAXSerializeAs("value")]
        public string TargetName { get; set; } = string.Empty;
        [YAXSerializeAs("TargetId")]
        public AcbTableReference TargetId { get; set; } = new AcbTableReference(uint.MaxValue); //uint
        [YAXAttributeFor("TargetAcbName")]
        [YAXSerializeAs("value")]
        public string TargetAcbName { get { return _targetAcbName; } set { _targetAcbName = value; NotifyPropertyChanged(nameof(UndoableTargetAcbName)); } }
        [YAXAttributeFor("TargetSelf")]
        [YAXSerializeAs("value")]
        public bool TargetSelf { get { return _targetSelf; } set { _targetSelf = value; NotifyPropertyChanged(nameof(UndoableTargetSelf)); } }//When true, set TargetAcbName to current Acb Name
        [YAXAttributeFor("Scope")]
        [YAXSerializeAs("value")]
        public byte Scope { get; set; }
        [YAXAttributeFor("TargetTrackNo")]
        [YAXSerializeAs("value")]
        public ushort TargetTrackNo { get; set; } = 65535;

        //Commands
        [YAXSerializeAs("EventIndex")]
        public AcbTableReference EventIndex { get; set; } = new AcbTableReference();
        [YAXSerializeAs("CommandIndex")]
        public AcbTableReference CommandIndex { get; set; } = new AcbTableReference();

        //LocalAisac
        [YAXSerializeAs("LocalAisac")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> LocalAisac { get; set; } = new List<AcbTableReference>();

        //GlobalAisac
        [YAXSerializeAs("GlobalAisacRefs")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<AcbTableReference> GlobalAisacRefs { get; set; } = new List<AcbTableReference>();

        public void Initialize()
        {
            if (EventIndex == null) EventIndex = new AcbTableReference();
            if (CommandIndex == null) CommandIndex = new AcbTableReference();
            if (LocalAisac == null) LocalAisac = new List<AcbTableReference>();
            if (GlobalAisacRefs == null) GlobalAisacRefs = new List<AcbTableReference>();
        }

        public static List<ACB_Track> Load(UTF_File table, Version ParseVersion, string currentAcbName, bool isActionTrack)
        {
            List<ACB_Track> rows = new List<ACB_Track>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, currentAcbName, ParseVersion, isActionTrack));
            }

            return rows;
        }

        public static ACB_Track Load(UTF_File trackTable, int index, string currentAcbName, Version ParseVersion, bool isActionTrack)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper((isActionTrack) ? "ActionTrackTable" : "TrackTable");

            ACB_Track track = new ACB_Track();
            track.Index = index;

            //Track values
            track.EventIndex.TableIndex = trackTable.GetValue<ushort>("EventIndex", TypeFlag.UInt16, index);
            track.CommandIndex.TableIndex = trackTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, index);
            track.LocalAisac = AcbTableReference.FromArray(BigEndianConverter.ToUInt16Array(trackTable.GetData("LocalAisacs", index, tableHelper, ParseVersion)));
            track.TargetType = (TargetType)trackTable.GetValue<byte>("TargetType", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            track.TargetName = trackTable.GetValue<string>("TargetName", TypeFlag.String, index, tableHelper, ParseVersion);
            track.TargetId.TableIndex = trackTable.GetValue<uint>("TargetId", TypeFlag.UInt32, index, tableHelper, ParseVersion);
            track.TargetAcbName = trackTable.GetValue<string>("TargetAcbName", TypeFlag.String, index, tableHelper, ParseVersion);
            track.Scope = trackTable.GetValue<byte>("Scope", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            track.TargetTrackNo = trackTable.GetValue<ushort>("TargetTrackNo", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            track.ParameterPallet = trackTable.GetValue<ushort>("ParameterPallet", TypeFlag.UInt16, index, tableHelper, ParseVersion);

            if (tableHelper.ColumnExists("GlobalAisacStartIndex", TypeFlag.UInt16, ParseVersion))
            {
                var globalAisacStartIndex = trackTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, index);
                var globalAisacNumRefs = trackTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, index);

                for (int i = 0; i < globalAisacNumRefs; i++)
                {
                    track.GlobalAisacRefs.Add(new AcbTableReference(globalAisacStartIndex + i));
                }
            }

            if (currentAcbName == track.TargetAcbName)
                track.TargetSelf = true;

            return track;
        }

        public static void WriteToTable(IList<ACB_Track> entries, Version ParseVersion, UTF_File utfTable, string currentAcbName, bool isActionTrack)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion, currentAcbName, isActionTrack);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, string currentAcbName, bool isActionTrack)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper((isActionTrack) ? "ActionTrackTable" : "TrackTable");

            if (TargetSelf)
                TargetAcbName = currentAcbName;

            ushort globalAisacRefIndex = (GlobalAisacRefs.Count > 0) ? GlobalAisacRefs[0].TableIndex_Ushort : ushort.MaxValue;

            utfTable.AddValue("ParameterPallet", TypeFlag.UInt16, index, ParameterPallet.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("TargetType", TypeFlag.UInt8, index, ((byte)TargetType).ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("TargetName", TypeFlag.String, index, TargetName, tableHelper, ParseVersion);
            utfTable.AddValue("TargetId", TypeFlag.UInt32, index, TargetId.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("TargetAcbName", TypeFlag.String, index, TargetAcbName, tableHelper, ParseVersion);
            utfTable.AddValue("Scope", TypeFlag.UInt8, index, Scope.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("TargetTrackNo", TypeFlag.UInt16, index, TargetTrackNo.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("CommandIndex", TypeFlag.UInt16, index, CommandIndex.ToString());
            utfTable.AddValue("EventIndex", TypeFlag.UInt16, index, EventIndex.ToString());
            utfTable.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, index, globalAisacRefIndex.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, index, GlobalAisacRefs.Count.ToString(), tableHelper, ParseVersion);
            
            if (LocalAisac != null)
            {
                utfTable.AddData("LocalAisacs", index, BigEndianConverter.GetBytes(AcbTableReference.ToArray(LocalAisac)), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("LocalAisacs", index, null, tableHelper, ParseVersion);
            }
        }

    }

    [YAXSerializeAs("GlobalAisacReference")]
    [Serializable]
    public class ACB_GlobalAisacReference : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; } = string.Empty;

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
            utfTable.AddValue("Name", TypeFlag.String, index, Name);
        }

    }

    [YAXSerializeAs("Waveform")]
    [Serializable]
    public class ACB_Waveform : AcbTableBase
    {
        #region Undoable
        [YAXDontSerialize]
        public bool UndoableStreaming
        {
            get
            {
                return Streaming;
            }
            set
            {
                if (Streaming != value)
                {
                    bool oldValue = Streaming;
                    Streaming = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Waveform>("Streaming", this, oldValue, value, "Streaming"));
                    NotifyPropertyChanged("UndoableStreaming");
                }
            }
        }

        #endregion

        //Values
        private bool _streaming = false;

        //Properties
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeFor("EncodeType")]
        [YAXSerializeAs("value")]
        public EncodeType EncodeType { get; set; }
        [YAXAttributeFor("Streaming")]
        [YAXSerializeAs("value")]
        public bool Streaming { get { return _streaming; } set { _streaming = value; NotifyPropertyChanged(nameof(UndoableStreaming)); } }
        [YAXAttributeFor("AwbId")]
        [YAXSerializeAs("value")]
        public ushort AwbId { get; set; } //Tracks will be stored in a seperate section
        [YAXAttributeFor("NumChannels")]
        [YAXSerializeAs("value")]
        public byte NumChannels { get; set; }
        [YAXAttributeFor("LoopFlag")]
        [YAXSerializeAs("value")]
        public byte LoopFlag { get; set; }// 0, 1, 2
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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("WaveformTable");

            ACB_Waveform waveform = new ACB_Waveform();
            waveform.Index = index;

            waveform.EncodeType = (EncodeType)waveformTable.GetValue<byte>("EncodeType", TypeFlag.UInt8, index);
            waveform.Streaming = Convert.ToBoolean(waveformTable.GetValue<byte>("Streaming", TypeFlag.UInt8, index));
            waveform.NumChannels = waveformTable.GetValue<byte>("NumChannels", TypeFlag.UInt8, index, tableHelper, ParseVersion);
            waveform.LoopFlag = waveformTable.GetValue<byte>("LoopFlag", TypeFlag.UInt8, index);
            waveform.SamplingRate = waveformTable.GetValue<ushort>("SamplingRate", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            waveform.NumSamples = waveformTable.GetValue<uint>("NumSamples", TypeFlag.UInt32, index, tableHelper, ParseVersion);
            waveform.StreamAwbPortNo = waveformTable.GetValue<ushort>("StreamAwbPortNo", TypeFlag.UInt16, index, tableHelper, ParseVersion);

            if (tableHelper.ColumnExists("StreamAwbId", TypeFlag.UInt16, ParseVersion))
            {
                //Is newer format
                waveform.AwbId = (waveform.Streaming) ? waveformTable.GetValue<ushort>("StreamAwbId", TypeFlag.UInt16, index) : waveformTable.GetValue<ushort>("MemoryAwbId", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            }
            else
            {
                //Older ACB ver. Just has a "Id" column.
                waveform.AwbId = waveformTable.GetValue<ushort>("Id", TypeFlag.UInt16, index, tableHelper, ParseVersion);
            }

            if (tableHelper.ColumnExists("ExtensionData", TypeFlag.UInt16, ParseVersion))
            {
                ushort extensionIndex = waveformTable.GetValue<ushort>("ExtensionData", TypeFlag.UInt16, index);

                if(extensionIndex != ushort.MaxValue)
                {
                    //waveform.LoopFlag = true;

                    if(waveformExtensionTable != null)
                    {
                        waveform.LoopStart = waveformExtensionTable.GetValue<uint>("LoopStart", TypeFlag.UInt32, extensionIndex);
                        waveform.LoopEnd = waveformExtensionTable.GetValue<uint>("LoopEnd", TypeFlag.UInt32, extensionIndex);
                    }
                    
                }
                else
                {
                    //waveform.LoopFlag = false;
                }
            }
            else if (tableHelper.ColumnExists("ExtensionData", TypeFlag.Data, ParseVersion))
            {
                //Older ACB ver where ExtensionData is of type Data.
                //I haven't found any instances where this is not null, so its structure is unknown... assume 2 x uint16
                ushort[] loopData = BigEndianConverter.ToUInt16Array(waveformTable.GetData("ExtensionData", index));

                if (loopData.Length == 2)
                {
                    //waveform.LoopFlag = true;
                    waveform.LoopStart = loopData[0];
                    waveform.LoopEnd = loopData[1];
                }
                else
                {
                    //waveform.LoopFlag = false;
                }
            }

            return waveform;
        }

        public static void WriteToTable(IList<ACB_Waveform> entries, Version ParseVersion, UTF_File utfTable, UTF_File waveformExtensionTable, bool eternityCompat)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion, waveformExtensionTable, eternityCompat);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion, UTF_File waveformExtensionTable, bool eternityCompat)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("WaveformTable");

            utfTable.AddValue("EncodeType", TypeFlag.UInt8, index, ((byte)EncodeType).ToString());
            utfTable.AddValue("Streaming", TypeFlag.UInt8, index, Convert.ToByte(Streaming).ToString());
            utfTable.AddValue("NumChannels", TypeFlag.UInt8, index, NumChannels.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("LoopFlag", TypeFlag.UInt8, index, Convert.ToByte(LoopFlag).ToString());
            utfTable.AddValue("SamplingRate", TypeFlag.UInt16, index, SamplingRate.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("NumSamples", TypeFlag.UInt32, index, NumSamples.ToString(), tableHelper, ParseVersion);

            if (tableHelper.ColumnExists("StreamAwbId", TypeFlag.UInt16, ParseVersion))
            {

                if (Streaming)
                {
                    utfTable.AddValue("StreamAwbId", TypeFlag.UInt16, index, AwbId.ToString());
                    utfTable.AddValue("MemoryAwbId", TypeFlag.UInt16, index, ushort.MaxValue.ToString());
                }
                else
                {
                    utfTable.AddValue("StreamAwbId", TypeFlag.UInt16, index, ushort.MaxValue.ToString());
                    utfTable.AddValue("MemoryAwbId", TypeFlag.UInt16, index, AwbId.ToString());
                }

            }
            else
            {
                //Older ACB ver. Just has a "Id" column.
                utfTable.AddValue("Id", TypeFlag.UInt16, index, AwbId.ToString());
            }

            if(tableHelper.ColumnExists("StreamAwbPortNo", TypeFlag.UInt16, ParseVersion))
            {
                if(StreamAwbPortNo == ushort.MaxValue)
                {
                    utfTable.AddValue("StreamAwbPortNo", TypeFlag.UInt16, index, (Streaming) ? "0" : ushort.MaxValue.ToString());
                }
                else
                {
                    utfTable.AddValue("StreamAwbPortNo", TypeFlag.UInt16, index, (Streaming) ? StreamAwbPortNo.ToString() : ushort.MaxValue.ToString());
                }
            }

            //ExtensionData
            ushort extensionIndex = ushort.MaxValue;
            byte[] loopBytes = null; 

            if (LoopFlag > 0)
            {
                if (tableHelper.ColumnExists("ExtensionData", TypeFlag.UInt16, ParseVersion))
                {
                    if (!eternityCompat && LoopEnd != 0)
                    {
                        int newIdx = waveformExtensionTable.RowCount();
                        waveformExtensionTable.AddValue("LoopStart", TypeFlag.UInt32, newIdx, LoopStart.ToString());
                        waveformExtensionTable.AddValue("LoopEnd", TypeFlag.UInt32, newIdx, LoopEnd.ToString());
                        extensionIndex = (ushort)newIdx;
                    }
                }
                else if (tableHelper.ColumnExists("ExtensionData", TypeFlag.Data, ParseVersion))
                {
                    loopBytes = (eternityCompat) ? null : BigEndianConverter.GetBytes(new uint[2] { LoopStart, LoopEnd });
                }
            }

            if (tableHelper.ColumnExists("ExtensionData", TypeFlag.UInt16, ParseVersion))
            {
                if(eternityCompat)
                    utfTable.AddValue("ExtensionData", TypeFlag.UInt16, index, ushort.MaxValue.ToString());
                else
                    utfTable.AddValue("ExtensionData", TypeFlag.UInt16, index, extensionIndex.ToString());

            }
            else if (tableHelper.ColumnExists("ExtensionData", TypeFlag.Data, ParseVersion))
            {
                if (eternityCompat)
                    utfTable.AddData("ExtensionData", index, null);
                else
                    utfTable.AddData("ExtensionData", index, loopBytes);
            }

        }

        public void UpdateProperties()
        {
            foreach(var prop in GetType().GetProperties())
            {
                NotifyPropertyChanged(prop.Name);
            }
        }
    }

    [YAXSerializeAs("Aisac")]
    [Serializable]
    public class ACB_Aisac : AcbTableBase, IAutoModulationIndex, IGraphIndexes
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
        public string ControlName { get; set; } = string.Empty; //Optional
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

        
        [YAXSerializeAs("AutoModulationIndex")]
        public AcbTableReference AutoModulationIndex { get; set; } = new AcbTableReference(); //ushort
        
        [YAXSerializeAs("GraphIndexes")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<AcbTableReference> GraphIndexes { get; set; } = new List<AcbTableReference>(); //ushort

        public void Initialize()
        {
            if (AutoModulationIndex == null) AutoModulationIndex = new AcbTableReference();
            if (GraphIndexes == null) GraphIndexes = new List<AcbTableReference>();
        }

        public static List<ACB_Aisac> Load(UTF_File table, UTF_File aisacNameTable, Version ParseVersion)
        {
            List<ACB_Aisac> rows = new List<ACB_Aisac>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, aisacNameTable, ParseVersion));
            }

            return rows;
        }

        public static ACB_Aisac Load(UTF_File aisacTable, int index, UTF_File aisacNameTable, Version version)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("AisacTable");

            ACB_Aisac aisac = new ACB_Aisac();
            aisac.Index = index;
            aisac.Id = aisacTable.GetValue<short>("Id", TypeFlag.Int16, index);
            aisac.Type = aisacTable.GetValue<byte>("Type", TypeFlag.UInt8, index);
            aisac.ControlId = aisacTable.GetValue<ushort>("ControlId", TypeFlag.UInt16, index);
            aisac.RandomRange = aisacTable.GetValue<float>("RandomRange", TypeFlag.Single, index, tableHelper, version);
            aisac.DefaultControlFlag = aisacTable.GetValue<byte>("DefaultControlFlag", TypeFlag.UInt8, index, tableHelper, version);
            aisac.DefaultControl = aisacTable.GetValue<float>("DefaultControl", TypeFlag.Single, index, tableHelper, version);
            aisac.GraphBitFlag = aisacTable.GetValue<byte>("GraphBitFlag", TypeFlag.UInt8, index, tableHelper, version);


            if(tableHelper.ColumnExists("AudoModulationIndex", TypeFlag.UInt16, version))
            {
                //typo in an earlier acb version
                aisac.AutoModulationIndex.TableIndex = aisacTable.GetValue<ushort>("AudoModulationIndex", TypeFlag.UInt16, index);
            }
            else if (tableHelper.ColumnExists("AutoModulationIndex", TypeFlag.UInt16, version))
            {
                aisac.AutoModulationIndex.TableIndex = aisacTable.GetValue<ushort>("AutoModulationIndex", TypeFlag.UInt16, index);
            }

            aisac.GraphIndexes = AcbTableReference.FromArray(BigEndianConverter.ToUInt16Array(aisacTable.GetData("GraphIndexes", index, tableHelper, version)));

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("AisacTable");

            utfTable.AddValue("Id", TypeFlag.Int16, index, Id.ToString());
            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString());
            utfTable.AddValue("ControlId", TypeFlag.UInt16, index, ControlId.ToString());
            utfTable.AddValue("RandomRange", TypeFlag.Single, index, RandomRange.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("DefaultControlFlag", TypeFlag.UInt8, index, DefaultControlFlag.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("DefaultControl", TypeFlag.Single, index, DefaultControl.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("GraphBitFlag", TypeFlag.UInt8, index, GraphBitFlag.ToString(), tableHelper, ParseVersion);


            if(tableHelper.ColumnExists("AudoModulationIndex", TypeFlag.UInt16, ParseVersion))
            {
                utfTable.AddValue("AudoModulationIndex", TypeFlag.UInt16, index, AutoModulationIndex.ToString());
            }
            else if (tableHelper.ColumnExists("AutoModulationIndex", TypeFlag.UInt16, ParseVersion))
            {
                utfTable.AddValue("AutoModulationIndex", TypeFlag.UInt16, index, AutoModulationIndex.ToString());
            }

            if (GraphIndexes != null)
            {
                utfTable.AddData("GraphIndexes", index, BigEndianConverter.GetBytes(AcbTableReference.ToArray(GraphIndexes)), tableHelper, ParseVersion);
            }
            else
            {
                utfTable.AddData("GraphIndexes", index, null, tableHelper, ParseVersion);
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
    [Serializable]
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
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_Graph Load(UTF_File graphTable, int index, Version version)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("GraphTable");

            ACB_Graph graph = new ACB_Graph();
            graph.Index = index;
            graph.Type = graphTable.GetValue<ushort>("Type", TypeFlag.UInt16, index, tableHelper, version);
            graph.Controls = graphTable.GetData("Controls", index, tableHelper, version);
            graph.Destinations = graphTable.GetData("Destinations", index, tableHelper, version);
            graph.Curve = graphTable.GetData("Curve", index, tableHelper, version);
            graph.ControlWorkArea = graphTable.GetValue<float>("ControlWorkArea", TypeFlag.Single, index, tableHelper, version);
            graph.DestinationWorkArea = graphTable.GetValue<float>("DestinationWorkArea", TypeFlag.Single, index, tableHelper, version);

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("GraphTable");

            utfTable.AddValue("Type", TypeFlag.UInt16, index, Type.ToString(), tableHelper, ParseVersion);
            utfTable.AddData("Controls", index, Controls, tableHelper, ParseVersion);
            utfTable.AddData("Destinations", index, Destinations, tableHelper, ParseVersion);
            utfTable.AddData("Curve", index, Curve, tableHelper, ParseVersion);
            utfTable.AddValue("ControlWorkArea", TypeFlag.Single, index, ControlWorkArea.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("DestinationWorkArea", TypeFlag.Single, index, DestinationWorkArea.ToString(), tableHelper, ParseVersion);
        }

    }

    [YAXSerializeAs("AutoModulation")]
    [Serializable]
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
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_AutoModulation Load(UTF_File autoModTable, int index, Version version)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("AutoModulationTable");

            ACB_AutoModulation autoMod = new ACB_AutoModulation();
            autoMod.Index = index;
            autoMod.Key = autoModTable.GetValue<uint>("Key", TypeFlag.UInt32, index, tableHelper, version);
            autoMod.Time = autoModTable.GetValue<uint>("Time", TypeFlag.UInt32, index, tableHelper, version);
            autoMod.Type = autoModTable.GetValue<byte>("Type", TypeFlag.UInt8, index, tableHelper, version);
            autoMod.TriggerType = autoModTable.GetValue<byte>("TriggerType", TypeFlag.UInt8, index, tableHelper, version);

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
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("AutoModulationTable");

            utfTable.AddValue("Type", TypeFlag.UInt8, index, Type.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("TriggerType", TypeFlag.UInt8, index, TriggerType.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("Time", TypeFlag.UInt32, index, Time.ToString(), tableHelper, ParseVersion);
            utfTable.AddValue("Key", TypeFlag.UInt32, index, Key.ToString(), tableHelper, ParseVersion);
        }
    }

    [Serializable]
    public class ACB_StringValue : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public string StringValue { get; set; }

        public ACB_StringValue() { }

        public ACB_StringValue(string stringValue, int index = -1)
        {
            StringValue = stringValue;
            Index = index;
        }

        public static List<ACB_StringValue> Load(UTF_File table, Version ParseVersion)
        {
            List<ACB_StringValue> rows = new List<ACB_StringValue>();
            if (table == null) return rows;

            for (int i = 0; i < table.DefaultRowCount; i++)
            {
                rows.Add(Load(table, i, ParseVersion));
            }

            return rows;
        }

        public static ACB_StringValue Load(UTF_File stringValueTable, int index, Version version)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("StringValueTable");

            ACB_StringValue stringValue = new ACB_StringValue();
            stringValue.Index = index;
            stringValue.StringValue = stringValueTable.GetValue<string>("StringValue", TypeFlag.String, index, tableHelper, version);

            return stringValue;
        }
        
        public static void WriteToTable(IList<ACB_StringValue> entries, Version ParseVersion, UTF_File utfTable)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].WriteToTable(utfTable, i, ParseVersion);
            }
        }

        public void WriteToTable(UTF_File utfTable, int index, Version ParseVersion)
        {
            AcbFormatHelperTable tableHelper = AcbFormatHelper.Instance.GetTableHelper("StringValueTable");

            utfTable.AddValue("StringValue", TypeFlag.String, index, StringValue, tableHelper, ParseVersion);
        }
        
        public static List<ACB_StringValue> DefaultStringTable()
        {
            List<ACB_StringValue> values = new List<ACB_StringValue>();

            values.Add(new ACB_StringValue("MasterOut", values.Count));
            values.Add(new ACB_StringValue("BUS1", values.Count));
            values.Add(new ACB_StringValue("BUS2", values.Count));
            values.Add(new ACB_StringValue("BUS3", values.Count));
            values.Add(new ACB_StringValue("BUS4", values.Count));
            values.Add(new ACB_StringValue("BUS5", values.Count));
            values.Add(new ACB_StringValue("BUS6", values.Count));
            values.Add(new ACB_StringValue("BUS7", values.Count));

            return values;
        }
    }

    [Serializable]
    [YAXSerializeAs("ReferenceItem")]
    public class ACB_ReferenceItem : IReferenceType
    {
        [YAXAttributeFor("ReferenceType")]
        [YAXSerializeAs("value")]
        public ReferenceType ReferenceType { get; set; } = ReferenceType.Waveform;
        [YAXSerializeAs("ReferenceIndex")]
        public AcbTableReference ReferenceIndex { get; set; } = new AcbTableReference(); //ushort

        public ACB_ReferenceItem() { }

        public ACB_ReferenceItem(ushort refType, ushort refIndex) 
        {
            ReferenceType = (ReferenceType)refType;
            ReferenceIndex = new AcbTableReference(refIndex);
        }

        public static List<ACB_ReferenceItem> Load(ushort[] values)
        {
            List<ACB_ReferenceItem> refItems = new List<ACB_ReferenceItem>();

            for(int i = 0; i < values.Length; i += 2)
            {
                if (values.Length <= i + 1) throw new ArgumentOutOfRangeException("ACB_ReferenceItem.Load: index out of range.");

                refItems.Add(new ACB_ReferenceItem(values[i], values[i + 1]));
            }

            return refItems;
        }

        public static byte[] Write(List<ACB_ReferenceItem> refItems)
        {
            List<byte> bytes = new List<byte>();

            foreach(var refItem in refItems)
            {
                bytes.AddRange(BigEndianConverter.GetBytes((ushort)refItem.ReferenceType));
                bytes.AddRange(BigEndianConverter.GetBytes((ushort)refItem.ReferenceIndex.TableIndex_Ushort));
            }

            return bytes.ToArray();
        }
    }

    #region Commands
    [Serializable]
    public class ACB_CommandTables
    {
        [YAXDontSerialize]
        public Version AcbVersion { get; set; }

        [YAXComment("Below ver 1.29.0.0: Always use SequenceCommand (just named CommandTable in the ACB file). Later versions separated the command tables.")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CommandTable")]
        public List<ACB_CommandTable> CommandTables { get; set; } = new List<ACB_CommandTable>();

        public IEnumerable<ACB_CommandGroup> GetIterator()
        {
            foreach(var commandTable in CommandTables)
            {
                foreach (var commandGroup in commandTable.CommandGroups)
                    yield return commandGroup;
            }
        }

        //todo: manager methods
        public List<IUndoRedo> AddCommand(ACB_CommandGroup command, CommandTableType type)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var table = GetCommandTable(type);
            table.CommandGroups.Add(command);
            undos.Add(new UndoableListAdd<ACB_CommandGroup>(table.CommandGroups, command));

            return undos;
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
            if (index >= ushort.MaxValue) return null;
            var table = GetCommandTable(type);

            if (index >= table.CommandGroups.Count)
                throw new IndexOutOfRangeException(string.Format("ACB_CommandTables.GetCommand: cannot get command at index {0} on {1} because it is out of range.", index, type));

            return table.CommandGroups[index];
        }

        public ACB_CommandGroup GetCommand(Guid guid, CommandTableType type, bool allowNull = true)
        {
            var table = GetCommandTable(type);

            if (table != null)
            {
                var result = table.CommandGroups.FirstOrDefault(x => x.InstanceGuid == guid);
                if(result == null && !allowNull)
                    throw new Exception($"ACB_CommandTables.GetCommand: Cannot find the command with the specified GUID.");

                return result;
            }
            else if (!allowNull)
                throw new Exception($"ACB_CommandTables.GetCommand: Cannot get the command with the specified GUID in CommandTable {type}. The CommandTable does not exist in the ACB.");
            else
                return null;
        }
        
        public ACB_CommandTable GetCommandTable(CommandTableType type)
        {
            //If on old ACB version, always return SequenceCommandTable
            if(AcbVersion < ACB_File._1_29_0_0)
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

        public Guid GetCommandGuid(int index, CommandTableType type)
        {
            var command = GetCommand(index, type);
            return (command != null) ? command.InstanceGuid : Guid.Empty;
        }

        public ushort GetCommandIndex(Guid guid, CommandTableType type)
        {
            var cmd = GetCommand(guid, type);
            return (ushort)GetCommandIndex(cmd, type);
        }

        public static ACB_CommandTables Load(UTF_File utfFile, ACB_File acbFile, bool loadUnknownCommands)
        {
            ACB_CommandTables commandTables = new ACB_CommandTables();
            commandTables.AcbVersion = acbFile.Version;
            var header = AcbFormatHelper.Instance.AcbFormatHelperMain.Header;

            if(header.ColumnExists("SeqCommandTable", TypeFlag.Data, acbFile.Version))
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("SeqCommandTable", true), CommandTableType.SequenceCommand, loadUnknownCommands));

            if (header.ColumnExists("TrackCommandTable", TypeFlag.Data, acbFile.Version))
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("TrackCommandTable", true), CommandTableType.TrackCommand, loadUnknownCommands));

            if (header.ColumnExists("SynthCommandTable", TypeFlag.Data, acbFile.Version))
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("SynthCommandTable", true), CommandTableType.SynthCommand, loadUnknownCommands));

            if (header.ColumnExists("TrackEventTable", TypeFlag.Data, acbFile.Version))
                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("TrackEventTable", true), CommandTableType.TrackEvent, loadUnknownCommands));

            if (header.ColumnExists("CommandTable", TypeFlag.Data, acbFile.Version))
            {
                if (header.ColumnExists("SeqCommandTable", TypeFlag.Data, acbFile.Version) || header.ColumnExists("TrackCommandTable", TypeFlag.Data, acbFile.Version) ||
                    header.ColumnExists("SynthCommandTable", TypeFlag.Data, acbFile.Version) || header.ColumnExists("TrackEventTable", TypeFlag.Data, acbFile.Version))
                {
                    throw new InvalidDataException($"CommandTable column cannot exist with SeqCommandTable, TrackCommandTable, SynthCommandTable or TrackEventTable.");
                }

                commandTables.CommandTables.Add(ACB_CommandTable.Load(utfFile.GetColumnTable("CommandTable", true), CommandTableType.SequenceCommand, loadUnknownCommands));
            }


            return commandTables;
        }

        public UTF_File WriteToTable(CommandTableType type)
        {
            foreach(var table in CommandTables)
            {
                if (table.Type == type) return table.WriteToTable(AcbVersion);
            }

            return ACB_CommandTable.CreateEmptyTable(AcbVersion, type);
        }


        //AcbTableReference linking
        public void LinkGuids(ACB_File root)
        {
            foreach(var cmdTable in CommandTables)
            {
                foreach(var cmdGroup in cmdTable.CommandGroups)
                {
                    foreach(var cmd in cmdGroup.Commands)
                    {
                        if(cmd.CommandType == CommandType.ReferenceItem || cmd.CommandType == CommandType.ReferenceItem2)
                        {
                            cmd.ReferenceIndex = new AcbTableReference(cmd.Param2);

                            switch ((ReferenceType)cmd.Param1)
                            {
                                case ReferenceType.Waveform:
                                    cmd.ReferenceIndex.TableGuid = root.GetTableGuid(cmd.Param2, root.Waveforms);
                                        break;
                                case ReferenceType.Synth:
                                    cmd.ReferenceIndex.TableGuid = root.GetTableGuid(cmd.Param2, root.Synths);
                                    break;
                                case ReferenceType.Sequence:
                                    cmd.ReferenceIndex.TableGuid = root.GetTableGuid(cmd.Param2, root.Sequences);
                                    break;
                            }
                        }
                        else if(cmd.CommandType == CommandType.GlobalAisacReference)
                        {
                            cmd.ReferenceIndex = new AcbTableReference(cmd.Param1);
                            cmd.ReferenceIndex.TableGuid = root.GetTableGuid(cmd.Param1, root.GlobalAisacReferences);
                        }
                        else if (cmd.CommandType == CommandType.VolumeBus || cmd.CommandType == CommandType.Bus)
                        {
                            cmd.ReferenceIndex = new AcbTableReference(cmd.Param1);
                            cmd.ReferenceIndex.TableGuid = root.GetTableGuid(cmd.Param1, root.StringValues);
                        }
                    }
                }
            }
        }

        public void LinkIndexes(ACB_File root)
        {
            foreach (var cmdTable in CommandTables)
            {
                foreach (var cmdGroup in cmdTable.CommandGroups)
                {
                    foreach (var cmd in cmdGroup.Commands)
                    {
                        if (cmd.ReferenceIndex == null) continue;

                        if (cmd.CommandType == CommandType.ReferenceItem || cmd.CommandType == CommandType.ReferenceItem2)
                        {
                            switch ((ReferenceType)cmd.Param1)
                            {
                                case ReferenceType.Waveform:
                                    cmd.ReferenceIndex.TableIndex = (ushort)root.GetTableIndex(cmd.ReferenceIndex.TableGuid, root.Waveforms);
                                    break;
                                case ReferenceType.Synth:
                                    cmd.ReferenceIndex.TableIndex = (ushort)root.GetTableIndex(cmd.ReferenceIndex.TableGuid, root.Synths);
                                    break;
                                case ReferenceType.Sequence:
                                    cmd.ReferenceIndex.TableIndex = (ushort)root.GetTableIndex(cmd.ReferenceIndex.TableGuid, root.Sequences);
                                    break;
                            }
                        }
                        else if (cmd.CommandType == CommandType.GlobalAisacReference)
                        {
                            cmd.ReferenceIndex.TableIndex = (ushort)root.GetTableIndex(cmd.ReferenceIndex.TableGuid, root.GlobalAisacReferences);
                        }
                        else if (cmd.CommandType == CommandType.VolumeBus || cmd.CommandType == CommandType.Bus)
                        {
                            cmd.ReferenceIndex.TableIndex = (ushort)root.GetTableIndex(cmd.ReferenceIndex.TableGuid, root.StringValues);
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public class ACB_CommandTable
    {
        [YAXAttributeForClass]
        public CommandTableType Type { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CommandGroup")]
        public List<ACB_CommandGroup> CommandGroups { get; set; } = new List<ACB_CommandGroup>();

        public static ACB_CommandTable Load(UTF_File commandTable, CommandTableType type, bool loadUnknownCommands)
        {
            ACB_CommandTable table = new ACB_CommandTable();
            table.Type = type;
            if (commandTable == null) return table;

            int numRows = commandTable.DefaultRowCount;

            for(int i = 0; i < numRows; i++)
            {
                table.CommandGroups.Add(ACB_CommandGroup.Load(commandTable, i, loadUnknownCommands));
            }

            return table;
        }

        public UTF_File WriteToTable(Version acbVersion)
        {
            UTF_File utfTable = CreateEmptyTable(acbVersion, Type);

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
    [Serializable]
    public class ACB_CommandGroup : AcbTableBase
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        public AsyncObservableCollection<ACB_Command> Commands { get; set; } = AsyncObservableCollection<ACB_Command>.Create();

        public static ACB_CommandGroup Load(UTF_File commandTable, int index, bool loadUnknownCommands)
        {
            ACB_CommandGroup commandGroup = new ACB_CommandGroup();
            commandGroup.Index = index;

            byte[] commandBytes = commandTable.GetData("Command", index);
            if (commandBytes == null) commandBytes = new byte[0];

            //Parse command bytes. If there are 3 or more bytes left, then there is another command to parse.
            int offset = 0;

            while (((commandBytes.Length) - offset) >= 3)
            {
                ACB_Command command = new ACB_Command();
                command.CommandType = (CommandType)BigEndianConverter.ReadUInt16(commandBytes, offset);
                command.Parameters = commandBytes.GetRange(offset + 3, commandBytes[offset + 2]).ToList();
                
                //Dont load unknown commands with more than 0 parameters unless loadUnknownCommands is true
                //For XML we want to load all commands, but otherwise they can cause issues when copying cues and rearanging tables so it's best to ignore them
                if ((Enum.IsDefined(typeof(CommandType), command.CommandType) || command.NumParameters == 0) || loadUnknownCommands)
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
                command.FinalizeParameters();
                bytes.AddRange(BigEndianConverter.GetBytes((ushort)command.CommandType));
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
    [Serializable]
    public class ACB_Command : INotifyPropertyChanged, IReferenceType
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Undoable
        [YAXDontSerialize]
        public CommandType UndoableCommandType
        {
            get
            {
                return CommandType;
            }
            set
            {
                if (CommandType != value)
                {
                    CommandType oldValue = CommandType;
                    CommandType = value;
                    UndoManager.Instance.AddUndo(new UndoableProperty<ACB_Command>("CommandType", this, oldValue, value, "Command Type"));
                    NotifyPropertyChanged("UndoableCommandType");
                }
            }
        }

        #endregion

        //Values


        //Properties
        [YAXDontSerialize]
        public string DisplayName { get { return $"Command: {CommandType}"; } }
        
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
                    NotifyPropertyChanged("DisplayName");
                    NotifyPropertyChanged("CommandType");
                    NotifyPropertyChanged("UndoableCommandType");
                }
            }
        }
        private CommandType _commandType = 0;

        [YAXDontSerialize]
        public byte NumParameters { get { return (byte)Parameters.Count; } }

        //Raw parameters. For some types some of these are handled by seperate props, when it is required (such as for linking to a table, since the index value wont be good enough for that)
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        public List<byte> Parameters { get; set; } = new List<byte>();

        //CommandType specific data:
        [YAXDontSerialize]
        public ReferenceType ReferenceType { get { return (ReferenceType)Param1; } set { Param1 = (ushort)value; } } //Quick access for the ReferenceType Command.
        //Used for any command types that require linking to other tables (via GUID). Linking is done on creation of the command or loading of the file (if required).
        [YAXDontSerialize]
        public AcbTableReference ReferenceIndex { get; set; }

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
            int newSize = 2 * (idx + 1);

            while (Parameters.Count < newSize)
            {
                Parameters.Add(0);
            }

            return BigEndianConverter.ReadUInt16(Parameters, adjustedIndex);
        }

        private void SetParam(int idx, ushort value)
        {
            if (Parameters == null) Parameters = new List<byte>();
            int adjustedIndex = idx * 2;
            int newSize = 2 * (idx + 1);

            //Expand Parameters if needed
            while (Parameters.Count < newSize)
            {
                Parameters.Add(0);
            }

            Utils.ReplaceRange(Parameters, BigEndianConverter.GetBytes(value), adjustedIndex);
        }
        #endregion

        public ACB_Command() { }

        /// <summary>
        /// Pre-initialize the values required for the command type.
        /// </summary>
        public ACB_Command(CommandType commandType)
        {
            CommandType = commandType;

            switch (commandType)
            {
                case CommandType.Bus:
                case CommandType.GlobalAisacReference:
                    Parameters = new byte[2].ToList();
                    ReferenceIndex = new AcbTableReference();
                    break;
                case CommandType.ReferenceItem:
                case CommandType.VolumeBus:
                    Parameters = new byte[4].ToList();
                    ReferenceIndex = new AcbTableReference();
                    break;
                case CommandType.VolumeRandomization1:
                    Parameters = new byte[2].ToList();
                    break;
                case CommandType.VolumeRandomization2:
                    Parameters = new byte[4].ToList();
                    break;
                case CommandType.CueLimit:
                    Parameters = new byte[5].ToList();
                    break;
                case CommandType.ReferenceItem2:
                    Parameters = new byte[6].ToList();
                    ReferenceIndex = new AcbTableReference();
                    break;
            }

        }


        public void FinalizeParameters()
        {
            if(ReferenceIndex != null)
            {
                switch (CommandType)
                {
                    case CommandType.GlobalAisacReference:
                    case CommandType.VolumeBus:
                    case CommandType.Bus:
                        Param1 = ReferenceIndex.TableIndex_Ushort;
                        break;
                    case CommandType.ReferenceItem:
                    case CommandType.ReferenceItem2:
                        Param2 = ReferenceIndex.TableIndex_Ushort;
                        break;
                }
            }
        }
    }

    #endregion

    #region Interfaces
    //Interfaces
    public interface IReferenceItems
    {
        List<ACB_ReferenceItem> ReferenceItems { get; set; }
    }

    public interface IReferenceType
    {
        ReferenceType ReferenceType { get; set; }
        AcbTableReference ReferenceIndex { get; set; }
    }

    public interface ILocalAisac
    {
        List<AcbTableReference> LocalAisac { get; set; }
    }

    public interface IGlobalAisacRef
    {
        List<AcbTableReference> GlobalAisacRefs { get; set; }
    }

    public interface ICommandIndex
    {
        AcbTableReference CommandIndex { get; set; }
    }

    public interface IEventIndex
    {
        AcbTableReference EventIndex { get; set; }
    }

    public interface IActionTrack
    {
        List<AcbTableReference> ActionTracks { get; set; }
    }

    public interface ITrack
    {
        AsyncObservableCollection<ACB_SequenceTrack> Tracks { get; set; }
    }

    public interface IAutoModulationIndex
    {
        AcbTableReference AutoModulationIndex { get; set; }
    }

    public interface IGraphIndexes
    {
        List<AcbTableReference> GraphIndexes { get; set; }
    }

    public interface ITargetId
    {
        TargetType TargetType { get; set; }
        AcbTableReference TargetId { get; set; }
        bool TargetSelf { get; set; }
    }

    public interface IBlock
    {
        List<AcbTableReference> Blocks { get; set; }
    }
    #endregion
    
    #region Enums
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
        DSP = 13,
        None = 255
    }
    
    public enum CommandType : ushort
    {
        //Reminder! All values are in big endian!
        Null = 0, //No parameters. Does nothing?.
        GlobalAisacReference = 0x004b, //2 parameters (1 uint16). 0 = GlobalAisacReference (index)
        CueLimit = 0x004f, //5 bytes, 1st value (uint16) = cue limit (Sequence only?)
        VolumeRandomization1 = 0x0058, //2 parameters (1 uint16) 0 = Random Range (scale 0-100, base volume is 0)
        VolumeRandomization2 = 0x0059, //4 parameters (2 uint16s) 0 = base volume, 1 = Random Range (scale 0-100)
        VolumeBus = 0x006f, //4 parameters (2 uint16s). 1st value = StringValue, 2nd value = volume, scale between 0 and 10000 (Sequence only)
        Action_Play = 0x1bbc,//No parameters. Uses target information from ActionTrack.
        Action_Stop = 0x1bbd,//No parameters. Uses target information from ActionTrack.
        ReferenceItem = 0x07d0, //4 parameters (2 uint16s). Identical to ReferenceType/ReferenceIndex on Cue and ReferenceItems on Sequence/Synth. (a Null command MUST follow or the game will crash)
        ReferenceItem2 = 0x07d3, //6 parameters (3 uint16s). Same as ReferenceItem, but with an additional 2 parameters. Requires Unk199 to function.
        LoopStart = 0x04b1, //4 parameters, 2 uint16s (0=LoopID,1=Loop Count)
        LoopEnd = 0x04b0, //6 parameters 3 uint16s (0=LoopID)
        Wait = 0x07d1, //4 parameters (1 uint32). Wait command, freezes command execution for desired time (0 = miliseconds)

        //Slightly known commands:
        Unk10 = 0x0010, //2 params (1 uint16). Seems to control volume, but only with a Unk85 entry present.
        Bus = 0x0055, //2 parameters (1 uint16). Points to a StringValue (Bus).

        //Unknown, but safe to copy commands - these are known not to have any references on them (invalid references will CRASH the game)
        //Added here so they dont get purged along with the other unknown (unsafe) commands
        Unk49 = 0x0031, //2 parameters
        Unk84 = 0x0054, //2 parameters
        Unk83 = 0x0053, //2 parameters
        Unk11 = 0x000b, //2 parameters
        Unk14 = 0x000e, //2 parameters
        Unk36 = 0x0024, //4 parameters
        Unk38 = 0x0026, //4 parameters
        Unk40 = 0x0028, //4 parameters
        Unk45 = 0x002d, //2 parameters
        Unk47 = 0x002f, //2 parameters
        Unk5 = 0x0005, //2 parameters
        Unk70 = 0x0046, //1 parameter
        Unk86 = 0x0056, //2 parameters
        Unk199 = 0x07c7, //6 parameters. Required for ReferencedItem2 to function (must preceed it).
        Unk65 = 0x0041, //4 parameters
        Unk87 = 0x0057, //2 parameters

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
        TrackCommand, //Use by Track Commands
        TrackEvent //Used by Track Events and ActionTrack Commands (assuming Action events as well...)
    }

    public enum TargetType : byte
    {
        AnyAcb = 0,
        SpecificAcb = 1
    }
    #endregion


    [Serializable]
    public class AcbTableBase : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion 

        /// <summary>
        /// The ID for this instance. Used in copying operations.
        /// </summary>
        public Guid InstanceGuid = Guid.NewGuid();
    }

    [Serializable]
    public class AcbTableReference
    {
        [YAXDontSerialize]
        private TypeFlag valueType = TypeFlag.UInt16;

        [YAXDontSerialize]
        internal Guid _tableGuid = Guid.Empty;

        [YAXAttributeForClass]
        [YAXSerializeAs("value")]
        public uint TableIndex { get; set; } //Set upon loading and will be written directly to UTF file when saving.
        [YAXDontSerialize]
        public Guid TableGuid { get { return _tableGuid; } set { _tableGuid = value; } } //Set this after loading all tables. Used as a better way to identify tables, even accross multiple instances.

        [YAXDontSerialize]
        public bool IsNull { get { return TableGuid == Guid.Empty; } }
        [YAXDontSerialize]
        public int TableIndex_Int { get { return (int)TableIndex; } }
        [YAXDontSerialize]
        public ushort TableIndex_Ushort { get { return (ushort)TableIndex; } }


        public AcbTableReference()
        {
            TableIndex = ushort.MaxValue;
        }
        
        public AcbTableReference(Guid guid)
        {
            TableGuid = guid;
        }

        public AcbTableReference(byte index)
        {
            TableIndex = index;
        }

        public AcbTableReference(ushort index)
        {
            TableIndex = index;
        }

        public AcbTableReference(int index)
        {
            if (index < ushort.MinValue || index > ushort.MaxValue) throw new ArgumentOutOfRangeException("AcbTableReference.ctor(int index) : index is out of bounds of ushort.MinValue or ushort.MaxValue.");
            TableIndex = (ushort)index;
        }

        public AcbTableReference(uint index)
        {
            TableIndex = index;
            valueType = TypeFlag.UInt32;
        }

        public bool Compare(AcbTableReference tableRef)
        {
            if (tableRef.TableGuid == Guid.Empty) return false;
            return (TableGuid == tableRef.TableGuid);
        }

        public override string ToString()
        {
            if(valueType == TypeFlag.UInt16)
            {
                return (TableIndex > ushort.MaxValue) ? ushort.MaxValue.ToString() : TableIndex.ToString();
            }
            //else if (valueType == TypeFlag.UInt32)
            //{
            //    return (TableIndex >= ushort.MaxValue) ? uint.MaxValue.ToString() : TableIndex.ToString();
            //}
            else 
            {
                return TableIndex.ToString();
            }
        }

        public static List<AcbTableReference> FromArray(IEnumerable<ushort> array)
        {
            List<AcbTableReference> list = new List<AcbTableReference>();
            if (array == null) return list;

            foreach (var val in array)
                list.Add(new AcbTableReference(val));

            return list;
        }

        public static ushort[] ToArray(IList<AcbTableReference> list)
        {
            if (list == null) return new ushort[0];
            List<ushort> array = new List<ushort>();

            foreach(var val in list)
            {
                array.Add((ushort)val.TableIndex);
            }

            return array.ToArray();
        }
    }
    
}
