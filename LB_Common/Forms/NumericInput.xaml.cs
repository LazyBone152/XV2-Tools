using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace LB_Common.Forms
{
    public partial class NumericInput : MetroWindow
    {
        public string FormName { get; private set; }
        public bool IsCancelled { get; private set; } = true;

        private static readonly Type[] ValidNumericTypes = new Type[]
        {
            typeof(byte), typeof(sbyte),
            typeof(ushort), typeof(short),
            typeof(uint), typeof(int),
            typeof(ulong), typeof(long),
            typeof(float), typeof(double)
        };

        public NumericInput(string formName, string valueName, object defaultValue, double min = 0.0, double max = 10000, double interval = 1.0, string tooltip = null, string helpText = null, bool topmost = true)
        {
            if (interval <= 0.0)
                interval = 1.0;

            if (!IsValidNumericType(defaultValue.GetType()))
            {
                throw new ArgumentException($"NumericInput: Form not available for this type ({defaultValue.GetType()}). Only primitive, numeric types are supported.");
            }

            FormName = formName;
            Topmost = topmost;

            InitializeComponent();
            DataContext = this;
            Topmost = true;

            //Use appropriate formatting for float types
            if (defaultValue is double || defaultValue is float)
                valueControl.StringFormat = "#########0.0##";

            valueControl.Value = Convert.ToDouble(defaultValue);
            valueControl.Minimum = min;
            valueControl.Maximum = max;
            valueControl.Interval = interval;
            valueLabel.ToolTip = tooltip;
            valueLabel.Content = valueName;

            helpTestStackpanel.Visibility = string.IsNullOrWhiteSpace(helpText) ? Visibility.Collapsed : Visibility.Visible;
            helpTextBlock.Text = helpText;

            valueControl.Focus();
        }

        public static T Show<T>(string formName, string valueName, T defaultValue, double min = 0.0, double max = 10000, double interval = 1.0, string tooltip = null, string helpText = null) where T : struct
        {
            if (!IsValidNumericType(typeof(T)))
            {
                throw new ArgumentException($"NumericInput.Show: Type ({typeof(T)}) is not a valid type. Only primitive, numeric types are supported.");
            }
            var inputForm = new NumericInput(formName, valueName, defaultValue, min, max, interval, tooltip, helpText, true);
            inputForm.ShowDialog();
            return inputForm.IsCancelled ? defaultValue : inputForm.GetValue<T>();
        }

        public T GetValue<T>() where T : struct
        {
            if (!IsValidNumericType(typeof(T)))
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
    
        internal static bool IsValidNumericType(Type type)
        {
            return ValidNumericTypes.Contains(type);
        }
    }
}