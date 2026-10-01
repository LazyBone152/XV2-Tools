using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Misc;
using EEPK_Organiser.ViewModel;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
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
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View
{
    public partial class EepkEditor : AutoObservableUserControl
    {
        public EffectPartViewModel EffectPartViewModel { get; private set; } = new EffectPartViewModel();
        public bool IsEffectPartEnabled => SelectedEffectPart != null;
        public Visibility EffectPartVisibility => SelectedEffectPart != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoEffectPartVisibility => SelectedEffectPart == null ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateViewModel()
        {
            EffectPartViewModel.ChangeModel(SelectedEffectPart);
        }

        #region Selection
        private Effect _selectedEffect = null;
        public Effect SelectedEffect
        {
            get => _selectedEffect;
            set
            {
                _selectedEffect = value;
                NotifyPropertyChanged(nameof(SelectedEffect));
                NotifyPropertyChanged(nameof(SelectedEffectID));

                //Force unselect effect part if it does not belong to currently selected effect
                if (SelectedEffectPart != null && _selectedEffect != null && !_selectedEffect.EffectParts.Contains(SelectedEffectPart))
                {
                    SelectedEffectPart = null;
                    SelectedEffectParts.Clear();

                    //Select first available effect part on effect
                    //if(SelectedEffect.EffectParts.Count > 0)
                    //    SelectedEffectPart = SelectedEffect.EffectParts[0];
                }
            }
        }
        public ObservableCollection<Effect> SelectedEffects { get; } = new();

        public int SelectedEffectID
        {
            get => SelectedEffect != null ? SelectedEffect.IndexNum : 0;
            set
            {
                if (SelectedEffect != null && SelectedEffect?.IndexNum != value)
                {
                    if (effectContainerFile.Effects.FirstOrDefault(x => x.IndexNum == value) == null)
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
                        MessagePrompt.Show($"This ID ({value}) is already used for another effect.", "ID Used", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    }
                }

            }
        }

        public ObservableCollection<EffectPart> SelectedEffectParts { get; } = new();
        private EffectPart _selectedEffectPart = null;
        public EffectPart SelectedEffectPart
        {
            get
            {
                return _selectedEffectPart;
            }
            set
            {
                if (value != _selectedEffectPart)
                {
                    _selectedEffectPart = value;
                    UpdateViewModel();
                    NotifyPropertyChanged(nameof(SelectedEffectPart));
                    NotifyPropertyChanged(nameof(IsEffectPartEnabled));
                    NotifyPropertyChanged(nameof(EffectPartVisibility));
                    NotifyPropertyChanged(nameof(NoEffectPartVisibility));
                }
            }
        }

        private void SelectEffect(Effect effect)
        {
            SelectedEffects.Clear();
            SelectedEffect = effect;

            if(effect != null)
                effectDataGrid.ScrollIntoView(effect);
        }

        private void SetSelectEffects(IList<Effect> effects)
        {
            if (effects.Count == 0) return;

            SelectedEffect = effects[0];
            SelectedEffects.Clear();

            foreach (var material in effects)
            {
                SelectedEffects.Add(material);
            }

            effectDataGrid.ScrollIntoView(SelectedEffect);
        }

        private void SelectEffectPart(EffectPart effectPart)
        {
            Effect parent = effectContainerFile.GetEffectAssociatedWithEffectPart(effectPart);
            SelectEffect(parent);
            SelectedEffectPart = effectPart;
            SelectedEffectParts.Clear();
        }

        private void SetSelectedEffectParts(IList<EffectPart> effectParts)
        {
            SelectedEffectParts.Clear();

            foreach(var effectPart in effectParts)
            {
                SelectedEffectParts.Add(effectPart);
            }

            SelectEffect(effectParts.Count > 0 ? effectContainerFile.GetEffectAssociatedWithEffectPart(effectParts[0]) : null);
        }

        /// <summary>
        /// Helper method to synchronize <see cref="SelectedEffect"/> with <see cref="SelectedEffectParts"/>. This is a workaround for an issue when <see cref="SelectedEffect"/> is set to null incorrectly by external UI code when selection changes in the embedded ListBox.
        /// </summary>
        private void SyncSelectedEffectWithParts()
        {
            SelectEffect(SelectedEffectParts?.Count > 0 ? effectContainerFile.GetEffectAssociatedWithEffectPart(SelectedEffectParts[0]) : null);
        }
        #endregion

        #region Effect

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void EffectNew()
        {
            Effect newEffect = new Effect();
            newEffect.IndexNum = effectContainerFile.GetUnusedEffectId(0);
            List<IUndoRedo> undos = effectContainerFile.AddEffect(newEffect, false, true);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New Effect"));

            SelectEffect(newEffect);

#if XenoKit
            XenoKit.Editor.Log.Add("Effect added with ID: " + newEffect.IndexNum);
#endif
        }

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        private void Effect_AddEffectPart()
        {
            if (SelectedEffect != null)
            {
                if (SelectedEffect.EffectParts == null)
                    SelectedEffect.EffectParts = new AsyncObservableCollection<EffectPart>();

                var newEffectPart = new EffectPart();
                SelectedEffect.EffectParts.Add(newEffectPart);

                UndoManager.Instance.AddUndo(new UndoableListAdd<EffectPart>(SelectedEffect.EffectParts, newEffectPart, "New EffectPart"));
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        private void Effect_Copy()
        {
            if (SelectedEffects.Count > 0)
            {
                effectContainerFile.SaveDds();
                CustomClipboard.SetData(ClipboardDataTypes.Effect, SelectedEffects.ToArray());
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteEffect))]
        private void Effect_Paste()
        {
            CustomClipboard.TryGetData(ClipboardDataTypes.Effect, out Effect[] effects);

            if (effects != null)
            {
                //Add effects
                EffectOptions_ImportEffects(new AsyncObservableCollection<Effect>(effects));
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        private void Effect_DeleteEffect()
        {
            var selectedEffects = SelectedEffects.ToArray();

            if (selectedEffects.Length > 0)
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

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        private void Effect_Duplicate()
        {
            if (SelectedEffect != null)
            {
                var copiedEffect = SelectedEffect.Clone();
                copiedEffect.IndexNum = effectContainerFile.GetUnusedEffectId(copiedEffect.IndexNum);
                effectContainerFile.Effects.Add(copiedEffect);
                SelectEffect(copiedEffect);

                UndoManager.Instance.AddUndo(new UndoableListAdd<Effect>(effectContainerFile.Effects, copiedEffect, "Duplicate Effect"));
#if XenoKit
                XenoKit.Editor.Log.Add("Effect duplicated with ID: " + copiedEffect.IndexNum);
#endif
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectSelected))]
        private void Effect_ExportSelected()
        {
            if (SelectedEffects.Count > 0)
            {
                ExportVfxPackage(SelectedEffects.ToList());
            }
        }

        #endregion

        #region EffectPart
        [RelayCommand(CanExecute = nameof(CanPasteEffectPart))]
        private void EffectPart_Paste()
        {
            Effect selectedEffect = SelectedEffect;

            if (selectedEffect != null)
            {
                CustomClipboard.TryGetData(ClipboardDataTypes.EffectPart, out ObservableCollection<EffectPart> effectParts);

                if (effectParts != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    List<EffectPart> newEffectParts = new List<EffectPart>(effectParts.Count);

                    try
                    {
                        foreach (var effectPart in effectParts)
                        {
                            if (effectPart.AssetRef != null)
                                effectPart.AssetRef = effectContainerFile.AddAsset(effectPart.AssetRef, effectPart.AssetType, undos);

                            var newEffectPart = effectPart.Clone();
                            newEffectParts.Add(newEffectPart);
                            selectedEffect.EffectParts.Add(newEffectPart);

                            undos.Add(new UndoableListAdd<EffectPart>(selectedEffect.EffectParts, newEffectPart));
                        }
                    }
                    finally
                    {
                        if (undos.Count > 0)
                        {
                            UndoManager.Instance.AddUndo(new CompositeUndo(undos, (effectParts.Count > 1) ? "Paste EffectParts" : "Paste EffectPart"));
                            SetSelectedEffectParts(newEffectParts);
                        }
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectPartSelected))]
        private void EffectPart_Copy()
        {
            if (SelectedEffect != null)
            {
                if (SelectedEffectParts != null)
                {
                    effectContainerFile.SaveDds();
                    CustomClipboard.SetData(ClipboardDataTypes.EffectPart, SelectedEffectParts);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectPartSelected))]
        private void EffectPart_Delete()
        {
            Effect selectedEffect = SelectedEffect;

            if (selectedEffect != null)
            {
                if (SelectedEffectParts != null)
                {
                    List<EffectPart> effectPartsToRemove = SelectedEffectParts.ToList();
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var effectPart in effectPartsToRemove)
                    {
                        undos.Add(new UndoableListRemove<EffectPart>(selectedEffect.EffectParts, effectPart));
                        selectedEffect.EffectParts.Remove(effectPart);
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, (SelectedEffectParts.Count > 1) ? "Delete EffectParts" : "Delete EffectPart"));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectPartSelected))]
        private void EffectPart_Duplicate()
        {
            Effect selectedEffect = SelectedEffect;

            if (selectedEffect != null)
            {
                if (SelectedEffectParts != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    List<EffectPart> newEffectParts = new List<EffectPart>(SelectedEffectParts.Count);

                    foreach (var effectPart in SelectedEffectParts)
                    {
                        var clone = effectPart.Clone();
                        newEffectParts.Add(clone);
                        selectedEffect.EffectParts.Add(clone);
                        undos.Add(new UndoableListAdd<EffectPart>(selectedEffect.EffectParts, clone));
                    }

                    SetSelectedEffectParts(newEffectParts);
                    UndoManager.Instance.AddCompositeUndo(undos, "EffectPart Duplicate");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToAsset))]
        private void EffectPart_GoToAsset()
        {
            if (SelectedEffectPart != null)
            {
                switch (SelectedEffectPart.AssetType)
                {
                    case AssetType.PBIND:
                        tabControl.SelectedIndex = (int)Tabs.Pbind;
                        break;
                    case AssetType.TBIND:
                        tabControl.SelectedIndex = (int)Tabs.Tbind;
                        break;
                    case AssetType.CBIND:
                        tabControl.SelectedIndex = (int)Tabs.Cbind;
                        break;
                    case AssetType.EMO:
                        tabControl.SelectedIndex = (int)Tabs.Emo;
                        break;
                    case AssetType.LIGHT:
                        tabControl.SelectedIndex = (int)Tabs.Light;
                        break;
                }

                SelectAsset(SelectedEffectPart.AssetRef);
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectPartSelected))]
        private void EffectPart_ChangeAsset()
        {
            if (SelectedEffectPart != null)
            {
                Forms.AssetSelector assetSel = new Forms.AssetSelector(effectContainerFile, false, false, SelectedEffectPart.AssetType, this, SelectedEffectPart.AssetRef);
                assetSel.ShowDialog();

                if (assetSel.SelectedAsset != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var effectPart in SelectedEffectParts)
                    {
                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetType), effectPart, effectPart.AssetType, assetSel.SelectedAssetType));
                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetRef), effectPart, effectPart.AssetRef, assetSel.SelectedAsset));

                        effectPart.AssetType = assetSel.SelectedAssetType;
                        effectPart.AssetRef = assetSel.SelectedAsset;
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Change Asset(s)"));

                    if (EffectPartViewModel != null)
                        EffectPartViewModel.UpdateProperties();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsEffectPartSelected))]
        private void EffectPart_Rescale()
        {
            if (SelectedEffectParts != null)
            {
                LB_Common.Forms.NumericInput inputForm = new LB_Common.Forms.NumericInput("Rescale", "Rescale Factor", 1f, 0, 50, 0.1, null, "The minimum and maximum sizes of the selected Effect Parts will be rescaled by the entered factor.");
                inputForm.ShowDialog();

                if (!inputForm.IsCancelled)
                {
                    float scaleFactor = inputForm.GetValue<float>();

                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var effectPart in SelectedEffectParts)
                    {
                        float size1 = effectPart.ScaleMin * scaleFactor;
                        float size2 = effectPart.ScaleMax * scaleFactor;

                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.ScaleMin), effectPart, effectPart.ScaleMin, size1));
                        undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.ScaleMax), effectPart, effectPart.ScaleMax, size2));

                        effectPart.ScaleMin = size1;
                        effectPart.ScaleMax = size2;
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Rescale EffectPart");
                    EffectPartViewModel?.UpdateProperties();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteEffectPartValues))]
        private void EffectPart_PasteValues()
        {
            if (SelectedEffectPart == null) return;

            if (!CustomClipboard.TryGetData(ClipboardDataTypes.EffectPart, out ObservableCollection<EffectPart> effectParts))
                return;

            if (effectParts == null) return;
            if (effectParts.Count != 1) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            effectParts[0].AssetRef = effectContainerFile.AddAsset(effectParts[0].AssetRef, effectParts[0].AssetType, undos);
            SelectedEffectPart.CopyValues(effectParts[0], undos);

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste Values"));

            if (EffectPartViewModel != null)
                EffectPartViewModel.UpdateProperties();
        }

        [RelayCommand]
        private void EffectPart_DoubleClick()
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

        private void EffectPartListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //MouseBinding isn't working, so as a quick fix I'm using an event
            if (e.LeftButton == MouseButtonState.Pressed)
                EffectPart_DoubleClick();
        }

        private void EffectPartListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Force select parent effect of selected effect parts
            //This is a workaround for a bug where selecting effect parts in the embedded list box can cause the SelectedEffect in the parent DataGrid to be set to null
            //Has a side effect of requiring two clicks to select a new effect part under a different effect
            SyncSelectedEffectWithParts();
        }

        //EffectPart_EditAsset_Command
        [RelayCommand(CanExecute = nameof(CanEditAsset))]
        private void EffectPart_OpenEditor()
        {
            if (SelectedEffectPart != null)
            {
                switch (SelectedEffectPart.AssetType)
                {
                    case AssetType.PBIND:
                        PBIND_OpenEditor(SelectedEffectPart.AssetRef.Files[0].EmpFile, effectContainerFile.Pbind, SelectedEffectPart.AssetRef.Files[0].FileName);
                        break;
                    case AssetType.TBIND:
                        TBIND_OpenEditor(SelectedEffectPart.AssetRef.Files[0].EtrFile, effectContainerFile.Tbind, SelectedEffectPart.AssetRef.Files[0].FileName);
                        break;
                    case AssetType.CBIND:
                        CBIND_OpenEditor(SelectedEffectPart.AssetRef.Files[0].EcfFile, SelectedEffectPart.AssetRef.Files[0].FileName);
                        break;
                }
            }
        }

        #endregion

        #region EffectName

        [RelayCommand(CanExecute = nameof(CanRenameEffect))]
        private void EffectRenameBegin()
        {
            nameListColumn.IsReadOnly = false;
            effectDataGrid.CurrentColumn = nameListColumn;
            effectDataGrid.BeginEdit();
        }

        [RelayCommand]
        private void EffectRenameCommit()
        {
            if (!IsRenameInProgress()) return;
            nameListColumn.IsReadOnly = true;
            effectDataGrid.CurrentColumn = nameListColumn;
            effectDataGrid.CommitEdit();
        }

        private void EffectDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column == nameListColumn)
            {
                nameListColumn.IsReadOnly = true;
            }
        }

        private bool CanRenameEffect()
        {
            return nameListColumn.IsReadOnly && IsEffectSelected();
        }

        private bool IsRenameInProgress()
        {
            return !nameListColumn.IsReadOnly && IsEffectSelected();
        }
        #endregion

        #region ImportEffect
        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        public async Task ImportEffectsFromFile()
        {
            var importEffectFile = await LoadEffectContainerFile();
            if (importEffectFile != null)
                EffectOptions_ImportEffects(importEffectFile.Effects);
        }

        [RelayCommand(CanExecute = nameof(CanImportFromGame))]
        public async Task ImportEffectsFromGame(EntitySelector.EntityType type)
        {
            var importEffectFile = await LoadEepkFromGame(type);
            if (importEffectFile != null)
                EffectOptions_ImportEffects(importEffectFile.Effects);
        }

        private void EffectOptions_ImportEffects(AsyncObservableCollection<Effect> effects)
        {
            if (effects != null)
            {
                EffectSelector effectSelector = new EffectSelector(effects, effectContainerFile, Application.Current.MainWindow);
                effectSelector.ShowDialog();
                List<IUndoRedo> undos = new List<IUndoRedo>();

                try
                {
                    if (effectSelector.SelectedEffects != null)
                    {
                        foreach (var effect in effectSelector.SelectedEffects)
                        {
                            var newEffect = effect.Clone();
                            undos.AddRange(effectContainerFile.AddEffect(newEffect, true));
                            undos.Add(new UndoableListAdd<Effect>(effectContainerFile.Effects, newEffect));
                        }

                        //Update UI
                        if (effectSelector.SelectedEffects.Count > 0)
                        {
                            SelectEffect(effectSelector.SelectedEffects[0]);
                            effectContainerFile.UpdateEffectFilter();
                            undos.Add(new UndoActionDelegate(effectContainerFile, nameof(effectContainerFile.UpdateAllFilters), true));
                        }
                    }
                }
                finally
                {
                    if (undos.Count > 0)
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Effects"));
                }
            }
        }

        private bool CanImportFromGame()
        {
            return GameDirectoryCheck() && IsFileLoaded;
        }
        #endregion

        #region Helper
        public void ExportVfxPackage(IList<Effect> selEffects = null)
        {
            if (effectContainerFile == null) return;

            //if selEffects is null, then pass in all the effects.
            IList<Effect> effects = (selEffects != null) ? selEffects : effectContainerFile.Effects;

            EffectSelector effectSelector = new EffectSelector(effects, effectContainerFile, Application.Current.MainWindow, EffectSelector.Mode.ExportEffect);
            effectSelector.ShowDialog();

            if (effectSelector.SelectedEffects != null)
            {
                EffectContainerFile vfxPackage = EffectContainerFile.New();
                vfxPackage.saveFormat = SaveFormat.VfxPackage;

                foreach (var effect in effectSelector.SelectedEffects)
                {
                    var newEffect = effect.Clone();
                    vfxPackage.AddEffect(newEffect);
                }

                //Get path to save to
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Export to .vfxpackage";
                saveDialog.Filter = string.Format("{1} File |*{0}", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0, 1));
                saveDialog.AddExtension = true;

                if (saveDialog.ShowDialog() == true)
                {
                    vfxPackage.Directory = string.Format("{0}/{1}", Path.GetDirectoryName(saveDialog.FileName), Path.GetFileNameWithoutExtension(saveDialog.FileName));
                    vfxPackage.Name = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    vfxPackage.SaveVfxPackage();

                    MessagePrompt.Show("Export successful.", "Export", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }

            }
        }

        //CanExecutes
        private bool CanGoToAsset()
        {
            if (IsEffectPartSelected())
            {
                return SelectedEffectPart.AssetRef != null;
            }
            return false;
        }

        private bool CanEditAsset()
        {
            if (IsEffectPartSelected())
            {
                return SelectedEffectPart.AssetType == AssetType.CBIND || SelectedEffectPart.AssetType == AssetType.TBIND || SelectedEffectPart.AssetType == AssetType.PBIND;
            }
            return false;
        }

        private bool IsEffectPartSelected()
        {
            return SelectedEffectPart != null;
        }

        private bool IsEffectSelected()
        {
            return SelectedEffect != null;
        }

        private bool CanPasteEffect()
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.Effect);
        }

        private bool CanPasteEffectPart()
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.EffectPart);
        }

        private bool CanPasteEffectPartValues()
        {
            return (IsEffectPartSelected()) ? CustomClipboard.ContainsData(ClipboardDataTypes.EffectPart) : false;
        }
        #endregion
    }
}
