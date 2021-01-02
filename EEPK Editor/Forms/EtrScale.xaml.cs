using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.Windows;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EtrScale.xaml
    /// </summary>
    public partial class EtrScale : MetroWindow
    {
        private Asset _asset = null;

        public float scaleFactor { get; set; } = 1.0f;


        public EtrScale(Asset asset, Window parent = null)
        {
            Owner = parent;
            _asset = asset;
            InitializeComponent();
            DataContext = this;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {

            if (scaleFactor != 1.0f)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                _asset.Files[0].EtrFile.ScaleETRParts(scaleFactor, undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "ETR Scale"));
            }

            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
