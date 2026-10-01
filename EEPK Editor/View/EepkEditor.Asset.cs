using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Misc;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using Application = System.Windows.Application;

namespace EEPK_Organiser.View
{
    public partial class EepkEditor : AutoObservableUserControl
    {
        #region Selection
        private Asset _selectedPbind = null;
        private Asset _selectedTbind = null;
        private Asset _selectedCbind = null;
        private Asset _selectedEmo = null;
        private Asset _selectedLight = null;

        public Asset SelectedPbind
        {
            get => _selectedPbind;
            set
            {
                _selectedPbind = value;
                NotifyPropertyChanged(nameof(SelectedPbind));
            }
        }
        public Asset SelectedTbind
        {
            get => _selectedTbind;
            set
            {
                _selectedTbind = value;
                NotifyPropertyChanged(nameof(SelectedTbind));
            }
        }
        public Asset SelectedCbind
        {
            get => _selectedCbind;
            set
            {
                _selectedCbind = value;
                NotifyPropertyChanged(nameof(SelectedCbind));
            }
        }
        public Asset SelectedEmo
        {
            get => _selectedEmo;
            set
            {
                _selectedEmo = value;
                NotifyPropertyChanged(nameof(SelectedEmo));
            }
        }
        public Asset SelectedLight
        {
            get => _selectedLight;
            set
            {
                _selectedLight = value;
                NotifyPropertyChanged(nameof(SelectedLight));
            }
        }

        private void SelectAsset(Asset asset)
        {
            switch (asset.AssetType)
            {
                case AssetType.PBIND:
                    SelectedPbind = asset;
                    pbindDataGrid.ScrollIntoView(asset);
                    break;
                case AssetType.TBIND:
                    SelectedTbind = asset;
                    tbindDataGrid.ScrollIntoView(asset);
                    break;
                case AssetType.CBIND:
                    SelectedCbind = asset;
                    cbindDataGrid.ScrollIntoView(asset);
                    break;
                case AssetType.LIGHT:
                    SelectedLight = asset;
                    lightDataGrid.ScrollIntoView(asset);
                    break;
                case AssetType.EMO:
                    SelectedEmo = asset;
                    emoDataGrid.ScrollIntoView(asset);
                    break;
            }
        }

        private Asset GetSelectedAsset(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.PBIND:
                    return SelectedPbind;
                case AssetType.TBIND:
                    return SelectedTbind;
                case AssetType.CBIND:
                    return SelectedCbind;
                case AssetType.LIGHT:
                    return SelectedLight;
                case AssetType.EMO:
                    return SelectedEmo;
                default:
                    throw new ArgumentException($"GetSelectedAssets: Bad AssetType ({assetType})");
            }
        }

        private IList<Asset> GetSelectedAssets(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.PBIND:
                    return pbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                case AssetType.TBIND:
                    return tbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                case AssetType.CBIND:
                    return cbindDataGrid.SelectedItems.Cast<Asset>().ToList();
                case AssetType.LIGHT:
                    return lightDataGrid.SelectedItems.Cast<Asset>().ToList();
                case AssetType.EMO:
                    return emoDataGrid.SelectedItems.Cast<Asset>().ToList();
                default:
                    throw new ArgumentException($"GetSelectedAssets: Bad AssetType ({assetType})");
            }
        }

        private int GetSelectedAssetsCount(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.PBIND:
                    return pbindDataGrid.SelectedItems.Count;
                case AssetType.TBIND:
                    return tbindDataGrid.SelectedItems.Count;
                case AssetType.CBIND:
                    return cbindDataGrid.SelectedItems.Count;
                case AssetType.LIGHT:
                    return lightDataGrid.SelectedItems.Count;
                case AssetType.EMO:
                    return emoDataGrid.SelectedItems.Count;
            }

            return 0;
        }

        #endregion

        #region Shared
        [RelayCommand(CanExecute = nameof(CanMergeAssets))]
        private void AssetMerge(AssetType assetType)
        {
            Asset primeAsset = GetSelectedAsset(assetType);
            IList<Asset> selectedAssets = GetSelectedAssets(assetType);
            selectedAssets.Remove(primeAsset);
            AssetContainerTool container = effectContainerFile.GetAssetContainer(assetType);

            if (primeAsset != null && selectedAssets.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                int count = selectedAssets.Count + 1;

                if (MessagePrompt.Show(string.Format("All currently selected assets will be MERGED into {0}.\n\nAll other selected assets will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", primeAsset.FileNamesPreview), string.Format("Merge ({0} assets)", count), MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var assetToRemove in selectedAssets)
                    {
                        effectContainerFile.AssetRefRefactor(assetToRemove, primeAsset, undos);
                        undos.Add(new UndoableListRemove<Asset>(container.Assets, assetToRemove));
                        container.Assets.Remove(assetToRemove);
                    }
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge"));
            }
            else
            {
                MessagePrompt.Show("Cannot merge with less than 2 assets selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessagePromptButtons.OK, MessagePromptIcon.Warning);
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetDelete(AssetType assetType)
        {
            IList<Asset> assets = GetSelectedAssets(assetType);

            if (assets.Count > 0)
            {
                AssetContainerTool container = effectContainerFile.GetAssetContainer(assetType);

                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (MessagePrompt.Show(string.Format("{0} asset(s) will be deleted. Any EffectParts that are linked to them will also be deleted.\n\nDo you want to continue?", assets.Count), "Delete Asset(s)", MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var asset in assets)
                    {
                        effectContainerFile.AssetRefRefactor(asset, null, undos); //Remove references to this asset
                        undos.Add(new UndoableListRemove<Asset>(container.Assets, asset));
                        container.Assets.Remove(asset);

                        if (asset.AssetType == AssetType.PBIND)
                        {
                            CloseEmpForm(asset.Files[0].EmpFile);
                        }
                        else if (asset.AssetType == AssetType.CBIND)
                        {
                            CloseEcfForm(asset.Files[0].EcfFile);
                        }
                        else if (asset.AssetType == AssetType.TBIND)
                        {
                            CloseEtrForm(asset.Files[0].EtrFile);
                        }
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Asset"));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetDuplicate(AssetType assetType)
        {
            IList<Asset> assets = GetSelectedAssets(assetType);

            if (assets.Count > 0)
            {
                AssetContainerTool container = effectContainerFile.GetAssetContainer(assetType);

                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in assets)
                {
                    Asset newAsset = asset.Clone();
                    newAsset.InstanceID = Guid.NewGuid();
                    newAsset.AssetType = assetType;

                    foreach (var file in newAsset.Files)
                    {
                        file.SetName(container.GetUnusedName(file.FullFileName));
                    }

                    container.Assets.Add(newAsset);

                    undos.Add(new UndoableListAdd<Asset>(container.Assets, newAsset));
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Duplicate Asset"));
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetUsedBy(AssetType assetType)
        {
            var asset = GetSelectedAsset(assetType);
            bool showExt = assetType != AssetType.EMO;

            if (asset != null)
            {
                List<int> effects = effectContainerFile.AssetUsedBy(asset);
                effects.Sort();
                StringBuilder str = new StringBuilder();

                foreach (int effect in effects)
                {
                    str.Append(string.Format("Effect ID: {0}\r", effect));
                }

                string shownName = showExt ? asset.FileNamesPreviewWithExtension : asset.FileNamesPreview;

                MessagePrompt prompt = new MessagePrompt($"{shownName}: Used By", "The following effects use this asset:", str.ToString());
                prompt.Show();
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetRename(AssetType assetType) //PBIND, TBIND, CBIND and LIGHT
        {
            if (assetType == AssetType.EMO) throw new ArgumentException("RenameAsset: EMO asset type cannot be renamed");
            var asset = GetSelectedAsset(assetType);

            if (asset != null)
            {
                AssetContainer_RenameFile(asset.Files[0], asset, effectContainerFile.GetAssetContainer(assetType));
                asset.RefreshNamePreview();
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetCopy(AssetType assetType)
        {
            IList<Asset> selectedAsets = GetSelectedAssets(assetType);

            if (selectedAsets?.Count > 0)
            {
                effectContainerFile.SaveDds();
                CustomClipboard.SetData(ClipboardDataTypes.GetAssetFormat(assetType), selectedAsets);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteAsset))]
        private void AssetPaste(AssetType assetType)
        {
            if (!CustomClipboard.TryGetData(ClipboardDataTypes.GetAssetFormat(assetType), out IList<Asset> assets))
                return;

            AssetContainerTool container = effectContainerFile.GetAssetContainer(assetType);

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (assets != null)
            {
                int alreadyExistCount = 0;
                int copied = 0;

                foreach (Asset asset in assets)
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
                    MessagePrompt.Show("All copied assets already exist. Copy failed.", "Warning", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }
                else if (alreadyExistCount > 0 && copied > 0)
                {
                    MessagePrompt.Show(string.Format("{0} assets were skipped as they already exist.", alreadyExistCount), "Warning", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste"));
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteAsset))]
        private void AssetPasteOver(AssetType assetType)
        {
            var asset = GetSelectedAsset(assetType);

            if (asset != null)
            {
                AssetContainerTool container = effectContainerFile.GetAssetContainer(assetType);

                if (!CustomClipboard.TryGetData(ClipboardDataTypes.GetAssetFormat(assetType), out IList<Asset> newAsset))
                    return;

                if (newAsset == null)
                {
                    MessagePrompt.Show("No asset was found in the clipboard. Cannot continue paste operation.", "Replace", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                if(newAsset.Count > 1)
                {
                    MessagePrompt.Show("More than one asset was in the clipboard, operation aborted.", "Replace", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                if (MessagePrompt.Show(string.Format("The asset \"{0}\" will be replaced with a copy of \"{1}\".\n\nDo you want to continue?", asset.FileNamesPreview, newAsset[0].FileNamesPreview), "Replace", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.No)
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
                            switch (assetType)
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
                            MessagePrompt.Show($"Encountered an error while pasting the asset. \n\nThe asset has not been changed.", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error, richText: ex.Message);
                            return;
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
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetHueAdjust(AssetType assetType)
        {
            AssetOpenRecolorDialog(assetType, GetSelectedAssets(assetType), false);
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetHueSet(AssetType assetType)
        {
            AssetOpenRecolorDialog(assetType, GetSelectedAssets(assetType), true);
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetOpenEditor(AssetType assetType)
        {
            if (assetType == AssetType.EMO || assetType == AssetType.LIGHT)
                throw new ArgumentException("AssetOpenEditor: No editors exist for EMO or LIGHT");

            var asset = GetSelectedAsset(assetType);

            switch (assetType)
            {
                case AssetType.PBIND:
                    PBIND_OpenEditor(asset.Files[0].EmpFile, effectContainerFile.Pbind, asset.Files[0].FileName);
                    break;
                case AssetType.TBIND:
                    TBIND_OpenEditor(asset.Files[0].EtrFile, effectContainerFile.Tbind, asset.Files[0].FileName);
                    break;
                case AssetType.CBIND:
                    CBIND_OpenEditor(asset.Files[0].EcfFile, asset.Files[0].FileName);
                    break;
            }
        }

        [RelayCommand(CanExecute = nameof(IsAssetTypeSelected))]
        private void AssetExtract(AssetType assetType)
        {
            if (assetType != AssetType.CBIND && assetType != AssetType.LIGHT)
                throw new ArgumentException("AssetExtract: Extraction only possible for CBIND and LIGHT asset types.");

            Asset asset = GetSelectedAsset(assetType);

            if (asset != null)
            {
                byte[] bytes;
                string filter;

                switch (assetType)
                {
                    case AssetType.CBIND:
                        bytes = asset.Files[0].EcfFile.SaveToBytes();
                        filter = "ECF File | *.ecf";
                        break;
                    case AssetType.LIGHT:
                        bytes = asset.Files[0].EmaFile.Write();
                        filter = "LIGHT EMA File | *.light.ema";
                        break;
                    default:
                        return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Title = "Extract file...",
                    FileName = asset.Files[0].FullFileName,
                    Filter = filter
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveDialog.FileName, bytes);
                }
            }
        }

        //CanExecute
        private bool CanMergeAssets(AssetType assetType)
        {
            return GetSelectedAssetsCount(assetType) > 1;
        }

        private bool IsAssetTypeSelected(AssetType assetType)
        {
            return GetSelectedAsset(assetType) != null;
        }

        private bool CanPasteAsset(AssetType assetType)
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.GetAssetFormat(assetType));
        }

        //Helpers
        private void AssetOpenRecolorDialog(AssetType assetType, IList<Asset> selectedAssets, bool hueSet)
        {
            if (selectedAssets?.Count > 0)
            {
                RecolorAll recolor = new RecolorAll(assetType, selectedAssets, hueSet);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
        }

        #endregion

        #region Pbind

        [RelayCommand(CanExecute = nameof(IsPbindSelected))]
        private void AssetPbindRemoveColorAnimations()
        {
            IList<Asset> selectedAssets = GetSelectedAssets(AssetType.PBIND);

            if (selectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in selectedAssets)
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
            IList<Asset> selectedAssets = GetSelectedAssets(AssetType.PBIND);

            if (selectedAssets != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var asset in selectedAssets)
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
            return GetSelectedAsset(AssetType.PBIND) != null;
        }

        #endregion

        #region Tbind

        [RelayCommand(CanExecute = nameof(IsTbindSelected))]
        private void AssetTbindScale()
        {
            Asset asset = GetSelectedAsset(AssetType.TBIND);

            float currentScale = asset.Files[0].EtrFile.GetAverageScale();
            LB_Common.Forms.NumericInput inputForm = new LB_Common.Forms.NumericInput("ETR Scale", "Scale Factor", currentScale, 0, 50, 0.01);
            inputForm.ShowDialog();

            if (!inputForm.IsCancelled)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                asset.Files[0].EtrFile.ScaleETRParts(Math.Max(0.001f, inputForm.GetValue<float>()) / Math.Max(0.001f, currentScale), undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "ETR Scale"));
                UndoManager.Instance.ForceEventCall();
            }
        }

        private bool IsTbindSelected()
        {
            return GetSelectedAsset(AssetType.TBIND) != null;
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
            Asset asset = GetSelectedAsset(AssetType.EMO);

            if (asset != null)
            {
                if (asset.Files.Count == 5)
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
                    string newName = effectContainerFile.Emo.GetUnusedName(originalName); //To prevent duplicates

                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    switch (EffectFile.GetExtension(openFile.FileName))
                    {
                        case ".emm":
                            asset.AddFile(EMM_File.LoadEmm(bytes), newName, EffectFile.FileType.EMM, undos);
                            break;
                        case ".emb":
                            asset.AddFile(EMB_File.Load(bytes), newName, EffectFile.FileType.EMB, undos);
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
                        MessagePrompt.Show(string.Format("The added file was renamed to \"{0}\" because \"{1}\" was already used.", newName, originalName), "Add File", MessagePromptButtons.OK, MessagePromptIcon.Information);
                    }

                    SelectAsset(asset);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSelected))]
        private void AssetEmoAddEmdFile()
        {
            Asset asset = GetSelectedAsset(AssetType.EMO);

            if (asset.Files.Any(x => x.fileType == EffectFile.FileType.EMB) || asset.Files.Any(x => x.fileType == EffectFile.FileType.EMM) || asset.Files.Any(x => x.fileType == EffectFile.FileType.EMO))
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

                asset.AddFile(emoFile, $"{name}.emo", EffectFile.FileType.EMO, undos);
                asset.AddFile(mergedEmb, $"{name}.emb", EffectFile.FileType.EMB, undos);
                asset.AddFile(mergedEmm, $"{name}.emm", EffectFile.FileType.EMM, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "EMD -> EMO Conversion");
            }

        }

        [RelayCommand(CanExecute = nameof(IsEmoSelected))]
        private void AssetEmoAddEanFile()
        {
            //TODO: Make worky
            var asset = GetSelectedAsset(AssetType.EMO);

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
                var parentAsset = effectContainerFile.Emo.GetAssetByFileInstance(SelectedEmoFile);

                AssetContainer_RenameFile(SelectedEmoFile, parentAsset, effectContainerFile.Emo);

                if (parentAsset != null)
                {
                    parentAsset.RefreshNamePreview();
                }

                emoDataGrid.Items.Refresh();
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
                    string newFileName = effectContainerFile.Emo.GetUnusedName(originalNewFileName); //To prevent duplicates

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

                    SelectAsset(emoDataGrid.SelectedItem as Asset);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEmoSubfileSelected))]
        private void AssetEmoFileDelete()
        {
            if (SelectedEmoFile != null)
            {
                var parentAsset = effectContainerFile.Emo.GetAssetByFileInstance(SelectedEmoFile);

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
            return GetSelectedAsset(AssetType.EMO) != null;
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
                    case EffectFile.FileType.EMO:
                        if (XenoKitAvailable)
                        {
                            AssetEmoOpenEditor(GetSelectedAsset(AssetType.EMO));
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
        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void PbindCreateNewEmp()
        {
            var asset = effectContainerFile.Pbind.AddAsset(new EMP_File(), "NewEmp.emp");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Pbind.Assets, asset));
            undos.Add(new UndoActionDelegate(effectContainerFile.Pbind, nameof(effectContainerFile.Pbind.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMP"));
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void TbindCreateNewEtr()
        {
            ETR_File etrFile = new ETR_File();
            etrFile.Nodes.Add(ETR_Node.GetNew());

            Asset asset = effectContainerFile.Tbind.AddAsset(etrFile, "NewEtr.etr");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Tbind.Assets, asset));
            undos.Add(new UndoActionDelegate(effectContainerFile.Tbind, nameof(effectContainerFile.Tbind.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ETR"));
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void CbindCreateNewEcf()
        {
            ECF_File ecfFile = new ECF_File();
            ecfFile.Nodes.Add(new ECF_Node());

            Asset asset = effectContainerFile.Cbind.AddAsset(ecfFile, "NewEcf.ecf");
            SelectAsset(asset);

            //Undos
            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableListAdd<Asset>(effectContainerFile.Cbind.Assets, asset));
            undos.Add(new UndoActionDelegate(effectContainerFile.Cbind, nameof(effectContainerFile.Cbind.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New ECF"));
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void EmoCreateNewAsset()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            Asset asset = Asset.Create(AssetType.EMO);
            effectContainerFile.Emo.AddAsset(asset, undos);
            SelectAsset(asset);

            //Undos
            undos.Add(new UndoActionDelegate(effectContainerFile.Emo, nameof(effectContainerFile.Emo.UpdateAssetFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New EMO Asset"));
        }


        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void CbindImportExternalEcf()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add file...";
            openFile.Filter = "ECF files | *.ecf";
            openFile.ShowDialog();

            if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
            {
                string newName = effectContainerFile.Cbind.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                Asset asset = new Asset(AssetType.CBIND);
                asset.Files.Add(new EffectFile()
                {
                    EcfFile = ECF_File.Load(openFile.FileName),
                    Extension = EffectFile.GetExtension(openFile.FileName),
                    fileType = EffectFile.FileType.ECF,
                    OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                    FileName = EffectFile.GetFileNameWithoutExtension(newName)
                });

                effectContainerFile.Cbind.Assets.Add(asset);
                SelectAsset(asset);
            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void EmoImportExternalFiles()
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
                    MessagePrompt.Show(string.Format("EMO assets cannot have more than 5 files. {0} were selected.\n\nImport cancelled.", openFile.FileNames.Length), "Import Asset", MessagePromptButtons.OK, MessagePromptIcon.Stop);
                    return;
                }

                Asset asset = new Asset(AssetType.EMO);

                foreach (string file in openFile.FileNames)
                {
                    string newName = effectContainerFile.Emo.GetUnusedName(System.IO.Path.GetFileName(file));

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

                effectContainerFile.Emo.Assets.Add(asset);
                SelectAsset(asset);

                UndoManager.Instance.AddUndo(new UndoableListAdd<Asset>(effectContainerFile.Emo.Assets, asset, "Add EMO"));
            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void LightImportExternal()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add file...";
            openFile.Filter = "light.ema files | *.light.ema";
            openFile.ShowDialog();

            if (!string.IsNullOrWhiteSpace(openFile.FileName) && File.Exists(openFile.FileName))
            {
                string newName = effectContainerFile.LightEma.GetUnusedName(System.IO.Path.GetFileName(openFile.FileName));

                Asset asset = new Asset(AssetType.LIGHT);
                asset.Files.Add(new EffectFile()
                {
                    EmaFile = EMA_File.Load(openFile.FileName),
                    Extension = EffectFile.GetExtension(openFile.FileName),
                    fileType = EffectFile.FileType.EMA,
                    OriginalFileName = EffectFile.GetFileNameWithoutExtension(openFile.FileName),
                    FileName = EffectFile.GetFileNameWithoutExtension(newName)
                });

                effectContainerFile.LightEma.Assets.Add(asset);
                SelectAsset(asset);
            }
        }


        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void AssetRemoveUnused(AssetType assetType)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            int amountRemoved = effectContainerFile.RemoveUnusedAssets(assetType, undos);

            if (amountRemoved > 0)
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, $"Remove Unusued ({assetType})"));
                MessagePrompt.Show(String.Format("{0} unused assets were removed.", amountRemoved), "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
            else
            {
                MessagePrompt.Show("There are no unused assets.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void AssetOpenSettings(AssetType assetType)
        {
            AssetContainerSettings assetSettings = new AssetContainerSettings(effectContainerFile.GetAssetContainer(assetType));
            assetSettings.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void AssetOpenTextureForm(AssetType assetType)
        {
            if (assetType == AssetType.EMO || assetType == AssetType.LIGHT || assetType == AssetType.CBIND)
                throw new ArgumentException("AssetOpenEditor: No container level textures exists for this asset type");

            AssetContainerOpenTextureForm(effectContainerFile.GetAssetContainer(assetType), assetType);
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void AssetOpenMaterialForm(AssetType assetType)
        {
            if (assetType == AssetType.EMO || assetType == AssetType.LIGHT || assetType == AssetType.CBIND)
                throw new ArgumentException("AssetOpenEditor: No container level materials exists for this asset type");

            AssetContainerOpenMaterialForm(effectContainerFile.GetAssetContainer(assetType), assetType);
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

        #region ImportAsset
        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportPbindAssetsFromGame(EntitySelector.EntityType fromType)
        {
            await ImportAssetsFromGame(AssetType.PBIND, fromType);
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportTbindAssetsFromGame(EntitySelector.EntityType fromType)
        {
            await ImportAssetsFromGame(AssetType.TBIND, fromType);
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportCbindAssetsFromGame(EntitySelector.EntityType fromType)
        {
            await ImportAssetsFromGame(AssetType.CBIND, fromType);
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportEmoAssetsFromGame(EntitySelector.EntityType fromType)
        {
            await ImportAssetsFromGame(AssetType.EMO, fromType);
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        private async Task ImportLightAssetsFromGame(EntitySelector.EntityType fromType)
        {
            await ImportAssetsFromGame(AssetType.LIGHT, fromType);
        }

        private async Task ImportAssetsFromGame(AssetType assetType, EntitySelector.EntityType fromType)
        {
            var effectFile = await LoadEepkFromGame(fromType);

            if (effectFile == null)
                return;

            switch (assetType)
            {
                case AssetType.EMO:
                    await AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO, effectFile);
                    break;
                case AssetType.PBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND, effectFile);
                    break;
                case AssetType.TBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND, effectFile);
                    break;
                case AssetType.CBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND, effectFile);
                    break;
                case AssetType.LIGHT:
                    await AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT, effectFile);
                    break;
            }
        }

        private async Task AssetContainer_ImportAssets(AssetContainerTool container, AssetType type, EffectContainerFile importFile = null)
        {
            importFile ??= await LoadEffectContainerFile();

            List<IUndoRedo> undos = new List<IUndoRedo>();
            int renameCount = 0;
            int alreadyExistCount = 0;
            int addedCount = 0;

            if (importFile != null)
            {
                AssetSelector assetSelector = new AssetSelector(importFile, true, true, type, this);
                assetSelector.ShowDialog();

                if (assetSelector.SelectedAssets != null)
                {
                    var controller = await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync($"Initializing...", $"", false, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, DialogTitleFontSize = 15, DialogMessageFontSize = 12 });
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
                    MessagePrompt.Show(String.Format("{0} assets were skipped because they already exist in this EEPK.\n\nHint: Use the duplicate function if that is what you want.", alreadyExistCount), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                if (renameCount > 0 && addedCount > 0)
                {
                    MessagePrompt.Show(String.Format("Assets imported.\n\nNote: {0} files were renamed during the add process due to existing file(s) having the same name.", renameCount), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else if (addedCount > 0)
                {
                    MessagePrompt.Show(String.Format("Assets imported."), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("Operation aborted.", "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                //Add Undos
                if (addedCount > 0)
                {
                    undos.Add(new UndoActionDelegate(container, nameof(container.UpdateAssetFilter), true));
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Asset"));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private async Task ImportAssetsFromExternalFile(AssetType assetType)
        {
            switch ((AssetType)assetType)
            {
                case AssetType.EMO:
                    await AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO);
                    break;
                case AssetType.PBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND);
                    break;
                case AssetType.TBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND);
                    break;
                case AssetType.CBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND);
                    break;
                case AssetType.LIGHT:
                    await AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT);
                    break;
            }
        }
        #endregion
    }
}
