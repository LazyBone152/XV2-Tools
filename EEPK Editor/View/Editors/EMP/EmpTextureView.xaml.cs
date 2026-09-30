using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Misc;
using EEPK_Organiser.ViewModel;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for EmpTextureView.xaml
    /// </summary>
    public partial class EmpTextureView : AutoObservableUserControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ITextureProperty = DependencyProperty.Register(nameof(TextureFile), typeof(ITexture), typeof(EmpTextureView), new PropertyMetadata(null));

        public ITexture TextureFile
        {
            get => (ITexture)GetValue(ITextureProperty);
            set => SetValue(ITextureProperty, value);
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
        public ObservableCollection<EMP_TextureSamplerDef> SelectedTextures { get; } = new ObservableCollection<EMP_TextureSamplerDef>();
        private EmpTextureViewModel _viewModel = new EmpTextureViewModel();
        public EmpTextureViewModel ViewModel => SelectedTexture != null ? _viewModel : null;

        public Visibility StaticVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.Static ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SpeedVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SpriteSheetVisibility => ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet ? Visibility.Visible : Visibility.Collapsed;

        private string FileType => TextureFile is EMP_File ? "EMP" : "ETR";

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
        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void TextureRemove()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            List<ITextureRef> textureInstances = new List<ITextureRef>();

            foreach (EMP_TextureSamplerDef texture in SelectedTextures)
            {
                textureInstances.AddRange(TextureFile.GetNodesThatUseTexture(texture));
            }

            if (MessagePrompt.Show(string.Format("The selected texture(s) will be deleted and all references to them on {0} nodes in this {1} will be removed.\n\nContinue?", textureInstances.Count, FileType), "Delete", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
            {
                var texturesToRemove = SelectedTextures.ToArray(); //Create a copy

                foreach (EMP_TextureSamplerDef texture in texturesToRemove)
                {
                    TextureFile.RemoveTextureReferences(texture, undos);
                    undos.Add(new UndoableListRemove<EMP_TextureSamplerDef>(TextureFile.Textures, texture, TextureFile.Textures.IndexOf(texture)));
                    TextureFile.Textures.Remove(texture);
                }

                UndoManager.Instance.AddCompositeUndo(undos, texturesToRemove.Length > 1 ? $"Delete Textures ({FileType})" : $"Delete Texture ({FileType})");
            }
        }

        [RelayCommand]
        private void TextureAdd()
        {
            EMP_TextureSamplerDef newTexture = EMP_TextureSamplerDef.GetNew();

            TextureFile.Textures.Add(newTexture);
            SelectTexture(newTexture);

            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_TextureSamplerDef>(TextureFile.Textures, newTexture, $"Add Texture ({FileType})"));
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void TextureDuplicate()
        {
            var selectedTextures = SelectedTextures;

            if(selectedTextures.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>(selectedTextures.Count);
                List<EMP_TextureSamplerDef> newTextures = new List<EMP_TextureSamplerDef>(selectedTextures.Count);

                foreach (EMP_TextureSamplerDef texture in selectedTextures)
                {
                    EMP_TextureSamplerDef newTexture = texture.Clone();
                    newTextures.Add(newTexture);
                    undos.Add(new UndoableListAdd<EMP_TextureSamplerDef>(TextureFile.Textures, newTexture));
                    TextureFile.Textures.Add(newTexture);

                }

                UndoManager.Instance.AddCompositeUndo(undos, selectedTextures.Count > 1 ? $"Duplicate Textures ({FileType})" : $"Duplicate Texture ({FileType})");
                SelectTexture(newTextures.ToArray());
            }
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void TextureCopy()
        {
            AssetContainer.File3_Ref.SaveDdsImages();
            CustomClipboard.SetData(ClipboardDataTypes.EmpTextureEntry, SelectedTextures);
        }

        [RelayCommand(CanExecute = nameof(CanPasteTexture))]
        private void TexturePaste()
        {
            if (!CustomClipboard.TryGetData(ClipboardDataTypes.EmpTextureEntry, out List<EMP_TextureSamplerDef> copiedTextures))
                return;

            if (copiedTextures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>(copiedTextures.Count * 2);
                List<EMP_TextureSamplerDef> newTextures = new List<EMP_TextureSamplerDef>(copiedTextures.Count);

                foreach (EMP_TextureSamplerDef textureEntry in copiedTextures)
                {
                    EMP_TextureSamplerDef newTexture = textureEntry.Clone();
                    newTextures.Add(newTexture);
                    Xv2CoreLib.EMB_CLASS.EMB_TextureEntry newEmbEntry = AssetContainer.File3_Ref.AddTexture(newTexture.TextureRef, undos);
                    newTexture.TextureRef = newEmbEntry;

                    TextureFile.Textures.Add(newTexture);

                    undos.Add(new UndoableListAdd<EMP_TextureSamplerDef>(TextureFile.Textures, newTexture));
                }

                UndoManager.Instance.AddCompositeUndo(undos, copiedTextures.Count > 1 ? "Paste Textures" : "Paste Texture");
                SelectTexture(newTextures.ToArray());
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteTextureValues))]
        private void TexturePasteValues()
        {
            if (!CustomClipboard.TryGetData(ClipboardDataTypes.EmpTextureEntry, out List<EMP_TextureSamplerDef> textures))
                return;

            if (textures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (textures.Count > 0)
                {
                    EMP_TextureSamplerDef newTexture = textures[0].Clone();
                    newTexture.TextureRef = AssetContainer.File3_Ref.AddTexture(newTexture.TextureRef, undos);

                    SelectedTexture.ReplaceValues(newTexture, undos);
                    UndoManager.Instance.AddCompositeUndo(undos, $"Paste Values ({FileType})");
                    ViewModel?.UpdateProperties();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanMergeTextures))]
        private void TextureMerge()
        {
            EMP_TextureSamplerDef texture = SelectedTexture;
            List<EMP_TextureSamplerDef> selectedTextures = SelectedTextures.ToList();
            selectedTextures.Remove(texture);

            if (texture != null && selectedTextures.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int count = selectedTextures.Count + 1;

                if (MessagePrompt.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.TextureName), string.Format("Merge ({0} textures)", count), MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var textureToRemove in selectedTextures)
                    {
                        TextureFile.RefactorTextureRef(textureToRemove, texture, undos);
                        undos.Add(new UndoableListRemove<EMP_TextureSamplerDef>(TextureFile.Textures, textureToRemove));
                        TextureFile.Textures.Remove(textureToRemove);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, $"Merge Textures ({FileType})");
                }
            }
            else
            {
                MessagePrompt.Show("Cannot merge with less than 2 textures selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessagePromptButtons.OK, MessagePromptIcon.Warning);
            }
        }


        private bool CanMergeTextures()
        {
            return SelectedTextures?.Count >= 2;
        }

        private bool CanPasteTextureValues()
        {
            return CanPasteTexture() && IsTextureSelected();
        }

        private bool IsTextureSelected()
        {
            return SelectedTexture != null;
        }

        private bool CanPasteTexture()
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.EmpTextureEntry);
        }
        #endregion

        #region TexturePropertyCommands
        [RelayCommand(CanExecute = nameof(IsEmbTextureSelected))]
        private void UnassignTexture()
        {
            ViewModel.SelectedEmbEntry = null;
        }

        [RelayCommand(CanExecute = nameof(IsEmbTextureSelected))]
        private void GotoTexture()
        {
            Forms.EmbEditForm textureViewer = EepkEditor.AssetContainerOpenTextureForm(AssetContainer, AssetContainer.ContainerAssetType);

            textureViewer.textureEditor.SelectedTexture = ViewModel.SelectedEmbEntry;
            textureViewer.textureEditor.textureDataGrid.ScrollIntoView(ViewModel.SelectedEmbEntry);
        }


        private bool IsEmbTextureSelected()
        {
            return ViewModel?.SelectedEmbEntry != null;
        }
        #endregion

        #region KeyframeCommands
        private List<EMP_ScrollKeyframe> SelectedKeyframes => keyframeDataGrid.SelectedItems.Cast<EMP_ScrollKeyframe>().ToList();

        [RelayCommand(CanExecute = nameof(IsSpriteSheet))]
        private void AddKeyframe()
        {
            EMP_ScrollKeyframe keyframe = new EMP_ScrollKeyframe();
            keyframe.Time = 2;
            SelectedTexture.ScrollState.Keyframes.Add(keyframe);
            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe, $"{FileType} Texture -> Add Keyframe"));
        }

        [RelayCommand(CanExecute = nameof(IsKeyframeSelected))]
        private void DeleteKeyframe()
        {
            List<EMP_ScrollKeyframe> keyframes = SelectedKeyframes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(EMP_ScrollKeyframe keyframe in keyframes)
            {
                if (SelectedTexture.ScrollState.Keyframes.Count == 1) break;
                undos.Add(new UndoableListRemove<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe));
                SelectedTexture.ScrollState.Keyframes.Remove(keyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, $"{FileType} Texture -> Delete Keyframe");
        }

        [RelayCommand(CanExecute = nameof(IsKeyframeSelected))]
        private void CopyKeyframe()
        {
            CustomClipboard.SetData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME, SelectedKeyframes);
        }

        [RelayCommand(CanExecute = nameof(CanPasteKeyframe))]
        private void PasteKeyframe()
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME, out List<EMP_ScrollKeyframe> keyframes))
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (EMP_ScrollKeyframe keyframe in keyframes)
            {
                undos.Add(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, keyframe));
                SelectedTexture.ScrollState.Keyframes.Add(keyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, $"{FileType} Texture -> Paste Keyframe");
        }

        [RelayCommand(CanExecute = nameof(IsKeyframeSelected))]
        private void DuplicateKeyframe()
        {
            List<EMP_ScrollKeyframe> keyframes = SelectedKeyframes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (EMP_ScrollKeyframe keyframe in keyframes)
            {
                EMP_ScrollKeyframe newKeyframe = keyframe.Clone();
                undos.Add(new UndoableListAdd<EMP_ScrollKeyframe>(SelectedTexture.ScrollState.Keyframes, newKeyframe));
                SelectedTexture.ScrollState.Keyframes.Add(newKeyframe);
            }

            UndoManager.Instance.AddCompositeUndo(undos, $"{FileType} Texture -> Duplicate Keyframe");
        }


        private bool CanPasteKeyframe()
        {
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_TEXTURE_KEYFRAME) && IsTextureSelected();
        }

        private bool IsSpriteSheet()
        {
            return ViewModel?.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet;
        }

        private bool IsKeyframeSelected()
        {
            return ViewModel?.SelectedKeyframe != null;
        }
        #endregion

        public void SelectTexture(params EMP_TextureSamplerDef[] textures)
        {
            SelectedTextures.Clear();

            if (textures.Length == 0)
            {
                SelectedTexture = null;
                return;
            }

            if(textures.Length == 1)
            {
                SelectedTexture = textures[0];
            }
            else
            {
                foreach(var texture in textures)
                {
                    SelectedTextures.Add(texture);
                }
            }

            textureListBox.ScrollIntoView(textures[0]);
        }
    }
}
