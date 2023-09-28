using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
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

#if XenoKit
        public Visibility XenoKitVisible => Visibility.Visible;
#else
        public Visibility XenoKitVisible => Visibility.Collapsed;
#endif

        public EmpEditorWindow(EMP_File empFile, AssetContainerTool assetContainer, string empName)
        {
            DataContext = this;
            EmpFile = empFile;
            AssetContainer = assetContainer;
            InitializeComponent();

            Title += $" ({empName})";
        }

        public RelayCommand RefreshParticleSystemCommand => new RelayCommand(RefreshParticleSystem);
        private void RefreshParticleSystem()
        {
            EmpFile.HasBeenEdited = true;
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Topmost = ((CheckBox)sender).IsChecked == true;
        }

    }
}
