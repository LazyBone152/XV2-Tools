using System;
using System.Collections.Generic;
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

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for LogForm.xaml
    /// </summary>
    public partial class LogPrompt : Window
    {
        public string Description { get; set; }
        public MessageBoxResult Result { get; set; } = MessageBoxResult.No;

        public LogPrompt(string _description, string _logText, string _windowName, Window _owner = null, bool showInTaskBar = false)
        {
            Description = _description;
            InitializeComponent();
            DataContext = this;
            Owner = _owner;
            Title = _windowName;
            richTextBox.AppendText(_logText);
            ShowInTaskbar = showInTaskBar;
        }

        private void Button_Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void Button_No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                e.Handled = true;
                Result = MessageBoxResult.Yes;
                Close();
            }
            else if (Keyboard.IsKeyDown(Key.Escape) || Keyboard.IsKeyDown(Key.Back))
            {
                e.Handled = true;
                Result = MessageBoxResult.No;
                Close();
            }
        }
    }
}
