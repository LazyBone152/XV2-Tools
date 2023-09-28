using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.ETR;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;
using EEPK_Organiser.Forms;
using Xv2CoreLib.EMP_NEW;
using EEPK_Organiser.View.Controls;
using EEPK_Organiser.Forms.Recolor;

namespace EEPK_Organiser.View.Editors
{
    /// <summary>
    /// Interaction logic for EtrEditor.xaml
    /// </summary>
    public partial class EtrEditor : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

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
                if (e.UndoContext is ETR_Node node)
                {
                    node.UpdatePreviewBrush();
                    return;
                }
                else if (e.UndoContext is Asset asset)
                {
                    if (asset.assetType == Xv2CoreLib.EEPK.AssetType.TBIND)
                    {
                        if (asset.Files[0].EtrFile == EtrFile)
                        {
                            EtrFile.UpdateBrushPreview();
                        }
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
        private List<ETR_Node> SelectedNodes => etrDataGrid.SelectedItems.Cast<ETR_Node>().ToList();

        public RelayCommand AddNodeCommand => new RelayCommand(AddNode, IsETRLoaded);
        private void AddNode()
        {
            ETR_Node node = ETR_Node.GetNew();
            EtrFile.Nodes.Add(node);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, node, "ETR -> Add Node"));
        }

        public RelayCommand DeleteNodeCommand => new RelayCommand(DeleteNode, IsNodeSelected);
        private void DeleteNode()
        {
            List<ETR_Node> nodes = SelectedNodes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ETR_Node node in nodes)
            {
                undos.Add(new UndoableListRemove<ETR_Node>(EtrFile.Nodes, node));
                EtrFile.Nodes.Remove(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Delete Node");
        }

        public RelayCommand CopyNodeCommand => new RelayCommand(CopyNode, IsNodeSelected);
        private void CopyNode()
        {
            Clipboard.SetData(ETR_Node.CLIPBOARD_ID, SelectedNodes);
        }

        public RelayCommand PasteNodeCommand => new RelayCommand(PasteNode, () => Clipboard.ContainsData(ETR_Node.CLIPBOARD_ID) && IsETRLoaded());
        private void PasteNode()
        {
            List<ETR_Node> nodes = (List<ETR_Node>)Clipboard.GetData(ETR_Node.CLIPBOARD_ID);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ETR_Node node in nodes)
            {
                undos.Add(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, node));
                EtrFile.Nodes.Add(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Paste Node");
        }

        public RelayCommand DuplicateNodeCommand => new RelayCommand(DuplicateNode, IsNodeSelected);
        private void DuplicateNode()
        {
            List<ETR_Node> nodes = SelectedNodes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ETR_Node node in nodes)
            {
                ETR_Node newNode = node.Copy();
                undos.Add(new UndoableListAdd<ETR_Node>(EtrFile.Nodes, newNode));
                EtrFile.Nodes.Add(newNode);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ETR -> Duplicate Node");
        }

        public RelayCommand HueAdjustment_Command => new RelayCommand(HueAdjustment, IsNodeSelected);
        private void HueAdjustment()
        {
            Window editorWindow = ((Grid)System.Windows.Media.VisualTreeHelper.GetParent(this)).DataContext as Window;

            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNode, editorWindow);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        public RelayCommand HueSet_Command => new RelayCommand(HueSet, IsNodeSelected);
        private void HueSet()
        {
            Window editorWindow = ((Grid)System.Windows.Media.VisualTreeHelper.GetParent(this)).DataContext as Window;

            RecolorAll_HueSet recolor = new RecolorAll_HueSet(SelectedNode, editorWindow);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        private bool IsETRLoaded()
        {
            return EtrFile != null;
        }

        private bool IsNodeSelected()
        {
            return SelectedNode != null;
        }
        #endregion

        #region NodeCommands

        public RelayCommand Node_RemoveMaterialCommand => new RelayCommand(Material_RemoveMaterialReference, HasMaterial);
        private void Material_RemoveMaterialReference()
        {
            UndoManager.Instance.AddUndo(new UndoableProperty<ETR_Node>(nameof(ETR_Node.MaterialRef), SelectedNode, SelectedNode.MaterialRef, null, "Remove Material"));
            SelectedNode.MaterialRef = null;
        }

        public RelayCommand Node_GotoMaterialCommand => new RelayCommand(Material_Goto, HasMaterial);
        private void Material_Goto()
        {
            MaterialsEditorForm emmForm = EepkEditor.PBIND_OpenMaterialEditor(AssetContainer, Xv2CoreLib.EEPK.AssetType.TBIND);
            emmForm.materialsEditor.SelectedMaterial = SelectedNode.MaterialRef;
            emmForm.materialsEditor.materialDataGrid.ScrollIntoView(SelectedNode.MaterialRef);
        }

        public RelayCommand<int> Node_RemoveTextureCommand => new RelayCommand<int>(TexturePart_RemoveTexture);
        private void TexturePart_RemoveTexture(int textureIndex)
        {
            if (!HasTextureRef(textureIndex)) return;

            TextureEntry_Ref textureRef = SelectedNode.TextureEntryRef[textureIndex];
            textureRef.UndoableTextureRef = null;
        }

        public RelayCommand<int> Node_GotoTextureCommand => new RelayCommand<int>(TexturePart_GotoTexture);
        private void TexturePart_GotoTexture(int textureIndex)
        {
            if (!HasTextureRef(textureIndex)) return;

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

        public RelayCommand NewShapeDrawPointCommand => new RelayCommand(NewShapeDrawPoint, IsNodeSelected);
        private void NewShapeDrawPoint()
        {
            ShapeDrawPoint newPoint = new ShapeDrawPoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ShapeDrawPoint>(SelectedNode.ExtrudeShapePoints, newPoint, "Shape -> Add Point"));
            SelectedNode.ExtrudeShapePoints.Add(newPoint);

            SelectedShapePoint.Point = newPoint;
        }

        public RelayCommand DeleteShapeDrawPointCommand => new RelayCommand(DeleteShapeDrawPoint, IsPointSelected);
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

        public RelayCommand CopyShapeDrawPointCommand => new RelayCommand(CopyShapeDrawPoint, IsPointSelected);
        private void CopyShapeDrawPoint()
        {
            List<ShapeDrawPoint> selectedPoints = shapeDrawPointDataGrid.SelectedItems.Cast<ShapeDrawPoint>().ToList();
            Clipboard.SetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT, selectedPoints);
        }

        public RelayCommand PasteShapeDrawPointCommand => new RelayCommand(PasteShapeDrawPoint, () => Clipboard.ContainsData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT) && IsNodeSelected());
        private void PasteShapeDrawPoint()
        {
            List<ShapeDrawPoint> points = (List<ShapeDrawPoint>)Clipboard.GetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT);

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

        public RelayCommand PasteShapeDrawPointValuesCommand => new RelayCommand(PasteShapeDrawPointValues, () => Clipboard.ContainsData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT) && IsPointSelected());
        private void PasteShapeDrawPointValues()
        {
            List<ShapeDrawPoint> points = (List<ShapeDrawPoint>)Clipboard.GetData(EMP_File.CLIPBOARD_SHAP_DRAW_POINT);
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
                MessageBox.Show("\"Paste Values\" only works with an equal amount of copied and selected points.", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        public RelayCommand NewPathCommand => new RelayCommand(NewPath, IsNodeSelected);
        private void NewPath()
        {
            ConeExtrudePoint newPoint = new ConeExtrudePoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ConeExtrudePoint>(SelectedNode.ExtrudePaths, newPoint, "Path -> Add Path"));
            SelectedNode.ExtrudePaths.Add(newPoint);

            SelectedPath = newPoint;
        }

        public RelayCommand DeletePathCommand => new RelayCommand(DeletePath, CanDeletePath);
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

        public RelayCommand CopyPathCommand => new RelayCommand(CopyPath, () => SelectedPath != null);
        private void CopyPath()
        {
            List<ConeExtrudePoint> selectedPoints = pathDataGrid.SelectedItems.Cast<ConeExtrudePoint>().ToList();
            Clipboard.SetData(EMP_File.CLIPBOARD_CONE_EXTRUSION, selectedPoints);
        }


        public RelayCommand PastePathCommand => new RelayCommand(PastePath, () => Clipboard.ContainsData(EMP_File.CLIPBOARD_CONE_EXTRUSION));
        private void PastePath()
        {
            List<ConeExtrudePoint> points = (List<ConeExtrudePoint>)Clipboard.GetData(EMP_File.CLIPBOARD_CONE_EXTRUSION);

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
    }
}
