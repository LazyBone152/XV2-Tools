using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP;
using EEPK_Organiser.Utils;
using Xv2CoreLib.ECF;
using EEPK_Organiser.Misc;
using MahApps.Metro.Controls;
using xv2 = Xv2CoreLib.Xenoverse2;
using MahApps.Metro.Controls.Dialogs;

namespace EEPK_Organiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EffectContainerFile _effectContainerFileValue = null;
        public EffectContainerFile effectContainerFile
        {
            get
            {
                return this._effectContainerFileValue;
            }
            set
            {
                if (value != this._effectContainerFileValue)
                {
                    this._effectContainerFileValue = value;
                    NotifyPropertyChanged("effectContainerFile");
                    NotifyPropertyChanged("IsFileLoaded");
                    NotifyPropertyChanged("CanSave");
                }
            }
        }
        public bool IsFileLoaded
        {
            get
            {
                if (effectContainerFile == null) return false;
                return true;
            }
        }
        public bool CanSave
        {
            get
            {
                if (effectContainerFile == null) return false;
                return effectContainerFile.CanSave;
            }
        }

        //Version
        public bool IsVerDBXV2
        {
            get
            {
                if (effectContainerFile == null) return false;
                return (effectContainerFile.Version == Xv2CoreLib.EMP.VersionEnum.DBXV2);
            }
            set
            {
                if (effectContainerFile != null)
                {
                    effectContainerFile.Version = Xv2CoreLib.EMP.VersionEnum.DBXV2;
                    UpdateSelectedVersion();
                }
            }
        }
        public bool IsVerSDBH
        {
            get
            {
                if (effectContainerFile == null) return false;
                return (effectContainerFile.Version == Xv2CoreLib.EMP.VersionEnum.SDBH);
            }
            set
            {
                if (effectContainerFile != null)
                {
                    effectContainerFile.Version = Xv2CoreLib.EMP.VersionEnum.SDBH;
                    UpdateSelectedVersion();
                }
            }
        }

        //GameInterface
        public bool CanLoadFromGame
        {
            get
            {
                return (eepkEditor.loadHelper != null);
            }
        }
        
        public MainWindow()
        {
            //Allows decimal points to be typed in float values with UpdateSourceTrigger=PropertyChanged
            FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;

            //Tooltips
            ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            //Load settings
            GeneralInfo.AppSettings = Settings.AppSettings.LoadSettings();
            GeneralInfo.UpdateEepkToolInterlop();


            //Init UI
            InitializeComponent();
            DataContext = this;

            if(GeneralInfo.AppSettings.ValidGameDir)
                AsyncInit();


            //Check for updates silently
#if !DEBUG
            if (GeneralInfo.CheckForUpdatesOnStartUp)
            {
                CheckForUpdate(false);
            }
            
#endif

        }

        public async Task AsyncInit()
        {
            var controller = await this.ShowProgressAsync($"Initializing...", $"", false, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, DialogTitleFontSize = 15, DialogMessageFontSize = 12 });
            controller.SetIndeterminate();

            try
            {
                await Task.Run(() =>
                {
                    xv2.GameDir = GeneralInfo.AppSettings.GameDirectory;
                    xv2.Instance.loadCharacters = true;
                    xv2.Instance.loadSkills = true;
                    xv2.Instance.loadCmn = false;
                    xv2.Instance.Init();
                });

                await controller.CloseAsync();
            }
            catch (Exception ex)
            {
                await controller.CloseAsync();
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            NotifyPropertyChanged(nameof(CanLoadFromGame));
        }

        private void Load(string path = null)
        {
            //If a file is already loaded then ask for confirmation
            if (effectContainerFile != null)
            {
                var ret = MessageBox.Show(App.Current.MainWindow, String.Format("Do you want to save the currently opened file first?", effectContainerFile.Name), "Open", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (ret == MessageBoxResult.Yes)
                {
                    if (effectContainerFile.CanSave)
                    {
                        Menu_Save_Click(null, null);
                    }
                    else
                    {
                        Menu_SaveAs_Click(null, null);
                    }
                }
                else if (ret == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            //Load the eepk + assets
            EffectContainerFile file = null;

            if (path == null)
            {
                file = eepkEditor.LoadEffectContainerFile(false);
            }
            else
            {
                file = eepkEditor.LoadEffectContainerFile(path, false);
            }

            if (file != null)
            {
                CloseOpenWindows();
                effectContainerFile = file;
                UpdateSelectedVersion();
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check startup args
            LoadOnStartUp();
        }


        private void LoadOnStartUp()
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach(var arg in args)
            {
                if(System.IO.Path.GetExtension(arg) == ".eepk" || System.IO.Path.GetExtension(arg) == EffectContainerFile.ZipExtension)
                {
                    Load(arg);
                    return;
                }
            }
        }

        private void Menu_New_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile != null)
            {
                var ret = MessageBox.Show(this, String.Format("Do you want to save the currently opened file first?", effectContainerFile.Name), "Open", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (ret == MessageBoxResult.Yes)
                {
                    if (effectContainerFile.CanSave)
                    {
                        Menu_Save_Click(null, null);
                    }
                    else
                    {
                        Menu_SaveAs_Click(null, null);
                    }
                }
                else if (ret == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            effectContainerFile = EffectContainerFile.New();
            UpdateSelectedVersion();
            CloseOpenWindows();
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                if(effectContainerFile.saveFormat == SaveFormat.Binary)
                {
                    effectContainerFile.Save();
                    FileCleanUp();
                }
                else if(effectContainerFile.saveFormat == SaveFormat.ZIP)
                {
                    effectContainerFile.SaveVfx2();
                    FileCleanUp();
                }

                MessageBox.Show("Save successful!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Save failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Open", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Save As..";
                saveDialog.Filter = string.Format("EEPK File | *.eepk; |{1} File |*{0};", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0, 1));
                saveDialog.AddExtension = true;
                saveDialog.ShowDialog(this);


                if (!String.IsNullOrWhiteSpace(saveDialog.FileName))
                {
                    if(System.IO.Path.GetExtension(saveDialog.FileName) == EffectContainerFile.ZipExtension)
                    {
                        effectContainerFile.Directory = string.Format("{0}/{1}", System.IO.Path.GetDirectoryName(saveDialog.FileName), System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName));
                        effectContainerFile.saveFormat = SaveFormat.ZIP;
                        effectContainerFile.SaveVfx2();
                    }
                    else if (System.IO.Path.GetExtension(saveDialog.FileName) == ".eepk")
                    {
                        effectContainerFile.Directory = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                        effectContainerFile.Name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                        effectContainerFile.saveFormat = SaveFormat.Binary;
                        effectContainerFile.Save();
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("The extension of \"{0}\" is invalid.", saveDialog.FileName));
                    }
                    
                    //No call to FileCleanUp because we are moving the EEPK location, thus the original EEPK wont be affected.
                    MessageBox.Show("Save successful!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                NotifyPropertyChanged("CanSave");
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Save failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Open", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            string originalGameDir = GeneralInfo.AppSettings.GameDirectory;

            Forms.Settings settingsForm = new Forms.Settings(GeneralInfo.AppSettings, this);
            settingsForm.ShowDialog();
            GeneralInfo.AppSettings.SaveSettings();
            
            if(GeneralInfo.AppSettings.GameDirectory != originalGameDir && GeneralInfo.AppSettings.ValidGameDir)
            {
                eepkEditor.loadHelper = new LoadFromGameHelper();
                NotifyPropertyChanged(nameof(CanLoadFromGame));
            }

        }

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void HelpMenu_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdate(true);
        }

        private void HelpMenu_ShortcutKeys_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ctrl + C = Copy\n" +
                "Ctrl + V = Paste\n" +
                "Shift + V = Paste Values\n" +
                "Ctrl + Del = Delete\n" +
                "Ctrl + N = New\n" +
                "Ctrl + D = Duplicate\n" +
                "Ctrl + R = Rename\n" +
                "Ctrl + E = Replace\n" +
                "Ctrl + M = Merge\n" +
                "Shift + ? = Used By?\n" +
                "Ctrl + Alt + V = Paste As Child (EMP Editor)\n" +
                "Ctrl + A = Add File (EMO Asset Tab)\n" +
                "Ctrl + H = Hue Adjustment (Texture Viewer)\n",
                "Shortcut Keys", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(String.Format("{0} is a tool for editing Dragon Ball Xenoverse 2 EEPKs and its " +
                "associated effect files.\n\n" +
                "Future feature plans:\n" +
                "ETR Editor (trail-like effects)\n" +
                "ECF Editor (shading effects)\n" +
                "LIGHT.EMA Editor (light effects)\n\n" +
                "Frameworks/Libraries used:\n" +
                "WPF (UI)\n" +
                "AForge.NET (image processing)\n" +
                "CSharpImageLibrary (dds loading)\n" +
                "YAXLib (xml)", GeneralInfo.AppName, GeneralInfo.CurrentVersionString), "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ToolMenu_AssociateEepkExt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(String.Format("This will associate the .eepk extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    FileAssociations.EnsureAssociationsSetForEepk();
                    MessageBox.Show(".eepk extension successfully associated!", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToolMenu_AssociateVfxExt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(String.Format("This will associate the .vfxpackage extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    FileAssociations.EnsureAssociationsSetForVfxPackage();
                    MessageBox.Show(".vfxpackage extension successfully associated!", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ToolMenu_ExportEffects_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                if (effectContainerFile.Effects.Count > 0)
                {
                    eepkEditor.ExportVfxPackage(effectContainerFile.Effects);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ToolMenu_HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                Forms.RecolorAll recolor = new Forms.RecolorAll(effectContainerFile, this);
                recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }


        private void UpdateSelectedVersion()
        {
            NotifyPropertyChanged("IsVerDBXV2");
            NotifyPropertyChanged("IsVerSDBH");
        }

        private void FileCleanUp()
        {
            if (effectContainerFile.LoadedExternalFilesNotSaved.Count > 0 && !GeneralInfo.FileCleanUp_Ignore)
            {
                if (GeneralInfo.FileCleanUp_Prompt)
                {
                    StringBuilder str = new StringBuilder();

                    foreach (var file in effectContainerFile.LoadedExternalFilesNotSaved)
                    {
                        str.Append(String.Format("{0}\r", file));
                    }

                    var log = new Forms.LogPrompt("The files listed below are no longer in any of the asset containers. Do you want to also delete them from disk?", str.ToString(), "Save", this, false);
                    log.ShowDialog();

                    if (log.Result == MessageBoxResult.Yes)
                    {
                        foreach (var file in effectContainerFile.LoadedExternalFilesNotSaved)
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                    }
                }
                else if (GeneralInfo.FileCleanUp_Delete)
                {
                    foreach (var file in effectContainerFile.LoadedExternalFilesNotSaved)
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                }
            }
        }

        private async Task CheckForUpdate(bool userInitiated)
        {
            try
            {
                Update.UpdateManager updateManager = new Update.UpdateManager();
                await Task.Run(new Action(updateManager.InitUpdateCheck));

                if (updateManager.GotResponseFromServer)
                {
                    if (updateManager.UpdateAvailable)
                    {
                        Forms.UpdateForm updateForm = new Forms.UpdateForm(updateManager.updateInfo, this);

                        if (userInitiated)
                        {
                            updateForm.ShowDialog();
                        }
                        else
                        {
                            updateForm.Show();
                        }

                        updateForm = null;
                    }
                    else if (userInitiated == true)
                    {
                        MessageBox.Show(this, String.Format("No updates available."), "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (userInitiated == true)
                {
                    MessageBox.Show(this, String.Format("Communication with the server failed."), "Update", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                updateManager = null;
            }
            catch (Exception ex)
            {
                if (userInitiated == true)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(this, String.Format("Update check failed.\n\n{0}", ex.Message), "Update", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveExceptionLog(string ex)
        {
            try
            {
                File.WriteAllText(GeneralInfo.ERROR_LOG_PATH, ex);
            }
            catch
            {
            }
        }

        //NameList
        private void NameList_Item_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    NameList.NameListFile nameList = selectedMenuItem.DataContext as NameList.NameListFile;

                    if (nameList != null)
                    {
                        eepkEditor.nameListManager.ApplyNameList(effectContainerFile.Effects, nameList.GetNameList());
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Failed to apply the name list.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NameList_Clear_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.nameListManager.ClearNameList(effectContainerFile.Effects);
        }

        private void NameList_Save_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.nameListManager.SaveNameList(effectContainerFile.Effects);
        }

        //Load From Game
        private void MenuItem_LoadFromGame_CMN_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_Character_Click(object sender, RoutedEventArgs e)
        {
            
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.Character, false);

            if(effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_SuperSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_UltimateSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_EvasiveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_BlastSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_AwokenSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_Demo_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        

        private void MenuItem_MouseMove(object sender, MouseEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            menuItem.IsSubmenuOpen = true;
        }
        
        //File Path Display
        private void FilePath_MenuItem_CopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                Clipboard.SetText(effectContainerFile.FullFilePath, TextDataFormat.Text);
            }
            catch
            {

            }
        }

        private void FilePath_MenuItem_CopyDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                Clipboard.SetText(effectContainerFile.Directory, TextDataFormat.Text);
            }
            catch
            {

            }
        }

        //File Dropped
        private void Grid_FilesDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                    if (droppedFilePaths.Length == 1)
                    {
                        switch (System.IO.Path.GetExtension(droppedFilePaths[0]))
                        {
                            case EffectContainerFile.ZipExtension:
                            case ".eepk":
                                Load(droppedFilePaths[0]);
                                break;
                            case ".emp":
                            case ".etr":
                            case ".ecf":
                            case ".emb":
                            case ".emm":
                            case ".ema":
                            case ".emo":
                                MessageBox.Show(Application.Current.MainWindow, String.Format("\"{0}\" files are not supported directly. Please load a .eepk.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            default:
                                MessageBox.Show(Application.Current.MainWindow, String.Format("The filetype of the dropped file ({0}) is not supported.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CloseOpenWindows()
        {
            //Close all open windows that are not the main
            //Used for when a new eepk is opened/created

            foreach (var window in App.Current.Windows)
            {
                if (window is Forms.EmbEditForm ||
                    window is Forms.EmmEditForm ||
                    window is Forms.EMP.EMP_Editor)
                {
                    (window as Window).Close();
                }
            }

        }

    }
}
