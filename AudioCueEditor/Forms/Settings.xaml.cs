using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Xv2CoreLib.Resource.App;

namespace AudioCueEditor.Forms
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : MetroWindow, INotifyPropertyChanged
    {
        #region NotPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public Xv2CoreLib.Resource.App.Settings settings { get; set; }
        private MainWindow _parent;

        public Visibility LightAccentVisibility { get { return (settings.GetCurrentTheme() == AppTheme.Light) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility DarkAccentVisibility { get { return (settings.GetCurrentTheme() == AppTheme.Dark) ? Visibility.Visible : Visibility.Collapsed; } }

        public Settings(MainWindow parent)
        {
            _parent = parent;
            settings = SettingsManager.Instance.Settings;
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
            DataContext = this;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
        }

        ~Settings()
        {
            SettingsManager.SettingsReloaded -= SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            settings = SettingsManager.Instance.Settings;
            NotifyPropertyChanged(nameof(settings));
            ThemeRadioButtons_CheckChanged(null, null);
        }

        private void ThemeRadioButtons_CheckChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(LightAccentVisibility));
            NotifyPropertyChanged(nameof(DarkAccentVisibility));

            _parent.InitTheme();
        }

        private void ThemeAccentComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _parent.InitTheme();
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
