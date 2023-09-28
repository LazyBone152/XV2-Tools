using System;
using System.Linq;
using System.Windows;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Forms.Recolor;
using EEPK_Organiser.ViewModel;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System.Windows.Media;
using Xv2CoreLib.EMM.Analyzer;
using LB_Common.Numbers;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for MaterialsEditor.xaml
    /// </summary>
    public partial class MaterialsEditor : UserControl, INotifyPropertyChanged, IDisposable
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
        public static readonly DependencyProperty EmmFileProperty = DependencyProperty.Register(nameof(EmmFile), typeof(EMM_File), typeof(MaterialsEditor), new PropertyMetadata(null));

        public EMM_File EmmFile
        {
            get { return (EMM_File)GetValue(EmmFileProperty); }
            set
            {
                SetValue(EmmFileProperty, value);
                NotifyPropertyChanged(nameof(EmmFile));
            }
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(MaterialsEditor), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get { return (AssetContainerTool)GetValue(AssetContainerProperty); }
            set
            {
                SetValue(AssetContainerProperty, value);
                NotifyPropertyChanged(nameof(AssetContainer));
                NotifyPropertyChanged(nameof(ContainerVisiblility));
                NotifyPropertyChanged(nameof(InverseContainerVisiblility));
                NotifyPropertyChanged(nameof(IsForContainer));
            }
        }

        #endregion

        #region Properties
        //UI
        private EmmMaterial _selectedMaterial = null;
        public EmmMaterial SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {

                _selectedMaterial = value;
                NotifyPropertyChanged(nameof(SelectedMaterial));
                NotifyPropertyChanged(nameof(SelectedMaterialName));
                NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));
                RefreshViewModel();
            }
        }
        public bool ParameterEditorEnabled => _selectedMaterial != null;

        //ViewModel
        public MaterialViewModel MaterialViewModel { get; set; } = new MaterialViewModel();

        //Selected Material Editing
        public int SelectedMaterialID
        {
            get => _selectedMaterial != null ? _selectedMaterial.Index : -1;
            set
            {
                if (value != _selectedMaterial.Index)
                {
                    SetID(value);
                }
            }
        }

        public string SelectedMaterialName
        {
            get => _selectedMaterial?.Name;
            set
            {
                if (_selectedMaterial == null) return;
                if (value != _selectedMaterial.Name)
                {
                    SetName(value);
                }
            }
        }
        public string SelectedMaterialShaderProgram
        {
            get => _selectedMaterial?.ShaderProgram;
            set
            {
                if (_selectedMaterial == null) return;
                if (value != _selectedMaterial.ShaderProgram)
                {
                    SetShaderProgram(value);
                }
            }
        }

        //Properties for disabling and hiding elements that aren't needed in the current context. (e.g: dont show merge/used by options when editing a emm file for a emo or character as those are only useful for PBIND/TBIND assets)
        public bool IsForContainer => AssetContainer != null;
        public Visibility ContainerVisiblility => IsForContainer ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InverseContainerVisiblility => IsForContainer ? Visibility.Collapsed : Visibility.Visible;

        //Options
        private bool _filterParameters = true;

        public bool FilterParameters
        {
            get => _filterParameters;
            set
            {
                _filterParameters = value;
                UpdateVisibilities();
            }
        }
        public bool FilterTabs { get; set; } = true;

        #endregion

        #region ViewMaterials
        private ListCollectionView _viewMaterials = null;
        public ListCollectionView ViewMaterials
        {
            get
            {
                if (_viewMaterials == null && EmmFile != null)
                {
                    _viewMaterials = new ListCollectionView(EmmFile.Materials.Binding);
                    _viewMaterials.Filter = new Predicate<object>(SearchFilterCheck);
                }
                return _viewMaterials;
            }
            set
            {
                if (value != _viewMaterials)
                {
                    _viewMaterials = value;
                    NotifyPropertyChanged(nameof(ViewMaterials));
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
                RefreshViewMaterials();
                NotifyPropertyChanged(nameof(SearchFilter));
            }
        }

        private void RefreshViewMaterials()
        {
            if (_viewMaterials == null)
                _viewMaterials = new ListCollectionView(EmmFile.Materials.Binding);

            _viewMaterials.Filter = new Predicate<object>(SearchFilterCheck);
            NotifyPropertyChanged(nameof(ViewMaterials));
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            var _material = material as EmmMaterial;
            string flattenedSearchParam = SearchFilter.ToLower();

            if (_material != null)
            {
                //Search is for material name or shader program
                if (_material.Name.ToLower().Contains(flattenedSearchParam) || _material.ShaderProgram.ToLower().Contains(flattenedSearchParam))
                {
                    return true;
                }

                //Search for parameters that are "used" (dont have default values)
                if (_material.DecompiledParameters.HasParameter(SearchFilter))
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

        public MaterialsEditor()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Loaded += MaterialsEditor_Loaded;
        }


        #region EventsAndManualUIUpdating
        private void MaterialsEditor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ContainerVisiblility));
            NotifyPropertyChanged(nameof(InverseContainerVisiblility));
            RefreshViewMaterials();

            if (AssetContainer != null)
                idColumn.Visibility = Visibility.Collapsed;

            ScaleOffsetParameters = new FrameworkElement[]
            {
                MatScale0,
                MatScale1,
                MatOffset0,
                MatOffset1
            };

            ColorParameters = new FrameworkElement[]
            {
                Glare,
                GlareCol,
                MatCol0,
                MatCol1,
                MatCol2,
                MatCol3
            };

            TextureParameters = new FrameworkElement[]
            {
                TexScrl0,
                TexScrl1,
                TexRep0,
                TexRep1,
                TexRep2,
                TextureFilter0,
                TextureFilter1,
                TextureFilter2,
                MipMapLod0,
                MipMapLod1,
                MipMapLod2,
                gToonTextureWidth,
                gToonTextureHeight,
                ToonSamplerAddress,
                MarkSamplerAddress
            };

            AlphaParameters = new FrameworkElement[]
            {
                AlphaBlend,
                AlphaBlendType,
                AlphaTest,
                AlphaTestThreshold,
                AlphaRef,
                AlphaSortMask,
                ZTestMask,
                ZWriteMask
            };

            LightingParameters = new FrameworkElement[]
            {
                gCamPos,
                MatDif,
                MatAmb,
                MatSpc,
                MatDifScale,
                MatAmbScale,
                SpcCoeff,
                SpcPower
            };

            MiscParameters = new FrameworkElement[]
            {
                VsFlag0,
                VsFlag1,
                VsFlag2,
                VsFlag3,
                CustomFlag,
                BackFace,
                TwoSidedRender,
                IncidencePower,
                IncidenceAlphaBias,
                ReflectCoeff,
                ReflectFresnelBias,
                ReflectFresnelCoeff,
                AnimationChannel
            };

            UnknownParameters = new FrameworkElement[]
            {
                gLightDir,
                gLightDif,
                gLightSpc,
                gLightAmb,
                DirLight0Dir,
                DirLight0Col,
                AmbLight0Col,
                Billboard,
                BillboardType,
                NoEdge,
                Shimmer,
                gTime,
                Ambient,
                Diffuse,
                Specular,
                SpecularPower,
                FadeInit,
                FadeSpeed,
                GradientInit,
                GradientSpeed,
                gGradientCol,
                RimCoeff,
                RimPower
            };

            ExpanderUpdate();
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            UpdateProperties();

            if(e.UndoArg == "ShaderProgram" && e.UndoContext == SelectedMaterial)
            {
                UpdateVisibilities();
            }
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(SelectedMaterialName));
            NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));

            try
            {
                //If properties on the material itself have changed externally (such as on a undo/redo), then the list needs to be refreshed.
                materialDataGrid.Items.Refresh();
            }
            catch { }

            if (MaterialViewModel != null)
                MaterialViewModel.UpdateProperties();
        }

        private void ExpanderUpdate()
        {
            if (FilterTabs)
            {
                //Collapse all expanders
                alphaExpander.IsExpanded = false;
                colorExpander.IsExpanded = false;
                lightingExpander.IsExpanded = false;
                miscExpander.IsExpanded = false;
                scaleOffsetExpander.IsExpanded = false;
                textureExpander.IsExpanded = false;
                unknownExpander.IsExpanded = false;

                if (SelectedMaterial != null)
                {
                    //Selectively expand ones that have parameters within
                    alphaExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Alpha);
                    colorExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Color);
                    lightingExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Lighting);
                    miscExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Misc);
                    scaleOffsetExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.MatScaleOffset);
                    textureExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Texture);
                    unknownExpander.IsExpanded = SelectedMaterial.DecompiledParameters.IsGroupUsed(ParameterGroup.Unsorted);
                }
            }

            UpdateVisibilities();
        }

        private void RefreshViewModel()
        {
            MaterialViewModel.SetMaterial(SelectedMaterial?.DecompiledParameters);
            NotifyPropertyChanged(nameof(MaterialViewModel));
            NotifyPropertyChanged(nameof(ParameterEditorEnabled));
            ExpanderUpdate();
        }
        #endregion

        #region ContextMenuCommands
        public RelayCommand AddNewMaterialCommand => new RelayCommand(AddNewMaterial);
        private void AddNewMaterial()
        {
            EmmMaterial material = EmmMaterial.NewMaterial();
            material.Name = EmmFile.GetUnusedName(material.Name);
            material.Index = EmmFile.GetNewID();

            EmmFile.Materials.Add(material);
            SelectedMaterial = material;
            materialDataGrid.ScrollIntoView(material);

            UndoManager.Instance.AddUndo(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material, "New Material"));
        }

        public RelayCommand DeleteMaterialCommand => new RelayCommand(DeleteMaterial, IsMaterialSelected);
        private async void DeleteMaterial()
        {
            bool materialInUse = false;
            int removed = 0;
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedMaterials.Count > 0)
            {
                if (IsForContainer)
                {
                    foreach (var material in selectedMaterials)
                    {
                        if (AssetContainer.IsMaterialUsed(material))
                        {
                            materialInUse = true;
                        }
                        else
                        {
                            removed++;
                            AssetContainer.DeleteMaterial(material, undos);
                        }
                    }

                    if (materialInUse && selectedMaterials.Count == 1)
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(this, "Delete", "The selected material cannot be deleted because it is currently being used.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                    else if (materialInUse && selectedMaterials.Count > 1)
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(this, "Delete", "One or more of the selected materials cannot be deleted because they are currently being used.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                }
                else
                {
                    foreach (var material in selectedMaterials)
                    {
                        removed++;
                        undos.Add(new UndoableListRemove<EmmMaterial>(EmmFile.Materials, material, EmmFile.Materials.IndexOf(material)));
                        EmmFile.Materials.Remove(material);
                    }
                }

                if (removed > 0)
                {
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Material"));
                }
            }
        }

        public RelayCommand DuplicateMaterialCommand => new RelayCommand(DuplicateMaterial, IsMaterialSelected);
        private void DuplicateMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EmmMaterial selectedMat = null;

            try
            {
                foreach (var mat in selectedMaterials)
                {
                    EmmMaterial newMaterial = mat.Copy();
                    newMaterial.Index = EmmFile.GetNewID();
                    newMaterial.Name = EmmFile.GetUnusedName(newMaterial.Name);
                    undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, newMaterial));
                    EmmFile.Materials.Add(newMaterial);

                    selectedMat = newMaterial;
                }

                SelectedMaterial = selectedMat;
                materialDataGrid.ScrollIntoView(SelectedMaterial);
            }
            finally
            {
                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Material(s)");
            }
        }

        public RelayCommand MergeMaterialCommand => new RelayCommand(MergeMaterial, IsMaterialSelected);
        private async void MergeMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            selectedMaterials.Remove(SelectedMaterial);

            if (SelectedMaterial != null && selectedMaterials.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int count = selectedMaterials.Count + 1;

                var result = await DialogCoordinator.Instance.ShowMessageAsync(this, string.Format("Merge ({0} materials)", count), string.Format("All currently selected materials will be MERGED into {0}.\n\nAll other selected materials will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", SelectedMaterial.Name), MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result == MessageDialogResult.Affirmative)
                {
                    foreach (var materialToRemove in selectedMaterials)
                    {
                        AssetContainer.RefactorMaterialRef(materialToRemove, SelectedMaterial, undos);
                        undos.Add(new UndoableListRemove<EmmMaterial>(AssetContainer.File2_Ref.Materials, materialToRemove));
                        AssetContainer.File2_Ref.Materials.Remove(materialToRemove);
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge (Material)"));
                }
            }
            else
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge", "Cannot merge with less than 2 materials selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }

        public RelayCommand UsedByCommand => new RelayCommand(UsedBy, IsMaterialSelected);
        private void UsedBy()
        {
            if (SelectedMaterial != null)
            {
                List<string> assets = AssetContainer.MaterialUsedBy(SelectedMaterial);
                assets.Sort();
                StringBuilder str = new StringBuilder();

                foreach (var asset in assets)
                {
                    str.Append(String.Format("{0}\r", asset));
                }

                LogForm logForm = new LogForm("The following assets use this material", str.ToString(), string.Format("{0}: Used By", SelectedMaterial.Name), null, true);
                logForm.Show();
            }
        }

        public RelayCommand HueShiftCommand => new RelayCommand(HueShift, IsMaterialSelected);
        private void HueShift()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll recolor = new RecolorAll(SelectedMaterial, Application.Current.Windows.OfType<MetroWindow>().SingleOrDefault(x => x.IsFocused));

                if (recolor.Initialize())
                    recolor.ShowDialog();

                UndoManager.Instance.ForceEventCall();
            }
        }

        public RelayCommand HueSetCommand => new RelayCommand(HueSet, IsMaterialSelected);
        private void HueSet()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll_HueSet recolor = new RecolorAll_HueSet(SelectedMaterial, Application.Current.Windows.OfType<MetroWindow>().SingleOrDefault(x => x.IsFocused));

                if (recolor.Initialize())
                    recolor.ShowDialog();

                UndoManager.Instance.ForceEventCall();
            }
        }

        public RelayCommand CopyMaterialCommand => new RelayCommand(CopyMaterial, IsMaterialSelected);
        private void CopyMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();

            if (selectedMaterials != null)
            {
                Clipboard.SetData(Misc.ClipboardDataTypes.EmmMaterial, selectedMaterials);
            }
        }

        public RelayCommand PasteMaterialCommand => new RelayCommand(PasteMaterial, CanPasteMaterial);
        private void PasteMaterial()
        {
            List<EmmMaterial> copiedMaterials = (List<EmmMaterial>)Clipboard.GetData(Misc.ClipboardDataTypes.EmmMaterial);

            if (copiedMaterials != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var material in copiedMaterials)
                {
                    material.Name = EmmFile.GetUnusedName(material.Name);
                    material.Index = EmmFile.GetNewID();
                    EmmFile.Materials.Add(material);

                    undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material));
                }

                UndoManager.Instance.AddCompositeUndo(undos, copiedMaterials.Count > 1 ? "Paste Materials" : "Paste Material");
            }

        }

        public RelayCommand PasteMaterialValuesCommand => new RelayCommand(PasteMaterialValues, CanPasteMaterialValues);
        private async void PasteMaterialValues()
        {
            List<EmmMaterial> copiedMaterials = (List<EmmMaterial>)Clipboard.GetData(Misc.ClipboardDataTypes.EmmMaterial);

            if (copiedMaterials != null)
            {
                if (copiedMaterials.Count == 0 || copiedMaterials.Count > 1)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Paste Values", "Cannot paste the material values as there were more than 1 copied.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                List<IUndoRedo> undos = SelectedMaterial.DecompiledParameters.PasteValues(copiedMaterials[0]);
                undos.Add(new UndoablePropertyGeneric(nameof(SelectedMaterial.ShaderProgram), SelectedMaterial, SelectedMaterial.ShaderProgram, copiedMaterials[0].ShaderProgram));
                SelectedMaterial.ShaderProgram = copiedMaterials[0].ShaderProgram;

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Values");

                NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));
                UpdateProperties();
            }

        }



        private bool IsMaterialSelected()
        {
            return _selectedMaterial != null;
        }

        private bool CanPasteMaterial()
        {
            return Clipboard.ContainsData(Misc.ClipboardDataTypes.EmmMaterial);
        }

        private bool CanPasteMaterialValues()
        {
            return CanPasteMaterial() && IsMaterialSelected();
        }
        #endregion

        #region ToolsCommand
        public RelayCommand MergeDuplicatesCommand => new RelayCommand(MergeDuplicates);
        private async void MergeDuplicates()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "All instances of duplicated materials will be merged into a single material. A duplicated material means any that share the same parameters, but have a different name. \n\nAll references to the duplicates in any assets will also be updated to reflect these changes.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.MergeDuplicateMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Material)"));

                if (duplicateCount > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", string.Format("{0} material instances were merged.", duplicateCount), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "No instances of duplicated materials were found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

        }

        public RelayCommand RemoveUnusedMaterialsCommand => new RelayCommand(RemoveUnusedMaterials);
        private async void RemoveUnusedMaterials()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused", "Any material that is not currently used by a asset will be deleted.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.RemoveUnusedMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Mats)"));

                if (duplicateCount > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", string.Format("{0} material instances were removed.", duplicateCount), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", "No unused materials were found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

        }


        #endregion

        #region ParameterVisibility
        private FrameworkElement[] ScaleOffsetParameters = null;
        private FrameworkElement[] ColorParameters = null;
        private FrameworkElement[] TextureParameters = null;
        private FrameworkElement[] AlphaParameters = null;
        private FrameworkElement[] LightingParameters = null;
        private FrameworkElement[] MiscParameters = null;
        private FrameworkElement[] UnknownParameters = null;

        private void UpdateVisibilities()
        {
            if (ScaleOffsetParameters == null || UnknownParameters == null) return;

            if (FilterParameters)
            {
                AnalyzedShader shaderHelper = MaterialAnalyzer.Instance.ShaderHelper.Shaders.FirstOrDefault(x => x.ShaderProgram == SelectedMaterial?.ShaderProgram);

                UpdateVisibilitiesRecursive(ScaleOffsetParameters, shaderHelper);
                UpdateVisibilitiesRecursive(ColorParameters, shaderHelper);
                UpdateVisibilitiesRecursive(TextureParameters, shaderHelper);
                UpdateVisibilitiesRecursive(AlphaParameters, shaderHelper);
                UpdateVisibilitiesRecursive(LightingParameters, shaderHelper);
                UpdateVisibilitiesRecursive(MiscParameters, shaderHelper);
                UpdateVisibilitiesRecursive(UnknownParameters, shaderHelper);

                scaleOffsetExpander.Visibility = ScaleOffsetParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                colorExpander.Visibility = ColorParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                textureExpander.Visibility = TextureParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                alphaExpander.Visibility = AlphaParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                lightingExpander.Visibility = LightingParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                miscExpander.Visibility = MiscParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                unknownExpander.Visibility = UnknownParameters.Any(x => x.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                //Make all visible
                UpdateVisibilitiesRecursive(ScaleOffsetParameters, null, true);
                UpdateVisibilitiesRecursive(ColorParameters, null, true);
                UpdateVisibilitiesRecursive(TextureParameters, null, true);
                UpdateVisibilitiesRecursive(AlphaParameters, null, true);
                UpdateVisibilitiesRecursive(LightingParameters, null, true);
                UpdateVisibilitiesRecursive(MiscParameters, null, true);
                UpdateVisibilitiesRecursive(UnknownParameters, null, true);

                scaleOffsetExpander.Visibility = Visibility.Visible;
                colorExpander.Visibility = Visibility.Visible;
                textureExpander.Visibility = Visibility.Visible;
                alphaExpander.Visibility = Visibility.Visible;
                lightingExpander.Visibility = Visibility.Visible;
                miscExpander.Visibility = Visibility.Visible;
                unknownExpander.Visibility = Visibility.Visible;
            }
        }

        private void UpdateVisibilitiesRecursive(FrameworkElement[] elements, AnalyzedShader shader, bool allVisible = false)
        {
            for(int i = 0; i < elements.Length; i++)
            {
                if (MaterialAnalyzer.Instance.ShaderHelper.AllParameters.Contains(elements[i].Name))
                {
                    if (shader != null)
                    {
                        elements[i].Visibility = shader.Parameters.Any(x => x.Name == elements[i].Name) ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        elements[i].Visibility = allVisible ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            Loaded -= MaterialsEditor_Loaded;
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private async void SetID(int newId)
        {
            if (EmmFile.Materials.Any(x => x.Index == newId && x != _selectedMaterial))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", $"Another material is already using ID \"{newId}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                NotifyPropertyChanged(nameof(SelectedMaterialID));
                return;
            }

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedMaterial.Index), _selectedMaterial, _selectedMaterial.Index, newId, "Material ID"));
            _selectedMaterial.Index = newId;
            NotifyPropertyChanged(nameof(SelectedMaterialID));
        }

        private async void SetName(string name)
        {
            //Name should never be more than 32 since the UI is limited to just 32 characters, but just in case we will trim the name here if it exceeds the limit.
            if (name.Length > 32)
                name = name.Substring(0, 32);

            if (EmmFile.Materials.Any(x => x.Name == name && x != _selectedMaterial))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", $"Another material is already named\"{name}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                NotifyPropertyChanged(nameof(SelectedMaterialName));
                return;
            }

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedMaterial.Name), _selectedMaterial, _selectedMaterial.Name, name, "Material Name"));
            _selectedMaterial.Name = name;
            NotifyPropertyChanged(nameof(SelectedMaterialName));
        }

        private void SetShaderProgram(string shader)
        {
            //ShaderProgram should never be more than 32 since the UI is limited to just 32 characters, but just in case we will trim the name here if it exceeds the limit.
            if (shader.Length > 32)
                shader = shader.Substring(0, 32);

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedMaterial.ShaderProgram), _selectedMaterial, _selectedMaterial.ShaderProgram, shader, "Material ShaderProgram"), 0, "ShaderProgram", SelectedMaterial);
            _selectedMaterial.ShaderProgram = shader;
            NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));
            UpdateVisibilities();
        }
    }
}
