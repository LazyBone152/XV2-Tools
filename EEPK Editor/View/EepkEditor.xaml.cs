using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Forms.Editors;
using EEPK_Organiser.Misc;
using EEPK_Organiser.ViewModel;
using LB_Common.Forms;
using LB_Common.Mvvm;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xv2CoreLib;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;
using Application = System.Windows.Application;

namespace EEPK_Organiser.View
{
    public partial class EepkEditor : AutoObservableUserControl
    {
        internal enum Tabs
        {
            //Number matches tab indexes.
            Effect = 0,
            Pbind = 1,
            Tbind = 2,
            Cbind = 3,
            Emo = 4,
            Light = 5
        }

        public event EventHandler SelectedEffectTabChanged;

#region DependencyProperty
        public static readonly DependencyProperty EepkInstanceProperty = DependencyProperty.Register(
            nameof(effectContainerFile), typeof(EffectContainerFile), typeof(EepkEditor), new PropertyMetadata(OnEepkChanged));

        private static DependencyPropertyChangedEventHandler EepkChanged;

        private static void OnEepkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (EepkChanged != null)
                EepkChanged.Invoke(sender, e);
        }

        private void EepkInstanceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(effectContainerFile));
            NotifyPropertyChanged(nameof(IsFileLoaded));

#if !XenoKit
            if(effectContainerFile != null)
                nameListManager.EepkLoaded(effectContainerFile);
#endif
        }
#endregion

        public EffectContainerFile effectContainerFile
        {
            get { return (EffectContainerFile)GetValue(EepkInstanceProperty); }
            set
            {
                SetValue(EepkInstanceProperty, value);
                NotifyPropertyChanged("EepkInstance");
                NotifyPropertyChanged(nameof(effectContainerFile));
            }
        }
        public bool IsFileLoaded => effectContainerFile != null;

#if !XenoKit
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
#endif

        //Cache
        public CacheManager cacheManager { get; set; } = new CacheManager();

        private LoadFromGameHelper _loadHelper = null;
        public LoadFromGameHelper loadHelper
        {
            get
            {
                if (Xenoverse2.Instance.IsInitialized && _loadHelper == null)
                {
                    loadHelper = new LoadFromGameHelper();
                }
                if (Xenoverse2.Instance.IsInitialized)
                {
                    return this._loadHelper;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value != this._loadHelper)
                {
                    this._loadHelper = value;
                    NotifyPropertyChanged(nameof(loadHelper));
                }
            }
        }


        public EepkEditor()
        {
            InitializeComponent();
            DataContext = this;
            rootGrid.DataContext = this;

            EepkChanged += EepkInstanceChanged;

#if !XenoKit
            //Load NameLists
            nameListManager = new NameList.NameListManager();
#endif

            XenoKit_Init();
        }

        //Loading
        public async Task<EffectContainerFile> LoadEffectContainerFile(bool cacheFile = true)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Open EEPK file...";
            //openFile.Filter = "EEPK File | *.eepk; *.vfx2";
            //openFile.Filter = "EEPK File | *.eepk; |VFXPACKAGE File |*.vfxpackage";
            openFile.Filter = string.Format("EEPK File | *.eepk; |{1} File |*{0};", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0, 1));
            openFile.ShowDialog();

            return await LoadEffectContainerFile(openFile.FileName, cacheFile);
        }

        public async Task<EffectContainerFile> LoadEffectContainerFile(string path, bool cacheFile = true)
        {
            if (File.Exists(path) && !string.IsNullOrWhiteSpace(path))
            {
                var loadedFile = await LoadFileAsync(path, false, false);


#if !XenoKit
                //Apply namelist
                nameListManager.EepkLoaded(loadedFile);
#endif

                //Cache the file
                if (cacheFile)
                {
                    cacheManager.CacheFile(path, loadedFile, "File");
                }
                else
                {
                    cacheManager.RemoveCachedFile(path);
                }

                return loadedFile;
            }

            return null;
        }

        public async Task<EffectContainerFile> LoadEepkFromGame(EntitySelector.EntityType type, bool cacheFile = true)
        {
            if (!SettingsManager.settings.ValidGameDir) throw new Exception("Game directory is not valid. Please set the game directory in the settings menu (File > Settings).");

            Forms.EntitySelector entitySelector = new Forms.EntitySelector(loadHelper, type, Application.Current.MainWindow);
            entitySelector.ShowDialog();

            if (entitySelector.SelectedEntity != null)
            {
                var loadedFile = await LoadFileAsync(entitySelector.SelectedEntity.EepkPath, true, entitySelector.OnlyLoadFromCPK);


#if !XenoKit
                //Apply namelist
                nameListManager.EepkLoaded(loadedFile);
#endif

                //Cache the file
                if (cacheFile)
                {
                    cacheManager.CacheFile(entitySelector.SelectedEntity.EepkPath, loadedFile, type.ToString());
                }
                else
                {
                    cacheManager.RemoveCachedFile(entitySelector.SelectedEntity.EepkPath);
                }

                loadedFile.LoadedExternalFiles.Clear();
                loadedFile.Directory = string.Format("{0}/data/{1}", SettingsManager.settings.GameDirectory, loadedFile.Directory);

                return loadedFile;
            }

            return null;
        }

        private async Task<EffectContainerFile> LoadFileAsync(string path, bool fromGame, bool onlyFromCpk)
        {
            var controller = await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync($"Loading...", $"", false, new MetroDialogSettings() { DialogTitleFontSize = 16, DialogMessageFontSize = 12, AnimateHide = false, AnimateShow = false });
            controller.SetIndeterminate();

            EffectContainerFile loadedFile = null;

            try
            {
                await Task.Run(() =>
                {
                    if (!fromGame)
                    {
                        //Load files directly
                        if (Path.GetExtension(path) == ".eepk")
                        {
                            loadedFile = EffectContainerFile.Load(path);
                        }
                        else if (Path.GetExtension(path) == EffectContainerFile.ZipExtension)
                        {
                            loadedFile = EffectContainerFile.LoadVfxPackage(path);
                        }
                    }
                    else
                    {
                        //Load from game
                        loadedFile = EffectContainerFile.Load(path, FileManager.Instance.FileIO, onlyFromCpk);
                    }
                });
            }
            finally
            {
                await controller.CloseAsync();
            }

            return loadedFile;
        }


        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEffectTabChanged?.Invoke(this, EventArgs.Empty);
            XenoKit_OnSelectedTabChanged((Tabs)tabControl.SelectedIndex);
            if (effectContainerFile == null) return;

            switch ((Tabs)tabControl.SelectedIndex)
            {
                case Tabs.Effect:
                    SearchFilter = effectContainerFile.EffectSearchFilter;
                    break;
                case Tabs.Pbind:
                    SearchFilter = effectContainerFile.Pbind.AssetSearchFilter;
                    break;
                case Tabs.Tbind:
                    SearchFilter = effectContainerFile.Tbind.AssetSearchFilter;
                    break;
                case Tabs.Cbind:
                    SearchFilter = effectContainerFile.Cbind.AssetSearchFilter;
                    break;
                case Tabs.Emo:
                    SearchFilter = effectContainerFile.Emo.AssetSearchFilter;
                    break;
                case Tabs.Light:
                    SearchFilter = effectContainerFile.LightEma.AssetSearchFilter;
                    break;
            }

            e.Handled = true;
        }

        private void EffectPart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SyncSelectedEffectWithParts();
            XenoKit_OnEffectPartSelectionChange();
        }

        private void EffectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XenoKit_OnEffectSelectionChange();
        }


        public static bool GameDirectoryCheck()
        {
            if (!SettingsManager.settings.ValidGameDir)
            {
                MessagePrompt.Show("Please set the game directory in the settings menu to use this option (File > Settings > Game Directory).", "Invalid Game Directory", MessagePromptButtons.OK, MessagePromptIcon.Information);
                return false;
            }
            return true;
        }

        #region CachedFiles
        //TODO: Remove this

        public void PBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        public void EMO_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        public void TBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        public void CBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        public void LIGHT_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        public void EffectOptions_ImportEffectsFromCache_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

            if (selectedMenuItem != null)
            {
                CachedFile cachedFile = selectedMenuItem.DataContext as CachedFile;

                if (cachedFile != null)
                {
                    if (cachedFile.effectContainerFile != null)
                        EffectOptions_ImportEffects(cachedFile.effectContainerFile.Effects);
                }
                else
                {
                    MessagePrompt.Show("There are no cached files.", "From Cached Files", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }
        #endregion

        #region Forms
        public static EmpEditorWindow PBIND_OpenEditor(EMP_File empFile, AssetContainerTool assetContainer, string name)
        {
            EmpEditorWindow form = GetActiveEmpForm(empFile);

            if (form == null)
            {
                form = new EmpEditorWindow(empFile, assetContainer, name);
                form.Show();
            }

            form.Focus();
            return form;
        }

        public static EtrEditorWindow TBIND_OpenEditor(ETR_File etrFile, AssetContainerTool assetContainer, string name)
        {
            EtrEditorWindow form = GetActiveEtrForm(etrFile);

            if (form == null)
            {
                form = new EtrEditorWindow(etrFile, assetContainer, name);
                form.Show();
            }

            form.Focus();
            return form;
        }

        public static EcfEditorWindow CBIND_OpenEditor(ECF_File ecfFile, string name)
        {
            EcfEditorWindow form = GetActiveEcfForm(ecfFile);

            if (form == null)
            {
                form = new EcfEditorWindow(ecfFile, name);
                form.Show();
            }

            form.Focus();
            return form;
        }

        public static MaterialsEditorForm AssetContainerOpenMaterialForm(AssetContainerTool assetContainer, AssetType assetType)
        {
            var emmFile = assetContainer.File2_Ref;
            MaterialsEditorForm emmForm = GetActiveEmmForm(emmFile);

            if (emmForm == null)
            {
                emmForm = new MaterialsEditorForm(emmFile, assetContainer, assetType, assetType.ToString());
            }
            else
            {
                emmForm.Focus();
            }

            emmForm.Show();

            return emmForm;
        }

        public static EmbEditForm AssetContainerOpenTextureForm(AssetContainerTool assetContainer, AssetType assetType)
        {
            var embFile = assetContainer.File3_Ref;
            EmbEditForm embForm = GetActiveEmbForm(embFile);

            if (embForm == null)
            {
                embForm = new EmbEditForm(embFile, assetContainer, assetType, assetType.ToString());
            }
            else
            {
                embForm.Focus();
            }

            embForm.Show();

            return embForm;
        }
        
        //Helpers
        public static EmbEditForm GetActiveEmbForm(EMB_TextureFile _embFile)
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is EmbEditForm textureViewer)
                {
                    if (textureViewer.EmbFile == _embFile)
                        return textureViewer;
                }
            }

            return null;
        }

        public static MaterialsEditorForm GetActiveEmmForm(EMM_File _emmFile)
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is MaterialsEditorForm materialsEditor)
                {
                    if (materialsEditor.EmmFile == _emmFile)
                        return materialsEditor;
                }
            }

            return null;
        }

        public static EmpEditorWindow GetActiveEmpForm(EMP_File _empFile)
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is EmpEditorWindow empEditor)
                {
                    if (empEditor.EmpFile == _empFile)
                        return empEditor;
                }
            }

            return null;
        }

        public static void CloseEmpForm(EMP_File empFile)
        {
            var form = GetActiveEmpForm(empFile);

            if (form != null)
                form.Close();
        }

        public static EcfEditorWindow GetActiveEcfForm(ECF_File _ecfFile)
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is EcfEditorWindow empEditor)
                {
                    if (empEditor.EcfFile == _ecfFile)
                        return empEditor;
                }
            }

            return null;
        }

        public static void CloseEcfForm(ECF_File _ecfFile)
        {
            var form = GetActiveEcfForm(_ecfFile);

            if (form != null)
                form.Close();
        }

        public static EtrEditorWindow GetActiveEtrForm(ETR_File _etrFile)
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is EtrEditorWindow etrEditor)
                {
                    if (etrEditor.EtrFile == _etrFile)
                        return etrEditor;
                }
            }

            return null;
        }

        public static void CloseEtrForm(ETR_File _etrFile)
        {
            var form = GetActiveEtrForm(_etrFile);

            if (form != null)
                form.Close();
        }

        public static void CloseAllEditorForms()
        {
            foreach (object window in Application.Current.Windows)
            {
                if (window is EmbEditForm ||
                    window is MaterialsEditorForm ||
                    window is EmpEditorWindow ||
                    window is EcfEditorWindow ||
                    window is EtrEditorWindow)
                {
                    (window as Window).Close();
                }
            }
        }

        #endregion

        //Not sure what these are for
        private void MenuItem_MouseMove(object sender, MouseEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            menuItem.IsSubmenuOpen = true;
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }
            e.Handled = true;
            var wheelArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = control
            };
            var parent = VisualTreeHelper.GetParent(control) as UIElement;
            //var parent = control.Parent as UIElement;
            parent?.RaiseEvent(wheelArgs);
        }

#if !XenoKit
        private void NameList_Item_Click(object sender, RoutedEventArgs e)
        {
            if (effectContainerFile == null) return;

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

        private void NameList_Clear_Click(object sender, RoutedEventArgs e)
        {
            nameListManager.ClearNameList(effectContainerFile.Effects);
        }

        private void NameList_Save_Click(object sender, RoutedEventArgs e)
        {
            nameListManager.SaveNameList(effectContainerFile.Effects);
        }
#endif

        #region ToolMenu
        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void ToolMenu_HueAdjust()
        {
            RecolorAll recolor = new RecolorAll(effectContainerFile, false, "Global");

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void ToolMenu_HueSet()
        {
            RecolorAll recolor = new RecolorAll(effectContainerFile, true, "Global");

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void ToolMenu_CreateSuperTexture()
        {
            if (MessagePrompt.Show($"This feature will attempt to optimize the number of textures used by this EEPK by combining them together. The result will be fewer, but larger individual textures. This should significantly increase the amount of textures that can be used.\n\nIt is advised to make backups of your files before using this feature.",
                "Optimize Textures (SuperTexture)", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
            {
                int[] ret = effectContainerFile.MergeAllTexturesIntoSuperTextures_PBIND();

                MessagePrompt.Show($"{ret[0]} textures were merged together to create {ret[1]} Super Textures.", "Optimize Textures (SuperTexture)", MessagePromptButtons.OK, MessagePromptIcon.Information);

            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void ToolMenu_CleanAll()
        {
            if (MessagePrompt.Show($"Delete all unused or duplicate assets, texture and material from this {effectContainerFile.saveFormat}?", "Clean All",
                MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
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
                MessagePrompt.Show($"{total} duplicate or unused references were purged.\n\nBreakdown by type:\nEMP: {emp}\nETR: {etr}\nECF: {ecf}\nEMO: {emo}\nLIGHT: {light}\nEMP Textures: {empTextures}\nTextures: {textures}\nMaterials: {materials}", "Clean All");
            }
        }

        #endregion

        #region Search 
        private string _searchFilter = null;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (value != _searchFilter)
                {
                    _searchFilter = value;
                    NotifyPropertyChanged(nameof(SearchFilter));
                }
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            if (effectContainerFile == null) return;

            SearchFilter = string.Empty;

            switch ((Tabs)tabControl.SelectedIndex)
            {
                case Tabs.Effect:
                    effectContainerFile.EffectSearchFilter = string.Empty;
                    effectContainerFile.UpdateEffectFilter();
                    break;
                case Tabs.Pbind:
                    effectContainerFile.Pbind.AssetSearchFilter = string.Empty;
                    effectContainerFile.Pbind.UpdateAssetFilter();
                    break;
                case Tabs.Tbind:
                    effectContainerFile.Tbind.AssetSearchFilter = string.Empty;
                    effectContainerFile.Tbind.UpdateAssetFilter();
                    break;
                case Tabs.Cbind:
                    effectContainerFile.Cbind.AssetSearchFilter = string.Empty;
                    effectContainerFile.Cbind.UpdateAssetFilter();
                    break;
                case Tabs.Emo:
                    effectContainerFile.Emo.AssetSearchFilter = string.Empty;
                    effectContainerFile.Emo.UpdateAssetFilter();
                    break;
                case Tabs.Light:
                    effectContainerFile.LightEma.AssetSearchFilter = string.Empty;
                    effectContainerFile.LightEma.UpdateAssetFilter();
                    break;
            }
        }

        [RelayCommand]
        private void Search()
        {
            if (effectContainerFile == null) return;

            switch ((Tabs)tabControl.SelectedIndex)
            {
                case Tabs.Effect:
                    effectContainerFile.NewEffectFilter(SearchFilter);
                    effectContainerFile.UpdateEffectFilter();
                    break;
                case Tabs.Pbind:
                    effectContainerFile.Pbind.NewAssetFilter(SearchFilter);
                    effectContainerFile.Pbind.UpdateAssetFilter();
                    break;
                case Tabs.Tbind:
                    effectContainerFile.Tbind.NewAssetFilter(SearchFilter);
                    effectContainerFile.Tbind.UpdateAssetFilter();
                    break;
                case Tabs.Cbind:
                    effectContainerFile.Cbind.NewAssetFilter(SearchFilter);
                    effectContainerFile.Cbind.UpdateAssetFilter();
                    break;
                case Tabs.Emo:
                    effectContainerFile.Emo.NewAssetFilter(SearchFilter);
                    effectContainerFile.Emo.UpdateAssetFilter();
                    break;
                case Tabs.Light:
                    effectContainerFile.LightEma.NewAssetFilter(SearchFilter);
                    effectContainerFile.LightEma.UpdateAssetFilter();
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Search();
        }

        #endregion

        #region XenoKit
        //Partial declarations to be implemented in XenoKit

        public Visibility XenoKitVisible { get; private set; } = Visibility.Collapsed;
        public bool XenoKitAvailable { get; private set; } = false;

        partial void XenoKit_Init();

        partial void XenoKit_OnSelectedTabChanged(Tabs tab);

        partial void XenoKit_OnEffectSelectionChange();

        partial void XenoKit_OnEffectPartSelectionChange();

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        partial void PlaySelectedEffect();

        partial void PlayAsset(Asset asset);


        #endregion

    }
}