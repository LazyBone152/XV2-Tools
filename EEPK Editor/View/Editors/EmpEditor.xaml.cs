using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EffectContainer;
using System.Windows.Input;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.Resource;
using EEPK_Organiser.Forms.Recolor;
using Xv2CoreLib;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for EmpEditor.xaml
    /// </summary>
    public partial class EmpEditor : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty EmpFileProperty = DependencyProperty.Register(nameof(EmpFile), typeof(EMP_File), typeof(EmpEditor), new PropertyMetadata(null));

        public EMP_File EmpFile
        {
            get { return (EMP_File)GetValue(EmpFileProperty); }
            set
            {
                SetValue(EmpFileProperty, value);
                NotifyPropertyChanged(nameof(EmpFile));
            }
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(EmpEditor), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get => (AssetContainerTool)GetValue(AssetContainerProperty);
            set => SetValue(AssetContainerProperty, value);
        }

        #endregion

        private ParticleNode _prevNode = null;
        private ParticleNode _selectedNode = null;
        public ParticleNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                if(value != _selectedNode)
                {
                    _selectedNode = value;
                    NotifyPropertyChanged(nameof(SelectedNode));
                    HandleNodeEvents();
                }
            }
        }

        public EmpEditor()
        {
            DataContext = this;
            InitializeComponent();
            Unloaded += EmpEditor_Unloaded;
        }

        private void EmpEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            //Remove any lingering event references when the EMP editor is closed
            if(_prevNode != null)
                _prevNode.PropertyChanged -= Node_PropertyChanged;

            if (SelectedNode != null)
                SelectedNode.PropertyChanged -= Node_PropertyChanged;
        }

        private void HandleNodeEvents()
        {
            if (_prevNode != null)
            {
                _prevNode.PropertyChanged -= Node_PropertyChanged;
            }

            if (SelectedNode != null)
            {
                SelectedNode.PropertyChanged += Node_PropertyChanged;
            }

            _prevNode = SelectedNode;
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //remove if not needed for anything else
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(e.NewValue is ParticleNode node)
            {
                SelectedNode = node;
            }
        }

        #region NodeCommands
        public RelayCommand<int> NewNodeCommand => new RelayCommand<int>(NewNode);
        private void NewNode(int nodeType)
        {
            if (EmpFile == null) return;

            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            ParticleNode newNode = ParticleNode.GetNew((NewNodeType)nodeType, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "New Node"));
        }

        public RelayCommand<int> NewNodeChildCommand => new RelayCommand<int>(NewNodeChild);
        private void NewNodeChild(int nodeType)
        {
            if (SelectedNode == null) return;
            AsyncObservableCollection<ParticleNode> nodes = SelectedNode.ChildParticleNodes;
            ParticleNode newNode = ParticleNode.GetNew((NewNodeType)nodeType, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "New Node"));
        }

        public RelayCommand DuplicateNodeCommand => new RelayCommand(DuplicateNode, IsNodeSelected);
        private void DuplicateNode()
        {
            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            ParticleNode newNode = SelectedNode.Clone();
            newNode.Name = NameHelper.GetUniqueName(newNode.Name, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "Duplicate Node"));
        }

        public RelayCommand DeleteNodeCommand => new RelayCommand(DeleteNode, IsNodeSelected);
        private void DeleteNode()
        {
            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            IUndoRedo undo = new UndoableListRemove<ParticleNode>(nodes, SelectedNode, "Delete Node");
            nodes.Remove(SelectedNode);

            UndoManager.Instance.AddUndo(undo);
        }

        public RelayCommand CopyNodeCommand => new RelayCommand(CopyNode, IsNodeSelected);
        private void CopyNode()
        {
            Clipboard.SetData(EMP_File.CLIPBOARD_NODE, SelectedNode);
        }

        public RelayCommand PasteNodeCommand => new RelayCommand(PasteNode, CanPasteNode);
        private void PasteNode()
        {
            PasteNode(EmpFile.GetParentList(SelectedNode));
        }

        public RelayCommand PasteNodeChildCommand => new RelayCommand(PasteNodeChild, CanPasteNodeChild);
        private void PasteNodeChild()
        {
            PasteNode(SelectedNode.ChildParticleNodes);
        }

        public RelayCommand PasteNodeValuesCommand => new RelayCommand(PasteNodeValues, CanPasteNodeChild);
        private void PasteNodeValues()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            ParticleNode copiedNode = (ParticleNode)Clipboard.GetData(EMP_File.CLIPBOARD_NODE);
            AssetContainer.AddPbindDependencies(copiedNode, EmpFile, undos);
            SelectedNode.CopyValues(copiedNode, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Node Values");
            UndoManager.Instance.ForceEventCall();
        }

        private void PasteNode(AsyncObservableCollection<ParticleNode> nodes)
        {
            ParticleNode copiedNode = (ParticleNode)Clipboard.GetData(EMP_File.CLIPBOARD_NODE);
            copiedNode.Name = NameHelper.GetUniqueName(copiedNode.Name, nodes);

            List<IUndoRedo> undos = new List<IUndoRedo>();
            AssetContainer.AddPbindDependencies(copiedNode, EmpFile, undos);

            nodes.Add(copiedNode);
            undos.Add(new UndoableListAdd<ParticleNode>(nodes, copiedNode));
            UndoManager.Instance.AddCompositeUndo(undos, "Paste Node");
        }

        public RelayCommand HueAdjustment_Command => new RelayCommand(HueAdjustment, IsNodeSelected);
        private void HueAdjustment()
        {
            Window empEditor = ((Grid)System.Windows.Media.VisualTreeHelper.GetParent(this)).DataContext as Window;

            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNode, empEditor);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        public RelayCommand HueSet_Command => new RelayCommand(HueSet, IsNodeSelected);
        private void HueSet()
        {
            Window empEditor = ((Grid)System.Windows.Media.VisualTreeHelper.GetParent(this)).DataContext as Window;

            RecolorAll_HueSet recolor = new RecolorAll_HueSet(SelectedNode, empEditor);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        public RelayCommand HideAll_Command => new RelayCommand(HideAll, IsEmpFileLoaded);
        private void HideAll()
        {
            foreach (var particleEffect in EmpFile.ParticleNodes)
            {
                SetHideStatus(particleEffect, true);
            }
        }

        public RelayCommand HideAllSelected_Command => new RelayCommand(HideAllSelected, IsNodeSelected);
        private void HideAllSelected()
        {
            SetHideStatus(SelectedNode, true);
        }

        public RelayCommand ShowAll_Command => new RelayCommand(ShowAll, IsEmpFileLoaded);
        private void ShowAll()
        {
            foreach (var particleEffect in EmpFile.ParticleNodes)
            {
                SetHideStatus(particleEffect, false);
            }
        }

        public RelayCommand ShowAllSelected_Command => new RelayCommand(ShowAllSelected, IsNodeSelected);
        private void ShowAllSelected()
        {
            SetHideStatus(SelectedNode, false);
        }

        private void SetHideStatus(ParticleNode node, bool hideStatus)
        {
            node.NodeFlags = node.NodeFlags.SetFlag(NodeFlags1.Hide, hideStatus);

            foreach (ParticleNode child in node.ChildParticleNodes)
            {
                SetHideStatus(child, hideStatus);
            }
        }



        private bool IsEmpFileLoaded(int i)
        {
            return EmpFile != null;
        }
        private bool IsEmpFileLoaded()
        {
            return EmpFile != null;
        }

        private bool IsNodeSelected()
        {
            return SelectedNode != null;
        }

        private bool CanAddChildNode(int i)
        {
            return SelectedNode != null && EmpFile != null;
        }

        private bool CanPasteNode()
        {
            return Clipboard.ContainsData(EMP_File.CLIPBOARD_NODE);
        }
        private bool CanPasteNodeChild()
        {
            return Clipboard.ContainsData(EMP_File.CLIPBOARD_NODE) && SelectedNode != null;
        }
        #endregion

        private void TreeView_PreviewDrop(object sender, DragEventArgs e)
        {
            const string key = "GongSolutions.Wpf.DragDrop";

            if (e.Data.GetDataPresent(key))
            {
                object data = e.Data.GetData(key);

                if(data is ParticleNode node)
                {
                    //Node is from another EMP or even EEPK. In this case, the dependencies need to be imported
                    if (!EmpFile.NodeExists(node))
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();
                        AssetContainer.AddPbindDependencies(node, EmpFile, undos);
                        UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Item Move", allowCompositeMerging: true));
                    }
                }
            }

        }
    }
}
