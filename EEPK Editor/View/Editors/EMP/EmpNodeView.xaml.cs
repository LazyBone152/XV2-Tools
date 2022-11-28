using EEPK_Organiser.Forms;
using EEPK_Organiser.View.Controls;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for EmpNodeView.xaml
    /// </summary>
    public partial class EmpNodeView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(nameof(Node), typeof(ParticleNode), typeof(EmpNodeView), new PropertyMetadata(OnDpChanged));

        public ParticleNode Node
        {
            get { return (ParticleNode)GetValue(NodeProperty); }
            set
            {
                SetValue(NodeProperty, value);
            }
        }
        public EmpNodeViewModel NodeViewModel
        {
            get
            {
                if (Node != null) return _nodeViewModel;
                return null;
            }
        }
        private EmpNodeViewModel _nodeViewModel = new EmpNodeViewModel();


        public static readonly DependencyProperty EmpFileProperty = DependencyProperty.Register(nameof(EmpFile), typeof(EMP_File), typeof(EmpNodeView), new PropertyMetadata(null));

        public EMP_File EmpFile
        {
            get => (EMP_File)GetValue(EmpFileProperty);
            set => SetValue(EmpFileProperty, value);
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(EmpNodeView), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get => (AssetContainerTool)GetValue(AssetContainerProperty);
            set => SetValue(AssetContainerProperty, value);
        }


        private static DependencyPropertyChangedEventHandler DpChanged;

        private static void OnDpChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (DpChanged != null)
                DpChanged.Invoke(sender, e);
        }

        private void NodeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Node != null)
                _nodeViewModel.SetContext(Node);

            NotifyPropertyChanged(nameof(Node));
            NotifyPropertyChanged(nameof(NodeViewModel));
            UpdateVisibilities();
            //UpdateProperties();
        }
        #endregion

        #region Visibilities
        //Node:
        public Visibility EmitterVisibility => Node?.NodeType == ParticleNodeType.Emitter ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionVisibility => Node?.NodeType == ParticleNodeType.Emission ? Visibility.Visible : Visibility.Collapsed;

        //Emitter:
        public Visibility EmitterFromAreaVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterPositionVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Point) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterSizeVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Sphere) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterSize2Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterAngleVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Point) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterF1Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Point) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterF2Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Point) ? Visibility.Visible : Visibility.Collapsed;

        //Emission
        public Visibility BillboardVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Plane, ParticleEmission.ParticleEmissionType.ShapeDraw) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionRotationValuesVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Mesh, ParticleEmission.ParticleEmissionType.ShapeDraw) || (IsEmissionType(ParticleEmission.ParticleEmissionType.Plane) && Node?.EmissionNode?.VisibleOnlyOnMotion == false) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionDefaultVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Plane) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionAutoRotationVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Plane) && Node?.EmissionNode?.BillboardEnabled == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionRotAxisVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Plane) && Node?.EmissionNode?.BillboardEnabled == false ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionShapeDrawVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.ShapeDraw) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionMeshVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.Mesh) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmissionConeExtrudeVisibility => IsEmissionType(ParticleEmission.ParticleEmissionType.ConeExtrude) ? Visibility.Visible : Visibility.Collapsed;
        
        public bool RandomUpVectorEnabled => IsNodeType(NodeSpecificType.Mesh, NodeSpecificType.Default) ? true : false;
        public bool RandomDirEnabled => IsNodeType(NodeSpecificType.AutoOriented, NodeSpecificType.Default, NodeSpecificType.ShapeDraw, NodeSpecificType.Mesh, NodeSpecificType.ShapeAreaDistribution, NodeSpecificType.ShapePerimeterDistribution, NodeSpecificType.VerticalDistribution) ? true : false;
        public bool EmissionRotEnabled => !(Node?.EmissionNode?.BillboardEnabled == true && Node?.EmissionNode?.VisibleOnlyOnMotion == true);
        public bool IsEmitterOrNull => Node != null ? (Node.IsEmitter || Node.IsNull) : false;

        private bool IsEmitterShape(params ParticleEmitter.ParticleEmitterShape[] shapes)
        {
            if (Node == null) return false;

            foreach (var shape in shapes)
                if (Node.EmitterNode.Shape == shape) return true;

            return false;
        }

        private bool IsEmissionType(params ParticleEmission.ParticleEmissionType[] types)
        {
            if (Node == null) return false;

            foreach (var type in types)
                if (Node.EmissionNode.EmissionType == type) return true;

            return false;
        }

        private bool IsNodeType(params NodeSpecificType[] types)
        {
            if (Node == null) return false;

            foreach (var type in types)
                if (Node.NodeSpecificType == type) return true;

            return false;
        }
        #endregion

        public string VarianceToolTip => "Adds a random amount onto the original value, between 0 and the amount here. Each new node instance will have its own random value.";

        public EmpNodeView()
        {
            DataContext = this;
            InitializeComponent();
            DpChanged += NodeChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            _nodeViewModel.PropertyChanged += NodeViewModel_PropertyChanged;
            SelectedShapeDrawPoint.PropertyChanged += SelectedShapeDrawPoint_PropertyChanged;
        }

        private void SelectedShapeDrawPoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(shapeDrawPointDataGrid != null && SelectedShapeDrawPoint.Point != null)
            {
                shapeDrawPointDataGrid.ScrollIntoView(SelectedShapeDrawPoint.Point);
            }
        }

        private void NodeViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NodeViewModel.Shape) || e.PropertyName == nameof(NodeViewModel.NodeType) || e.PropertyName == nameof(NodeViewModel.EmissionType) || e.PropertyName == nameof(NodeViewModel.BillboardType) || e.PropertyName == nameof(NodeViewModel.VisibleOnlyOnMotion))
            {
                UpdateVisibilities();
            }
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            UpdateVisibilities();
            NodeViewModel?.UpdateProperties();
        }

        private void UpdateVisibilities()
        {
            NotifyPropertyChanged(nameof(EmitterVisibility));
            NotifyPropertyChanged(nameof(EmissionVisibility));
            NotifyPropertyChanged(nameof(BillboardVisibility));

            NotifyPropertyChanged(nameof(EmitterFromAreaVisibility));
            NotifyPropertyChanged(nameof(EmitterPositionVisibility));
            NotifyPropertyChanged(nameof(EmitterSizeVisibility));
            NotifyPropertyChanged(nameof(EmitterSize2Visibility));
            NotifyPropertyChanged(nameof(EmitterAngleVisibility));
            NotifyPropertyChanged(nameof(EmitterF1Visibility));
            NotifyPropertyChanged(nameof(EmitterF2Visibility));

            NotifyPropertyChanged(nameof(EmissionDefaultVisibility));
            NotifyPropertyChanged(nameof(EmissionAutoRotationVisibility));
            NotifyPropertyChanged(nameof(EmissionRotAxisVisibility));
            NotifyPropertyChanged(nameof(EmissionShapeDrawVisibility));
            NotifyPropertyChanged(nameof(EmissionMeshVisibility));
            NotifyPropertyChanged(nameof(EmissionConeExtrudeVisibility));

            NotifyPropertyChanged(nameof(EmissionRotEnabled));
            NotifyPropertyChanged(nameof(IsEmitterOrNull));
            NotifyPropertyChanged(nameof(EmissionRotationValuesVisibility));

            NotifyPropertyChanged(nameof(RandomUpVectorEnabled));
            NotifyPropertyChanged(nameof(RandomDirEnabled));
        }

        #region Texture
        public RelayCommand TexturePart_RemoveMaterialCommand => new RelayCommand(Material_RemoveMaterialReference, HasMaterial);
        private void Material_RemoveMaterialReference()
        {
            UndoManager.Instance.AddUndo(new UndoableProperty<ParticleTexture>(nameof(ParticleTexture.MaterialRef), Node.EmissionNode.Texture, Node.EmissionNode.Texture.MaterialRef, null, "Remove Material"));
            Node.EmissionNode.Texture.MaterialRef = null;
        }

        public RelayCommand TexturePart_GotoMaterialCommand => new RelayCommand(Material_Goto, HasMaterial);
        private void Material_Goto()
        {
            MaterialsEditorForm emmForm = EepkEditor.PBIND_OpenMaterialEditor(AssetContainer, Xv2CoreLib.EEPK.AssetType.PBIND);
            emmForm.materialsEditor.SelectedMaterial = Node.EmissionNode.Texture.MaterialRef;
            emmForm.materialsEditor.materialDataGrid.ScrollIntoView(Node.EmissionNode.Texture.MaterialRef);
        }

        public RelayCommand<int> TexturePart_RemoveTextureCommand => new RelayCommand<int>(TexturePart_RemoveTexture);
        private void TexturePart_RemoveTexture(int textureIndex)
        {
            if (!HasTextureRef(textureIndex)) return;

            TextureEntry_Ref textureRef = Node.EmissionNode.Texture.TextureEntryRef[textureIndex];
            textureRef.UndoableTextureRef = null;
        }

        public RelayCommand<int> TexturePart_GotoTextureCommand => new RelayCommand<int>(TexturePart_GotoTexture);
        private void TexturePart_GotoTexture(int textureIndex)
        {
            if (!HasTextureRef(textureIndex)) return;

            TextureEntry_Ref textureRef = Node.EmissionNode.Texture.TextureEntryRef[textureIndex];

            if (textureRef.TextureRef != null)
            {
                EmpEditor empEditor = ((Grid)VisualTreeHelper.GetParent(this)).DataContext as EmpEditor;
                empEditor.SelectTexture(textureRef.TextureRef);
            }
        }


        private bool HasMaterial()
        {
            return IsNodeSelected() ? Node.EmissionNode.Texture.MaterialRef != null : false;
        }

        private bool HasTextureRef(int texIndex)
        {
            return IsNodeSelected() ? Node.EmissionNode.Texture.TextureEntryRef[texIndex].TextureRef != null : false;
        }

        private bool IsNodeSelected()
        {
            return Node != null;
        }
        #endregion

        #region ShapeDraw
        public ShapePointRef SelectedShapeDrawPoint { get; set; } = new ShapePointRef();

        public RelayCommand NewShapeDrawPointCommand => new RelayCommand(NewShapeDrawPoint, IsNodeSelected);
        private void NewShapeDrawPoint()
        {
            ShapeDrawPoint newPoint = new ShapeDrawPoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ShapeDrawPoint>(Node.EmissionNode.ShapeDraw.Points, newPoint, "Shape Draw -> Add Point"));
            Node.EmissionNode.ShapeDraw.Points.Add(newPoint);

            SelectedShapeDrawPoint.Point = newPoint;
        }

        public RelayCommand DeleteShapeDrawPointCommand => new RelayCommand(DeleteShapeDrawPoint, IsPointSelected);
        private void DeleteShapeDrawPoint()
        {
            List<ShapeDrawPoint> selectedPoints = shapeDrawPointDataGrid.SelectedItems.Cast<ShapeDrawPoint>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var point in selectedPoints)
            {
                if (Node.EmissionNode.ShapeDraw.Points.Count <= 2) break;

                undos.Add(new UndoableListRemove<ShapeDrawPoint>(Node.EmissionNode.ShapeDraw.Points, point, "Shape Draw -> Delete Point"));
                Node.EmissionNode.ShapeDraw.Points.Remove(point);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Shape Draw -> Delete Point");
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

            if(points != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                Node.EmissionNode.ShapeDraw.Points.AddRange(points);

                foreach (var point in points)
                {
                    undos.Add(new UndoableListAdd<ShapeDrawPoint>(Node.EmissionNode.ShapeDraw.Points, point));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Shape Draw -> Paste");
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

                foreach(var point in selectedPoints)
                {
                    point.PasteValues(points[0], undos);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Shape Draw -> Paste Values");
            }
            else
            {
                MessageBox.Show("\"Paste Values\" only works with an equal amount of copied and selected points.", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsPointSelected()
        {
            if (!IsNodeSelected()) return false;
            return SelectedShapeDrawPoint?.Point != null;
        }

        #endregion

        #region Mesh
        public RelayCommand Mesh_ImportMeshCommand => new RelayCommand(Mesh_ImportMesh, IsNodeSelected);
        private void Mesh_ImportMesh()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Import Mesh...";
            openFile.Filter = string.Format("XV2 Mesh File | *.emd; *.emg");
            //openFile.Filter = string.Format("XV2 Mesh File | *.emd");

            if (openFile.ShowDialog() == true)
            {
                EMG_File emgFile;

                if (Path.GetExtension(openFile.FileName) == ".emd")
                {
                    emgFile = EMG_File.ConvertToEmg(Xv2CoreLib.EMD.EMD_File.Load(openFile.FileName));
                }
                else if (Path.GetExtension(openFile.FileName) == ".emg")
                {
                    emgFile = EMG_File.Load(openFile.FileName);
                }
                else
                {
                    throw new InvalidDataException("Mesh_ImportMesh: File must either be an EMD or EMG.");
                }

                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(ParticleStaticMesh.EmgFile), Node.EmissionNode.Mesh, Node.EmissionNode.Mesh.EmgFile, emgFile, "Import Static Mesh"));
                Node.EmissionNode.Mesh.EmgFile = emgFile;
                NodeViewModel?.UpdateProperties();
            }
        }

        public RelayCommand Mesh_ExportMeshCommand => new RelayCommand(Mesh_ExportMesh, () => Node?.EmissionNode?.Mesh?.EmgFile != null);
        private void Mesh_ExportMesh()
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Title = "Export Mesh...";
            saveFile.Filter = string.Format("EMD | *.emd");

            if (saveFile.ShowDialog() == true)
            {
                if (Path.GetExtension(saveFile.FileName) == ".emd")
                {
                    Xv2CoreLib.EMD.EMD_File emdFile = Node.EmissionNode.Mesh.EmgFile.ConvertToEmd();
                    emdFile.Save(saveFile.FileName);
                }
            }
        }

        public RelayCommand Mesh_CopyMeshCommand => new RelayCommand(Mesh_CopyMesh, () => Node?.EmissionNode?.Mesh?.EmgFile != null);
        private void Mesh_CopyMesh()
        {
            Clipboard.SetData(EMG_File.CLIPBOARD_ID, Node.EmissionNode.Mesh.EmgFile);
        }

        public RelayCommand Mesh_PasteMeshCommand => new RelayCommand(Mesh_PasteMesh, () => Clipboard.ContainsData(EMG_File.CLIPBOARD_ID));
        private void Mesh_PasteMesh()
        {
            EMG_File emg = (EMG_File)Clipboard.GetData(EMG_File.CLIPBOARD_ID);

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(ParticleStaticMesh.EmgFile), Node.EmissionNode.Mesh, Node.EmissionNode.Mesh.EmgFile, emg, "Paste Static Mesh"));
            Node.EmissionNode.Mesh.EmgFile = emg;
            NodeViewModel?.UpdateProperties();
        }
        #endregion

        #region ConeExtrude
        private ConeExtrudePoint _selectedExtrudePoint = null;
        public ConeExtrudePoint SelectedExtrudePoint
        {
            get => _selectedExtrudePoint;
            set
            {
                if (_selectedExtrudePoint != value)
                {
                    _selectedExtrudePoint = value;
                    NotifyPropertyChanged(nameof(SelectedExtrudePoint));
                }
            }
        }

        public RelayCommand NewExtrudePointCommand => new RelayCommand(NewExtrudePoint, IsNodeSelected);
        private void NewExtrudePoint()
        {
            ConeExtrudePoint newPoint = new ConeExtrudePoint();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ConeExtrudePoint>(Node.EmissionNode.ConeExtrude.Points, newPoint, "Cone Extrude -> Add Point"));
            Node.EmissionNode.ConeExtrude.Points.Add(newPoint);

            SelectedExtrudePoint = newPoint;
        }

        public RelayCommand DeleteExtrudePointCommand => new RelayCommand(DeleteExtrudePoint, CanDeleteExtrudePoint);
        private void DeleteExtrudePoint()
        {
            List<ConeExtrudePoint> selectedPoints = extrudeDataGrid.SelectedItems.Cast<ConeExtrudePoint>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ConeExtrudePoint point in selectedPoints)
            {
                if (Node.EmissionNode.ConeExtrude.Points.IndexOf(point) == 0) continue; //Cannot remove or edit first point

                undos.Add(new UndoableListRemove<ConeExtrudePoint>(Node.EmissionNode.ConeExtrude.Points, point));
                Node.EmissionNode.ConeExtrude.Points.Remove(point);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Cone Extrude -> Delete Point");
        }

        public RelayCommand CopyExtrudePointCommand => new RelayCommand(CopyExtrudePoint, () => SelectedExtrudePoint != null);
        private void CopyExtrudePoint()
        {
            List<ConeExtrudePoint> selectedPoints = extrudeDataGrid.SelectedItems.Cast<ConeExtrudePoint>().ToList();
            Clipboard.SetData(EMP_File.CLIPBOARD_CONE_EXTRUSION, selectedPoints);
        }


        public RelayCommand PasteExtrudePointCommand => new RelayCommand(PasteExtrudePoint, () => Clipboard.ContainsData(EMP_File.CLIPBOARD_CONE_EXTRUSION));
        private void PasteExtrudePoint()
        {
            List<ConeExtrudePoint> points = (List<ConeExtrudePoint>)Clipboard.GetData(EMP_File.CLIPBOARD_CONE_EXTRUSION);

            if (points != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                Node.EmissionNode.ConeExtrude.Points.AddRange(points);

                foreach (ConeExtrudePoint point in points)
                {
                    undos.Add(new UndoableListAdd<ConeExtrudePoint>(Node.EmissionNode.ConeExtrude.Points, point));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Cone Extrude -> Paste");
            }
        }

        private bool CanDeleteExtrudePoint()
        {
            if (SelectedExtrudePoint == null || Node == null) return false;
            return Node.EmissionNode.ConeExtrude.Points.IndexOf(SelectedExtrudePoint) != 0;
        }
        #endregion
    }
}
