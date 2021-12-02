using AudioCueEditor.Audio;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Xv2CoreLib.ACB_NEW;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for AddCueForm.xaml
    /// </summary>
    public partial class AddCueForm : MetroWindow, INotifyPropertyChanged
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

        public ObservableCollection<ReferenceType> SelectableReferenceTypes { get; private set; } = new ObservableCollection<ReferenceType>()
        {
            ReferenceType.Waveform,
            ReferenceType.Synth,
            ReferenceType.Sequence
        };


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
                    NotifyPropertyChanged(nameof(CanSetLoop));
                }
            }
        }

        private string _name = null;
        private ReferenceType _referenceType = ReferenceType.Sequence;
        private bool _addTrack = false;
        private bool _3dSound = true;

        public string CueName
        {
            get
            {
                return _name;
            }
            set
            {
                if(_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged("CueName");
                }
            }
        }
        public ReferenceType ReferenceType
        {
            get
            {
                return _referenceType;
            }
            set
            {
                if (_referenceType != value)
                {
                    _referenceType = value;
                    NotifyPropertyChanged("ReferenceType");
                }
            }
        }
        public bool AddTrack
        {
            get
            {
                return _addTrack;
            }
            set
            {
                if (_addTrack != value)
                {
                    _addTrack = value;
                    NotifyPropertyChanged("AddTrack");
                    NotifyPropertyChanged(nameof(CanSetLoop));
                }
            }
        }
        public bool Is3DSound
        {
            get
            {
                return _3dSound;
            }
            set
            {
                if (_3dSound != value)
                {
                    _3dSound = value;
                    NotifyPropertyChanged("Is3DSound");
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
                    NotifyPropertyChanged("Loop");
                }
            }
        }
        public bool CanSetLoop { get { return EncodeType == EncodeType.HCA && AddTrack; } }

        private string _audioFilePath = null;
        public string AudioFilePath
        {
            get
            {
                return _audioFilePath;
            }
            set
            {
                if (_audioFilePath != value)
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

        public AddCueForm(Window parent, string defaultCueNameOrPath = "", bool dropOperation = false)
        {
            CueName = defaultCueNameOrPath;
            InitializeComponent();
            DataContext = this;
            Owner = parent;

            if (dropOperation)
            {
                CueName = System.IO.Path.GetFileNameWithoutExtension(defaultCueNameOrPath);
                AudioFilePath = defaultCueNameOrPath;
                AddTrack = true;
                Done();
            }
        }

        public RelayCommand DoneCommand => new RelayCommand(Done);
        private async void Done()
        {
#if !DEBUG
            try
#endif
            {
                if (string.IsNullOrWhiteSpace(CueName))
                {
                    MessageBox.Show("Name is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (AddTrack)
                {
                    if (string.IsNullOrWhiteSpace(AudioFilePath) || !File.Exists(AudioFilePath))
                    {
                        MessageBox.Show("Path is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Task.Run(new Action(LoadAndConvertAudioFile));
                    Hide();
                    var controller = await ((MetroWindow)Owner).ShowProgressAsync("Encoding", "Encoding audio file...", true, DialogSettings.Default);
                    controller.SetIndeterminate();

                    while (TrackBytes == null && !ConversionSuccessful && ConvertException == null)
                    {
                        if (controller.IsCanceled)
                        {
                            await controller.CloseAsync();
                            Close();
                            return;
                        }
                        await Task.Delay(50);
                    }

                    await controller.CloseAsync();

                    if (!ConversionSuccessful)
                    {
                        await((MetroWindow)Owner).ShowMessageAsync("Encoding Error", string.Format("An exception occured during the encoding process:\n\n{0}", ConvertException?.Message), MessageDialogStyle.Affirmative, DialogSettings.Default);
                        Close();
                    }
                    else
                    {
                        Finished = true;
                        Close();
                    }
                }
                else
                {
                    Finished = true;
                    Close();
                }
                
            }
#if !DEBUG
            catch (Exception ex)
            {
                this.ShowMessageAsync($"Failed", $"An error occured while executing the command.\n\nDetails:{ex.Message}", MessageDialogStyle.Affirmative);
            }
#endif
        }

        private void AddTrack_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select audio file...";
            openFile.Filter = "Audio files | *.hca; *.adx; *.wav; *.mp3; *.wma; *.acc";

            if (openFile.ShowDialog(this) == true)
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
                TrackBytes = Helper.LoadAndConvertFile(AudioFilePath, Helper.GetFileType(EncodeType), Loop);
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
    }
}
