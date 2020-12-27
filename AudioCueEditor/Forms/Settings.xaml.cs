using AudioCueEditor.Settings;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace AudioCueEditor.Forms
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {
        public AppSettings settings { get; set; }

        public Settings(AppSettings _settings)
        {
            settings = _settings;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = this;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!File.Exists(String.Format("{0}/bin/DBXV2.exe", settings.GameDirectory)) && !String.IsNullOrWhiteSpace(settings.GameDirectory))
            {
                MessageBox.Show("The entered game directory is not valid and it will be removed.", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                settings.GameDirectory = string.Empty;
            }
        }
    }
}
