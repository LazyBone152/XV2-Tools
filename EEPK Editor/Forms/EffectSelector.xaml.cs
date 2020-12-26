using EEPK_Organiser.View;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EffectSelector.xaml
    /// </summary>
    public partial class EffectSelector : MetroWindow, INotifyPropertyChanged
    {
        public enum Mode
        {
            ImportEffect,
            ExportEffect
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EffectContainerFile MainContainerFile { get; set; }
        public List<Effect> SelectedEffects = null;
        public IList<Effect> Effects { get; set; }

        private ushort _idIncreaseValue = 0;
        public ushort IdIncreaseValue
        {
            get
            {
                return this._idIncreaseValue;
            }
            set
            {
                if (value != this._idIncreaseValue)
                {
                    this._idIncreaseValue = value;
                    NotifyPropertyChanged("IdIncreaseValue");
                }
            }
        }

        private bool editModeCancelling = false;
        private Mode currentMode;

        public EffectSelector(IList<Effect> effects, EffectContainerFile mainContainerFile, object parent, Mode mode = Mode.ImportEffect)
        {
            currentMode = mode;
            Effects = effects;
            MainContainerFile = mainContainerFile;
            InitializeComponent();
            DataContext = this;
            Owner = Application.Current.MainWindow;

            switch (currentMode)
            {
                case Mode.ExportEffect:
                    Title = "Export Effects";
                    break;
            }


        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            var selectedEffects = GetSelectedEffects();

            if(selectedEffects.Count > 0)
            {
                if(currentMode == Mode.ImportEffect)
                {
                    bool wasError = false;
                    StringBuilder str = new StringBuilder();

                    foreach (var effect in selectedEffects)
                    {
                        if (MainContainerFile.IsEffectIdUsed(effect.ImportIdIncrease))
                        {
                            wasError = true;
                            str.Append(String.Format("Effect ID: {0} > New ID: {1}\r", effect.IndexNum, effect.ImportIdIncrease));
                        }
                    }

                    if (wasError)
                    {
                        LogForm log = new LogForm("The following effect IDs are conflicting.\nPlease change them to be unique.", str.ToString(), "ID Conflict", this, true);
                        log.Show();
                    }
                    else
                    {
                        SelectedEffects = selectedEffects;
                        Close();
                    }
                }
                else
                {
                    SelectedEffects = selectedEffects;
                    Close();
                }
            }
        }

        private void IdIncreaseValueButton_Click(object sender, RoutedEventArgs e)
        {
            if(Effects != null)
            {
                foreach(var effect in Effects)
                {
                    effect.ImportIdIncrease += IdIncreaseValue;
                }
            }
        }

        private void IdDecreaseValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (Effects != null)
            {
                foreach (var effect in Effects)
                {
                    effect.ImportIdIncrease -= IdIncreaseValue;
                }
            }
        }

        private void EffectDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (editModeCancelling)
            {
                return;
            }

            var selectedEffect = effectDataGrid.SelectedItem as Effect;

            if (selectedEffect != null)
            {
                string value = ((TextBox)e.EditingElement).Text;
                ushort ret = 0;

                if (!ushort.TryParse(value, out ret))
                {
                    //Value contained invalid text
                    e.Cancel = true;
                    try
                    {
                        MessageBox.Show(string.Format("The entered Effect ID contained invalid characters. Please enter a number between {0} and {1}.", ushort.MinValue, ushort.MaxValue), "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Error);
                        editModeCancelling = true;
                        (sender as DataGrid).CancelEdit();
                    }
                    finally
                    {
                        editModeCancelling = false;
                    }
                }
                else
                {
                    //Value is a valid number.

                    //Now check if it is used by another Effect
                    if (ImportEffectIdInceaseUsedByOtherEffects(ret, selectedEffect))
                    {
                        e.Cancel = true;
                        try
                        {
                            MessageBox.Show(string.Format("The entered New ID is already taken.", ushort.MinValue, ushort.MaxValue), "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Error);
                            editModeCancelling = true;
                            (sender as DataGrid).CancelEdit();
                        }
                        finally
                        {
                            editModeCancelling = false;
                        }
                    }
                }
            }
        }


        public bool ImportEffectIdInceaseUsedByOtherEffects(ushort id, Effect effect)
        {
            foreach (var _effect in Effects)
            {
                if (_effect != effect && _effect.ImportIdIncrease == id) return true;
            }

            return false;
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach(var effect in Effects)
            {
                effect.IsSelected = false;
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var effect in Effects)
            {
                effect.IsSelected = true;
            }
        }

        private List<Effect> GetSelectedEffects()
        {
            return Effects.Where(p => p.IsSelected == true).ToList();
        }

        private void ContextMenu_IncreaseID_Click(object sender, RoutedEventArgs e)
        {
            var selected = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

            if (selected != null)
            {
                foreach (var effect in selected)
                {
                    effect.ImportIdIncrease += IdIncreaseValue;
                }
            }
        }

        private void ContextMenu_DecreaseID_Click(object sender, RoutedEventArgs e)
        {
            var selected = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

            if (selected != null)
            {
                foreach (var effect in selected)
                {
                    effect.ImportIdIncrease -= IdIncreaseValue;
                }
            }
        }

        private void ContextMenu_Select_Click(object sender, RoutedEventArgs e)
        {
            var selected = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

            if (selected != null)
            {
                foreach (var effect in selected)
                {
                    effect.IsSelected = true;
                }
            }
        }

        private void ContextMenu_Unselect_Click(object sender, RoutedEventArgs e)
        {
            var selected = effectDataGrid.SelectedItems.Cast<Effect>().ToList();

            if (selected != null)
            {
                foreach (var effect in selected)
                {
                    effect.IsSelected = false;
                }
            }
        }
    }
}
