using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource;

#if NvvmLight
using GalaSoft.MvvmLight.CommandWpf;
#endif

namespace Xv2CoreLib.ACB
{
    //Planned Features:
    //-Faciliates UI interaction 
    //-Adding tracks to cues
    //-Adding new cues
    //-Copying cues
    //-All interactions are undoable (via UndoManager)
    

    public class ACB_Wrapper : IIsNull, INotifyPropertyChanged
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

        public bool AudioPackage_IsNewOptions
        {
            get
            {
                return !IsAwbWrapper ? AcbFile.AudioPackageType == AudioPackageType.BGM_NewOption : false;
            }
            set
            {
                if (!IsAwbWrapper)
                {
                    var oldValue = AcbFile.AudioPackageType;
                    AcbFile.AudioPackageType = (value) ? AudioPackageType.BGM_NewOption : AudioPackageType.BGM_Direct;
                    NotifyPropertyChanged(nameof(AudioPackage_IsNewOptions));
                    NotifyPropertyChanged(nameof(AudioPackage_IsDirect));
                    NotifyPropertyChanged(nameof(AudioPackage_IsAutoVoice));

                    if (oldValue != AcbFile.AudioPackageType)
                        UndoManager.Instance.AddUndo(new UndoableProperty<ACB_File>(nameof(AcbFile.AudioPackageType), AcbFile, oldValue, AcbFile.AudioPackageType, "AudioPackage Type"));
                }
            }
        }
        public bool AudioPackage_IsDirect
        {
            get
            {
                return !IsAwbWrapper ? AcbFile.AudioPackageType == AudioPackageType.BGM_Direct : false;
            }
            set
            {
                if (!IsAwbWrapper)
                {
                    var oldValue = AcbFile.AudioPackageType;
                    AcbFile.AudioPackageType = (value) ? AudioPackageType.BGM_Direct : AudioPackageType.BGM_NewOption;
                    NotifyPropertyChanged(nameof(AudioPackage_IsNewOptions));
                    NotifyPropertyChanged(nameof(AudioPackage_IsDirect));
                    NotifyPropertyChanged(nameof(AudioPackage_IsAutoVoice));

                    if (oldValue != AcbFile.AudioPackageType)
                        UndoManager.Instance.AddUndo(new UndoableProperty<ACB_File>(nameof(AcbFile.AudioPackageType), AcbFile, oldValue, AcbFile.AudioPackageType, "AudioPackage Type"));
                }
            }
        }
        public bool AudioPackage_IsAutoVoice
        {
            get
            {
                return !IsAwbWrapper ? AcbFile.AudioPackageType == AudioPackageType.AutoVoice : false;
            }
            set
            {
                if (!IsAwbWrapper)
                {
                    var oldValue = AcbFile.AudioPackageType;

                    if (value)
                    {
                        AcbFile.AudioPackageType = AudioPackageType.AutoVoice;

                        NotifyPropertyChanged(nameof(AudioPackage_IsNewOptions));
                        NotifyPropertyChanged(nameof(AudioPackage_IsDirect));
                        NotifyPropertyChanged(nameof(AudioPackage_IsAutoVoice));

                        if (oldValue != AcbFile.AudioPackageType)
                            UndoManager.Instance.AddUndo(new UndoableProperty<ACB_File>(nameof(AcbFile.AudioPackageType), AcbFile, oldValue, AcbFile.AudioPackageType, "AudioPackage Type"));
                    }
                }

            }
        }
        public bool IsAwbWrapper => AwbWrapper != null;

        public ACB_File AcbFile { get; set; }
        public AWB_Wrapper AwbWrapper { get; set; }
        public AsyncObservableCollection<Cue_Wrapper> Cues { get; set; } = new AsyncObservableCollection<Cue_Wrapper>();
        
        

        public ACB_Wrapper(ACB_File acbFile)
        {
            AcbFile = acbFile;
            Refresh();
        }
        
        /// <summary>
        /// Initialse an ACB_Wrapper from an AWB_Wrapper. This disables all normal functions of the class. 
        /// Intended for solo-loading AWBs within ACE.
        /// </summary>
        public ACB_Wrapper(AWB_Wrapper awbWraper)
        {
            AwbWrapper = awbWraper;
        }

        /// <summary>
        /// Refresh the wrapper.
        /// </summary>
        public void Refresh()
        {
            if (IsAwbWrapper) return;

            Cues.Clear();

            foreach (var cue in AcbFile.Cues)
                Cues.Add(new Cue_Wrapper(cue, this));
        }
        
        /// <summary>
        /// Force updates all bindable properties.
        /// </summary>
        public void UpdateProperties()
        {
            foreach (var cue in Cues)
                cue.UpdateProperties();
        }

        #region UndoableOperations
        public int UndoableAddCue(string name, ReferenceType type, bool is3Dsound)
        {
            List<IUndoRedo> undos = AddCue(name, type, is3Dsound, out int cueId);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Add Cue: {name}"));

            return cueId;
        }

        public int UndoableAddCue(string name, ReferenceType type, byte[] trackBytes, bool streaming, bool loop, bool is3Dsound, EncodeType encodeType)
        {
            List<IUndoRedo> undos = AddCue(name, type, trackBytes, streaming, loop, is3Dsound, encodeType, out int cueId);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Add Cue: {name}"));

            return cueId;
        }

        public void UndoableDeleteCue(Cue_Wrapper cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListRemove<ACB_Cue>(AcbFile.Cues, cue.CueRef, AcbFile.Cues.IndexOf(cue.CueRef)));
            AcbFile.Cues.Remove(cue.CueRef);
            undos.Add(new UndoableListRemove<Cue_Wrapper>(Cues, cue, Cues.IndexOf(cue)));
            Cues.Remove(cue);

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Delete Cue: {cue.CueRef.Name}"));
        }

        public void UndoableDeleteCues(IList<Cue_Wrapper> cues)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var cue in cues)
            {
                undos.Add(new UndoableListRemove<ACB_Cue>(AcbFile.Cues, cue.CueRef, AcbFile.Cues.IndexOf(cue.CueRef)));
                AcbFile.Cues.Remove(cue.CueRef);
                undos.Add(new UndoableListRemove<Cue_Wrapper>(Cues, cue, Cues.IndexOf(cue)));
                Cues.Remove(cue);
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Delete {cues.Count} Cues"));
        }
        
        public void UndoableRandomizeCueIds()
        {
            var undos = AcbFile.RandomizeCueIds();
            Refresh();
            undos.Add(new UndoActionDelegate(this, nameof(Refresh), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Randomize Cue IDs"));
        }
        #endregion

        #region Operations
        //Methods that modify the base ACB file. All methods must return an List<IUndoRedo> (the calling method is responsible for pushing that to the UndoManager)

        public List<IUndoRedo> AddCue(string name, ReferenceType type, bool is3Dsound, out int cueId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ACB_Cue acbCue;
            undos.AddRange(AcbFile.AddCue(name, type, out acbCue));

            if(is3Dsound)
                AcbFile.Add3dDefToCue(acbCue);

            Cue_Wrapper wrapper = new Cue_Wrapper(acbCue, this);
            Cues.Add(wrapper);
            undos.Add(new UndoableListAdd<Cue_Wrapper>(Cues, wrapper, ""));
            undos.Add(GetRefreshDelegate());

            cueId = (int)acbCue.ID;
            return undos;
        }

        public List<IUndoRedo> AddCue(string name, ReferenceType type, byte[] trackBytes, bool streaming, bool loop, bool is3Dsound, EncodeType encodeType, out int cueId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            ACB_Cue acbCue;
            undos.AddRange(AcbFile.AddCue(name, type, trackBytes, streaming, loop, encodeType, out acbCue));

            if (is3Dsound)
                AcbFile.Add3dDefToCue(acbCue);

            Cue_Wrapper wrapper = new Cue_Wrapper(acbCue, this);
            Cues.Add(wrapper);
            undos.Add(new UndoableListAdd<Cue_Wrapper>(Cues, wrapper, ""));
            undos.Add(GetRefreshDelegate());

            cueId = (int)acbCue.ID;
            return undos;
        }
        
        #endregion


        #region AddFunctions

        /// <summary>
        /// Add a AWB entry.
        /// </summary>
        /// <param name="afs2Entry">The afs2Entry to add. (ID will be automatically generated and returned)</param>
        /// <param name="useExisting">If an identical afs2Entry exists, then reuse that.</param>
        /// <returns>The Awb ID assigned to the entry.</returns>
        public ushort AddAwbEntry(AFS2_Entry afs2Entry, bool useExisting = true)
        {
            return AcbFile.AudioTracks.AddEntry(afs2Entry, useExisting);
        }

        #endregion

        #region Clipboard
        //Cues
        public void UndoablePasteCues()
        {
            List<IUndoRedo> undos = PasteCues();
            if(undos.Count > 0)
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Cues"));
        }

        public void CopyCues(IList<Cue_Wrapper> cues)
        {
            ACB_File newAcb = ACB_File.NewXv2Acb();

            foreach (var cue in cues)
                newAcb.CopyCue((int)cue.CueRef.ID, AcbFile, false);
            
            newAcb.SaveToClipboard();
        }

        public List<IUndoRedo> PasteCues()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (!CanPasteCues()) return undos;

            ACB_File tempAcb = ACB_File.LoadFromClipboard();
            
            //Check if cues exist in current ACB. If they do, then generate new GUIDs for them (duplication mode)
            if(AcbFile.Cues.Any(x=> tempAcb.Cues.Any(y => y.InstanceGuid == x.InstanceGuid)))
                tempAcb.NewInstanceGuids();
            
            foreach(var cue in tempAcb.Cues)
            {
                undos.AddRange(AcbFile.CopyCue((int)cue.ID, tempAcb, false));
            }

            Refresh();
            undos.Add(new UndoActionDelegate(this, "Refresh", true));
            return undos;
        }

        //Track
        public void UndoablePasteTrack(Cue_Wrapper cue)
        {
            List<IUndoRedo> undos = PasteTrack(cue);
            if (undos.Count > 0)
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Track"));
        }

        public void CopyTrack(Track_Wrapper trackWrapper)
        {
            if (trackWrapper?.WaveformWrapper == null) return;

            CopiedTrack track = new CopiedTrack(trackWrapper.WaveformWrapper.WaveformRef, trackWrapper.WaveformWrapper.AwbEntry);
            Clipboard.SetData(ACB_File.CLIPBOARD_ACB_TRACK, track);
        }

        public List<IUndoRedo> PasteTrack(Cue_Wrapper cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (!CanPasteTrack()) return undos;

            CopiedTrack track = (CopiedTrack)Clipboard.GetData(ACB_File.CLIPBOARD_ACB_TRACK);

            if(track != null && cue != null)
            {
                undos.AddRange(AcbFile.AddTrackToCue(cue.CueRef, track.TrackBytes, track.Streaming, track.Loop, track.encodeType));
            }

            cue.Refresh();
            undos.Add(new UndoActionDelegate(cue, "Refresh", true));

            return undos;
        }

        //Action
        public void UndoablePasteAction(Cue_Wrapper cue)
        {
            List<IUndoRedo> undos = PasteAction(cue);
            if (undos.Count > 0)
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Action"));
        }

        public void CopyAction(Track_Wrapper trackWrapper)
        {
            if (trackWrapper?.TrackRef == null) return;
            CopiedAction action = new CopiedAction(trackWrapper.TrackRef, trackWrapper.TrackCommand);
            Clipboard.SetData(ACB_File.CLIPBOARD_ACB_ACTION, action);
        }

        public List<IUndoRedo> PasteAction(Cue_Wrapper cue)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            if (!CanPasteAction()) return undos;

            CopiedAction action = (CopiedAction)Clipboard.GetData(ACB_File.CLIPBOARD_ACB_ACTION);

            if (action != null && cue != null)
            {
                undos.AddRange(AcbFile.AddActionToCue(cue.CueRef, action));
            }

            cue.Refresh();
            undos.Add(new UndoActionDelegate(cue, "Refresh", true));

            return undos;
        }


        //Helpers
        public bool CanPasteCues()
        {
            return Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_CUES);
        }

        public bool CanPasteTrack()
        {
            return Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_TRACK);
        }

        public bool CanPasteAction()
        {
            return Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_ACTION);
        }
        #endregion

        public static ACB_Wrapper NewXv2Acb()
        {
            return new ACB_Wrapper(ACB_File.NewXv2Acb());
        }

        public static ACB_Wrapper NewAcb(Version version)
        {
            return new ACB_Wrapper(ACB_File.NewAcb(version));
        }

        public bool IsNull()
        {
            return (Cues.Count == 0);
        }

        internal IUndoRedo GetRefreshDelegate()
        {
            return new UndoActionDelegate(this, "Refresh",  true, "");
        }

        internal IUndoRedo GetPropertyUpdateDelegate()
        {
            return new UndoActionDelegate(this, "UpdateProperties", true, "");
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
        public AsyncObservableCollection<Track_Wrapper> Tracks { get; set; } = AsyncObservableCollection<Track_Wrapper>.Create();

        //Wrapped Values
        public ACB_Sequence SequenceRef
        {
            get
            {
                if (CueRef.ReferenceType == ReferenceType.Sequence)
                    return WrapperRoot.AcbFile.GetSequence(CueRef.ReferenceIndex.TableGuid);
                else
                    return null;
            }

        }
        public ACB_Synth SynthRef
        {
            get
            {
                if (CueRef.ReferenceType == ReferenceType.Synth)
                    return WrapperRoot.AcbFile.GetSynth(CueRef.ReferenceIndex.TableGuid);
                else
                    return null;
            }

        }
        public ACB_CommandGroup SequenceCommand
        {
            get
            {
                if (SequenceRef?.CommandIndex.IsNull == null) return null;
                return WrapperRoot.AcbFile.CommandTables.GetCommand(SequenceRef.CommandIndex.TableGuid, CommandTableType.SequenceCommand);
            }
        }
        public uint UndoableCueId
        {
            get
            {
                return CueRef.ID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>()
                {
                    new UndoableProperty<ACB_Cue>("ID", CueRef, CueRef.ID, value),
                    new UndoActionDelegate(this, "UpdateProperties", true)
                }, "Cue ID"));
                CueRef.ID = value;
                NotifyPropertyChanged("UndoableCueId");
            }
        }
        public bool UndoableIs3DSound
        {
            get
            {
                return WrapperRoot.AcbFile.DoesCueHave3dDef(CueRef);
            }
            set
            {

                List<IUndoRedo> undos;
                if (value)
                    undos = WrapperRoot.AcbFile.Add3dDefToCue(CueRef);
                else
                    undos = WrapperRoot.AcbFile.Remove3dDefFromCue(CueRef);

                undos.Add(new UndoActionDelegate(this, nameof(UpdateCueProperties), true));
                UpdateCueProperties();
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Modify 3D Sound"));
            }
        }

        //AudioPackage values
        public string UndoableAlias
        {
            get
            {
                return CueRef.AliasBinding;
            }
            set
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>()
                {
                    new UndoableProperty<ACB_Cue>(nameof(ACB_Cue.AliasBinding), CueRef, CueRef.AliasBinding, value),
                    new UndoActionDelegate(this, "UpdateProperties", true)
                }, "Cue AliasBinding"));
                CueRef.AliasBinding = value;
                NotifyPropertyChanged(nameof(UndoableAlias));
            }
        }
        public VoiceLanguageEnum UndoableVoiceLanguage
        {
            get
            {
                return CueRef.VoiceLanguage;
            }
            set
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>()
                {
                    new UndoableProperty<ACB_Cue>(nameof(ACB_Cue.VoiceLanguage), CueRef, CueRef.VoiceLanguage, value),
                    new UndoActionDelegate(this, "UpdateProperties", true)
                }, "Cue VoiceLanguage"));
                CueRef.VoiceLanguage = value;
                NotifyPropertyChanged(nameof(UndoableVoiceLanguage));
            }
        }


        //Props
        public int NumTracks { get { return Tracks.Where(t => t.Type == TrackType.Track).Count(); } }
        public int NumActionTracks { get { return Tracks.Where(t => t.Type == TrackType.ActionTrack).Count(); } }

        #region ViewBinding
        [NonSerialized]
        private AsyncObservableCollection<Track_Wrapper> _selectedTracks = new AsyncObservableCollection<Track_Wrapper>();
        public AsyncObservableCollection<Track_Wrapper> SelectedTracks
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
        
        private void UpdateCueProperties()
        {
            NotifyPropertyChanged("SeqType");
            NotifyPropertyChanged("SeqTrack");
            NotifyPropertyChanged("SelectedTrack");
            NotifyPropertyChanged("NoTracksSelectedVisibility");
            NotifyPropertyChanged("TrackSelectedVisibility");
            NotifyPropertyChanged("ActionTrackSelectedVisibility");
            NotifyPropertyChanged("SequenceCueNotVisibile");
            NotifyPropertyChanged("SequenceCueVisibile");
            NotifyPropertyChanged(nameof(NumTracks));
            NotifyPropertyChanged(nameof(NumActionTracks));
            NotifyPropertyChanged(nameof(UndoableCueId));
            NotifyPropertyChanged(nameof(UndoableIs3DSound));
            NotifyPropertyChanged(nameof(UndoableAlias));
            NotifyPropertyChanged(nameof(UndoableVoiceLanguage));
        }

        public void UpdateProperties()
        {
            UpdateCueProperties();

            foreach (var track in Tracks)
                track.UpdateProperties();
        }
        #endregion

        #region Init
        public Cue_Wrapper(ACB_Cue cue, ACB_Wrapper root)
        {
            CueRef = cue;
            WrapperRoot = root;

            Tracks.CollectionChanged += new NotifyCollectionChangedEventHandler(TrackAddedEvent);
            Refresh();
        }

        public void Refresh()
        {
            //Cleanup
            Tracks.Clear();

            //Init
            //Note: currently BlockSequence is completely unsupported
            if(CueRef.ReferenceType == ReferenceType.Sequence)
            {
                //For Sequence types the Track and Waveform objects must both be linked to the TrackWrapper
                ACB_Sequence sequence = WrapperRoot.AcbFile.GetTable(CueRef.ReferenceIndex.TableGuid, WrapperRoot.AcbFile.Sequences, true);

                foreach(var track in sequence?.Tracks)
                {
                    var acbTrack = WrapperRoot.AcbFile.GetTable(track.Index.TableGuid, WrapperRoot.AcbFile.Tracks, true);

                    if(acbTrack != null)
                    {
                        List<ACB_Waveform> waveforms = WrapperRoot.AcbFile.GetWaveformsFromTrack(acbTrack);

                        //if (waveforms.Count > 1)
                        //    throw new InvalidDataException("Validation error. More than 1 waveform on track.");

                        //if (waveforms.Count == 1)
                        //    Tracks.Add(new Track_Wrapper(waveforms[0], WrapperRoot, this, acbTrack));

                        foreach(var waveform in waveforms)
                            Tracks.Add(new Track_Wrapper(waveform, WrapperRoot, this, acbTrack));

                    }
                }
            }
            else if(CueRef.ReferenceType == ReferenceType.Synth)
            {
                ACB_Synth synth = WrapperRoot.AcbFile.GetTable(CueRef.ReferenceIndex.TableGuid, WrapperRoot.AcbFile.Synths, true);

                foreach(var refItem in synth.ReferenceItems)
                {
                    List<ACB_Waveform> waveforms = WrapperRoot.AcbFile.GetWaveformsFromReferenceItem(refItem);

                    foreach (var waveform in waveforms)
                    {
                        Tracks.Add(new Track_Wrapper(waveform, WrapperRoot, this, null, refItem));
                    }
                }
            }
            else
            {
                //Just link the waveform object.
                List<ACB_Waveform> waveforms = WrapperRoot.AcbFile.GetWaveformsFromCue(CueRef);
                foreach (var waveform in waveforms)
                {
                    Tracks.Add(new Track_Wrapper(waveform, WrapperRoot, this));
                }
            }

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
        }

        private void InitSynth()
        {
            foreach (var trackIndex in WrapperRoot.AcbFile.GetSynth(CueRef.ReferenceIndex.TableGuid).ActionTracks)
            {
                ACB_Track track = WrapperRoot.AcbFile.GetActionTrack(trackIndex.TableGuid);

                Tracks.Add(new Track_Wrapper(track, WrapperRoot, this, ActionTrackType.Synth));
            }
        }

        private void InitSequence()
        {

            //ActionTracks
            foreach (var trackIndex in WrapperRoot.AcbFile.GetSequence(CueRef.ReferenceIndex.TableGuid).ActionTracks)
            {
                ACB_Track track = WrapperRoot.AcbFile.GetActionTrack(trackIndex.TableGuid);

                Tracks.Add(new Track_Wrapper(track, WrapperRoot, this, ActionTrackType.Sequence));
            }
        }


        #endregion

        #region Operations
        public void UndoableEditVolumeBus(Guid stringGuid, ushort volume)
        {
            //Currently not used as a better method was discovered for volume control. May delete later.
            if (SequenceRef == null) return;

            List<IUndoRedo> undos = CreateSequenceCommands();
            ACB_CommandGroup commands = SequenceCommand;
            ACB_Command command = commands.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeBus);

            if (command == null)
            {
                //No volume command exists, so create one
                command = new ACB_Command(CommandType.VolumeBus);
                commands.Commands.Add(command);
                undos.Add(new UndoableListAdd<ACB_Command>(commands.Commands, command));
            }

            undos.Add(new UndoableProperty<AcbTableReference>("TableGuid", command.ReferenceIndex, command.ReferenceIndex.TableGuid, stringGuid));
            undos.Add(new UndoableProperty<ACB_Command>("Param2", command, command.Param2, volume));

            command.Param2 = volume;
            command.ReferenceIndex.TableGuid = stringGuid;

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Edit Volume"));
        }

        /// <summary>
        /// Cue level volume control.
        /// </summary>
        public void UndoableEditVolumeControl(ushort volumeBase, ushort randomRange)
        {
            if (SequenceRef == null) return;

            List<IUndoRedo> undos = CreateSequenceCommands();
            ACB_CommandGroup commands = SequenceCommand;
            ACB_Command command = commands.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);

            if (command == null)
            {
                //No volume command exists, so create one
                command = new ACB_Command(CommandType.VolumeRandomization2);
                commands.Commands.Add(command);
                undos.Add(new UndoableListAdd<ACB_Command>(commands.Commands, command));
            }

            if(command.CommandType == CommandType.VolumeRandomization1)
            {
                //Convert into VolumeRandomization2
                command.CommandType = CommandType.VolumeRandomization2;
                command.Param2 = command.Param1;
                command.Param1 = 0;
            }
            
            undos.Add(new UndoableProperty<ACB_Command>("Param1", command, command.Param1, volumeBase));
            undos.Add(new UndoableProperty<ACB_Command>("Param2", command, command.Param2, randomRange));

            command.Param1 = volumeBase;
            command.Param2 = randomRange;

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Volume Control"));
        }

        public void UndoableEditCueLimit(ushort cueLimit)
        {
            if (SequenceRef == null) return;
            
            //Might be worth seperating out all this duplicated code soon...
            List<IUndoRedo> undos = CreateSequenceCommands();
            ACB_CommandGroup commands = SequenceCommand;
            ACB_Command command = commands.Commands.FirstOrDefault(x => x.CommandType == CommandType.CueLimit);

            if (command == null)
            {
                command = new ACB_Command(CommandType.CueLimit);
                commands.Commands.Add(command);
                undos.Add(new UndoableListAdd<ACB_Command>(commands.Commands, command));
            }

            undos.Add(new UndoableProperty<ACB_Command>("Param1", command, command.Param1, cueLimit));
            command.Param1 = cueLimit;

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Cue Limit"));
        }

        public void UndoableAddTrackToCue(byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType)
        {
            List<IUndoRedo> undos = AddTrackToCue(trackBytes, streaming, loop, encodeType);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Add Track"));
        }

        public void UndoableAddActionToCue()
        {
            List<IUndoRedo> undos = AddActionToCue();
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Add Action"));
        }
        
        public void UndoableDeleteTrack(Track_Wrapper track)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(track.Type == TrackType.ActionTrack)
            {
                if(CueRef.ReferenceType == ReferenceType.Sequence)
                {
                    var trackEntryToRemove = SequenceRef.ActionTracks.FirstOrDefault(x => x.TableGuid == track.TrackRef.InstanceGuid);
                    undos.Add(new UndoableListRemove<AcbTableReference>(SequenceRef.ActionTracks, trackEntryToRemove));
                    SequenceRef.ActionTracks.Remove(trackEntryToRemove);
                }
                else if(CueRef.ReferenceType == ReferenceType.Synth)
                {
                    var trackEntryToRemove = SynthRef.ActionTracks.FirstOrDefault(x => x.TableGuid == track.TrackRef.InstanceGuid);
                    undos.Add(new UndoableListRemove<AcbTableReference>(SynthRef.ActionTracks, trackEntryToRemove));
                    SynthRef.ActionTracks.Remove(trackEntryToRemove);
                }

                undos.Add(new UndoableListRemove<Track_Wrapper>(Tracks, track));
                Tracks.Remove(track);
                
            }
            else if(track.Type == TrackType.Track)
            {
                if (CueRef.ReferenceType == ReferenceType.Sequence)
                {
                    var trackEntryToRemove = SequenceRef.Tracks.FirstOrDefault(x => x.Index.TableGuid == track.TrackRef.InstanceGuid);
                    undos.Add(new UndoableListRemove<ACB_SequenceTrack>(SequenceRef.Tracks, trackEntryToRemove));
                    SequenceRef.Tracks.Remove(trackEntryToRemove);
                }
                else if (CueRef.ReferenceType == ReferenceType.Synth)
                {
                    if(SynthRef.ReferenceItems.Count > 1)
                    {
                        //Just delete it
                        undos.Add(new UndoableListRemove<ACB_ReferenceItem>(SynthRef.ReferenceItems, track.refItemsRef));
                        SynthRef.ReferenceItems.Remove(track.refItemsRef);
                    }
                    else
                    {
                        //Last ReferenceItem on Synth. In this case, nullify it.
                        undos.Add(new UndoableProperty<AcbTableReference>("TableGuid", track.refItemsRef.ReferenceIndex, track.refItemsRef.ReferenceIndex.TableGuid, Guid.Empty));
                        track.refItemsRef.ReferenceIndex.TableGuid = Guid.Empty;
                    }
                }
                else if (CueRef.ReferenceType == ReferenceType.Waveform)
                {
                    undos.Add(new UndoableProperty<AcbTableReference>("TableGuid", CueRef.ReferenceIndex, CueRef.ReferenceIndex.TableGuid, Guid.Empty));
                    CueRef.ReferenceIndex.TableGuid = Guid.Empty;
                }
            }

            Refresh();
            undos.Add(new UndoActionDelegate(this, "Refresh", true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, (track.Type == TrackType.Track) ? "Delete Track" : "Delete Action"));
        }

        public List<IUndoRedo> AddTrackToCue(byte[] trackBytes, bool streaming, bool loop, EncodeType encodeType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.AddRange(WrapperRoot.AcbFile.AddTrackToCue(CueRef, trackBytes, streaming, loop, encodeType));
            undos.Add(new UndoActionDelegate(this, "Refresh", true));
            Refresh();
            return undos;
        }

        public List<IUndoRedo> AddActionToCue()
        {
            List<IUndoRedo> undos = WrapperRoot.AcbFile.AddActionToCue(CueRef);
            undos.Add(new UndoActionDelegate(this, "Refresh", true));
            Refresh();
            return undos;
        }

        #endregion

#if NvvmLight
        public RelayCommand CopyTrackCommand => new RelayCommand(CopyTrack);
        private void CopyTrack()
        {
            if(SelectedTrack != null)
            {
                if(SelectedTrack.Type == TrackType.Track)
                    WrapperRoot.CopyTrack(SelectedTrack);
                else if (SelectedTrack.Type == TrackType.ActionTrack)
                    WrapperRoot.CopyAction(SelectedTrack);
            }
        }

        public RelayCommand PasteTrackCommand => new RelayCommand(PasteTrack, CanPasteTrackOrAction);
        private void PasteTrack()
        {
            if (Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_TRACK))
                WrapperRoot.UndoablePasteTrack(this);
            else if (Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_ACTION))
                WrapperRoot.UndoablePasteAction(this);
        }

        public RelayCommand DeleteTrackCommand => new RelayCommand(DeleteTrack);
        private void DeleteTrack()
        {
            if (SelectedTrack != null)
            {
                UndoableDeleteTrack(SelectedTrack);
            }
        }


        private bool CanPasteTrackOrAction()
        {
            return (WrapperRoot.CanPasteTrack() || WrapperRoot.CanPasteAction());
        }
#endif

        /// <summary>
        /// Creates a command for the sequence if required (Sequence-type cues only).
        /// </summary>
        private List<IUndoRedo> CreateSequenceCommands()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            ACB_CommandGroup commands = SequenceCommand;

            if (commands == null)
            {
                commands = new ACB_CommandGroup();
                SequenceRef.CommandIndex.TableGuid = commands.InstanceGuid;
                undos.Add(new UndoableProperty<AcbTableReference>("TableGuid", SequenceRef.CommandIndex, Guid.Empty, SequenceRef.CommandIndex.TableGuid));
                undos.AddRange(WrapperRoot.AcbFile.CommandTables.AddCommand(commands, CommandTableType.SequenceCommand));
            }

            return undos;
        }
        

        public bool CanAddTracks()
        {
            return WrapperRoot.AcbFile.CanAddTrack(CueRef);
        }

        public bool CanAddActions()
        {
            return WrapperRoot.AcbFile.CanAddAction(CueRef);
        }

        public float GetBaseVolume()
        {
            ACB_Command volumeCommand = SequenceCommand?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            if (volumeCommand == null) return 1f;
            return (volumeCommand.CommandType == CommandType.VolumeRandomization2) ? volumeCommand.Param1 / 100f : 0f;
        }

        public float GetRandomVolume()
        {
            ACB_Command volumeCommand = SequenceCommand?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            if (volumeCommand == null) return 0f;
            return (volumeCommand.CommandType == CommandType.VolumeRandomization2) ? volumeCommand.Param2 / 100f : volumeCommand.Param1 / 100f;
        }
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
        public Cue_Wrapper CueWrapper;
        public ACB_ReferenceItem refItemsRef = null;

        public TrackType Type;
        public ActionTrackType ActionTrackType;
        public ACB_Track TrackRef { get; set; }
        public Waveform_Wrapper WaveformWrapper { get; set; }

        //Wrapped values
        public ACB_CommandGroup TrackCommand
        {
            get
            {
                //Gets the command on TrackRef (could be Track or ActionTrack)
                if (TrackRef?.CommandIndex.IsNull == null) return null;
                return WrapperRoot.AcbFile.CommandTables.GetCommand(TrackRef.CommandIndex.TableGuid, (Type == TrackType.ActionTrack) ? CommandTableType.TrackEvent : CommandTableType.TrackCommand);
            }
        }
        public int UndoableTargetId //We must update the GUID as the ID is changed
        {
            get
            {
                if (TrackRef == null) return 0;
                //Ensure the cue GUID matches the cue ID (in the event it was changed)
                var cue = WrapperRoot.AcbFile.GetCue(TrackRef.TargetId.TableGuid);

                if (cue != null)
                    TrackRef.TargetId.TableIndex = cue.ID;

                return (int)TrackRef?.TargetId.TableIndex;
            }
            set
            {
                if (TrackRef == null) return;
                var cue = WrapperRoot.AcbFile.GetCue(value);
                var originalGuid = TrackRef.TargetId.TableGuid;
                var originalId = TrackRef.TargetId.TableIndex;

                if (cue != null)
                {
                    TrackRef.TargetId.TableGuid = cue.InstanceGuid;
                    TrackRef.TargetId.TableIndex = (uint)value;
                }
                else
                {
                    TrackRef.TargetId.TableGuid = Guid.Empty;
                    TrackRef.TargetId.TableIndex = (uint)value;
                }
                UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>()
                {
                    new UndoableProperty<AcbTableReference>("TableGuid", TrackRef.TargetId, originalGuid, TrackRef.TargetId.TableGuid),
                    new UndoableProperty<AcbTableReference>("TableIndex", TrackRef.TargetId, originalId, TrackRef.TargetId.TableIndex),
                    new UndoActionDelegate(this, "UpdateProperties", true)
                },  "Target Id"));
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
                        if(CueWrapper.CueRef.ReferenceType == ReferenceType.Sequence)
                            return string.Format("[TRACK {1}] Duration: {0}", WaveformWrapper?.HcaMeta.Duration, IndexOfTrack());
                        else
                            return string.Format("[TRACK] Duration: {0}", WaveformWrapper?.HcaMeta.Duration);
                    default:
                        return "[Unknown Track Type]";
                }
            }
        }
        public Visibility IsTrack { get { return (Type == TrackType.Track) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsAction { get { return (Type == TrackType.ActionTrack) ? Visibility.Visible : Visibility.Collapsed; } }

        /// <summary>
        /// Wrapper for a Track.
        /// </summary>
        public Track_Wrapper(ACB_Waveform waveform, ACB_Wrapper root, Cue_Wrapper _cueWrapper, ACB_Track track = null, ACB_ReferenceItem refItems = null)
        {
            TrackRef = track;
            CueWrapper = _cueWrapper;
            WaveformWrapper = new Waveform_Wrapper(waveform, root);
            WrapperRoot = root;
            Type = TrackType.Track;
            refItemsRef = refItems;
        }

        /// <summary>
        /// Wrapper for an Action Track.
        /// </summary>
        public Track_Wrapper(ACB_Track acbTrack, ACB_Wrapper root, Cue_Wrapper _cueWrapper, ActionTrackType actionType)
        {
            CueWrapper = _cueWrapper;
            TrackRef = acbTrack;
            WrapperRoot = root;
            Type = TrackType.ActionTrack;
            ActionTrackType = actionType;
        }


        #region UndoableOperations
        public void UndoableReplaceTrack(byte[] trackBytes, bool streaming, EncodeType encodeType)
        {
            if (WaveformWrapper == null) return;
            List<IUndoRedo> undos = WrapperRoot.AcbFile.ReplaceTrackOnWaveform(WaveformWrapper.WaveformRef, trackBytes, streaming, encodeType);
            undos.AddRange(WrapperRoot.AcbFile.UpdateCueLength(CueWrapper.CueRef));
            UpdateProperties();
            undos.Add(new UndoActionDelegate(this, "UpdateProperties", true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace Track"));
        }

        /// <summary>
        /// Track level volume control.
        /// </summary>
        public void UndoableEditVolumeControl(ushort volumeBase, ushort randomRange)
        {
            if (TrackRef == null) return;

            List<IUndoRedo> undos = CreateTrackCommands();
            ACB_CommandGroup commands = TrackCommand;
            ACB_Command command = commands.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);

            if (command == null)
            {
                //No volume command exists, so create one
                command = new ACB_Command(CommandType.VolumeRandomization2);
                commands.Commands.Add(command);
                undos.Add(new UndoableListAdd<ACB_Command>(commands.Commands, command));
            }

            if (command.CommandType == CommandType.VolumeRandomization1)
            {
                //Convert into VolumeRandomization2
                command.CommandType = CommandType.VolumeRandomization2;
                command.Param2 = command.Param1;
                command.Param1 = 0;
            }

            undos.Add(new UndoableProperty<ACB_Command>("Param1", command, command.Param1, volumeBase));
            undos.Add(new UndoableProperty<ACB_Command>("Param2", command, command.Param2, randomRange));

            command.Param1 = volumeBase;
            command.Param2 = randomRange;

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Volume Control"));
        }

        #endregion

        /// <summary>
        /// Creates commands for this track if there are none already present. Note: this is the commands on the ACB_Track object itself, not any child objects.
        /// </summary>
        public List<IUndoRedo> CreateTrackCommands()
        {
            //Presently unused...
            List<IUndoRedo> undos = new List<IUndoRedo>();
            ACB_CommandGroup commands = TrackCommand;
            
            if (TrackCommand == null)
            {
                commands = new ACB_CommandGroup();
                TrackRef.CommandIndex.TableGuid = commands.InstanceGuid;
                undos.Add(new UndoableProperty<AcbTableReference>("TableGuid", TrackRef.CommandIndex, Guid.Empty, TrackRef.CommandIndex.TableGuid));
                undos.AddRange(WrapperRoot.AcbFile.CommandTables.AddCommand(commands, CommandTableType.TrackCommand));
            }

            return undos;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("Type");
            NotifyPropertyChanged("ActionTrackType");
            NotifyPropertyChanged("IsTrack");
            NotifyPropertyChanged("IsAction");
            NotifyPropertyChanged("DisplayName");
            NotifyPropertyChanged("UndoableTargetId");

            WaveformWrapper?.UpdateProperties();
        }

        public int IndexOfTrack()
        {
            if(CueWrapper.SequenceRef != null)
            {
                //Determines the correct index when multiple waveforms are on the same track
                foreach(var track in CueWrapper.SequenceRef.Tracks)
                {
                    if (track.Index.TableGuid == TrackRef.InstanceGuid) return CueWrapper.SequenceRef.Tracks.IndexOf(track);
                }
            }
            else
            {
                //Generic index checking. Wont show the actual track # from acb, just the wrapper index!
                int index = 0;
                foreach (var track in CueWrapper.Tracks.Where(x => x.Type == TrackType.Track))
                {
                    if (track == this) return index;
                    index++;
                }
            }
            
            return -1;
        }

        public float GetBaseVolume()
        {
            ACB_Command volumeCommand = TrackCommand?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            if (volumeCommand == null) return 1f;
            return (volumeCommand.CommandType == CommandType.VolumeRandomization2) ? volumeCommand.Param1 / 100f : 0f;
        }

        public float GetRandomVolume()
        {
            ACB_Command volumeCommand = TrackCommand?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            if (volumeCommand == null) return 0f;
            return (volumeCommand.CommandType == CommandType.VolumeRandomization2) ? volumeCommand.Param2 / 100f : volumeCommand.Param1 / 100f;
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
        public AFS2_Entry AwbEntry
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
        public TrackMetadata HcaMeta
        {
            get
            {
                if(AwbEntry == null) return new TrackMetadata();
                return AwbEntry.HcaInfo;
            }
        }

        public Waveform_Wrapper(ACB_Waveform waveform, ACB_Wrapper root)
        {
            WaveformRef = waveform;
            WrapperRoot = root;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("HcaMeta");
        }

        public void UndoableEditLoop(bool loop, int loopStartMs, int loopEndMs)
        {
            List<IUndoRedo> undos = WrapperRoot.AcbFile.EditLoopOnWaveform(WaveformRef, loop, loopStartMs, loopEndMs);
            undos.Add(new UndoActionDelegate(this, "UpdateProperties", true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Edit Loop"));
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

    //Copy/Paste
    [Serializable]
    public class CopiedTrack
    {
        public byte[] TrackBytes { get; set; }
        public bool Streaming { get; set; }
        public bool Loop { get; set; }
        public EncodeType encodeType { get; set; }

        public CopiedTrack(ACB_Waveform waveform, AFS2_Entry awbEntry)
        {
            TrackBytes = awbEntry?.bytes;
            Streaming = (bool)waveform?.IsStreaming;
            Loop = (waveform?.LoopFlag == 0) ? false : true;
            encodeType = waveform.EncodeType;
        }
    }

    [Serializable]
    public class CopiedAction
    {
        public ACB_Track Track;
        public ACB_CommandGroup Commands;

        public CopiedAction(ACB_Track action, ACB_CommandGroup command)
        {
            Track = action.Copy();
            Commands = command.Copy();

            //Generate new guids
            Track.InstanceGuid = Guid.NewGuid();
            Commands.InstanceGuid = Guid.NewGuid();
            Track.CommandIndex = new AcbTableReference();
            Track.CommandIndex.TableGuid = Commands.InstanceGuid;
        }
    }
}
