using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.ECF;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;

namespace EEPK_Organiser.View.Editors
{
    /// <summary>
    /// Interaction logic for EcfEditor.xaml
    /// </summary>
    public partial class EcfEditor : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

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
            ViewModel?.UpdateProperties();
            NotifyPropertyChanged(nameof(IsNodeEnabled));
        }

        #region Commands
        private List<ECF_Node> SelectedNodes => ecfDataGrid.SelectedItems.Cast<ECF_Node>().ToList();

        public RelayCommand AddNodeCommand => new RelayCommand(AddNode, IsEcfLoaded);
        private void AddNode()
        {
            ECF_Node node = new ECF_Node();
            EcfFile.Nodes.Add(node);

            UndoManager.Instance.AddUndo(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, node, "ECF -> Add Node"));
        }

        public RelayCommand DeleteNodeCommand => new RelayCommand(DeleteNode, IsNodeSelected);
        private void DeleteNode()
        {
            List<ECF_Node> nodes = SelectedNodes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(ECF_Node node in nodes)
            {
                undos.Add(new UndoableListRemove<ECF_Node>(EcfFile.Nodes, node));
                EcfFile.Nodes.Remove(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Delete Node");
        }

        public RelayCommand CopyNodeCommand => new RelayCommand(CopyNode, IsNodeSelected);
        private void CopyNode()
        {
            Clipboard.SetData(ECF_Node.CLIPBOARD_ID, SelectedNodes);
        }

        public RelayCommand PasteNodeCommand => new RelayCommand(PasteNode, () => Clipboard.ContainsData(ECF_Node.CLIPBOARD_ID) && IsEcfLoaded());
        private void PasteNode()
        {
            List<ECF_Node> nodes = (List<ECF_Node>)Clipboard.GetData(ECF_Node.CLIPBOARD_ID);
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ECF_Node node in nodes)
            {
                undos.Add(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, node));
                EcfFile.Nodes.Add(node);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Paste Node");
        }

        public RelayCommand DuplicateNodeCommand => new RelayCommand(DuplicateNode, IsNodeSelected);
        private void DuplicateNode()
        {
            List<ECF_Node> nodes = SelectedNodes;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (ECF_Node node in nodes)
            {
                ECF_Node newNode = node.Copy();
                undos.Add(new UndoableListAdd<ECF_Node>(EcfFile.Nodes, newNode));
                EcfFile.Nodes.Add(newNode);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "ECF -> Duplicate Node");
        }

        private bool IsEcfLoaded()
        {
            return EcfFile != null;
        }

        private bool IsNodeSelected()
        {
            return SelectedNode != null;
        }
        #endregion
    }
}
