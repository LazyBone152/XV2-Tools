using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using Application = System.Windows.Application;
using Microsoft.Win32;
using Xv2CoreLib;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.ESK;
using Xv2CoreLib.EMO;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource;
using EEPK_Organiser.Misc;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Forms.Editors;
using EEPK_Organiser.Forms.Recolor;
using EEPK_Organiser.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;

#if XenoKit
using XenoKit;
using XenoKit.Engine;
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

            if(effectContainerFile != null)
                nameListManager.EepkLoaded(effectContainerFile);
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

        //Selected Effect
        private Effect _selectedEffect = null;
        public Effect SelectedEffect
        {
            get => _selectedEffect;
            set
            {
                _selectedEffect = value;
                NotifyPropertyChanged(nameof(SelectedEffect));
                NotifyPropertyChanged(nameof(SelectedEffectID));
            }
        }
        public int SelectedEffectID
        {
            get => SelectedEffect != null ? SelectedEffect.IndexNum : 0;
            set
            {
                if(SelectedEffect != null && SelectedEffect?.IndexNum != value)
                {
                    if(effectContainerFile.Effects.FirstOrDefault(x => x.IndexNum == value) == null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();
                        undos.Add(new UndoablePropertyGeneric(nameof(SelectedEffect.IndexNum), SelectedEffect, SelectedEffect.IndexNum, (ushort)value));
                        undos.Add(new UndoActionDelegate(effectContainerFile, nameof(EffectContainerFile.UpdateEffectFilter), true));

                        SelectedEffect.IndexNum = (ushort)value;

                        UndoManager.Instance.AddCompositeUndo(undos, "Effect ID");
                        NotifyPropertyChanged(nameof(SelectedEffectID));
                        effectContainerFile.UpdateEffectFilter();

                        effectDataGrid.ScrollIntoView(SelectedEffect);
                    }
                    else
                    {
                        MessageBox.Show($"This ID ({value}) is already used for another effect.", "ID Used", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
        }

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

#if XenoKit
        public Visibility XenoKitVisible => Visibility.Visible;
#else
        public Visibility XenoKitVisible => Visibility.Collapsed;
#endif

        public EepkEditor()
        {
            InitializeComponent();
            rootGrid.DataContext = this;

            //Load NameLists
            nameListManager = new NameList.NameListManager();

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
                            loadedFile = EffectContainerFile.LoadVfxPackage(path);
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
            SelectedEffectTabChanged?.Invoke(this, EventArgs.Empty);
            if (effectContainerFile == null) return;

            switch ((Tabs)tabControl.SelectedIndex)
            {
                case Tabs.Effect:
                    SearchFilter = effectContainerFile.EffectSearchFilter;
                    break;
                case Tabs.Pbind:
                    SearchFilter = effectContainerFile.Pbind.AssetSearchFilter;
                    pbindDataGrid_SelectionChanged(null, null);
                    break;
                case Tabs.Tbind:
                    SearchFilter = effectContainerFile.Tbind.AssetSearchFilter;
                    tbindDataGrid_SelectionChanged(null, null);
                    break;
                case Tabs.Cbind:
                    SearchFilter = effectContainerFile.Cbind.AssetSearchFilter;
                    cbindDataGrid_SelectionChanged(null, null);
                    break;
                case Tabs.Emo:
                    SearchFilter = effectContainerFile.Emo.AssetSearchFilter;
                    emoDataGrid_SelectionChanged(null, null);
                    break;
                case Tabs.Light:
                    SearchFilter = effectContainerFile.LightEma.AssetSearchFilter;
                    lightDataGrid_SelectionChanged(null, null);
                    break;
            }

            e.Handled = true;
        }

#region EMO

        private void EMO_AssetContainer_NewAsset_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var asset = Asset.Create(AssetType.EMO);
            effectContainerFile.Emo.AddAsset(asset, undos);
            effectContainerFile.Emo.RefreshAssetCount();
            emoDataGrid.SelectedItem = asset;
            emoDataGrid.ScrollIntoView(asset);

            //Undos
            undos.Add(new UndoActionDelegate(effectContainerFile.Emo, nameof(effectContainerFile.Emo.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMO Asset"));
        }

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
                            if (EffectFile.GetExtension(openFile.FileName) != selectedFile.Extension)
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

        private async void OpenEmoEffectFileEditor(EffectFile selectedFile, bool showError)
        {
            if (selectedFile != null)
            {
                bool focus = false;
                Window window = null;

                switch (selectedFile.fileType)
                {
                    case EffectFile.FileType.EMB:
                        {
                            window = GetActiveEmbForm(selectedFile.EmbFile);

                            if (window == null)
                            {
                                window = new Forms.EmbEditForm(selectedFile.EmbFile, null, AssetType.EMO, selectedFile.FullFileName);
                            }
                            else
                            {
                                focus = true;
                            }
                        }
                        break;
                    case EffectFile.FileType.EMM:
                        {
                            window = GetActiveEmmForm(selectedFile.EmmFile);

                            if (window == null)
                            {
                                window = new Forms.MaterialsEditorForm(selectedFile.EmmFile, null, AssetType.EMO, selectedFile.FullFileName);
                            }
                            else
                            {
                                focus = true;
                            }
                        }
                        break;
                    default:
                        if(showError)
                            MessageBox.Show(string.Format("Edit not possible for {0} files.", selectedFile.Extension), "Edit", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                }

                if(window != null)
                {
                    await Task.Delay(100);

                    if (focus)
                    {
                        window.Focus();
                    }
                    else
                    {
                        window.Show();
                    }
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
                        Files = new AsyncObservableCollection<EffectFile>()
                    };

                    foreach (string file in openFile.FileNames)
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
                            case EffectFile.FileType.EMA:
                                asset.AddFile(EMA_File.Load(file), newName, EffectFile.FileType.EMA);
                                break;
                            case EffectFile.FileType.EMO:
                                asset.AddFile(EMO_File.Load(file), newName, EffectFile.FileType.EMO);
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

        private async void EMO_AssetContainer_AddEmd_Click(object sender, RoutedEventArgs e)
        {
            var asset = emoDataGrid.SelectedItem as Asset;
            if (asset == null) return;

            if (asset.Files.Any(x => x.fileType == EffectFile.FileType.EMB) || asset.Files.Any(x => x.fileType == EffectFile.FileType.EMM) || asset.Files.Any(x => x.fileType == EffectFile.FileType.EMO))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "File Already Exists", $"An EMO, EMB or EMM already exists on the selected asset. Please delete it/them to complete the conversion.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "EMD + ESK -> EMO conversion";
            openFile.Filter = "EMD and ESK | *.emd; *.esk";
            openFile.Multiselect = true;

            if (openFile.ShowDialog() == true)
            {
                ESK_File eskFile = null;
                List<EMD_File> emdFiles = new List<EMD_File>();
                List<EMB_File> embFiles = new List<EMB_File>();
                List<EMB_File> dytFiles = new List<EMB_File>();
                List<EMM_File> emmFiles = new List<EMM_File>();

                bool hasDyt = false;


                foreach (var file in openFile.FileNames)
                {
                    if(Path.GetExtension(file) == ".emd")
                    {
                        string emmPath = string.Format("{0}/{1}.emm", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                        string embPath = string.Format("{0}/{1}.emb", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                        string dytPath = string.Format("{0}/{1}.dyt.emb", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                        if (!File.Exists(emmPath))
                        {
                            await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "File Not Found", $"Could not find \"{emmPath}\".\n\nPlease note that each EMD file must have a EMM and EMB in the same directory.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                            return;
                        }

                        if (!File.Exists(embPath))
                        {
                            await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "File Not Found", $"Could not find \"{embPath}\".\n\nPlease note that each EMD file must have a EMM and EMB in the same directory.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                            return;
                        }

                        if (!File.Exists(dytPath) && hasDyt)
                        {
                            await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "File Not Found", $"Could not find \"{dytPath}\".\n\nIf at least one of the EMD files have a dyt file, then one is required for each EMD.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                            return;
                        }

                        EMD_File emd = EMD_File.Load(file);
                        EMB_File emb = EMB_File.LoadEmb(embPath);
                        EMM_File emm = EMM_File.LoadEmm(emmPath);
                        EMB_File dyt = null;

                        emdFiles.Add(emd);
                        embFiles.Add(emb);
                        emmFiles.Add(emm);

                        if (File.Exists(dytPath))
                        {
                            hasDyt = true;
                            dyt = EMB_File.LoadEmb(dytPath);
                            dytFiles.Add(dyt);
                        }

                    }
                    else if (Path.GetExtension(file) == ".esk")
                    {
                        eskFile = ESK_File.Load(file);
                    }

                }

                //Validation
                if(emdFiles.Count == 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "No EMDs", $"No EMD files were selected/loaded, so no EMO can be created.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                if (eskFile == null)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "No ESK", $"No ESK file was selected/loaded, so no EMO can be created.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                EMB_File mergedEmb;
                EMM_File mergedEmm;
                EMO_File emoFile = EMO_File.ConvertToEmo(emdFiles.ToArray(), embFiles.ToArray(), dytFiles.ToArray(), emmFiles.ToArray(), eskFile, out mergedEmb, out mergedEmm);

                List<IUndoRedo> undos = new List<IUndoRedo>();

                string name = Path.GetFileNameWithoutExtension(openFile.FileName);

                asset.AddFile(emoFile, $"{name}.emo", EffectFile.FileType.EMO, undos);
                asset.AddFile(mergedEmb, $"{name}.emb", EffectFile.FileType.EMB, undos);
                asset.AddFile(mergedEmm, $"{name}.emm", EffectFile.FileType.EMM, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "EMD -> EMO Conversion");
            }

        }

        private async void EMO_AssetContainer_AddEan_Click(object sender, RoutedEventArgs e)
        {
            //Completely fucked

            var asset = emoDataGrid.SelectedItem as Asset;
            if (asset == null) return;

            if (asset.Files.Any(x => x.Extension == ".obj.ema"))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "File Already Exists", $"An OBJ.EMA already exists on the selected asset.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "EAN -> EMA conversion";
            openFile.Filter = "EAN files | *.ean";

            if (openFile.ShowDialog() == true)
            {
                /*
                EAN_File eanFile = EAN_File.Load(openFile.FileName);
                EMA_File emaFile = EMA_File.ConvertToEma(eanFile);

                List<IUndoRedo> undos = new List<IUndoRedo>();
                asset.AddFile(emaFile, $"{Path.GetFileNameWithoutExtension(openFile.FileName)}.obj.ema", EffectFile.FileType.EMA, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "EAN -> EMA");
                */
            }
        }

        private void EMO_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            AssetContainer_OpenSettings(effectContainerFile.Emo);
        }

        //EMO Files
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
            MaterialsEditorForm emmForm = GetActiveEmmForm(effectContainerFile.Pbind.File2_Ref);

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
            Asset selectedAsset = pbindDataGrid.SelectedItem as Asset;

            if (selectedAsset != null)
            {
                PBIND_OpenEditor(selectedAsset.Files[0].EmpFile, effectContainerFile.Pbind, selectedAsset.Files[0].FullFileName);
            }
        }

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

        public static EmbEditForm PBIND_OpenTextureViewer(AssetContainerTool assetContainer, AssetType assetType)
        {
            EmbEditForm form = GetActiveEmbForm(assetContainer.File3_Ref);

            if (form == null)
            {
                form = new EmbEditForm(assetContainer.File3_Ref, assetContainer, assetType, assetType.ToString());
                form.Show();
            }

            form.Focus();
            return form;
        }

        public static MaterialsEditorForm PBIND_OpenMaterialEditor(AssetContainerTool assetContainer, AssetType assetType)
        {
            MaterialsEditorForm form = GetActiveEmmForm(assetContainer.File2_Ref);

            if(form == null)
            {
                form = new MaterialsEditorForm(assetContainer.File2_Ref, assetContainer, assetType, assetType.ToString());
                form.Show();
            }

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

        private void PBIND_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            AssetContainer_OpenSettings(effectContainerFile.Pbind);
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

        private void TBIND_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            AssetContainer_OpenSettings(effectContainerFile.Tbind);
        }

        private void TBIND_AssetContainer_Edit_Click(object sender, RoutedEventArgs e)
        {
            Asset selectedAsset = tbindDataGrid.SelectedItem as Asset;

            if (selectedAsset != null)
            {
                TBIND_OpenEditor(selectedAsset.Files[0].EtrFile, effectContainerFile.Tbind, selectedAsset.Files[0].FileName);
            }
        }

        private void TBIND_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TBIND_AssetContainer_Edit_Click(null, null);
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

        private void TBIND_NewEtr_Click(object sender, RoutedEventArgs e)
        {
            ETR_File etrFile = new ETR_File();
            etrFile.Nodes.Add(ETR_Node.GetNew());

            Asset asset = effectContainerFile.Tbind.AddAsset(etrFile, "NewEtr.etr");
            effectContainerFile.Tbind.RefreshAssetCount();
            tbindDataGrid.SelectedItem = asset;
            tbindDataGrid.ScrollIntoView(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Tbind.Assets, asset));
            undos.Add(new UndoActionDelegate(effectContainerFile.Tbind, nameof(effectContainerFile.Tbind.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ETR"));
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

        private void CBIND_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            AssetContainer_OpenSettings(effectContainerFile.Cbind);
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

        private void CBIND_AssetContainer_Edit_Click(object sender, RoutedEventArgs e)
        {
            Asset selectedAsset = cbindDataGrid.SelectedItem as Asset;

            if (selectedAsset != null)
            {
                CBIND_OpenEditor(selectedAsset.Files[0].EcfFile, selectedAsset.Files[0].FileName);
            }
        }

        private void CBIND_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CBIND_AssetContainer_Edit_Click(null, null);
        }

        private void CBIND_NewEcf_Click(object sender, RoutedEventArgs e)
        {
            ECF_File ecfFile = new ECF_File();
            ecfFile.Nodes.Add(new ECF_Node());

            Asset asset = effectContainerFile.Cbind.AddAsset(ecfFile, "NewEcf.ecf");
            effectContainerFile.Cbind.RefreshAssetCount();
            cbindDataGrid.SelectedItem = asset;
            cbindDataGrid.ScrollIntoView(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Cbind.Assets, asset));
            undos.Add(new UndoActionDelegate(effectContainerFile.Cbind, nameof(effectContainerFile.Cbind.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ECF"));
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

        private void LIGHT_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            AssetContainer_OpenSettings(effectContainerFile.LightEma);
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
                    else if (asset.assetType == AssetType.CBIND)
                    {
                        CloseEcfForm(asset.Files[0].EcfFile);
                    }
                    else if (asset.assetType == AssetType.TBIND)
                    {
                        CloseEtrForm(asset.Files[0].EtrFile);
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

        private void AssetContainer_OpenSettings(AssetContainerTool container)
        {
            AssetContainerSettings assetSettings = new AssetContainerSettings(container);
            assetSettings.ShowDialog();
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
        public RelayCommand Effect_Play_Command => new RelayCommand(PlaySelectedEffect, IsEffectSelected);

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
                if (SelectedEffect != null)
                {
                    ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                    if (effectParts != null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        try
                        {
                            foreach (var effectPart in effectParts)
                            {
                                if (effectPart.AssetRef != null)
                                    effectPart.AssetRef = effectContainerFile.AddAsset(effectPart.AssetRef, effectPart.AssetType, undos);

                                var newEffectPart = effectPart.Clone();
                                SelectedEffect.EffectParts.Add(newEffectPart);
                                effectContainerFile.RefreshAssetCounts();

                                undos.Add(new UndoableListAdd<EffectPart>(SelectedEffect.EffectParts, newEffectPart));
                            }

                            undos.Add(new UndoActionDelegate(effectContainerFile, nameof(effectContainerFile.RefreshAssetCounts), true));
                        }
                        finally
                        {
                            if(undos.Count > 0)
                                UndoManager.Instance.AddUndo(new CompositeUndo(undos, (effectParts.Count > 1) ? "Paste EffectParts" : "Paste EffectPart"));
                        }
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
                if (SelectedEffect != null)
                {
                    if (SelectedEffect.SelectedEffectParts != null)
                    {
                        effectContainerFile.SaveDds();
                        Clipboard.SetData(ClipboardDataTypes.EffectPart, SelectedEffect.SelectedEffectParts);
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
                var selectedEffect = SelectedEffect;

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
            var selectedEffect = SelectedEffect;

            if (selectedEffect != null)
            {
                if (selectedEffect.SelectedEffectParts != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var effectPart in selectedEffect.SelectedEffectParts)
                    {
                        var clone = effectPart.Clone();
                        selectedEffect.EffectParts.Add(clone);
                        undos.Add(new UndoableListAdd<EffectPart>(selectedEffect.EffectParts, clone));
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "EffectPart Duplicate");
                }
            }
        }

        public RelayCommand EffectPart_GoToAsset_Command => new RelayCommand(EffectPart_GoToAsset, CanGoToAsset);
        private void EffectPart_GoToAsset()
        {
            try
            {
                var selectedEffectPart = GetSelectedEffectParts();

                if (selectedEffectPart != null)
                {
                    if (selectedEffectPart.Count > 0)
                    {
                        switch (selectedEffectPart[0].AssetType)
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

        public RelayCommand EffectPart_EditAsset_Command => new RelayCommand(EffectPart_OpenEditor, CanEditAsset);
        
        public RelayCommand EffectPart_ChangeAsset_Command => new RelayCommand(EffectPart_ChangeAsset, IsEffectPartSelected);
        private void EffectPart_ChangeAsset()
        {
            if (SelectedEffect != null)
            {
                if (SelectedEffect.SelectedEffectPart != null)
                {
                    Forms.AssetSelector assetSel = new Forms.AssetSelector(effectContainerFile, false, false, SelectedEffect.SelectedEffectPart.AssetType, this, SelectedEffect.SelectedEffectPart.AssetRef);
                    assetSel.ShowDialog();

                    if(assetSel.SelectedAsset != null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach (var effectPart in SelectedEffect.SelectedEffectParts)
                        {
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetType), effectPart, effectPart.AssetType, assetSel.SelectedAssetType));
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetRef), effectPart, effectPart.AssetRef, assetSel.SelectedAsset));

                            effectPart.AssetType = assetSel.SelectedAssetType;
                            effectPart.AssetRef = assetSel.SelectedAsset;
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Change Asset(s)"));

                        if (effectPartViewModel != null)
                            effectPartViewModel.UpdateProperties();
                    }
                }
            }
        }

        public RelayCommand EffectPart_Rescale_Command => new RelayCommand(EffectPart_Rescale, IsEffectPartSelected);
        private async void EffectPart_Rescale()
        {
            if (SelectedEffect != null)
            {
                if (SelectedEffect.SelectedEffectParts != null)
                {
                    var result = await DialogCoordinator.Instance.ShowInputAsync(Application.Current.MainWindow, "Rescale Factor", "Rescale the Min and Max values on the selected EffectParts (does not edit the underlying assets at all). \n\nEnter the factor to rescale by:", DialogSettings.Default);

                    if (!float.TryParse(result, out float scaleFactor))
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(Application.Current.MainWindow, "Invalid Input", "Only numbers are valid.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return;
                    }

                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var effectPart in SelectedEffect.SelectedEffectParts)
                    {
                        float size1 = effectPart.ScaleMin * scaleFactor;
                        float size2 = effectPart.ScaleMax * scaleFactor;

                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.ScaleMin), effectPart, effectPart.ScaleMin, size1));
                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.ScaleMax), effectPart, effectPart.ScaleMax, size2));

                        effectPart.ScaleMin = size1;
                        effectPart.ScaleMax = size2;
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale EffectPart");
                }
            }
        }

        public RelayCommand EffectPart_PasteValues_Command => new RelayCommand(EffectPart_PasteValues, CanPasteEffectPartValues);
        private void EffectPart_PasteValues()
        {
            try
            {
                if (SelectedEffect == null) return;
                if (SelectedEffect.SelectedEffectPart == null) return;

                ObservableCollection<EffectPart> effectParts = (ObservableCollection<EffectPart>)Clipboard.GetData(ClipboardDataTypes.EffectPart);

                if (effectParts == null) return;
                if (effectParts.Count != 1) return;

                List<IUndoRedo> undos = new List<IUndoRedo>();

                effectParts[0].AssetRef = effectContainerFile.AddAsset(effectParts[0].AssetRef, effectParts[0].AssetType, undos);
                SelectedEffect.SelectedEffectPart.CopyValues(effectParts[0], undos);

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
            if (!CanGoToAsset()) return;

            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                EffectPart_OpenEditor();
            }
            else
            {
                EffectPart_GoToAsset();
            }
        }

        private void EffectPart_OpenEditor()
        {
            ObservableCollection<EffectPart> selectedEffectPart = GetSelectedEffectParts();

            if (selectedEffectPart != null)
            {
                if (selectedEffectPart.Count > 0)
                {
                    switch (selectedEffectPart[0].AssetType)
                    {
                        case AssetType.PBIND:
                            PBIND_OpenEditor(selectedEffectPart[0].AssetRef.Files[0].EmpFile, effectContainerFile.Pbind, selectedEffectPart[0].AssetRef.Files[0].FileName);
                            break;
                        case AssetType.TBIND:
                            TBIND_OpenEditor(selectedEffectPart[0].AssetRef.Files[0].EtrFile, effectContainerFile.Tbind, selectedEffectPart[0].AssetRef.Files[0].FileName);
                            break;
                        case AssetType.CBIND:
                            CBIND_OpenEditor(selectedEffectPart[0].AssetRef.Files[0].EcfFile, selectedEffectPart[0].AssetRef.Files[0].FileName);
                            break;
                    }
                }
            }
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

        //Effect Options
        public RelayCommand EffectOptions_AddEffect_Command => new RelayCommand(EffectOptions_AddEffect);
        private void EffectOptions_AddEffect()
        {
            try
            {
                Effect newEffect = new Effect();
                newEffect.IndexNum = effectContainerFile.GetUnusedEffectId(0);
                List<IUndoRedo> undos = effectContainerFile.AddEffect(newEffect, false, true);
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New Effect"));

                effectDataGrid.SelectedItem = newEffect;
                effectDataGrid.ScrollIntoView(newEffect);

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

                try
                {
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
                    }
                }
                finally
                {
                    if(undos.Count > 0)
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Effects"));
                }
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
                return SelectedEffect.SelectedEffectPart.AssetRef != null;
            }
            return false;
        }
        
        public bool CanEditAsset()
        {
            if (IsEffectPartSelected())
            {
                return SelectedEffect.SelectedEffectPart.AssetType == AssetType.CBIND || SelectedEffect.SelectedEffectPart.AssetType == AssetType.TBIND || SelectedEffect.SelectedEffectPart.AssetType == AssetType.PBIND;
            }
            return false;
        }

        public bool IsEffectPartSelected()
        {
            return SelectedEffect?.SelectedEffectPart != null;
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
        public static EmbEditForm GetActiveEmbForm(EMB_File _embFile)
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
            foreach (object window in App.Current.Windows)
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
                    vfxPackage.saveFormat = SaveFormat.VfxPackage;

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
                        vfxPackage.SaveVfxPackage();

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

        private void effectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateEffectPartViewModel();
            PlaySelectedEffect();
        }

        private void PlaySelectedEffect()
        {
#if XenoKit
            if (SelectedEffect != null && SceneManager.MainGameInstance != null)
            {
                SceneManager.MainGameInstance.VfxPreview.PreviewEffect(SelectedEffect);
            }
#endif
        }

        private void CreateEffectPartViewModel()
        {
            if (SelectedEffect?.SelectedEffectPart != null)
            {
                if (effectPartViewModel != null) effectPartViewModel.Dispose();

                _effectPartViewModel = new EffectPartViewModel(SelectedEffect?.SelectedEffectPart);
            }
            else if (effectPartViewModel != null)
            {
                effectPartViewModel.Dispose();
                _effectPartViewModel = null;
            }

            NotifyPropertyChanged(nameof(effectPartViewModel));
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
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Q) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PBIND_AssetContainer_Recolor(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                PBIND_AssetContainer_RecolorHueSet(null, null);
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
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Q) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TBIND_AssetContainer_Recolor(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                TBIND_AssetContainer_RecolorHueSet(null, null);
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
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.T) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Q) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                CBIND_AssetContainer_Recolor(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                CBIND_AssetContainer_RecolorHueSet(null, null);
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
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Q) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EMO_AssetContainer_Recolor(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                EMO_AssetContainer_RecolorHueSet(null, null);
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
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_RenameAsset_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.Q) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                LIGHT_AssetContainer_Recolor(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                LIGHT_AssetContainer_RecolorHueSet(null, null);
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

        //Tool Button (XenoKit - hidden in EEPK Organiser as the Tools menu can be used there)
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


        /// <summary>
        /// Returns the SelectedEffectParts for the first SelectedEffect, if it exists.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<EffectPart> GetSelectedEffectParts()
        {
            if (SelectedEffect != null)
            {
                return SelectedEffect.SelectedEffectParts;
            }

            return null;
        }

        private void pbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(pbindDataGrid.SelectedItem as Asset);
        }


        private void tbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PlayAsset(tbindDataGrid.SelectedItem as Asset);
        }

        private void cbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(cbindDataGrid.SelectedItem as Asset);
        }

        private void emoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(emoDataGrid.SelectedItem as Asset);
        }

        private void lightDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(lightDataGrid.SelectedItem as Asset);
        }

        private void PlayAsset(Asset asset)
        {
#if XenoKit
            if (SceneManager.MainGameInstance != null && asset != null)
                SceneManager.MainGameInstance.VfxPreview.PreviewAsset(asset);
#endif
        }
    }
}