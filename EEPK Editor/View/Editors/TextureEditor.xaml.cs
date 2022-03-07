using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using System.IO;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using EEPK_Organiser.Misc;
using EEPK_Organiser.Forms;
using MahApps.Metro.Controls;
using EEPK_Organiser.Forms.Recolor;
using Xv2CoreLib.EMP;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for TextureEditor.xaml
    /// </summary>
    public partial class TextureEditor : UserControl, INotifyPropertyChanged, IDisposable
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty EmbFileProperty = DependencyProperty.Register(nameof(EmbFile), typeof(EMB_File), typeof(TextureEditor), new PropertyMetadata(null));

        public EMB_File EmbFile
        {
            get { return (EMB_File)GetValue(EmbFileProperty); }
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
        private EmbEntry _selectedTexture = null;
        public EmbEntry SelectedTexture
        {
            get => _selectedTexture;
            set
            {
                _selectedTexture = value;
                NotifyPropertyChanged(nameof(SelectedTexture));
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
        
        //Properties for disabling and hiding elements that aren't needed in the current context. (e.g: dont show merge/used by options when editing a emm file for a emo or character as those are only useful for PBIND/TBIND assets)
        public bool IsForContainer => TextureEditorType == TextureEditorType.Pbind || TextureEditorType == TextureEditorType.Tbind;
        public Visibility ContainerVisiblility => IsForContainer ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PbindVisiblility => TextureEditorType == TextureEditorType.Pbind ? Visibility.Visible : Visibility.Collapsed;
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
                _viewTextures = new ListCollectionView(EmbFile.Entry.Binding);

            _viewTextures.Filter = new Predicate<object>(SearchFilterCheck);
            NotifyPropertyChanged(nameof(ViewTextures));
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            var _texture = material as EmbEntry;
            string flattenedSearchParam = SearchFilter.ToLower();

            if (_texture?.Name != null)
            {
                if (_texture.Name.ToLower().Contains(flattenedSearchParam))
                    return true;
            }

            return false;
        }


        //Filtering
        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
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

        private void TextureEditor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ContainerVisiblility));
            NotifyPropertyChanged(nameof(PbindVisiblility));
            RefreshViewTextures();
            UpdateProperties();
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
        }

        public async void SetName(string newName)
        {
            if(_selectedTexture != null)
            {
                string oldName = Path.GetFileNameWithoutExtension(_selectedTexture.Name);

                if(newName != oldName)
                {
                    string fullNewName = $"{newName}{Path.GetExtension(_selectedTexture.Name)}";
                    string fullOldName = _selectedTexture.Name;

                    if(EmbFile.Entry.Any(x => x.Name == fullNewName && x != SelectedTexture))
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", $"Another texture is already named\"{fullNewName}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                        return;
                    }

                    _selectedTexture.Name = fullNewName;

                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedTexture.Name), _selectedTexture, fullOldName, fullNewName, "Texture Name"));
                }
            }
        }

        /*
        public async void SetID(int newId)
        {
            //ID feature removed. Will keep this function incase I want to revive it.

            string strId = newId.ToString();

            if(SelectedTexture != null)
            {
                if(EmbFile.Entry.Any(x => x.Index == strId && x != SelectedTexture))
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", $"Another texture already has ID \"{newId}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                string oldId = SelectedTexture.Index;
                SelectedTexture.Index = strId;

                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedTexture.Index), _selectedTexture, oldId, strId, "Texture ID"));
            }
        }
        */

        #region TextureCommands
        public RelayCommand AddTextureFromFileCommand => new RelayCommand(AddTextureFromFile);
        private void AddTextureFromFile()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Validation
            if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
            {
                MessageBox.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.", EMB_File.MAX_EFFECT_TEXTURES), "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
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
                        MessageBox.Show(String.Format("{0} is not a supported format.", System.IO.Path.GetExtension(file)), "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    string fileName = Path.GetFileName(file);

                    EmbEntry newEntry = new EmbEntry();
                    newEntry.Data = bytes;
                    newEntry.Name = EmbFile.GetUnusedName(fileName);
                    newEntry.LoadDds();


                    //Validat the emb again, so we dont go over the limit
                    if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                    {
                        MessageBox.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.\n\n{1} of the selected textures were added before the limit was reached.", EMB_File.MAX_EFFECT_TEXTURES, added), "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                        break;
                    }

                    //Add the emb
                    undos.Add(new UndoableListAdd<EmbEntry>(EmbFile.Entry, newEntry));
                    EmbFile.Entry.Add(newEntry);
                    added++;

                    if (newEntry.Name != fileName)
                    {
                        renameCount++;
                    }
                }
            }

            if (added > 0)
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, added > 1 ? "Add Textures" : "Add Texture"));

            if (renameCount > 0)
            {
                MessageBox.Show(String.Format("{0} texture(s) were renamed during the add process because textures already existed in the EMB with the same name.", renameCount), "Add Texture", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand ExtractTextureCommand => new RelayCommand(ExtractTexture, IsTextureSelected);
        private void ExtractTexture()
        {
            List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();
            if (selectedTextures.Count == 0) return;

            string extractionPath;

            if (selectedTextures.Count > 1)
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
                _browser.FileName += $"{selectedTextures[0].Name}";

                if (_browser.ShowDialog() == false)
                    return;

                extractionPath = _browser.FileName;
            }

            foreach (var texture in selectedTextures)
            {
                string path = (selectedTextures.Count == 1) ? extractionPath : $"{extractionPath}/{texture.Name}";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                texture.SaveDds();
                File.WriteAllBytes(path, texture.Data);
            }
        }

        public RelayCommand DeleteTextureCommand => new RelayCommand(DeleteTexture, IsTextureSelected);
        private void DeleteTexture()
        {
            bool textureInUse = false;
            List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedTextures.Count > 0)
            {
                foreach (var texture in selectedTextures)
                {
                    if (IsForContainer)
                    {
                        //Container mode. Only allow deleting if texture is unused by EMP/ETRs.
                        if (AssetContainer.IsTextureUsed(texture))
                        {
                            textureInUse = true;
                        }
                        else
                        {
                            AssetContainer.DeleteTexture(texture, undos);
                        }
                    }
                    else
                    {
                        //Non Container Mode. Always allow deleting.
                        undos.Add(new UndoableListRemove<EmbEntry>(EmbFile.Entry, texture));
                        EmbFile.Entry.Remove(texture);
                    }
                }
                if (textureInUse && selectedTextures.Count == 1)
                {
                    MessageBox.Show("The selected texture cannot be deleted because it is currently being used.", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (textureInUse && selectedTextures.Count > 1)
                {
                    MessageBox.Show("One or more of the selected textures cannot be deleted because they are currently being used.", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Delete"));
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }
        
        public RelayCommand DuplicateTextureCommand => new RelayCommand(DuplicateTexture, IsTextureSelected);
        private void DuplicateTexture()
        {
            List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var texture in selectedTextures)
            {
                if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                {
                    MessageBox.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }

                var newTexture = texture.Clone();
                newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);
                undos.Add(new UndoableListAdd<EmbEntry>(EmbFile.Entry, newTexture));
                EmbFile.Add(newTexture);
            }

            if (selectedTextures.Count > 0)
            {
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Duplicate"));

                textureDataGrid.SelectedItem = EmbFile.Entry[EmbFile.Entry.Count - 1];
                textureDataGrid.ScrollIntoView(EmbFile.Entry[EmbFile.Entry.Count - 1]);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand CopyTextureCommand => new RelayCommand(CopyTexture, IsTextureSelected);
        private void CopyTexture()
        {
            List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();

            if (selectedTextures.Count > 0)
            {
                Clipboard.SetData(ClipboardDataTypes.EmbTexture, selectedTextures);
            }
        }

        public RelayCommand PasteTextureCommand => new RelayCommand(PasteTexture, CanPasteTexture);
        private void PasteTexture()
        {
            List<EmbEntry> copiedTextures = (List<EmbEntry>)Clipboard.GetData(ClipboardDataTypes.EmbTexture);

            if (copiedTextures != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var texture in copiedTextures)
                {
                    if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                    {
                        MessageBox.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    var newTexture = texture.Copy();
                    newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);
                    EmbFile.Add(newTexture);

                    undos.Add(new UndoableListAdd<EmbEntry>(EmbFile.Entry, newTexture));
                }

                if (copiedTextures.Count > 0)
                {
                    SelectedTexture = EmbFile.Entry[EmbFile.Entry.Count - 1];
                    textureDataGrid.ScrollIntoView(EmbFile.Entry[EmbFile.Entry.Count - 1]);

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Paste"));
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand MergeTexturesCommand => new RelayCommand(MergeTextures, CanMergeTextures);
        private void MergeTextures()
        {
            if (!IsForContainer) return;

            try
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                var texture = SelectedTexture;
                List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();
                selectedTextures.Remove(texture);

                if (texture != null && selectedTextures.Count > 0)
                {
                    int count = selectedTextures.Count + 1;

                    if (MessageBox.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.Name), string.Format("Merge ({0} textures)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        foreach (var textureToRemove in selectedTextures)
                        {
                            AssetContainer.RefactorTextureRef(textureToRemove, texture, undos);
                            undos.Add(new UndoableListRemove<EmbEntry>(AssetContainer.File3_Ref.Entry, textureToRemove));
                            AssetContainer.File3_Ref.Entry.Remove(textureToRemove);
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Merge"));
                    }
                }
                else
                {
                    MessageBox.Show("Cannot merge with less than 2 textures selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand UsedByCommand => new RelayCommand(UsedBy, IsTextureSelected);
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
                    str.Append(String.Format("{0}\r", asset));
                }

                LogForm logForm = new LogForm(String.Format("The following {0} assets use this texture:", TextureEditorType.ToString().ToUpper()), str.ToString(), String.Format("{0}: Used By", SelectedTexture.Name), null, true);
                logForm.Show();
            }
        }

        public RelayCommand HueShiftCommand => new RelayCommand(HueShift, IsTextureSelected);
        private void HueShift()
        {
            if (SelectedTexture != null)
            {
                if (SelectedTexture.Texture == null)
                {
                    MessageBox.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var editForm = new TextureEditHueChange(SelectedTexture, Application.Current.Windows.OfType<MetroWindow>().SingleOrDefault(x => x.IsFocused));
                    editForm.ShowDialog();
                }
            }
        }

        public RelayCommand HueSetCommand => new RelayCommand(HueSet, IsTextureSelected);
        private void HueSet()
        {
            if (SelectedTexture != null)
            {
                if (SelectedTexture.Texture == null)
                {
                    MessageBox.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var editForm = new RecolorTexture_HueSet(SelectedTexture, Application.Current.Windows.OfType<MetroWindow>().SingleOrDefault(x => x.IsFocused));
                    editForm.ShowDialog();
                }
            }
        }

        public RelayCommand ReplaceTextureCommand => new RelayCommand(ReplaceTexture, IsTextureSelected);
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

                if (name != SelectedTexture.Name)
                {
                    string newName = AssetContainer.File3_Ref.GetUnusedName(name);
                    undos.Add(new UndoableProperty<EmbEntry>(nameof(SelectedTexture.Name), SelectedTexture, SelectedTexture.Name, newName));
                    SelectedTexture.Name = newName;
                }

                var newData = bytes;
                undos.Add(new UndoableProperty<EmbEntry>(nameof(SelectedTexture.Data), SelectedTexture, SelectedTexture.Data, newData));
                SelectedTexture.Data = newData;

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace Texture"));
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand CreateSuperTextureCommand => new RelayCommand(CreateSuperTexture, CanCreateSuperTexture);
        private void CreateSuperTexture()
        {
            if (TextureEditorType != TextureEditorType.Pbind) return;

            List<EmbEntry> selectedTextures = textureDataGrid.SelectedItems.Cast<EmbEntry>().ToList();

            if (selectedTextures.Count < 2)
            {
                MessageBox.Show("Cannot proceed. A SuperTexture requires atleast two selected textures.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var entry in selectedTextures)
            {

                if (AssetContainer.GetAllTextureDefinitions(entry).Any(x => x.TextureType == Xv2CoreLib.EMP.EMP_TextureDefinition.TextureAnimationType.Speed || x.I_06_byte == Xv2CoreLib.EMP.EMP_TextureDefinition.TextureRepitition.Mirror || x.I_07_byte == Xv2CoreLib.EMP.EMP_TextureDefinition.TextureRepitition.Mirror))
                {
                    MessageBox.Show("One of the selected textures is used by an EMP with an unallowed type. These can't be merged.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            //Remove all repeating textures
            selectedTextures.RemoveAll(x => EMP_TextureDefinition.IsRepeatingTexture(x, AssetContainer));

            if (selectedTextures.Count < 2)
            {
                MessageBox.Show("Some of the selected textures cannot be merged and were removed from the process, but now only 1 texture remains. Aborting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            var bitmaps = EmbEntry.GetBitmaps(selectedTextures);
            double maxDimension = EmbEntry.HighestDimension(bitmaps);
            int textureSize = (int)EmbEntry.SelectTextureSize(maxDimension, bitmaps.Count);

            if (textureSize == -1)
            {
                MessageBox.Show("Cannot proceed. The resulting texture would be too large (greater than 2048 x 2048).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            return Clipboard.ContainsData(ClipboardDataTypes.EmbTexture);
        }

        private bool CanCreateSuperTexture()
        {
            return CanMergeTextures() && TextureEditorType == TextureEditorType.Pbind;
        }
        #endregion

        #region ToolCommands
        public RelayCommand MergeDuplicatesCommand => new RelayCommand(MergeDuplicates);
        private async void MergeDuplicates()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "All instances of duplicated textures will be merged into a single texture. A duplicated texture means any that share the exact same data, but have a different name. \n\nAll references to the duplicates in any assets will also be updated to reflect these changes.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);
            
            if(result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int merged = AssetContainer.MergeDuplicateTextures(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Texture)"));

                if (merged > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", string.Format("{0} texture instances were merged.", merged), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "No instances of duplicated texture were found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        public RelayCommand RemoveUnusedTexturesCommand => new RelayCommand(RemoveUnusedTextures);
        private async void RemoveUnusedTextures()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused", "Any texture that is not currently used by a asset will be deleted.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int unusued = AssetContainer.RemoveUnusedTextures(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Texture)"));

                if (unusued > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", string.Format("{0} texture instances were removed.", unusued), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", "No unused textures were found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

            NotifyPropertyChanged(nameof(TextureCount));
        }

        #endregion
    }

    public enum TextureEditorType
    {
        Character,
        Pbind,
        Tbind,
        Emo
    }
}
