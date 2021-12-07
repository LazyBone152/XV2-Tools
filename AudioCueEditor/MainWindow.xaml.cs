using AudioCueEditor.Audio;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.ACB_NEW;
using MahApps.Metro.Controls.Dialogs;
using xv2Utils = Xv2CoreLib.Utils;
using Xv2CoreLib.Resource.UndoRedo;
using MahApps.Metro;
using Xv2CoreLib.AFS2;
using VGAudio.Cli;
using System.Runtime.ExceptionServices;
using Xv2CoreLib.Resource.App;
using AutoUpdater;
using System.Diagnostics;
using LB_Common.Utils;

namespace AudioCueEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
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
        
        private ACB_Wrapper _acbFile = null;
        public ACB_Wrapper AcbFile
        {
            get
            {
                return this._acbFile;
            }

            set
            {
                if (value != this._acbFile)
                {
                    this._acbFile = value;
                    NotifyPropertyChanged("AcbFile");
                    NotifyPropertyChanged("AcbPath");
                    NotifyPropertyChanged("AcbPathDisplay");
                }
            }
        }

        private string _acbPath = null;
        public string AcbPath
        {
            get
            {
                return _acbPath;
            }
            set
            {
                _acbPath = value;
                NotifyPropertyChanged("AcbPath");
                NotifyPropertyChanged("AcbPathDisplay");
            }
        }

        public string AcbPathDisplay
        {
            get
            {
                if (_acbFile == null) return "<No file loaded>";
                if (_acbPath == null) return "<No path set>";
                return _acbPath;
            }
        }

        public MainWindow()
        {
            //Tooltips
            ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            //Load settings
            SettingsManager.Instance.CurrentApp = Xv2CoreLib.Resource.App.Application.Ace;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;

            InitializeComponent();
            DataContext = this;
            InitTheme();
            LoadOnStartUp();


            //Check for updates silently
#if !DEBUG
            if (SettingsManager.Instance.Settings.UpdateNotifications)
            {
                CheckForUpdate(false);
            }
#endif

        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            InitTheme();
        }

        private async void CheckForUpdate(bool userInitiated)
        {
            //GitHub Settings
            Update.APP_TAG = "ACE";
            Update.GITHUB_ACCOUNT = "LazyBone152";
            Update.GITHUB_REPO = "ACE";
            Update.DEFAULT_APP_NAME = "ACE.exe";

            //Check for update
            object[] ret = await Update.CheckForUpdate();

            //Return values
            bool isUpdateAvailable = (bool)ret[0];
            Version latestVersion = (Version)ret[1];
            string changelog = (string)ret[2];
            int numUpdates = (int)ret[3];

            await Task.Delay(1000);

            if (isUpdateAvailable)
            {
                var messageResult = await this.ShowMessageAsync("Update Available", $"An update is available ({latestVersion}). Do you want to download and install it?\n\nNote: All instances of the application will be closed and any unsaved work will be lost.\n\nChangelog:\n{changelog}", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.ScrollDialog);

                if (messageResult == MessageDialogResult.Affirmative)
                {
                    var controller = await this.ShowProgressAsync("Update Available", "Downloading...", false, DialogSettings.Default);
                    controller.SetIndeterminate();

                    try
                    {
                        await Task.Run(() =>
                        {
                            Update.DownloadUpdate();
                        });
                    }
                    finally
                    {
                        await controller.CloseAsync();
                    }

                    if (Update.IsDownloadSuccessful)
                    {
                        Update.UpdateApplication();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Update Failed", Update.DownloadFailedText, MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);
                    }

                }
            }
            else if (userInitiated)
            {
                await this.ShowMessageAsync("Update", $"No update is available.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }


        public void InitTheme()
        {
            Dispatcher.Invoke((() =>
            {
                switch (SettingsManager.Instance.Settings.GetCurrentTheme())
                {
                    case Xv2CoreLib.Resource.App.AppTheme.Light:
                        ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(SettingsManager.Instance.Settings.CurrentLightAccent.ToString()), ThemeManager.GetAppTheme("BaseLight"));
                        break;
                    case Xv2CoreLib.Resource.App.AppTheme.Dark:
                        ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(SettingsManager.Instance.Settings.CurrentDarkAccent.ToString()), ThemeManager.GetAppTheme("BaseDark"));
                        break;
                }
            }));
        }
        private void LoadOnStartUp()
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (var arg in args)
            {
                if (System.IO.Path.GetExtension(arg) == ".acb" || System.IO.Path.GetExtension(arg) == ACB_File.MUSIC_PACKAGE_EXTENSION)
                {
                    LoadAcb(arg);
                    return;
                }
            }
        }

        public RelayCommand NewAcbCommand => new RelayCommand(NewAcb);
        private async void NewAcb()
        {
            if(AcbFile != null)
            {
                //Confirm
                var result = await this.ShowMessageAsync("Discard current file?", "An acb file is already open. Any unsaved changes will be lost if you continue.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result != MessageDialogResult.Affirmative) return;
            }
            
            AcbFile = ACB_Wrapper.NewXv2Acb();
            UndoManager.Instance.Clear();
            AcbPath = null;
        }

        public RelayCommand LoadAcbCommand => new RelayCommand(LoadAcb);
        private async void LoadAcb()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open ACB file...";
            //openFile.Filter = "ACB File | *.acb";
            openFile.Filter = string.Format("ACB File | *.acb; *{0}", ACB_File.MUSIC_PACKAGE_EXTENSION, ACB_File.MUSIC_PACKAGE_EXTENSION.ToUpper().Remove(0, 1));

            openFile.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(openFile.FileName))
            {
                LoadAcb(openFile.FileName);
            }
        }

        public RelayCommand ForceLoadAcbCommand => new RelayCommand(ForceLoadAcb);
        private async void ForceLoadAcb()
        {
            await this.ShowMessageAsync("Force Load", "Warning: Force loading disables most validation done on the ACB file when loading, such as checks about if columns exist or not. This can allow some ACBs to load that normally give errors but can also cause major issues!", MessageDialogStyle.Affirmative, DialogSettings.Default);

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open ACB file...";
            //openFile.Filter = "ACB File | *.acb";
            openFile.Filter = string.Format("ACB File | *.acb; *{0}", ACB_File.MUSIC_PACKAGE_EXTENSION, ACB_File.MUSIC_PACKAGE_EXTENSION.ToUpper().Remove(0, 1));

            openFile.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(openFile.FileName))
            {
                LoadAcb(openFile.FileName, true);
            }
        }


        private async void LoadAcb(string path, bool forceLoad = false)
        {
            AcbFormatHelper.Instance.AcbFormatHelperMain.ForceLoad = forceLoad;

            var controller = await this.ShowProgressAsync($"Loading \"{System.IO.Path.GetFileName(path)}\"...", $"", false, DialogSettings.Default);
            controller.SetIndeterminate();

            try
            {
                await Task.Run(() =>
                {
                    AcbFile = new ACB_Wrapper(ACB_File.Load(path));
                });

                UndoManager.Instance.Clear();

                if (AcbFile != null)
                {
                    AcbPath = path;

                    if (AcbFile.AcbFile.TableValidationFailed)
                    {
                        await this.ShowMessageAsync("Validation Failed", "This file contains unknown data that could not be loaded and was skipped. Continuing at this point may very likely result in this ACB losing data or becoming corrupt.\n\nYou've been warned!", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                    if (AcbFile.AcbFile.ExternalAwbError)
                    {
                        await this.ShowMessageAsync("Validation Failed", "There should be an external AWB file with this ACB, but none was found. Because of this some tracks could not be loaded.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                }
                
            }
            finally
            {
                controller.CloseAsync();
            }
        }

        public RelayCommand SaveAcbCommand => new RelayCommand(SaveAcb, CanSaveAcb);
        private void SaveAcb()
        {
            Save(AcbPath);
        }

        public RelayCommand SaveAsAcbCommand => new RelayCommand(SaveAsAcb, IsAcbLoaded);
        private void SaveAsAcb()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save ACB file...";
            //saveDialog.Filter = "ACB File | *.acb";
            saveDialog.Filter = string.Format("ACB File | *.acb; |{1} File |*{0}", ACB_File.MUSIC_PACKAGE_EXTENSION, ACB_File.MUSIC_PACKAGE_EXTENSION.ToUpper().Remove(0, 1));
            
            saveDialog.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(saveDialog.FileName))
            {
                if (System.IO.Path.GetExtension(saveDialog.FileName) == ACB_File.MUSIC_PACKAGE_EXTENSION)
                    AcbFile.AcbFile.SaveFormat = SaveFormat.MusicPackage;
                
                Save(saveDialog.FileName);

                AcbPath = saveDialog.FileName;
            }
        }

        private async void Save(string path)
        {
            var controller = await this.ShowProgressAsync($"Saving \"{System.IO.Path.GetFileName(path)}\"...", $"", false, DialogSettings.Default);
            controller.SetIndeterminate();

            Task task = null;

            try
            {
                task = Task.Run(() =>
                {
                    AcbFile.AcbFile.Save(xv2Utils.GetPathWithoutExtension(path));
                });

                await task;
                await this.ShowMessageAsync("Save successful", "The acb file was successfully saved!", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
            finally
            {
                await controller.CloseAsync();
            }

        }

        public RelayCommand SettingsCommand => new RelayCommand(OpenSettings);
        private async void OpenSettings()
        {
            string originalGameDir = SettingsManager.settings.GameDirectory;

            Forms.Settings settingsForm = new Forms.Settings(this);
            settingsForm.ShowDialog();
            SettingsManager.Instance.SaveSettings();
            InitTheme();

            if (SettingsManager.settings.GameDirectory != originalGameDir && SettingsManager.settings.ValidGameDir)
            {
                //placeholder for whenever loading from game is added
            }
            
        }

        public RelayCommand ExitCommand => new RelayCommand(Exit);
        private async void Exit()
        {
            if(AcbFile != null)
            {
                var result = await this.ShowMessageAsync("Exit?", "An ACB file is open. Any unsaved changes will be lost if you continue.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result != MessageDialogResult.Affirmative) return;
            }

            Environment.Exit(0);
        }

        public RelayCommand SetAcbFileAssociationCommand => new RelayCommand(SetAcbFileAssociation);
        private async void SetAcbFileAssociation()
        {
            if (MessageBox.Show(String.Format("This will associate the .acb extension with Audio Cue Editor and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileAssociations.ACE_EnsureAssociationsSetForAcb();
                MessageBox.Show(".acb extension successfully associated!\n\nNote: If for some reason it did not work then you may need to go into the properties and change the default program manually.", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public RelayCommand SetMusicPackageAssociationCommand => new RelayCommand(SetMusicPackageAssociation);
        private async void SetMusicPackageAssociation()
        {
            if (MessageBox.Show(String.Format("This will associate the {1} extension with Audio Cue Editor and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location, ACB_File.MUSIC_PACKAGE_EXTENSION.ToLower()), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileAssociations.ACE_EnsureAssociationsSetForAcb();
                MessageBox.Show($"{ACB_File.MUSIC_PACKAGE_EXTENSION.ToLower()} extension successfully associated!\n\nNote: If for some reason it did not work then you may need to go into the properties and change the default program manually.", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        public RelayCommand ExtractAllTracksRawCommand => new RelayCommand(ExtractAllTracksRaw, IsAcbLoaded);
        private async void ExtractAllTracksRaw()
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (_browser.ShowDialog() == true && Directory.Exists(_browser.SelectedPath))
            {
                ExtractAllTracks(_browser.SelectedPath, false);
            }
        }

        public RelayCommand ExtractAllTracksWavCommand => new RelayCommand(ExtractAllTracksWav, IsAcbLoaded);
        private async void ExtractAllTracksWav()
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (_browser.ShowDialog() == true && Directory.Exists(_browser.SelectedPath))
            {
                ExtractAllTracks(_browser.SelectedPath, true);
            }
        }

        public RelayCommand CreateMusicInstallerCommand => new RelayCommand(CreateMusicInstaller, IsAcbLoaded);
        private async void CreateMusicInstaller()
        {
            if(AcbFile.AcbFile.MusicPackageType == MusicPackageType.CSS_Voice)
            {
                MessageBox.Show("This option is only for music installers.", "NO", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var form = new View.CreateMusicInstaller(this, AcbFile.AcbFile);
            form.ShowDialog();

            if (form.Success)
                MessageBox.Show($"Installer successfully created at \"{form.InstallInfoPath}\".\n\nThis file must be paired with an LB Mod Installer (v3.4 or greater) executable to be usable, which can be found at the same place as this tool was downloaded from. The executable should be renamed to match the installinfo file.", "Installer Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public RelayCommand RandomizeCueIDsCommand => new RelayCommand(RandomizeCueIDs, IsAcbLoaded);
        private async void RandomizeCueIDs()
        {
            AcbFile.UndoableRandomizeCueIds();
        }

        public RelayCommand FixSilentCuesCommand => new RelayCommand(FixSilentCues, CanFixSilentCues);
        private async void FixSilentCues()
        {
            if (AcbFile != null)
            {
                //Confirm
                var result = await this.ShowMessageAsync("Fix Silent Cues", "This tool is a fixer for old modded ACBs that have become broken with the 1.16 update of DBXV2, with all added tracks now being silent.\n\nFix the ACB?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result != MessageDialogResult.Affirmative) return;

                var undos = AcbFile.AcbFile.AddVolumeBusToCues();
                UndoManager.Instance.AddCompositeUndo(undos, "Fix Silent Cues");
            }
        }

        private bool CanFixSilentCues()
        {
            if (!IsAcbLoaded()) return false;
            return AcbFile.AcbFile.Version >= ACB_File.VolumeBusRequiredVersion;
        }

        private bool IsAcbLoaded()
        {
            return AcbFile != null;
        }

        private bool CanSaveAcb()
        {
            if (IsAcbLoaded() == false) return false;
            return !string.IsNullOrWhiteSpace(AcbPath);
        }
        
        private async Task ExtractAllTracks(string extractDirectory, bool convertToWav)
        {
            if (!IsAcbLoaded()) return;
            List<AFS2_Entry> extractedAwbEntries = new List<AFS2_Entry>();

            var controller = await this.ShowProgressAsync("Extracting all tracks (this may take a while)", "", true, DialogSettings.Default);
            controller.Maximum =  AcbFile.AcbFile.Cues.Count;

            try
            {
                await Task.Run(() =>
                {
                    int progress = 0;
                    foreach (var cue in AcbFile.AcbFile.Cues)
                    {
                        if (controller.IsCanceled)
                            break;

                        controller.SetMessage($"Extracting \"{cue.Name}\" (Cue ID: {cue.ID})...");
                        var waveforms = AcbFile.AcbFile.GetWaveformsFromCue(cue);

                        foreach (var waveform in waveforms)
                        {
                            if (waveform.AwbId != ushort.MaxValue)
                            {
                                var awbEntry = AcbFile.AcbFile.GetAfs2Entry(waveform.AwbId);

                                if (!extractedAwbEntries.Contains(awbEntry))
                                {
                                    string trackName = $"{cue.ID}_{cue.Name}_({waveform.AwbId})";
                                    byte[] trackBytes;

                                    if (convertToWav)
                                    {
                                        using (MemoryStream stream = new MemoryStream(awbEntry.bytes))
                                        {
                                            trackBytes = ConvertStream.ConvertFile(new Options(), stream, Helper.GetFileType(waveform.EncodeType), FileType.Wave);
                                        }

                                        File.WriteAllBytes($"{extractDirectory}/{trackName}.wav", trackBytes);
                                    }
                                    else
                                    {
                                        File.WriteAllBytes($"{extractDirectory}/{trackName}.{waveform.EncodeType}", awbEntry.bytes);
                                    }
                                    extractedAwbEntries.Add(awbEntry);
                                }
                            }
                        }

                        progress++;
                        controller.SetProgress(progress);
                    }
                });
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                await controller.CloseAsync();
            }
        }

        private void Help_CheckForUpdates(object sender, RoutedEventArgs e)
        {
            CheckForUpdate(true);
        }

        private void Help_GitHub(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://github.com/LazyBone152/ACE");
        }
    }
}
