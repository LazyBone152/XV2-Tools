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
using Xv2CoreLib.ACB;
using MahApps.Metro.Controls.Dialogs;
using xv2Utils = Xv2CoreLib.Utils;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.AFS2;
using VGAudio.Cli;
using System.Runtime.ExceptionServices;
using Xv2CoreLib.Resource.App;
using AutoUpdater;
using System.Diagnostics;
using LB_Common.Utils;
using ControlzEx.Theming;
using System.Linq;
using System.Globalization;

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
                    NotifyPropertyChanged(nameof(AcbFile));
                    NotifyPropertyChanged(nameof(AcbPath));
                    NotifyPropertyChanged(nameof(AcbPathDisplay));
                }
            }
        }

        public bool IsAcbFileLoaded => AcbFile != null ? !AcbFile.IsAwbWrapper : false;
        public bool IsAwbFileLoaded => AcbFile != null ? AcbFile.IsAwbWrapper : false;

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
                NotifyPropertyChanged(nameof(AcbPath));
                NotifyPropertyChanged(nameof(AcbPathDisplay));
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
            //Force en-US culture accross whole application to ensure error messages will always be in english
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

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

            //Update title
            Title += $" ({SettingsManager.Instance.CurrentVersionString})";

            //Check for updates silently
#if !DEBUG
            CheckForUpdate(false);
#endif

        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            InitTheme();
        }

        private async Task CheckForUpdate(bool userInitiated)
        {
            //Check for update
            AppUpdate appUpdate = default;

            await Task.Run(() =>
            {
                appUpdate = Update.CheckForUpdate(AutoUpdater.App.ACE);
            });

            await Task.Delay(1000);

            if (Update.UpdateState == UpdateState.XmlDownloadFailed && userInitiated)
            {
                await this.ShowMessageAsync("Update Failed", "The AppUpdate XML file failed to download.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (Update.UpdateState == UpdateState.XmlParseFailed && userInitiated)
            {
                await this.ShowMessageAsync("Update Failed", $"The AppUpdate XML file could not be parsed.\n\n{Update.FailedErrorMessage}", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            if (!appUpdate.ForceUpdate && !SettingsManager.settings.UpdateNotifications && !userInitiated)
            {
                return;
            }

            if (appUpdate.HasUpdate)
            {
                MetroDialogSettings dialogSettings = DialogSettings.ScrollDialog;
                dialogSettings.FirstAuxiliaryButtonText = "Ignore";
                dialogSettings.AffirmativeButtonText = "Update";
                dialogSettings.NegativeButtonText = "Open in Browser";
                dialogSettings.DefaultButtonFocus = MessageDialogResult.Affirmative;

                MessageDialogResult messageResult = await this.ShowMessageAsync("Update Available", $"An update is available ({appUpdate.Version}). The application can automatically download and update itself (confirmation may be required), or you may also open the website in a browser and download the update manually. \n\nNote: All instances of the application will be closed and any unsaved work will be lost if Update is selected.\n\nChangelog:\n{appUpdate.Changelog}", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);

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

                    if (Update.UpdateState == UpdateState.DownloadSuccess)
                    {
                        Update.UpdateApplication();
                    }
                    else if (Update.UpdateState == UpdateState.DownloadFail)
                    {
                        await this.ShowMessageAsync("Download Failed", Update.FailedErrorMessage, MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }

                }
                else if (messageResult == MessageDialogResult.Negative)
                {
                    Process.Start("https://github.com/LazyBone152/ACE/releases");
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
                ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, SettingsManager.Instance.GetTheme());
            }));
        }

        private void LoadOnStartUp()
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (var arg in args)
            {
                if (Path.GetExtension(arg).Equals(".acb", StringComparison.OrdinalIgnoreCase) || 
                    Path.GetExtension(arg).Equals(ACB_File.AUDIO_PACKAGE_EXTENSION, StringComparison.OrdinalIgnoreCase) || 
                    Path.GetExtension(arg).Equals(ACB_File.AUDIO_PACKAGE_EXTENSION_OLD, StringComparison.OrdinalIgnoreCase))
                {
                    LoadAcb(arg);
                    return;
                }
                else if(Path.GetExtension(arg).Equals(".awb", StringComparison.OrdinalIgnoreCase))
                {
                    LoadAwb(arg);
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
            cueEditor.UpdateCueDataGridVisibilities();
        }

        public RelayCommand LoadAcbCommand => new RelayCommand(LoadAcb);
        private async void LoadAcb()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open ACB file...";
            openFile.Filter = string.Format("ACB File | *.acb; *{0}; *{1}", ACB_File.AUDIO_PACKAGE_EXTENSION, ACB_File.AUDIO_PACKAGE_EXTENSION_OLD);

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
            openFile.Filter = string.Format("ACB File | *.acb; *{0}; *{1}", ACB_File.AUDIO_PACKAGE_EXTENSION, ACB_File.AUDIO_PACKAGE_EXTENSION_OLD);

            openFile.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(openFile.FileName))
            {
                LoadAcb(openFile.FileName, true);
            }
        }

        public RelayCommand LoadAwbCommand => new RelayCommand(LoadAwb);
        private async void LoadAwb()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open AWB file...";
            openFile.Filter = string.Format("AWB File | *.awb; *.acb");

            openFile.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(openFile.FileName))
            {
                bool isInternalAwb = Path.GetExtension(openFile.FileName) == ".acb";
                LoadAwb(openFile.FileName, isInternalAwb);
            }
        }

        private async void LoadAwb(string path, bool isInternalAwb = false)
        {
            //Check for a matching ACB and offer to load that, if it exists
            string acbPath = string.Format("{0}/{1}.acb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

            if (File.Exists(acbPath) && !isInternalAwb)
            {
                var ret = await this.ShowMessageAsync("ACB Found", "A matching ACB was found for this AWB. Do you want to load that instead?\n\nEditing is very limited when loading just the AWB. To unlock all the editing features of ACE, an ACB is required.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.DefaultYesNo);

                if(ret == MessageDialogResult.Affirmative)
                {
                    LoadAcb(acbPath);
                    return;
                }
            }

            var controller = await this.ShowProgressAsync($"Loading \"{Path.GetFileName(path)}\"...", $"", false, DialogSettings.Default);
            controller.SetIndeterminate();

            try
            {
                AWB_Wrapper awbFile = null;

                await Task.Run(() =>
                {
                    awbFile = new AWB_Wrapper(path);
                });

                if (awbFile != null)
                {
                    if (awbFile.Type == AwbType.CpkUnsupported)
                    {
                        await this.ShowMessageAsync("Unsupported AWB", "This is an older, unsupported type of AWB file (CPK based). Load failed.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return;
                    }

                    if (awbFile.Type == AwbType.None)
                    {
                        await this.ShowMessageAsync("Nothing Found", "No internal AWB file was found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return;
                    }

                    AcbPath = path;
                    AcbFile = new ACB_Wrapper(awbFile);
                    UndoManager.Instance.Clear();
                }
            }
            finally
            {
                await controller.CloseAsync();
            }
        }

        private async void LoadAcb(string path, bool forceLoad = false)
        {
            AcbFormatHelper.Instance.AcbFormatHelperMain.ForceLoad = forceLoad;

            var controller = await this.ShowProgressAsync($"Loading \"{System.IO.Path.GetFileName(path)}\"...", $"", false, DialogSettings.Create(14));
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
                await controller.CloseAsync();
            }

            cueEditor.UpdateCueDataGridVisibilities();
        }

        public RelayCommand SaveAcbCommand => new RelayCommand(SaveAcb, CanSave);
        private void SaveAcb()
        {
            Save(AcbPath);
        }

        public RelayCommand SaveAsAcbCommand => new RelayCommand(SaveAsAcb, IsAcbLoaded);
        private void SaveAsAcb()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save ACB file...";
            saveDialog.Filter = string.Format("ACB File | *.acb; |AudioPackage |*{0}", ACB_File.AUDIO_PACKAGE_EXTENSION);
            
            saveDialog.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(saveDialog.FileName))
            {
                if (Path.GetExtension(saveDialog.FileName) == ACB_File.AUDIO_PACKAGE_EXTENSION)
                    AcbFile.AcbFile.SaveFormat = SaveFormat.AudioPackage;
                
                Save(saveDialog.FileName);

                AcbPath = saveDialog.FileName;
            }
        }

        private async void Save(string path)
        {
            if(AcbFile?.AcbFile?.SaveFormat == SaveFormat.AudioPackage)
            {
                bool validate = await AudioPackageValidation();

                if (!validate)
                    return;
            }

            var controller = await this.ShowProgressAsync($"Saving \"{Path.GetFileName(path)}\"...", $"", false, DialogSettings.Create(14));
            controller.SetIndeterminate();

            Task task = null;

            try
            {
                task = Task.Run(() =>
                {
                    if (IsAwbFileLoaded)
                    {
                        AcbFile.AwbWrapper.Save();
                    }
                    else
                    {
                        //Is ACB
                        AcbFile.AcbFile.Save(xv2Utils.GetPathWithoutExtension(path));
                    }
                });

                await task;
                //await this.ShowMessageAsync("Save successful", "The acb file was successfully saved!", MessageDialogStyle.Affirmative, DialogSettings.Default);
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
        private void SetAcbFileAssociation()
        {
            if (MessageBox.Show(String.Format("This will associate the .acb extension with Audio Cue Editor and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileAssociations.ACE_EnsureAssociationsSetForAcb();
                MessageBox.Show(".acb extension successfully associated!\n\nNote: If for some reason it did not work then you may need to go into the properties and change the default program manually.", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public RelayCommand SetAwbFileAssociationCommand => new RelayCommand(SetAwbFileAssociation);
        private void SetAwbFileAssociation()
        {
            if (MessageBox.Show(String.Format("This will associate the .awb extension with Audio Cue Editor and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileAssociations.ACE_EnsureAssociationsSetForAwb();
                MessageBox.Show(".awb extension successfully associated!\n\nNote: If for some reason it did not work then you may need to go into the properties and change the default program manually.", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public RelayCommand SetAudioPackageAssociationCommand => new RelayCommand(SetAudioPackageAssociation);
        private void SetAudioPackageAssociation()
        {
            if (MessageBox.Show(String.Format("This will associate the {1} extension with Audio Cue Editor and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location, ACB_File.AUDIO_PACKAGE_EXTENSION.ToLower()), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileAssociations.ACE_EnsureAssociationsSetForAudioPackage();
                MessageBox.Show($"{ACB_File.AUDIO_PACKAGE_EXTENSION.ToLower()} extension successfully associated!\n\nNote: If for some reason it did not work then you may need to go into the properties and change the default program manually.", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        public RelayCommand ExtractAllTracksRawCommand => new RelayCommand(ExtractAllTracksRaw, IsAcbOrAwbLoaded);
        private async void ExtractAllTracksRaw()
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (_browser.ShowDialog() == true && Directory.Exists(_browser.SelectedPath))
            {
                ExtractAllTracks(_browser.SelectedPath, false);
            }
        }

        public RelayCommand ExtractAllTracksWavCommand => new RelayCommand(ExtractAllTracksWav, IsAcbOrAwbLoaded);
        private async void ExtractAllTracksWav()
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (_browser.ShowDialog() == true && Directory.Exists(_browser.SelectedPath))
            {
                ExtractAllTracks(_browser.SelectedPath, true);
            }
        }

        public RelayCommand CreateAudioInstallerCommand => new RelayCommand(CreateAudioInstaller, IsAcbLoaded);
        private async void CreateAudioInstaller()
        {
            if(AcbFile.AcbFile.AudioPackageType == AudioPackageType.AutoVoice)
            {
                MessageBox.Show("This option is only for music installers.", "NO", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var form = new View.CreateMusicInstaller(this, AcbFile.AcbFile);
            form.ShowDialog();

            if (form.Success)
                MessageBox.Show($"Installer successfully created at \"{form.InstallInfoPath}\".\n\nThis file must be paired with an LB Mod Installer (v3.4 or greater) executable to be usable, which can be found at the same place as this tool was downloaded from. The executable should be renamed to match the installinfo file.", "Installer Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        public RelayCommand EncryptKeysHcaCommand => new RelayCommand(EncryptKeysHca);
        private void EncryptKeysHca()
        {
            ulong key = IsAcbLoaded() ? AcbFile.AcbFile.TryGetEncrpytionKey() : 0;

            Forms.HcaEncryptionKeysEditor form = new Forms.HcaEncryptionKeysEditor(key, this);
            form.ShowDialog();
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
            return IsAcbFileLoaded;
        }

        private bool IsAcbOrAwbLoaded()
        {
            return IsAcbFileLoaded || IsAwbFileLoaded;
        }

        private bool CanSave()
        {
            if (IsAcbFileLoaded == false && IsAwbFileLoaded == false) return false;
            return !string.IsNullOrWhiteSpace(AcbPath);
        }
        
        private async Task ExtractAllTracks(string extractDirectory, bool convertToWav)
        {
            if (!IsAcbOrAwbLoaded()) return;
            List<AFS2_Entry> extractedAwbEntries = new List<AFS2_Entry>();

            var controller = await this.ShowProgressAsync("Extract All", "This may take a while.", true, DialogSettings.Default);
            controller.Maximum = IsAcbFileLoaded ? AcbFile.AcbFile.Cues.Count : AcbFile.AwbWrapper.AwbFile.Entries.Count;

            try
            {
                await Task.Run(() =>
                {
                    int progress = 0;

                    if (IsAcbFileLoaded)
                    {
                        foreach (var cue in AcbFile.AcbFile.Cues)
                        {
                            if (controller.IsCanceled)
                                break;

                            controller.SetMessage($"Extracting \"{cue.Name}\" (Cue ID: {cue.ID})...\n\nThis may take a while.");
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
                                            File.WriteAllBytes($"{extractDirectory}/{trackName}.{waveform.EncodeType.ToString().ToLower()}", awbEntry.bytes);
                                        }
                                        extractedAwbEntries.Add(awbEntry);
                                    }
                                }
                            }

                            progress++;
                            controller.SetProgress(progress);
                        }

                    }
                    else if (IsAwbFileLoaded)
                    {
                        foreach(var awbEntry in AcbFile.AwbWrapper.AwbFile.Entries)
                        {
                            if (controller.IsCanceled)
                                break;

                            controller.SetMessage($"Extracting \"{awbEntry.ID}\"...\n\nThis may take a while.");

                            string trackName = $"{awbEntry.ID}";
                            byte[] trackBytes = awbEntry.bytes;

                            if (convertToWav && awbEntry.HcaInfo.EncodeType != EncodeType.None)
                            {
                                using (MemoryStream stream = new MemoryStream(awbEntry.bytes))
                                {
                                    trackBytes = ConvertStream.ConvertFile(new Options(), stream, Helper.GetFileType(awbEntry.HcaInfo.EncodeType), FileType.Wave);
                                }

                                File.WriteAllBytes($"{extractDirectory}/{trackName}.wav", trackBytes);
                            }
                            else
                            {
                                if(awbEntry.HcaInfo.EncodeType != EncodeType.None)
                                {
                                    File.WriteAllBytes($"{extractDirectory}/{trackName}.{awbEntry.HcaInfo.EncodeType.ToString().ToLower()}", awbEntry.bytes);
                                }
                                else
                                {
                                    File.WriteAllBytes($"{extractDirectory}/{trackName}.bin", awbEntry.bytes);
                                }
                            }

                            progress++;
                            controller.SetProgress(progress);
                        }
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

        private async void Help_CheckForUpdates(object sender, RoutedEventArgs e)
        {
            CheckForUpdate(true);
        }

        private void Help_GitHub(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://github.com/LazyBone152/ACE");
        }

        private async void FileDrop_Event(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if(droppedFilePaths.Length == 1)
                {
                    if(Path.GetExtension(droppedFilePaths[0]) == ".acb")
                    {
                        LoadAcb(droppedFilePaths[0]);
                        e.Handled = true;
                    }
                    if (Path.GetExtension(droppedFilePaths[0]) == ".awb")
                    {
                        LoadAwb(droppedFilePaths[0]);
                        e.Handled = true;
                    }
                }
            }
        }
    
        private async Task<bool> AudioPackageValidation()
        {
            if (AcbFile.AcbFile.SaveFormat != SaveFormat.AudioPackage) return true;

            if(AcbFile.AcbFile.AudioPackageType == AudioPackageType.None)
            {
                await this.ShowMessageAsync("Invalid AudioPackage Type", "The AudioPackage Type has not been set.\n\nYou can do so in the Tools -> DBXV2 Installer menu at the top of the window.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return false;
            }

            if(AcbFile.AcbFile.AudioPackageType == AudioPackageType.BGM_NewOption)
            {
                for(int i = 0; i < AcbFile.AcbFile.Cues.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(AcbFile.AcbFile.Cues[i].Name))
                    {
                        await this.ShowMessageAsync("Invalid NewOption Configuration", "One or more of the cues does not have a name. Names are required for NewOption (BGM) AudioPackages.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }
                }
            }

            if(AcbFile.AcbFile.AudioPackageType == AudioPackageType.AutoVoice)
            {
                for (int i = 0; i < AcbFile.AcbFile.Cues.Count; i++)
                {
                    if (AcbFile.AcbFile.Cues.Any(x => x.Name == AcbFile.AcbFile.Cues[i].Name && x.VoiceLanguage == AcbFile.AcbFile.Cues[i].VoiceLanguage && x != AcbFile.AcbFile.Cues[i]))
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", "The same name is used multiple times for different tracks!\n\nEnsure that ALL tracks have a unique name. Using the same name as another track will overwrite that track!", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }

                    if (AcbFile.AcbFile.Cues.Any(x => x.InstallID_Lang == AcbFile.AcbFile.Cues[i].InstallID_Lang && x != AcbFile.AcbFile.Cues[i]))
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", "Duplicate tracks of the same language have been detected in the AudioPackage. (A duplicate track is a track with the same name, alias and language).", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }

                    if (AcbFile.AcbFile.Cues.Any(x => x.AliasBinding == AcbFile.AcbFile.Cues[i].AliasBinding && !string.IsNullOrWhiteSpace(x.AliasBinding) && x.InstallID != AcbFile.AcbFile.Cues[i].InstallID && x != AcbFile.AcbFile.Cues[i]))
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", "The same alias was used multiple times for different tracks. This is not allowed.\n\nEnsure that the same alias is never used for another track (different name). The same alias can only be used twice - once for an English track and for a Japanese track (same cue name!).", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }

                    if (AcbFile.AcbFile.Cues[i].VoiceLanguage == VoiceLanguageEnum.Japanese)
                    {
                        if (!AcbFile.AcbFile.Cues.Any(x => x.VoiceLanguage == VoiceLanguageEnum.English && x.InstallID == AcbFile.AcbFile.Cues[i].InstallID))
                        {
                            await this.ShowMessageAsync("Invalid AutoVoice Configuration", string.Format("No English track was found for the Japanese track named \"{0}\" with the alias \"{1}\".\n\nEnglish tracks are always mandatory.", AcbFile.AcbFile.Cues[i].Name, AcbFile.AcbFile.Cues[i].AliasBinding), MessageDialogStyle.Affirmative, DialogSettings.Default);
                            return false;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(AcbFile.AcbFile.Cues[i].Name))
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", "One or more of the cues does not have a name. Names are required for AutoVoice AudioPackages.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }
                }

                for (int i = 0; i < AcbFile.Cues.Count; i++)
                {
                    if (AcbFile.Cues[i].NumActionTracks > 0)
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", "Actions are not allowed on AutoVoice AudioPackages.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }

                    if (AcbFile.Cues[i].NumTracks != 1)
                    {
                        await this.ShowMessageAsync("Invalid AutoVoice Configuration", string.Format("Invalid number of tracks on cue: {0}.\n\nAutoVoice cues must always have 1 track - no more or less.", AcbFile.Cues[i].CueRef.Name), MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return false;
                    }
                }

                if(AcbFile.AcbFile.Cues.Count == 0)
                {
                    await this.ShowMessageAsync("Invalid AutoVoice Configuration", "The AudioPackage has no cues.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return false;
                }

            }

            return true;
        }

        private void MenuItem_SubmenuClosed(object sender, RoutedEventArgs e)
        {
            cueEditor.UpdateCueDataGridVisibilities();
        }
    }
}
