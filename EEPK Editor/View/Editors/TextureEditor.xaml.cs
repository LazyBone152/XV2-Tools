using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Misc;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for TextureEditor.xaml
    /// </summary>
    public partial class TextureEditor : AutoObservableUserControl, IDisposable
    {
        #region DependencyProperty
        public static readonly DependencyProperty EmbFileProperty = DependencyProperty.Register(nameof(EmbFile), typeof(EMB_TextureFile), typeof(TextureEditor), new PropertyMetadata(null));

        public EMB_TextureFile EmbFile
        {
            get { return (EMB_TextureFile)GetValue(EmbFileProperty); }
            set
            {
                SetValue(EmbFileProperty, value);
                NotifyPropertyChanged(nameof(EmbFile));
                NotifyPropertyChanged(nameof(TextureCount));
            }
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(TextureEditor), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get { return (AssetContainerTool)GetValue(AssetContainerProperty); }
            set
            {
                SetValue(AssetContainerProperty, value);
                NotifyPropertyChanged(nameof(AssetContainer));
                NotifyPropertyChanged(nameof(ContainerVisiblility));
                NotifyPropertyChanged(nameof(InverseContainerVisiblility));
                NotifyPropertyChanged(nameof(PbindVisiblility));
                NotifyPropertyChanged(nameof(IsForContainer));
            }
        }

        public static readonly DependencyProperty TextureEditorTypeProperty = DependencyProperty.Register(nameof(TextureEditorType), typeof(TextureEditorType), typeof(TextureEditor), new PropertyMetadata(null));

        public TextureEditorType TextureEditorType
        {
            get { return (TextureEditorType)GetValue(TextureEditorTypeProperty); }
            set
            {
                SetValue(TextureEditorTypeProperty, value);
                NotifyPropertyChanged(nameof(TextureEditorType));
            }
        }

        #endregion

        #region Properties
        private EMB_TextureEntry _selectedTexture = null;
        public EMB_TextureEntry SelectedTexture
        {
            get => _selectedTexture;
            set
            {
                _selectedTexture = value;
                NotifyPropertyChanged(nameof(SelectedTexture));
            }
        }
        public ObservableCollection<EMB_TextureEntry> SelectedTextures { get; } = new ObservableCollection<EMB_TextureEntry>();
        public bool AllowTextureNames
        {
            get => EmbFile?.UseFileNames == true;
            set
            {
                if(EmbFile.UseFileNames != value)
                {
                    EmbFile.UseFileNames = value;
                    nameColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    nameNullColumn.Visibility = !value ? Visibility.Visible : Visibility.Collapsed;
                    NotifyPropertyChanged(nameof(AllowTextureNames));
                }
            }
        }

        private int _thumbnailSizeBinding = 0;
        public int ThumbnailSizeBinding
        {
            get => _thumbnailSizeBinding;
            set
            {
                if(value != _thumbnailSizeBinding)
                {
                    _thumbnailSizeBinding = value;
                    NotifyPropertyChanged(nameof(ThumbnailSizeBinding));
                    NotifyPropertyChanged(nameof(ThumbnailSize));
                }
            }
        }
        public int ThumbnailSize
        {
            get
            {
                switch (_thumbnailSizeBinding)
                {
                    case 0:
                        return 32;
                    case 1:
                        return 96;
                    case 2:
                        return 164;
                    default:
                        return 0;
                }
            }
        }

        //Other
        public string TextureCount
        {
            get
            {
                if (EmbFile == null) return "--/--";
                if (IsForContainer) return $"{EmbFile.Entry.Count}/{EMB_File.MAX_EFFECT_TEXTURES}";
                return $"{EmbFile.Entry.Count}/--";
            }
        }

        //Selected Texture Values
        public string SelectedTextureName
        {
            get => _selectedTexture != null ? Path.GetFileNameWithoutExtension(_selectedTexture.Name) : string.Empty;
            set
            {
                if (value != _selectedTexture.Name)
                {
                    SetName(value);
                    NotifyPropertyChanged(nameof(SelectedTextureName));
                }
            }
        }
        public int SelectedTextureID
        {
            get => _selectedTexture?.ID ?? 0;
            set
            {
                if (_selectedTexture == null || _selectedTexture.ID == value) return;

                SetID(value);
                NotifyPropertyChanged(nameof(SelectedTextureID));
            }
        }

        //Properties for disabling and hiding elements that aren't needed in the current context. (e.g: dont show merge/used by options when editing a emm file for a emo or character as those are only useful for PBIND/TBIND assets)
        public bool IsForContainer => TextureEditorType == TextureEditorType.Pbind || TextureEditorType == TextureEditorType.Tbind;
        public Visibility ContainerVisiblility => IsForContainer ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InverseContainerVisiblility => IsForContainer ? Visibility.Collapsed : Visibility.Visible;
        public Visibility PbindVisiblility => TextureEditorType == TextureEditorType.Pbind ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NameVisibility => EmbFile.UseFileNames ? Visibility.Visible : Visibility.Collapsed;
        #endregion

        #region FilteredTextureList
        private ListCollectionView _viewTextures = null;
        public ListCollectionView ViewTextures
        {
            get
            {
                if (_viewTextures == null && EmbFile != null)
                {
                    _viewTextures = new ListCollectionView(EmbFile.Entry.Binding);
                    _viewTextures.Filter = new Predicate<object>(SearchFilterCheck);
                }
                return _viewTextures;
            }
            set
            {
                if (value != _viewTextures)
                {
                    _viewTextures = value;
                    NotifyPropertyChanged(nameof(ViewTextures));
                }
            }
        }

        private string _searchFilter = null;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                RefreshViewTextures();
                NotifyPropertyChanged(nameof(SearchFilter));
            }
        }

        private void RefreshViewTextures()
        {
            if (_viewTextures == null)
            {
                _viewTextures = new ListCollectionView(EmbFile.Entry.Binding);
                _viewTextures.Filter = new Predicate<object>(SearchFilterCheck);
            }

            _viewTextures.CommitEdit();

            //Apply ID sorting for non-container EMBs (a container EMB is one used as a EEPK texture container... PBIND, TBIND etc - which has it's IDs automatically calculated and hidden from the user)
            if (!IsForContainer)
            {
                _viewTextures.SortDescriptions.Clear();
                _viewTextures.SortDescriptions.Add(new SortDescription(nameof(EMB_TextureEntry.ID), ListSortDirection.Ascending));
            }

            NotifyPropertyChanged(nameof(ViewTextures));
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            var _texture = material as EMB_TextureEntry;
            string flattenedSearchParam = SearchFilter.ToLower();

            if (_texture?.Name != null)
            {
                if (_texture.Name.ToLower().Contains(flattenedSearchParam))
                    return true;
            }

            return false;
        }


        //Filtering
        [RelayCommand]
        private void ClearSearch()
        {
            SearchFilter = string.Empty;
        }
        #endregion

        public TextureEditor()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Loaded += TextureEditor_Loaded;
        }

        private void EmbFile_TexturesChanged(object sender, EventArgs e)
        {
            RefreshViewTextures();
        }

        private void TextureEditor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ContainerVisiblility));
            NotifyPropertyChanged(nameof(InverseContainerVisiblility));
            NotifyPropertyChanged(nameof(PbindVisiblility));
            RefreshViewTextures();
            UpdateProperties();

            nameColumn.Visibility = EmbFile.UseFileNames ? Visibility.Visible : Visibility.Collapsed;
            nameNullColumn.Visibility = EmbFile.UseFileNames ? Visibility.Collapsed : Visibility.Visible;
            idColumn.Visibility = InverseContainerVisiblility;
            EmbFile.TexturesChanged += EmbFile_TexturesChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(TextureCount));
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            EmbFile.TexturesChanged -= EmbFile_TexturesChanged;
        }

        public void SetName(string newName)
        {
            if(_selectedTexture != null)
            {
                string oldName = Path.GetFileNameWithoutExtension(_selectedTexture.Name);
                string newNameWithExtension = newName.Contains(".") ? newName : $"{newName}{Path.GetExtension(_selectedTexture.Name)}";

                if (newNameWithExtension != oldName)
                {
                    //string fullNewName = $"{newName}{Path.GetExtension(_selectedTexture.Name)}";
                    string fullOldName = _selectedTexture.Name;

                    if(EmbFile.Entry.Any(x => x.Name == newNameWithExtension && x != SelectedTexture))
                    {
                        MessagePrompt.Show($"Another texture is already named \"{newNameWithExtension}\".", "Name Already Used", MessagePromptButtons.OK, MessagePromptIcon.Error);
                        return;
                    }

                    _selectedTexture.Name = newNameWithExtension;

                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedTexture.Name), _selectedTexture, fullOldName, newNameWithExtension, "Texture Name"));
                }
            }
        }

        public void SetID(int newId)
        {
            if (IsForContainer) return; 

            if(SelectedTexture != null)
            {
                if(EmbFile.Entry.Any(x => x.ID == newId && x != SelectedTexture))
                {
                    MessagePrompt.Show($"Another texture already has ID \"{newId}\".", "ID Already Used", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    return;
                }

                UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                {
                    new UndoablePropertyGeneric(nameof(_selectedTexture.ID), _selectedTexture, SelectedTexture.ID, newId),
                    new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true)
                }, "Texture ID");
                SelectedTexture.ID = newId;

                EmbFile.TriggerTexturesChanged();
            }
        }


        #region TextureCommands
        [RelayCommand]
        private void AddTextureFromFile()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Validation
            if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
            {
                MessagePrompt.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.", EMB_File.MAX_EFFECT_TEXTURES), "Error", MessagePromptButtons.OK, MessagePromptIcon.Stop);
                return;
            }

            //Add file
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add texture(s)...";
            openFile.Filter = "DDS texture files | *.dds";
            openFile.Multiselect = true;
            openFile.ShowDialog();

            int renameCount = 0;
            int added = 0;

            foreach (var file in openFile.FileNames)
            {
                if (File.Exists(file) && !String.IsNullOrWhiteSpace(file))
                {
                    if (Path.GetExtension(file) != ".dds")
                    {
                        MessagePrompt.Show(String.Format("{0} is not a supported format.", System.IO.Path.GetExtension(file)), "Invalid Format", MessagePromptButtons.OK, MessagePromptIcon.Error);
                        continue;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    string fileName = Path.GetFileName(file);

                    EMB_TextureEntry newEntry = new EMB_TextureEntry();
                    newEntry.ID = EmbFile.GetNewID();
                    newEntry.Data = bytes;
                    newEntry.Name = EmbFile.GetUnusedName(fileName);
                    newEntry.LoadDds();


                    //Validat the emb again, so we dont go over the limit
                    if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                    {
                        MessagePrompt.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.\n\n{1} of the selected textures were added before the limit was reached.", EMB_File.MAX_EFFECT_TEXTURES, added), "Error", MessagePromptButtons.OK, MessagePromptIcon.Stop);
                        break;
                    }

                    //Add the emb
                    undos.Add(new UndoableListAdd<EMB_TextureEntry>(EmbFile.Entry, newEntry));
                    EmbFile.Entry.Add(newEntry);
                    added++;

                    if (newEntry.Name != fileName)
                    {
                        renameCount++;
                    }
                }
            }

            if (added > 0)
            {
                undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                EmbFile.TriggerTexturesChanged();

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, added > 1 ? "Add Textures" : "Add Texture"));
            }

            if (renameCount > 0)
            {
                MessagePrompt.Show(String.Format("{0} texture(s) were renamed during the add process because textures already existed in the EMB with the same name.", renameCount), "Add Texture", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void ExtractTexture()
        {
            if (SelectedTextures.Count == 0) return;

            string extractionPath;

            if (SelectedTextures.Count > 1)
            {
                //Select dir to dump textures
                var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

                if (_browser.ShowDialog() == false)
                    return;

                extractionPath = _browser.SelectedPath;
            }
            else
            {
                var _browser = new SaveFileDialog();
                _browser.Filter = "DDS file |*.dds;";
                _browser.FileName += $"{SelectedTextures[0].Name}";

                if (_browser.ShowDialog() == false)
                    return;

                extractionPath = _browser.FileName;
            }

            foreach (var texture in SelectedTextures)
            {
                string path = (SelectedTextures.Count == 1) ? extractionPath : $"{extractionPath}/{texture.Name}";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                texture.SaveDds();
                File.WriteAllBytes(path, texture.Data);
            }
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void DeleteTexture()
        {
            //bool textureInUse = false;
            var selectedTextures = SelectedTextures.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedTextures.Length > 0)
            {
                if (IsForContainer && selectedTextures.Any(x => AssetContainer.IsTextureUsed(x)))
                {
                    string type = TextureEditorType == TextureEditorType.Pbind ? "EMP" : "ETR";
                    if (MessagePrompt.Show($"One or more of the selected textures is currently assigned to a {type} and its reference will be removed if the texture is deleted.\n\nDo you wish to continue?",
                            "Delete", MessagePromptButtons.YesNo, MessagePromptIcon.Warning) != MessagePromptResult.Yes)
                        return;
                }

                foreach (var texture in selectedTextures)
                {
                    if (IsForContainer)
                    {
                        AssetContainer.DeleteTexture(texture, undos);
                    }
                    else
                    {
                        //Non Container Mode. Always allow deleting.
                        undos.Add(new UndoableListRemove<EMB_TextureEntry>(EmbFile.Entry, texture));
                        EmbFile.Entry.Remove(texture);
                    }
                }

                undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                EmbFile.TriggerTexturesChanged();
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Delete"));
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void DuplicateTexture()
        {
            var selectedTextures = SelectedTextures.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>(selectedTextures.Length);
            List<EMB_TextureEntry> newTextures = new List<EMB_TextureEntry>(selectedTextures.Length);

            int newEntryId = EmbFile.GetNewID();

            foreach (var texture in selectedTextures)
            {
                if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                {
                    MessagePrompt.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                    break;
                }

                var newTexture = texture.Clone() as EMB_TextureEntry;
                newTextures.Add(newTexture);
                newTexture.ID = EmbFile.GetNewID();
                newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);

                undos.Add(new UndoableListAdd<EMB_TextureEntry>(EmbFile.Entry, newTexture));
                EmbFile.AddTexture(newTexture);
            }

            if (selectedTextures.Length > 0)
            {
                undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                EmbFile.TriggerTexturesChanged();
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Duplicate"));

                SetSelectedTextures(newTextures);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void CopyTexture()
        {
            if (SelectedTextures.Count > 0)
            {
                CustomClipboard.SetData(ClipboardDataTypes.EmbTexture, SelectedTextures.ToArray());
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteTexture))]
        private void PasteTexture()
        {
            if (!CustomClipboard.TryGetData(ClipboardDataTypes.EmbTexture, out EMB_TextureEntry[] copiedTextures))
                return;

            if (copiedTextures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>(copiedTextures.Length);
                List<EMB_TextureEntry> newTextures = new List<EMB_TextureEntry>(copiedTextures.Length);

                foreach (var texture in copiedTextures)
                {
                    if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                    {
                        MessagePrompt.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                        break;
                    }

                    var newTexture = texture.Copy();
                    newTextures.Add(newTexture);
                    newTexture.ID = EmbFile.GetNewID();
                    newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);
                    EmbFile.AddTexture(newTexture);

                    undos.Add(new UndoableListAdd<EMB_TextureEntry>(EmbFile.Entry, newTexture));
                }

                if (copiedTextures.Length > 0)
                {
                    undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                    EmbFile.TriggerTexturesChanged();
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Paste"));

                    SetSelectedTextures(newTextures);
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(CanMergeTextures))]
        private void MergeTextures()
        {
            if (!IsForContainer) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            var texture = SelectedTexture;
            List<EMB_TextureEntry> selectedTextures = SelectedTextures.ToList();
            selectedTextures.Remove(texture);

            if (texture != null && selectedTextures.Count > 0)
            {
                int count = selectedTextures.Count + 1;

                if (MessagePrompt.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.Name), string.Format("Merge ({0} textures)", count), MessagePromptButtons.OKCancel, MessagePromptIcon.Question) == MessagePromptResult.OK)
                {
                    foreach (var textureToRemove in selectedTextures)
                    {
                        AssetContainer.RefactorTextureRef(textureToRemove, texture, undos);
                        undos.Add(new UndoableListRemove<EMB_TextureEntry>(AssetContainer.File3_Ref.Entry, textureToRemove));
                        AssetContainer.File3_Ref.Entry.Remove(textureToRemove);
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Merge"));
                }
            }
            else
            {
                MessagePrompt.Show("Cannot merge with less than 2 textures selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessagePromptButtons.OK, MessagePromptIcon.Warning);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void UsedBy()
        {
            if (!IsForContainer) return;

            if (SelectedTexture != null)
            {
                List<string> assets = AssetContainer.TextureUsedBy(SelectedTexture);
                assets.Sort();
                StringBuilder str = new StringBuilder();

                foreach (var asset in assets)
                {
                    str.Append(string.Format("{0}\r", asset));
                }

                MessagePrompt prompt = new MessagePrompt($"{SelectedTexture.Name}: Used By", $"The following {assets.Count} assets use this texture:", str.ToString());
                prompt.Show();
            }
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void HueShift()
        {
            if (SelectedTexture != null)
            {
                if (SelectedTexture.Texture == null)
                {
                    MessagePrompt.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    var editForm = new RecolorTexture(SelectedTexture, SelectedTextures.ToList(), false);
                    editForm.ShowDialog();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void HueSet()
        {
            if (SelectedTexture != null)
            {
                if (SelectedTexture.Texture == null)
                {
                    MessagePrompt.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    var editForm = new RecolorTexture(SelectedTexture, SelectedTextures.ToList(), true);
                    editForm.ShowDialog();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsTextureSelected))]
        private void ReplaceTexture()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Replace texture...";
            openFile.Filter = "DDS texture files | *.dds";
            openFile.Multiselect = false;
            openFile.ShowDialog();

            if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                byte[] bytes = File.ReadAllBytes(openFile.FileName);
                string name = Path.GetFileName(openFile.FileName);

                if (name != SelectedTexture.Name && (TextureEditorType == TextureEditorType.Pbind || TextureEditorType == TextureEditorType.Tbind))
                {
                    string newName = AssetContainer.File3_Ref.GetUnusedName(name);
                    undos.Add(new UndoableProperty<EMB_TextureEntry>(nameof(SelectedTexture.Name), SelectedTexture, SelectedTexture.Name, newName));
                    SelectedTexture.Name = newName;
                }

                var newData = bytes;
                undos.Add(new UndoableProperty<EMB_TextureEntry>(nameof(SelectedTexture.Data), SelectedTexture, SelectedTexture.Data, newData));
                SelectedTexture.Data = newData;

                undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                EmbFile.TriggerTexturesChanged();
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace Texture"));
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand(CanExecute = nameof(CanCreateSuperTexture))]
        private void CreateSuperTexture()
        {
            if (TextureEditorType != TextureEditorType.Pbind) return;

            List<EMB_TextureEntry> selectedTextures = SelectedTextures.ToList();

            if (selectedTextures.Count < 2)
            {
                MessagePrompt.Show("Cannot proceed. A SuperTexture requires atleast two selected textures.", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            foreach (var entry in selectedTextures)
            {

                if (AssetContainer.GetAllTextureDefinitions(entry).Any(x => x.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed || x.RepetitionU == EMP_TextureSamplerDef.TextureRepitition.Mirror || x.RepetitionV == EMP_TextureSamplerDef.TextureRepitition.Mirror))
                {
                    MessagePrompt.Show("One of the selected textures is used by an EMP with an unallowed type. These can't be merged.", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }
            }

            //Remove all repeating textures
            selectedTextures.RemoveAll(x => EMP_TextureSamplerDef.IsRepeatingTexture(x, AssetContainer));

            if (selectedTextures.Count < 2)
            {
                MessagePrompt.Show("Some of the selected textures cannot be merged and were removed from the process, but now only 1 texture remains. Aborting.", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }


            List<System.Windows.Media.Imaging.WriteableBitmap> bitmaps = EMB_TextureEntry.GetBitmaps(selectedTextures);
            double maxDimension = EMB_TextureEntry.HighestDimension(bitmaps);
            int textureSize = (int)EMB_TextureEntry.SelectTextureSize(maxDimension, bitmaps.Count);

            if (textureSize == -1)
            {
                MessagePrompt.Show("Cannot proceed. The resulting texture would be too large (greater than 2048 x 2048).", "Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Do the merge
            AssetContainer.MergeIntoSuperTexture_PBIND(selectedTextures, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Combine SuperTexture");

            NotifyPropertyChanged(nameof(TextureCount));
        }


        private bool IsTextureSelected()
        {
            return SelectedTexture != null;
        }

        private bool CanMergeTextures()
        {
            return textureDataGrid.SelectedItems != null ? textureDataGrid.SelectedItems.Count >= 2 : false;
        }

        private bool CanPasteTexture()
        {
            return CustomClipboard.ContainsData(ClipboardDataTypes.EmbTexture);
        }

        private bool CanCreateSuperTexture()
        {
            return CanMergeTextures() && TextureEditorType == TextureEditorType.Pbind;
        }
        #endregion

        #region ToolCommands
        [RelayCommand]
        private void MergeDuplicates()
        {
            var result = MessagePrompt.Show("All instances of duplicated textures will be merged into a single texture. A duplicated texture means any that share the exact same data, but have a different name. \n\nAll references to the duplicates in any assets will also be updated to reflect these changes.\n\nDo you want to continue?", "Merge Duplicates", MessagePromptButtons.YesNo, MessagePromptIcon.Question);
            
            if(result == MessagePromptResult.Yes)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int merged = AssetContainer.MergeDuplicateTextures(undos);

                if (merged > 0)
                {
                    undos.Add(new UndoActionDelegate(EmbFile, nameof(EmbFile.TriggerTexturesChanged), true));
                    EmbFile.TriggerTexturesChanged();
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Texture)"));

                    MessagePrompt.Show($"{merged} texture instances were merged.", "Merge Duplicates", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("No instances of duplicated texture were found.", "Merge Duplicates", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        [RelayCommand]
        private void RemoveUnusedTextures()
        {
            if (!IsForContainer) return;
            var result = MessagePrompt.Show("Any texture that is not currently used by a asset will be deleted.\n\nDo you want to continue?", "Remove Unused", MessagePromptButtons.YesNo, MessagePromptIcon.Question);

            if (result == MessagePromptResult.Yes)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int unusued = AssetContainer.RemoveUnusedTextures(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Texture)"));

                if (unusued > 0)
                {
                    MessagePrompt.Show($"{unusued} texture instances were removed.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("No unused textures were found.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        #endregion

        private void SetSelectedTexture(EMB_TextureEntry texture)
        {
            SelectedTextures.Clear();
            SelectedTexture = texture;

            if (texture != null)
                textureDataGrid.ScrollIntoView(texture);
        }

        private void SetSelectedTextures(IList<EMB_TextureEntry> textures)
        {
            if (textures.Count == 0) return;

            SelectedTexture = textures[0];
            SelectedTextures.Clear();

            foreach (var material in textures)
            {
                SelectedTextures.Add(material);
            }

            textureDataGrid.ScrollIntoView(SelectedTexture);
        }
    
    }

    public enum TextureEditorType
    {
        Character,
        Pbind,
        Tbind,
        Emo
    }
}
