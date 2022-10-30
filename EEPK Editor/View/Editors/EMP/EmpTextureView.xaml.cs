using EEPK_Organiser.Misc;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for EmpTextureView.xaml
    /// </summary>
    public partial class EmpTextureView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty EmpFileProperty = DependencyProperty.Register(nameof(EmpFile), typeof(EMP_File), typeof(EmpTextureView), new PropertyMetadata(null));

        public EMP_File EmpFile
        {
            get => (EMP_File)GetValue(EmpFileProperty);
            set => SetValue(EmpFileProperty, value);
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(EmpTextureView), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get => (AssetContainerTool)GetValue(AssetContainerProperty);
            set => SetValue(AssetContainerProperty, value);
        }

        #endregion

        private EMP_TextureSamplerDef _selectedTexture = null;
        public EMP_TextureSamplerDef SelectedTexture
        {
            get => _selectedTexture;
            set
            {
                _selectedTexture = value;
                _viewModel.SetContext(value);
                NotifyPropertyChanged(nameof(SelectedTexture));
                NotifyPropertyChanged(nameof(ViewModel));
                UpdateVisibilities();
            }
        }
        public List<EMP_TextureSamplerDef> SelectedTextures => textureListBox.SelectedItems.Cast<EMP_TextureSamplerDef>().ToList();

        private EmpTextureViewModel _viewModel = new EmpTextureViewModel();
        public EmpTextureViewModel ViewModel => SelectedTexture != null ? _viewModel : null;

        public Visibility StaticVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.Static ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SpeedVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SpriteSheetVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet ? Visibility.Visible : Visibility.Collapsed;

        public EmpTextureView()
        {
            DataContext = this;
            InitializeComponent();
            Unloaded += EmpTextureView_Unloaded;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
        }

        private void _viewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(EMP_ScrollState.ScrollType))
            {
                UpdateVisibilities();
            }
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            ViewModel?.UpdateProperties();
        }

        private void EmpTextureView_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void UpdateVisibilities()
        {
            NotifyPropertyChanged(nameof(StaticVisibility));
            NotifyPropertyChanged(nameof(SpeedVisibility));
            NotifyPropertyChanged(nameof(SpriteSheetVisibility));
        }

        #region Commands
        public RelayCommand TextureRemoveCommand => new RelayCommand(TextureRemove, IsTextureSelected);
        private void TextureRemove()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            List<ParticleTexture> textureInstances = new List<ParticleTexture>();

            foreach (var texture in SelectedTextures)
            {
                textureInstances.AddRange(EmpFile.GetTexturePartsThatUseEmbEntryRef(texture));
            }

            if (MessageBox.Show(String.Format("The selected texture(s) will be deleted and all references to them on {0} Particle Effects in this EMP will be removed.\n\nContinue?", textureInstances.Count, SelectedTextures.Count), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var texture in SelectedTextures)
                {
                    EmpFile.RemoveTextureReferences(texture, undos);
                    undos.Add(new UndoableListRemove<EMP_TextureSamplerDef>(EmpFile.Textures, texture));
                    EmpFile.Textures.Remove(texture);
                }

                UndoManager.Instance.AddCompositeUndo(undos, SelectedTextures.Count > 1 ? "Delete Textures (EMP)" : "Delete Texture (EMP)");
            }
        }

        public RelayCommand TextureAddCommand => new RelayCommand(TextureAdd);
        private void TextureAdd()
        {
            EMP_TextureSamplerDef newTexture = EMP_TextureSamplerDef.GetNew();

            EmpFile.Textures.Add(newTexture);
            SelectTexture(newTexture);

            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_TextureSamplerDef>(EmpFile.Textures, newTexture, "Add Texture (EMP)"));
        }

        public RelayCommand TextureDuplicateCommand => new RelayCommand(TextureDuplicate, IsTextureSelected);
        private void TextureDuplicate()
        {
            EMP_TextureSamplerDef newTexture = SelectedTexture.Clone();
            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_TextureSamplerDef>(EmpFile.Textures, newTexture, "Duplicate Texture (EMP)"));
            EmpFile.Textures.Add(newTexture);

            SelectTexture(newTexture);
        }

        public RelayCommand TextureCopyCommand => new RelayCommand(TextureCopy, IsTextureSelected);
        private void TextureCopy()
        {
            AssetContainer.File3_Ref.SaveDdsImages();
            Clipboard.SetData(ClipboardDataTypes.EmpTextureEntry, SelectedTextures);
        }

        public RelayCommand TexturePasteCommand => new RelayCommand(TexturePaste, CanPasteTexture);
        private void TexturePaste()
        {
            List<EMP_TextureSamplerDef> copiedTextures = (List<EMP_TextureSamplerDef>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

            if (copiedTextures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (EMP_TextureSamplerDef textureEntry in copiedTextures)
                {
                    EMP_TextureSamplerDef newTexture = textureEntry.Clone();
                    Xv2CoreLib.EMB_CLASS.EmbEntry newEmbEntry = AssetContainer.File3_Ref.Add(newTexture.TextureRef, undos);
                    newTexture.TextureRef = newEmbEntry;

                    EmpFile.Textures.Add(newTexture);

                    undos.Add(new UndoableListAdd<EMP_TextureSamplerDef>(EmpFile.Textures, newTexture));
                }

                UndoManager.Instance.AddCompositeUndo(undos, copiedTextures.Count > 1 ? "Paste Textures" : "Paste Texture");
            }
        }

        public RelayCommand TexturePasteValuesCommand => new RelayCommand(TexturePasteValues, () => CanPasteTexture() && IsTextureSelected());
        private void TexturePasteValues()
        {
            List<EMP_TextureSamplerDef> textures = (List<EMP_TextureSamplerDef>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

            if (textures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (textures.Count > 0)
                {
                    EMP_TextureSamplerDef newTexture = textures[0].Clone();
                    newTexture.TextureRef = AssetContainer.File3_Ref.Add(newTexture.TextureRef, undos);

                    SelectedTexture.ReplaceValues(newTexture, undos);
                    UndoManager.Instance.AddCompositeUndo(undos, "Paste Values (EMP)");
                }
            }
        }

        public RelayCommand TextureMergeCommand => new RelayCommand(TextureMerge, () => SelectedTextures?.Count >= 2);
        private void TextureMerge()
        {
            EMP_TextureSamplerDef texture = SelectedTexture;
            List<EMP_TextureSamplerDef> selectedTextures = SelectedTextures.ToList();
            selectedTextures.Remove(texture);

            if (texture != null && selectedTextures.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int count = selectedTextures.Count + 1;

                if (MessageBox.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.TextureName), string.Format("Merge ({0} textures)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    foreach (var textureToRemove in selectedTextures)
                    {
                        EmpFile.RefactorTextureRef(textureToRemove, texture, undos);
                        undos.Add(new UndoableListRemove<EMP_TextureSamplerDef>(EmpFile.Textures, textureToRemove));
                        EmpFile.Textures.Remove(textureToRemove);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Merge Textures (EMP)");
                }
            }
            else
            {
                MessageBox.Show("Cannot merge with less than 2 textures selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private bool IsTextureSelected()
        {
            return SelectedTexture != null;
        }

        private bool CanPasteTexture()
        {
            return Clipboard.ContainsData(ClipboardDataTypes.EmpTextureEntry);
        }
        #endregion

        #region TexturePropertyCommands
        public RelayCommand UnassignTextureCommand => new RelayCommand(UnassignTexture, () => ViewModel?.SelectedEmbEntry != null);
        private void UnassignTexture()
        {
            ViewModel.SelectedEmbEntry = null;
        }

        public RelayCommand GotoTextureCommand => new RelayCommand(GotoTexture, () => ViewModel?.SelectedEmbEntry != null);
        private void GotoTexture()
        {
            Forms.EmbEditForm textureViewer = EepkEditor.PBIND_OpenTextureViewer(AssetContainer, Xv2CoreLib.EEPK.AssetType.PBIND);
            textureViewer.textureEditor.SelectedTexture = ViewModel.SelectedEmbEntry;
            textureViewer.textureEditor.textureDataGrid.ScrollIntoView(ViewModel.SelectedEmbEntry);
        }

        #endregion

        #region KeyframeCommands
        private List<EMP_ScrollKeyframe> SelectedKeyframes => keyframeDataGrid.SelectedItems.Cast<EMP_ScrollKeyframe>().ToList();

        public RelayCommand AddKeyframeCommand => new RelayCommand(AddKeyframe, () => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet);
        private void AddKeyframe()
        {
            EMP_ScrollKeyframe keyframe = new EMP_ScrollKeyframe();
            keyframe.Time = 2;
            SelectedTexture.ScrollState.Keyframes.Add(keyframe);
            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe, "EMP Texture -> Add Keyframe"));
        }

        public RelayCommand DeleteKeyframeCommand => new RelayCommand(DeleteKeyframe, IsKeyframeSelected);
        private void DeleteKeyframe()
        {
            List<EMP_ScrollKeyframe> keyframes = SelectedKeyframes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(EMP_ScrollKeyframe keyframe in keyframes)
            {
                undos.Add(new UndoableListRemove<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe));
                SelectedTexture.ScrollState.Keyframes.Remove(keyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP Texture -> Delete Keyframe");
        }

        public RelayCommand CopyKeyframeCommand => new RelayCommand(CopyKeyframe, IsKeyframeSelected);
        private void CopyKeyframe()
        {
            Clipboard.SetData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME, SelectedKeyframes);
        }

        public RelayCommand PasteKeyframeCommand => new RelayCommand(PasteKeyframe, () => Clipboard.ContainsData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME) && IsTextureSelected());
        private void PasteKeyframe()
        {
            List<EMP_ScrollKeyframe> keyframes = (List<EMP_ScrollKeyframe>)Clipboard.GetData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (EMP_ScrollKeyframe keyframe in keyframes)
            {
                undos.Add(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe));
                SelectedTexture.ScrollState.Keyframes.Add(keyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP Texture -> Paste Keyframe");
        }

        public RelayCommand DuplicateKeyframeCommand => new RelayCommand(DuplicateKeyframe, IsKeyframeSelected);
        private void DuplicateKeyframe()
        {
            List<EMP_ScrollKeyframe> keyframes = SelectedKeyframes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (EMP_ScrollKeyframe keyframe in keyframes)
            {
                undos.Add(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe));
                SelectedTexture.ScrollState.Keyframes.Add(keyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "EMP Texture -> Duplicate Keyframe");
        }


        private bool IsKeyframeSelected()
        {
            return ViewModel?.SelectedKeyframe != null;
        }
        #endregion

        private void SelectTexture(EMP_TextureSamplerDef texture)
        {
            if (EmpFile.Textures.Contains(texture))
            {
                SelectedTexture = texture;
                textureListBox.ScrollIntoView(texture);
            }
        }
    }
}
