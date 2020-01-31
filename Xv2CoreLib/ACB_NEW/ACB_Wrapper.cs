using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.HCA;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.ACB_NEW
{
    //Planned Features:
    //-Faciliates UI interaction 
    //-Adding tracks to cues
    //-Adding new cues
    //-Copying cues
    //-All interactions are undoable (via UndoManager)

    public class ACB_Wrapper : IIsNull
    {
        public ACB_File AcbFile { get; set; }
        public ObservableCollection<Cue_Wrapper> Cues { get; set; } = new ObservableCollection<Cue_Wrapper>();
        

        public ACB_Wrapper(ACB_File acbFile)
        {
            AcbFile = acbFile;

            foreach (var cue in AcbFile.Cues)
            {
                Cues.Add(new Cue_Wrapper(cue, this));
            }
        }
        
        //These functions copy tables from another acb to this one. They are recursive - meaning they copy sub-tables as well.
        #region CopyTableFunctions
        public int CopyReferenceItemsTable(ACB_Wrapper copyAcb, ReferenceType refType, ushort refIndex)
        {
            return 0;
        }

        #endregion

        #region RefactorFunctions
        public void RefactorCueId(uint oldCueId, uint newCueId, bool addUndo)
        {
            foreach(var cue in AcbFile.Cues.Where(x => x.ID == oldCueId))
            {
                cue.ID = newCueId;
                if(addUndo)
                    UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_Cue>("ID", cue, oldCueId, newCueId));
            }

            foreach(var action in AcbFile.ActionTracks.Where(x => x.TargetSelf && x.TargetType == 1 && x.TargetId == oldCueId))
            {
                action.TargetId = newCueId;
                if(addUndo)
                    UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_Track>("TargetId", action, oldCueId, newCueId));
            }
        }

        public void RefactorSequenceIndex(int oldIndex, int newIndex, bool addUndo)
        {
            RefactoReferenceItemIndex(ReferenceType.Sequence, oldIndex, newIndex, addUndo);
        }

        public void RefactorSynthIndex(int oldIndex, int newIndex, bool addUndo)
        {
            RefactoReferenceItemIndex(ReferenceType.Synth, oldIndex, newIndex, addUndo);
        }

        public void RefactorWaveformIndex(int oldIndex, int newIndex, bool addUndo)
        {
            RefactoReferenceItemIndex(ReferenceType.Waveform, oldIndex, newIndex, addUndo);
        }

        public void RefactorTrackIndex(int oldIndex, int newIndex, bool addUndo)
        {
            foreach(var table in AcbFile.Sequences)
            {
                foreach(var track in table.Tracks.Where(x => x.Index == (ushort)oldIndex))
                {
                    track.Index = (ushort)newIndex;
                    if(addUndo)
                        UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_SequenceTrack>("Index", track, (ushort)oldIndex, (ushort)newIndex));
                }
            }
        }

        public void RefactorActionTrackIndex(int oldIndex, int newIndex, bool addUndo)
        {
            foreach (var table in AcbFile.Sequences)
            {
                for(int i = 0; i < table.ActionTracks.Count; i++)
                {
                    if(table.ActionTracks[i] == (ushort)oldIndex)
                    {
                        table.ActionTracks[i] = (ushort)newIndex;
                        if (addUndo)
                            UndoManager.Instance.AddToUndoComposition(new UndoableStateChange<ushort>(table.ActionTracks, i, (ushort)oldIndex, (ushort)newIndex, null));

                    }
                }
            }

            foreach (var table in AcbFile.Synths)
            {
                for (int i = 0; i < table.ActionTracks.Count; i++)
                {
                    if (table.ActionTracks[i] == (ushort)oldIndex)
                    {
                        table.ActionTracks[i] = (ushort)newIndex;
                        if (addUndo)
                            UndoManager.Instance.AddToUndoComposition(new UndoableStateChange<ushort>(table.ActionTracks, i, (ushort)oldIndex, (ushort)newIndex, null));

                    }
                }
            }
        }

        private void RefactoReferenceItemIndex(ReferenceType type, int oldIndex, int newIndex, bool addUndo)
        {
            foreach (var table in AcbFile.Cues.Where(x => x.ReferenceType == type && x.ReferenceIndex == (ushort)oldIndex))
            {
                table.ReferenceIndex = (ushort)newIndex;
                if (addUndo)
                    UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_Cue>("ReferenceIndex", table, (ushort)oldIndex, (ushort)newIndex));
            }

            foreach (var table in AcbFile.Synths.Where(x => x.ReferenceType == type && x.ReferenceIndex == (ushort)oldIndex))
            {
                table.ReferenceIndex = (ushort)newIndex;
                if (addUndo)
                    UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_Synth>("ReferenceIndex", table, (ushort)oldIndex, (ushort)newIndex));
            }

            RefactorCommandTableReferenceItems(type, oldIndex, newIndex, addUndo);
        }

        private void RefactorCommandTableReferenceItems(ReferenceType type, int oldId, int newId, bool addUndo)
        {
            foreach (var command in AcbFile.CommandTables.CommandTables)
            {
                foreach (var cmdGroup in command.CommandGroups)
                {
                    foreach (var seqCmd in cmdGroup.Commands.Where(x => x.CommandType == CommandType.ReferenceItem))
                    {
                        if (seqCmd.Param1 == (ushort)type && seqCmd.Param2 == (ushort)oldId)
                        {
                            seqCmd.Param2 = (ushort)newId;
                            if (addUndo)
                                UndoManager.Instance.AddToUndoComposition(new UndoableProperty<ACB_Command>("Param2", seqCmd, (ushort)oldId, (ushort)newId));

                        }
                    }
                }
            }
        }
        #endregion

        #region AddFunctions
        public int AddCue(string name, ReferenceType type)
        {
            ACB_Cue acbCue = AcbFile.AddCue(name, type);
            Cue_Wrapper wrapper = new Cue_Wrapper(acbCue, this);
            Cues.Add(wrapper);
            return (int)acbCue.ID;
        }

        /// <summary>
        /// Add a AWB entry.
        /// </summary>
        /// <param name="afs2Entry">The afs2Entry to add. (ID will be automatically generated and returned)</param>
        /// <param name="useExisting">If an identical afs2Entry exists, then reuse that.</param>
        /// <returns>The Awb ID assigned to the entry.</returns>
        public ushort AddAwbEntry(AFS2_AudioFile afs2Entry, bool useExisting = true)
        {
            return AcbFile.AudioTracks.AddEntry(afs2Entry, useExisting);
        }

        #endregion

        #region HelperFunctions
        public ACB_Waveform GetWaveformFromCommand(ACB_CommandGroup commandGroup)
        {
            ACB_Waveform waveformRef = null;

            if (commandGroup != null)
            {
                foreach (var command in commandGroup.Commands)
                {
                    if (command.CommandType == CommandType.ReferenceItem || command.CommandType == CommandType.Unk209)
                    {
                        waveformRef = GetWaveformFromRefItem((ReferenceType)command.Param1, command.Param2);
                    }
                }
            }

            return waveformRef;
        }

        public ACB_Waveform GetWaveformFromRefItem(ReferenceType referenceType, int referenceIndex)
        {
            switch (referenceType)
            {
                case ReferenceType.Waveform:
                    return GetWaveform(referenceIndex);
                case ReferenceType.Synth:
                    var synth = GetSynth(referenceIndex);
                    if ((ReferenceType)synth.ReferenceType == ReferenceType.Waveform)
                        return GetWaveform(synth.ReferenceIndex);
                    break;
            }

            return null;
        }

        public List<Track_Wrapper> GetAllTracks(ReferenceType referenceType, int referenceIndex, List<Track_Wrapper> tracks, ACB_Track trackRef = null)
        {
            if (referenceIndex == ushort.MaxValue) return tracks;

            if(referenceType == ReferenceType.Waveform)
            {
                tracks.Add(new Track_Wrapper(trackRef, GetWaveform(referenceIndex), this, TrackType.Track));
            }
            else if (referenceType == ReferenceType.Synth)
            {
                var synth = GetSynth(referenceIndex);
                tracks = GetAllTracks(synth.ReferenceType, synth.ReferenceIndex, tracks, trackRef);
            }
            else if(referenceType == ReferenceType.Sequence)
            {
                var sequence = GetSequence(referenceIndex);

                foreach(var track in sequence.Tracks)
                {
                    trackRef = GetTrack(track.Index);
                    if (trackRef == null) continue;

                    if(trackRef.EventIndex != ushort.MaxValue)
                    {
                        var commandGroup = GetCommand(trackRef.EventIndex, CommandTableType.TrackEvent);

                        if (commandGroup != null)
                        {
                            foreach (var command in commandGroup.Commands)
                            {
                                if (command.CommandType == CommandType.ReferenceItem)
                                {
                                    tracks = GetAllTracks((ReferenceType)command.Param1, command.Param2, tracks, trackRef);
                                }
                            }
                        }
                    }
                }
            }

            return tracks;
        }
        #endregion

        #region GetFunctions
        public ACB_Sequence GetSequence(int index)
        {
            if (index >= AcbFile.Sequences.Count) throw new Exception(string.Format("ACB_Wrapper.GetSequence: A sequence does not exist at index {0}.", index));
            return AcbFile.Sequences[index];
        }

        public ACB_Cue GetCue(int index)
        {
            if (index >= AcbFile.Cues.Count) throw new Exception(string.Format("ACB_Wrapper.GetCue: A cue does not exist at index {0}.", index));
            return AcbFile.Cues[index];
        }

        public ACB_Waveform GetWaveform(int index)
        {
            if (index >= AcbFile.Waveforms.Count) throw new Exception(string.Format("ACB_Wrapper.GetWaveform: A waveform does not exist at index {0}.", index));
            return AcbFile.Waveforms[index];
        }

        public ACB_Synth GetSynth(int index)
        {
            if (index >= AcbFile.Synths.Count) throw new Exception(string.Format("ACB_Wrapper.GetSynth: A synth does not exist at index {0}.", index));
            return AcbFile.Synths[index];
        }

        public ACB_Track GetTrack(int index)
        {
            if (index >= AcbFile.Tracks.Count) throw new Exception(string.Format("ACB_Wrapper.GetTrack: A track does not exist at index {0}.", index));
            return AcbFile.Tracks[index];
        }

        public ACB_Track GetAction(int index)
        {
            if (index >= AcbFile.ActionTracks.Count) throw new Exception(string.Format("ACB_Wrapper.GetAction: A action does not exist at index {0}.", index));
            return AcbFile.ActionTracks[index];
        }

        public ACB_CommandGroup GetCommand(int index, CommandTableType type)
        {
            return AcbFile.CommandTables.GetCommand(index, type);
        }

        #endregion

        public static ACB_Wrapper NewXv2Acb()
        {
            return new ACB_Wrapper(ACB_File.NewXv2Acb());
        }

        public bool IsNull()
        {
            return (Cues.Count == 0);
        }
    }

    public class Cue_Wrapper : INotifyPropertyChanged
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

        public ACB_Wrapper WrapperRoot;
        public ACB_Cue CueRef { get; set; }
        public ObservableCollection<Track_Wrapper> Tracks { get; set; } = new ObservableCollection<Track_Wrapper>();

        //Wrapped Values
        public SequenceType SeqType
        {
            get
            {
                if (CueRef.ReferenceType != ReferenceType.Sequence) return (SequenceType)0;
                return WrapperRoot.GetSequence(CueRef.ReferenceIndex).Type;
            }
            set
            {
                if(CueRef.ReferenceType == ReferenceType.Sequence)
                    WrapperRoot.GetSequence(CueRef.ReferenceIndex).Type = value;
            }
        }
        public ObservableCollection<ACB_SequenceTrack> SeqTrack
        {
            get
            {
                if (CueRef.ReferenceType == ReferenceType.Sequence)
                    return WrapperRoot.GetSequence(CueRef.ReferenceIndex).Tracks;
                else
                    return null;
            }
            //set
            //{
            //    WrapperRoot.GetSequence(CueRef.ReferenceIndex).Tracks = value;
            //}
        }


        //Props
        public int NumTracks { get { return Tracks.Where(t => t.Type == TrackType.Track).Count(); } }
        public int NumActionTracks { get { return Tracks.Where(t => t.Type == TrackType.ActionTrack).Count(); } }

        #region ViewBinding
        [NonSerialized]
        private ObservableCollection<Track_Wrapper> _selectedTracks = new ObservableCollection<Track_Wrapper>();
        public ObservableCollection<Track_Wrapper> SelectedTracks
        {
            get
            {
                return this._selectedTracks;
            }
        }
        
        [NonSerialized]
        private Track_Wrapper _selectedTrack = null;
        public Track_Wrapper SelectedTrack
        {
            get
            {
                return this._selectedTrack;
            }
            set
            {
                if (value != this._selectedTrack)
                {
                    this._selectedTrack = value;
                    UpdateProperties();
                }
            }
        }
        
        private void UpdateProperties()
        {
            NotifyPropertyChanged("SeqType");
            NotifyPropertyChanged("SeqTrack");
            NotifyPropertyChanged("SelectedTrack");
            NotifyPropertyChanged("NoTracksSelectedVisibility");
            NotifyPropertyChanged("TrackSelectedVisibility");
            NotifyPropertyChanged("ActionTrackSelectedVisibility");
            NotifyPropertyChanged("SequenceCueNotVisibile");
            NotifyPropertyChanged("SequenceCueVisibile");
        }
        #endregion

        #region Init
        public Cue_Wrapper(ACB_Cue cue, ACB_Wrapper root)
        {
            CueRef = cue;
            WrapperRoot = root;

            //Tracks
            List<Track_Wrapper> allTracks = WrapperRoot.GetAllTracks(CueRef.ReferenceType, CueRef.ReferenceIndex, new List<Track_Wrapper>());

            foreach (var _track in allTracks)
                Tracks.Add(_track);

            //ActionTracks
            switch (CueRef.ReferenceType)
            {
                case ReferenceType.Sequence:
                    InitSequence();
                    break;
                case ReferenceType.Synth:
                    InitSynth();
                    break;
            }

            Tracks.CollectionChanged += new NotifyCollectionChangedEventHandler(TrackAddedEvent);
        }

        private void InitSynth()
        {
            foreach (var trackIndex in WrapperRoot.AcbFile.Sequences[CueRef.ReferenceIndex].ActionTracks)
            {
                ACB_Track track = WrapperRoot.AcbFile.ActionTracks[trackIndex];

                Tracks.Add(new Track_Wrapper(track, null, WrapperRoot, TrackType.ActionTrack, ActionTrackType.Synth));
            }
        }

        private void InitSequence()
        {
            //Tracks
            /*
            foreach(var trackIndex in WrapperRoot.AcbFile.Sequences[CueRef.ReferenceIndex].Tracks)
            {
                ACB_Track track = WrapperRoot.AcbFile.Tracks[trackIndex.Index];
                ACB_Waveform waveformRef = null;

                if(track.EventIndex != ushort.MaxValue)
                {
                    var commandGroup = WrapperRoot.AcbFile.CommandTables.GetCommand(track.EventIndex, CommandTableType.SequenceCommand);
                    waveformRef = WrapperRoot.GetWaveformFromCommand(commandGroup);

                }

                Tracks.Add(new Track_Wrapper(track, waveformRef, WrapperRoot, TrackType.Track));
            }
            */

            //ActionTracks
            foreach (var trackIndex in WrapperRoot.AcbFile.Sequences[CueRef.ReferenceIndex].ActionTracks)
            {
                ACB_Track track = WrapperRoot.AcbFile.ActionTracks[trackIndex];

                Tracks.Add(new Track_Wrapper(track, null, WrapperRoot, TrackType.ActionTrack, ActionTrackType.Sequence));
            }
        }

        #endregion

        #region Event
        private void TrackAddedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("NumTracks");
            NotifyPropertyChanged("NumActionTracks");
        }
        #endregion
    }

    public class Track_Wrapper : INotifyPropertyChanged
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

        public ACB_Wrapper WrapperRoot;
        public TrackType Type;
        public ActionTrackType ActionTrackType;
        public ACB_Track TrackRef { get; set; }
        public Waveform_Wrapper WaveformWrapper { get; set; }

        //Wrapped values
        public ACB_CommandGroup Command //For Action. Command uses TrackEvent table, confusingly...
        {
            get
            {
                if (TrackRef == null) return null;
                if (TrackRef.CommandIndex == ushort.MaxValue) return null;
                return WrapperRoot.GetCommand(TrackRef.CommandIndex, CommandTableType.TrackEvent);
            }
        }
        

        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case TrackType.ActionTrack:
                        return string.Format("[ACTION]");
                    case TrackType.Track:
                        return string.Format("[TRACK {1}] Duration: {0}", WaveformWrapper?.HcaMeta.Duration, TrackRef?.Index);
                    default:
                        return "[Unknown Track Type]";
                }
            }
        }
        public Visibility IsTrack { get { return (Type == TrackType.Track) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsAction { get { return (Type == TrackType.ActionTrack) ? Visibility.Visible : Visibility.Collapsed; } }

        public Track_Wrapper(ACB_Track acbTrack, ACB_Waveform waveform, ACB_Wrapper root, TrackType type, ActionTrackType actionType = ActionTrackType.Sequence)
        {
            TrackRef = acbTrack;
            WaveformWrapper = new Waveform_Wrapper(waveform, root);
            WrapperRoot = root;
            Type = type;
            ActionTrackType = actionType;
        }
    }

    public class Waveform_Wrapper : INotifyPropertyChanged
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

        public ACB_Wrapper WrapperRoot;
        public ACB_Waveform WaveformRef { get; set; }

        //Wrapper values
        public AFS2_AudioFile AwbEntry
        {
            get
            {
                if (WaveformRef == null) return null;
                return WrapperRoot.AcbFile.GetAfs2Entry(WaveformRef.AwbId);
            }
            set
            {
                if (WaveformRef != null)
                {
                    WaveformRef.AwbId = WrapperRoot.AddAwbEntry(value, true);
                    NotifyPropertyChanged("HcaMeta");
                    NotifyPropertyChanged("WaveformRef");
                }
            }
        }
        public HcaMetadata HcaMeta
        {
            get
            {
                if(AwbEntry == null) return new HcaMetadata();
                return AwbEntry.HcaInfo;
            }
        }

        public Waveform_Wrapper(ACB_Waveform waveform, ACB_Wrapper root)
        {
            WaveformRef = waveform;
            WrapperRoot = root;
        }
    }

    public enum TrackType
    {
        Track,
        ActionTrack
    }

    public enum ActionTrackType
    {
        Synth, 
        Sequence
    }
}
