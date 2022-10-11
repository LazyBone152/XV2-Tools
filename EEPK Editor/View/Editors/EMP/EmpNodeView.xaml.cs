using EEPK_Organiser.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
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

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(nameof(Node), typeof(ParticleNode), typeof(EmpNodeView), new PropertyMetadata(OnDpChanged));

        private ParticleNode PrevNode = null;
        public ParticleNode Node
        {
            get { return (ParticleNode)GetValue(NodeProperty); }
            set
            {
                SetValue(NodeProperty, value);
            }
        }
        public EmpNodeViewModel NodeViewModel { get; set; }

        private static DependencyPropertyChangedEventHandler DpChanged;

        private static void OnDpChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (DpChanged != null)
                DpChanged.Invoke(sender, e);
        }

        private void NodeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NodeViewModel = Node != null ? new EmpNodeViewModel(Node) : null;
            NotifyPropertyChanged(nameof(Node));
            UpdateVisibilities();
            //UpdateProperties();
        }
        #endregion

        #region Visibilities
        //Emitter:
        public Visibility EmitterFromAreaVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterPositionVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Cone) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterSizeVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Sphere) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterSize2Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterAngleVisibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Cone) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterF1Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Circle, ParticleEmitter.ParticleEmitterShape.Square, ParticleEmitter.ParticleEmitterShape.Cone) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmitterF2Visibility => IsEmitterShape(ParticleEmitter.ParticleEmitterShape.Cone) ? Visibility.Visible : Visibility.Collapsed;

        //Emission:

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
        #endregion

        public EmpNodeView()
        {
            InitializeComponent();
            DpChanged += NodeChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            UpdateVisibilities();
        }


        private void UpdateVisibilities()
        {
            NotifyPropertyChanged(nameof(EmitterFromAreaVisibility));
            NotifyPropertyChanged(nameof(EmitterPositionVisibility));
            NotifyPropertyChanged(nameof(EmitterSizeVisibility));
            NotifyPropertyChanged(nameof(EmitterSize2Visibility));
            NotifyPropertyChanged(nameof(EmitterAngleVisibility));
            NotifyPropertyChanged(nameof(EmitterF1Visibility));
            NotifyPropertyChanged(nameof(EmitterF2Visibility));
        }
    }
}
