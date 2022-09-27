using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
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
        }

        private void AssetContainerSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}
