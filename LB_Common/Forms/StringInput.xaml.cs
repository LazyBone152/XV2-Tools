using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace LB_Common.Forms
{
    public partial class StringInput : MetroWindow
    {
        public string FormName { get; private set; }
        public bool IsCancelled { get; private set; } = true;
        public string Text { get; private set; }

        public StringInput(string formName, string inputFieldName, string defaultInputText, string tooltip = null, string helpText = null, bool topmost = true)
        {
            FormName = formName;

            InitializeComponent();
            DataContext = this;
            Topmost = topmost;
            textControl.Text = defaultInputText;
            textLabel.ToolTip = tooltip;
            textLabel.Content = inputFieldName;

            helpTestStackpanel.Visibility = string.IsNullOrWhiteSpace(helpText) ? Visibility.Collapsed : Visibility.Visible;
            helpTextBlock.Text = helpText;
        }

        public static string Show(string formName, string inputFieldName, string defaultInputText, string tooltip = null, string helpText = null)
        {
            var inputForm = new StringInput(formName, inputFieldName, defaultInputText, tooltip, helpText, true);
            inputForm.ShowDialog();
            return inputForm.IsCancelled ? null : inputForm.Text;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = false;
            Text = textControl.Text;
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
                Text = textControl.Text;
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