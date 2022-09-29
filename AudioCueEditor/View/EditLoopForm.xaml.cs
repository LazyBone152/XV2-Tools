using AudioCueEditor.Audio;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Xv2CoreLib.ACB;
using Xv2CoreLib.HCA;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for EditLoopForm.xaml
    /// </summary>
    public partial class EditLoopForm : MetroWindow, INotifyPropertyChanged
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

        public Waveform_Wrapper WaveformWrapper { get; set; }

        private uint _loopStartMs = 0;
        private uint _loopEndMs = 50;
        private uint _trackLengthMs = 50;
        private bool _loopEnabled = false;
        private byte[] awbBytes = null;

        public uint LoopStartMs { get { return _loopStartMs; } set { _loopStartMs = value; ValuesChanged(); } }
        public uint LoopEndMs { get { return _loopEndMs; } set { _loopEndMs = value; ValuesChanged(); } }
        public uint TrackLengthMs { get { return _trackLengthMs; } set { _trackLengthMs = value; ValuesChanged(); } }

        public string LoopStartString { get { return FormatTime(LoopStartMs); } }
        public string LoopEndString { get { return FormatTime(LoopEndMs); } }
        public string CurrentTimeString { get { return (wavStream != null) ? FormatTime((wavStream.waveStream != null) ? wavStream.waveStream.CurrentTime : new TimeSpan()) : null; } }
        
        public TimeSpan StartLoopTime
        {
            get => new TimeSpan(0, 0, 0, 0, (int)LoopStartMs);
            set
            {
                if(value.TotalMilliseconds < LoopEndMs)
                    LoopStartMs = (uint)value.TotalMilliseconds;

                ValuesChanged();
            }
        }
        public TimeSpan EndLoopTime
        {
            get => new TimeSpan(0, 0, 0, 0, (int)LoopEndMs);
            set
            {
                if (value.TotalMilliseconds <= TrackLengthMs)
                    LoopEndMs = (uint)value.TotalMilliseconds;

                ValuesChanged();
            }
        }

        public bool LoopEnabled { get { return _loopEnabled; } set { _loopEnabled = value; NotifyPropertyChanged(nameof(LoopEnabled)); } }

        private WavStream wavStream = null;
        private AudioPlayer audioPlayer;

        private DispatcherTimer timer = new DispatcherTimer();
        

        public EditLoopForm(Window parent, Waveform_Wrapper waveformWrapper, AudioPlayer _audioPlayer)
        {
            _audioPlayer?.Stop();

            WaveformWrapper = waveformWrapper;
            InitializeComponent();
            DataContext = this;
            audioPlayer = _audioPlayer;
            Owner = parent;
            InitValues();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(CurrentTimeString));
        }

        private async void InitValues()
        {
            var awbEntry = WaveformWrapper.WrapperRoot.AcbFile.GetAfs2Entry(WaveformWrapper.WaveformRef.AwbId);

            if(awbEntry != null)
            {
                VGAudio.Containers.Hca.HcaReader reader = new VGAudio.Containers.Hca.HcaReader();
                var header = reader.ParseFile(awbEntry.bytes);

                if (header.Hca != null)
                {
                    LoopEnabled = header.Hca.Looping;
                    LoopStartMs = (uint)header.Hca.LoopStartMs;
                    LoopEndMs = (uint)header.Hca.LoopEndMs;
                    TrackLengthMs = (uint)header.Hca.TrackLengthMs;
                    ValuesChanged();

                    awbBytes = awbEntry.bytes;
                    await Task.Run(() => wavStream = HCA.DecodeToWavStream(awbEntry.bytes));
                    SetLoopOnStream();

                    CommandManager.InvalidateRequerySuggested();
                }
            }
            else
            {
                Close();
            }
        }

        private void ValuesChanged()
        {
            NotifyPropertyChanged(nameof(LoopStartMs));
            NotifyPropertyChanged(nameof(LoopEndMs));
            NotifyPropertyChanged(nameof(LoopStartString));
            NotifyPropertyChanged(nameof(LoopEndString));
            NotifyPropertyChanged(nameof(TrackLengthMs));
            NotifyPropertyChanged(nameof(CurrentTimeString));

            NotifyPropertyChanged(nameof(StartLoopTime));
            NotifyPropertyChanged(nameof(EndLoopTime));
            SetLoopOnStream();
        }

        private void SetLoopOnStream()
        {
            if (wavStream == null) return;
            if (!wavStream.IsValid) return;

            wavStream.waveStream.EnableLooping = true;
            wavStream.waveStream.ForceLoop = true;
            wavStream.waveStream.LoopStart = new TimeSpan(0, 0, 0, 0, (int)LoopStartMs);
            wavStream.waveStream.LoopEnd = new TimeSpan(0, 0, 0, 0, (int)LoopEndMs);
        }

        private string FormatTime(uint ms)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, 0, (int)ms);
            return FormatTime(time);
        }

        private string FormatTime(TimeSpan time)
        {
            return string.Format("{0}:{1}:{2}:{3}", time.Hours.ToString("00"), time.Minutes.ToString("00"), time.Seconds.ToString("00"), time.Milliseconds.ToString("0000"));
        }

        public RelayCommand ApplyCommand => new RelayCommand(Apply, CanApply);
        private void Apply()
        {
            try
            {
                WaveformWrapper.UndoableEditLoop(LoopEnabled, (int)LoopStartMs, (int)LoopEndMs);
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while processing the ApplyCommand command.\n\nDetails:{ex.Message}", $"ApplyCommand failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
            finally
            {
                Close();
            }
        }

        public RelayCommand PlayPreviewCommand => new RelayCommand(PlayPreview, IsStreamLoaded);
        private async void PlayPreview()
        {
#if !DEBUG
            try
#endif
            {
                if (!wavStream.IsValid)
                {
                    wavStream = null;
                    await Task.Run(() => wavStream = HCA.DecodeToWavStream(awbBytes));
                    SetLoopOnStream();
                }

                if(!audioPlayer.HasAudio(wavStream))
                    audioPlayer.SetAudio(wavStream);
                
                audioPlayer.Play();
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while processing the PlayPreview command.\n\nDetails:{ex.Message}", $"PlayPreview failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        public RelayCommand PausePreviewCommand => new RelayCommand(PausePreview, IsStreamLoaded);
        private void PausePreview()
        {
#if !DEBUG
            try
#endif
            {
                audioPlayer.Pause();
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while processing the PausePreview command.\n\nDetails:{ex.Message}", $"PausePreview failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        public RelayCommand SkipPreviewCommand => new RelayCommand(SkipPreview, IsStreamLoaded);
        private void SkipPreview()
        {
            //If loop duration is longer than 5 seconds
            if((LoopEndMs - LoopStartMs) > 5000 && audioPlayer.HasAudio(wavStream))
            {
                audioPlayer.Seek((LoopEndMs / 1000) - 5f);
            }
        }

        public RelayCommand LoopStartIncreaseCommand => new RelayCommand(LoopStartIncrease);
        private void LoopStartIncrease()
        {
            if(LoopStartMs + 1 < LoopEndMs && LoopStartMs + 1 < TrackLengthMs)
                LoopStartMs++;
        }

        public RelayCommand LoopStartDecreaseCommand => new RelayCommand(LoopStartDecrease);
        private void LoopStartDecrease()
        {
            if (LoopStartMs - 1 >= 0)
                LoopStartMs--;
        }
        
        public RelayCommand LoopEndIncreaseCommand => new RelayCommand(LoopEndIncrease);
        private void LoopEndIncrease()
        {
            if (LoopEndMs + 1 <= TrackLengthMs)
                LoopEndMs++;
        }

        public RelayCommand LoopEndDecreaseCommand => new RelayCommand(LoopEndDecrease);
        private void LoopEndDecrease()
        {
            if (LoopEndMs - 1 > LoopStartMs)
                LoopEndMs--;
        }

        private bool CanApply()
        {
            return LoopEndMs > LoopStartMs || !LoopEnabled;
        }

        private bool IsStreamLoaded()
        {
            return wavStream != null;
        }
        
        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            audioPlayer.Stop();
            timer.Stop();
        }
    }
}
