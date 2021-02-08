using EEPK_Organiser.Forms.Recolor;
using EEPK_Organiser.View;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EmmEditForm.xaml
    /// </summary>
    public partial class EmmEditForm : MetroWindow, INotifyPropertyChanged
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
        public EMM_File EmmFile { get; set; }
        public AssetContainerTool container { get; set; }
        public bool IsForContainer { get; set; }
        public EepkEditor parent = null;

        //View
        public string MaterialCount
        {
            get
            {
                if (EmmFile.Materials == null) return null; //Shouldn't happen
                return string.Format("{0}/--", EmmFile.Materials.Count);
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


        public EmmEditForm(EMM_File _emmFile, AssetContainerTool _container, AssetType _assetType, EepkEditor parent, bool isForContainer = true, string windowTitle = null)
        {
            IsForContainer = isForContainer;
            EmmFile = _emmFile;
            container = _container;
            assetType = _assetType;
            InitializeComponent();
            DataContext = this;
            //Owner = parent;
            this.parent = parent;

            if(windowTitle != null)
            {
                Title += string.Format(" ({0})", windowTitle);
            }

            if (assetType != AssetType.PBIND && assetType != AssetType.TBIND && IsForContainer)
            {
                MessageBox.Show("EmmEditForm cannot be used on AssetType: " + assetType);
                Close();
            }

            dataGrid.EnableColumnVirtualization = true;
            dataGrid.EnableRowVirtualization = true;
            dataGrid.SelectedItem = null;
        }

        private void ValueType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Do validation here
            //As I cant seem to figure out how to access the nested dataGrid to find the currently selected parameter, I'll just validate the whole material.


            try
            {
                var material = dataGrid.SelectedItem as Material;
                
                if(material != null)
                {
                    material.Validate();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured while changing the selection.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var material = dataGrid.SelectedItem as Material;

                if (material != null)
                {
                    material.Validate(true);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured while validating the text.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Value_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var material = dataGrid.SelectedItem as Material;

                if (material != null)
                {
                    material.Validate(false);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured while validating the text.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Misc
        public void RefreshMaterialCount()
        {
            NotifyPropertyChanged("MaterialCount");
        }

        //Options
        private void Options_AddMaterials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Material material = Material.NewMaterial();
                material.Str_00 = EmmFile.GetUnusedName(material.Str_00);
                
                EmmFile.Materials.Add(material);
                RefreshMaterialCount();
                dataGrid.SelectedItem = material;
                dataGrid.ScrollIntoView(material);

                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoableListAdd<Material>(EmmFile.Materials, material));
                undos.Add(new UndoActionDelegate(this, nameof(RefreshMaterialCount), true));
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New Material"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured while adding the material.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Options_MergeDuplicates_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(String.Format("All instances of duplicated materials will be merged into a single material. A duplicated material means any that share the same parameters, but have a different name. \n\nAll references to the duplicates in any {0} will also be updated to reflect these changes.\n\nDo you want to continue?", AssetTypeWildcard), "Merge Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            int duplicateCount = Emm_Options_MergeDuplicateMaterials();

            if (duplicateCount > 0)
            {
                MessageBox.Show(String.Format("{0} material instances were merged.", duplicateCount), "Merge Duplicates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No instances of duplicated materials were found.", "Merge Duplicates", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshMaterialCount();
        }

        private void Options_DeleteUnused_Click(object sender, RoutedEventArgs e)
        {
            if (!IsForContainer) return;

            if (MessageBox.Show(String.Format("Any material that is not currently used by a {0} will be deleted. This cannot be undone.\n\nDo you want to continue?", AssetTypeWildcard), "Delete Unused", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            int removeCount = Emm_Options_RemoveUnusedMaterials();

            if (removeCount > 0)
            {
                MessageBox.Show(String.Format("{0} unused materials were removed.", removeCount), "Delete Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No unused materials were found.", "Delete Unused", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshMaterialCount();
        }

        //Logic
        private int Emm_Options_MergeDuplicateMaterials()
        {
            if (!IsForContainer || parent == null) return 0;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            int duplicates = parent.effectContainerFile.MergeDuplicateMaterials(assetType, undos);

            undos.Add(new UndoActionDelegate(this, nameof(RefreshMaterialCount), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Material)"));

            return duplicates;
        }

        private int Emm_Options_RemoveUnusedMaterials()
        {
            if (!IsForContainer || parent == null) return 0;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            int removed = parent.effectContainerFile.RemoveUnusedMaterials(assetType, undos);

            undos.Add(new UndoActionDelegate(this, nameof(RefreshMaterialCount), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Material)"));

            return removed;
        }

        private void RenameFile_PopUp(Material material)
        {
            RenameForm renameForm = new RenameForm(material.Str_00, "", String.Format("Renaming {0}", material.Str_00), EmmFile, this, RenameForm.Mode.Material, 32);
            renameForm.ShowDialog();

            if (renameForm.WasNameChanged)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Material>(nameof(material.Str_00), material, material.Str_00, renameForm.NameValue, "Rename (Material)"));
                material.Str_00 = renameForm.NameValue;
            }
        }


        //Context Menu
        private void ContextMenu_AddParameter_Click(object sender, RoutedEventArgs e)
        {
            var material = dataGrid.SelectedItem as Material;

            if(material != null)
            {
                Parameter newParam = Parameter.NewParameter();
                material.Parameters.Add(newParam);

                UndoManager.Instance.AddUndo(new UndoableListAdd<Parameter>(material.Parameters, newParam, "Add Parameter"));
            }
        }

        private void ContextMenu_RenameMaterial_Click(object sender, RoutedEventArgs e)
        {
            var material = dataGrid.SelectedItem as Material;

            if (material != null)
            {
                RenameFile_PopUp(material);
            }
        }

        private void ContextMenu_Merge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var material = dataGrid.SelectedItem as Material;
                List<Material> selectedMaterials = dataGrid.SelectedItems.Cast<Material>().ToList();
                selectedMaterials.Remove(material);


                if (material != null && selectedMaterials.Count > 0)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    int count = selectedMaterials.Count + 1;

                    if (MessageBox.Show(string.Format("All currently selected materials will be MERGED into {0}.\n\nAll other selected materials will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", material.Str_00), string.Format("Merge ({0} materials)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        foreach (var materialToRemove in selectedMaterials)
                        {
                            container.RefactorMaterialRef(materialToRemove, material, undos);
                            undos.Add(new UndoableListRemove<Material>(container.File2_Ref.Materials, materialToRemove));
                            container.File2_Ref.Materials.Remove(materialToRemove);
                        }

                        undos.Add(new UndoActionDelegate(this, nameof(RefreshMaterialCount), true));
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge (Material)"));
                    }
                }
                else
                {
                    MessageBox.Show("Cannot merge with less than 2 materials selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            RefreshMaterialCount();
        }

        private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            bool materialInUse = false;
            int removed = 0;
            List<Material> selectedMaterials = dataGrid.SelectedItems.Cast<Material>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedMaterials.Count > 0)
            {
                foreach (var material in selectedMaterials)
                {
                    if (container.IsMaterialUsed(material))
                    {
                        materialInUse = true;
                    }
                    else
                    {
                        removed++;
                        container.DeleteMaterial(material, undos);
                    }
                }

                if(removed > 0)
                {
                    undos.Add(new UndoActionDelegate(this, nameof(RefreshMaterialCount), true));
                    RefreshMaterialCount();
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Material"));
                }


                if (materialInUse && selectedMaterials.Count == 1)
                {
                    MessageBox.Show("The selected material cannot be deleted because it is currently being used.", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (materialInUse && selectedMaterials.Count > 1)
                {
                    MessageBox.Show("One or more of the selected materials cannot be deleted because they are currently being used.", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ContextMenu_UsedBy_Click(object sender, RoutedEventArgs e)
        {
            var material = dataGrid.SelectedItem as Material;

            if (material != null)
            {
                List<string> assets = container.MaterialUsedBy(material);
                assets.Sort();
                StringBuilder str = new StringBuilder();

                foreach (var asset in assets)
                {
                    str.Append(String.Format("{0}\r", asset));
                }

                LogForm logForm = new LogForm(String.Format("The following {0} assets use this material:", AssetTypeWildcard), str.ToString(), String.Format("{0}: Used By", material.Str_00), this, true);
                logForm.Show();
            }
        }

        private void ContextMenu_DeleteParameter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;

                if (menuItem != null)
                {
                    var nestedDataGrid = ((ContextMenu)menuItem.Parent).PlacementTarget as DataGrid;

                    var selectedParams = nestedDataGrid.SelectedItems.Cast<Parameter>().ToList();
                    var parentMaterial = container.GetMaterialAssociatedWithParameters(selectedParams, dataGrid.SelectedItem as Material);
                    

                    if (selectedParams.Count > 0 && parentMaterial != null)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        foreach(var param in selectedParams)
                        {
                            undos.Add(new UndoableListRemove<Parameter>(parentMaterial.Parameters, param));
                            parentMaterial.Parameters.Remove(param);
                        }

                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, selectedParams.Count > 1 ? "Delete Parameters" : "Delete Parameter"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContextMenu_HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var material = dataGrid.SelectedItem as Material;

                if (material != null)
                {
                    RecolorAll recolor = new RecolorAll(material, this);

                    if(recolor.Initialize())
                        recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        private void ContextMenu_HueSet_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                var material = dataGrid.SelectedItem as Material;

                if (material != null)
                {
                    RecolorAll_HueSet recolor = new RecolorAll_HueSet(material, this);

                    if (recolor.Initialize())
                        recolor.ShowDialog();
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                parent.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        //Filter
        private void Filter_Search_Click(object sender, RoutedEventArgs e)
        {
            EmmFile.UpdateMaterialFilter();
        }

        private void Filter_ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EmmFile.MaterialSearchFilter))
            {
                EmmFile.MaterialSearchFilter = string.Empty;
                EmmFile.UpdateMaterialFilter();
            }
        }

        private void Filter_SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Filter_Search_Click(null, null);
            }
        }
        
    }
}
