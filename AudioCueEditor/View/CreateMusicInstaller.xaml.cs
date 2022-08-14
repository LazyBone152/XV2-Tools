using MahApps.Metro.Controls;
using Microsoft.Win32;
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
using System.Windows.Shapes;
using Xv2CoreLib.ACB;
using Xv2CoreLib.Resource.UndoRedo;
using LB_Mod_Installer.Installer;
using System.IO.Compression;
using System.IO;
using Xv2CoreLib.Resource;
using YAXLib;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for CreateMusicInstaller.xaml
    /// </summary>
    public partial class CreateMusicInstaller : MetroWindow, INotifyPropertyChanged
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

        private string _modName = string.Empty;
        private string _modAuthor = string.Empty;
        private string _modVersion = "1.0.0.0";
        private string _modDescription = string.Empty;
        private int musicPackageType = 0;

        public string ModName { get { return _modName; } set { _modName = value; NotifyPropertyChanged("ModName"); } }
        public string ModAuthor { get { return _modAuthor; } set { _modAuthor = value; NotifyPropertyChanged("ModAuthor"); } }
        public string ModVersion { get { return _modVersion; } set { _modVersion = value; NotifyPropertyChanged("ModVersion"); } }
        public string ModDescription { get { return _modDescription; } set { _modDescription = value; NotifyPropertyChanged("ModDescription"); } }
        public int MusicPackageType { get { return musicPackageType; } set { musicPackageType = value; NotifyPropertyChanged("MusicPackageType"); } }


        private ACB_File acbFile;
        public bool Success { get; set; }
        public string InstallInfoPath { get; set; }

        public CreateMusicInstaller(Window parent, ACB_File musicPackage)
        {
            DataContext = this;
            Owner = parent;
            acbFile = musicPackage;
            MusicPackageType = (int)acbFile.AudioPackageType;
            
            InitializeComponent();
        }

        private void Done()
        {
            string savePath = GetSavePath();
            if (string.IsNullOrWhiteSpace(savePath)) return;
            
            //Change acbFile saveFormat if needed (undoable action)
            if (acbFile.SaveFormat != SaveFormat.AudioPackage)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ACB_File>(nameof(acbFile.SaveFormat), acbFile, acbFile.SaveFormat, SaveFormat.AudioPackage, "Save Format"));
                acbFile.SaveFormat = SaveFormat.AudioPackage;
            }

            if((int)acbFile.AudioPackageType != (MusicPackageType))
            {
                acbFile.AudioPackageType = (AudioPackageType)MusicPackageType;
            }

            //Create MusicPackage
            byte[] audioPackageBytes = acbFile.SaveAudioPackageToBytes();
            string audioPackagePath = $"CAR_BGM{ACB_File.AUDIO_PACKAGE_EXTENSION}";

            //Create InstallerXml
            InstallerXml installerXml = new InstallerXml();
            installerXml.Name = ModName;
            installerXml.Author = ModAuthor;
            installerXml.VersionString = ModVersion;
            installerXml.InstallOptionSteps = new List<InstallStep>();
            installerXml.InstallFiles = new List<FilePath>();

            if (!string.IsNullOrWhiteSpace(ModDescription))
            {
                InstallStep installStep = new InstallStep();
                installStep.Name = "Info";
                installStep.Message = ModDescription;
                installStep.StepType = InstallStep.StepTypes.Message;
                installerXml.InstallOptionSteps.Add(installStep);
            }

            FilePath file = new FilePath();
            file.SourcePath = audioPackagePath;
            installerXml.InstallFiles.Add(file);


            YAXSerializer serializer = new YAXSerializer(typeof(InstallerXml));
            string strXml = serializer.Serialize(installerXml);
            byte[] xmlBytes = Encoding.UTF8.GetBytes(strXml);

            //Create installinfo zip
            using (var stream = File.Create(savePath))
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    ZipWriter zipWriter = new ZipWriter(zip);
                    zipWriter.AddFile("InstallerXml.xml", xmlBytes, CompressionLevel.Optimal, true);
                    zipWriter.AddFile("data/" + audioPackagePath, audioPackageBytes, CompressionLevel.Optimal, true);
                }
            }

            Success = true;
            InstallInfoPath = savePath;
            Close();
        }

        private string GetSavePath()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save INSTALLINFO file...";
            saveDialog.Filter = "INSTALLINFO File | *.installinfo";
            saveDialog.AddExtension = true;
            saveDialog.ShowDialog(this);
            return saveDialog.FileName;
        }

        private bool CanDo()
        {
            if(ModVersion.Where(x => x == '.').Count() != 3)
            {
                MessageBox.Show("Version format not valid.\n\nMust have four numbers separated by 3 periods (e.g. 1.0.0.0).", "Invalid Version", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ModAuthor))
            {
                MessageBox.Show("Author cannot be empty.", "Invalid Author", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ModName))
            {
                MessageBox.Show("Name cannot be empty.", "Invalid Author", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (CanDo()) Done();
        }
    }
}
