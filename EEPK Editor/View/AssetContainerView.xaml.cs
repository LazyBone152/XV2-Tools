using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Misc;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ESK;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for AssetContainerView.xaml
    /// </summary>
    public partial class AssetContainerView : AutoObservableUserControl
    {
        #region DP
        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(AssetContainerView), new PropertyMetadata(OnAssetContainerChanged));

        public AssetContainerTool AssetContainer
        {
            get { return (AssetContainerTool)GetValue(AssetContainerProperty); }
            set
            {
                SetValue(AssetContainerProperty, value);
                NotifyPropertyChanged(nameof(AssetContainer));
            }
        }

        private static void OnAssetContainerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is AssetContainerView view)
            {
                view.LoadContainer();
            }
        }
        #endregion

        public event SelectionChangedEventHandler SelectionChanged;

        private EepkEditor MainView;
        private EffectContainerFile effectContainerFile => AssetContainer?.EffectContainer;
        private AssetType AssetType => AssetContainer?.ContainerAssetType ?? AssetType.EMO;
        public bool HasAssetContainer => AssetContainer != null;

        private Asset _selectedAsset = null;
        public Asset SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                _selectedAsset = value;
                NotifyPropertyChanged(nameof(SelectedAsset));
            }
        }
        public ObservableCollection<Asset> SelectedAssets { get; } = new ObservableCollection<Asset>();

        //Exposed for binding visibilities in the UI
        public bool IsEmo => AssetType == AssetType.EMO;
        public bool IsPbind => AssetType == AssetType.PBIND;
        public bool IsTbind => AssetType == AssetType.TBIND;
        public bool IsCbind => AssetType == AssetType.CBIND;
        public bool IsLight => AssetType == AssetType.LIGHT;
        public bool IsCbindOrLight => AssetType == AssetType.CBIND || AssetType == AssetType.LIGHT;
        public bool IsPbindOrTbind => AssetType == AssetType.PBIND || AssetType == AssetType.TBIND;
        public bool IsNotLight => AssetType != AssetType.LIGHT;
        public bool IsNotEmo => AssetType != AssetType.EMO;

        public AssetContainerView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += AssetContainerView_Loaded;
            Unloaded += AssetContainerView_Unloaded;
        }

        private void AssetContainerView_Loaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            dataGrid.SelectionChanged += DataGrid_SelectionChanged;
            MainView = GetEditorView();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void AssetContainerView_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            dataGrid?.Items?.Refresh();
        }

        private EepkEditor GetEditorView()
        {
            DependencyObject current = this;
            
            while(current != null && current is not EepkEditor)
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as EepkEditor;
        }

        private void LoadContainer()
        {
            if(AssetType == AssetType.EMO)
            {
                dataGrid.RowDetailsTemplate = FindResource("EmoListBox") as DataTemplate;
                dataGrid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
            }
            else
            {
                dataGrid.RowDetailsTemplate = null;
                dataGrid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Collapsed;
            }

            NotifyPropertyChanged(nameof(AssetContainer));
            NotifyPropertyChanged(nameof(IsEmo));
            NotifyPropertyChanged(nameof(IsPbind));
            NotifyPropertyChanged(nameof(IsTbind));
            NotifyPropertyChanged(nameof(IsCbind));
            NotifyPropertyChanged(nameof(IsLight));
            NotifyPropertyChanged(nameof(IsPbindOrTbind));
            NotifyPropertyChanged(nameof(IsNotEmo));
            NotifyPropertyChanged(nameof(IsNotLight));
            NotifyPropertyChanged(nameof(IsCbindOrLight));
        }

        public void SelectAsset(Asset asset)
        {
            if(asset == null)
            {
                SelectedAssets.Clear();
                SelectedAsset = null;
                return;
            }

            if (!SelectedAssets.Contains(asset))
            {
                SelectedAssets.Clear();
                SelectedAsset = asset;
            }
            
            dataGrid.ScrollIntoView(asset);
        }

        private void SetSelectedAssets(IList<Asset> assets)
        {
            if (assets == null || assets.Count == 0) return;

            SelectedAsset = assets[0];
            SelectedAssets.Clear();

            foreach (var asset in assets)
            {
                SelectedAssets.Add(asset);
            }

            dataGrid.ScrollIntoView(SelectedAsset);
        }

        #region Shared
        [RelayCommand(CanExecute = nameof(CanMergeAssets))]
        private void AssetMerge()
        {
            Asset primeAsset = SelectedAsset;
            List<Asset> selectedAssets = SelectedAssets.ToList();
            selectedAssets.Remove(primeAsset);

            if (primeAsset != null && selectedAssets.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                int count = selectedAssets.Count + 1;

                if (MessagePrompt.Show(string.Format("All currently selected assets will be MERGED into {0}.\n\nAll other selected assets will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", primeAsset.FileNamesPreview), string.Format("Merge ({0} assets)", count), MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var assetToRemove in selectedAssets)
                    {
                        AssetContainer.EffectContainer.AssetRefRefactor(assetToRemove, primeAsset, undos);
                        undos.Add(new UndoableListRemove<Asset>(AssetContainer.Assets, assetToRemove));
                        AssetContainer.Assets.Remove(assetToRemove);
                    }
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge"));
            }
            else
            {
                MessagePrompt.Show("Cannot merge with less than 2 assets selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessagePromptButtons.OK, MessagePromptIcon.Warning);
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetDelete()
        {
            var assets = SelectedAssets.ToArray();

            if (assets.Length > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (MessagePrompt.Show(string.Format("{0} asset(s) will be deleted. Any EffectParts that are linked to them will also be deleted.\n\nDo you want to continue?", assets.Length), "Delete Asset(s)", MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var asset in assets)
                    {
                        effectContainerFile.AssetRefRefactor(asset, null, undos); //Remove references to this asset
                        undos.Add(new UndoableListRemove<Asset>(AssetContainer.Assets, asset));
                        AssetContainer.Assets.Remove(asset);

                        if (asset.AssetType == AssetType.PBIND)
                        {
                            EepkEditor.CloseEmpForm(asset.Files[0].EmpFile);
                        }
                        else if (asset.AssetType == AssetType.CBIND)
                        {
                            EepkEditor.CloseEcfForm(asset.Files[0].EcfFile);
                        }
                        else if (asset.AssetType == AssetType.TBIND)
                        {
                            EepkEditor.CloseEtrForm(asset.Files[0].EtrFile);
                        }
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Asset"));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetDuplicate()
        {
            if (SelectedAssets.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>(SelectedAssets.Count);
                List<Asset> newAssets = new List<Asset>(SelectedAssets.Count);

                foreach (var asset in SelectedAssets)
                {
                    Asset newAsset = asset.Clone();
                    newAssets.Add(newAsset);
                    newAsset.InstanceID = Guid.NewGuid();
                    newAsset.AssetType = AssetType;

                    foreach (var file in newAsset.Files)
                    {
                        file.SetName(AssetContainer.GetUnusedName(file.FullFileName));
                    }

                    AssetContainer.Assets.Add(newAsset);

                    undos.Add(new UndoableListAdd<Asset>(AssetContainer.Assets, newAsset));
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Duplicate Asset"));
                SetSelectedAssets(newAssets);
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetUsedBy()
        {
            bool showExt = AssetType != AssetType.EMO;

            if (SelectedAsset != null)
            {
                List<int> effects = effectContainerFile.AssetUsedBy(SelectedAsset);
                effects.Sort();
                StringBuilder str = new StringBuilder();

                foreach (int effect in effects)
                {
                    str.Append(string.Format("Effect ID: {0}\r", effect));
                }

                string shownName = showExt ? SelectedAsset.FileNamesPreviewWithExtension : SelectedAsset.FileNamesPreview;

                MessagePrompt prompt = new MessagePrompt($"{shownName}: Used By", "The following effects use this asset:", str.ToString());
                prompt.Show();
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetRename() //PBIND, TBIND, CBIND and LIGHT
        {
            if (AssetType == AssetType.EMO) return;

            if (SelectedAsset != null)
            {
                AssetContainer_RenameFile(SelectedAsset.Files[0], SelectedAsset, AssetContainer);
                SelectedAsset.RefreshNamePreview();
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetCopy()
        {
            Asset[] selectedAsets = SelectedAssets.ToArray();

            if (selectedAsets.Length > 0)
            {
                effectContainerFile.SaveDds();
                CustomClipboard.SetData(ClipboardDataTypes.GetAssetFormat(AssetType), selectedAsets);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteAsset))]
        private void AssetPaste()
        {
            if (!CustomClipboard.TryGetData(ClipboardDataTypes.GetAssetFormat(AssetType), out Asset[] assets))
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>(assets.Length);
            List<Asset> newAssets = new List<Asset>(assets.Length);

            if (assets != null)
            {
                int alreadyExistCount = 0;
                int copied = 0;

                foreach (Asset asset in assets)
                {
                    if (AssetContainer.AssetExists(asset) == null)
                    {
                        copied++;
                        AssetContainer.AddAsset(asset, undos);
                        newAssets.Add(asset);
                    }
                    else
                    {
                        alreadyExistCount++;
                    }
                }

                if (alreadyExistCount > 0 && copied == 0)
                {
                    MessagePrompt.Show("All copied assets already exist. Copy failed.", "Warning", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }
                else if (alreadyExistCount > 0 && copied > 0)
                {
                    MessagePrompt.Show(string.Format("{0} assets were skipped as they already exist.", alreadyExistCount), "Warning", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste"));
                SetSelectedAssets(newAssets);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteAsset))]
        private void AssetPasteOver()
        {
            if (SelectedAsset != null)
            {
                if (!CustomClipboard.TryGetData(ClipboardDataTypes.GetAssetFormat(AssetType), out IList<Asset> newAsset))
                    return;

                if (newAsset == null)
                {
                    MessagePrompt.Show("No asset was found in the clipboard. Cannot continue paste operation.", "Replace", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                if (newAsset.Count > 1)
                {
                    MessagePrompt.Show("More than one asset was in the clipboard, operation aborted.", "Replace", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                if (MessagePrompt.Show(string.Format("The asset \"{0}\" will be replaced with a copy of \"{1}\".\n\nDo you want to continue?", SelectedAsset.FileNamesPreview, newAsset[0].FileNamesPreview), "Replace", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.No)
                {
                    return;
                }

                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (newAsset.Count == 1)
                {
                    //back up of original asset data, so it can be restored in the event of an error
                    var oldFiles = SelectedAsset.Files;
                    var selectedAsset = newAsset;

                    foreach (var file in selectedAsset[0].Files)
                    {
                        string newName = AssetContainer.GetUnusedName(file.FullFileName);
                        undos.Add(new UndoableProperty<EffectFile>(nameof(file.FileName), file, file.FileName, Path.GetFileNameWithoutExtension(newName)));
                        file.SetName(newName);

                        try
                        {
                            switch (AssetType)
                            {
                                case AssetType.PBIND:
                                    AssetContainer.AddPbindDependencies(file.EmpFile, undos);
                                    break;
                                case AssetType.TBIND:
                                    AssetContainer.AddTbindDependencies(file.EtrFile, undos);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            //Restore the previous asset
                            SelectedAsset.Files = oldFiles;
                            MessagePrompt.Show($"Encountered an error while pasting the asset. \n\nThe asset has not been changed.", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error, richText: ex.Message);
                            return;
                        }
                    }

                    undos.Add(new UndoableProperty<Asset>(nameof(SelectedAsset.Files), SelectedAsset, SelectedAsset.Files, selectedAsset[0].Files));
                    undos.Add(new UndoActionDelegate(SelectedAsset, nameof(SelectedAsset.RefreshNamePreview), true));

                    SelectedAsset.Files = selectedAsset[0].Files;
                    SelectedAsset.RefreshNamePreview();

                    effectContainerFile.AssetRefDetailsRefresh(SelectedAsset);

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Over"));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetHueAdjust()
        {
            AssetOpenRecolorDialog(false);
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetHueSet()
        {
            AssetOpenRecolorDialog(true);
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetOpenEditor()
        {
            if (AssetType == AssetType.EMO || AssetType == AssetType.LIGHT) return;

            switch (AssetType)
            {
                case AssetType.PBIND:
                    EepkEditor.PBIND_OpenEditor(SelectedAsset.Files[0].EmpFile, AssetContainer, SelectedAsset.Files[0].FileName);
                    break;
                case AssetType.TBIND:
                    EepkEditor.TBIND_OpenEditor(SelectedAsset.Files[0].EtrFile, AssetContainer, SelectedAsset.Files[0].FileName);
                    break;
                case AssetType.CBIND:
                    EepkEditor.CBIND_OpenEditor(SelectedAsset.Files[0].EcfFile, SelectedAsset.Files[0].FileName);
                    break;
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetSelected))]
        private void AssetExtract()
        {
            if (AssetType != AssetType.CBIND && AssetType != AssetType.LIGHT) return;

            if (SelectedAsset != null)
            {
                byte[] bytes;
                string filter;

                switch (AssetType)
                {
                    case AssetType.CBIND:
                        bytes = SelectedAsset.Files[0].EcfFile.SaveToBytes();
                        filter = "ECF File | *.ecf";
                        break;
                    case AssetType.LIGHT:
                        bytes = SelectedAsset.Files[0].EmaFile.Write();
                        filter = "LIGHT EMA File | *.light.ema";
                        break;
                    default:
                        return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Title = "Extract file...",
                    FileName = SelectedAsset.Files[0].FullFileName,
                    Filter = filter
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveDialog.FileName, bytes);
                }
            }
        }

        //CanExecute
        private bool CanMergeAssets()
        {
            return SelectedAssets.Count > 1;
        }

        private bool IsAssetSelected()
        {
            return SelectedAsset != null;
        }

        private bool CanPasteAsset()
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.GetAssetFormat(AssetType));
        }

        //Helpers
        private void AssetOpenRecolorDialog(bool hueSet)
        {
            if (SelectedAssets?.Count > 0)
            {
                RecolorAll recolor = new RecolorAll(AssetContainer, SelectedAssets, hueSet);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
        }
        #endregion

        #region Pbind

        [RelayCommand(CanExecute = nameof(IsPbindSelected))]
        private void AssetPbindRemoveColorAnimations()
        {
            if (SelectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in SelectedAssets)
                    undos.AddRange(asset.Files[0].EmpFile.RemoveColorAnimations());

                if (undos.Count > 0)
                {
                    UndoManager.Instance.AddCompositeUndo(undos, "Remove Color Animations (PBIND)");
                    MessagePrompt.Show($"{undos.Count} animations were removed", "Remove Color Animations (PBIND)");
                }
                else
                {
                    MessagePrompt.Show("No animations found", "Remove Color Animations (PBIND)");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsPbindSelected))]
        private void AssetPbindRemoveRandomColorRange()
        {
            if (SelectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in SelectedAssets)
                    undos.AddRange(asset.Files[0].EmpFile.RemoveRandomColorRange());

                if (undos.Count > 0)
                {
                    UndoManager.Instance.AddCompositeUndo(undos, "Remove Random Color Range (PBIND)");
                    MessagePrompt.Show($"{undos.Count} random instances were removed", "Remove Random Color Range (PBIND)");
                }
                else
                {
                    MessagePrompt.Show("No random instances found", "Remove Random Color Range (PBIND)");
                }
            }
        }

        private bool IsPbindSelected()
        {
            return AssetType == AssetType.PBIND && SelectedAsset != null;
        }

        #endregion

        #region Tbind

        [RelayCommand(CanExecute = nameof(IsTbindSelected))]
        private void AssetTbindScale()
        {
            float currentScale = SelectedAsset.Files[0].EtrFile.GetAverageScale();
            NumericInput inputForm = new NumericInput("ETR Scale", "Scale Factor", currentScale, 0, 50, 0.01);
            inputForm.ShowDialog();

            if (!inputForm.IsCancelled)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                SelectedAsset.Files[0].EtrFile.ScaleETRParts(Math.Max(0.001f, inputForm.GetValue<float>()) / Math.Max(0.001f, currentScale), undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "ETR Scale"));
                UndoManager.Instance.ForceEventCall();
            }
        }

        private bool IsTbindSelected()
        {
            return AssetType == AssetType.TBIND && SelectedAsset != null;
        }

        #endregion

        #region Emo
        private EffectFile _selectedEmoFile = null;
        public EffectFile SelectedEmoFile
        {
            get => _selectedEmoFile;
            set
            {
                if (_selectedEmoFile != value)
                {
                    _selectedEmoFile = value;
                    RaisePropertyChanged(nameof(SelectedEmoFile));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSelected))]
        private void AssetEmoAddFile()
        {
            if (SelectedAsset != null)
            {
                if (SelectedAsset.Files.Count == 5)
                {
                    MessagePrompt.Show("An asset cannot have more than 5 files assigned to it.", "Add File", MessagePromptButtons.OK, MessagePromptIcon.Error);
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
                    string newName = AssetContainer.GetUnusedName(originalName); //To prevent duplicates

                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    switch (EffectFile.GetExtension(openFile.FileName))
                    {
                        case ".emm":
                            SelectedAsset.AddFile(EMM_File.LoadEmm(bytes), newName, EffectFile.FileType.EMM, undos);
                            break;
                        case ".emb":
                            SelectedAsset.AddFile(EMB_File.Load(bytes), newName, EffectFile.FileType.EMB, undos);
                            break;
                        case ".light.ema":
                            SelectedAsset.AddFile(EMA_File.Load(bytes), newName, EffectFile.FileType.EMA, undos);
                            break;
                        default:
                            SelectedAsset.AddFile(bytes, newName, EffectFile.FileType.Other, undos);
                            break;
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Add File (EMO)");

                    if (newName != originalName)
                    {
                        MessagePrompt.Show(string.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newName, originalName), "Add File", MessagePromptButtons.OK, MessagePromptIcon.Information);
                    }

                    SelectAsset(SelectedAsset);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSelected))]
        private void AssetEmoAddEmdFile()
        {
            if (SelectedAsset.Files.Any(x => x.fileType == EffectFile.FileType.EMB) || SelectedAsset.Files.Any(x => x.fileType == EffectFile.FileType.EMM) || SelectedAsset.Files.Any(x => x.fileType == EffectFile.FileType.EMO))
            {
                MessagePrompt.Show($"An EMO, EMB or EMM already exists on the selected asset. Please delete it/them to complete the conversion.", "File Already Exists", MessagePromptIcon.Error);
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
                List<EMB_TextureFile> embFiles = new List<EMB_TextureFile>();
                List<EMB_TextureFile> dytFiles = new List<EMB_TextureFile>();
                List<EMM_File> emmFiles = new List<EMM_File>();

                bool hasDyt = false;


                foreach (var file in openFile.FileNames)
                {
                    if (Path.GetExtension(file) == ".emd")
                    {
                        string emmPath = string.Format("{0}/{1}.emm", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                        string embPath = string.Format("{0}/{1}.emb", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                        string dytPath = string.Format("{0}/{1}.dyt.emb", Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                        if (!File.Exists(emmPath))
                        {
                            MessagePrompt.Show($"Could not find \"{emmPath}\".\n\nPlease note that each EMD file must have a EMM and EMB in the same directory.", "File Already Exists", MessagePromptIcon.Error);
                            return;
                        }

                        if (!File.Exists(embPath))
                        {
                            MessagePrompt.Show($"Could not find \"{embPath}\".\n\nPlease note that each EMD file must have a EMM and EMB in the same directory.", "File Already Exists", MessagePromptIcon.Error);
                            return;
                        }

                        if (!File.Exists(dytPath) && hasDyt)
                        {
                            MessagePrompt.Show($"Could not find \"{dytPath}\".\n\nIf at least one of the EMD files have a dyt file, then one is required for each EMD.", "File Already Exists", MessagePromptIcon.Error);
                            return;
                        }

                        EMD_File emd = EMD_File.Load(file);
                        EMB_TextureFile emb = EMB_TextureFile.Load(embPath);
                        EMM_File emm = EMM_File.LoadEmm(emmPath);
                        EMB_TextureFile dyt = null;

                        emdFiles.Add(emd);
                        embFiles.Add(emb);
                        emmFiles.Add(emm);

                        if (File.Exists(dytPath))
                        {
                            hasDyt = true;
                            dyt = EMB_TextureFile.Load(dytPath);
                            dytFiles.Add(dyt);
                        }

                    }
                    else if (Path.GetExtension(file) == ".esk")
                    {
                        eskFile = ESK_File.Load(file);
                    }

                }

                //Validation
                if (emdFiles.Count == 0)
                {
                    MessagePrompt.Show($"No EMD files were selected/loaded, so no EMO can be created.", "File Already Exists", MessagePromptIcon.Error);
                    return;
                }

                if (eskFile == null)
                {
                    MessagePrompt.Show($"No ESK file was selected/loaded, so no EMO can be created.", "File Already Exists", MessagePromptIcon.Error);
                    return;
                }

                EMB_TextureFile mergedEmb;
                EMM_File mergedEmm;
                EMO_File emoFile = EMO_File.ConvertToEmo(emdFiles.ToArray(), embFiles.ToArray(), dytFiles.ToArray(), emmFiles.ToArray(), eskFile, out mergedEmb, out mergedEmm);

                List<IUndoRedo> undos = new List<IUndoRedo>();

                string name = Path.GetFileNameWithoutExtension(openFile.FileName);

                SelectedAsset.AddFile(emoFile, $"{name}.emo", EffectFile.FileType.EMO, undos);
                SelectedAsset.AddFile(mergedEmb, $"{name}.emb", EffectFile.FileType.EMB, undos);
                SelectedAsset.AddFile(mergedEmm, $"{name}.emm", EffectFile.FileType.EMM, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "EMD -> EMO Conversion");
            }

        }

        [RelayCommand(CanExecute = nameof(IsEmoSelected))]
        private void AssetEmoAddEanFile()
        {
            //TODO: Make worky
            var asset = SelectedAsset;

            if (asset.Files.Any(x => x.Extension.ToLower() == ".obj.ema"))
            {
                MessagePrompt.Show($"An OBJ.EMA already exists on the selected asset.", "File Already Exists", MessagePromptIcon.Warning);
                return;
            }

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "EAN -> EMA conversion";
            openFile.Filter = "EAN files | *.ean";

            if (openFile.ShowDialog() == true)
            {
                Xv2CoreLib.AnimationFramework.AnimationFile animationFile = new Xv2CoreLib.AnimationFramework.AnimationFile(EAN_File.Load(openFile.FileName));
                EMA_File emaFile = animationFile.ConvertToEma();
                List<IUndoRedo> undos = new List<IUndoRedo>();
                asset.AddFile(emaFile, $"{Path.GetFileNameWithoutExtension(openFile.FileName)}.obj.ema", EffectFile.FileType.EMA, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "EAN -> EMA");
            }
        }

        [RelayCommand]
        private void AssetEmoDoubleClick(EffectFile file)
        {
            OpenEmoEffectFileEditor(file, false);
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileRename()
        {
            if (SelectedEmoFile != null)
            {
                var parentAsset = AssetContainer.GetAssetByFileInstance(SelectedEmoFile);

                AssetContainer_RenameFile(SelectedEmoFile, parentAsset, AssetContainer);

                if (parentAsset != null)
                {
                    parentAsset.RefreshNamePreview();
                }

                dataGrid.Items.Refresh();
                SelectAsset(parentAsset);
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileReplace()
        {
            if (SelectedEmoFile != null)
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Add file...";
                openFile.Filter = "XV2 effect files | *.emo; *.ema; *.emm; *.emb";
                openFile.ShowDialog();

                if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                {
                    if (EffectFile.GetExtension(openFile.FileName) != SelectedEmoFile.Extension)
                    {
                        MessagePrompt.Show(String.Format("The file type of the selected external file ({0}) does not match that of {1}.", openFile.FileName, SelectedEmoFile.FullFileName), "Replace", MessagePromptButtons.OK, MessagePromptIcon.Error);
                        return;
                    }

                    string originalNewFileName = Path.GetFileName(openFile.FileName);
                    byte[] bytes = File.ReadAllBytes(openFile.FileName);
                    string newFileName = AssetContainer.GetUnusedName(originalNewFileName); //To prevent duplicates

                    object oldFile;

                    switch (SelectedEmoFile.fileType)
                    {
                        case EffectFile.FileType.EMM:
                            oldFile = SelectedEmoFile.EmmFile;
                            SelectedEmoFile.EmmFile = EMM_File.LoadEmm(bytes);
                            UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.EmmFile), SelectedEmoFile, oldFile, SelectedEmoFile.EmmFile, "Replace File (EMO)"));
                            break;
                        case EffectFile.FileType.EMB:
                            oldFile = SelectedEmoFile.EmbFile;
                            SelectedEmoFile.EmbFile = EMB_TextureFile.Load(bytes);
                            UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.EmbFile), SelectedEmoFile, oldFile, SelectedEmoFile.EmbFile, "Replace File (EMO)"));
                            break;
                        default:
                            oldFile = SelectedEmoFile.Bytes;
                            SelectedEmoFile.Bytes = bytes;
                            UndoManager.Instance.AddUndo(new UndoableProperty<EffectFile>(nameof(EffectFile.Bytes), SelectedEmoFile, oldFile, SelectedEmoFile.Bytes, "Replace File (EMO)"));
                            break;
                    }

                    if (MessagePrompt.Show("Do you want to keep the old file name?", "Keep Name?", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.No)
                    {
                        SelectedEmoFile.SetName(newFileName);

                        if (newFileName != originalNewFileName)
                        {
                            MessagePrompt.Show(String.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newFileName, originalNewFileName), "Add File", MessagePromptButtons.OK, MessagePromptIcon.Information);
                        }
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileDelete()
        {
            if (SelectedEmoFile != null)
            {
                var parentAsset = AssetContainer.GetAssetByFileInstance(SelectedEmoFile);

                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (parentAsset != null)
                {
                    if (parentAsset.Files.Count > 1)
                    {
                        parentAsset.RemoveFile(SelectedEmoFile, undos);
                    }
                    else
                    {
                        MessagePrompt.Show(string.Format("Cannot delete the last file."), "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Delete File (EMO)");

                    SelectAsset(parentAsset);
                }
                else
                {
                    MessagePrompt.Show(string.Format("Could not find the parent asset."), "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileExtract()
        {
            if (SelectedEmoFile != null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Extract file...";
                saveDialog.AddExtension = false;
                saveDialog.Filter = string.Format("{1} File | *{0}", Path.GetExtension(SelectedEmoFile.Extension), Path.GetExtension(SelectedEmoFile.Extension).Remove(0, 1).ToUpper());
                saveDialog.FileName = SelectedEmoFile.FullFileName;

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveDialog.FileName, SelectedEmoFile.GetBytes());
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileEdit()
        {
            if (SelectedEmoFile != null)
            {
                OpenEmoEffectFileEditor(SelectedEmoFile, true);
            }
        }


        private bool IsEmoSelected()
        {
            return AssetType == AssetType.EMO && SelectedAsset != null;
        }

        private bool IsEmoSubfileSelected()
        {
            return SelectedEmoFile != null;
        }

        //Helpers
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
                            window = EepkEditor.GetActiveEmbForm(selectedFile.EmbFile);

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
                            window = EepkEditor.GetActiveEmmForm(selectedFile.EmmFile);

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
                    case EffectFile.FileType.EMO:
                        if (MainView.XenoKitAvailable)
                        {
                            MainView.AssetEmoOpenEditor(SelectedAsset);
                            break;
                        }
                        else
                        {
                            goto default;
                        }
                    default:
                        if (showError)
                            MessagePrompt.Show(string.Format("Edit not possible for {0} files.", selectedFile.Extension), "Edit", MessagePromptButtons.OK, MessagePromptIcon.Stop);
                        return;
                }

                if (window != null)
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

        #endregion

        #region AssetContainer
        private void PbindCreateNewEmp()
        {
            if (AssetType != AssetType.PBIND) return;
            var asset = AssetContainer.AddAsset(new EMP_File(), "NewEmp.emp");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(AssetContainer.Assets, asset));
            undos.Add(new UndoActionDelegate(AssetContainer, nameof(AssetContainer.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMP"));
        }

        private void TbindCreateNewEtr()
        {
            if (AssetType != AssetType.TBIND) return;
            ETR_File etrFile = new ETR_File();
            etrFile.Nodes.Add(ETR_Node.GetNew());

            Asset asset = AssetContainer.AddAsset(etrFile, "NewEtr.etr");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(AssetContainer.Assets, asset));
            undos.Add(new UndoActionDelegate(AssetContainer, nameof(AssetContainer.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ETR"));
        }

        private void CbindCreateNewEcf()
        {
            if (AssetType != AssetType.CBIND) return;
            ECF_File ecfFile = new ECF_File();
            ecfFile.Nodes.Add(new ECF_Node());

            Asset asset = AssetContainer.AddAsset(ecfFile, "NewEcf.ecf");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(AssetContainer.Assets, asset));
            undos.Add(new UndoActionDelegate(AssetContainer, nameof(AssetContainer.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ECF"));
        }

        private void EmoCreateNewAsset()
        {
            if (AssetType != AssetType.EMO) return;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            Asset asset = Asset.Create(AssetType.EMO);
            AssetContainer.AddAsset(asset, undos);
            SelectAsset(asset);

            //Undos
            undos.Add(new UndoActionDelegate(AssetContainer, nameof(AssetContainer.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMO Asset"));
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void CbindImportExternalEcf()
        {
            if (AssetType != AssetType.CBIND) return;
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add file...";
            openFile.Filter = "ECF files | *.ecf";
            openFile.ShowDialog();

            if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
            {
                string newName = AssetContainer.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                Asset asset = new Asset(AssetType.CBIND);
                asset.Files.Add(new EffectFile()
                {
                    EcfFile = ECF_File.Load(openFile.FileName),
                    Extension = EffectFile.GetExtension(openFile.FileName),
                    fileType = EffectFile.FileType.ECF,
                    OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                    FileName = EffectFile.GetFileNameWithoutExtension(newName)
                });

                AssetContainer.Assets.Add(asset);
                SelectAsset(asset);
            }
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void EmoImportExternalFiles()
        {
            if (AssetType != AssetType.EMO) return;
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Import Asset...";
            openFile.Filter = "EMO effect files | *.emo; *.mat.ema; *.obj.ema; *.emm; *.emb";
            openFile.Multiselect = true;
            openFile.ShowDialog();

            if (openFile.FileNames.Length > 0)
            {
                if (openFile.FileNames.Length > 5)
                {
                    MessagePrompt.Show(string.Format("EMO assets cannot have more than 5 files. {0} were selected.\n\nImport cancelled.", openFile.FileNames.Length), "Import Asset", MessagePromptButtons.OK, MessagePromptIcon.Stop);
                    return;
                }

                Asset asset = new Asset(AssetType.EMO);

                foreach (string file in openFile.FileNames)
                {
                    string newName = AssetContainer.GetUnusedName(System.IO.Path.GetFileName(file));

                    switch (EffectFile.GetFileType(file))
                    {
                        case EffectFile.FileType.EMB:
                            asset.AddFile(EMB_File.Load(file), newName, EffectFile.FileType.EMB);
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

                AssetContainer.Assets.Add(asset);
                SelectAsset(asset);

                UndoManager.Instance.AddUndo(new UndoableListAdd<Asset>(AssetContainer.Assets, asset, "Add EMO"));
            }
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void LightImportExternal()
        {
            if (AssetType != AssetType.LIGHT) return;
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add file...";
            openFile.Filter = "light.ema files | *.light.ema";
            openFile.ShowDialog();

            if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
            {
                string newName = AssetContainer.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                Asset asset = new Asset(AssetType.LIGHT);
                asset.Files.Add(new EffectFile()
                {
                    EmaFile = EMA_File.Load(openFile.FileName),
                    Extension = EffectFile.GetExtension(openFile.FileName),
                    fileType = EffectFile.FileType.EMA,
                    OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                    FileName = EffectFile.GetFileNameWithoutExtension(newName)
                });

                AssetContainer.Assets.Add(asset);
                SelectAsset(asset);
            }
        }


        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void AssetRemoveUnused()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            int amountRemoved = effectContainerFile.RemoveUnusedAssets(AssetType, undos);

            if (amountRemoved > 0)
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Remove Unusued ({AssetType})"));
                MessagePrompt.Show(String.Format("{0} unused assets were removed.", amountRemoved), "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
            else
            {
                MessagePrompt.Show("There are no unused assets.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void AssetOpenSettings()
        {
            AssetContainerSettings assetSettings = new AssetContainerSettings(AssetContainer);
            assetSettings.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void AssetOpenTextureForm()
        {
            if (AssetType == AssetType.EMO || AssetType == AssetType.LIGHT || AssetType == AssetType.CBIND) return;

            EepkEditor.AssetContainerOpenTextureForm(AssetContainer, AssetType);
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private void AssetOpenMaterialForm()
        {
            if (AssetType == AssetType.EMO || AssetType == AssetType.LIGHT || AssetType == AssetType.CBIND) return;

            EepkEditor.AssetContainerOpenMaterialForm(AssetContainer, AssetType);
        }


        private void AssetContainer_RenameFile(EffectFile effectFile, Asset asset, AssetContainerTool container)
        {
            RenameForm renameForm = new RenameForm(effectFile.FileName, effectFile.Extension, string.Format("Renaming {0}", effectFile.FullFileName), container, Application.Current.MainWindow);
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
        #endregion

        #region Menu
        [RelayCommand(CanExecute = nameof(CanCreateAsset))]
        private void NewAsset()
        {
            switch (AssetType)
            {
                case AssetType.PBIND:
                    PbindCreateNewEmp();
                    break;
                case AssetType.TBIND:
                    TbindCreateNewEtr();
                    break;
                case AssetType.CBIND:
                    CbindCreateNewEcf();
                    break;
                case AssetType.EMO:
                    EmoCreateNewAsset();
                    break;
            }
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportAssetsFromGame(EntitySelector.EntityType fromType)
        {
            List<Asset> importedAssets = await MainView.ImportAssetsFromGame(AssetType, fromType);
            SetSelectedAssets(importedAssets);
        }

        [RelayCommand(CanExecute = nameof(HasAssetContainer))]
        private async Task ImportAssetsFromExternalFile()
        {
            await MainView.AssetContainer_ImportAssets(AssetContainer, AssetType);
        }

        private bool CanImportFromGame()
        {
            return EepkEditor.GameDirectoryCheck() && AssetContainer != null;
        }

        private bool CanCreateAsset()
        {
            return AssetContainer != null && AssetType != AssetType.LIGHT;
        }

        #endregion

    }
}
