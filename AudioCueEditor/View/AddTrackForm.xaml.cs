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
using Xv2CoreLib.ACB;
using AudioCueEditor.Data;
using Xv2CoreLib.Resource;

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

        public List<EncodeType> EncodeTypes { get; private set; } = Helper.SupportedEncodeTypes;

        private EncodeType _encodeType = EncodeType.HCA;
        public EncodeType EncodeType
        {
            get
            {
                return _encodeType;
            }
            set
            {
                if (_encodeType != value)
                {
                    _encodeType = value;
                    NotifyPropertyChanged(nameof(EncodeType));
                }
            }
        }

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
                    NotifyPropertyChanged(nameof(Loop));
                    NotifyPropertyChanged(nameof(CanSetLoop));
                }
            }
        }
        public bool CanSetLoop { get { return EncodeType == EncodeType.HCA; } }

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
        
        public byte[] TrackBytes { get; private set; }
        private bool ConversionSuccessful = false;
        private Exception ConvertException = null;

        public bool IsDone { get; private set; } 
        public bool Finished { get; private set; }

        public AsyncObservableCollection<HcaEncryptionKey> HcaKeys { get; set; } = HcaEncryptionKeysManager.Instance.GetReadOnlyViewEncryptionKeys();
        public HcaEncryptionKey SelectedHcaKey { get; set; }


        public AddTrackForm(Window parent, ACB_File acbFile)
        {
            InitializeComponent();
            DataContext = this;
            Owner = parent;
            AutoSetEncryptionKey(acbFile.TryGetEncrpytionKey());
            AutoSetValues(acbFile);
        }
        
        public AddTrackForm(Window parent, string path, ACB_File acbFile)
        {
            AudioFilePath = path;
            InitializeComponent();
            DataContext = this;
            Owner = parent;
            AutoSetEncryptionKey(acbFile.TryGetEncrpytionKey());
            AutoSetValues(acbFile);
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
            var controller = await ((MetroWindow)Owner).ShowProgressAsync("Encoding", "Encoding audio file...", true, DialogSettings.Default);
            controller.SetIndeterminate();

            while(TrackBytes == null && !ConversionSuccessful && ConvertException == null)
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
                await ((MetroWindow)Owner).ShowMessageAsync("Encoding Error", string.Format("An exception occured during the encoding process:\n\n{0}", ConvertException?.Message), MessageDialogStyle.Affirmative, DialogSettings.Default);
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
            openFile.Filter = "Audio files | *.hca; *.adx; *.wav; *.mp3; *.wma; *.aac";
            
            if(openFile.ShowDialog(this) == true)
            {
                AudioFilePath = openFile.FileName;

                if (System.IO.Path.GetExtension(openFile.FileName) == ".adx")
                    EncodeType = EncodeType.ADX;
            }
        }

        private void LoadAndConvertAudioFile()
        {
            try
            {
                ulong key = SelectedHcaKey != null && EncodeType == EncodeType.HCA ? SelectedHcaKey.Key : 0;

                TrackBytes = Helper.LoadAndConvertFile(AudioFilePath, Helper.GetFileType(EncodeType), Loop, key);
                ConversionSuccessful = true;
                
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

        private void AutoSetEncryptionKey(ulong key)
        {
            SelectedHcaKey = HcaKeys.FirstOrDefault(x => x.Key == key);

            if (SelectedHcaKey == null)
            {
                if (key != 0)
                {
                    //Key doesnt exist in global key list, so we add it and save
                    HcaEncryptionKeysManager.Instance.AddKey($"Auto Generated Key {HcaEncryptionKeysManager.Instance.EncryptionKeys.Keys.Count + 1}", key);
                    HcaEncryptionKeysManager.Instance.Save();

                    HcaKeys = HcaEncryptionKeysManager.Instance.GetReadOnlyViewEncryptionKeys();
                    SelectedHcaKey = HcaKeys.FirstOrDefault(x => x.Key == key);
                }
                else
                {
                    SelectedHcaKey = HcaKeys[0];
                }
            }
        }

        private void AutoSetValues(ACB_File acbFile)
        {
            Streaming = acbFile.IsStreamingAcb();
        }
    }
}
