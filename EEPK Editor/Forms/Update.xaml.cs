using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class UpdateForm : Window
    {
        public MessageBoxResult result { get; private set; }
        private Update.UpdateInfo UpdateInfo { get; set; }

        public UpdateForm(Update.UpdateInfo updateInfo, Window parent)
        {
            UpdateInfo = updateInfo;
            InitializeComponent();
            Owner = parent;
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
