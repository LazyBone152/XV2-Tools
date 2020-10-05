using ControlzEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EffectContainer;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EEPK_Organiser.Forms.ETR
{
    /// <summary>
    /// Interaction logic for ScalePart.xaml
    /// </summary>
    public partial class Scale : Window
    {
     


        private Asset _asset = null;

        public float scaleFactor { get; set; }

      

        public Scale( Asset asset, Window parent = null)
        {
            Owner = parent;
            _asset = asset;
            DataContext = this;
            scaleFactor = 1.0f;
            InitializeComponent();
          
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
         
            if (scaleFactor != 1.0)
                _asset.Files[0].EtrFile.ScaleETRParts(scaleFactor);
         


            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
