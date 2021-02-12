using AudioCueEditor.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xv2CoreLib.ACB_NEW;
using Xv2CoreLib.AFS2;
using VGAudio.Cli;
using System.IO;
using Microsoft.Win32;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using Xv2CoreLib.Resource.UndoRedo;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for CueEditorView.xaml
    /// </summary>
    public partial class CueEditorView : UserControl, INotifyPropertyChanged
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


        public static readonly DependencyProperty AcbFileProperty = DependencyProperty.Register(
            "AcbFile", typeof(ACB_Wrapper), typeof(CueEditorView), new PropertyMetadata(default(ACB_Wrapper)));

        public ACB_Wrapper AcbFile
        {
            get { return (ACB_Wrapper)GetValue(AcbFileProperty); }
            set
            {
                SetValue(AcbFileProperty, value);
                NotifyPropertyChanged(nameof(AcbFile));
            }
        }

        

        public AudioPlayer audioPlayer { get; set; } = new AudioPlayer();


        //Visibilities
        public Visibility SequenceCueNotVisibile
        {
            get
            {
                if (dataGrid.SelectedItem is Cue_Wrapper cue)
                    return (cue.CueRef.ReferenceType == ReferenceType.Sequence) ? Visibility.Hidden : Visibility.Visible;
                return Visibility.Visible;
            }
        }
        public Visibility SequenceCueVisibile
        {
            get
            {
                if (dataGrid.SelectedItem is Cue_Wrapper cue)
                    return (cue.CueRef.ReferenceType == ReferenceType.Sequence) ? Visibility.Visible : Visibility.Hidden;
                return Visibility.Hidden;
            }
        }
        public Visibility ActionNotVisibile
        {
            get
            {
                return (GetSelectedTrack(TrackType.ActionTrack) != null) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public Visibility ActionVisibile
        {
            get
            {
                return (GetSelectedTrack(TrackType.ActionTrack) != null) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility TrackNotVisibile
        {
            get
            {
                return (GetSelectedTrack(TrackType.Track) != null) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public Visibility TrackVisibile
        {
            get
            {
                return (GetSelectedTrack(TrackType.Track) != null) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility ActionChoiceVisible
        {
            get
            {
                return (actionComboBox.SelectedItem is ACB_Command) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        //Props
        public Cue_Wrapper SelectedCue
        {
            get
            {
                return GetSelectedCue();
            }
        }
        public Track_Wrapper SelectedTrack
        {
            get
            {
                if (dataGrid.SelectedItem is Cue_Wrapper cue)
                    return cue.SelectedTrack;
                return null;
            }
        }
        public List<CommandType> ActionCommandTypes { get; set; } = new List<CommandType>() { CommandType.Null, CommandType.Action_Play, CommandType.Action_Stop, CommandType.Wait };

        #region Commands
        public RelayCommand PlayCommand => new RelayCommand(Play, CanPlayTrack);
        private async void Play()
        {
            audioPlayer.Play();
        }

        public RelayCommand StopCommand => new RelayCommand(Stop, CanPlayTrack);
        private async void Stop()
        {
            audioPlayer.Stop();
        }

        public RelayCommand PauseCommand => new RelayCommand(Pause, CanPlayTrack);
        private async void Pause()
        {
            audioPlayer.Pause();
        }
        
        public RelayCommand RewindCommand => new RelayCommand(Rewind, CanPlayTrack);
        private async void Rewind()
        {
            audioPlayer.Rewind();
        }
        
        public RelayCommand FastForwardCommand => new RelayCommand(FastForward, CanPlayTrack);
        private async void FastForward()
        {
            audioPlayer.FastForward();
        }
        
        public RelayCommand AddNewCueCommand => new RelayCommand(AddNewCue, IsFileLoaded);
        private async void AddNewCue()
        {
            AddCueForm form = new AddCueForm(Application.Current.MainWindow, string.Format("cue_{0}", AcbFile.AcbFile.GetFreeCueId()));
            form.ShowDialog();

            while (!form.IsDone)
            {
                await Task.Delay(50);
            }

            if (form.Finished)
            {
                if (form.AddTrack)
                    AcbFile.UndoableAddCue(form.CueName, form.ReferenceType, form.HcaBytes, form.Streaming, form.Loop, form.Is3DSound);
                else
                    AcbFile.UndoableAddCue(form.CueName, form.ReferenceType, form.Is3DSound);
            }
        }


        private bool CanPlayTrack()
        {
            return audioPlayer.HasWave;
        }
        
        private bool IsCueSelected()
        {
            return dataGrid.SelectedItem is Cue_Wrapper;
        }

        private bool IsFileLoaded()
        {
            return AcbFile != null;
        }
        #endregion


        public CueEditorView()
        {
            InitializeComponent();
            rootGrid.DataContext = this;
            dataGrid.SelectionChanged += new SelectionChangedEventHandler(CueSelectionChanged);
        }
        
        private void CueSelectionChanged(object sender, SelectionChangedEventArgs arg)
        {
            NotifyPropertyChanged("SequenceCueNotVisibile");
            NotifyPropertyChanged("SequenceCueVisibile");
            NotifyPropertyChanged("ActionNotVisibile");
            NotifyPropertyChanged("ActionVisibile");
            NotifyPropertyChanged("TrackNotVisibile");
            NotifyPropertyChanged("TrackVisibile");
            NotifyPropertyChanged("SelectedCue");
        }
        
        //Actions
        public RelayCommand ActionCommandAddCommand => new RelayCommand(ActionCommandAdd, CanAddActionCommand);
        private async void ActionCommandAdd()
        {
            var track = GetSelectedTrack(TrackType.ActionTrack);
            if (track != null)
            {
                var newCommand = new ACB_Command() { CommandType = CommandType.Null };
                track.TrackCommand.Commands.Add(newCommand);
                UndoManager.Instance.AddUndo(new UndoableListAdd<ACB_Command>(track.TrackCommand.Commands, newCommand, "Add Action"));
            }
        }

        public RelayCommand ActionCommandRemoveCommand => new RelayCommand(ActionCommandRemove, CanRemoveActionCommand);
        private async void ActionCommandRemove()
        {
            var track = GetSelectedTrack(TrackType.ActionTrack);
            if (track != null)
            {
                if (actionComboBox.SelectedItem is ACB_Command command)
                {
                    int originalIdx = track.TrackCommand.Commands.IndexOf(command);
                    var original = command.Copy();
                    track.TrackCommand.Commands.Remove(command);
                    UndoManager.Instance.AddUndo(new UndoableListRemove<ACB_Command>(track.TrackCommand.Commands, original, originalIdx, "Remove Action"));
                }
            }
        }
        
        private bool CanAddActionCommand()
        {
            return GetSelectedTrack(TrackType.ActionTrack) != null;
        }
        
        private bool CanRemoveActionCommand()
        {
            return actionComboBox.SelectedItem is ACB_Command command;
        }


        //Cue/Track DataGrid
        public RelayCommand PlaySelectedTrackCommand => new RelayCommand(PlaySelectedTrack, IsTrackSelected);
        private async void PlaySelectedTrack()
        {
            var track = GetSelectedTrack(TrackType.Track);
            var cue = GetSelectedCue();

            if (track != null && cue != null)
            {
                if (track.WaveformWrapper != null)
                {
                    var afs2Entry = AcbFile.AcbFile.GetAfs2Entry((track.WaveformWrapper.WaveformRef != null) ? track.WaveformWrapper.WaveformRef.AwbId : ushort.MaxValue);

                    if (afs2Entry != null)
                    {
                        audioPlayer.Stop();

                        switch (track.WaveformWrapper.WaveformRef.EncodeType)
                        {
                            case EncodeType.HCA:
                            case EncodeType.HCA_ALT:
                                await audioPlayer.AsyncSetHcaAudio(afs2Entry.bytes);
                                break;
                            case EncodeType.ADX:
                                audioPlayer.SetAudio(ADX.Decode(afs2Entry.bytes));
                                break;
                            case EncodeType.ATRAC9:
                                audioPlayer.SetAudio(AT9.Decode(afs2Entry.bytes));
                                break;
                        }

                        //Set volume
                        float cueBaseVolume = cue.GetBaseVolume();
                        float cueRandom = cue.GetRandomVolume();
                        float trackBasekVolume = track.GetBaseVolume();
                        float trackRandom = track.GetRandomVolume();

                        float cueVolume = cueBaseVolume + Xv2CoreLib.Random.Range(0, cueRandom);
                        float trackVolume = trackBasekVolume + Xv2CoreLib.Random.Range(0, trackRandom);
                        float finalVolume = ((trackVolume * cueVolume) > 1f) ? 1f : trackVolume * cueVolume;

                        audioPlayer.SetVolume(finalVolume);

                        //Play
                        audioPlayer.Play();

                        //Force Update UI
                        CommandManager.InvalidateRequerySuggested();
                    }
                }
            }
        }
        
        public RelayCommand ReplaceSelectedTrackCommand => new RelayCommand(ReplaceSelectedTrack, IsTrackSelected);
        private async void ReplaceSelectedTrack()
        {
            var track = GetSelectedTrack(TrackType.Track);

            if (track != null)
            {
                var trackForm = new AddTrackForm(Application.Current.MainWindow);
                trackForm.ShowDialog();

                while (!trackForm.IsDone)
                {
                    await Task.Delay(50);
                }

                if (trackForm.Finished)
                {
                    track.UndoableReplaceTrack(trackForm.HcaBytes, trackForm.Streaming);
                }
            }
        }

        public RelayCommand ExtractTrackCommand => new RelayCommand(ExtractSelectedTrack, IsTrackSelected);
        private async void ExtractSelectedTrack()
        {
            var track = GetSelectedTrack(TrackType.Track);
                
            if (track != null)
            {
                if (track.WaveformWrapper != null)
                {
                    var afs2Entry = AcbFile.AcbFile.GetAfs2Entry((track.WaveformWrapper.WaveformRef != null) ? track.WaveformWrapper.WaveformRef.AwbId : ushort.MaxValue);
                    await Task.Run(() => ExtractTrack(afs2Entry, string.Format("{0}_{1}", track.CueWrapper.CueRef.Name, track.IndexOfTrack()), Helper.GetFileType(track.WaveformWrapper.WaveformRef.EncodeType)));

                }
            }
        }

        public RelayCommand AddTrackToCueCommand => new RelayCommand(AddTrackToCue, IsCueSelected);
        private async void AddTrackToCue()
        {
            var cue = GetSelectedCue();

            var trackForm = new AddTrackForm(Application.Current.MainWindow);
            trackForm.ShowDialog();

            while (!trackForm.IsDone)
            {
                await Task.Delay(50);
            }

            if (trackForm.Finished)
            {
                cue.UndoableAddTrackToCue(trackForm.HcaBytes, trackForm.Streaming, trackForm.Loop);
            }
        }

        public RelayCommand AddActionToCueCommand => new RelayCommand(AddActionToCue, IsCueSelected);
        private async void AddActionToCue()
        {
            GetSelectedCue().UndoableAddActionToCue();
        }

        public RelayCommand EditLoopCommand => new RelayCommand(EditLoopOnSelectedTrack, IsTrackSelected);
        private async void EditLoopOnSelectedTrack()
        {
            var track = GetSelectedTrack(TrackType.Track);

            if (track != null)
            {
                audioPlayer.Stop();
                var trackForm = new EditLoopForm(Application.Current.MainWindow, track.WaveformWrapper, audioPlayer);
                trackForm.ShowDialog();
            }
        }

        public RelayCommand CopyCuesCommand => new RelayCommand(CopyCues, IsCueSelected);
        private async void CopyCues()
        {
            List<Cue_Wrapper> selectedCues = dataGrid.SelectedItems.Cast<Cue_Wrapper>().ToList();

            if(selectedCues != null)
            {
                AcbFile.CopyCues(selectedCues);
            }
        }

        public RelayCommand PasteCuesCommand => new RelayCommand(PasteCues, CanPasteOnCue);
        private async void PasteCues()
        {
            if (Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_CUES))
                AcbFile.UndoablePasteCues();
            else if (Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_TRACK))
                AcbFile.UndoablePasteTrack(GetSelectedCue());
            else if (Clipboard.ContainsData(ACB_File.CLIPBOARD_ACB_ACTION))
                AcbFile.UndoablePasteAction(GetSelectedCue());
        }

        public RelayCommand EditVolumeCommand => new RelayCommand(EditVolume, IsCueSelected);
        private async void EditVolume()
        {
            var cue = GetSelectedCue();

            if (cue != null)
            {
                if (cue.SequenceRef == null)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, $"Unsupported operation", $"Edit Volume is not available on this cue type. Only Sequence type cues are supported.", MessageDialogStyle.Affirmative);
                    return;
                }
                var trackForm = new VolumeControl(Application.Current.MainWindow, cue);
                trackForm.ShowDialog();
            }
        }

        public RelayCommand EditCueLimitCommand => new RelayCommand(EditCueLimit, IsCueSelected);
        private async void EditCueLimit()
        {
            var cue = GetSelectedCue();

            if (cue != null)
            {
                if (cue.SequenceRef == null)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, $"Unsupported operation", $"Edit Cue Limit is not available on this cue type. Only Sequence type cues are supported.", MessageDialogStyle.Affirmative);
                    return;
                }
                var trackForm = new EditCueLimit(Application.Current.MainWindow, cue);
                trackForm.ShowDialog();
            }
        }

        public RelayCommand EditCueIdCommand => new RelayCommand(EditCueId, IsCueSelected);
        private async void EditCueId()
        {
            var cue = GetSelectedCue();
            start:
            var result = await DialogCoordinator.Instance.ShowInputAsync(Application.Current.MainWindow, "Change Cue ID", "Enter a new ID for the cue:", new MetroDialogSettings() { DefaultText = cue.CueRef.ID.ToString(), AnimateShow = false, AnimateHide = false });

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            uint num;
            if (!uint.TryParse(result, out num))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "Invalid Characters", "The entered ID contains invalid characters.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }

            if (AcbFile.Cues.FirstOrDefault(a => a.CueRef.ID == num && a != cue) != null)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "ID Already Used", "The entered ID is already used by another cue. Please enter a unique one.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateShow = false, AnimateHide = false });
                goto start;
            }
            else
            {
                cue.UndoableCueId = num;
            }
        }

        public RelayCommand DeleteCueCommand => new RelayCommand(DeleteCue, IsCueSelected);
        private async void DeleteCue()
        {
            List<Cue_Wrapper> selectedCues = dataGrid.SelectedItems.Cast<Cue_Wrapper>().ToList();
            AcbFile.UndoableDeleteCues(selectedCues);
        }


        private bool IsTrackSelected()
        {
            return GetSelectedTrack(TrackType.Track) != null;
        }
        
        private bool CanPasteOnCue()
        {
            if (!IsCueSelected() || !IsFileLoaded()) return false;
            if (AcbFile.CanPasteCues() || AcbFile.CanPasteAction() || AcbFile.CanPasteTrack()) return true;
            return false;
        }
        
        //Helper
        private Track_Wrapper GetSelectedTrack(TrackType type)
        {
            if (dataGrid.SelectedItem is Cue_Wrapper cue)
            {
                if (cue.SelectedTrack != null)
                {
                    if (cue.SelectedTrack.Type == type)
                        return cue.SelectedTrack;
                }
            }
            return null;
        }

        private Cue_Wrapper GetSelectedCue()
        {
            return dataGrid.SelectedItem as Cue_Wrapper;
        }

        public void ExtractTrack(AFS2_AudioFile entry, string name, FileType format)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Extract track...";

            if (format != FileType.Hca && format != FileType.Adx && format != FileType.Wave)
                saveDialog.Filter = "Binary file |*.bin;";
            else
                saveDialog.Filter = "WAV |*.wav; |HCA |*.hca; |ADX |*.adx;";

            saveDialog.AddExtension = true;
            saveDialog.FileName = name;

            if (saveDialog.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(saveDialog.FileName)) return;
                FileType outputType = FileType.NotSet;

                switch (System.IO.Path.GetExtension(saveDialog.FileName).ToLower())
                {
                    case ".hca":
                        outputType = FileType.Hca;
                        break;
                    case ".adx":
                        outputType = FileType.Adx;
                        break;
                    case ".atrac9":
                        outputType = FileType.Atrac9;
                        break;
                    case ".wav":
                        outputType = FileType.Wave;
                        break;
                }

                if (outputType == FileType.NotSet)
                {
                    File.WriteAllBytes(saveDialog.FileName, entry.bytes);
                }
                else
                {
                    using (MemoryStream stream = new MemoryStream(entry.bytes))
                    {
                        byte[] bytes = ConvertStream.ConvertFile(new Options(), stream, format, outputType);
                        File.WriteAllBytes(saveDialog.FileName, bytes);
                    }
                }
            }
        }


        //Track Events (cant use commands on a DataTemplated listbox... so this is needed)
        private void ListBoxEvent_PlaySelectedTrack(object sender, RoutedEventArgs e)
        {
            PlaySelectedTrack();
        }
        
        private void ListBoxEvent_ReplaceSelectedTrack(object sender, RoutedEventArgs e)
        {
            if (IsTrackSelected())
                ReplaceSelectedTrack();
        }

        private void ListBoxEvent_EditLoop_Click(object sender, RoutedEventArgs e)
        {
            if (IsTrackSelected())
                EditLoopOnSelectedTrack();
        }

        private void ListBoxEvent_ExtractSelectedTrack(object sender, RoutedEventArgs e)
        {
            if (IsTrackSelected())
                ExtractSelectedTrack();
        }

        private async void ListBoxEvent_EditVolumeOnTrack_Click(object sender, RoutedEventArgs e)
        {
            var track = GetSelectedTrack(TrackType.Track);

            if (track != null)
            {
                if (track.TrackRef == null)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, $"Unsupported operation", $"Edit Volume is not available on this cue type. Only Sequence type cues are supported.", MessageDialogStyle.Affirmative);
                    return;
                }
                var trackForm = new VolumeControl(Application.Current.MainWindow, track);
                trackForm.ShowDialog();
            }
        }

        private void ActionCommandAdd_Proxy(object sender, RoutedEventArgs e)
        {
            if (CanAddActionCommand())
                ActionCommandAdd();
        }

        private void ActionCommandRemove_Proxy(object sender, RoutedEventArgs e)
        {
            if (CanRemoveActionCommand())
                ActionCommandRemove();
        }


        private void TrackListBox_DoubleMouseClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelectedTrack();
        }

        private void SeqTrack_DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()).ToString();
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ActionChoiceVisible");
        }
        
        //File Drop (messy duplication, for now...)
        private async void DataGrid_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                    
                    foreach (string droppedFile in droppedFilePaths)
                    {
                        switch (System.IO.Path.GetExtension(droppedFile))
                        {
                            case ".wav":
                            case ".mp3":
                            case ".wma":
                            case ".aac":
                            case ".hca":
                                AddCueForm form = new AddCueForm(Application.Current.MainWindow, droppedFile, true);

                                while (!form.IsDone)
                                {
                                    await Task.Delay(50);
                                }

                                if (form.Finished)
                                {
                                    if (form.AddTrack)
                                        AcbFile.UndoableAddCue(form.CueName, form.ReferenceType, form.HcaBytes, form.Streaming, form.Loop, false);
                                    else
                                        AcbFile.UndoableAddCue(form.CueName, form.ReferenceType, false);
                                }
                                break;
                            default:
                                MessageBox.Show(String.Format("The filetype of the dropped file ({0}) is not supported.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ListBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                    foreach (string droppedFile in droppedFilePaths)
                    {
                        switch (System.IO.Path.GetExtension(droppedFile))
                        {
                            case ".wav":
                            case ".mp3":
                            case ".wma":
                            case ".aac":
                            case ".hca":
                                var cue = GetSelectedCue();
                                if (cue == null) return;
                                var trackForm = new AddTrackForm(Application.Current.MainWindow, droppedFile);

                                while (!trackForm.IsDone)
                                {
                                    await Task.Delay(50);
                                }

                                if (trackForm.Finished)
                                {
                                    cue.UndoableAddTrackToCue(trackForm.HcaBytes, trackForm.Streaming, trackForm.Loop);
                                }
                                break;
                            default:
                                MessageBox.Show(String.Format("The filetype of the dropped file ({0}) is not supported.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
    }
}
