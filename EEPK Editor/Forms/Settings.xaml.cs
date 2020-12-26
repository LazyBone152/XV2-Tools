using System;
using System.IO;
using System.Windows;
using EEPK_Organiser.Settings;
using MahApps.Metro.Controls;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {

        public AppSettings settings { get; set; }

        public Settings(AppSettings _settings, object parent)
        {
            settings = _settings;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = this;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var _browser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            _browser.UseDescriptionForTitle = true;
            _browser.Description = "Browse for DBXV2 Directory";
            _browser.ShowDialog();

            if (!String.IsNullOrEmpty(_browser.SelectedPath))
            {
                if (File.Exists(String.Format("{0}/bin/DBXV2.exe", _browser.SelectedPath)))
                {
                    settings.GameDirectory = _browser.SelectedPath;
                }
                else
                {
                    MessageBox.Show(this, "The entered game directory is not valid.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
