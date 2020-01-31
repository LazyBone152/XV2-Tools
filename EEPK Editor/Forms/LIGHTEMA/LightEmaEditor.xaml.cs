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
using Xv2CoreLib.EMA;

namespace EEPK_Organiser.Forms.LIGHTEMA
{
    /// <summary>
    /// Interaction logic for LightEmaEditor.xaml
    /// </summary>
    public partial class LightEmaEditor : Window
    {
        public EMA_File emaFile { get; set; }
        private string emaName { get; set; }

        public LightEmaEditor(EMA_File _emaFile, string _emaName)
        {
            emaFile = _emaFile;
            emaName = _emaName;
            InitializeComponent();
            DataContext = this;
            Title = String.Format("Light Editor ({0})", emaName);
            ValidateEmaFile();
        }

        private void ValidateEmaFile()
        {
            if (emaFile.EmaType != EmaType.light)
            {
                MessageBox.Show("\"{0}\" is an invalid light.ema file. It does not contain light animations.", "Invalid Ema", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void AddAnimation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveAnimaton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
