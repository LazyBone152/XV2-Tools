using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LB_Common.Mvvm;
using LB_Common.Utils;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.ViewModel;

namespace EEPK_Organiser.View.Editors
{
    /// <summary>
    /// Interaction logic for EcfEditor.xaml
    /// </summary>
    public partial class EcfEditor : AutoObservableUserControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty EcfFileProperty = DependencyProperty.Register(nameof(EcfFile), typeof(ECF_File), typeof(EcfEditor), new PropertyMetadata(null));

        public ECF_File EcfFile
        {
            get { return (ECF_File)GetValue(EcfFileProperty); }
            set
            {
                SetValue(EcfFileProperty, value);
                NotifyPropertyChanged(nameof(EcfFile));
            }
        }
        #endregion

        private ECF_Node _selectedNode = null;
        public ECF_Node SelectedNode
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
        public ObservableCollection<ECF_Node> SelectedNodes { get; } = new ObservableCollection<ECF_Node>();

        private EcfNodeViewModel _viewModel = new EcfNodeViewModel();
        public EcfNodeViewModel ViewModel => SelectedNode != null ? _viewModel : null;

        public bool IsNodeEnabled => SelectedNode != null;

        public EcfEditor()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Unloaded += EcfEditor_Unloaded;
            Loaded += EcfEditor_Loaded;
        }

        private void EcfEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (EcfFile?.Nodes?.Count > 0)
                SelectedNode = EcfFile.Nodes[0];
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
                if (e.UndoContext is ECF_Node node)
                {
                    node.UpdatePreviewBrush();
                }
                else if (e.UndoContext is IList<ECF_Node> nodes)
                {
                    foreach(var _node in nodes)
                    {
                        _node.UpdatePreviewBrush();
                    }
                }
                else if (e.UndoContext is Asset asset)
                {
                    if (asset.AssetType == Xv2CoreLib.EEPK.AssetType.CBIND && asset.Files[0].EcfFile == EcfFile)
                    {
                        EcfFile.UpdatePreviewBrush();
                    }
                }
                else if (e.UndoContext is IList<Asset> assets)
                {
                    if (assets.Any(x => x.AssetType == Xv2CoreLib.EEPK.AssetType.CBIND && x.Files[0].EcfFile == EcfFile))
                    {
                        EcfFile.UpdatePreviewBrush();
                    }
                }

                return;
            }

            ViewModel?.UpdateProperties();
            NotifyPropertyChanged(nameof(IsNodeEnabled));
        }

        #region Commands

        [RelayCommand(CanExecute = nameof(IsEcfLoaded))]
        private void AddNode()
        {
            ECF_Node node = new ECF_Node();
            EcfFile.Nodes.Add(node);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, node, "ECF -> Add Node"));

            SelectedNodes.Clear();
            SelectedNode = node;
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DeleteNode()
        {
            var nodes = SelectedNodes.ToArray();
            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Length);

            foreach(ECF_Node node in nodes)
            {
                undos.Add(new UndoableListRemove<ECF_Node>(EcfFile.Nodes, node));
                EcfFile.Nodes.Remove(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Delete Node");
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void CopyNode()
        {
            CustomClipboard.SetData(ECF_Node.CLIPBOARD_ID, SelectedNodes.ToArray());
        }

        [RelayCommand(CanExecute = nameof(IsNodeInClipboard))]
        private void PasteNode()
        {
            if (!CustomClipboard.TryGetData(ECF_Node.CLIPBOARD_ID, out ECF_Node[] nodes))
                return;

            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Length);
            List<ECF_Node> newNodes = new List<ECF_Node>(nodes.Length);

            foreach (ECF_Node node in nodes)
            {
                undos.Add(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, node));
                EcfFile.Nodes.Add(node);
                newNodes.Add(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Paste Node");
            SetSelectedNodes(newNodes);
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void DuplicateNode()
        {
            List<ECF_Node> nodes = SelectedNodes.ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>(nodes.Count);
            List<ECF_Node> newNodes = new List<ECF_Node>(nodes.Count);

            foreach (ECF_Node node in nodes)
            {
                ECF_Node newNode = node.Copy();
                newNodes.Add(newNode);
                undos.Add(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, newNode));
                EcfFile.Nodes.Add(newNode);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Duplicate Node");
            SetSelectedNodes(newNodes);
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueAdjustment()
        {
            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNodes, false);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        [RelayCommand(CanExecute = nameof(IsNodeSelected))]
        private void HueSet()
        {
            Forms.RecolorAll recolor = new Forms.RecolorAll(SelectedNodes, true);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        private bool IsEcfLoaded()
        {
            return EcfFile != null;
        }

        private bool IsNodeSelected()
        {
            return SelectedNode != null;
        }
        
        private bool IsNodeInClipboard()
        {
            return CustomClipboard.ContainsData(ECF_Node.CLIPBOARD_ID) && IsEcfLoaded();
        }
        #endregion

        private void EmpKeyframesView_IsEnabledToggled(object sender, EventArgs e)
        {
            SelectedNode.UpdatePreviewBrush();
        }

        private void SetSelectedNodes(IList<ECF_Node> nodes)
        {
            if (nodes.Count == 0) return;

            SelectedNode = nodes[0];
            SelectedNodes.Clear();

            foreach (var material in nodes)
            {
                SelectedNodes.Add(material);
            }

            ecfDataGrid.ScrollIntoView(SelectedNode);
        }

    }
}
