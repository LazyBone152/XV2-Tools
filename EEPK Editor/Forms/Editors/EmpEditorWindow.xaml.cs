using MahApps.Metro.Controls;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMP_NEW;

namespace EEPK_Organiser.Forms.Editors
{
    /// <summary>
    /// Interaction logic for EmpEditorWindow.xaml
    /// </summary>
    public partial class EmpEditorWindow : MetroWindow
    {
        public EMP_File EmpFile { get; set; }
        public AssetContainerTool AssetContainer { get; set; }

        public EmpEditorWindow(EMP_File empFile, AssetContainerTool assetContainer, string empName)
        {
            DataContext = this;
            EmpFile = empFile;
            AssetContainer = assetContainer;
            InitializeComponent();

            Title += $" ({empName})";
        }
    }
}
