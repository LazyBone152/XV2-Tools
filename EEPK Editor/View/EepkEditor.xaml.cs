using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using Microsoft.Win32;
using Xv2CoreLib;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMP;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Misc;
using MahApps.Metro.Controls;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;
using EEPK_Organiser.Forms.Recolor;
using Xv2CoreLib.Resource.App;
using Application = System.Windows.Application;
using Xv2CoreLib.Resource;
using System.Windows.Media;

#if XenoKit
using XenoKit;
#endif

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for EepkEditor.xaml
    /// </summary>
    public partial class EepkEditor : UserControl, INotifyPropertyChanged
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
        public bool IsFileLoaded { get { return effectContainerFile != null; } }

        //ViewModel
        private EffectPartViewModel _effectPartViewModel = null;
        public EffectPartViewModel effectPartViewModel
        {
            get
            {
                return _effectPartViewModel;
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

        //Filtering
        private string _searchFilter = null;
        public string SearchFilter
        {
            get
            {
                return this._searchFilter;
            }
            set
            {
                if (value != this._searchFilter)
                {
                    this._searchFilter = value;
                    NotifyPropertyChanged(nameof(SearchFilter));
                }
            }
        }

        private bool effectPartTabInitialize = false;

        public EepkEditor()
        {
            InitializeComponent();
            rootGrid.DataContext = this;

            //Load NameLists
            nameListManager = new NameList.NameListManager();

            //Set tab status
            effectPartGeneral.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_General_Expanded;
            effectPartAnimation.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_Animation_Expanded;
            effectPartPos.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_Position_Expanded;
            effectPartFlags.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_Flags_Expanded;
            effectPartUnkFlags.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_UnkFlags_Expanded;
            effectPartUnkValues.IsExpanded = SettingsManager.settings.EepkOrganiser_EffectPart_UnkValues_Expanded;
            effectPartTabInitialize = true;

            EepkChanged += EepkInstanceChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
#if XenoKit
            toolButton.Visibility = Visibility.Visible;
#else
            toolButton.Visibility = Visibility.Hidden;
#endif
        }

        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (effectContainerFile == null) return;

            emoDataGrid.Items.Refresh();
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
            try
            {
                if (File.Exists(path) && !string.IsNullOrWhiteSpace(path))
                {
                    var loadedFile = await LoadFileAsync(path, false, false);

                    //Apply namelist
                    nameListManager.EepkLoaded(loadedFile);

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
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("Load failed.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Open", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public async Task<EffectContainerFile> LoadEepkFromGame(Forms.EntitySelector.EntityType type, bool cacheFile = true)
        {
            if (!SettingsManager.settings.ValidGameDir) throw new Exception("Game directory is not valid. Please set the game directory in the settings menu (File > Settings).");

            Forms.EntitySelector entitySelector = new Forms.EntitySelector(loadHelper, type, App.Current.MainWindow);
            entitySelector.ShowDialog();

            if (entitySelector.SelectedEntity != null)
            {
                var loadedFile = await LoadFileAsync(entitySelector.SelectedEntity.EepkPath, true, entitySelector.OnlyLoadFromCPK);

                //Apply namelist
                nameListManager.EepkLoaded(loadedFile);

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
            var controller = await ((MetroWindow)App.Current.MainWindow).ShowProgressAsync($"Loading...", $"", false, new MetroDialogSettings() { DialogTitleFontSize = 16, DialogMessageFontSize = 12, AnimateHide = false, AnimateShow = false });
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
                            loadedFile = EffectContainerFile.LoadVfx2(path);
                        }
                    }
                    else
                    {
                        //Load from game
                        loadedFile = EffectContainerFile.Load(path, FileManager.Instance.fileIO, onlyFromCpk);
                    }
                });
            }
            finally
            {
                await controller.CloseAsync();
            }

            return loadedFile;
        }


        //Main TabControl
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

#region EMO
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    openFile.ShowDialog();

                    if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                    {
                        string originalName = Path.GetFileName(openFile.FileName);
                        byte[] bytes = File.ReadAllBytes(openFile.FileName);
                        string newName = effectContainerFile.Emo.GetUnusedName(originalName); //To prevent duplicates

                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        switch (EffectFile.GetExtension(openFile.FileName))
                        {
                            case ".emm":
                                asset.AddFile(EMM_File.LoadEmm(bytes), newName, EffectFile.FileType.EMM, undos);
                                break;
                            case ".emb":
                                asset.AddFile(EMB_File.LoadEmb(bytes), newName, EffectFile.FileType.EMB, undos);
                                break;
                            case ".light.ema":
                                asset.AddFile(EMA_File.Load(bytes), newName, EffectFile.FileType.EMA, undos);
                                break;
                            default:
                                asset.AddFile(bytes, newName, EffectFile.FileType.Other, undos);
                                break;
                        }

                        UndoManager.Instance.AddCompositeUndo(undos, "Add File (EMO)");

                        if (newName != originalName)
                        {
                            MessageBox.Show(String.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newName, originalName), "Add File", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        emoDataGrid.SelectedItem = asset;
                        emoDataGrid.ScrollIntoView(asset);
                    }

                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        var parentAsset = effectContainerFile.Emo.GetAssetByFileInstance(selectedFile);

                        AssetContainer_RenameFile(selectedFile, parentAsset, effectContainerFile.Emo);

                        if (parentAsset != null)
                        {
                            parentAsset.RefreshNamePreview();
                        }

                        emoDataGrid.Items.Refresh();
                        emoDataGrid.SelectedItem = parentAsset;
                        emoDataGrid.ScrollIntoView(parentAsset);
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        openFile.ShowDialog();

                        if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                        {
                            if (Path.GetExtension(openFile.FileName) != selectedFile.Extension)
                            {
                                MessageBox.Show(String.Format("The file type of the selected external file ({0}) does not match that of {1}.", openFile.FileName, selectedFile.FullFileName), "Replace", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            string originalNewFileName = Path.GetFileName(openFile.FileName);
                            byte[] bytes = File.ReadAllBytes(openFile.FileName);
                            string newFileName = effectContainerFile.Emo.GetUnusedName(originalNewFileName); //To prevent duplicates

                            object oldFile;

                            switch (selectedFile.fileType)
                            {
                                case EffectFile.FileType.EMM:
                                    oldFile = selectedFile.EmmFile;
                                    selectedFile.EmmFile = EMM_File.LoadEmm(bytes);
                                    UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.EmmFile), selectedFile, oldFile, selectedFile.EmmFile, "Replace File (EMO)"));
                                    break;
                                case EffectFile.FileType.EMB:
                                    oldFile = selectedFile.EmbFile;
                                    selectedFile.EmbFile = EMB_File.LoadEmb(bytes);
                                    UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.EmbFile), selectedFile, oldFile, selectedFile.EmbFile, "Replace File (EMO)"));
                                    break;
                                default:
                                    oldFile = selectedFile.Bytes;
                                    selectedFile.Bytes = bytes;
                                    UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.Bytes), selectedFile, oldFile, selectedFile.Bytes, "Replace File (EMO)"));
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

                            var selectedItem = emoDataGrid.SelectedItem;
                            emoDataGrid.SelectedItem = selectedItem;
                            emoDataGrid.ScrollIntoView(selectedItem);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        if (parentAsset != null)
                        {
                            if (parentAsset.Files.Count > 1)
                            {
                                parentAsset.RemoveFile(selectedFile, undos);
                            }
                            else
                            {
                                MessageBox.Show(String.Format("Cannot delete the last file."), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            UndoManager.Instance.AddCompositeUndo(undos, "Delete File (EMO)");

                            emoDataGrid.SelectedItem = parentAsset;
                            emoDataGrid.ScrollIntoView(parentAsset);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EMO_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    OpenEmoEffectFileEditor(selectedFile, true);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEmoEffectFileEditor(EffectFile selectedFile, bool showError)
        {
            if (selectedFile != null)
            {
                switch (selectedFile.fileType)
                {
                    case EffectFile.FileType.EMB:
                        {
                            Forms.EmbEditForm embForm = GetActiveEmbForm(selectedFile.EmbFile);

                            if (embForm == null)
                            {
                                embForm = new Forms.EmbEditForm(selectedFile.EmbFile, null, AssetType.EMO, selectedFile.FullFileName);
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
                            Forms.MaterialsEditorForm emmForm = GetActiveEmmForm(selectedFile.EmmFile);

                            if (emmForm == null)
                            {
                                emmForm = new Forms.MaterialsEditorForm(selectedFile.EmmFile, null, AssetType.EMO, selectedFile.FullFileName);
                            }
                            else
                            {
                                emmForm.Focus();
                            }

                            emmForm.Show();
                        }
                        break;
                    default:
                        if(showError)
                            MessageBox.Show(string.Format("Edit not possible for {0} files.", selectedFile.Extension), "Edit", MessageBoxButton.OK, MessageBoxImage.Stop);
                        break;
                }
            }
        }

        public void EMO_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EMO_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportEmoAssets(effectFile);
        }

        public async void EMO_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
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
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void EMO_ImportAsset_MenuItem_LoadEmoFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Import Asset...";
                openFile.Filter = "EMO effect files | *.emo; *.mat.ema; *.obj.ema; *.emm; *.emb";
                openFile.Multiselect = true;
                openFile.ShowDialog();

                if (openFile.FileNames.Length > 0)
                {
                    if (openFile.FileNames.Length > 5)
                    {
                        MessageBox.Show(string.Format("EMO assets cannot have more than 5 files. {0} were selected.\n\nImport cancelled.", openFile.FileNames.Length), "Import Asset", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    Asset asset = new Asset()
                    {
                        assetType = AssetType.EMO,
                        Files = AsyncObservableCollection<EffectFile>.Create()
                    };

                    foreach (var file in openFile.FileNames)
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

                    UndoManager.Instance.AddUndo(new UndoableListAdd<Asset>(effectContainerFile.Emo.Assets, asset, "Add EMO"));
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        saveDialog.Filter = string.Format("{1} File | *{0}", System.IO.Path.GetExtension(selectedFile.Extension), System.IO.Path.GetExtension(selectedFile.Extension).Remove(0, 1).ToUpper());
                        saveDialog.FileName = selectedFile.FullFileName;

                        if (saveDialog.ShowDialog() == true)
                        {
                            File.WriteAllBytes(saveDialog.FileName, selectedFile.GetBytes());
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.EMO, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private void EMO_AssetContainer_RecolorHueSet(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = emoDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(AssetType.EMO, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        public RelayCommand<EffectFile> EMO_DoubleClickCommand => new RelayCommand<EffectFile>(EMO_DoubleClick);
        private void EMO_DoubleClick(EffectFile file)
        {
            OpenEmoEffectFileEditor(file, false);
        }

        #endregion

        #region PBIND
        public void PBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], asset, effectContainerFile.Pbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PBIND_AssetContainer_EmbEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmbEditForm embForm = GetActiveEmbForm(effectContainerFile.Pbind.File3_Ref);

            if (embForm == null)
            {
                embForm = new Forms.EmbEditForm(effectContainerFile.Pbind.File3_Ref, effectContainerFile.Pbind, AssetType.PBIND, AssetType.PBIND.ToString());
            }
            else
            {
                embForm.Focus();
            }

            embForm.Show();
        }

        private void PBIND_AssetContainer_EmmEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.MaterialsEditorForm emmForm = GetActiveEmmForm(effectContainerFile.Pbind.File2_Ref);

            if (emmForm == null)
            {
                emmForm = new Forms.MaterialsEditorForm(effectContainerFile.Pbind.File2_Ref, effectContainerFile.Pbind, AssetType.PBIND, AssetType.PBIND.ToString());
            }
            else
            {
                emmForm.Focus();
            }

            emmForm.Show();
        }

        private void PBIND_AssetContainer_Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedAsset = pbindDataGrid.SelectedItem as Asset;

                if (selectedAsset != null)
                {
                    Forms.EMP.EMP_Editor empEditor = GetActiveEmpForm(selectedAsset.Files[0].EmpFile);

                    if (empEditor == null)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Forms.EmbEditForm PBIND_OpenTextureViewer()
        {
            PBIND_AssetContainer_EmbEdit_Click(null, null);
            var form = GetActiveEmbForm(effectContainerFile.Pbind.File3_Ref);
            form.Focus();
            return form;
        }

        public Forms.MaterialsEditorForm PBIND_OpenMaterialEditor()
        {
            PBIND_AssetContainer_EmmEdit_Click(null, null);
            var form = GetActiveEmmForm(effectContainerFile.Pbind.File2_Ref);
            form.Focus();
            return form;
        }

        public void PBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void PBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportPbindAssets(effectFile);
        }

        public async void PBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
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
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void PBIND_ImportAsset_MenuItem_CreateNewEmp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = effectContainerFile.Pbind.AddAsset(new EMP_File(), "NewEmp.emp");
                effectContainerFile.Pbind.RefreshAssetCount();
                pbindDataGrid.SelectedItem = asset;
                pbindDataGrid.ScrollIntoView(asset);

                //Undos
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Pbind.Assets, asset));
                undos.Add(new UndoActionDelegate(effectContainerFile.Pbind, nameof(effectContainerFile.Pbind.UpdateAssetFilter), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMP"));
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.PBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private void PBIND_AssetContainer_RecolorHueSet(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = pbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(AssetType.PBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private async void PBIND_RemoveColorAnimations(object sender, RoutedEventArgs e)
        {
            List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();

            if (selectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in selectedAssets)
                    undos.AddRange(asset.Files[0].EmpFile.RemoveColorAnimations());

                if(undos.Count > 0)
                {
                    UndoManager.Instance.AddCompositeUndo(undos, "Remove Color Animations (PBIND)");
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "Remove Color Animations (PBIND)", $"{undos.Count} animations were removed", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "Remove Color Animations (PBIND)", $"No animations found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }
        }

        private void PBIND_RemoveRandomColorRange(object sender, RoutedEventArgs e)
        {
            List<Asset> selectedAssets = pbindDataGrid.SelectedItems.Cast<Asset>().ToList();

            if (selectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in selectedAssets)
                    undos.AddRange(asset.Files[0].EmpFile.RemoveRandomColorRange());

                UndoManager.Instance.AddCompositeUndo(undos, "Remove Random Color Range (PBIND)");
            }
        }

#endregion

#region TBIND
        public void TBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], asset, effectContainerFile.Tbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TBIND_AssetContainer_EmbEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.EmbEditForm embForm = GetActiveEmbForm(effectContainerFile.Tbind.File3_Ref);

            if (embForm == null)
            {
                embForm = new Forms.EmbEditForm(effectContainerFile.Tbind.File3_Ref, effectContainerFile.Tbind, AssetType.TBIND, AssetType.TBIND.ToString());
            }
            else
            {
                embForm.Focus();
            }

            embForm.Show();
        }

        private void TBIND_AssetContainer_EmmEdit_Click(object sender, RoutedEventArgs e)
        {
            Forms.MaterialsEditorForm emmForm = GetActiveEmmForm(effectContainerFile.Tbind.File2_Ref);

            if (emmForm == null)
            {
                emmForm = new Forms.MaterialsEditorForm(effectContainerFile.Tbind.File2_Ref, effectContainerFile.Tbind, AssetType.TBIND, AssetType.TBIND.ToString());
            }
            else
            {
                emmForm.Focus();
            }

            emmForm.Show();
        }

        public Forms.EmbEditForm TBIND_OpenTextureViewer()
        {
            TBIND_AssetContainer_EmbEdit_Click(null, null);
            var form = GetActiveEmbForm(effectContainerFile.Tbind.File3_Ref);
            form.Focus();
            return form;
        }

        public Forms.MaterialsEditorForm TBIND_OpenMaterialEditor()
        {
            TBIND_AssetContainer_EmmEdit_Click(null, null);
            var form = GetActiveEmmForm(effectContainerFile.Tbind.File2_Ref);
            form.Focus();
            return form;
        }

        public void TBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void TBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportTbindAssets(effectFile);
        }

        public async void TBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
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
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.TBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private void TBIND_AssetContainer_RecolorHueSet(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(AssetType.TBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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
        private void TBIND_AssetContainer_Scale(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = tbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    Forms.EtrScale scaleForm = new Forms.EtrScale(asset, App.Current.MainWindow);
                    scaleForm.ShowDialog();
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

#endregion

#region CBIND
        public void CBIND_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CBIND_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], asset, effectContainerFile.Cbind);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CBIND_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void CBIND_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportCbindAssets(effectFile);
        }

        public async void CBIND_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
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
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void CBIND_ImportAsset_MenuItem_LoadEcf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Add file...";
                openFile.Filter = "ECF files | *.ecf";
                openFile.ShowDialog();

                if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
                {
                    string newName = effectContainerFile.Cbind.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));
                    
                    Asset asset = new Asset()
                    {
                        assetType = AssetType.CBIND,
                        Files = new AsyncObservableCollection<EffectFile>()
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveDialog.FileName, asset.Files[0].EcfFile.SaveToBytes());
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.CBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private void CBIND_AssetContainer_RecolorHueSet(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = cbindDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(AssetType.CBIND, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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
#endregion

#region LIGHT
        public void LIGHT_AssetContainer_AddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT);

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void LIGHT_AssetContainer_RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    AssetContainer_RenameFile(asset.Files[0], asset, effectContainerFile.LightEma);
                    asset.RefreshNamePreview();
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LIGHT_ImportAsset_MenuItem_FromCachedFiles_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void LIGHT_ImportAsset_MenuItem_FromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
            ImportLightAssets(effectFile);
        }

        public async void LIGHT_ImportAsset_MenuItem_FromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            var effectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
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
                    MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LIGHT_ImportAsset_MenuItem_LoadLightEma_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Add file...";
                openFile.Filter = "light.ema files | *.light.ema";
                openFile.ShowDialog();

                if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
                {
                    string newName = effectContainerFile.LightEma.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                    Asset asset = new Asset()
                    {
                        assetType = AssetType.LIGHT,
                        Files = new AsyncObservableCollection<EffectFile>()
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveDialog.FileName, asset.Files[0].EmaFile.Write());
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Forms.RecolorAll recolor = new Forms.RecolorAll(AssetType.LIGHT, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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

        private void LIGHT_AssetContainer_RecolorHueSet(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var asset = lightDataGrid.SelectedItem as Asset;

                if (asset != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(AssetType.LIGHT, asset, Application.Current.MainWindow);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
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
#endregion

#region Asset_Containers_General
        private async Task AssetContainer_ImportAssets(AssetContainerTool container, AssetType type, EffectContainerFile importFile = null)
        {
            if (importFile == null)
            {
                importFile = await LoadEffectContainerFile();
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();
            int renameCount = 0;
            int alreadyExistCount = 0;
            int addedCount = 0;

            if (importFile != null)
            {
                Forms.AssetSelector assetSelector = new Forms.AssetSelector(importFile, true, true, type, this);
                assetSelector.ShowDialog();

                if (assetSelector.SelectedAssets != null)
                {
                    var controller = await ((MetroWindow)App.Current.MainWindow).ShowProgressAsync($"Initializing...", $"", false, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, DialogTitleFontSize = 15, DialogMessageFontSize = 12 });
                    controller.Minimum = 0;
                    controller.Maximum = assetSelector.SelectedAssets.Count;
                    controller.SetMessage(String.Format("Importing assets: 0 of {0}.", assetSelector.SelectedAssets.Count));

                    try
                    {
                        await Task.Run(() =>
                        {
                            foreach (var newAsset in assetSelector.SelectedAssets)
                            {
                                Asset existingAsset = container.AssetExists(newAsset);

                                if (existingAsset == null)
                                {
                                    //First, regenerate the names
                                    foreach (var newFile in newAsset.Files)
                                    {
                                        newFile.SetName(container.GetUnusedName(newFile.FullFileName));

                                        if (newFile.FullFileName != newFile.OriginalFileName)
                                        {
                                            renameCount++;
                                        }

                                        try
                                        {
                                            switch (type)
                                            {
                                                case AssetType.PBIND:
                                                    container.AddPbindDependencies(newFile.EmpFile, undos);
                                                    break;
                                                case AssetType.TBIND:
                                                    container.AddTbindDependencies(newFile.EtrFile, undos);
                                                    break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            //There was an overflow of textures
                                            //Since we haven't added the new asset to the main list yet we dont need to revert anything.
                                            throw new InvalidOperationException(String.Format("{0}\n\nThe asset was not imported.", ex.Message));
                                        }
                                    }

                                    newAsset.RefreshNamePreview();

                                    container.AddAsset(newAsset);
                                    undos.Add(new UndoableListAdd<Asset>(container.Assets, newAsset));

                                    controller.SetProgress(addedCount);
                                    controller.SetMessage(String.Format("Importing assets: {0} of {1}.", addedCount, assetSelector.SelectedAssets.Count));

                                    addedCount++;
                                }
                                else
                                {
                                    alreadyExistCount++;
                                }
                            }


                        });


                        await controller.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        await controller.CloseAsync();
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }

                }

                if (alreadyExistCount > 0)
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

                //Add Undos
                if (addedCount > 0)
                {
                    undos.Add(new UndoActionDelegate(container, nameof(container.UpdateAssetFilter), true));
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Asset"));
                }
            }

            container.RefreshAssetCount();
        }

        private void AssetContainer_PasteOverReplaceAsset(Asset asset, AssetContainerTool container, AssetType type)
        {
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

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (newAsset.Count == 1)
            {
                //back up of original asset data, so it can be restored in the event of an error
                var oldFiles = asset.Files;
                var selectedAsset = newAsset;

                foreach (var file in selectedAsset[0].Files)
                {
                    string newName = container.GetUnusedName(file.FullFileName);
                    undos.Add(new UndoableProperty<EffectFile>(nameof(file.FileName), file, file.FileName, Path.GetFileNameWithoutExtension(newName)));
                    file.SetName(newName);

                    try
                    {
                        switch (type)
                        {
                            case AssetType.PBIND:
                                container.AddPbindDependencies(file.EmpFile, undos);
                                break;
                            case AssetType.TBIND:
                                container.AddTbindDependencies(file.EtrFile, undos);
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

                undos.Add(new UndoableProperty<Asset>(nameof(asset.Files), asset, asset.Files, selectedAsset[0].Files));
                undos.Add(new UndoActionDelegate(asset, nameof(asset.RefreshNamePreview), true));

                asset.Files = selectedAsset[0].Files;
                asset.RefreshNamePreview();

                effectContainerFile.AssetRefDetailsRefresh(asset);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Over"));
            }
        }

        private void AssetContainer_MergeAssets(Asset primeAsset, List<Asset> selectedAssets, AssetContainerTool container, AssetType type)
        {

            if (primeAsset != null && selectedAssets.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                int count = selectedAssets.Count + 1;

                if (MessageBox.Show(string.Format("All currently selected assets will be MERGED into {0}.\n\nAll other selected assets will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", primeAsset.FileNamesPreview), string.Format("Merge ({0} assets)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    foreach (var assetToRemove in selectedAssets)
                    {
                        effectContainerFile.AssetRefRefactor(assetToRemove, primeAsset, undos);
                        undos.Add(new UndoableListRemove<Asset>(container.Assets, assetToRemove));
                        container.Assets.Remove(assetToRemove);
                    }
                }

                container.RefreshAssetCount();

                undos.Add(new UndoActionDelegate(container, nameof(container.RefreshAssetCount), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge"));
            }
            else
            {
                MessageBox.Show("Cannot merge with less than 2 assets selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AssetContainer_DeleteAsset(List<Asset> assets, AssetContainerTool container)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (MessageBox.Show(String.Format("{0} asset(s) will be deleted. Any EffectParts that are linked to them will also be deleted.\n\nDo you want to continue?", assets.Count), "Delete Asset(s)", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                foreach (var asset in assets)
                {
                    effectContainerFile.AssetRefRefactor(asset, null, undos); //Remove references to this asset
                    undos.Add(new UndoableListRemove<Asset>(container.Assets, asset));
                    container.Assets.Remove(asset);

                    if (asset.assetType == AssetType.PBIND)
                    {
                        CloseEmpForm(asset.Files[0].EmpFile);
                    }
                }

                container.RefreshAssetCount();

                undos.Add(new UndoActionDelegate(container, nameof(container.RefreshAssetCount), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Asset"));
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

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (assets != null)
            {
                int alreadyExistCount = 0;
                int copied = 0;

                foreach (var asset in assets)
                {
                    if (container.AssetExists(asset) == null)
                    {
                        copied++;
                        container.AddAsset(asset, undos);
                    }
                    else
                    {
                        alreadyExistCount++;
                    }
                }

                if (alreadyExistCount > 0 && copied == 0)
                {
                    MessageBox.Show("All copied assets already exist. Copy failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (alreadyExistCount > 0 && copied > 0)
                {
                    MessageBox.Show(String.Format("{0} assets were skipped as they already exist.", alreadyExistCount), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                container.RefreshAssetCount();

                undos.Add(new UndoActionDelegate(container, nameof(container.RefreshAssetCount), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste"));
            }
        }

        private void AssetContainer_DuplicateAsset(List<Asset> assets, AssetContainerTool container, AssetType assetType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var asset in assets)
            {
                Asset newAsset = asset.Clone();
                newAsset.InstanceID = Guid.NewGuid();
                newAsset.assetType = assetType;

                foreach (var file in newAsset.Files)
                {
                    file.SetName(container.GetUnusedName(file.FullFileName));
                }

                container.Assets.Add(newAsset);

                undos.Add(new UndoableListAdd<Asset>(container.Assets, newAsset));
            }

            container.RefreshAssetCount();

            undos.Add(new UndoActionDelegate(container, nameof(container.RefreshAssetCount), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Duplicate Asset"));
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
                Forms.LogForm logForm = new Forms.LogForm("The following effects use this asset:", str.ToString(), String.Format("{0}: Used By", asset.FileNamesPreviewWithExtension), App.Current.MainWindow, true);
                logForm.Show();
            }
            else
            {
                Forms.LogForm logForm = new Forms.LogForm("The following effects use this asset:", str.ToString(), String.Format("{0}: Used By", asset.FileNamesPreview), App.Current.MainWindow, true);
                logForm.Show();
            }


        }

        private void AssetContainer_RenameFile(EffectFile effectFile, Asset asset, AssetContainerTool container)
        {
            Forms.RenameForm renameForm = new Forms.RenameForm(effectFile.FileName, effectFile.Extension, String.Format("Renaming {0}", effectFile.FullFileName), container, App.Current.MainWindow);
            renameForm.ShowDialog();

            if (renameForm.WasNameChanged)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoableProperty<EffectFile>(nameof(effectFile.FileName), effectFile, effectFile.FileName, renameForm.NameValue));
                undos.Add(new UndoActionDelegate(asset, nameof(asset.RefreshNamePreview), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Rename Asset"));

                effectFile.FileName = renameForm.NameValue;

                effectContainerFile.AssetRefDetailsRefresh(container.GetAssetByFileInstance(effectFile));
            }
        }

        private void AssetContainer_RemoveUnusedAssets(AssetType type)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            int amountRemoved = effectContainerFile.RemoveUnusedAssets(type, undos);

            if (amountRemoved > 0)
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Remove Unusued ({type})"));
                MessageBox.Show(String.Format("{0} unused assets were removed.", amountRemoved), "Remove Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("There are no unused assets.", "Remove Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshCounts()
        {
            effectContainerFile.Emo.RefreshAssetCount();
            effectContainerFile.Pbind.RefreshAssetCount();
            effectContainerFile.Tbind.RefreshAssetCount();
            effectContainerFile.Cbind.RefreshAssetCount();
            effectContainerFile.LightEma.RefreshAssetCount();
        }

#endregion


#region EFFECT
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

        public RelayCommand Effect_AddEffectPart_Command => new RelayCommand(Effect_AddEffectPart, IsEffectSelected);
        private void Effect_AddEffectPart()
        {
            try
            {
                var selectedEffect = effectDataGrid.SelectedItem as Effect;

                if (selectedEffect != null)
                {
                    if (selectedEffect.EffectParts == null)
                        selectedEffect.EffectParts = new AsyncObservableCollection<EffectPart>();

                    var newEffectPart = EffectPart.NewEffectPart();
                    selectedEffect.EffectParts.Add(newEffectPart);

                    UndoManager.Instance.AddUndo(new UndoableListAdd<EffectPart>(selectedEffect.EffectParts, newEffectPart, "New EffectPart"));
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Effect_Copy_Command => new RelayCommand(Effect_Copy, IsEffectSelected);
        private void Effect_Copy()
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
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Effect_Paste_Command => new RelayCommand(Effect_Paste, CanPasteEffect);
        private void Effect_Paste()
        {
            try
            {
                List<Effect> effects = (List<Effect>)Clipboard.GetData(ClipboardDataTypes.Effect);

                if (effects != null)
                {
                    //Add effects
                    EffectOptions_ImportEffects(new AsyncObservableCollection<Effect>(effects));

                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Effect_DeleteEffect_Command => new RelayCommand(Effect_DeleteEffect, IsEffectSelected);
        private void Effect_DeleteEffect()
        {
            try
            {
                var selectedEffects = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

                if (selectedEffects.Count > 0)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var _effect in selectedEffects)
                    {
                        undos.Add(new UndoableListRemove<Effect>(effectContainerFile.Effects, _effect));
                        effectContainerFile.Effects.Remove(_effect);
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Effects"));
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Effect_Duplicate_Command => new RelayCommand(Effect_Duplicate, IsEffectSelected);
        private void Effect_Duplicate()
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

                    UndoManager.Instance.AddUndo(new UndoableListAdd<Effect>(effectContainerFile.Effects, copiedEffect, "Duplicate Effect"));
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_Paste_Command => new RelayCommand(EffectPart_Paste, CanPasteEffectPart);
        private void EffectPart_Paste()
        {
            try
            {
                if (effectContainerFile.SelectedEffect != null)
                {
                    ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                    if (effectParts != null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach (var effectPart in effectParts)
                        {
                            if (effectPart.AssetRef != null)
                                effectPart.AssetRef = effectContainerFile.AddAsset(effectPart.AssetRef, effectPart.I_02, undos);

                            var newEffectPart = effectPart.Clone();
                            effectContainerFile.SelectedEffect.EffectParts.Add(newEffectPart);
                            effectContainerFile.RefreshAssetCounts();

                            undos.Add(new UndoableListAdd<EffectPart>(effectContainerFile.SelectedEffect.EffectParts, newEffectPart));
                        }

                        undos.Add(new UndoActionDelegate(effectContainerFile, nameof(effectContainerFile.RefreshAssetCounts), true));
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, (effectParts.Count > 1) ? "Paste EffectParts" : "Paste EffectPart"));
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured while pasting the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_Copy_Command => new RelayCommand(EffectPart_Copy, IsEffectPartSelected);
        private void EffectPart_Copy()
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
                MessageBox.Show(String.Format("An error occured while copying the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_Delete_Command => new RelayCommand(EffectPart_Delete, IsEffectPartSelected);
        private void EffectPart_Delete()
        {
            try
            {
                var selectedEffect = effectContainerFile.SelectedEffect;

                if (selectedEffect != null)
                {
                    if (selectedEffect.SelectedEffectParts != null)
                    {
                        List<EffectPart> effectPartsToRemove = selectedEffect.SelectedEffectParts.ToList();
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach (var effectPart in effectPartsToRemove)
                        {
                            undos.Add(new UndoableListRemove<EffectPart>(selectedEffect.EffectParts, effectPart));
                            selectedEffect.EffectParts.Remove(effectPart);
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, (selectedEffect.SelectedEffectParts.Count > 1) ? "Delete EffectParts" : "Delete EffectPart"));
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_Duplicate_Command => new RelayCommand(EffectPart_Duplicate, IsEffectPartSelected);
        private void EffectPart_Duplicate()
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
                            var clone = effectPart.Clone();
                            selectedEffect.EffectParts.Add(clone);
                            UndoManager.Instance.AddUndo(new UndoableListAdd<EffectPart>(selectedEffect.EffectParts, clone, "Duplicate"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while duplicating the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_GoToAsset_Command => new RelayCommand(EffectPart_GoToAsset, CanGoToAsset);
        private void EffectPart_GoToAsset()
        {
            try
            {
                var selectedEffectPart = effectContainerFile.GetSelectedEffectParts();

                if (selectedEffectPart != null)
                {
                    if (selectedEffectPart.Count > 0)
                    {
                        switch (selectedEffectPart[0].I_02)
                        {
                            case AssetType.PBIND:
                                tabControl.SelectedIndex = (int)Tabs.Pbind;
                                pbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                pbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.TBIND:
                                tabControl.SelectedIndex = (int)Tabs.Tbind;
                                tbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                tbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.CBIND:
                                tabControl.SelectedIndex = (int)Tabs.Cbind;
                                cbindDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                cbindDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.EMO:
                                tabControl.SelectedIndex = (int)Tabs.Emo;
                                emoDataGrid.SelectedItem = selectedEffectPart[0].AssetRef;
                                emoDataGrid.ScrollIntoView(selectedEffectPart[0].AssetRef);
                                break;
                            case AssetType.LIGHT:
                                tabControl.SelectedIndex = (int)Tabs.Light;
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand EffectPart_ChangeAsset_Command => new RelayCommand(EffectPart_ChangeAsset, IsEffectPartSelected);
        private void EffectPart_ChangeAsset()
        {
            if (effectContainerFile.SelectedEffect != null)
            {
                if (effectContainerFile.SelectedEffect.SelectedEffectPart != null)
                {
                    Forms.AssetSelector assetSel = new Forms.AssetSelector(effectContainerFile, false, false, effectContainerFile.SelectedEffect.SelectedEffectPart.I_02, this, effectContainerFile.SelectedEffect.SelectedEffectPart.AssetRef);
                    assetSel.ShowDialog();

                    if(assetSel.SelectedAsset != null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach (var effectPart in effectContainerFile.SelectedEffect.SelectedEffectParts)
                        {
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.I_02), effectPart, effectPart.I_02, assetSel.SelectedAssetType));
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetRef), effectPart, effectPart.AssetRef, assetSel.SelectedAsset));

                            effectPart.I_02 = assetSel.SelectedAssetType;
                            effectPart.AssetRef = assetSel.SelectedAsset;
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Change Asset(s)"));

                        if (effectPartViewModel != null)
                            effectPartViewModel.UpdateProperties();
                    }
                }
            }
        }

        public RelayCommand EffectPart_PasteValues_Command => new RelayCommand(EffectPart_PasteValues, CanPasteEffectPartValues);
        private void EffectPart_PasteValues()
        {
            try
            {
                if (effectContainerFile.SelectedEffect == null) return;
                if (effectContainerFile.SelectedEffect.SelectedEffectPart == null) return;

                ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                if (effectParts == null) return;
                if (effectParts.Count != 1) return;

                List<IUndoRedo> undos = new List<IUndoRedo>();

                effectParts[0].AssetRef = effectContainerFile.AddAsset(effectParts[0].AssetRef, effectParts[0].I_02, undos);
                effectContainerFile.SelectedEffect.SelectedEffectPart.CopyValues(effectParts[0], undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Values"));

                if (_effectPartViewModel != null)
                    _effectPartViewModel.UpdateProperties();
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while copying the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectPart_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CanGoToAsset())
                EffectPart_GoToAsset();
        }

        public RelayCommand Effect_Export_Command => new RelayCommand(Effect_Export, IsEffectSelected);
        private void Effect_Export()
        {
            try
            {
                var selectedEffects = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

                if (selectedEffects.Count > 0)
                {
                    ExportVfxPackage(selectedEffects);
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An unknown error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Relay EffectPart Events (ContextMenu cannot be binded because WPF is stupid and today I do not feel like banging my head against the wall trying to figure it out, so events must be used)
        private void EffectPart_Paste_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_Paste();
            e.Handled = true;
        }

        private void EffectPart_PasteValues_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_PasteValues();
            e.Handled = true;
        }

        private void EffectPart_Copy_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_Copy();
            e.Handled = true;
        }

        private void EffectPart_Delete_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_Delete();
            e.Handled = true;
        }

        private void EffectPart_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_Duplicate();
            e.Handled = true;
        }

        private void EffectPart_GoToAsset_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_GoToAsset();
            e.Handled = true;
        }

        private void EffectPart_ChangeAsset_Click(object sender, RoutedEventArgs e)
        {
            EffectPart_ChangeAsset();
            e.Handled = true;
        }

        private async void EffectPart_Rescale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (effectContainerFile.SelectedEffect != null)
                {
                    if (effectContainerFile.SelectedEffect.SelectedEffectParts != null)
                    {
                        var result = await DialogCoordinator.Instance.ShowInputAsync(Application.Current.MainWindow, "Rescale Factor", "Rescale the Min and Max values on the selected EffectParts (does not edit the underlying assets at all). \n\nEnter the factor to rescale by:", DialogSettings.Default);

                        if(!float.TryParse(result, out float scaleFactor))
                        {
                            await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "Invalid Input", "Only numbers are valid.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                            return;
                        }

                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach(var effectPart in effectContainerFile.SelectedEffect.SelectedEffectParts)
                        {
                            float size1 = effectPart.SIZE_1 * scaleFactor;
                            float size2 = effectPart.SIZE_2 * scaleFactor;

                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.SIZE_1), effectPart, effectPart.SIZE_1, size1));
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.SIZE_2), effectPart, effectPart.SIZE_2, size2));

                            effectPart.SIZE_1 = size1;
                            effectPart.SIZE_2 = size2;
                        }

                        UndoManager.Instance.AddCompositeUndo(undos, "Rescale EffectPart");
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured while rescaling the EffectParts.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
        }


        //Effect Options
        public RelayCommand EffectOptions_AddEffect_Command => new RelayCommand(EffectOptions_AddEffect);
        private void EffectOptions_AddEffect()
        {
            try
            {
                Effect newEffect = new Effect();
                newEffect.EffectParts = new AsyncObservableCollection<EffectPart>();
                newEffect.IndexNum = effectContainerFile.GetUnusedEffectId(0);
                effectContainerFile.Effects.Add(newEffect);
                effectDataGrid.SelectedItem = newEffect;
                effectDataGrid.Items.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                effectDataGrid.ScrollIntoView(newEffect);

                effectContainerFile.UpdateEffectFilter();

                //Undo
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoableListAdd<Effect>(effectContainerFile.Effects, newEffect));
                undos.Add(new UndoActionDelegate(effectContainerFile, nameof(effectContainerFile.UpdateEffectFilter), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New Effect"));
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public async void EffectOptions_ImportEffectsFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var importEffectFile = await LoadEffectContainerFile();
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Character);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromSuper_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.SuperSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromUltimate_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.UltimateSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromEvasive_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.EvasiveSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromBlast_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.BlastSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromAwoken_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.AwokenSkill);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromCMN_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.CMN);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void EffectOptions_ImportEffectsFromDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!GameDirectoryCheck()) return;
            try
            {
                var importEffectFile = await LoadEepkFromGame(Forms.EntitySelector.EntityType.Demo);
                if (importEffectFile != null)
                    EffectOptions_ImportEffects(importEffectFile.Effects);
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EffectOptions_ImportEffectsFromCache_Click(object sender, RoutedEventArgs e)
        {
            try
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
                        MessageBox.Show("There are no cached files.", "From Cached Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EffectOptions_ImportEffects(AsyncObservableCollection<Effect> effects)
        {
            if (effects != null)
            {
                Forms.EffectSelector effectSelector = new Forms.EffectSelector(effects, effectContainerFile, Application.Current.MainWindow);
                effectSelector.ShowDialog();
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (effectSelector.SelectedEffects != null)
                {
                    foreach (var effect in effectSelector.SelectedEffects)
                    {
                        var newEffect = effect.Clone();
                        newEffect.IndexNum = effect.ImportIdIncrease;
                        undos.AddRange(effectContainerFile.AddEffect(newEffect, true));
                        undos.Add(new UndoableListAdd<Effect>(effectContainerFile.Effects, newEffect));
                    }

                    //Update UI
                    if (effectSelector.SelectedEffects.Count > 0)
                    {
                        RefreshCounts();
                        effectDataGrid.SelectedItem = effectSelector.SelectedEffects[0];
                        effectDataGrid.ScrollIntoView(effectSelector.SelectedEffects);
                        effectContainerFile.UpdateEffectFilter();
                        undos.Add(new UndoActionDelegate(effectContainerFile, nameof(effectContainerFile.UpdateAllFilters), true));
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Effects"));
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
            //This may not be required anymore. Need to test. For now just leave disable the function with a return.
            return;
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
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("ListBox_MouseRightButtonUp: Failed to force select parent effect.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EffectOptions_ImportEffects_FileDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                    if (droppedFilePaths.Length == 1)
                    {
                        switch (Path.GetExtension(droppedFilePaths[0]))
                        {
                            case EffectContainerFile.ZipExtension:
                            case ".eepk":
                                var importEffectFile = await LoadEffectContainerFile(droppedFilePaths[0]);
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
                MessageBox.Show(Application.Current.MainWindow, String.Format("The dropped file could not be opened.\n\nThe reason given by the system: {0}", ex.Message), "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        //CanExecutes
        public bool CanGoToAsset()
        {
            if (IsEffectPartSelected())
            {
                return effectContainerFile.SelectedEffect.SelectedEffectPart.AssetRef != null;
            }
            return false;
        }

        public bool IsEffectPartSelected()
        {
            return effectContainerFile?.SelectedEffect?.SelectedEffectPart != null;
        }

        public bool IsEffectSelected()
        {
            return effectDataGrid.SelectedItem is Xv2CoreLib.EEPK.Effect;
        }

        public bool CanPasteEffect()
        {
            return Clipboard.ContainsData(ClipboardDataTypes.Effect);
        }

        public bool CanPasteEffectPart()
        {
            return Clipboard.ContainsData(ClipboardDataTypes.EffectPart);
        }
        public bool CanPasteEffectPartValues()
        {
            return (IsEffectPartSelected()) ? Clipboard.ContainsData(ClipboardDataTypes.EffectPart) : false;
        }
#endregion



        //Window Finding
        public Window GetActiveForm<T>() where T : Window
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is T)
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

        public Forms.MaterialsEditorForm GetActiveEmmForm(EMM_File _emmFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is Forms.MaterialsEditorForm)
                {
                    Forms.MaterialsEditorForm _form = (Forms.MaterialsEditorForm)window;

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

        public bool GameDirectoryCheck()
        {
            if (!SettingsManager.settings.ValidGameDir)
            {
                MessageBox.Show("Please set the game directory in the settings menu to use this option (File > Settings > Game Directory).", "Invalid Game Directory", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        //Export
        public void ExportVfxPackage(IList<Effect> selEffects = null)
        {
            if (effectContainerFile == null) return;

            try
            {
                //if selEffects is null, then pass in all the effects.
                IList<Effect> effects = (selEffects != null) ? selEffects : effectContainerFile.Effects;

                Forms.EffectSelector effectSelector = new Forms.EffectSelector(effects, effectContainerFile, App.Current.MainWindow, Forms.EffectSelector.Mode.ExportEffect);
                effectSelector.ShowDialog();

                if (effectSelector.SelectedEffects != null)
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

                    if (saveDialog.ShowDialog() == true)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_MouseMove(object sender, MouseEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            menuItem.IsSubmenuOpen = true;
        }

        //Filtering
        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
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

        public RelayCommand SearchCommand => new RelayCommand(Search);
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

        private void effectPart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateEffectPartViewModel();
        }

        private void CreateEffectPartViewModel()
        {
            if (effectContainerFile?.SelectedEffect?.SelectedEffectPart != null)
            {
                if (effectPartViewModel != null) effectPartViewModel.Dispose();

                _effectPartViewModel = new EffectPartViewModel(effectContainerFile?.SelectedEffect?.SelectedEffectPart);
            }
            else if (effectPartViewModel != null)
            {
                effectPartViewModel.Dispose();
                _effectPartViewModel = null;
            }

            NotifyPropertyChanged(nameof(effectPartViewModel));
        }




        //Hotkeys (to be replaced with commands)

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

        private void effectPartTabs_ExpandedOrCollapsed(object sender, RoutedEventArgs e)
        {
            if (SettingsManager.settings == null || !effectPartTabInitialize) return;
            SettingsManager.settings.EepkOrganiser_EffectPart_General_Expanded = effectPartGeneral.IsExpanded;
            SettingsManager.settings.EepkOrganiser_EffectPart_Position_Expanded = effectPartPos.IsExpanded;
            SettingsManager.settings.EepkOrganiser_EffectPart_Animation_Expanded = effectPartAnimation.IsExpanded;
            SettingsManager.settings.EepkOrganiser_EffectPart_Flags_Expanded = effectPartFlags.IsExpanded;
            SettingsManager.settings.EepkOrganiser_EffectPart_UnkFlags_Expanded = effectPartUnkFlags.IsExpanded;
            SettingsManager.settings.EepkOrganiser_EffectPart_UnkValues_Expanded = effectPartUnkValues.IsExpanded;
        }

        //Tool Button
        private void ToolMenu_HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                Forms.RecolorAll recolor = new Forms.RecolorAll(effectContainerFile, App.Current.MainWindow);

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

        private void ToolMenu_HueSet_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                RecolorAll_HueSet recolor = new RecolorAll_HueSet(effectContainerFile, App.Current.MainWindow);

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
    }
}