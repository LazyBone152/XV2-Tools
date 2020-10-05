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
using EEPK_Organiser.GameInterface;
using EEPK_Organiser.Utils;
using Xv2CoreLib.ECF;
using EEPK_Organiser.Misc;

namespace EEPK_Organiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
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
        private bool _isNotTBIND;
        public bool isNotTBIND
        {
            get
            {
                return _isNotTBIND;
            }

            set
            {
                if (value != this._isNotTBIND)
                {
                    _isNotTBIND = value;
                    NotifyPropertyChanged("isNotTBIND");
                }
            }
        }
        //Effect View
        private bool editModeCancelling = false;
        
        //NameLists
        private NameList.NameListManager _nameListManager = null;
        public NameList.NameListManager nameListManager
        {
            get
            {
                return this._nameListManager;
            }
            set
            {
                if (value != this._nameListManager)
                {
                    this._nameListManager = value;
                    NotifyPropertyChanged("nameListManager");
                }
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

        //Cache
        public CacheManager cacheManager { get; set; } = new Utils.CacheManager();

        //GameInterface
        public bool CanLoadFromGame
        {
            get
            {
                return (gameInterface != null);
            }
        }
        private GameInterface.GameInterface _gameInterfaceValue = null;
        public GameInterface.GameInterface gameInterface
        {
            get
            {
                return this._gameInterfaceValue;
            }
            set
            {
                if (value != this._gameInterfaceValue)
                {
                    this._gameInterfaceValue = value;
                    NotifyPropertyChanged("gameInterface");
                    NotifyPropertyChanged("CanLoadFromGame");
                }
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

            //Load NameLists
            nameListManager = new NameList.NameListManager();

            //Init UI
            InitializeComponent();
            InitInputValidators();
            DataContext = this;

            //Load GameInterface
            try
            {
                InitGameInterface();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


            //Check for updates silently
#if !DEBUG
            if (GeneralInfo.CheckForUpdatesOnStartUp)
            {
                CheckForUpdate(false);
            }
            
#endif

        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check startup args
            LoadOnStartUp();
        }

        private void InitInputValidators()
        {
            floatTextBox1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox2.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox3.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox4.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox5.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox6.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox7.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox8.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox9.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox10.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox11.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox13.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            floatTextBox14.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
        }

        private void InitGameInterface()
        {
            try
            {
                if (GeneralInfo.AppSettings.ValidGameDir)
                {
                    gameInterface = new GameInterface.GameInterface(new Xv2CoreLib.Resource.Xv2FileIO(GeneralInfo.AppSettings.GameDirectory, false, new string[5] { "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk", "data_d4_5_xv1.cpk" }));
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            if(GeneralInfo.AppSettings.GameDirectory != originalGameDir)
            {
                InitGameInterface();
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
                    ExportVfxPackage(effectContainerFile.Effects);
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

        //Misc
        private EffectContainerFile LoadEffectContainerFile(bool cacheFile = true)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open EEPK file...";
            //openFile.Filter = "EEPK File | *.eepk; *.vfx2";
            //openFile.Filter = "EEPK File | *.eepk; |VFXPACKAGE File |*.vfxpackage";
            openFile.Filter = string.Format("EEPK File | *.eepk; |{1} File |*{0};", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0,1));
            openFile.ShowDialog(this);

            return LoadEffectContainerFile(openFile.FileName, cacheFile);
        }

        private EffectContainerFile LoadEffectContainerFile(string path, bool cacheFile = true)
        {
            try
            {
                if (File.Exists(path) && !string.IsNullOrWhiteSpace(path))
                {

                    Forms.ProgressBarFileLoad progressBarForm = new Forms.ProgressBarFileLoad(path, this);
                    progressBarForm.ShowDialog();

                    if (progressBarForm.exception != null)
                    {
                        ExceptionDispatchInfo.Capture(progressBarForm.exception).Throw();
                    }
                    if (progressBarForm.effectContainerFile == null)
                    {
                        throw new FileLoadException("The file load was interupted.");
                    }

                    //Apply namelist
                    nameListManager.EepkLoaded(progressBarForm.effectContainerFile);

                    //Cache the file
                    if(cacheFile)
                    {
                        cacheManager.CacheFile(path, progressBarForm.effectContainerFile, "File");
                    }
                    else
                    {
                        cacheManager.RemoveCachedFile(path);
                    }

                    return progressBarForm.effectContainerFile;
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Load failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Open", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private void Load(string path = null)
        {
            //If a file is already loaded then ask for confirmation
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

            //Load the eepk + assets
            EffectContainerFile file = null;

            if (path == null)
            {
                file = LoadEffectContainerFile(false);
            }
            else
            {
                file = LoadEffectContainerFile(path, false);
            }

            if (file != null)
            {
                CloseOpenWindows();
                effectContainerFile = file;
                UpdateSelectedVersion();
            }

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
                        nameListManager.ApplyNameList(effectContainerFile.Effects, nameList.GetNameList());
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
            nameListManager.ClearNameList(effectContainerFile.Effects);
        }

        private void NameList_Save_Click(object sender, RoutedEventArgs e)
        {
            nameListManager.SaveNameList(effectContainerFile.Effects);
        }

        private void RefreshCounts()
        {
            effectContainerFile.Emo.RefreshAssetCount();
            effectContainerFile.Pbind.RefreshAssetCount();
            effectContainerFile.Tbind.RefreshAssetCount();
            effectContainerFile.Cbind.RefreshAssetCount();
            effectContainerFile.LightEma.RefreshAssetCount();
        }

        //Main TabControl
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedIndex == 0 && effectContainerFile != null)
            {
                var selectedItem = effectDataGrid.SelectedItem as Effect;

                if (selectedItem != null)
                {
                    effectDataGrid.Dispatcher.Invoke((() =>
                    {
                        //effectDataGrid.Focus();
                        //effectDataGrid.ScrollIntoView(selectedItem);
                    }));
                }
            }
        }

        //Asset Containers: EMO
        private void EMO_AssetContainer_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = emoDataGrid.SelectedItem as Asset;
                List<Asset> selectedAssets = emoDataGrid.SelectedItems.Cast<Asset>().ToList();
                selectedAssets.Remove(asset);

                AssetContainer_MergeAssets(asset, selectedAssets, effectContainerFile.Emo, AssetType.EMO);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_AddFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = emoDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    if (asset.Files.Count == 5)
                    {
                        MessageBox.Show("An asset cannot have more than 5 files assigned to it.", "Add File", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    OpenFileDialog openFile = new OpenFileDialog();
                    openFile.Title = "Add file...";
                    openFile.Filter = "EMO effect files | *.emo; *.mat.ema; *.obj.ema; *.emm; *.emb";
                    openFile.ShowDialog(this);

                    if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                    {
                        string originalName = System.IO.Path.GetFileName(openFile.FileName);
                        byte[] bytes = File.ReadAllBytes(openFile.FileName);
                        string newName = effectContainerFile.Emo.GetUnusedName(originalName); //To prevent duplicates

                        switch (EffectFile.GetExtension(openFile.FileName))
                        {
                            case ".emm":
                                asset.AddFile(EMM_File.LoadEmm(bytes), newName, EffectFile.FileType.EMM);
                                break;
                            case ".emb":
                                asset.AddFile(EMB_File.LoadEmb(bytes), newName, EffectFile.FileType.EMB);
                                break;
                            case ".light.ema":
                                asset.AddFile(EMA_File.Load(bytes), newName, EffectFile.FileType.EMA);
                                break;
                            default:
                                asset.AddFile(bytes, newName, EffectFile.FileType.Other);
                                break;
                        }

                        if (newName != originalName)
                        {
                            MessageBox.Show(String.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newName, originalName), "Add File", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EMO_AssetContainer_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = emoDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_PasteOverReplaceAsset(asset, effectContainerFile.Emo, AssetType.EMO);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = emoDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DeleteAsset(selectedAssets, effectContainerFile.Emo);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = emoDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DuplicateAsset(selectedAssets, effectContainerFile.Emo, AssetType.EMO);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = emoDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_UsedBy(asset, false);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_RenameFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedListBox = ((ContextMenu)menuItem.Parent).PlacementTarget as ListBox;

                    var selectedFile = nestedListBox.SelectedItem as EffectFile;

                    if (selectedFile != null)
                    {
                        AssetContainer_RenameFile(selectedFile, effectContainerFile.Emo);

                        var parentAsset = effectContainerFile.Emo.GetAssetByFileInstance(selectedFile);

                        if (parentAsset != null)
                        {
                            parentAsset.RefreshNamePreview();
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_ReplaceFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedListBox = ((ContextMenu)menuItem.Parent).PlacementTarget as ListBox;

                    var selectedFile = nestedListBox.SelectedItem as EffectFile;

                    if (selectedFile != null)
                    {
                        OpenFileDialog openFile = new OpenFileDialog();
                        openFile.Title = "Add file...";
                        openFile.Filter = "XV2 effect files | *.emo; *.ema; *.emm; *.emb";
                        openFile.ShowDialog(this);

                        if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                        {
                            if (System.IO.Path.GetExtension(openFile.FileName) != selectedFile.Extension)
                            {
                                MessageBox.Show(String.Format("The file type of the selected external file ({0}) does not match that of {1}.", openFile.FileName, selectedFile.FullFileName), "Replace", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            string originalNewFileName = System.IO.Path.GetFileName(openFile.FileName);
                            byte[] bytes = File.ReadAllBytes(openFile.FileName);
                            string newFileName = effectContainerFile.Emo.GetUnusedName(originalNewFileName); //To prevent duplicates

                            switch (selectedFile.fileType)
                            {
                                case EffectFile.FileType.EMM:
                                    selectedFile.EmmFile = EMM_File.LoadEmm(bytes);
                                    break;
                                case EffectFile.FileType.EMB:
                                    selectedFile.EmbFile = EMB_File.LoadEmb(bytes);
                                    break;
                                default:
                                    selectedFile.Bytes = bytes;
                                    break;
                            }

                            if (MessageBox.Show("Do you want to keep the old file name?", "Keep Name?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                            {
                                selectedFile.SetName(newFileName);

                                if (newFileName != originalNewFileName)
                                {
                                    MessageBox.Show(String.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newFileName, originalNewFileName), "Add File", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedListBox = ((ContextMenu)menuItem.Parent).PlacementTarget as ListBox;

                    var selectedFile = nestedListBox.SelectedItem as EffectFile;

                    if (selectedFile != null)
                    {
                        var parentAsset = effectContainerFile.Emo.GetAssetByFileInstance(selectedFile);

                        if (parentAsset != null)
                        {
                            if (parentAsset.Files.Count > 1)
                            {
                                parentAsset.RemoveFile(selectedFile);
                            }
                            else
                            {
                                MessageBox.Show(String.Format("Cannot delete the last file."), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(String.Format("Could not find the parent asset."), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Emo.UpdateAssetFilter();
        }

        private void EMO_AssetContainer_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Emo.AssetSearchFilter = string.Empty;
            effectContainerFile.Emo.UpdateAssetFilter();
        }

        private void EMO_AssetContainer_EditFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedListBox = ((ContextMenu)menuItem.Parent).PlacementTarget as ListBox;

                    var selectedFile = nestedListBox.SelectedItem as EffectFile;

                    if (selectedFile != null)
                    {
                        switch (selectedFile.fileType)
                        {
                            case EffectFile.FileType.EMB:
                                {
                                    Forms.EmbEditForm embForm = GetActiveEmbForm(selectedFile.EmbFile);

                                    if(embForm == null)
                                    {
                                        embForm = new Forms.EmbEditForm(selectedFile.EmbFile, null, AssetType.EMO, this, false, selectedFile.FullFileName);
                                    }
                                    else
                                    {
                                        embForm.Focus();
                                    }

                                    embForm.Show();
                                }
                                break;
                            case EffectFile.FileType.EMM:
                                {
                                    Forms.EmmEditForm emmForm = GetActiveEmmForm(selectedFile.EmmFile);

                                    if (emmForm == null)
                                    {
                                        emmForm = new Forms.EmmEditForm(selectedFile.EmmFile, null, AssetType.EMO, this, false, selectedFile.FullFileName);
                                    }
                                    else
                                    {
                                        emmForm.Focus();
                                    }

                                    emmForm.Show();
                                }
                                break;
                            default:
                                MessageBox.Show(String.Format("Edit not possible for {0} files.", selectedFile.Extension), "Edit", MessageBoxButton.OK, MessageBoxImage.Stop);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO, cachedFile.effectContainerFile);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportEmoAssets(effectFile);
        }

        private void EMO_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
            ImportEmoAssets(effectFile);
        }

        private void ImportEmoAssets(EffectContainerFile effectFile)
        {
            if (effectFile != null)
            {
                try
                {
                    AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO, effectFile);
                }
                catch (Exception ex)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EMO_ImportAsset_MenuItem_LoadEmoFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Import Asset...";
                openFile.Filter = "EMO effect files | *.emo; *.mat.ema; *.obj.ema; *.emm; *.emb";
                openFile.Multiselect = true;
                openFile.ShowDialog(this);

                if (openFile.FileNames.Length > 0)
                {
                    if(openFile.FileNames.Length > 5)
                    {
                        MessageBox.Show(string.Format("EMO assets cannot have more than 5 files. {0} were selected.\n\nImport cancelled.", openFile.FileNames.Length), "Import Asset", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    Asset asset = new Asset()
                    {
                        assetType = AssetType.EMO,
                        Files = new ObservableCollection<EffectFile>()
                    };

                    foreach(var file in openFile.FileNames)
                    {
                        string newName = effectContainerFile.Emo.GetUnusedName(System.IO.Path.GetFileName(file));

                        switch (EffectFile.GetFileType(file))
                        {
                            case EffectFile.FileType.EMB:
                                asset.AddFile(EMB_File.LoadEmb(file), newName, EffectFile.FileType.EMB);
                                break;
                            case EffectFile.FileType.EMM:
                                asset.AddFile(EMM_File.LoadEmm(file), newName, EffectFile.FileType.EMM);
                                break;
                            case EffectFile.FileType.Other:
                                asset.AddFile(File.ReadAllBytes(file), newName, EffectFile.FileType.Other);
                                break;
                            default:
                                throw new InvalidDataException(String.Format("EMO_ImportAsset_MenuItem_LoadEmoFiles_Click: FileType = {0} is not valid for EMO.", EffectFile.GetFileType(file)));
                        }
                    }

                    effectContainerFile.Emo.Assets.Add(asset);
                    effectContainerFile.Emo.RefreshAssetCount();
                    emoDataGrid.SelectedItem = asset;
                    emoDataGrid.ScrollIntoView(asset);
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = emoDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_CopyAsset(selectedAssets, AssetType.EMO);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_PasteAsset(AssetType.EMO, effectContainerFile.Emo);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_RemoveUnusedAssets_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                AssetContainer_RemoveUnusedAssets(AssetType.EMO);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EMO_AssetContainer_ExtractFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedListBox = ((ContextMenu)menuItem.Parent).PlacementTarget as ListBox;

                    var selectedFile = nestedListBox.SelectedItem as EffectFile;

                    if (selectedFile != null)
                    {
                        SaveFileDialog saveDialog = new SaveFileDialog();
                        saveDialog.Title = "Extract file...";
                        saveDialog.AddExtension = false;
                        saveDialog.Filter = string.Format("{1} File | *{0}", System.IO.Path.GetExtension(selectedFile.Extension), System.IO.Path.GetExtension(selectedFile.Extension).Remove(0,1).ToUpper());
                        saveDialog.FileName = selectedFile.FullFileName;

                        if (saveDialog.ShowDialog(this) == true)
                        {
                            File.WriteAllBytes(saveDialog.FileName, selectedFile.GetBytes());
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EMO_AssetContainer_Recolor(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = emoDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.EMO, asset, this);
                    recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        //Asset Containers: PBIND
        private void PBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = pbindDataGrid.SelectedItem as Asset;
                List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                selectedAssets.Remove(asset);

                AssetContainer_MergeAssets(asset, selectedAssets, effectContainerFile.Pbind, AssetType.PBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_PasteOverReplaceAsset(asset, effectContainerFile.Pbind, AssetType.PBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DeleteAsset(selectedAssets, effectContainerFile.Pbind);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DuplicateAsset(selectedAssets, effectContainerFile.Pbind, AssetType.PBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_UsedBy(asset, true);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], effectContainerFile.Pbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_EmbEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmbEditForm embForm = GetActiveEmbForm(effectContainerFile.Pbind.File3_Ref);

            if(embForm == null)
            {
                embForm = new Forms.EmbEditForm(effectContainerFile.Pbind.File3_Ref, effectContainerFile.Pbind, AssetType.PBIND, this, true, AssetType.PBIND.ToString());
            }
            else
            {
                embForm.Focus();
            }

            embForm.Show();
        }

        private void PBIND_AssetContainer_EmmEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmmEditForm emmForm = GetActiveEmmForm(effectContainerFile.Pbind.File2_Ref);

            if (emmForm == null)
            {
                emmForm = new Forms.EmmEditForm(effectContainerFile.Pbind.File2_Ref, effectContainerFile.Pbind, AssetType.PBIND, this, true, AssetType.PBIND.ToString());
            }
            else
            {
                emmForm.Focus();
            }

            emmForm.Show();
        }

        private void PBIND_AssetContainer_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Pbind.UpdateAssetFilter();
        }

        private void PBIND_AssetContainer_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Pbind.AssetSearchFilter = string.Empty;
            effectContainerFile.Pbind.UpdateAssetFilter();
        }

        private void PBIND_AssetContainer_Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedAsset = pbindDataGrid.SelectedItem as Asset;

                if (selectedAsset != null)
                {
                    Forms.EMP.EMP_Editor empEditor = GetActiveEmpForm(selectedAsset.Files[0].EmpFile);

                    if(empEditor == null)
                    {
                        empEditor = new Forms.EMP.EMP_Editor(selectedAsset.Files[0].EmpFile, selectedAsset.Files[0].FullFileName, effectContainerFile.Pbind.File3_Ref, effectContainerFile.Pbind.File2_Ref, this);
                    }
                    else
                    {
                        empEditor.Focus();
                    }

                    empEditor.Show();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Forms.EmbEditForm PBIND_OpenTextureViewer()
        {
            PBIND_AssetContainer_EmbEdit_Click(null, null);
            var form = GetActiveEmbForm(effectContainerFile.Pbind.File3_Ref);
            form.Focus();
            return form;
        }

        public Forms.EmmEditForm PBIND_OpenMaterialEditor()
        {
            PBIND_AssetContainer_EmmEdit_Click(null, null);
            var form = GetActiveEmmForm(effectContainerFile.Pbind.File2_Ref);
            form.Focus();
            return form;
        }
        
        private void PBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND, cachedFile.effectContainerFile);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportPbindAssets(effectFile);
        }

        private void PBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
            ImportPbindAssets(effectFile);
        }

        private void ImportPbindAssets(EffectContainerFile effectFile)
        {
            if (effectFile != null)
            {
                try
                {
                    AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND, effectFile);
                }
                catch (Exception ex)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void PBIND_ImportAsset_MenuItem_CreateNewEmp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = effectContainerFile.Pbind.AddAsset(new EMP_File(), "NewEmp.emp");
                effectContainerFile.Pbind.RefreshAssetCount();
                pbindDataGrid.SelectedItem = asset;
                pbindDataGrid.ScrollIntoView(asset);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void PBIND_AssetContainer_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_CopyAsset(selectedAssets, AssetType.PBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_PasteAsset(AssetType.PBIND, effectContainerFile.Pbind);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void PBIND_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PBIND_AssetContainer_Edit_Click(null, null);
        }

        private void PBIND_RemoveUnusedAssets_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                AssetContainer_RemoveUnusedAssets(AssetType.PBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_Recolor(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.PBIND, asset, this);
                    recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        //Asset Containers: TBIND
        private void TBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = tbindDataGrid.SelectedItem as Asset;
                List<Asset> selectedAssets = tbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                selectedAssets.Remove(asset);

                AssetContainer_MergeAssets(asset, selectedAssets, effectContainerFile.Tbind, AssetType.TBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_PasteOverReplaceAsset(asset, effectContainerFile.Tbind, AssetType.TBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = tbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DeleteAsset(selectedAssets, effectContainerFile.Tbind);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = tbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DuplicateAsset(selectedAssets, effectContainerFile.Tbind, AssetType.TBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_UsedBy(asset, true);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], effectContainerFile.Tbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_EmbEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmbEditForm embForm = GetActiveEmbForm(effectContainerFile.Tbind.File3_Ref);

            if (embForm == null)
            {
                embForm = new Forms.EmbEditForm(effectContainerFile.Tbind.File3_Ref, effectContainerFile.Tbind, AssetType.TBIND, this, true, AssetType.TBIND.ToString());
            }
            else
            {
                embForm.Focus();
            }

            embForm.Show();
        }

        private void TBIND_AssetContainer_EmmEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmmEditForm emmForm = GetActiveEmmForm(effectContainerFile.Tbind.File2_Ref);

            if (emmForm == null)
            {
                emmForm = new Forms.EmmEditForm(effectContainerFile.Tbind.File2_Ref, effectContainerFile.Tbind, AssetType.TBIND, this, true, AssetType.TBIND.ToString());
            }
            else
            {
                emmForm.Focus();
            }

            emmForm.Show();
        }

        private void TBIND_AssetContainer_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Tbind.UpdateAssetFilter();
        }

        private void TBIND_AssetContainer_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Tbind.AssetSearchFilter = string.Empty;
            effectContainerFile.Tbind.UpdateAssetFilter();
        }

        public Forms.EmbEditForm TBIND_OpenTextureViewer()
        {
            TBIND_AssetContainer_EmbEdit_Click(null, null);
            var form = GetActiveEmbForm(effectContainerFile.Tbind.File3_Ref);
            form.Focus();
            return form;
        }

        public Forms.EmmEditForm TBIND_OpenMaterialEditor()
        {
            TBIND_AssetContainer_EmmEdit_Click(null, null);
            var form = GetActiveEmmForm(effectContainerFile.Tbind.File2_Ref);
            form.Focus();
            return form;
        }

        private void TBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND, cachedFile.effectContainerFile);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportTbindAssets(effectFile);
        }

        private void TBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
            ImportTbindAssets(effectFile);
        }

        private void ImportTbindAssets(EffectContainerFile effectFile)
        {
            if (effectFile != null)
            {
                try
                {
                    AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND, effectFile);
                }
                catch (Exception ex)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TBIND_AssetContainer_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = tbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_CopyAsset(selectedAssets, AssetType.TBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_PasteAsset(AssetType.TBIND, effectContainerFile.Tbind);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_RemoveUnusedAssets_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                AssetContainer_RemoveUnusedAssets(AssetType.TBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_Recolor(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.TBIND, asset, this);
                    recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        private void TBIND_AssetContainer_Scale(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.ETR.Scale scaleForm = new Forms.ETR.Scale(asset, this);
                    scaleForm.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        //Asset Containers: CBIND
        private void CBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;
                List<Asset> selectedAssets = cbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                selectedAssets.Remove(asset);

                AssetContainer_MergeAssets(asset, selectedAssets, effectContainerFile.Cbind, AssetType.CBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_PasteOverReplaceAsset(asset, effectContainerFile.Cbind, AssetType.CBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = cbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DeleteAsset(selectedAssets, effectContainerFile.Cbind);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = cbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DuplicateAsset(selectedAssets, effectContainerFile.Cbind, AssetType.CBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_UsedBy(asset, true);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], effectContainerFile.Cbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Cbind.UpdateAssetFilter();
        }

        private void CBIND_AssetContainer_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.Cbind.AssetSearchFilter = string.Empty;
            effectContainerFile.Cbind.UpdateAssetFilter();
        }

        private void CBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND, cachedFile.effectContainerFile);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportCbindAssets(effectFile);
        }

        private void CBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
            ImportCbindAssets(effectFile);
        }

        private void ImportCbindAssets(EffectContainerFile effectFile)
        {
            if (effectFile != null)
            {
                try
                {
                    AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND, effectFile);
                }
                catch (Exception ex)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void CBIND_ImportAsset_MenuItem_LoadEcf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Add file...";
                openFile.Filter = "ECF files | *.ecf";
                openFile.ShowDialog(this);

                if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
                {
                    string newName = effectContainerFile.Cbind.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                    Asset asset = new Asset()
                    {
                        assetType = AssetType.CBIND,
                        Files = new ObservableCollection<EffectFile>()
                        {
                            new EffectFile()
                            {
                                EcfFile = ECF_File.Load(openFile.FileName),
                                Extension = EffectFile.GetExtension(openFile.FileName),
                                fileType = EffectFile.FileType.ECF,
                                OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                                FileName = EffectFile.GetFileNameWithoutExtension(newName)
                            }
                        }
                    };

                    effectContainerFile.Cbind.Assets.Add(asset);
                    effectContainerFile.Cbind.RefreshAssetCount();
                    cbindDataGrid.SelectedItem = asset;
                    cbindDataGrid.ScrollIntoView(asset);
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = cbindDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_CopyAsset(selectedAssets, AssetType.CBIND);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_PasteAsset(AssetType.CBIND, effectContainerFile.Cbind);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_RemoveUnusedAssets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_RemoveUnusedAssets(AssetType.CBIND);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_ExtractFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Title = "Extract file...";
                    saveDialog.Filter = "ECF File | *.ecf";
                    saveDialog.FileName = asset.Files[0].FullFileName;

                    if (saveDialog.ShowDialog(this) == true)
                    {
                        File.WriteAllBytes(saveDialog.FileName, asset.Files[0].EcfFile.SaveToBytes());
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_Recolor(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.CBIND, asset, this);
                    recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        //Asset Containers: LIGHT.EMA
        private void LIGHT_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;
                List<Asset> selectedAssets = lightDataGrid.SelectedItems.Cast<Asset>().ToList();
                selectedAssets.Remove(asset);

                AssetContainer_MergeAssets(asset, selectedAssets, effectContainerFile.LightEma, AssetType.LIGHT);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_PasteOverReplaceAsset(asset, effectContainerFile.LightEma, AssetType.LIGHT);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = lightDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DeleteAsset(selectedAssets, effectContainerFile.LightEma);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = lightDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_DuplicateAsset(selectedAssets, effectContainerFile.LightEma, AssetType.LIGHT);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_UsedBy(asset, true);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void LIGHT_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], effectContainerFile.LightEma);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.LightEma.UpdateAssetFilter();
        }

        private void LIGHT_AssetContainer_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.LightEma.AssetSearchFilter = string.Empty;
            effectContainerFile.LightEma.UpdateAssetFilter();
        }

        private void LIGHT_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT, cachedFile.effectContainerFile);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportLightAssets(effectFile);
        }

        private void LIGHT_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
            ImportLightAssets(effectFile);
        }

        private void ImportLightAssets(EffectContainerFile effectFile)
        {
            if (effectFile != null)
            {
                try
                {
                    AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT, effectFile);
                }
                catch (Exception ex)
                {
                    SaveExceptionLog(ex.ToString());
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LIGHT_ImportAsset_MenuItem_LoadLightEma_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Add file...";
                openFile.Filter = "light.ema files | *.light.ema";
                openFile.ShowDialog(this);

                if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
                {
                    string newName = effectContainerFile.LightEma.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                    Asset asset = new Asset()
                    {
                        assetType = AssetType.LIGHT,
                        Files = new ObservableCollection<EffectFile>()
                        {
                            new EffectFile()
                            {
                                EmaFile = EMA_File.Load(openFile.FileName),
                                Extension = EffectFile.GetExtension(openFile.FileName),
                                fileType = EffectFile.FileType.EMA,
                                OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                                FileName = EffectFile.GetFileNameWithoutExtension(newName)
                            }
                        }
                    };

                    effectContainerFile.LightEma.Assets.Add(asset);
                    effectContainerFile.LightEma.RefreshAssetCount();
                    lightDataGrid.SelectedItem = asset;
                    lightDataGrid.ScrollIntoView(asset);
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Asset> selectedAssets = lightDataGrid.SelectedItems.Cast<Asset>().ToList();

                if (selectedAssets.Count > 0)
                {
                    AssetContainer_CopyAsset(selectedAssets, AssetType.LIGHT);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_PasteAsset(AssetType.LIGHT, effectContainerFile.LightEma);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_RemoveUnusedAssets_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

            try
            {
                AssetContainer_RemoveUnusedAssets(AssetType.LIGHT);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_ExtractFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Title = "Extract file...";
                    saveDialog.Filter = "EMA File | *.ema";
                    saveDialog.FileName = asset.Files[0].FullFileName;

                    if (saveDialog.ShowDialog(this) == true)
                    {
                        File.WriteAllBytes(saveDialog.FileName, asset.Files[0].EmaFile.Write());
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LIGHT_AssetContainer_Recolor(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.LIGHT, asset, this);
                    recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        //Asset Containers: General
        private void AssetContainer_ImportAssets(AssetContainerTool container, AssetType type, EffectContainerFile importFile = null)
        {
            if(importFile == null)
            {
                importFile = LoadEffectContainerFile();
            }

            int renameCount = 0;
            int alreadyExistCount = 0;
            int addedCount = 0;

            if (importFile != null)
            {
                Forms.AssetSelector assetSelector = new Forms.AssetSelector(importFile, true, true, type, this);
                assetSelector.ShowDialog();

                if (assetSelector.SelectedAssets != null)
                {
                    Forms.ProgressBarAssetImport assetImportForm = new Forms.ProgressBarAssetImport(container, assetSelector.SelectedAssets, type, this);
                    assetImportForm.ShowDialog();

                    if (assetImportForm.exception != null)
                    {
                        ExceptionDispatchInfo.Capture(assetImportForm.exception).Throw();
                    }

                    renameCount = assetImportForm.renameCount;
                    alreadyExistCount = assetImportForm.alreadyExistCount;
                    addedCount = assetImportForm.addedCount;
                }

                if(alreadyExistCount > 0)
                {
                    MessageBox.Show(String.Format("{0} assets were skipped because they already exist in this EEPK.\n\nHint: Use the duplicate function if that is what you want.", alreadyExistCount), "Add Asset(s)", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (renameCount > 0 && addedCount > 0)
                {
                    MessageBox.Show(String.Format("Assets imported.\n\nNote: {0} files were renamed during the add process due to existing file(s) having the same name.", renameCount), "Add Asset(s)", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (addedCount > 0)
                {
                    MessageBox.Show(String.Format("Assets imported."), "Add Asset(s)", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Operation aborted.", "Add Asset(s)", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            container.RefreshAssetCount();
        }

        private void AssetContainer_PasteOverReplaceAsset(Asset asset, AssetContainerTool container, AssetType type)
        {
            //Changed to work via pasting from clipboard, rather than loading a eepk

            var newAsset = (List<Asset>)Clipboard.GetData(string.Format("{0}{1}", ClipboardDataTypes.Asset, type.ToString()));
            if (newAsset == null)
            {
                MessageBox.Show("No asset was found in the clipboard. Cannot continue paste operation.", "Replace", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (MessageBox.Show(String.Format("The asset \"{0}\" will be replaced with a copy of \"{1}\". This cannot be undone.\n\nDo you want to continue?", asset.FileNamesPreview, newAsset[0].FileNamesPreview), "Replace", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            if (newAsset.Count == 1)
            {
                //back up of original asset data, so it can be restored in the event of an error
                var oldFiles = asset.Files;
                var selectedAsset = newAsset;

                foreach (var file in selectedAsset[0].Files)
                {
                    file.SetName(container.GetUnusedName(file.FullFileName));

                    try
                    {
                        switch (type)
                        {
                            case AssetType.PBIND:
                                container.AddPbindDependencies(file.EmpFile);
                                break;
                            case AssetType.TBIND:
                                container.AddTbindDependencies(file.EtrFile);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Restore the previous asset
                        asset.Files = oldFiles;
                        throw new InvalidOperationException(String.Format("{0}\n\nThe asset has not been changed.", ex.Message));
                    }
                }

                asset.Files = selectedAsset[0].Files;
                asset.RefreshNamePreview();

                effectContainerFile.AssetRefDetailsRefresh(asset);
            }
        }

        private void AssetContainer_MergeAssets(Asset primeAsset, List<Asset> selectedAssets, AssetContainerTool container, AssetType type)
        {

            if (primeAsset != null && selectedAssets.Count > 0)
            {
                int count = selectedAssets.Count + 1;

                if (MessageBox.Show(string.Format("All currently selected assets will be MERGED into {0}.\n\nAll other selected assets will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", primeAsset.FileNamesPreview), string.Format("Merge ({0} assets)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    foreach (var assetToRemove in selectedAssets)
                    {
                        effectContainerFile.AssetRefRefactor(assetToRemove, primeAsset);
                        container.Assets.Remove(assetToRemove);
                    }
                }
                container.RefreshAssetCount();
            }
            else
            {
                MessageBox.Show("Cannot merge with less than 2 assets selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AssetContainer_DeleteAsset(List<Asset> assets, AssetContainerTool container)
        {
            if (MessageBox.Show(String.Format("{0} asset(s) will be deleted. Any EffectParts that are linked to them will also be deleted.\n\nDo you want to continue?", assets.Count), "Delete Asset(s)", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                foreach (var asset in assets)
                {
                    effectContainerFile.AssetRefRefactor(asset, null); //Remove references to this asset
                    container.Assets.Remove(asset);

                    if(asset.assetType == AssetType.PBIND)
                    {
                        CloseEmpForm(asset.Files[0].EmpFile);
                    }
                }
                container.RefreshAssetCount();
            }
        }

        private void AssetContainer_CopyAsset(List<Asset> assets, AssetType type)
        {
            if (assets == null) return;

            effectContainerFile.SaveDds();
            Clipboard.SetData(String.Format("{0}{1}", ClipboardDataTypes.Asset, type.ToString()), assets);
        }

        private void AssetContainer_PasteAsset(AssetType type, AssetContainerTool container)
        {
            List<Asset> assets = (List<Asset>)Clipboard.GetData(String.Format("{0}{1}", ClipboardDataTypes.Asset, type.ToString()));

            if(assets != null)
            {
                int alreadyExistCount = 0;
                int copied = 0;

                foreach(var asset in assets)
                {
                    if(container.AssetExists(asset) == null)
                    {
                        copied++;
                        container.AddAsset(asset);
                    }
                    else
                    {
                        alreadyExistCount++;
                    }
                }

                if(alreadyExistCount > 0 && copied == 0)
                {
                    MessageBox.Show("All copied assets already exist. Copy failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (alreadyExistCount > 0 && copied > 0)
                {
                    MessageBox.Show(String.Format("{0} assets were skipped as they already exist.", alreadyExistCount), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                container.RefreshAssetCount();
            }
        }

        private void AssetContainer_DuplicateAsset(List<Asset> assets, AssetContainerTool container, AssetType assetType)
        {
            foreach (var asset in assets)
            {
                Asset newAsset = asset.Clone();
                newAsset.InstanceID = Guid.NewGuid();
                newAsset.assetType = assetType;

                foreach(var file in newAsset.Files)
                {
                    file.SetName(container.GetUnusedName(file.FullFileName));
                }

                container.Assets.Add(newAsset);
            }
            container.RefreshAssetCount();
        }

        private void AssetContainer_UsedBy(Asset asset, bool showExt)
        {
            if (asset == null) return;

            List<int> effects = effectContainerFile.AssetUsedBy(asset);
            effects.Sort();
            StringBuilder str = new StringBuilder();

            foreach (var effect in effects)
            {
                str.Append(String.Format("Effect ID: {0}\r", effect));
            }

            if (showExt)
            {
                Forms.LogForm logForm = new Forms.LogForm("The following effects use this asset:", str.ToString(), String.Format("{0}: Used By", asset.FileNamesPreviewWithExtension), this, true);
                logForm.Show();
            }
            else
            {
                Forms.LogForm logForm = new Forms.LogForm("The following effects use this asset:", str.ToString(), String.Format("{0}: Used By", asset.FileNamesPreview), this, true);
                logForm.Show();
            }


        }

        private void AssetContainer_RenameFile(EffectFile effectFile, AssetContainerTool container)
        {
            Forms.RenameForm renameForm = new Forms.RenameForm(effectFile.FileName, effectFile.Extension, String.Format("Renaming {0}", effectFile.FullFileName), container, this);
            renameForm.ShowDialog();

            if (renameForm.WasNameChanged)
            {
                effectFile.FileName = renameForm.NameValue;

                effectContainerFile.AssetRefDetailsRefresh(container.GetAssetByFileInstance(effectFile));
            }
        }
        
        private void AssetContainer_RemoveUnusedAssets(AssetType type)
        {
            int amountRemoved = effectContainerFile.RemoveUnusedAssets(type);

            if (amountRemoved > 0)
            {
                MessageBox.Show(String.Format("{0} unused assets were removed.", amountRemoved), "Remove Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("There are no unused assets.", "Remove Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //Hotkeys
        private void EMO_SearchFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EMO_AssetContainer_Search_Click(null, null);
            }
        }

        private void PBIND_SearchFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PBIND_AssetContainer_Search_Click(null, null);
            }
        }

        private void TBIND_SearchFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TBIND_AssetContainer_Search_Click(null, null);
            }
        }

        private void CBIND_SearchFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CBIND_AssetContainer_Search_Click(null, null);
            }
        }

        private void LIGHT_SearchFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LIGHT_AssetContainer_Search_Click(null, null);
            }
        }

        private void EffectDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Copy

                if(e.OriginalSource is ListBoxItem)
                {
                    EffectPart_Copy_Click(null, null);
                }
                else if(e.OriginalSource is DataGridCell)
                {
                    Effect_Copy_Click(null, null);
                }
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Delete

                if (e.OriginalSource is ListBoxItem)
                {
                    EffectPart_Delete_Click(null, null);
                }
                else if (e.OriginalSource is DataGridCell)
                {
                    Effect_DeleteEffect_Click(null, null);
                }

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Paste

                if (Clipboard.ContainsData(ClipboardDataTypes.EffectPart))
                {
                    EffectPart_Paste_Click(null, null);
                }
                else if (Clipboard.ContainsData(ClipboardDataTypes.Effect))
                {
                    Effect_Paste_Click(null, null);
                }

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                //Paste Values

                if (Clipboard.ContainsData(ClipboardDataTypes.EffectPart))
                {
                    EffectPart_PasteValues_Click(null, null);
                }

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Duplicate

                if (e.OriginalSource is ListBoxItem)
                {
                    EffectPart_Duplicate_Click(null, null);
                }
                else if (e.OriginalSource is DataGridCell)
                {
                    Effect_Duplicate_Click(null, null);
                }

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.N) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Duplicate

                if (e.OriginalSource is ListBoxItem)
                {
                    Effect_AddEffectPart_Click(null, null);
                }
                else if (e.OriginalSource is DataGridCell)
                {
                    EffectOptions_AddEffect_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void PbindDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Copy_Click(sender, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                PBIND_AssetContainer_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Duplicate_Click(null, null);
                e.Handled = true;
            }
        }

        private void TbindDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Copy_Click(sender, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                TBIND_AssetContainer_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Duplicate_Click(null, null);
                e.Handled = true;
            }
        }

        private void CbindDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Copy_Click(sender, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                CBIND_AssetContainer_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Duplicate_Click(null, null);
                e.Handled = true;
            }
        }

        private void EmoDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Copy_Click(sender, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                EMO_AssetContainer_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Duplicate_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.A) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_AddFile_Click(null, null);
                e.Handled = true;
            }
        }

        private void LightEmaDataGrid_Hotkeys_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Copy_Click(sender, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                LIGHT_AssetContainer_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Duplicate_Click(null, null);
                e.Handled = true;
            }
        }

        //Effects
        private void Effect_EffectIdChange_ValueChanged(object sender, DataGridCellEditEndingEventArgs e)
        {
            if ((string)e.Column.Header == "Name")
            {
                //We only want to do validation for the Effect ID
                return;
            }

            if (editModeCancelling)
            {
                return;
            }

            var selectedEffect = effectDataGrid.SelectedItem as Effect;

            if (selectedEffect != null)
            {
                string value = ((TextBox)e.EditingElement).Text;
                ushort ret = 0;

                if (!ushort.TryParse(value, out ret))
                {
                    //Value contained invalid text
                    e.Cancel = true;
                    try
                    {
                        MessageBox.Show(string.Format("The entered Effect ID contained invalid characters. Please enter a number between {0} and {1}.", ushort.MinValue, ushort.MaxValue), "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Error);
                        editModeCancelling = true;
                        (sender as DataGrid).CancelEdit();
                    }
                    finally
                    {
                        editModeCancelling = false;
                    }
                }
                else
                {
                    //Value is a valid number.

                    //Now check if it is used by another Effect
                    if (effectContainerFile.EffectIdUsedByOtherEffects(ret, selectedEffect))
                    {
                        e.Cancel = true;
                        try
                        {
                            MessageBox.Show(string.Format("The entered Effect ID is already taken.", ushort.MinValue, ushort.MaxValue), "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Error);
                            editModeCancelling = true;
                            (sender as DataGrid).CancelEdit();
                        }
                        finally
                        {
                            editModeCancelling = false;
                        }
                    }
                }
            }
        }

        private void Effect_AddEffectPart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffect = effectDataGrid.SelectedItem as Effect;

                if (selectedEffect != null)
                {
                    if (selectedEffect.EffectParts == null)
                        selectedEffect.EffectParts = new System.Collections.ObjectModel.ObservableCollection<EffectPart>();

                    selectedEffect.EffectParts.Add(EffectPart.NewEffectPart());
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Effect_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffects = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

                if (selectedEffects.Count > 0)
                {
                    effectContainerFile.SaveDds();
                    Clipboard.SetData(ClipboardDataTypes.Effect, selectedEffects);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Effect_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Effect> effects = (List<Effect>)Clipboard.GetData(ClipboardDataTypes.Effect);

                if(effects != null)
                {
                    //Add effects
                    EffectOptions_ImportEffects(new ObservableCollection<Effect>(effects));
                    
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Effect_DeleteEffect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffects = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

                if (selectedEffects.Count > 0)
                {
                    foreach (var _effect in selectedEffects)
                    {
                        effectContainerFile.Effects.Remove(_effect);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Effect_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffect = effectDataGrid.SelectedItem as Effect;

                if (selectedEffect != null)
                {
                    var copiedEffect = selectedEffect.Clone();
                    copiedEffect.IndexNum = effectContainerFile.GetUnusedEffectId(copiedEffect.IndexNum);
                    effectContainerFile.Effects.Add(copiedEffect);
                    effectDataGrid.SelectedItem = copiedEffect;
                    effectDataGrid.Items.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                    effectDataGrid.ScrollIntoView(copiedEffect);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (effectContainerFile.SelectedEffect != null)
                {
                    ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                    if (effectParts != null)
                    {
                        foreach (var effectPart in effectParts)
                        {
                            if(effectPart.AssetRef != null)
                                effectPart.AssetRef = effectContainerFile.AddAsset(effectPart.AssetRef, effectPart.I_02);
                            
                            effectContainerFile.SelectedEffect.EffectParts.Add(effectPart.Clone());
                            effectContainerFile.RefreshAssetCounts();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured while pasting the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (effectContainerFile.SelectedEffect != null)
                {
                    if (effectContainerFile.SelectedEffect.SelectedEffectParts != null)
                    {
                        effectContainerFile.SaveDds();
                        Clipboard.SetData(ClipboardDataTypes.EffectPart, effectContainerFile.SelectedEffect.SelectedEffectParts);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while copying the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EffectPart_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffect = effectContainerFile.SelectedEffect;

                if (selectedEffect != null)
                {
                    if (selectedEffect.SelectedEffectParts != null)
                    {
                        List<EffectPart> effectPartsToRemove = selectedEffect.SelectedEffectParts.ToList();

                        foreach (var effectPart in effectPartsToRemove)
                        {
                            selectedEffect.EffectParts.Remove(effectPart);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffect = effectContainerFile.SelectedEffect;

                if (selectedEffect != null)
                {
                    if (selectedEffect.SelectedEffectParts != null)
                    {
                        foreach (var effectPart in selectedEffect.SelectedEffectParts)
                        {
                            selectedEffect.EffectParts.Add(effectPart.Clone());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while duplicating the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_GoToAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffectPart = effectContainerFile.GetSelectedEffectParts();

                if (selectedEffectPart != null)
                {
                    if (selectedEffectPart.Count > 0)
                    {
                        tabControl.SelectedIndex = 1; //Go to Asset Tab

                        switch (selectedEffectPart[0].I_02)
                        {
                            case AssetType.PBIND:
                                assetTabControl.SelectedIndex = 0;
                                pbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                pbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.TBIND:
                                assetTabControl.SelectedIndex = 1;
                                tbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                tbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.CBIND:
                                assetTabControl.SelectedIndex = 2;
                                cbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                cbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.EMO:
                                assetTabControl.SelectedIndex = 3;
                                emoDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                emoDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.LIGHT:
                                assetTabControl.SelectedIndex = 4;
                                lightDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                lightDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_ChangeAssetContextMenu_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_ChangeAsset_Click(null, null);
        }

        private void EffectPart_PasteValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (effectContainerFile.SelectedEffect == null) return;
                if (effectContainerFile.SelectedEffect.SelectedEffectPart == null) return;

                ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                if (effectParts == null) return;
                if (effectParts.Count != 1) return;

                effectParts[0].AssetRef = effectContainerFile.AddAsset(effectParts[0].AssetRef, effectParts[0].I_02);
                effectContainerFile.SelectedEffect.SelectedEffectPart.CopyValues(effectParts[0]);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while copying the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            EffectPart_GoToAsset_Click(null, null);
        }
        
        private void Effect_Export(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEffects = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

                if(selectedEffects.Count > 0)
                {
                    ExportVfxPackage(selectedEffects);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Effect Options
        private void EffectOptions_AddEffect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Effect newEffect = new Effect();
                newEffect.EffectParts = new ObservableCollection<EffectPart>();
                newEffect.IndexNum = effectContainerFile.GetUnusedEffectId(0);
                effectContainerFile.Effects.Add(newEffect);
                effectDataGrid.SelectedItem = newEffect;
                effectDataGrid.Items.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                effectDataGrid.ScrollIntoView(newEffect);

                effectContainerFile.UpdateEffectFilter();
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EffectOptions_ImportEffectsFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var importEffectFile = LoadEffectContainerFile();
                if(importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffectsFromCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

                if (selectedMenuItem != null)
                {
                    CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                    if (cachedFile != null)
                    {
                        if(cachedFile.effectContainerFile != null)
                            EffectOptions_ImportEffects(cachedFile.effectContainerFile.Effects);
                    }
                    else
                    {
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffects(ObservableCollection<Effect> effects)
        {
            if (effects != null)
            {
                Forms.EffectSelector effectSelector = new Forms.EffectSelector(effects, effectContainerFile, this);
                effectSelector.ShowDialog();

                if (effectSelector.SelectedEffects != null)
                {
                    foreach (var effect in effectSelector.SelectedEffects)
                    {
                        var newEffect = effect.Clone();
                        newEffect.IndexNum = effect.ImportIdIncrease;
                        effectContainerFile.AddEffect(newEffect, true);
                    }

                    //Update UI
                    if (effectSelector.SelectedEffects.Count > 0)
                    {
                        RefreshCounts();
                        effectDataGrid.SelectedItem = effectSelector.SelectedEffects[0];
                        effectDataGrid.ScrollIntoView(effectSelector.SelectedEffects);
                        effectContainerFile.UpdateEffectFilter();
                    }
                }
            }

            try
            {
                effectDataGrid.Items.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
            }
            catch
            {

            }
        }

        private void EffectOptions_Search_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.UpdateEffectFilter();
        }

        private void EffectOptions_SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                effectContainerFile.UpdateEffectFilter();
        }

        private void EffectOptions_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            effectContainerFile.EffectSearchFilter = string.Empty;
            effectContainerFile.UpdateEffectFilter();
        }

        private void ListBox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Force select the parent effect when right-clicking an effect part.
            ForceSelectEffect(sender as ListBox);
        }

        private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Force select the parent effect when left-clicking.
            //When Left Ctrl + Left Clicking the selection event gets passed to the parent datagrid, which unselects/selects the effect, so this is needed
            ForceSelectEffect(sender as ListBox);
        }

        private void ForceSelectEffect(ListBox sender)
        {
            try
            {
                ListBox listBox = sender;

                if (listBox != null)
                {
                    var selectedEffectPart = listBox.SelectedItem as EffectPart;

                  
              
                    if (selectedEffectPart != null)
                    {
                        var parentEffect = effectContainerFile.GetEffectAssociatedWithEffectPart(selectedEffectPart);

                        if (parentEffect != null)
                        {
                            effectDataGrid.SelectedItem = parentEffect;
                            isNotTBIND = (selectedEffectPart.AssetRef.assetType != AssetType.TBIND);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("ListBox_MouseRightButtonUp: Failed to force select parent effect.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffects_FileDrop(object sender, DragEventArgs e)
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
                                var importEffectFile = LoadEffectContainerFile(droppedFilePaths[0]);
                                if (importEffectFile != null)
                                    EffectOptions_ImportEffects(importEffectFile.Effects);

                                e.Handled = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        

        //EffectPart Details
        private void EffectPart_ChangeAsset_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile.SelectedEffect != null)
            {
                if (effectContainerFile.SelectedEffect.SelectedEffectPart != null)
                {
                    Forms.AssetSelector assetSel = new Forms.AssetSelector(effectContainerFile, false, false, effectContainerFile.SelectedEffect.SelectedEffectPart.I_02, this, effectContainerFile.SelectedEffect.SelectedEffectPart.AssetRef);
                    assetSel.ShowDialog();

                    if (assetSel.SelectedAsset != null)
                    {
                        effectContainerFile.SelectedEffect.SelectedEffectPart.I_02 = assetSel.SelectedAssetType;
                        effectContainerFile.SelectedEffect.SelectedEffectPart.AssetRef = assetSel.SelectedAsset;
                    }
                }
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
                                MessageBox.Show(this, String.Format("\"{0}\" files are not supported directly. Please load a .eepk.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            default:
                                MessageBox.Show(this, String.Format("The filetype of the dropped file ({0}) is not supported.", System.IO.Path.GetExtension(droppedFilePaths[0])), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Window Finding
        public Window GetActiveForm<T>() where T : Window
        {
            foreach (var window in App.Current.Windows)
            {
                if(window is T)
                {
                    return (Window)window;
                }
            }

            return null;
        }

        public Forms.EmbEditForm GetActiveEmbForm(EMB_File _embFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is Forms.EmbEditForm)
                {
                    Forms.EmbEditForm _form = (Forms.EmbEditForm)window;

                    if (_form.EmbFile == _embFile)
                        return _form;
                }
            }

            return null;
        }

        public Forms.EmmEditForm GetActiveEmmForm(EMM_File _emmFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is Forms.EmmEditForm)
                {
                    Forms.EmmEditForm _form = (Forms.EmmEditForm)window;

                    if (_form.EmmFile == _emmFile)
                        return _form;
                }
            }

            return null;
        }

        public Forms.EMP.EMP_Editor GetActiveEmpForm(EMP_File _empFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is Forms.EMP.EMP_Editor)
                {
                    Forms.EMP.EMP_Editor _form = (Forms.EMP.EMP_Editor)window;

                    if (_form.empFile == _empFile)
                        return _form;
                }
            }

            return null;
        }
        
        public void CloseEmpForm(EMP_File empFile)
        {
            var form = GetActiveEmpForm(empFile);

            if (form != null)
                form.Close();
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

        //Load From Game
        private void MenuItem_LoadFromGame_CMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_Character_Click(object sender, RoutedEventArgs e)
        {
            
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Character, false);

            if(effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_SuperSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_UltimateSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_EvasiveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_BlastSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_AwokenSkill_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private void MenuItem_LoadFromGame_Demo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;

            var effectFile = LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo, false);

            if (effectFile != null)
            {
                effectContainerFile = effectFile;
                NotifyPropertyChanged("CanSave");
                UpdateSelectedVersion();
            }
        }

        private EffectContainerFile LoadEepkFromGame(Forms.EntitySelector.EntityType type, bool cacheFile = true)
        {
            try
            {
                if (!GeneralInfo.AppSettings.ValidGameDir) throw new Exception("Game directory is not valid. Please set the game directory in the settings menu (File > Settings).");

                Forms.EntitySelector entitySelector = new Forms.EntitySelector(gameInterface, type, this);
                entitySelector.ShowDialog();

                if (entitySelector.SelectedEntity != null)
                {
                    Forms.ProgressBarFileLoad progressBarForm = new Forms.ProgressBarFileLoad(entitySelector.SelectedEntity.EepkPath, this, gameInterface.fileIO, entitySelector.OnlyLoadFromCPK);
                    progressBarForm.ShowDialog();

                    if (progressBarForm.exception != null)
                    {
                        ExceptionDispatchInfo.Capture(progressBarForm.exception).Throw();
                    }
                    if (progressBarForm.effectContainerFile == null)
                    {
                        throw new FileLoadException("The file load was interupted.");
                    }

                    //Apply namelist
                    nameListManager.EepkLoaded(progressBarForm.effectContainerFile);

                    //Cache the file
                    if (cacheFile)
                    {
                        cacheManager.CacheFile(entitySelector.SelectedEntity.EepkPath, progressBarForm.effectContainerFile, type.ToString());
                    }
                    else
                    {
                        cacheManager.RemoveCachedFile(entitySelector.SelectedEntity.EepkPath);
                    }

                    progressBarForm.effectContainerFile.LoadedExternalFiles.Clear();
                    progressBarForm.effectContainerFile.Directory = string.Format("{0}/data/{1}", GeneralInfo.AppSettings.GameDirectory, progressBarForm.effectContainerFile.Directory);

                    return progressBarForm.effectContainerFile;
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private bool GameDirectoryCheck()
        {
            if (!GeneralInfo.AppSettings.ValidGameDir)
            {
                MessageBox.Show("Please set the game directory in the settings menu to use this option (File > Settings > Game Directory).", "Invalid Game Directory", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
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
        
        //Export
        private void ExportVfxPackage(IList<Effect> selEffects = null)
        {
            if (effectContainerFile == null) return;

            try
            {
                //if selEffects is null, then pass in all the effects.
                IList<Effect> effects = (selEffects != null) ? selEffects : effectContainerFile.Effects;

                Forms.EffectSelector effectSelector = new Forms.EffectSelector(effects, effectContainerFile, this, Forms.EffectSelector.Mode.ExportEffect);
                effectSelector.ShowDialog();

                if(effectSelector.SelectedEffects != null)
                {
                    EffectContainerFile vfxPackage = EffectContainerFile.New();
                    vfxPackage.saveFormat = SaveFormat.ZIP;

                    foreach (var effect in effectSelector.SelectedEffects)
                    {
                        var newEffect = effect.Clone();
                        newEffect.IndexNum = effect.ImportIdIncrease;
                        vfxPackage.AddEffect(newEffect);
                    }

                    //Get path to save to
                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Title = "Export to .vfxpackage";
                    saveDialog.Filter = string.Format("{1} File |*{0}", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0, 1));
                    saveDialog.AddExtension = true;

                    if (saveDialog.ShowDialog(this) == true)
                    {
                        vfxPackage.Directory = string.Format("{0}/{1}", System.IO.Path.GetDirectoryName(saveDialog.FileName), System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName));
                        vfxPackage.Name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                        vfxPackage.SaveVfx2();

                        MessageBox.Show("Export successful.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                }

            } 
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

      
    }
}
