using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.ViewModel;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMM.Analyzer;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for MaterialsEditor.xaml
    /// </summary>
    public partial class MaterialsEditor : AutoObservableUserControl, IDisposable
    {
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
        public ObservableCollection<EmmMaterial> SelectedMaterials { get; } = new ObservableCollection<EmmMaterial>();
        public bool ParameterEditorEnabled => _selectedMaterial != null;

        //ViewModel
        public MaterialViewModel MaterialViewModel { get; set; } = new MaterialViewModel();

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
        [RelayCommand]
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
            Unloaded += MaterialsEditor_Unloaded;
        }

        private void MaterialsEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            MaterialViewModel?.Dispose();
        }


        #region EventsAndManualUIUpdating
        private void MaterialsEditor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ContainerVisiblility));
            NotifyPropertyChanged(nameof(InverseContainerVisiblility));
            RefreshViewMaterials();

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
                MatCol3,
                MatCol4,
                MatCol5,
                MatCol6,
                MatCol7
            };

            TextureParameters = new FrameworkElement[]
            {
                TexScrl0,
                TexScrl1,
                TexScrl2,
                TexScrl3,
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
        [RelayCommand]
        private void AddNewMaterial()
        {
            EmmMaterial material = EmmMaterial.NewMaterial();
            material.Name = EmmFile.GetUnusedName(material.Name);
            material.Index = EmmFile.GetNewID();

            EmmFile.Materials.Add(material);
            SetSelectedMaterial(material);

            UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
            {
                new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material),
                new UndoActionDelegate(EmmFile, nameof(EmmFile.TriggerMaterialsChanged), true)
            }, "New Material");
            EmmFile.TriggerMaterialsChanged();
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void DeleteMaterial()
        {
            //bool materialInUse = false;
            int removed = 0;
            var selectedMaterials = SelectedMaterials.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedMaterials.Length > 0)
            {

                if (IsForContainer && selectedMaterials.Any(x => AssetContainer.IsMaterialUsed(x)))
                {
                    string type = AssetContainer.ContainerAssetType == Xv2CoreLib.EEPK.AssetType.PBIND ? "EMP" : "ETR";
                    if (MessagePrompt.Show($"One or more of the selected materials is currently assigned to a {type} and its reference will be removed if the material is deleted.\n\nDo you wish to continue?",
                            "Delete", MessagePromptButtons.YesNo, MessagePromptIcon.Warning) != MessagePromptResult.Yes)
                        return;
                }

                if (IsForContainer)
                {
                    foreach (var material in selectedMaterials)
                    {
                        removed++;
                        AssetContainer.DeleteMaterial(material, undos);
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
                    undos.Add(new UndoActionDelegate(EmmFile, nameof(EmmFile.TriggerMaterialsChanged), true));
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Material"));

                    EmmFile.TriggerMaterialsChanged();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void DuplicateMaterial()
        {
            var selectedMaterials = SelectedMaterials.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>(selectedMaterials.Length);
            List<EmmMaterial> newMaterials = new List<EmmMaterial>(selectedMaterials.Length);

            try
            {
                foreach (var mat in selectedMaterials)
                {
                    EmmMaterial newMaterial = mat.Copy();
                    newMaterials.Add(newMaterial);
                    newMaterial.Index = EmmFile.GetNewID();
                    newMaterial.Name = EmmFile.GetUnusedName(newMaterial.Name);
                    undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, newMaterial));
                    EmmFile.Materials.Add(newMaterial);
                }

                SetSelectedMaterials(newMaterials);
            }
            finally
            {
                undos.Add(new UndoActionDelegate(EmmFile, nameof(EmmFile.TriggerMaterialsChanged), true));
                EmmFile.TriggerMaterialsChanged();
                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Material(s)");
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void MergeMaterial()
        {
            var selectedMaterials = SelectedMaterials.ToList();
            selectedMaterials.Remove(SelectedMaterial);

            if (SelectedMaterial != null && selectedMaterials.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int count = selectedMaterials.Count + 1;

                var result = MessagePrompt.Show(
                    string.Format("All currently selected materials will be MERGED into {0}.\n\nAll other selected materials will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", SelectedMaterial.Name),
                    string.Format("Merge ({0} materials)", count), 
                    MessagePromptButtons.OKCancel, 
                    MessagePromptIcon.Question);

                if (result == MessagePromptResult.OK)
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
                MessagePrompt.Show("Cannot merge with less than 2 materials selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessagePromptButtons.OK, MessagePromptIcon.Warning);
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void UsedBy()
        {
            if (SelectedMaterial != null)
            {
                List<string> assets = AssetContainer.MaterialUsedBy(SelectedMaterial);
                assets.Sort();
                StringBuilder str = new StringBuilder();

                foreach (var asset in assets)
                {
                    str.Append(string.Format("{0}\r", asset));
                }

                MessagePrompt prompt = new MessagePrompt($"{SelectedMaterial.Name}: Used By", $"The following {assets.Count} assets use this material", str.ToString());
                prompt.Show();
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void HueShift()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll recolor = new RecolorAll(SelectedMaterials.ToList(), false, "Material");

                if (recolor.Initialize())
                    recolor.ShowDialog();

                UndoManager.Instance.ForceEventCall();
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void HueSet()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll recolor = new RecolorAll(SelectedMaterials.ToList(), true, "Material");

                if (recolor.Initialize())
                    recolor.ShowDialog();

                UndoManager.Instance.ForceEventCall();
            }
        }

        [RelayCommand(CanExecute = nameof(IsMaterialSelected))]
        private void CopyMaterial()
        {
            IList<EmmMaterial> selectedMaterials = SelectedMaterials.ToList();

            if (selectedMaterials != null)
            {
                CustomClipboard.SetData(Misc.ClipboardDataTypes.EmmMaterial, selectedMaterials);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteMaterial))]
        private void PasteMaterial()
        {
            if (!CustomClipboard.TryGetData(Misc.ClipboardDataTypes.EmmMaterial, out List<EmmMaterial> copiedMaterials))
                return;

            if (copiedMaterials != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>(copiedMaterials.Count);
                List<EmmMaterial> newMaterials = new List<EmmMaterial>(copiedMaterials.Count);

                foreach (var material in copiedMaterials)
                {
                    newMaterials.Add(material);
                    material.Name = EmmFile.GetUnusedName(material.Name);
                    material.Index = EmmFile.GetNewID();
                    EmmFile.Materials.Add(material);

                    undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material));
                }

                undos.Add(new UndoActionDelegate(EmmFile, nameof(EmmFile.TriggerMaterialsChanged), true));
                EmmFile.TriggerMaterialsChanged();
                UndoManager.Instance.AddCompositeUndo(undos, copiedMaterials.Count > 1 ? "Paste Materials" : "Paste Material");

                SetSelectedMaterials(newMaterials);
            }

        }

        [RelayCommand(CanExecute = nameof(CanPasteMaterialValues))]
        private void PasteMaterialValues()
        {
            if (!CustomClipboard.TryGetData(Misc.ClipboardDataTypes.EmmMaterial, out List<EmmMaterial> copiedMaterials))
                return;

            if (copiedMaterials != null)
            {
                if (copiedMaterials.Count == 0 || copiedMaterials.Count > 1)
                {
                    MessagePrompt.Show("Cannot paste the material values as there were more than 1 copied.", "Paste Values", MessagePromptButtons.OK, MessagePromptIcon.Warning);
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
            return CustomClipboard.ContainsData(Misc.ClipboardDataTypes.EmmMaterial);
        }

        private bool CanPasteMaterialValues()
        {
            return CanPasteMaterial() && IsMaterialSelected();
        }
        #endregion

        #region ToolsCommand
        [RelayCommand]
        private void MergeDuplicates()
        {
            var result = MessagePrompt.Show("All instances of duplicated materials will be merged into a single material. A duplicated material means any that share the same parameters, but have a different name. \n\nAll references to the duplicates in any assets will also be updated to reflect these changes.\n\nDo you want to continue?", "Merge Duplicates", MessagePromptButtons.OKCancel, MessagePromptIcon.Question);

            if (result == MessagePromptResult.OK)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.MergeDuplicateMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Material)"));

                if (duplicateCount > 0)
                {
                    MessagePrompt.Show($"{duplicateCount} material instances were merged.", "Merge Duplicates", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("No instances of duplicated materials were found.", "Merge Duplicates", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
            }
        }

        [RelayCommand]
        private void RemoveUnusedMaterials()
        {
            var result = MessagePrompt.Show("All materials that are not currently used by an asset will be deleted.\n\nDo you want to continue?", "Remove Unused", MessagePromptButtons.YesNo, MessagePromptIcon.Question);

            if (result == MessagePromptResult.Yes)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.RemoveUnusedMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Mats)"));

                if (duplicateCount > 0)
                {
                    MessagePrompt.Show($"{duplicateCount} material instances were removed.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("No unused materials were found.", "Remove Unused", MessagePromptButtons.OK, MessagePromptIcon.Information);
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
                AnalyzedShader shaderHelper = MaterialAnalyzer.Instance.ShaderHelper.GetShader(SelectedMaterial);

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

        private static void UpdateVisibilitiesRecursive(FrameworkElement[] elements, AnalyzedShader shader, bool allVisible = false)
        {
            for(int i = 0; i < elements.Length; i++)
            {
                if (MaterialAnalyzer.Instance.ShaderHelper.AllParameters.Contains(elements[i].Name) && shader != null)
                {
                    elements[i].Visibility = shader.Parameters.Any(x => x.Name == elements[i].Name) ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    elements[i].Visibility = allVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        #endregion

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            GC.SuppressFinalize(this);
        }

        private void SetName(string name)
        {
            //Name should never be more than 32 since the UI is limited to just 32 characters, but just in case we will trim the name here if it exceeds the limit.
            if (name.Length > 32)
                name = name.Substring(0, 32);

            if (EmmFile.Materials.Any(x => x.Name == name && x != _selectedMaterial))
            {
                MessagePrompt.Show($"Another material is already named \"{name}\".", "Name Already Used", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                NotifyPropertyChanged(nameof(SelectedMaterialName));
                return;
            }

            UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
            {
                new UndoablePropertyGeneric(nameof(_selectedMaterial.Name), _selectedMaterial, _selectedMaterial.Name, name),
                new UndoActionDelegate(EmmFile, nameof(EmmFile.TriggerMaterialsChanged), true)
            }, "Material Name");
            EmmFile.TriggerMaterialsChanged();

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

        private void SetSelectedMaterial(EmmMaterial material)
        {
            SelectedMaterials.Clear();
            SelectedMaterial = material;

            if (material != null)
                materialDataGrid.ScrollIntoView(material);
        }

        private void SetSelectedMaterials(IList<EmmMaterial> materials)
        {
            if (materials.Count == 0) return;

            SelectedMaterial = materials[0];
            SelectedMaterials.Clear();

            foreach(var material in materials)
            {
                SelectedMaterials.Add(material);
            }

            materialDataGrid.ScrollIntoView(SelectedMaterial);
        }
    }
}
