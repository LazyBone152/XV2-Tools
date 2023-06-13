using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Win32;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;
using LB_Common.Utils;
using AutoUpdater;
using EEPK_Organiser.Forms.Recolor;
using xv2 = Xv2CoreLib.Xenoverse2;

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
                return (effectContainerFile.Version == Xv2CoreLib.EMP_NEW.VersionEnum.DBXV2);
            }
            set
            {
                if (effectContainerFile != null)
                {
                    effectContainerFile.Version = Xv2CoreLib.EMP_NEW.VersionEnum.DBXV2;
                    UpdateSelectedVersion();
                }
            }
        }
        public bool IsVerSDBH
        {
            get
            {
                if (effectContainerFile == null) return false;
                return (effectContainerFile.Version == Xv2CoreLib.EMP_NEW.VersionEnum.SDBH);
            }
            set
            {
                if (effectContainerFile != null)
                {
                    effectContainerFile.Version = Xv2CoreLib.EMP_NEW.VersionEnum.SDBH;
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
        
        public NameList.NameListManager nameListManager { get { return eepkEditor.nameListManager; } }

        public MainWindow()
        {
            //Allows decimal points to be typed in float values with UpdateSourceTrigger=PropertyChanged
            FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;

            //Tooltips
            ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            //Init settings
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;

            //Init UI
            InitializeComponent();
            DataContext = this;
            InitTheme();

            //Update title
            Title += $" ({SettingsManager.Instance.CurrentVersionString})";

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SettingsManager.Instance.SaveSettings();
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            InitTheme();

            if(sender is Settings oldSettings)
            {
                if(oldSettings.GameDirectory != SettingsManager.settings.GameDirectory && SettingsManager.settings.ValidGameDir)
                {
                    AsyncInit();
                }
            }
        }

        public void InitTheme()
        {
            Dispatcher.Invoke((() =>
            {
                ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, SettingsManager.Instance.GetTheme());
            }));
        }

        public async Task AsyncInit()
        {
            var controller = await this.ShowProgressAsync($"Initializing...", $"", false, DialogSettings.Default);
            controller.SetIndeterminate();

            try
            {
                await Task.Run(() =>
                {
                    xv2.Instance.loadCharacters = true;
                    xv2.Instance.loadSkills = true;
                    xv2.Instance.loadCmn = false;
                    xv2.Instance.Init();
                });

                eepkEditor.loadHelper = null;
                NotifyPropertyChanged(nameof(CanLoadFromGame));
                await controller.CloseAsync();
            }
            catch (Exception ex)
            {
                await controller.CloseAsync();
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            NotifyPropertyChanged(nameof(CanLoadFromGame));
        }

        private async void Load(string path = null)
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

            //Clear Undo stack
            UndoManager.Instance.Clear();

            //Load the eepk + assets
            EffectContainerFile file = null;

            if (path == null)
            {
                file = await eepkEditor.LoadEffectContainerFile(false);
            }
            else
            {
                file = await eepkEditor.LoadEffectContainerFile(path, false);
            }

            if (file != null)
            {
                View.EepkEditor.CloseAllEditorForms();
                effectContainerFile = file;
                UpdateSelectedVersion();
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check startup args
            LoadOnStartUp();

            //Async Tasks
            AsyncStartUpTasks();
        }

        private async void AsyncStartUpTasks()
        {
            if (SettingsManager.Instance.Settings.ValidGameDir)
                AsyncInit();


            //Check for updates silently
#if !DEBUG
            CheckForUpdate(false);
#endif

        }

        private async void CheckForUpdate(bool userInitiated)
        {
            //Check for update
            AppUpdate appUpdate = default;

            await Task.Run(() =>
            {
                appUpdate = Update.CheckForUpdate(AutoUpdater.App.EEPK_Organiser);
            });

            await Task.Delay(1000);

            if(Update.UpdateState == UpdateState.XmlDownloadFailed && userInitiated)
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
                //Update is forced to appear even if notifications are disabled. Only to be used for the most vital updates.
                bool updateIsForced = appUpdate.ForceUpdate && !SettingsManager.settings.UpdateNotifications;

                var messageResult = await this.ShowMessageAsync(updateIsForced ? "Update Available (Forced)" : "Update Available", $"An update is available ({appUpdate.Version}). Do you want to download and install it?\n\nNote: All instances of the application will be closed and any unsaved work will be lost.\n\nChangelog:\n{appUpdate.Changelog}", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.ScrollDialog);

                if(messageResult == MessageDialogResult.Affirmative)
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
            }
            else if (userInitiated)
            {
                await this.ShowMessageAsync("Update", $"No update is available.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
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
            View.EepkEditor.CloseAllEditorForms();
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

#if !DEBUG
            try
#endif
            {
                if(effectContainerFile.saveFormat == SaveFormat.EEPK)
                {
                    effectContainerFile.Save();
                    FileCleanUp();
                }
                else if(effectContainerFile.saveFormat == SaveFormat.VfxPackage)
                {
                    effectContainerFile.SaveVfxPackage();
                    FileCleanUp();
                }

                MessageBox.Show("Save successful!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Save failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Open", MessageBoxButton.OK, MessageBoxImage.Error);

            }
#endif
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
                        effectContainerFile.saveFormat = SaveFormat.VfxPackage;
                        effectContainerFile.SaveVfxPackage();
                    }
                    else if (System.IO.Path.GetExtension(saveDialog.FileName) == ".eepk")
                    {
                        effectContainerFile.Directory = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                        effectContainerFile.Name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                        effectContainerFile.saveFormat = SaveFormat.EEPK;
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
                MessageBox.Show(String.Format("Save failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Open", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            string originalGameDir = SettingsManager.Instance.Settings.GameDirectory;

            Forms.Settings settingsForm = new Forms.Settings(this);
            settingsForm.ShowDialog();
            SettingsManager.Instance.SaveSettings();
            InitTheme();
            
            //Reload game cpk stuff if directory was changed
            if(SettingsManager.Instance.Settings.GameDirectory != originalGameDir && SettingsManager.Instance.Settings.ValidGameDir)
            {
                AsyncInit();
            }

        }

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private async void HelpMenu_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdate(true);
        }

        private void HelpMenu_ShortcutKeys_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ctrl + C = Copy\n" +
                "Ctrl + V = Paste\n" +
                "Ctrl + X = Paste Values\n" +
                "Del = Delete\n" +
                "Ctrl + N = New\n" +
                "Ctrl + D = Duplicate\n" +
                "Ctrl + Q = Used By?\n" +
                "Ctrl + Alt + V = Paste As Child (EMP Editor)\n" +
                "Ctrl + A = Add File (EMO Tab)\n" +
                "Ctrl + H = Hue Adjustment\n" +
                "Alt + H = Hue Set\n",
                "Hotkeys", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(String.Format("{0} is a tool for editing Dragon Ball Xenoverse 2 EEPKs and its " +
                "associated effect files (emp, ecf, etr, emo, ema, emb, emm...).\n\n" +
                "Frameworks/Libraries used:\n" +
                "WPF (UI)\n" +
                "MahApps (UI)\n" +
                "AForge.NET (image processing)\n" +
                "Pfim (primary texture loading)\n" +
                "CSharpImageLibrary (texture saving and alternative texture loading)\n" +
                "YAXLib (xml)", "EEPK Organiser", SettingsManager.Instance.CurrentVersionString), "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ToolMenu_AssociateEepkExt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(String.Format("This will associate the .eepk extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    FileAssociations.EepkOrganiser_EnsureAssociationsSetForEepk();
                    MessageBox.Show(".eepk extension successfully associated!", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToolMenu_AssociateVfxExt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(String.Format("This will associate the .vfxpackage extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    FileAssociations.EepkOrganiser_EnsureAssociationsSetForVfxPackage();
                    MessageBox.Show(".vfxpackage extension successfully associated!", "Associate Extension", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ToolMenu_HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                Forms.RecolorAll recolor = new Forms.RecolorAll(effectContainerFile, this);

                if(recolor.Initialize())
                    recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        private void ToolMenu_HueSet_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                RecolorAll_HueSet recolor = new RecolorAll_HueSet(effectContainerFile, this);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        private async void ToolMenu_SuperTextureMerge_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var messageResult = await this.ShowMessageAsync("Optimize Textures (SuperTexture)", $"This feature will attempt to optimize the number of textures used by this EEPK by combining them together. The result will be fewer, but larger individual textures. This should significantly increase the amount of textures that can be used.\n\nIt is advised to make backups of your files before using this feature.", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if(messageResult == MessageDialogResult.Affirmative)
                {
                    int[] ret = effectContainerFile.MergeAllTexturesIntoSuperTextures_PBIND();

                    await this.ShowMessageAsync("Optimize Textures (SuperTexture)", $"{ret[0]} textures were merged together to create {ret[1]} Super Textures.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }

            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        private async void ToolMenu_CleanAll_Click(object sender, RoutedEventArgs e)
        {
            var messageResult = await this.ShowMessageAsync("Clean All", $"Delete all unused or duplicate assets, texture and material from this {effectContainerFile.saveFormat}?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (messageResult == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int[] totals = effectContainerFile.RemoveAllUnusedOrDuplicates(undos);

                int emp = totals[0];
                int etr = totals[1];
                int ecf = totals[2];
                int emo = totals[3];
                int light = totals[4];
                int empTextures = totals[5];
                int textures = totals[6];
                int materials = totals[7];
                int total = totals[8];

                UndoManager.Instance.AddCompositeUndo(undos, $"Clean All ({total})");

                await this.ShowMessageAsync("Clean All", $"{total} duplicate or unused references were purged.\n\nBreakdown by type:\nEMP: {emp}\nETR: {etr}\nECF: {ecf}\nEMO: {emo}\nLIGHT: {light}\nEMP Textures: {empTextures}\nTextures: {textures}\nMaterials: {materials}", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }

        private void UpdateSelectedVersion()
        {
            NotifyPropertyChanged("IsVerDBXV2");
            NotifyPropertyChanged("IsVerSDBH");
        }

        private void FileCleanUp()
        {
            if (effectContainerFile.LoadedExternalFilesNotSaved.Count > 0 && !SettingsManager.Instance.Settings.FileCleanUp_Ignore)
            {
                if (SettingsManager.Instance.Settings.FileCleanUp_Prompt)
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
                else if (SettingsManager.Instance.Settings.FileCleanUp_Delete)
                {
                    foreach (var file in effectContainerFile.LoadedExternalFilesNotSaved)
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                }
            }
        }


        public void SaveExceptionLog(string ex)
        {
            try
            {
                File.WriteAllText(SettingsManager.Instance.GetErrorLogPath(), ex);
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
                MessageBox.Show(String.Format("Failed to apply the name list.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private async void MenuItem_LoadFromGame_CMN_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_Character_Click(object sender, RoutedEventArgs e)
        {
            
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.Character, false);

            if(effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_SuperSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_UltimateSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_EvasiveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_BlastSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_AwokenSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private async void MenuItem_LoadFromGame_Demo_Click(object sender, RoutedEventArgs e)
        {
            if (!eepkEditor.GameDirectoryCheck()) return;

            var effectFile = await eepkEditor.LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                View.EepkEditor.CloseAllEditorForms();

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
                                MessageBox.Show(System.Windows.Application.Current.MainWindow, String.Format("\"{0}\" files are not supported directly. Please load a .eepk.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            default:
                                MessageBox.Show(System.Windows.Application.Current.MainWindow, String.Format("The filetype of the dropped file ({0}) is not supported.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //"Import" relay events
#region Import_relay_Events
        private void EffectOptions_ImportEffectsFromFile_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromFile_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromCMN_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromCharacter_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromSuper_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromUltimate_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromEvasive_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromAwoken_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromBlast_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromDemo_Click(sender, e);
        }

        private void EffectOptions_ImportEffectsFromCache_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EffectOptions_ImportEffectsFromCache_Click(sender, e);
        }


        private void PBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_AssetContainer_AddAsset_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_CreateNewEmp_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_CreateNewEmp_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromCMN_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromCharacter_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromSuper_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromUltimate_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromEvasive_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromBlast_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromAwoken_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromDemo_Click(sender, e);
        }

        private void PBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.PBIND_ImportAsset_MenuItem_FromCachedFiles_Click(sender, e);
        }


        private void TBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_AssetContainer_AddAsset_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromCMN_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromCharacter_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromSuper_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromUltimate_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromEvasive_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromBlast_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromAwoken_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromDemo_Click(sender, e);
        }

        private void TBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.TBIND_ImportAsset_MenuItem_FromCachedFiles_Click(sender, e);
        }


        private void CBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_AssetContainer_AddAsset_Click(sender, e);
        }

        public void CBIND_ImportAsset_MenuItem_LoadEcf_Click(object sender, RoutedEventArgs e)
        {
            CBIND_ImportAsset_MenuItem_LoadEcf_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromCMN_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromCharacter_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromSuper_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromUltimate_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromEvasive_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromBlast_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromAwoken_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromDemo_Click(sender, e);
        }

        private void CBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.CBIND_ImportAsset_MenuItem_FromCachedFiles_Click(sender, e);
        }


        private void EMO_ImportAsset_MenuItem_LoadEmoFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_LoadEmoFiles_Click(sender, e);
        }

        private void EMO_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_AssetContainer_AddAsset_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromCMN_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromCharacter_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromSuper_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromUltimate_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromEvasive_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromBlast_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromAwoken_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromDemo_Click(sender, e);
        }

        private void EMO_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.EMO_ImportAsset_MenuItem_FromCachedFiles_Click(sender, e);
        }


        private void LIGHT_ImportAsset_MenuItem_LoadLightEma_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_LoadLightEma_Click(sender, e);
        }

        private void LIGHT_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_AssetContainer_AddAsset_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromCMN_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromCharacter_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromSuper_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromUltimate_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromEvasive_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromBlast_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromAwoken_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromDemo_Click(sender, e);
        }

        private void LIGHT_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            eepkEditor.LIGHT_ImportAsset_MenuItem_FromCachedFiles_Click(sender, e);
        }
#endregion

        private void Help_GitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/LazyBone152/EEPKOrganiser");
        }

    }
}
