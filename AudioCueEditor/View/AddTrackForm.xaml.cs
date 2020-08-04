using AudioCueEditor.Audio;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for AddTrackForm.xaml
    /// </summary>
    public partial class AddTrackForm : MetroWindow, INotifyPropertyChanged
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

        private bool _streaming = false;
        public bool Streaming
        {
            get
            {
                return _streaming;
            }
            set
            {
                if (_streaming != value)
                {
                    _streaming = value;
                    NotifyPropertyChanged("Streaming");
                }
            }
        }

        private bool _loop = false;
        public bool Loop
        {
            get
            {
                return _loop;
            }
            set
            {
                if (_loop != value)
                {
                    _loop = value;
                    NotifyPropertyChanged("Loop");
                }
            }
        }

        private string _audioFilePath = null;
        public string AudioFilePath
        {
            get
            {
                return _audioFilePath;
            }
            set
            {
                if(_audioFilePath != value)
                {
                    _audioFilePath = value;
                    NotifyPropertyChanged("AudioFilePath");
                }
            }
        }
        
        public byte[] HcaBytes { get; private set; }
        private bool ConversionSuccessful = false;
        private Exception ConvertException = null;

        public bool IsDone { get; private set; } 
        public bool Finished { get; private set; }

        public AddTrackForm(Window parent)
        {
            InitializeComponent();
            DataContext = this;
            Owner = parent;
        }
        
        public AddTrackForm(Window parent, string path)
        {
            AudioFilePath = path;
            InitializeComponent();
            DataContext = this;
            Owner = parent;
            Done();
        }


        public RelayCommand DoneCommand => new RelayCommand(Done, ()=> File.Exists(AudioFilePath));
        private async void Done()
        {
            //Close
            //Show progress bar
            //If error, show error dialog, reopen window
            Task.Run(new Action(LoadAndConvertAudioFile));
            Hide();
            var controller = await ((MetroWindow)Owner).ShowProgressAsync("Encoding", "Encoding audio file...", true, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false });
            controller.SetIndeterminate();

            while(HcaBytes == null && !ConversionSuccessful && ConvertException == null)
            {
                if(controller.IsCanceled)
                {
                    await controller.CloseAsync();
                    Close();
                    return;
                }
                await Task.Delay(50);
            }

            await controller.CloseAsync();

            if(!ConversionSuccessful)
            {
                await ((MetroWindow)Owner).ShowMessageAsync("Encoding Error", string.Format("An exception occured during the encoding process:\n\n{0}", ConvertException?.Message), MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false });
                Close();
            }
            else
            {
                Finished = true;
                Close();
            }
            
        }


        private void AddTrack_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select audio file...";
            openFile.Filter = "Audio files | *.hca; *.wav; *.mp3; *.wma; *.aac";
            
            if(openFile.ShowDialog(this) == true)
            {
                AudioFilePath = openFile.FileName;
            }
        }

        private void LoadAndConvertAudioFile()
        {
            try
            {
                
                if (File.Exists(AudioFilePath))
                {
                    byte[] bytes = File.ReadAllBytes(AudioFilePath);

                    if (System.IO.Path.GetExtension(AudioFilePath).ToLower() == ".hca")
                    {
                        HcaBytes = bytes;
                        ConversionSuccessful = true;
                    }
                    else if (System.IO.Path.GetExtension(AudioFilePath).ToLower() == ".wav")
                    {
                        var hcaBytes = HCA.Encode(bytes);
                        hcaBytes = HCA.EncodeLoop(hcaBytes, Loop);

                        HcaBytes = hcaBytes;
                        ConversionSuccessful = true;
                    }
                    else if (System.IO.Path.GetExtension(AudioFilePath).ToLower() == ".mp3" ||
                             System.IO.Path.GetExtension(AudioFilePath).ToLower() == ".wma" ||
                             System.IO.Path.GetExtension(AudioFilePath).ToLower() == ".aac")
                    {
                        var wavBytes = Audio.CommonFormatsConverter.ConvertToWav(AudioFilePath);
                        var hcaBytes = HCA.Encode(wavBytes);
                        hcaBytes = HCA.EncodeLoop(hcaBytes, Loop);

                        HcaBytes = hcaBytes;
                        ConversionSuccessful = true;
                    }
                    else
                    {
                        ConversionSuccessful = false;
                        throw new InvalidDataException($"The selected audio file is not a supported format ({System.IO.Path.GetExtension(AudioFilePath)}.");
                    }
                }
            }
            catch (Exception ex)
            {
                ConversionSuccessful = false;
                ConvertException = ex;
            }
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            IsDone = true;
        }
    }
}
