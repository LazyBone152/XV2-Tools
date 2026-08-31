using System;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace LB_Common.Forms
{
    public partial class NumericBoolInput : MetroWindow
    {
        public string FormName { get; private set; }
        public bool IsCancelled { get; private set; } = true;

        public bool BoolValue { get; private set; }

        public NumericBoolInput(string formName, string valueName, object defaultValue, string boolValueName, bool defaultBoolValue = false, double min = 0.0, double max = 10000, double interval = 1.0, string tooltip = null, string helpText = null)
        {
            if (interval <= 0.0)
                interval = 1.0;

            if (!NumericInput.IsValidNumericType(defaultValue.GetType()))
            {
                throw new ArgumentException($"NumericInput: Form not available for this type ({defaultValue.GetType()}). Only primitive, numeric types are supported.");
            }

            FormName = formName;
            BoolValue = defaultBoolValue;

            InitializeComponent();
            DataContext = this;

            //Use appropriate formatting for float types
            if (defaultValue is double || defaultValue is float)
                valueControl.StringFormat = "#########0.0##";

            valueControl.Value = Convert.ToDouble(defaultValue);
            valueControl.Minimum = min;
            valueControl.Maximum = max;
            valueControl.Interval = interval;
            valueLabel.Content = valueName;
            valueLabel.ToolTip = tooltip;
            boolLabel.Content = boolValueName;

            helpTestStackpanel.Visibility = string.IsNullOrWhiteSpace(helpText) ? Visibility.Collapsed : Visibility.Visible;
            helpTextBlock.Text = helpText;
        }

        public T GetValue<T>() where T : struct
        {
            if (!NumericInput.IsValidNumericType(typeof(T)))
            {
                throw new ArgumentException($"NumericInput.GetValue: Type ({typeof(T)}) is not a valid type. Only primitive, numeric types are supported.");
            }

            return (T)Convert.ChangeType(valueControl.Value ?? 0.0, typeof(T));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                e.Handled = true;
                IsCancelled = false;
                Close();
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                e.Handled = true;
                IsCancelled = true;
                Close();
            }
        }
    }
}