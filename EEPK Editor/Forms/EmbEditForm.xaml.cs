using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EEPK;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using EEPK_Organiser.Misc;
using EEPK_Organiser.View;
using MahApps.Metro.Controls;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms.Recolor;
using Xv2CoreLib.Resource.App;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EmbEditForm.xaml
    /// </summary>
    public partial class EmbEditForm : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AssetType assetType { get; private set; }
        public EMB_File EmbFile { get; set; }
        private AssetContainerTool container { get; set; }
        public bool IsForContainer { get; set; }
        private EepkEditor parent = null;

        //View
        public string TextureCount
        {
            get
            {
                return String.Format("{0}/{1}", EmbFile.Entry.Count, EMB_File.MAX_EFFECT_TEXTURES);
            }
        }

        public string AssetTypeWildcard
        {
            get
            {
                switch (assetType)
                {
                    case AssetType.PBIND:
                        return "EMP";
                    case AssetType.TBIND:
                        return "ETR";
                    default:
                        return null;
                }
            }
        }

        public EmbEditForm(EMB_File _embFile, AssetContainerTool _container, AssetType _assetType, EepkEditor _parent, bool isForContainer = true, string windowTitle = null)
        {
            IsForContainer = isForContainer;
            EmbFile = _embFile;
            container = _container;
            assetType = _assetType;
            InitializeComponent();
            DataContext = this;
            //Owner = parent;
            parent = _parent;

            if(windowTitle != null)
            {
                Title += String.Format(" ({0})", windowTitle);
            }

            if(assetType != AssetType.PBIND && assetType != AssetType.TBIND && isForContainer)
            {
                MessageBox.Show("EmbEditForm cannot be used on AssetType: " + assetType);
                Close();
            }

            //EmbFile.LoadDdsImages(false);
        }


        //EMB Options
        private void EmbOptions_AddTexture_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Validation
            if(EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
            {
                MessageBox.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.", EMB_File.MAX_EFFECT_TEXTURES), "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            
            //Add file
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Add texture(s)...";
            openFile.Filter = "DDS texture files | *.dds";
            openFile.Multiselect = true;
            openFile.ShowDialog(this);

            int renameCount = 0;
            int added = 0;

            foreach(var file in openFile.FileNames)
            {
                if (File.Exists(file) && !String.IsNullOrWhiteSpace(file))
                {
                    if (System.IO.Path.GetExtension(file) != ".dds")
                    {
                        MessageBox.Show(String.Format("{0} is not a supported format.", System.IO.Path.GetExtension(file)), "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    byte[] bytes = File.ReadAllBytes(file);
                    string fileName = System.IO.Path.GetFileName(file);

                    EmbEntry newEntry = new EmbEntry();
                    newEntry.Data = bytes.ToList();
                    newEntry.Name = EmbFile.GetUnusedName(fileName);
                    newEntry.LoadDds();


                    //Validat the emb again, so we dont go over the limit
                    if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES && IsForContainer)
                    {
                        MessageBox.Show(String.Format("The maximum allowed amount of textures has been reached. Cannot add anymore.\n\n{1} of the selected textures were added before the limit was reached.", EMB_File.MAX_EFFECT_TEXTURES, added), "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
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

            undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
            RefreshTextureCount();

            if(added > 0) 
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, added > 1 ? "Add Textures" : "Add Texture"));

            if (renameCount > 0)
            {
                MessageBox.Show(String.Format("{0} texture(s) were renamed during the add process because textures already existed in the EMB with the same name.", renameCount), "Add Texture", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void EmbOptions_RemoveUnusuedTextures_Click(object sender, RoutedEventArgs e)
        {
            if (!IsForContainer) return;

            if (MessageBox.Show(String.Format("Any texture that is not currently used by a {0} will be deleted. This cannot be undone.\n\nDo you want to continue?", AssetTypeWildcard), "Delete Unused", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            int removeCount = Emb_Options_RemoveUnusedTextures();

            if (removeCount > 0)
            {
                MessageBox.Show(String.Format("{0} unused textures were removed.", removeCount), "Delete Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No unused textures were found.", "Delete Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshTextureCount();
        }

        private void EmbOptions_RemoveDuplicates_Click(object sender, RoutedEventArgs e)
        {
            if (!IsForContainer) return;

            if (MessageBox.Show(String.Format("All instances of duplicated textures will be merged into a single texture. A duplicated texture means any that are 100% identical, those that are only similar wont be affected.\n\nAll references to the duplicates in any {0} will also be updated to reflect these changes.\n\nDo you want to continue?", AssetTypeWildcard), "Merge Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            int duplicateCount = Emb_Options_MergeDuplicateTextures();

            if (duplicateCount > 0)
            {
                MessageBox.Show(String.Format("{0} texture instances were merged.", duplicateCount), "Merge Duplicates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No instances of duplicated textures were found.", "Merge Duplicates", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshTextureCount();
        }

        //EMB Context Menu
        private void EmbContextMenu_Rename_Click(object sender, RoutedEventArgs e)
        {
            var texture = listBox_Textures.SelectedItem as EmbEntry;

            if(texture != null)
            {
                RenameFile_PopUp(texture);
            }
        }

        private void EmbContextMenu_Replace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var texture = listBox_Textures.SelectedItem as EmbEntry;

                if (texture != null)
                {
                    OpenFileDialog openFile = new OpenFileDialog();
                    openFile.Title = "Replace texture...";
                    openFile.Filter = "DDS texture files | *.dds";
                    openFile.Multiselect = false;
                    openFile.ShowDialog(this);

                    if (File.Exists(openFile.FileName) && !String.IsNullOrWhiteSpace(openFile.FileName))
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        byte[] bytes = File.ReadAllBytes(openFile.FileName);
                        string name = System.IO.Path.GetFileName(openFile.FileName);

                        if (name != texture.Name)
                        {
                            string newName = container.File3_Ref.GetUnusedName(name);
                            undos.Add(new UndoableProperty<EmbEntry>(nameof(texture.Name), texture, texture.Name, newName));
                            texture.Name = newName;
                        }

                        var newData = bytes.ToList();
                        undos.Add(new UndoableProperty<EmbEntry>(nameof(texture.Data), texture, texture.Data, newData));
                        texture.Data = newData;

                        if (texture.Name != name)
                        {
                            MessageBox.Show(String.Format("The new texture was automatically renamed because \"{0}\" was already used.", name), "Rename", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Replace Texture"));
                    }

                }
            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void EmbContextMenu_Merge_Click(object sender, RoutedEventArgs e)
        {
            if (!IsForContainer)
            {
                MessageBox.Show("Merge is not available for non-container EMBs.", "Merge", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                var texture = listBox_Textures.SelectedItem as EmbEntry;
                List<EmbEntry> selectedTextures = listBox_Textures.SelectedItems.Cast<EmbEntry>().ToList();
                selectedTextures.Remove(texture);


                if (texture != null && selectedTextures.Count > 0)
                {
                    int count = selectedTextures.Count + 1;

                    if (MessageBox.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.Name), string.Format("Merge ({0} textures)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        foreach (var textureToRemove in selectedTextures)
                        {
                            container.RefactorTextureRef(textureToRemove, texture, undos);
                            undos.Add(new UndoableListRemove<EmbEntry>(container.File3_Ref.Entry, textureToRemove));
                            container.File3_Ref.Entry.Remove(textureToRemove);
                        }

                        undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
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

            RefreshTextureCount();
        }
        
        private void EmbContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool textureInUse = false;
                List<EmbEntry> selectedTextures = listBox_Textures.SelectedItems.Cast<EmbEntry>().ToList();
                List<IUndoRedo> undos = new List<IUndoRedo>();

                if (selectedTextures.Count > 0)
                {
                    foreach (var texture in selectedTextures)
                    {
                        if (IsForContainer)
                        {
                            //Container mode. Only allow deleting if texture is unused by EMP/ETRs.
                            if (container.IsTextureUsed(texture))
                            {
                                textureInUse = true;
                            }
                            else
                            {
                                container.DeleteTexture(texture, undos);
                            }
                        }
                        else
                        {
                            //Non Container Mode. Always allow deleting.
                            undos.Add(new UndoableListRemove<EmbEntry>(EmbFile.Entry, texture));
                            EmbFile.Entry.Remove(texture);
                        }
                    }

                    undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
                    RefreshTextureCount();

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

            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EmbContextMenu_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsForContainer)
                {
                    MessageBox.Show("Used By not available for non-container EMBs.", "Used By", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var texture = listBox_Textures.SelectedItem as EmbEntry;

                if (texture != null)
                {
                    List<string> assets = container.TextureUsedBy(texture);
                    assets.Sort();
                    StringBuilder str = new StringBuilder();

                    foreach (var asset in assets)
                    {
                        str.Append(String.Format("{0}\r", asset));
                    }

                    LogForm logForm = new LogForm(String.Format("The following {0} assets use this texture:", AssetTypeWildcard), str.ToString(), String.Format("{0}: Used By", texture.Name), this, true);
                    logForm.Show();
                }
            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void EmbContextMenu_HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTexture = listBox_Textures.SelectedItem as EmbEntry;

                if (selectedTexture != null)
                {
                    if (selectedTexture.DdsImage == null)
                    {
                        MessageBox.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var editForm = new TextureEditHueChange(selectedTexture, this);
                        editForm.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EmbContextMenu_HueSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTexture = listBox_Textures.SelectedItem as EmbEntry;

                if (selectedTexture != null)
                {
                    if (selectedTexture.DdsImage == null)
                    {
                        MessageBox.Show("Cannot edit because no texture was loaded.\n\nEither the texture loading failed or texture loading has been disabled in the settings.", "No Texture Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var editForm = new RecolorTexture_HueSet(selectedTexture, this);
                        editForm.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EmbContextMenu_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<EmbEntry> selectedTextures = listBox_Textures.SelectedItems.Cast<EmbEntry>().ToList();
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var texture in selectedTextures)
                {
                    if(EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
                    {
                        MessageBox.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newTexture = texture.Clone();
                    newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);
                    undos.Add(new UndoableListAdd<EmbEntry>(EmbFile.Entry, newTexture));
                    EmbFile.Add(newTexture);
                }

                if(selectedTextures.Count > 0)
                {
                    RefreshTextureCount();
                    undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Duplicate"));

                    listBox_Textures.SelectedItem = EmbFile.Entry[EmbFile.Entry.Count - 1];
                    listBox_Textures.ScrollIntoView(EmbFile.Entry[EmbFile.Entry.Count - 1]);
                }

            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EmbContextMenu_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<EmbEntry> selectedTextures = listBox_Textures.SelectedItems.Cast<EmbEntry>().ToList();

                if(selectedTextures.Count > 0)
                {
                    Clipboard.SetData(ClipboardDataTypes.EmbTexture, selectedTextures);
                }

            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EmbContextMenu_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<EmbEntry> copiedTextures = (List<EmbEntry>)Clipboard.GetData(ClipboardDataTypes.EmbTexture);

                if (copiedTextures != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    foreach (var texture in copiedTextures)
                    {
                        if (EmbFile.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
                        {
                            MessageBox.Show("The maximum amount of textures has been reached. Cannot add anymore.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var newTexture = texture.Clone();
                        newTexture.Name = EmbFile.GetUnusedName(newTexture.Name);
                        EmbFile.Add(newTexture);
                        RefreshTextureCount();

                        undos.Add(new UndoableListAdd<EmbEntry>(EmbFile.Entry, newTexture));
                    }

                    if (copiedTextures.Count > 0)
                    {
                        listBox_Textures.SelectedItem = EmbFile.Entry[EmbFile.Entry.Count - 1];
                        listBox_Textures.ScrollIntoView(EmbFile.Entry[EmbFile.Entry.Count - 1]);

                        undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Texture Paste"));
                    }
                }
            }
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Options Logic
        private int Emb_Options_RemoveUnusedTextures()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            int removedCount = parent.effectContainerFile.RemoveUnusedTextures(assetType, undos);

            undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Texture)"));

            return removedCount;
        }

        private int Emb_Options_MergeDuplicateTextures()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            int merged = parent.effectContainerFile.MergeDuplicateTextures(assetType, undos);

            undos.Add(new UndoActionDelegate(this, nameof(RefreshTextureCount), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Texture)"));

            return merged;
        }

        private void RenameFile_PopUp(EmbEntry embEntry)
        {
            RenameForm renameForm = new RenameForm(System.IO.Path.GetFileNameWithoutExtension(embEntry.Name), ".dds", String.Format("Renaming {0}", embEntry.Name), EmbFile, this, RenameForm.Mode.Texture);
            renameForm.ShowDialog();

            if (renameForm.WasNameChanged)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EmbEntry>(nameof(EmbEntry.Name), embEntry, embEntry.Name, renameForm.NameValue + ".dds", "EMB Rename"));
                embEntry.Name = renameForm.NameValue + ".dds";
            }
        }

        //Misc
        public void RefreshTextureCount()
        {
            NotifyPropertyChanged(nameof(TextureCount));
        }

        //Search
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                EmbFile.UpdateTextureFilter();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            EmbFile.UpdateTextureFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            EmbFile.TextureSearchFilter = string.Empty;
            EmbFile.UpdateTextureFilter();
        }

        private void ListBox_Textures_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.Delete) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Delete_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.R) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Rename_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.E) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Replace_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.OemQuestion) && Keyboard.IsKeyDown(Key.LeftShift) && IsForContainer)
            {
                EmbContextMenu_UsedBy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.H) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_HueAdjustment_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.LeftCtrl) && IsForContainer)
            {
                EmbContextMenu_Merge_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Copy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                EmbContextMenu_Duplicate_Click(null, null);
                e.Handled = true;
            }
        }

        private void ToggleGrid_Button_Click(object sender, RoutedEventArgs e)
        {
            if(grid.Opacity >= 0.5)
            {
                grid.Opacity = 0.0;
            }
            else
            {
                grid.Opacity = 0.5;
            }
        }
    }
}
