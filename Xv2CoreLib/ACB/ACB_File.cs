using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.Resource;
using Xv2CoreLib.UTF;
using YAXLib;

namespace Xv2CoreLib.ACB
{
    //MADE OBSOLETE BY ACB_NEW - USE THAT INSTEAD!!!
    //Can copy cues between ACB files, and thats about it.
    //Keeping this here because the skill/moveset merger still uses it currently.

    [Obsolete("Use ACB_NEW instead. This is an old legacy ACB parser with very limited (and buggy) functionality.")]
    public class ACB_File
    {
        #region Versions
        //Supported Versions
        public static Version _1_30_0_0 = new Version("1.30.0.0"); //SDBH. Adds SoundGeneratorTable.
        public static Version _1_29_2_0 = new Version("1.29.2.0"); //SDBH
        public static Version _1_29_0_0 = new Version("1.29.0.0"); //SDBH. Splits CommandTable up into several tables, and add some tables for PalleteParameter
        public static Version _1_28_2_0 = new Version("1.28.2.0"); //SDBH, identical to 1.27.7.0
        public static Version _1_27_7_0 = new Version("1.27.7.0"); //Used in XV2
        public static Version _1_27_2_0 = new Version("1.27.2.0"); //Used in XV2
        public static Version _1_22_4_0 = new Version("1.22.4.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public static Version _1_22_0_0 = new Version("1.22.0.0"); //Not observed, just a guess
        public static Version _1_21_1_0 = new Version("1.21.1.0"); //Used in XV1, and a few old XV2 ACBs (leftovers from xv1)
        public static Version _1_6_0_0 = new Version("1.6.0.0"); //Used in 1 file in XV1. Missing a lot of data.
        
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
        
        //Current version information
        public Version Version = null;
        private Version ParseVersion = null;

        #endregion

        //Saving and Loading
        public string FilePath = null; //Without extension
        private Xv2FileIO FileIO = null;

        //UTF
        public UTF_File UtfFile { get; set; }

        //Awb
        public AFS2_File MemoryAwb { get; set; }
        public AFS2_File StreamAwb { get; set; }

        #region Constructors
        /// <summary>
        /// Parse an ACB File from disk.
        /// </summary>
        public ACB_File(string path)
        {
            FilePath = string.Format("{0}{2}{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), Path.DirectorySeparatorChar);
            LoadFiles(File.ReadAllBytes(path));
            VersionValidation();
            ValidateColumns();
        }

        /// <summary>
        /// Parse an ACB File from game.
        /// </summary>
        public ACB_File(string path, Xv2FileIO fileIO)
        {
            FilePath = string.Format("{0}{2}{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), Path.DirectorySeparatorChar);
            FileIO = fileIO;
            LoadFiles(fileIO.GetFileFromGame(path));
            VersionValidation();
            ValidateColumns();
        }
        #endregion

        #region FileLoadSave
        /// <summary>
        /// Load the raw files for ACB, Memory Awb and Streaming Awb.
        /// </summary>
        private void LoadFiles(byte[] acbBytes)
        {
            UtfFile = UTF_File.LoadUtfTable(acbBytes, acbBytes.Length);

            var awbTable = UtfFile.GetColumn("AwbFile");
            if (awbTable == null) throw new InvalidDataException(string.Format("Failed parsing \"{0}\". Could not locate the \"AwbFile\" column.", FilePath + ".acb"));

            MemoryAwb = UtfFile.GetColumnAfs2File("AwbFile");

            //Just load a external AWB if one exists with the same name as the ACB
            string awbPath = string.Format("{0}/{1}.awb", Path.GetDirectoryName(FilePath), Path.GetFileName(FilePath));

            if (FileIO == null)
            {
                if (File.Exists(awbPath))
                {
                    StreamAwb = AFS2_File.LoadAfs2File(awbPath);
                }
            }
            else
            {
                if (FileIO.FileExists(awbPath))
                    StreamAwb = AFS2_File.LoadAfs2File(FileIO.GetFileFromGame(awbPath));
            }
        }

        private void VersionValidation()
        {
            Version = BigEndianConverter.UIntToVersion(UtfFile.GetValue<uint>("Version", TypeFlag.UInt32, 0), true);
            ParseVersion = Version; //In the future make this match the CLOSEST supported version. This will allow unsupported versions to still be parsed.

            if (!ACB_File.SupportedVersions.Contains(Version))
            {
                throw new InvalidDataException(string.Format("ACB version {0} is not supported.", Version));
            }
        }

        private void ValidateColumns()
        {
            if (UtfFile.GetColumnTable("CueLimitWorkTable") != null)
                throw new InvalidDataException("CueLimitWorkTable is a UtfTable while bytes were expected. Parse failed.");

            if (UtfFile.ColumnHasData("EventTable", 0))
                throw new InvalidDataException("EventTable has data. Parse failed.");

            if (UtfFile.ColumnHasData("OutsideLinkTable", 0))
                throw new InvalidDataException("OutsideLinkTable has data. Parse failed.");

            if (UtfFile.ColumnHasData("AisacNameTable", 0))
                throw new InvalidDataException("AisacNameTable has data. Parse failed.");

            //if (utfFile.ColumnHasData("CueTable", 0))
            //    throw new InvalidDataException("CueTable has data. Parse failed.");

            if (ParseVersion >= ACB_File._1_27_2_0)
            {
                if (UtfFile.ColumnHasData("BeatSyncInfoTable", 0))
                    throw new InvalidDataException("BeatSyncInfoTable has data. Parse failed.");

            }

            if (ParseVersion >= ACB_File._1_29_0_0)
            {
                if (UtfFile.ColumnHasData("SeqParameterPalletTable", 0))
                    throw new InvalidDataException("SeqParameterPalletTable has data. Parse failed.");

                if (UtfFile.ColumnHasData("TrackParameterPalletTable", 0))
                    throw new InvalidDataException("TrackParameterPalletTable has data. Parse failed.");

                if (UtfFile.ColumnHasData("SynthParameterPalletTable", 0))
                    throw new InvalidDataException("SynthParameterPalletTable has data. Parse failed.");
            }
        }

        public void Save(string path = null)
        {
            if (!string.IsNullOrWhiteSpace(path))
                FilePath = (Path.GetExtension(path) != ".acb") ? path : string.Format("{0}{1}{2}", Path.GetDirectoryName(path), Path.DirectorySeparatorChar, Path.GetFileNameWithoutExtension(path));

            string acbPath = FilePath + ".acb";
            string awbPath = FilePath + ".awb";

            //Set AwbFile column
            if(MemoryAwb != null)
            {
                UtfFile.SetAfs2File("AwbFile", MemoryAwb);
            }

            //Save ACB
            UtfFile.Save(acbPath);

            //Save Stream AWB
            if(StreamAwb != null)
            {
                //Maybe update hashes and headers in this the future, IF i decide to add tracks to StreamAwb.
                StreamAwb.Save(awbPath);
            }

        }

        #endregion

        #region PublicInteract
        /// <summary>
        /// Add a cue from another ACB file.
        /// </summary>
        /// <param name="importAcb">The ACB file to import a cue from.</param>
        /// <param name="cueId">The cue ID to import.</param>
        /// <returns></returns>
        public int AddCue(ACB_File importAcb, int cueId, bool reuseCueWithSameName = true)
        {
            //Get rowIdx for this cue ID
            int rowIdx = importAcb.UtfFile.IndexOfRow("CueTable", "CueId", cueId.ToString());

            //Check that cue ID actually exists in importAcb
            if (rowIdx == -1)
            {
                throw new InvalidDataException(string.Format("A cue with the CueId {0} does not exist in \"{1}\".", cueId, importAcb.FilePath + ".acb"));
            }

            //First check if cue already exists in this ACB and return that cue ID if it does.
            int existingCueId = GetCueId(importAcb.GetCueName(cueId));

            if (existingCueId != -1 && reuseCueWithSameName)
            {
                return existingCueId;
            }

            //Cue doesn't exist, so now it's time to add it.
            var table = UtfFile.GetColumnTable("CueTable");
            var importTable = importAcb.UtfFile.GetColumnTable("CueTable");

            //Generate new rowIdx and cueId
            int newRowIdx = table.RowCount();
            int newCueId = GetNextCueID();

            //Add data
            table.AddValue("CueId", TypeFlag.UInt32, newRowIdx, newCueId.ToString());
            table.AddValue("UserData", TypeFlag.String, newRowIdx, importTable.GetValue<string>("UserData", TypeFlag.String, rowIdx).ToString());
            table.AddValue("Worksize", TypeFlag.UInt16, newRowIdx, importTable.GetValue<ushort>("Worksize", TypeFlag.UInt16, rowIdx).ToString());
            table.AddData("AisacControlMap", newRowIdx, importTable.GetData("AisacControlMap", rowIdx));
            table.AddValue("Length", TypeFlag.UInt32, newRowIdx, importTable.GetValue<uint>("Length", TypeFlag.UInt32, rowIdx).ToString());
            table.AddValue("NumAisacControlMaps", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("NumAisacControlMaps", TypeFlag.UInt8, rowIdx).ToString());

            if (ParseVersion >= _1_22_0_0)
            {
                if (importAcb.ParseVersion >= _1_22_0_0)
                {
                    table.AddValue("HeaderVisibility", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("HeaderVisibility", TypeFlag.UInt8, rowIdx).ToString());
                }
                else
                {
                    //Current ACB has HeaderVisibility but importAcb does not. Set it to 1.
                    table.AddValue("HeaderVisibility", TypeFlag.UInt8, newRowIdx, "1");
                }
            }

            //ReferenceItem
            ReferenceType refType = (ReferenceType)importTable.GetValue<byte>("ReferenceType", TypeFlag.UInt8, rowIdx);
            ushort refIndex = importTable.GetValue<ushort>("ReferenceIndex", TypeFlag.UInt16, rowIdx);

            //Write them to UTF... but update then later. This keeps the rows in sync for when recursively adding new cue from an action track
            table.AddValue("ReferenceType", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("ReferenceType", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("ReferenceIndex", TypeFlag.UInt16, newRowIdx, refIndex.ToString());


            //CueName
            AddCueName(importAcb, cueId, newRowIdx);

            //RefType
            switch (refType)
            {
                case ReferenceType.Sequence:
                    refIndex = (ushort)AddSequence(importAcb, refIndex);
                    break;
                case ReferenceType.Synth:
                    refIndex = (ushort)AddSynth(importAcb, refIndex);
                    break;
                case ReferenceType.Waveform:
                    refIndex = (ushort)AddWaveform(importAcb, refIndex);
                    break;
                case ReferenceType.BlockSequence:
                    throw new InvalidDataException(string.Format("BlockSequence not implemented (failed to import data from \"{0}\")", importAcb.FilePath + ".acb"));
            }

            table.SetValue<ushort>("ReferenceIndex", refIndex, TypeFlag.UInt16, newRowIdx);
            


            return newCueId;
        }

        public List<ACB_Cue> GetCueList()
        {
            List<ACB_Cue> cues = new List<ACB_Cue>();
            var table = UtfFile.GetColumnTable("CueTable");

            for(int i = 0; i < table.RowCount(); i++)
            {
                cues.Add(new ACB_Cue()
                {
                    CueId = (int)table.GetValue<uint>("CueId", TypeFlag.UInt32, i),
                    Name = GetCueNameFromCueIndex(i)
                });
            }

            return cues;
        }

        public bool CueExists(string cueName)
        {
            return GetCueId(cueName) != -1;
        }

        #endregion

        #region PrivateAddMethods

        private void AddCueName(ACB_File importAcb, int importCueId, int newCueIdx)
        {
            var table = UtfFile.GetColumnTable("CueNameTable");
            string name = importAcb.GetCueName(importCueId);

            int newRowIdx = table.RowCount();
            
            table.AddValue("CueName", TypeFlag.String, newRowIdx, name);
            table.AddValue("CueIndex", TypeFlag.UInt16, newRowIdx, newCueIdx.ToString());
        }

        private int AddSequence(ACB_File importAcb, int rowIdx)
        {
            var table = UtfFile.GetColumnTable("SequenceTable");
            var importTable = importAcb.UtfFile.GetColumnTable("SequenceTable");

            //Create table if it doesn't exist
            if (table == null)
            {
                table = GetDefaultSequenceTable();
                UtfFile.SetTable("SequenceTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add data
            table.AddValue("PlaybackRatio", TypeFlag.UInt16, newRowIdx, importTable.GetValue<ushort>("PlaybackRatio", TypeFlag.UInt16, rowIdx).ToString());
            table.AddValue("ParameterPallet", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddData("TrackValues",  newRowIdx, null);
            table.AddValue("Type", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("Type", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("ControlWorkArea1", TypeFlag.UInt16, newRowIdx, newRowIdx.ToString());
            table.AddValue("ControlWorkArea2", TypeFlag.UInt16, newRowIdx, newRowIdx.ToString());

            //Write initial values to keep rows in sync... replace after with SetValue
            table.AddValue("NumTracks", TypeFlag.UInt16, newRowIdx, "0");
            table.AddData("TrackIndex", newRowIdx, null);
            table.AddValue("CommandIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddValue("NumActionTracks", TypeFlag.UInt16, newRowIdx, "0");
            table.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddData("LocalAisacs", newRowIdx, null);
            table.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, newRowIdx, "0");

            //Track
            int numTrack = importTable.GetValue<ushort>("NumTracks", TypeFlag.UInt16, rowIdx);
            ushort[] tracks = BigEndianConverter.ToUInt16Array(importTable.GetData("TrackIndex", rowIdx));

            for(int i = 0; i < tracks.Length; i++)
            {
                if(tracks[i] != ushort.MaxValue)
                {
                    tracks[i] = (ushort)AddTracks(importAcb, tracks[i], 1, false);
                }
            }

            table.SetValue("NumTracks", numTrack, TypeFlag.UInt16, newRowIdx);
            table.SetData("TrackIndex", BigEndianConverter.GetBytes(tracks), newRowIdx);

            //Command
            ushort commandIdx = importTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, rowIdx);

            if (commandIdx != ushort.MaxValue)
            {
                commandIdx = (ushort)AddCommand(importAcb, commandIdx, CommandTableType.TrackCommand);
            }

            table.SetValue("CommandIndex", commandIdx, TypeFlag.UInt16, newRowIdx);

            //ActionTrack
            int numActionTrack = importTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, rowIdx);
            int actionTrackIndex = importTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, rowIdx);

            if(actionTrackIndex != ushort.MaxValue)
            {
                actionTrackIndex = AddTracks(importAcb, actionTrackIndex, numActionTrack, true);
            }

            table.SetValue("NumActionTracks", numActionTrack,TypeFlag.UInt16, newRowIdx);
            table.SetValue("ActionTrackStartIndex", actionTrackIndex,TypeFlag.UInt16, newRowIdx);

            //Aisacs
            ushort[] aisacIndexes = BigEndianConverter.ToUInt16Array(importTable.GetData("LocalAisacs", rowIdx));

            for (int a = 0; a < aisacIndexes.Length; a++)
            {
                if (aisacIndexes[a] != ushort.MaxValue)
                {
                    aisacIndexes[a] = (ushort)AddAisac(importAcb, aisacIndexes[a]);
                }
            }

            table.SetData("LocalAisacs", BigEndianConverter.GetBytes(aisacIndexes),newRowIdx);

            //Global Aisacs
            ushort globalAisacIndex = importTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, rowIdx);
            ushort globalAisacCount = importTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, rowIdx);

            if (globalAisacIndex != ushort.MaxValue)
            {
                globalAisacIndex = (ushort)AddGlobalAisacReferences(importAcb, globalAisacIndex, globalAisacCount);
            }

            table.SetValue("GlobalAisacStartIndex", globalAisacIndex, TypeFlag.UInt16, newRowIdx);
            table.SetValue("GlobalAisacNumRefs", globalAisacCount, TypeFlag.UInt16, newRowIdx);


            //Return
            return newRowIdx;
        }

        private int AddSynth(ACB_File importAcb, int rowIdx)
        {
            var table = UtfFile.GetColumnTable("SynthTable");
            var importTable = importAcb.UtfFile.GetColumnTable("SynthTable");

            //Create table if it doesn't exist
            if (table == null)
            {
                table = GetDefaultSynthTable();
                UtfFile.SetTable("SynthTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add data
            table.AddValue("Type", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("Type", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("VoiceLimitGroupName", TypeFlag.String, newRowIdx, importTable.GetValue<string>("VoiceLimitGroupName", TypeFlag.String, rowIdx).ToString());
            table.AddValue("ControlWorkArea1", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddValue("ControlWorkArea2", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddData("TrackValues", newRowIdx, null);
            table.AddValue("ParameterPallet", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());

            //Write initial values
            table.AddData("ReferenceItems", newRowIdx, null);
            table.AddValue("CommandIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddValue("NumActionTracks", TypeFlag.UInt16, newRowIdx, "0");
            table.AddValue("ActionTrackStartIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddData("LocalAisacs", newRowIdx, null);
            table.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
            table.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, newRowIdx, "0");

            //ReferenceItems
            ushort[] refItems = BigEndianConverter.ToUInt16Array(importTable.GetData("ReferenceItems", rowIdx));

            if(refItems.Length == 2)
            {
                switch ((ReferenceType)refItems[0])
                {
                    case ReferenceType.Sequence:
                        refItems[1] = (ushort)AddSequence(importAcb, refItems[1]);
                        break;
                    case ReferenceType.Synth:
                        refItems[1] = (ushort)AddSynth(importAcb, refItems[1]);
                        break;
                    case ReferenceType.Waveform:
                        refItems[1] = (ushort)AddWaveform(importAcb, refItems[1]);
                        break;
                    case ReferenceType.BlockSequence:
                        throw new InvalidDataException(string.Format("BlockSequence not implemented (failed to import data from \"{0}\")", importAcb.FilePath + ".acb"));
                }

            }
            else if(refItems.Length != 0)
            {
                throw new InvalidDataException(string.Format("Invalid ReferenceItems size on {0}.\nSize = {1}\nExpectedSize = 0 or 2", importAcb.FilePath + ".acb", refItems.Length));
            }

            table.SetData("ReferenceItems", BigEndianConverter.GetBytes(refItems), newRowIdx);

            //Command
            ushort commandIdx = importTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, rowIdx);

            if (commandIdx != ushort.MaxValue)
            {
                commandIdx = (ushort)AddCommand(importAcb, commandIdx, CommandTableType.TrackCommand);
            }

            table.SetValue("CommandIndex", commandIdx, TypeFlag.UInt16, newRowIdx);

            //ActionTrack
            int numActionTrack = importTable.GetValue<ushort>("NumActionTracks", TypeFlag.UInt16, rowIdx);
            int actionTrackIndex = importTable.GetValue<ushort>("ActionTrackStartIndex", TypeFlag.UInt16, rowIdx);

            if (actionTrackIndex != ushort.MaxValue)
            {
                actionTrackIndex = AddTracks(importAcb, actionTrackIndex, numActionTrack, true);
            }

            table.SetValue("NumActionTracks", numActionTrack, TypeFlag.UInt16, newRowIdx);
            table.SetValue("ActionTrackStartIndex", actionTrackIndex, TypeFlag.UInt16, newRowIdx);

            //Aisacs
            ushort[] aisacIndexes = BigEndianConverter.ToUInt16Array(importTable.GetData("LocalAisacs", rowIdx));

            for (int a = 0; a < aisacIndexes.Length; a++)
            {
                if (aisacIndexes[a] != ushort.MaxValue)
                {
                    aisacIndexes[a] = (ushort)AddAisac(importAcb, aisacIndexes[a]);
                }
            }

            table.SetData("LocalAisacs", BigEndianConverter.GetBytes(aisacIndexes), newRowIdx);

            //Global Aisacs
            ushort globalAisacIndex = importTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, rowIdx);
            ushort globalAisacCount = importTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, rowIdx);

            if (globalAisacIndex != ushort.MaxValue)
            {
                globalAisacIndex = (ushort)AddGlobalAisacReferences(importAcb, globalAisacIndex, globalAisacCount);
            }

            table.SetValue("GlobalAisacStartIndex", globalAisacIndex, TypeFlag.UInt16, newRowIdx);
            table.SetValue("GlobalAisacNumRefs", globalAisacCount, TypeFlag.UInt16, newRowIdx);

            //Return
            return newRowIdx;

        }

        private int AddTracks(ACB_File importAcb, int rowIdx, int count, bool isActionTrack)
        {
            //Determine which UtfTable to use (Track vs ActionTrack)
            string tableName = (!isActionTrack) ? "TrackTable" : "ActionTrackTable";
            string tableName2 = (!isActionTrack) ? "Track" : "ActionTrack";

            //Load table
            var table = UtfFile.GetColumnTable(tableName);
            var importTable = importAcb.UtfFile.GetColumnTable(tableName, true);

            //Create table if needed
            if(table == null)
            {
                table = GetDefaultTrackTable(tableName2);
                UtfFile.SetTable(tableName, table);
            }

            int newRowIdx = table.RowCount();


            for(int i = 0; i < count; i++)
            {
                //Add row
                table.AddValue("ParameterPallet", TypeFlag.UInt16, newRowIdx + i, ushort.MaxValue.ToString()); //Unknown, but it looks like Row Index so we dont want to copy it
                table.AddValue("Scope", TypeFlag.UInt8, newRowIdx + i, importTable.GetValue<byte>("Scope", TypeFlag.UInt8, rowIdx + i).ToString());
                table.AddValue("TargetTrackNo", TypeFlag.UInt16, newRowIdx + i, importTable.GetValue<ushort>("TargetTrackNo", TypeFlag.UInt16, rowIdx + i).ToString());

                //Target
                string importAcbName = importAcb.UtfFile.GetValue<string>("Name", TypeFlag.String, 0);
                string targetAcbName = importTable.GetValue<string>("TargetAcbName", TypeFlag.String, rowIdx + i);
                uint targetId = importTable.GetValue<uint>("TargetId", TypeFlag.UInt32, rowIdx + i);

                if(targetAcbName == importAcbName && targetId != ushort.MaxValue)
                {
                    //TODO: Research TargetType more.
                    //For now, assume all targets are cueIDs
                    //targetId = (uint)AddCue(importAcb, (int)targetId);
                    targetAcbName = UtfFile.GetValue<string>("Name", TypeFlag.String, 0);
                    
                }

                table.AddValue("TargetType", TypeFlag.UInt8, newRowIdx + i, importTable.GetValue<byte>("TargetType", TypeFlag.UInt8, rowIdx + i).ToString());
                table.AddValue("TargetName", TypeFlag.String, newRowIdx + i, importTable.GetValue<string>("TargetName", TypeFlag.String, rowIdx + i).ToString());
                table.AddValue("TargetId", TypeFlag.UInt32, newRowIdx + i, targetId.ToString());
                table.AddValue("TargetAcbName", TypeFlag.String, newRowIdx + i, targetAcbName.ToString());

                //Set initial values
                table.AddData("LocalAisacs", newRowIdx + i, null);
                table.AddValue("GlobalAisacStartIndex", TypeFlag.UInt16, newRowIdx + i, ushort.MaxValue.ToString());
                table.AddValue("GlobalAisacNumRefs", TypeFlag.UInt16, newRowIdx + i, "0");
                table.AddValue("EventIndex", TypeFlag.UInt16, newRowIdx + i, ushort.MaxValue.ToString());
                table.AddValue("CommandIndex", TypeFlag.UInt16, newRowIdx + i, ushort.MaxValue.ToString());

                //Aisacs
                ushort[] aisacIndexes = BigEndianConverter.ToUInt16Array(importTable.GetData("LocalAisacs", rowIdx + i));

                for (int a = 0; a < aisacIndexes.Length; a++)
                {
                    if (aisacIndexes[a] != ushort.MaxValue)
                    {
                        aisacIndexes[a] = (ushort)AddAisac(importAcb, aisacIndexes[a]);
                    }
                }

                table.SetData("LocalAisacs", BigEndianConverter.GetBytes(aisacIndexes), newRowIdx + i);

                //Global Aisacs
                ushort globalAisacIndex = importTable.GetValue<ushort>("GlobalAisacStartIndex", TypeFlag.UInt16, rowIdx + i);
                ushort globalAisacCount = importTable.GetValue<ushort>("GlobalAisacNumRefs", TypeFlag.UInt16, rowIdx + i);

                if (globalAisacIndex != ushort.MaxValue)
                {
                    globalAisacIndex = (ushort)AddGlobalAisacReferences(importAcb, globalAisacIndex, globalAisacCount);
                }

                table.SetValue("GlobalAisacStartIndex", globalAisacIndex, TypeFlag.UInt16, newRowIdx + i);
                table.SetValue("GlobalAisacNumRefs", globalAisacCount, TypeFlag.UInt16, newRowIdx + i);
                

            }

            //Add Commands and Events (after all rows have been added, as Commands can also add new Tracks)
            for (int i = 0; i < count; i++)
            {
                //Events
                ushort eventIdx = importTable.GetValue<ushort>("EventIndex", TypeFlag.UInt16, rowIdx + i);

                if (eventIdx != ushort.MaxValue)
                {
                    eventIdx = (ushort)AddCommand(importAcb, eventIdx, CommandTableType.TrackEvent);
                }

                table.SetValue("EventIndex", eventIdx, TypeFlag.UInt16, newRowIdx + i);

                //Commands
                ushort commandIdx = importTable.GetValue<ushort>("CommandIndex", TypeFlag.UInt16, rowIdx + i);

                if (commandIdx != ushort.MaxValue)
                {
                    commandIdx = (ushort)AddCommand(importAcb, commandIdx, CommandTableType.TrackCommand);
                }

                table.SetValue("CommandIndex", commandIdx, TypeFlag.UInt16, newRowIdx + i);
            }

            //Add target stuff (needs to be done AFTER all rows are added, or they go out of sync if we add a new cue recursivelly)
            for (int i = 0; i < count; i++)
            {
                string acbName = UtfFile.GetValue<string>("Name", TypeFlag.String, 0);
                string targetAcbName = table.GetValue<string>("TargetAcbName", TypeFlag.String, newRowIdx + i);
                uint targetId = table.GetValue<uint>("TargetId", TypeFlag.UInt32, newRowIdx + i);

                if (targetAcbName == acbName && targetId != ushort.MaxValue)
                {
                    targetId = (uint)AddCue(importAcb, (int)targetId);
                }
                
                table.SetValue<uint>("TargetId", targetId, TypeFlag.UInt32, newRowIdx + i);
            }

            return newRowIdx;
        }
        
        private int AddCommand(ACB_File importAcb, int rowIdx, CommandTableType type)
        {
            //Determine which CommandTable to use based on ACB Version.
            string commandTableNameFrom = "CommandTable";
            string commandTableNameTo = "CommandTable";

            if(importAcb.ParseVersion >= _1_29_0_0)
            {
                switch (type)
                {
                    case CommandTableType.SequenceCommand:
                        commandTableNameFrom = "SeqCommandTable";
                        break;
                    case CommandTableType.SynthCommand:
                        commandTableNameFrom = "SynthCommandTable";
                        break;
                    case CommandTableType.TrackCommand:
                        commandTableNameFrom = "TrackCommandTable";
                        break;
                    case CommandTableType.TrackEvent:
                        commandTableNameFrom = "TrackEventTable";
                        break;
                }
            }

            if (ParseVersion >= _1_29_0_0)
            {
                switch (type)
                {
                    case CommandTableType.SequenceCommand:
                        commandTableNameTo = "SeqCommandTable";
                        break;
                    case CommandTableType.SynthCommand:
                        commandTableNameTo = "SynthCommandTable";
                        break;
                    case CommandTableType.TrackCommand:
                        commandTableNameTo = "TrackCommandTable";
                        break;
                    case CommandTableType.TrackEvent:
                        commandTableNameTo = "TrackEventTable";
                        break;
                }
            }
            
            //Load command bytes and parse them
            var data = importAcb.UtfFile.GetData(commandTableNameFrom, "Command", rowIdx);
            List<ACB_Command> commands = ParseCommands(data);

            //Remove unknown commands
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if(!Enum.IsDefined(typeof(CommandType), commands[i].CommandType))
                {
                    commands.RemoveAt(i);
                    continue;
                }
            }

            //Add null command if all were removed
            if(commands.Count == 0)
            {
                commands.Add(new ACB_Command() { CommandType = CommandType.Null, Parameters = new List<byte>() });
            }

            //Add any known linked entry in Commands
            foreach (var command in commands)
            {
                switch (command.CommandType)
                {
                    case CommandType.ReferenceItem:
                        ReferenceType refType = (ReferenceType)command.Param1;
                        ushort refIndex = command.Param2;

                        switch (refType)
                        {
                            case ReferenceType.Sequence:
                                command.Param2 = (ushort)AddSequence(importAcb, refIndex);
                                break;
                            case ReferenceType.Synth:
                                command.Param2 = (ushort)AddSynth(importAcb, refIndex);
                                break;
                            case ReferenceType.Waveform:
                                command.Param2 = (ushort)AddWaveform(importAcb, refIndex);
                                break;
                            case ReferenceType.BlockSequence:
                                throw new InvalidDataException(string.Format("BlockSequence not implemented (failed to import data from \"{0}\")", importAcb.FilePath + ".acb"));
                        }

                        break;
                }
            }

            //Save commands to bytes and add it
            int newRowIdx = UtfFile.GetColumnTable(commandTableNameTo).RowCount();
            byte[] newCommandBytes = ACB_Command.GetBytes(commands);
            UtfFile.AddData(commandTableNameTo, "Command", newRowIdx, newCommandBytes);

            //Return RowIndex of newly added Commands
            return newRowIdx;
        }

        private int AddWaveform(ACB_File importAcb, int rowIdx)
        {
            var table = UtfFile.GetColumnTable("WaveformTable", true);
            var importTable = importAcb.UtfFile.GetColumnTable("WaveformTable", true);
            int newRowIdx = table.RowCount();

            //Add row
            table.AddValue("EncodeType", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("EncodeType", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("Streaming", TypeFlag.UInt8, newRowIdx, "0"); //We always add tracks to internal AWB
            table.AddValue("NumChannels", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("NumChannels", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("LoopFlag", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("LoopFlag", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("SamplingRate", TypeFlag.UInt16, newRowIdx, importTable.GetValue<ushort>("SamplingRate", TypeFlag.UInt16, rowIdx).ToString());
            table.AddValue("NumSamples", TypeFlag.UInt32, newRowIdx, importTable.GetValue<uint>("NumSamples", TypeFlag.UInt32, rowIdx).ToString());
            //table.AddValue("EncodeType", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("EncodeType", TypeFlag.UInt8, rowIdx).ToString());

            //ExtensionData
            if(ParseVersion >= _1_22_4_0)
            {
                if(importAcb.ParseVersion >= _1_22_4_0)
                {
                    //ExtensionData is a RowIdx (UInt16)
                    ushort extensionDataIndex = importTable.GetValue<ushort>("ExtensionData", TypeFlag.UInt16, rowIdx);

                    if(extensionDataIndex != ushort.MaxValue)
                    {
                        extensionDataIndex = (ushort)AddWaveformExtensionData(importAcb, extensionDataIndex);
                    }
                    table.AddValue("ExtensionData", TypeFlag.UInt16, newRowIdx, extensionDataIndex.ToString());
                }
                else
                {
                    //importAcb is an old ver so we cant import anything from it here. Set it to ushort.MaxValue
                    table.AddValue("ExtensionData", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
                }
            }
            else
            {
                //ExtensionData is Data. This version is unknown... but copy it over anyway.
                if(importAcb.ParseVersion < _1_22_4_0)
                {
                    table.AddData("ExtensionData", newRowIdx, importTable.GetData("ExtensionData", rowIdx));
                }
                else
                {
                    //Importing from a newer ACB that uses WaveformExtensionDataTable... so set it to null. 
                    table.AddData("ExtensionData", newRowIdx, null);
                }
            }

            //Add track
            ushort awbId = ushort.MaxValue;
            bool streaming = Convert.ToBoolean(importTable.GetValue<byte>("Streaming", TypeFlag.UInt8, rowIdx));

            if (importAcb.ParseVersion >= _1_27_2_0)
            {
                if (streaming)
                {
                    awbId = AddAfs2Track(importAcb.StreamAwb, importTable.GetValue<ushort>("StreamAwbId", TypeFlag.UInt16, rowIdx));
                }
                else
                {
                    awbId = AddAfs2Track(importAcb.MemoryAwb, importTable.GetValue<ushort>("MemoryAwbId", TypeFlag.UInt16, rowIdx));
                }
            }
            else
            {
                if (streaming)
                {
                    awbId = AddAfs2Track(importAcb.StreamAwb, importTable.GetValue<ushort>("Id", TypeFlag.UInt16, rowIdx));
                }
                else
                {
                    awbId = AddAfs2Track(importAcb.MemoryAwb, importTable.GetValue<ushort>("Id", TypeFlag.UInt16, rowIdx));
                }
            }

            //AWB ID
            if (ParseVersion >= _1_27_2_0)
            {
                //MemoryAwbId, StreamAwbId and StreamAwbPortNo
                table.AddValue("StreamAwbPortNo", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString()); //Not needed for tracks in internal AWB

                table.AddValue("StreamAwbId", TypeFlag.UInt16, newRowIdx, ushort.MaxValue.ToString());
                table.AddValue("MemoryAwbId", TypeFlag.UInt16, newRowIdx, awbId.ToString());
                
            }
            else
            {
                //No MemoryAwbId, StreamAwbId and StreamAwbPortNo... just an Id column.
                table.AddValue("Id", TypeFlag.UInt16, newRowIdx, awbId.ToString());
            }

            return newRowIdx;
        }
        
        private int AddWaveformExtensionData(ACB_File importAcb, int rowIdx)
        {
            if (ParseVersion < _1_22_4_0) return -1; //No waveform extension table on this ver

            var table = UtfFile.GetColumnTable("WaveformExtensionDataTable");
            var importTable = importAcb.UtfFile.GetColumnTable("WaveformExtensionDataTable");

            //Create table if it doesn't exist
            if(table == null)
            {
                table = GetDefaultWaveformExtensionTable();
                UtfFile.SetTable("WaveformExtensionDataTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add row
            table.AddValue("LoopStart", TypeFlag.UInt32, newRowIdx, importTable.GetValue<uint>("LoopStart", TypeFlag.UInt32, rowIdx).ToString());
            table.AddValue("LoopEnd", TypeFlag.UInt32, newRowIdx, importTable.GetValue<uint>("LoopEnd", TypeFlag.UInt32, rowIdx).ToString());

            return newRowIdx;
        }

        private ushort AddAfs2Track(AFS2_File importAwb, ushort awbId)
        {
            if(importAwb == null)
            {
                throw new FileNotFoundException("Awb file was not loaded.");
            }

            var audioFileToAdd = importAwb.GetEntry(awbId);
            return MemoryAwb.AddEntry(audioFileToAdd, true);
        }

        private int AddAisac(ACB_File importAcb, int rowIdx)
        {
            //Currently not copying control names. Don't think it's needed?

            //Load table
            var table = UtfFile.GetColumnTable("AisacTable");
            var importTable = importAcb.UtfFile.GetColumnTable("AisacTable", true);

            //Create table if needed
            if (table == null)
            {
                table = GetDefaultAisacTable();
                UtfFile.SetTable("AisacTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add data
            table.AddValue("Id", TypeFlag.Int16, newRowIdx, importTable.GetValue<short>("Id", TypeFlag.Int16, rowIdx).ToString());
            table.AddValue("Type", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("Type", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("ControlId", TypeFlag.UInt16, newRowIdx, importTable.GetValue<ushort>("ControlId", TypeFlag.UInt16, rowIdx).ToString());
            table.AddValue("RandomRange", TypeFlag.Single, newRowIdx, importTable.GetValue<float>("RandomRange", TypeFlag.Single, rowIdx).ToString());
            table.AddValue("DefaultControlFlag", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("DefaultControlFlag", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("DefaultControl", TypeFlag.Single, newRowIdx, importTable.GetValue<float>("DefaultControl", TypeFlag.Single, rowIdx).ToString());
            table.AddValue("GraphBitFlag", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("GraphBitFlag", TypeFlag.UInt8, rowIdx).ToString());

            //AutoModulation
            ushort autoModulationIndex = importTable.GetValue<ushort>("AutoModulationIndex", TypeFlag.UInt16, rowIdx);

            if(autoModulationIndex != ushort.MaxValue)
            {
                autoModulationIndex = (ushort)AddAutoModulation(importAcb, autoModulationIndex);
            }

            table.AddValue("AutoModulationIndex", TypeFlag.UInt16, newRowIdx, autoModulationIndex.ToString());

            //Graph
            ushort[] graphIndexes = BigEndianConverter.ToUInt16Array(importTable.GetData("GraphIndexes", rowIdx));

            for(int i =0; i < graphIndexes.Length; i++)
            {
                if(graphIndexes[i] != ushort.MaxValue)
                {
                    graphIndexes[i] = (ushort)AddGraph(importAcb, graphIndexes[i]);
                }
            }

            table.AddData("GraphIndexes", newRowIdx, BigEndianConverter.GetBytes(graphIndexes));

            return newRowIdx;
        }

        private int AddAutoModulation(ACB_File importAcb, int rowIdx)
        {
            //Load table
            var table = UtfFile.GetColumnTable("AutoModulationTable");
            var importTable = importAcb.UtfFile.GetColumnTable("AutoModulationTable", true);

            //Create table if needed
            if (table == null)
            {
                table = GetDefaultAutoModulationTable();
                UtfFile.SetTable("AutoModulationTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add data
            table.AddValue("Type", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("Type", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("TriggerType", TypeFlag.UInt8, newRowIdx, importTable.GetValue<byte>("TriggerType", TypeFlag.UInt8, rowIdx).ToString());
            table.AddValue("Time", TypeFlag.UInt32, newRowIdx, importTable.GetValue<int>("Time", TypeFlag.UInt32, rowIdx).ToString());
            table.AddValue("Key", TypeFlag.UInt32, newRowIdx, importTable.GetValue<int>("Key", TypeFlag.UInt32, rowIdx).ToString());

            return newRowIdx;
        }

        private int AddGraph(ACB_File importAcb, int rowIdx)
        {
            //Load table
            var table = UtfFile.GetColumnTable("GraphTable");
            var importTable = importAcb.UtfFile.GetColumnTable("GraphTable", true);

            //Create table if needed
            if (table == null)
            {
                table = GetDefaultGraphTable();
                UtfFile.SetTable("GraphTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add data
            table.AddValue("Type", TypeFlag.UInt16, newRowIdx, importTable.GetValue<ushort>("Type", TypeFlag.UInt16, rowIdx).ToString());
            table.AddData("Controls", newRowIdx, importTable.GetData("Controls", rowIdx));
            table.AddData("Destinations", newRowIdx, importTable.GetData("Destinations", rowIdx));
            table.AddData("Curve", newRowIdx, importTable.GetData("Curve", rowIdx));
            table.AddValue("ControlWorkArea", TypeFlag.Single, newRowIdx, importTable.GetValue<float>("ControlWorkArea", TypeFlag.Single, rowIdx).ToString());
            table.AddValue("DestinationWorkArea", TypeFlag.Single, newRowIdx, importTable.GetValue<float>("DestinationWorkArea", TypeFlag.Single, rowIdx).ToString());

            return newRowIdx;
        }

        private int AddGlobalAisacReferences(ACB_File importAcb, int rowIdx, int count)
        {
            //Load table
            var table = UtfFile.GetColumnTable("GlobalAisacReferenceTable");
            var importTable = importAcb.UtfFile.GetColumnTable("GlobalAisacReferenceTable", true);

            //Create table if needed
            if (table == null)
            {
                table = GetDefaultGlobalAisacReferenceTable();
                UtfFile.SetTable("GlobalAisacReferenceTable", table);
            }

            int newRowIdx = table.RowCount();

            //Add new rows
            for(int i = 0; i < count; i++)
            {
                //Add data
                table.AddValue("Name", TypeFlag.String, newRowIdx + i, importTable.GetValue<string>("Name", TypeFlag.String, rowIdx + i).ToString());
            }

            return newRowIdx;
        }



        #endregion

        #region Parse
        public List<ACB_Command> ParseCommands(byte[] commandBytes)
        {
            List<ACB_Command> commands = new List<ACB_Command>();

            //Parse command bytes. If there are 3 or more bytes left, then there is another command to parse.
            int offset = 0;

            while (((commandBytes.Length) - offset) >= 3)
            {
                ACB_Command command = new ACB_Command();
                command.I_00 = commandBytes[offset + 0];
                command.CommandType = (CommandType)commandBytes[offset + 1];
                command.Parameters = commandBytes.GetRange(offset + 3, commandBytes[offset + 2]).ToList();
                commands.Add(command);

                offset += 3 + command.NumParameters;
            }

            return commands;
        }

        #endregion

        #region DefaultTables
        //Generate default UTF tables for optional columns that may not always exist.

        private static UTF_File GetDefaultWaveformExtensionTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "WaveformExtensionDataTable",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("LoopStart", TypeFlag.UInt32),
                    new UTF_Column("LoopEnd", TypeFlag.UInt32)
                }
            };
        }

        private static UTF_File GetDefaultTrackTable(string name)
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = name,
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("EventIndex", TypeFlag.UInt16),
                    new UTF_Column("CommandIndex", TypeFlag.UInt16),
                    new UTF_Column("LocalAisacs", TypeFlag.Data),
                    new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16),
                    new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16),
                    new UTF_Column("ParameterPallet", TypeFlag.UInt16),
                    new UTF_Column("TargetType", TypeFlag.UInt8),
                    new UTF_Column("TargetName", TypeFlag.String),
                    new UTF_Column("TargetId", TypeFlag.UInt32),
                    new UTF_Column("TargetAcbName", TypeFlag.String),
                    new UTF_Column("Scope", TypeFlag.UInt8),
                    new UTF_Column("TargetTrackNo", TypeFlag.UInt16),
                }
            };
        }

        private static UTF_File GetDefaultAisacTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "Aisac",
                Columns = new List<UTF_Column>()
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
                }
            };
        }

        private static UTF_File GetDefaultGlobalAisacReferenceTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "GlobalAisacReference",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Name", TypeFlag.String)
                }
            };
        }

        private static UTF_File GetDefaultGraphTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "Graph",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Type", TypeFlag.UInt16),
                    new UTF_Column("Controls", TypeFlag.Data),
                    new UTF_Column("Destinations", TypeFlag.Data),
                    new UTF_Column("Curve", TypeFlag.Data),
                    new UTF_Column("ControlWorkArea", TypeFlag.Single),
                    new UTF_Column("DestinationWorkArea", TypeFlag.Single)
                }
            };
        }

        private static UTF_File GetDefaultAutoModulationTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "AutoModulation",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Type", TypeFlag.UInt8),
                    new UTF_Column("TriggerType", TypeFlag.UInt8),
                    new UTF_Column("Time", TypeFlag.UInt32),
                    new UTF_Column("Key", TypeFlag.UInt32)
                }
            };
        }

        private static UTF_File GetDefaultSequenceTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "Sequence",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("PlaybackRatio", TypeFlag.UInt16),
                    new UTF_Column("NumTracks", TypeFlag.UInt16),
                    new UTF_Column("TrackIndex", TypeFlag.Data),
                    new UTF_Column("CommandIndex", TypeFlag.UInt16),
                    new UTF_Column("LocalAisacs", TypeFlag.Data),
                    new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16),
                    new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16),
                    new UTF_Column("ParameterPallet", TypeFlag.UInt16),
                    new UTF_Column("ActionTrackStartIndex", TypeFlag.UInt16),
                    new UTF_Column("NumActionTracks", TypeFlag.UInt16),
                    new UTF_Column("TrackValues", TypeFlag.Data),
                    new UTF_Column("Type", TypeFlag.UInt8),
                    new UTF_Column("ControlWorkArea1", TypeFlag.UInt16),
                    new UTF_Column("ControlWorkArea2", TypeFlag.UInt16)
                }
            };
        }

        private static UTF_File GetDefaultSynthTable()
        {
            return new UTF_File()
            {
                EncodingType = _EncodingType.UTF8,
                TableName = "Synth",
                Columns = new List<UTF_Column>()
                {
                    new UTF_Column("Type", TypeFlag.UInt8),
                    new UTF_Column("VoiceLimitGroupName", TypeFlag.String),
                    new UTF_Column("CommandIndex", TypeFlag.UInt16),
                    new UTF_Column("ReferenceItems", TypeFlag.Data),
                    new UTF_Column("LocalAisacs", TypeFlag.Data),
                    new UTF_Column("GlobalAisacStartIndex", TypeFlag.UInt16),
                    new UTF_Column("GlobalAisacNumRefs", TypeFlag.UInt16),
                    new UTF_Column("ControlWorkArea1", TypeFlag.UInt16),
                    new UTF_Column("ControlWorkArea2", TypeFlag.UInt16),
                    new UTF_Column("TrackValues", TypeFlag.Data),
                    new UTF_Column("ParameterPallet", TypeFlag.UInt16),
                    new UTF_Column("ActionTrackStartIndex", TypeFlag.UInt16),
                    new UTF_Column("NumActionTracks", TypeFlag.UInt16)
                }
            };
        }

        #endregion

        #region HelperPublic
        public string GetCueName(int cueID)
        {
            var cueTable = UtfFile.GetColumnTable("CueTable");

            for(int i = 0; i < cueTable.RowCount(); i++)
            {
                uint _cueId = cueTable.GetValue<uint>("CueId", TypeFlag.UInt32, i);

                if (_cueId == (uint)cueID) return GetCueNameFromCueIndex(i);
            }

            return string.Empty;
        }

        public int GetCueId(string cueName)
        {
            var rowIdx = UtfFile.IndexOfRow("CueNameTable", "CueName", cueName);

            if (rowIdx == -1) return -1; //Cue with this name doesn't exist in this ACB file.

            int cueIdx = UtfFile.GetValue<ushort>("CueNameTable", "CueIndex", TypeFlag.UInt16, rowIdx);

            return (int)UtfFile.GetValue<uint>("CueTable", "CueId", TypeFlag.UInt32, cueIdx);
        }
        
        #endregion


        #region HelperPrivate
        private string GetCueNameFromCueIndex(int cueIndex)
        {
            if (UtfFile.GetColumnTable("CueNameTable") == null) return string.Empty;

            int value = UtfFile.IndexOfRow("CueNameTable", "CueIndex", cueIndex.ToString());

            if (value != -1)
            {
                return UtfFile.GetValue<string>("CueNameTable", "CueName", TypeFlag.String, value);
            }
            else
            {
                //Cue has no name
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the next available cue ID. Last cue + 1. Does not check gaps.
        /// </summary>
        /// <returns></returns>
        private int GetNextCueID()
        {
            var table = UtfFile.GetColumnTable("CueTable");
            int lastIdx = table.RowCount() - 1;
            return (int)(table.GetValue<uint>("CueId", TypeFlag.UInt32, lastIdx) + 1);
        }
        #endregion
    }
    
    public class ACB_Command
    {
        public byte I_00 { get; set; }
        public CommandType CommandType { get; set; }
        public byte NumParameters { get { return (byte)((Parameters != null) ? Parameters.Count : 0); } }

        //Data, depending on CommandType
        public List<byte> Parameters { get; set; } = new List<byte>(); 

        #region ParameterProperties
        public ushort Param1 { get { return GetParam(0); } set { SetParam(0, value); } }
        public ushort Param2 { get { return GetParam(1); } set { SetParam(1, value); } }
        public ushort Param3 { get { return GetParam(2); } set { SetParam(2, value); } }
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

        public static byte[] GetBytes(List<ACB_Command> commands)
        {
            List<byte> bytes = new List<byte>();

            foreach(var command in commands)
            {
                bytes.Add(command.I_00);
                bytes.Add((byte)command.CommandType);
                bytes.Add(command.NumParameters);

                if(command.Parameters != null)
                {
                    bytes.AddRange(command.Parameters);
                }
            }

            return bytes.ToArray();
        }
        
    }

    public class ACB_Cue
    {
        public int CueId { get; set; }
        public string Name { get; set; }
    }

    //Enums
    public enum ReferenceType : byte
    {
        Waveform = 1,
        Synth = 2,
        Sequence = 3,
        BlockSequence = 8
    }

    public enum EncodeType : byte
    {
        HCA = 2
    }

    public enum CommandType
    {
        Null = 0,
        //Unk79 = 79, //CAN CAUSE CRASHES (Must reference something...)
        Action_Play = 188,
        Action_Stop = 189,
        ReferenceItem = 208
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
        SequenceCommand, //In older ACB versions this is the only command table and was used for everything
        SynthCommand,
        TrackCommand,
        TrackEvent
    }
}
