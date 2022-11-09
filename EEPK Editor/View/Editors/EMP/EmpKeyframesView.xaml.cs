using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EEPK_Organiser.ViewModel;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Editors.EMP
{

    public partial class EmpKeyframesView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP
        public static readonly DependencyProperty KeyframedValueProperty = DependencyProperty.Register(nameof(KeyframedValue), typeof(KeyframedBaseValue), typeof(EmpKeyframesView), new PropertyMetadata(OnKeyframedValueChanged));

        public KeyframedBaseValue KeyframedValue
        {
            get => (KeyframedBaseValue)GetValue(KeyframedValueProperty);
            set => SetValue(KeyframedValueProperty, value);
        }

        public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(nameof(Node), typeof(ISelectedKeyframedValue), typeof(EmpKeyframesView), new PropertyMetadata(null));

        public ISelectedKeyframedValue Node
        {
            get => (ISelectedKeyframedValue)GetValue(NodeProperty);
            set => SetValue(NodeProperty, value);
        }


        private static void OnKeyframedValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is EmpKeyframesView view)
            {
                view._viewModel.SetContext(e.NewValue as KeyframedBaseValue);
                view.DataGridSorting();
                view.NotifyPropertyChanged(nameof(KeyframedValue));
                view.NotifyPropertyChanged(nameof(ViewModel));
                view.NotifyPropertyChanged(nameof(KeyframedValueName));
                view.NotifyPropertyChanged(nameof(MaxTime));

                if (view.KeyframedValue == null)
                {
                    view.mainGrid.Visibility = Visibility.Collapsed;
                    view.nothingSelectedText.Visibility = Visibility.Visible;
                }
                else
                {
                    view.mainGrid.Visibility = Visibility.Visible;
                    view.nothingSelectedText.Visibility = Visibility.Collapsed;
                }

                if(view.KeyframedValue != null)
                {
                    if (view.KeyframedValue.IsEtrValue)
                    {
                        view.defaultValues.Visibility = Visibility.Collapsed;
                        view.etrValues.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        view.defaultValues.Visibility = Visibility.Visible;
                        view.etrValues.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        #endregion

        private EmpKeyframesViewModel _viewModel = new EmpKeyframesViewModel();
        public EmpKeyframesViewModel ViewModel => KeyframedValue != null ? _viewModel : null;

        public string KeyframedValueName => KeyframedValue != null ? KeyframedValue.GetValueName().ToUpper() : null;
        public int MaxTime => KeyframedValue?.IsEtrValue == true ? 20000 : 1;

        public EmpKeyframesView()
        {
            DataContext = this;
            InitializeComponent();
            nothingSelectedText.Text = "<Select a keyframed value to begin editing keyframes>";
            mainGrid.Visibility = Visibility.Collapsed;
            nothingSelectedText.Visibility = Visibility.Visible;

            _viewModel.FloatKeyframeDataGrid = floatKeyframesGrid;
            _viewModel.ColorKeyframeDataGrid = colorKeyframesGrid;
            _viewModel.Vector2KeyframeDataGrid = vector2KeyframesGrid;
            _viewModel.Vector3KeyframeDataGrid = vector3KeyframesGrid;

            //Sorting
            DataGridSorting();

            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void DataGridSorting()
        {
            floatKeyframesGrid.Items.SortDescriptions.Clear();
            colorKeyframesGrid.Items.SortDescriptions.Clear();
            vector2KeyframesGrid.Items.SortDescriptions.Clear();
            vector3KeyframesGrid.Items.SortDescriptions.Clear();

            floatKeyframesGrid.Items.SortDescriptions.Add(new SortDescription(nameof(KeyframeBaseValue.Time), ListSortDirection.Ascending));
            floatKeyframesGrid.Items.IsLiveSorting = true;
            colorKeyframesGrid.Items.SortDescriptions.Add(new SortDescription(nameof(KeyframeBaseValue.Time), ListSortDirection.Ascending));
            colorKeyframesGrid.Items.IsLiveSorting = true;
            vector2KeyframesGrid.Items.SortDescriptions.Add(new SortDescription(nameof(KeyframeBaseValue.Time), ListSortDirection.Ascending));
            vector2KeyframesGrid.Items.IsLiveSorting = true;
            vector3KeyframesGrid.Items.SortDescriptions.Add(new SortDescription(nameof(KeyframeBaseValue.Time), ListSortDirection.Ascending));
            vector3KeyframesGrid.Items.IsLiveSorting = true;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            ViewModel?.UpdateProperties();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        public RelayCommand DeselectedKeyframedValueCommand => new RelayCommand(DeselectedKeyframedValue, () => Node != null);
        private void DeselectedKeyframedValue()
        {
            Node.SelectedKeyframedValue = null;
        }

    }
}
