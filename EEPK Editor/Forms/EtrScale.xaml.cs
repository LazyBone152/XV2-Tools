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

        public float ScaleFactor { get; set; } = 1.0f;
        private float OriginalScale;


        public EtrScale(Asset asset, Window parent = null)
        {
            Owner = parent;
            _asset = asset;
            InitializeComponent();
            DataContext = this;

            OriginalScale = asset.Files[0].EtrFile.GetAverageScale();
            ScaleFactor = OriginalScale;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (ScaleFactor != OriginalScale)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                _asset.Files[0].EtrFile.ScaleETRParts(ScaleFactor / OriginalScale, undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "ETR Scale"));
                UndoManager.Instance.ForceEventCall();
            }

            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
