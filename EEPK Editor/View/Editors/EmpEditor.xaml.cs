using CommunityToolkit.Mvvm.Input;
using LB_Common.Mvvm;
using LB_Common.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xv2CoreLib;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for EmpEditor.xaml
    /// </summary>
    public partial class EmpEditor : AutoObservableUserControl
    {
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

                if(!node.IsEmission && nodeView.tabControl.SelectedIndex == 1)
                {
                    nodeView.tabControl.SelectedIndex = 0;
                }
            }
        }

        #region NodeCommands
        [RelayCommand]
        private void NewNode(NewNodeType nodeType)
        {
            if (EmpFile == null) return;

            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            ParticleNode newNode = ParticleNode.GetNew(nodeType, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "New Node"));
        }

        [RelayCommand]
        private void NewNodeChild(NewNodeType nodeType)
        {
            if (SelectedNode == null) return;
            AsyncObservableCollection<ParticleNode> nodes = SelectedNode.ChildParticleNodes;
            ParticleNode newNode = ParticleNode.GetNew(nodeType, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "New Node"));
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DuplicateNode()
        {
            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            ParticleNode newNode = SelectedNode.Clone();
            newNode.Name = NameHelper.GetUniqueName(newNode.Name, nodes);
            nodes.Add(newNode);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ParticleNode>(nodes, newNode, "Duplicate Node"));
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DeleteNode()
        {
            AsyncObservableCollection<ParticleNode> nodes = EmpFile.GetParentList(SelectedNode);
            IUndoRedo undo = new UndoableListRemove<ParticleNode>(nodes, SelectedNode, "Delete Node");
            nodes.Remove(SelectedNode);

            UndoManager.Instance.AddUndo(undo);
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void CopyNode()
        {
            CustomClipboard.SetData(EMP_File.CLIPBOARD_NODE, SelectedNode);
        }

        [RelayCommand(CanExecute = nameof(CanPasteNode))]
        private void PasteNode()
        {
            PasteNode(EmpFile.GetParentList(SelectedNode));
        }

        [RelayCommand(CanExecute = nameof(CanPasteNodeChild))]
        private void PasteNodeChild()
        {
            PasteNode(SelectedNode.ChildParticleNodes);
        }

        [RelayCommand(CanExecute = nameof(CanPasteNodeChild))]
        private void PasteNodeValues()
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_NODE, out ParticleNode copiedNode))
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            AssetContainer.AddPbindDependencies(copiedNode, EmpFile, undos);
            SelectedNode.CopyValues(copiedNode, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Node Values");
            UndoManager.Instance.ForceEventCall();
        }

        private void PasteNode(AsyncObservableCollection<ParticleNode> nodes)
        {
            if (!CustomClipboard.TryGetData(EMP_File.CLIPBOARD_NODE, out ParticleNode copiedNode))
                return;

            copiedNode.Name = NameHelper.GetUniqueName(copiedNode.Name, nodes);

            List<IUndoRedo> undos = new List<IUndoRedo>();
            AssetContainer.AddPbindDependencies(copiedNode, EmpFile, undos);

            nodes.Add(copiedNode);
            undos.Add(new UndoableListAdd<ParticleNode>(nodes, copiedNode));
            UndoManager.Instance.AddCompositeUndo(undos, "Paste Node");
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueAdjustment()
        {
            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNode, false);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueSet()
        {
            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNode, true);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsEmpFileLoaded))]
        private void HideAll()
        {
            foreach (var particleEffect in EmpFile.ParticleNodes)
            {
                SetHideStatus(particleEffect, true);
            }
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HideAllSelected()
        {
            SetHideStatus(SelectedNode, true);
        }

        [RelayCommand(CanExecute = nameof(IsEmpFileLoaded))]
        private void ShowAll()
        {
            foreach (var particleEffect in EmpFile.ParticleNodes)
            {
                SetHideStatus(particleEffect, false);
            }
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
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
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_NODE);
        }
        
        private bool CanPasteNodeChild()
        {
            return CustomClipboard.ContainsData(EMP_File.CLIPBOARD_NODE) && SelectedNode != null;
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
    
        public void SelectTexture(EMP_TextureSamplerDef texture)
        {
            tabItems.SelectedIndex = 1;
            textureView.SelectTexture(texture);
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                nodeView.tabControl.SelectedIndex = 1;
            }
        }

        private void EmpKeyframesView_IsEnabledToggled(object sender, EventArgs e)
        {
            SelectedNode.UpdatePreviewBrush(false);
        }
    }
}
