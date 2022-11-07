using MahApps.Metro.Controls;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.ETR;

namespace EEPK_Organiser.Forms.Editors
{
    public partial class EtrEditorWindow : MetroWindow
    {
        public ETR_File EtrFile { get; set; }
        public AssetContainerTool AssetContainer { get; set; }

        public EtrEditorWindow(ETR_File etrFile, AssetContainerTool assetContainer, string etrName)
        {
            DataContext = this;
            EtrFile = etrFile;
            AssetContainer = assetContainer;
            InitializeComponent();

            Title += $" ({etrName})";
        }
    }
}
