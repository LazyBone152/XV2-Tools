using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LB_Mod_Installer.Installer;
using System.IO;
using System.IO.Compression;
using System.Windows.Threading;
using System.Globalization;
using System.Threading;
using System.ComponentModel;
using Xv2CoreLib.Resource;
using MahApps.Metro.Controls;

namespace LB_Mod_Installer
{
    public enum InstallState
    {
        InitialPrompt, //Update/Reinstall or Uninstall. State only happens if mod is already installed.
        InstallSteps, //When going through the InstallSteps.
        InstallPrompt, //When InstallSteps are finished and before Installing.
        UninstallPrompt, //Prompt before Uninstalling
        Installing, //Mod is installing
        Uninstalling, //Mod is uninstalling
    }

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

        private InstallerXml _installerXml = null;
        public InstallerXml InstallerInfo
        {
            get
            {
                return this._installerXml;
            }
            set
            {
                if (value != this._installerXml)
                {
                    this._installerXml = value;
                    NotifyPropertyChanged("InstallerInfo");
                }
            }
        }

        //Settings
        private Settings.GameDirXml settings = null;

        //Zip
        private ZipReader zipManager = null;

        //Cpk
        private Xv2FileIO FileIO = null;

        //File Manager
        private FileCacheManager fileManager = new FileCacheManager();

        //Install
        private Install installer;

        //UI Binding
        private InstallState _installState = InstallState.InstallSteps;
        public InstallState CurrentInstallState
        {
            get
            {
                return this._installState;
            }
            set
            {
                if (value != this._installState)
                {
                    this._installState = value;
                    UpdateUI();
                    NotifyPropertyChanged("_installState");
                }
            }
        }

        private InstallStep _installStep = null;
        public InstallStep CurrentInstallStep
        {
            get
            {
                return this._installStep;
            }
            set
            {
                if (value != this._installStep)
                {
                    this._installStep = value;
                    NotifyPropertyChanged("CurrentInstallStep");
                }
            }
        }

        private bool isInstalled = false;

        //Colors and Backgrounds
        private Brush DefaultTextColor = Brushes.Black;
        private Brush DefaultBackground = Brushes.White;

        private Brush _currentTextColor = Brushes.Black;
        public Brush CurrentTextColor
        {
            get
            {
                return this._currentTextColor;
            }
            set
            {
                if (value != this._currentTextColor)
                {
                    this._currentTextColor = value;
                    NotifyPropertyChanged("CurrentTextColor");
                }
            }
        }
        private Brush _currentBackground = Brushes.White;
        public Brush CurrentBackground
        {
            get
            {
                return this._currentBackground;
            }
            set
            {
                if (value != this._currentBackground)
                {
                    this._currentBackground = value;
                    NotifyPropertyChanged("CurrentBackground");
                }
            }
        }

        //Visibilities
        private Visibility generalVisibility = Visibility.Hidden;
        private Visibility installStepVisibility = Visibility.Hidden;
        private Visibility installStepCountVisibility = Visibility.Hidden;
        private Visibility progressBarVisibility = Visibility.Hidden;
        private Visibility gameDirVisibility = Visibility.Hidden;

        public Visibility GeneralVisibility
        {
            get
            {
                return this.generalVisibility;
            }
            set
            {
                if (value != this.generalVisibility)
                {
                    this.generalVisibility = value;
                    NotifyPropertyChanged("GeneralVisibility");
                }
            }
        }
        public Visibility InstallStepVisibility
        {
            get
            {
                return this.installStepVisibility;
            }
            set
            {
                if (value != this.installStepVisibility)
                {
                    this.installStepVisibility = value;
                    NotifyPropertyChanged("InstallStepVisibility");
                }
            }
        }
        public Visibility ProgressBarVisibility
        {
            get
            {
                return this.progressBarVisibility;
            }
            set
            {
                if (value != this.progressBarVisibility)
                {
                    this.progressBarVisibility = value;
                    NotifyPropertyChanged("ProgressBarVisibility");
                }
            }
        }
        public Visibility GameDirVisibility
        {
            get
            {
                return this.gameDirVisibility;
            }
            set
            {
                if (value != this.gameDirVisibility)
                {
                    this.gameDirVisibility = value;
                    NotifyPropertyChanged("GameDirVisibility");
                }
            }
        }
        public Visibility InstallStepCountVisibility
        {
            get
            {
                return this.installStepCountVisibility;
            }
            set
            {
                if (value != this.installStepCountVisibility)
                {
                    this.installStepCountVisibility = value;
                    NotifyPropertyChanged("InstallStepCountVisibility");
                }
            }
        }

        public bool CanGoBack
        {
            get
            {
                switch (CurrentInstallState)
                {
                    case InstallState.Installing:
                    case InstallState.Uninstalling:
                    case InstallState.InitialPrompt:
                        return false;
                    case InstallState.InstallPrompt:
                        return (InstallerInfo.HasInstallSteps() || isInstalled) ? true : false;
                    case InstallState.UninstallPrompt:
                        return true;
                    case InstallState.InstallSteps:
                        if (isInstalled) return true;
                        return (!InstallerInfo.AtFirstInstallStep());
                    default:
                        return true;
                }
            }
        }
        
        public string GameDir
        {
            get
            {
                return GeneralInfo.GameDir;
            }

            set
            {
                if (value != GeneralInfo.GameDir)
                {
                    GeneralInfo.GameDir = value;
                    NotifyPropertyChanged("GameDir");
                }
            }
        }


        //Methods

        public MainWindow()
        {
#if !DEBUG
            try
#endif
            {
                //Tooltip
                ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));


                //CultureInfo
                GeneralInfo.SystemCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                //Init window
                InitializeComponent();
                DataContext = this;


                //Load installinfo
                LoadInstallInfoZip();
                LoadDefaultBrushes();
                LoadTitleBarBrushes();

                //Load settings
                settings = Settings.GameDirXml.LoadSettings();

                //Find game dir, init fileIO
                InitGameDir();
                InitFileIO();

                //Init UI
                StateInit();
                UpdateUI();

                //Init installer
                installer = new Install(InstallerInfo, zipManager, this, FileIO, fileManager);
                Installer.Install.bindingManager.ProcessInstallerXmlBindings(_installerXml);

            }
#if !DEBUG
            catch (Exception ex)
            {
                SaveErrorLog(ex.ToString());
                MessageBox.Show(String.Format("A fatal exception has occured and cannot be recovered from.\n\nException:\n{0}\n\n---------------------------------\nPlease note that this application requires .NET Framework 4.7 or greater, so ensure that you have that installed.", ex.ToString()), "Fatal Exception", MessageBoxButton.OK, MessageBoxImage.Stop);
                ShutdownApp();
            }
#endif
        }

        private void InitGameDir()
        {
            GameDir = settings.GameDirectory;

            if (!GeneralInfo.IsGameDirValid())
            {
                GameDir = FindGameDirectory();
                settings.GameDirectory = GameDir;
                settings.SaveSettings();
            }
        }

        private void LoadInstallInfoZip()
        {

#if !DEBUG
            try
#endif
            {
                //Look for .installinfo file
                string path = System.IO.Path.GetFullPath(String.Format("{0}.installinfo", System.Diagnostics.Process.GetCurrentProcess().ProcessName));
                
                if (!File.Exists(path))
                {
                    MessageBox.Show(String.Format("The file \"{0}\" could not be found!\nThis file should be in the same directory as the executable.\n\nThe installer will now close.", System.IO.Path.GetFileName(path)), "Initialization", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }

                //Load .installinfo
                zipManager = new ZipReader(ZipFile.Open(path, ZipArchiveMode.Read));

                //Load installerXml from .installinfo
                if (!zipManager.Exists(GeneralInfo.InstallerXml))
                {
                    MessageBox.Show(string.Format("Could not find \"{0}\" in \"{1}\". This file must be at the root level in the archive.\n\nThe installer will now close.", GeneralInfo.InstallerXml, System.IO.Path.GetFileName(path)), "Initialization", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }

                InstallerInfo = zipManager.DeserializeXmlFromArchive_Ext<InstallerXml>(GeneralInfo.InstallerXml);
                InstallerInfo.Init();
                GeneralInfo.InstallerXmlInfo = InstallerInfo;
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "InstallInfo open failed.", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            
#endif
        }

        private void LoadDefaultBrushes()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.DefaultBackgroundBrush))
                {
                    InstallerInfo.UiOverrides.DefaultBackgroundBrush = Brushes.White.ToString();
                }
                if (string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.DefaultFontColor))
                {
                    InstallerInfo.UiOverrides.DefaultFontColor = Brushes.Black.ToString();
                }

                if (zipManager.Exists(InstallerInfo.UiOverrides.DefaultBackgroundBrush) && !string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.DefaultBackgroundBrush))
                {
                    DefaultBackground = LoadBackgroundImage(InstallerInfo.UiOverrides.DefaultBackgroundBrush);
                }
                else
                {
                    DefaultBackground = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.DefaultBackgroundBrush);
                }
                DefaultTextColor = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.DefaultFontColor);
            }
            catch
            {
                MessageBox.Show(String.Format("Failed loading the default background and font brushes (DefaultBackground={0}, DefaultFontColor={1}).", InstallerInfo.UiOverrides.DefaultBackgroundBrush, InstallerInfo.UiOverrides.DefaultFontColor), "Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTitleBarBrushes()
        {
            if (!string.IsNullOrWhiteSpace(GeneralInfo.InstallerXmlInfo.UiOverrides.TitleBarBackground))
            {
                WindowTitleBrush = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.TitleBarBackground);
                BorderBrush = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.TitleBarBackground);
            }

            if (!string.IsNullOrWhiteSpace(GeneralInfo.InstallerXmlInfo.UiOverrides.TitleBarFontColor))
            {
                TitleForeground = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.TitleBarFontColor);
                OverrideDefaultWindowCommandsBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.TitleBarFontColor);
            }
        }

        private void InitFileIO()
        {
            if (GeneralInfo.IsGameDirValid())
            {
                //Only load the "data" cpks.
                FileIO = new Xv2FileIO(GeneralInfo.GameDir, true, new string[5] { "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk", "data_d4_5_xv1.cpk" });
            }
        }
        
        //State Init
        private void StateInit()
        {
            if (GeneralInfo.IsGameDirValid())
            {
                GeneralInfo.LoadTracker();
                isInstalled = !GeneralInfo.Tracker.Mod.newMod;
                GeneralInfo.isInstalled = isInstalled;
            }

            if (isInstalled)
            {
                //Set installstep for this
                InitialPrompt();
            }
            else
            {
                if (InstallerInfo.HasInstallSteps())
                {
                    CurrentInstallState = InstallState.InstallSteps;
                    InstallStepNext();
                }
                else
                {
                    InstallPrompt();
                }
            }
        }

        //New navigation methods
        private void ButtonNext()
        {
            //note: check if mod is installed at app start, and manually set the state for that. Otherwise we will default to InstallSteps.

            switch (CurrentInstallState)
            {
                case InstallState.InitialPrompt:
                    InitialPromptNext();
                    break;
                case InstallState.InstallSteps:
                    if (ProcessCurrentStep())
                    {
                        InstallStepNext();
                    }
                    break;
                case InstallState.InstallPrompt:
                    Install();
                    break;
                case InstallState.UninstallPrompt:
                    Uninstall();
                    break;
            }

            NotifyPropertyChanged("CanGoBack");
        }

        private void ButtonBack()
        {
            //It is not possible to go "back" into the Install or Uninstall states, so no need to define those

            switch (CurrentInstallState)
            {
                case InstallState.InitialPrompt:
                    break;
                case InstallState.InstallSteps:
                    InstallStepPrev();
                    break;
                case InstallState.UninstallPrompt:
                    InitialPrompt();
                    break;
                case InstallState.InstallPrompt:
                    InstallPromptBack();
                    break;
            }

            NotifyPropertyChanged("CanGoBack");
        }

        //Initial Prompt
        private void InitialPrompt()
        {
            CurrentInstallState = InstallState.InitialPrompt;
            GeneralInfo.InstallerXmlInfo.CurrentInstallStep = -1;

            InstallStep step = null;

            if (GeneralInfo.Tracker.GetCurrentMod().Version > GeneralInfo.InstallerXmlInfo.Version)
            {
                //Installing an older ver
                step = InstallStep.InitialPromptReinstall_PrevVer;
            }
            else if (GeneralInfo.Tracker.GetCurrentMod().Version < GeneralInfo.InstallerXmlInfo.Version)
            {
                //Installing an new ver
                step = InstallStep.InitialPromptUpdate;
            }
            else if (GeneralInfo.Tracker.GetCurrentMod().Version == GeneralInfo.InstallerXmlInfo.Version)
            {
                //Installing the same ver
                step = InstallStep.InitialPromptReinstall;
            }

            CurrentInstallStep = step;
            SetBrushesForCurrentInstallStep();
        }

        private void InitialPromptNext()
        {
            if(CurrentInstallStep.SelectedOptionBinding == 0)
            {
                //Install
                if (GeneralInfo.InstallerXmlInfo.HasInstallSteps())
                {
                    InstallStepNext();
                }
                else
                {
                    InstallPrompt();
                }
            }
            else if(CurrentInstallStep.SelectedOptionBinding == 1)
            {
                //Uninstall
                UninstallPrompt();
            }
        }

        //Install Step
        private bool ProcessCurrentStep()
        {
            if(CurrentInstallStep != null)
            {
                if(CurrentInstallStep._requireSelection && CurrentInstallStep.StepType == InstallStep.StepTypes.OptionsMultiSelect)
                {
                    if(CurrentInstallStep.GetSelectedOptions().Count == 0)
                    {
                        MessageBox.Show("Please select atleast 1 option before continuing.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        private void InstallStepNext()
        {
            CurrentInstallState = InstallState.InstallSteps;
            var nextStep = InstallerInfo.GetNextInstallStep();

            if(nextStep != null)
            {
                //Display next step
                CurrentInstallStep = nextStep;
                SetBrushesForCurrentInstallStep();
            }
            else
            {
                //There is no more steps. Go into InstallPrompt state.
                InstallPrompt();
            }
        }

        private void InstallStepPrev()
        {
            var prevStep = InstallerInfo.GetPreviousInstallStep();

            if(prevStep != null)
            {
                //Go into previous step
                CurrentInstallStep = prevStep;
                SetBrushesForCurrentInstallStep();
            }
            else
            {
                //We are already at the first viewable step, so go into InitialPrompt state if that is allowed, else just do nothing
                if (isInstalled)
                    InitialPrompt();
            }
        }

        //Install Prompt
        private void InstallPrompt()
        {
            CurrentInstallState = InstallState.InstallPrompt;
            CurrentInstallStep = InstallStep.InstallConfirm;
            SetBrushesForCurrentInstallStep();
        }
        
        private void InstallPromptBack()
        {
            if (InstallerInfo.HasInstallSteps())
            {
                //Has install steps, revert to last shown one
                CurrentInstallStep = InstallerInfo.GetLastValidInstallStep();
                CurrentInstallState = InstallState.InstallSteps;
                SetBrushesForCurrentInstallStep();
            }
            else if (isInstalled)
            {
                //Has no install steps, but mod is installed so show InitialPrompt
                InitialPrompt();
            }
        }
        
        //Uninstall
        private void UninstallPrompt()
        {
            CurrentInstallState = InstallState.UninstallPrompt;
            CurrentInstallStep = InstallStep.UninstallConfirm;
            SetBrushesForCurrentInstallStep();
        }
        
        private async Task Uninstall()
        {
            if (GeneralInfo.IsGameDirValid())
            {
                CurrentInstallState = InstallState.Uninstalling;
                SetBrushesForUninstalling();

                await SetupInstallProcess();

                //Reload tracker
                GeneralInfo.LoadTracker();

                Uninstall uninstall = new Uninstall(this, FileIO, fileManager);

                //uninstall.Start();
                //uninstall.SaveFiles();
                try
                {
                    await Task.Run(uninstall.Start);
                    await Task.Run(uninstall.Uninstall_JungleFiles);
                    await Task.Run(uninstall.SaveFiles);
                }
                catch (Exception ex)
                {
                    SaveErrorLog(ex.ToString());
                    MessageBox.Show(string.Format("A critical exception occured while uninstalling that cannot be recovered from. The installer will now close.\n\nException:{0}", ex.ToString()), "Critical Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShutdownApp();
                }

                GeneralInfo.DeleteTracker();

                MessageBox.Show("The mod was successfully uninstalled.", GeneralInfo.InstallerXmlInfo.InstallerName, MessageBoxButton.OK, MessageBoxImage.Information);

                ShutdownApp();
            }
            else
            {
                MessageBox.Show(String.Format("The directory where the game was installed could not be located.\n\nSelect the correct directory using the Browse button and then try again. It should be named \"DB Xenoverse 2\"."), "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
        }
        
        
        //Install
        private async Task Install()
        {
            if (GeneralInfo.IsGameDirValid())
            {
                CurrentInstallState = InstallState.Installing;

                //Change UI
                SetBrushesForInstalling();

                //Ensure everything needed for installing is initialized
                await SetupInstallProcess();
                //Time to install, all other details have been confirmed
                StartInstall();
            }
            else
            {
                MessageBox.Show(String.Format("The directory where the game was installed could not be located.\n\nSelect the correct directory using the Browse button and then try again. It should be named \"DB Xenoverse 2\"."), "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SetupInstallProcess()
        {
            try
            {
                if (FileIO == null)
                {
                    InitFileIO();
                }

                if (FileIO.IsInitialized == false || FileIO.GameDir != GameDir)
                {
                    if (FileIO.GameDir != GameDir)
                    {
                        //If game dir does not match the dir on FileIO, then we need to reload it for the current game dir. (this means the user changed the game dir)
                        InitFileIO();
                    }

                    Label_MsgUnderProgressBar.Content = "Loading...";
                    ProgressBar_Main.Visibility = Visibility.Hidden;

                    //Wait while cpkReader is being initialized
                    while (FileIO.IsInitialized == false)
                    {
                        await Task.Delay(50);
                    }

                    Label_MsgUnderProgressBar.Content = "";
                    ProgressBar_Main.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("A critical exception occured that cannot be recovered from. The installer will now close.\n\nException:{0}", ex.ToString()), "Critical Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                ShutdownApp();
            }
        }

        private async Task StartInstall()
        {
            //todo: for release build, make installer.Start call async
            //non-async call is for easy debuging

            try
            {
                //Reload tracker.
                GeneralInfo.LoadTracker();

                //Uninstall previous version (if already installed)
                if (isInstalled)
                {
                    Uninstall uninstall = new Uninstall(this, FileIO, fileManager);
                    await Task.Run(uninstall.Start);
                    //Do not uninstall jungle files. In the event of an error there would be no way to restore them.
                }

                //installer.Start();
                await Task.Run(new Action(installer.Start));

                ShutdownApp();
            }
            catch (Exception ex)
            {
                SaveErrorLog(ex.ToString());
                MessageBox.Show(string.Format("A critical exception occured that cannot be recovered from. The installer will now close.\n\nException:{0}", ex.ToString()), "Critical Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                ShutdownApp();
            }
        }


        //UI
        private void ChangeBackgroundBrush(string brush)
        {
            //First check if its empty OR if its the same as default, and if so use the default
            if (string.IsNullOrWhiteSpace(brush) || brush == InstallerInfo.UiOverrides.DefaultBackgroundBrush)
            {
                CurrentBackground = DefaultBackground;
                return;
            }

            //Load and set brush
            if (zipManager.Exists(brush))
            {
                CurrentBackground = LoadBackgroundImage(brush);
            }
            else
            {
                CurrentBackground = (Brush)new BrushConverter().ConvertFromString(brush);
            }
        }

        private void ChangeTextBrush(string brush)
        {
            //First check if its empty OR if its the same as default, and if so use the default
            if (string.IsNullOrWhiteSpace(brush) || brush == InstallerInfo.UiOverrides.DefaultFontColor)
            {
                CurrentTextColor = DefaultTextColor;
                return;
            }

            //Load and set brush
            CurrentTextColor = (Brush)new BrushConverter().ConvertFromString(brush);
        }

        private void SetBrushesForCurrentInstallStep()
        {
            ChangeBackgroundBrush(CurrentInstallStep.GetBackgroundBrush());
            ChangeTextBrush(CurrentInstallStep.GetTextBrush());
        }

        private void SetBrushesForDefault()
        {
            ChangeBackgroundBrush(InstallerInfo.UiOverrides.DefaultBackgroundBrush);
            ChangeTextBrush(InstallerInfo.UiOverrides.DefaultFontColor);
        }

        private void SetBrushesForInstalling()
        {
            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.InstallingBackgroundBrush))
            {
                ChangeBackgroundBrush(InstallerInfo.UiOverrides.InstallingBackgroundBrush);
            }
            else
            {
                ChangeBackgroundBrush(InstallerInfo.UiOverrides.DefaultBackgroundBrush);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.InstallingFontColor))
            {
                ChangeTextBrush(InstallerInfo.UiOverrides.InstallingFontColor);
            }
            else
            {
                ChangeTextBrush(InstallerInfo.UiOverrides.DefaultFontColor);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.ProgressBarColor))
            {
                ProgressBar_Main.Foreground = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.ProgressBarColor);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.ProgressBarBackgroundColor))
            {
                ProgressBar_Main.Background = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.ProgressBarBackgroundColor);
            }

        }

        private void SetBrushesForUninstalling()
        {
            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.UninstallingBackgroundBrush))
            {
                ChangeBackgroundBrush(InstallerInfo.UiOverrides.UninstallingBackgroundBrush);
            }
            else
            {
                ChangeBackgroundBrush(InstallerInfo.UiOverrides.DefaultBackgroundBrush);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.UninstallingFontColor))
            {
                ChangeTextBrush(InstallerInfo.UiOverrides.UninstallingFontColor);
            }
            else
            {
                ChangeTextBrush(InstallerInfo.UiOverrides.DefaultFontColor);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.ProgressBarColor))
            {
                ProgressBar_Main.Foreground = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.ProgressBarColor);
            }

            if (!string.IsNullOrWhiteSpace(InstallerInfo.UiOverrides.ProgressBarBackgroundColor))
            {
                ProgressBar_Main.Background = (Brush)new BrushConverter().ConvertFromString(InstallerInfo.UiOverrides.ProgressBarBackgroundColor);
            }
        }


        private void UpdateUI()
        {
            //Hide everything
            GameDirVisibility = Visibility.Hidden;
            GeneralVisibility = Visibility.Hidden;
            InstallStepVisibility = Visibility.Hidden;
            ProgressBarVisibility = Visibility.Hidden;
            InstallStepCountVisibility = Visibility.Hidden;

            //Show whats needed based on current state
            switch (CurrentInstallState)
            {
                case InstallState.InitialPrompt:
                    InstallStepVisibility = Visibility.Visible;
                    GameDirVisibility = Visibility.Visible;
                    GeneralVisibility = Visibility.Visible;
                    break;
                case InstallState.Installing:
                    ProgressBarVisibility = Visibility.Visible;
                    break;
                case InstallState.InstallPrompt:
                    InstallStepVisibility = Visibility.Visible;
                    GameDirVisibility = Visibility.Visible;
                    GeneralVisibility = Visibility.Visible;
                    break;
                case InstallState.InstallSteps:
                    InstallStepVisibility = Visibility.Visible;
                    InstallStepCountVisibility = Visibility.Visible;
                    GameDirVisibility = Visibility.Visible;
                    GeneralVisibility = Visibility.Visible;
                    break;
                case InstallState.Uninstalling:
                    ProgressBarVisibility = Visibility.Visible;
                    break;
                case InstallState.UninstallPrompt:
                    InstallStepVisibility = Visibility.Visible;
                    GameDirVisibility = Visibility.Visible;
                    GeneralVisibility = Visibility.Visible;
                    break;
            }
            
        }
        
        //Events
        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonNext();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception:\n{0}", ex.ToString()), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception:\n{0}", ex.ToString()), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RadioButtons_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                SetBrushesForCurrentInstallStep();
                GeneralInfo.InstallerXmlInfo.StepCountUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception:\n{0}", ex.ToString()), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                GeneralInfo.InstallerXmlInfo.StepCountUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception:\n{0}", ex.ToString()), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Utility

        private void SaveErrorLog(string ex)
        {
            try
            {
                File.WriteAllText("install_error.txt", ex);
            }
            catch
            {

            }
        }

        private ImageBrush LoadBackgroundImage(string path)
        {
            try
            {
                BitmapImage image = zipManager.LoadBitmapFromArchive(path);
                
                return new ImageBrush(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ShutdownApp();
                return null;
            }
            
        }
        
        public void ShutdownApp()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                if(Application.Current != null)
                    Application.Current.Shutdown();
            }));
        }
        
        private void Button_About_Click(object sender, EventArgs e)
        {
            MessageBox.Show(String.Format("Name: {0}\nVersion: {1}\nAuthor: {2}\n\nInstaller created by lazybone", InstallerInfo.Name, InstallerInfo.VersionFormattedString, InstallerInfo.Author), "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        //Game Directory
        private string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            bool found = false;
            try
            {
                string GameDirectoryPath = String.Empty;

                if (File.Exists("DB Xenoverse 2/bin/DBXV2.exe"))
                {
                    //At same level as DB Xenoverse 2 folder
                    GameDirectoryPath = System.IO.Path.GetFullPath("DB Xenoverse 2");
                    found = true;
                }
                else if (File.Exists("../bin/DBXV2.exe") && found == false)
                {
                    //In data folder
                    GameDirectoryPath = System.IO.Path.GetFullPath("..");
                    found = true;
                }
                else if (File.Exists("bin/DBXV2.exe") && found == false)
                {
                    //In DB Xenoverse 2 root directory
                    GameDirectoryPath = Directory.GetCurrentDirectory();
                    found = true;
                }
                else if (found == false)
                {
                    foreach(string letter in alphabet)
                    {
                        string _path = String.Format(@"{0}:{1}Program Files (x86){1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                        if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                        {
                            GameDirectoryPath = _path;
                            found = true;
                        }
                    }

                    if(found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }

                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }


                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }

                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }
                }

                GeneralInfo.GameDir = GameDirectoryPath;
                return GameDirectoryPath;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        
        private void Button_BrowseForDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string initialGameDir = GameDir;
                var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

                ShowBrowser:
                _browser.ShowDialog();

                if (!String.IsNullOrWhiteSpace(_browser.SelectedPath))
                {
                    //Folder was selected

                    if (File.Exists(String.Format("{0}/bin/DBXV2.exe", _browser.SelectedPath)))
                    {
                        //DBXV2.exe found (this is the good path)
                        GameDir = _browser.SelectedPath;
                        settings.GameDirectory = GameDir;
                        settings.SaveSettings();
                        InitFileIO();

                        if(initialGameDir != GameDir)
                        {
                            //The game directly was changed so go back to InitialPrompt state
                            StateInit();
                        }
                    }
                    else
                    {
                        //DBXV2.exe not found
                        MessageBoxResult _result = MessageBox.Show("The directory could not be validated. Are you sure you selected the correct directory?\n\nThe main folder of the game must be selected. It should be named \"DB Xenoverse 2\".", GeneralInfo.AppName, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (_result == MessageBoxResult.No)
                        {
                            goto ShowBrowser;
                        }
                        else
                        {
                            GameDir = String.Format("{0}", _browser.SelectedPath);
                        }
                    }
                    _browser = null;
                }

                GeneralInfo.GameDir = GameDir;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("An unhandled exception occured while selecting the game directory.\n\nException:{0}", ex.ToString()), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
    }
}
