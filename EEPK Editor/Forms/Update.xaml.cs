using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class UpdateForm : MetroWindow
    {
        public MessageBoxResult result { get; private set; }
        private Update.UpdateInfo UpdateInfo { get; set; }

        public UpdateForm(Update.UpdateInfo updateInfo, object parent)
        {
            UpdateInfo = updateInfo;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            changelog.AppendText(updateInfo.Changelog);
            text.Text = String.Format("A new version has been detected ({0}). Do you want to go to the download page?", UpdateInfo.Version);
            
        }

        private void Button_Yes_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(UpdateInfo.DownloadUrl);
            result = MessageBoxResult.Yes;
            Close();
        }

        private void Button_No_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.No;
            Close();
        }
    }
}
