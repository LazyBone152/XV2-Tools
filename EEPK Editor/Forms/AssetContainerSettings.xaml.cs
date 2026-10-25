using MahApps.Metro.Controls;
using Xv2CoreLib.EffectContainer;
using EEPK_Organiser.ViewModel;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for AssetContainerSettings.xaml
    /// </summary>
    public partial class AssetContainerSettings : MetroWindow
    {
        public AssetContainerTool AssetContainer { get; private set; }
        public AssetContainerViewModel ViewModel { get; private set; }

        public AssetContainerSettings(AssetContainerTool assetContainer)
        {
            AssetContainer = assetContainer;
            ViewModel = new AssetContainerViewModel(assetContainer);
            InitializeComponent();

            Title = $"{AssetContainer.ContainerAssetType} Settings";

            Closing += AssetContainerSettings_Closing;

            if(assetContainer.ContainerAssetType != Xv2CoreLib.EEPK.AssetType.PBIND)
            {
                poolSizeLabel.Content = "Initial Pool Size";
                poolSizeLabel.ToolTip = "Sets the initial size of an internal asset pool used by the game for this EEPK asset type. " +
                    "The pool will automatically resize if the ammount of assets currently active exceeds its capacity.";
                particleConfig1.IsEnabled = false;
                particleConfig2.IsEnabled = false;
                particleConfig3.IsEnabled = false;
            }
            else
            {
                poolSizeLabel.Content = "Pool Size";
                poolSizeLabel.ToolTip = "Sets the size of an internal asset pool used by the game for particle effects. The pool will not automatically resize, " +
                    "so this is also the maximum amount of PBIND assets that can be active at any given time.\n\n" +
                    "NOTE: The real pool size used by the game for PBIND is actually the Pool Size you see here multipled by 4.";
            }
        }

        private void AssetContainerSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}
