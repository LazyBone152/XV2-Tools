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
            if (SelectedNode == null) return;
            const int animationTabIndex = 1;

            if (e.PropertyName == nameof(SelectedNode.SelectedKeyframedValue) && nodeView.tabControl.SelectedIndex != animationTabIndex)
            {
                nodeView.tabControl.SelectedIndex = animationTabIndex;
            }
        }
    }
}
