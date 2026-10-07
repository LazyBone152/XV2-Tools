using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using EEPK_Organiser.View.Controls;
using EEPK_Organiser.ViewModel;

namespace EEPK_Organiser.View.Editors
{
    /// <summary>
    /// Interaction logic for EtrEditor.xaml
    /// </summary>
    public partial class EtrEditor : AutoObservableUserControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty EtrFileProperty = DependencyProperty.Register(nameof(EtrFile), typeof(ETR_File), typeof(EtrEditor), new PropertyMetadata(null));

        public ETR_File EtrFile
        {
            get { return (ETR_File)GetValue(EtrFileProperty); }
            set
            {
                SetValue(EtrFileProperty, value);
                NotifyPropertyChanged(nameof(EtrFile));
            }
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(EtrEditor), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get => (AssetContainerTool)GetValue(AssetContainerProperty);
            set => SetValue(AssetContainerProperty, value);
        }

        #endregion

        private ETR_Node _selectedNode = null;
        public ETR_Node SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (value != _selectedNode)
                {
                    _selectedNode = value;
                    _viewModel.SetContext(value);
                    NotifyPropertyChanged(nameof(SelectedNode));
                    NotifyPropertyChanged(nameof(IsNodeEnabled));
                    NotifyPropertyChanged(nameof(ViewModel));
                }
            }
        }
        public ObservableCollection<ETR_Node> SelectedNodes { get; } = new ObservableCollection<ETR_Node>();
        private EtrNodeViewModel _viewModel = new EtrNodeViewModel();
        public EtrNodeViewModel ViewModel => SelectedNode != null ? _viewModel : null;
        public bool IsNodeEnabled => SelectedNode != null;


        public EtrEditor()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Unloaded += EcfEditor_Unloaded;
            Loaded += EtrEditor_Loaded;
            SelectedShapePoint.PropertyChanged += SelectedShapePoint_PropertyChanged;
        }

        private void SelectedShapePoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (shapeDrawPointDataGrid != null && SelectedShapePoint.Point != null)
            {
                shapeDrawPointDataGrid.ScrollIntoView(SelectedShapePoint.Point);
            }
        }

        private void EtrEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (EtrFile?.Nodes?.Count > 0)
                SelectedNode = EtrFile.Nodes[0];
        }

        private void EcfEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            //Update PreviewBrush when color has been changed externally
            if (e.UndoGroup == UndoGroup.ColorControl)
            {
                if (e.UndoContext is ETR_Node _node)
                {
                    _node.UpdatePreviewBrush();
                }
                else if (e.UndoContext is IList<ETR_Node> nodes)
                {
                    foreach(var node in nodes)
                    {
                        node.UpdatePreviewBrush();
                    }
                }
                else if (e.UndoContext is Asset asset)
                {
                    if (asset.AssetType == Xv2CoreLib.EEPK.AssetType.TBIND && asset.Files[0].EtrFile == EtrFile)
                    {
                        EtrFile.UpdateBrushPreview();
                    }
                }
                else if (e.UndoContext is IList<Asset> assets)
                {
                    if(assets.Any(x => x.AssetType == Xv2CoreLib.EEPK.AssetType.TBIND && x.Files[0].EtrFile == EtrFile))
                    {
                        EtrFile.UpdateBrushPreview();
                    }
                }
                else if (e.UndoContext is EffectContainerFile eepk)
                {
                    if (eepk.Tbind == AssetContainer)
                    {
                        EtrFile.UpdateBrushPreview();
                    }
                }

                return;
            }

            ViewModel?.UpdateProperties();
            NotifyPropertyChanged(nameof(IsNodeEnabled));
        }

        #region Commands

        [RelayCommand(CanExecute = nameof(IsETRLoaded))]
        private void AddNode()
        {
            ETR_Node node = ETR_Node.GetNew();
            EtrFile.Nodes.Add(node);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, node, "ETR -> Add Node"));

            SelectedNodes.Clear();
            SelectedNode = node;
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DeleteNode()
        {
            var nodes = SelectedNodes.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Length);

            foreach (ETR_Node node in nodes)
            {
                undos.Add(new UndoableListRemove<ETR_Node>(EtrFile.Nodes, node));
                EtrFile.Nodes.Remove(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Delete Node");
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void CopyNode()
        {
            CustomClipboard.SetData(ETR_Node.CLIPBOARD_ID, SelectedNodes.ToArray());
        }

        [RelayCommand(CanExecute = nameof(CanPasteNode))]
        private void PasteNode()
        {
            if (!CustomClipboard.TryGetData(ETR_Node.CLIPBOARD_ID, out ETR_Node[] nodes))
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Length);
            List<ETR_Node> newNodes = new List<ETR_Node>();

            foreach (ETR_Node node in nodes)
            {
                AssetContainer.AddTbindDependencies(node, EtrFile, undos);
                undos.Add(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, node));
                EtrFile.Nodes.Add(node);
                newNodes.Add(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Paste Node");
            SetSelectedNodes(newNodes);
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DuplicateNode()
        {
            var nodes = SelectedNodes.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Length);
            List<ETR_Node> newNodes = new List<ETR_Node>();

            foreach (ETR_Node node in nodes)
            {
                ETR_Node newNode = node.Clone();
                undos.Add(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, newNode));
                EtrFile.Nodes.Add(newNode);
                newNodes.Add(newNode);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Duplicate Node");
            SetSelectedNodes(newNodes);
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueAdjustment()
        {
            RecolorAll recolor = new RecolorAll(AssetContainer, SelectedNodes, false);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueSet()
        {
            RecolorAll recolor = new RecolorAll(AssetContainer, SelectedNodes, true);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        private bool IsETRLoaded()
        {
            return EtrFile != null;
        }

        private bool CanPasteNode()
        {
            return CustomClipboard.ContainsData(ETR_Node.CLIPBOARD_ID) && IsETRLoaded();
        }

        private bool IsNodeSelected()
        {
            return SelectedNode != null;
        }
        #endregion

        #region NodeCommands

        [RelayCommand(CanExecute = nameof(HasMaterial))]
        private void Node_RemoveMaterial()
        {
            ViewModel.MaterialRef = null;
        }

        [RelayCommand(CanExecute = nameof(HasMaterial))]
        private void Node_GotoMaterial()
        {
            MaterialsEditorForm emmForm = EepkEditor.AssetContainerOpenMaterialForm(AssetContainer, Xv2CoreLib.EEPK.AssetType.TBIND);
            emmForm.SelectMaterial(SelectedNode.MaterialRef);
        }

        [RelayCommand(CanExecute = nameof(HasTextureRef))]
        private void Node_RemoveTexture(int textureIndex)
        {
            //if (!HasTextureRef(textureIndex)) return;

            TextureEntry_Ref textureRef = SelectedNode.TextureEntryRef[textureIndex];
            textureRef.UndoableTextureRef = null;
        }

        [RelayCommand(CanExecute = nameof(HasTextureRef))]
        private void Node_GotoTexture(int textureIndex)
        {
            //if (!HasTextureRef(textureIndex)) return;

            TextureEntry_Ref textureRef = SelectedNode.TextureEntryRef[textureIndex];

            if (textureRef.TextureRef != null)
            {
                tabItems.SelectedIndex = 1;
                textureView.SelectTexture(textureRef.TextureRef);
            }
        }

        private bool HasMaterial()
        {
            return IsNodeSelected() ? SelectedNode?.MaterialRef != null : false;
        }

        private bool HasTextureRef(int texIndex)
        {
            return IsNodeSelected() ? SelectedNode.TextureEntryRef[texIndex].TextureRef != null : false;
        }
        #endregion

        #region ShapePoints
        public ShapePointRef SelectedShapePoint { get; set; } = new ShapePointRef();

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void NewShapeDrawPoint()
        {
            ShapeDrawPoint newPoint = new ShapeDrawPoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ShapeDrawPoint>(SelectedNode.ExtrudeShapePoints, newPoint, "Shape -> Add Point"));
            SelectedNode.ExtrudeShapePoints.Add(newPoint);

            SelectedShapePoint.Point = newPoint;
        }

        [RelayCommand(CanExecute = nameof(IsPointSelected))]
        private void DeleteShapeDrawPoint()
        {
            List<ShapeDrawPoint> selectedPoints = shapeDrawPointDataGrid.SelectedItems.Cast<ShapeDrawPoint>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var point in selectedPoints)
            {
                undos.Add(new UndoableListRemove<ShapeDrawPoint>(SelectedNode.ExtrudeShapePoints, point, "Shape -> Delete Point"));
                SelectedNode.ExtrudeShapePoints.Remove(point);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Shape -> Delete Point");
        }

        [RelayCommand(CanExecute = nameof(IsPointSelected))]
        private void CopyShapeDrawPoint()
        {
            List<ShapeDrawPoint> selectedPoints = shapeDrawPointDataGrid.SelectedItems.Cast<ShapeDrawPoint>().ToList();
            CustomClipboard.SetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT, selectedPoints);
        }

        [RelayCommand(CanExecute = nameof(CanPasteShapeDraw))]
        private void PasteShapeDrawPoint()
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT, out List<ShapeDrawPoint> points))
                return;

            if (points != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                SelectedNode.ExtrudeShapePoints.AddRange(points);

                foreach (var point in points)
                {
                    undos.Add(new UndoableListAdd<ShapeDrawPoint>(SelectedNode.ExtrudeShapePoints, point));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Shape -> Paste");
            }
        }

        [RelayCommand(CanExecute = nameof(CanPasteShapeDrawValues))]
        private void PasteShapeDrawPointValues()
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT, out List<ShapeDrawPoint> points))
                return;

            List<ShapeDrawPoint> selectedPoints = shapeDrawPointDataGrid.SelectedItems.Cast<ShapeDrawPoint>().ToList();

            if (points?.Count == selectedPoints.Count)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var point in selectedPoints)
                {
                    point.PasteValues(points[0], undos);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Shape -> Paste Values");
            }
            else
            {
                MessagePrompt.Show("\"Paste Values\" only works with an equal amount of copied and selected points.", "Paste Error", MessagePromptButtons.OK, MessagePromptIcon.Error);
            }
        }


        private bool CanPasteShapeDraw()
        {
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT) && IsNodeSelected();
        }

        private bool CanPasteShapeDrawValues()
        {
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT) && IsPointSelected();
        }

        private bool IsPointSelected()
        {
            if (!IsNodeSelected()) return false;
            return SelectedShapePoint?.Point != null;
        }

        #endregion

        #region Paths
        private ConeExtrudePoint _selectedPath = null;
        public ConeExtrudePoint SelectedPath
        {
            get => _selectedPath;
            set
            {
                if (_selectedPath != value)
                {
                    _selectedPath = value;
                    NotifyPropertyChanged(nameof(SelectedPath));
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void NewPath()
        {
            ConeExtrudePoint newPoint = new ConeExtrudePoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ConeExtrudePoint>(SelectedNode.ExtrudePaths, newPoint, "Path -> Add Path"));
            SelectedNode.ExtrudePaths.Add(newPoint);

            SelectedPath = newPoint;
        }

        [RelayCommand(CanExecute = nameof(CanDeletePath))]
        private void DeletePath()
        {
            List<ConeExtrudePoint> selectedPoints = pathDataGrid.SelectedItems.Cast<ConeExtrudePoint>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ConeExtrudePoint point in selectedPoints)
            {
                if (SelectedNode.ExtrudePaths.IndexOf(point) == 0) continue; //Cannot remove or edit first point

                undos.Add(new UndoableListRemove<ConeExtrudePoint>(SelectedNode.ExtrudePaths, point));
                SelectedNode.ExtrudePaths.Remove(point);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Path -> Delete Point");
        }

        [RelayCommand(CanExecute = nameof(IsPathSelected))]
        private void CopyPath()
        {
            List<ConeExtrudePoint> selectedPoints = pathDataGrid.SelectedItems.Cast<ConeExtrudePoint>().ToList();
            CustomClipboard.SetData(EMP_File.CLIPBOARD_CONE_EXTRUSION, selectedPoints);
        }

        [RelayCommand(CanExecute = nameof(CanPastePath))]
        private void PastePath()
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_CONE_EXTRUSION, out List<ConeExtrudePoint> points))
                return;

            if (points != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                SelectedNode.ExtrudePaths.AddRange(points);

                foreach (ConeExtrudePoint point in points)
                {
                    undos.Add(new UndoableListAdd<ConeExtrudePoint>(SelectedNode.ExtrudePaths, point));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Path -> Paste");
            }
        }

        private bool CanDeletePath()
        {
            if (SelectedPath == null || SelectedNode == null) return false;
            return SelectedNode.ExtrudePaths.IndexOf(SelectedPath) != 0;
        }
        
        private bool CanPastePath()
        {
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_CONE_EXTRUSION);
        }

        private bool IsPathSelected()
        {
            return SelectedPath != null;
        }
        #endregion

        private void etrDataGrid_PreviewDrop(object sender, DragEventArgs e)
        {
            const string key = "GongSolutions.Wpf.DragDrop";

            if (e.Data.GetDataPresent(key))
            {
                object data = e.Data.GetData(key);

                if (data is ETR_Node node)
                {
                    //Node is from another ETR or even EEPK. In this case, the dependencies need to be imported
                    if (!EtrFile.Nodes.Contains(node))
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();
                        AssetContainer.AddTbindDependencies(node, EtrFile, undos);
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Item Move", allowCompositeMerging: true));
                    }
                }
            }
        }

        private void EmpKeyframesView_IsEnabledToggled(object sender, EventArgs e)
        {
            SelectedNode.UpdatePreviewBrush();
        }

        private void SetSelectedNodes(IList<ETR_Node> nodes)
        {
            if (nodes.Count == 0) return;

            SelectedNode = nodes[0];
            SelectedNodes.Clear();

            foreach (var material in nodes)
            {
                SelectedNodes.Add(material);
            }

            etrDataGrid.ScrollIntoView(SelectedNode);
        }
    }
}
