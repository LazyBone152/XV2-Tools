using GalaSoft.MvvmLight.CommandWpf;
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
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Editors.EMP
{
    /// <summary>
    /// Interaction logic for EmpModifiersView.xaml
    /// </summary>
    public partial class EmpModifiersView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(nameof(Modifiers), typeof(AsyncObservableCollection<EMP_Modifier>), typeof(EmpModifiersView), new PropertyMetadata(ModifiersChangedCallback));

        public AsyncObservableCollection<EMP_Modifier> Modifiers
        {
            get => (AsyncObservableCollection<EMP_Modifier>)GetValue(ModifiersProperty);
            set => SetValue(ModifiersProperty, value);
        }

        public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(nameof(Node), typeof(ISelectedKeyframedValue), typeof(EmpModifiersView), new PropertyMetadata(ModifiersChangedCallback));

        public ISelectedKeyframedValue Node
        {
            get => (ISelectedKeyframedValue)GetValue(NodeProperty);
            set => SetValue(NodeProperty, value);
        }


        public static readonly DependencyProperty IsEtrProperty = DependencyProperty.Register(nameof(IsEtr), typeof(bool), typeof(EmpModifiersView), new PropertyMetadata(false));

        public bool IsEtr
        {
            get => (bool)GetValue(IsEtrProperty);
            set => SetValue(IsEtrProperty, value);
        }

        private static void ModifiersChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is EmpModifiersView view)
            {
                view.NotifyPropertyChanged(nameof(Modifiers));
                view.NotifyPropertyChanged(nameof(Node));
            }
        }

        #endregion

        private EMP_Modifier _selectedModifier = null;
        public EMP_Modifier SelectedModifier
        {
            get => _selectedModifier;
            set
            {
                _selectedModifier = value;
                UpdateVisibilities();
                NotifyPropertyChanged(nameof(SelectedModifier));
            }
        }

        public Visibility EmpVisibility => IsEtr ? Visibility.Collapsed : Visibility.Visible;
        public Visibility EtrVisibility => IsEtr ? Visibility.Visible : Visibility.Collapsed;

        public string NothingSelectedText => "<Select a modifier to begin editing>";

        public EmpModifiersView()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Unloaded += EmpModifiersView_Unloaded;
            Loaded += EmpModifiersView_Loaded;
        }

        private void EmpModifiersView_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(EmpVisibility));
            NotifyPropertyChanged(nameof(EtrVisibility));
            UpdateVisibilities();
        }

        private void EmpModifiersView_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {

        }

        public void UpdateVisibilities()
        {
            nothingSelectedLabel.Visibility = Visibility.Collapsed;
            translatePanel.Visibility = Visibility.Collapsed;
            acceleratePanel.Visibility = Visibility.Collapsed;
            anglePanel.Visibility = Visibility.Collapsed;
            pointLoopPanel.Visibility = Visibility.Collapsed;
            vortexPanel.Visibility = Visibility.Collapsed;
            jitterPanel.Visibility = Visibility.Collapsed;
            dragPanel.Visibility = Visibility.Collapsed;
            attractPanel.Visibility = Visibility.Collapsed;
            unk7Panel.Visibility = Visibility.Collapsed;

            if (SelectedModifier != null)
            {
                if(SelectedModifier.Type == 2 || SelectedModifier.Type == 4)
                {
                    translatePanel.Visibility = Visibility.Visible;
                }
                else if(SelectedModifier.Type == 3 || SelectedModifier.Type == 5)
                {
                    acceleratePanel.Visibility = Visibility.Visible;
                }
                else if((SelectedModifier.Type == 6 || SelectedModifier.Type == 7) && !IsEtr)
                {
                    anglePanel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 7 && IsEtr)
                {
                    unk7Panel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 8)
                {
                    pointLoopPanel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 9)
                {
                    vortexPanel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 10)
                {
                    jitterPanel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 11)
                {
                    dragPanel.Visibility = Visibility.Visible;
                }
                else if (SelectedModifier.Type == 12)
                {
                    attractPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                nothingSelectedLabel.Visibility = Visibility.Visible;
            }
        }

        #region Commands
        public List<EMP_Modifier> SelectedModifiers => dataGrid.SelectedItems.Cast<EMP_Modifier>().ToList();

        public RelayCommand<int> AddModifierCommand => new RelayCommand<int>(AddModifier);
        private void AddModifier(int modifierType)
        {
            if (!HasModifiers()) return;

            EMP_Modifier modifier = new EMP_Modifier((byte)modifierType, IsEtr);
            Modifiers.Add(modifier);

            UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_Modifier>(Modifiers, modifier, "Add Modifier"));
        }

        public RelayCommand DeleteModifierCommand => new RelayCommand(DeleteModifier, IsModifierSelected);
        private void DeleteModifier()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<EMP_Modifier> modifiers = SelectedModifiers;

            foreach(EMP_Modifier modifier in modifiers)
            {
                undos.Add(new UndoableListRemove<EMP_Modifier>(Modifiers, modifier));
                Modifiers.Remove(modifier);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Delete Modifiers");
        }

        public RelayCommand DuplicateModifierCommand => new RelayCommand(DuplicateModifier, IsModifierSelected);
        private void DuplicateModifier()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<EMP_Modifier> modifiers = SelectedModifiers;

            foreach (EMP_Modifier modifier in modifiers)
            {
                EMP_Modifier newModifier = modifier.Copy();
                undos.Add(new UndoableListAdd<EMP_Modifier>(Modifiers, newModifier));
                Modifiers.Add(newModifier);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Modifiers");
        }

        public RelayCommand CopyModifierCommand => new RelayCommand(CopyModifier, IsModifierSelected);
        private void CopyModifier()
        {
            Clipboard.SetData(GetClipboardFormat(), SelectedModifiers);
        }

        public RelayCommand PasteModifierCommand => new RelayCommand(PasteModifier, CanPasteModifier);
        private void PasteModifier()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<EMP_Modifier> modifiers = (List<EMP_Modifier>)Clipboard.GetData(GetClipboardFormat());

            foreach (EMP_Modifier modifier in modifiers)
            {
                EMP_Modifier newModifier = modifier.Copy();
                undos.Add(new UndoableListAdd<EMP_Modifier>(Modifiers, newModifier));
                Modifiers.Add(newModifier);
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Modifiers");
        }

        private bool CanPasteModifier()
        {
            return Clipboard.ContainsData(GetClipboardFormat());
        }

        private bool IsModifierSelected()
        {
            return SelectedModifier != null;
        }

        private bool HasModifiers()
        {
            return Modifiers != null;
        }
        #endregion

        private string GetClipboardFormat()
        {
            return IsEtr ? "XV2_ETR_MODIFIER" : "XV2_EMP_MODIFIER";
        }
    }
}
