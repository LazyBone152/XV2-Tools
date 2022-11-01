using MahApps.Metro.Controls;
using Xv2CoreLib.ECF;

namespace EEPK_Organiser.Forms.Editors
{
    /// <summary>
    /// Interaction logic for EcfEditorWindow.xaml
    /// </summary>
    public partial class EcfEditorWindow : MetroWindow
    {
        public ECF_File EcfFile { get; set; }

        public EcfEditorWindow(ECF_File ecfFile, string name)
        {
            DataContext = this;
            EcfFile = ecfFile;
            InitializeComponent();

            Title += $" ({name})";
        }
    }
}
