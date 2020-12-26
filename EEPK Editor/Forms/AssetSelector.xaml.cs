using EEPK_Organiser.View;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for AssetSelector.xaml
    /// </summary>
    public partial class AssetSelector : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public EffectContainerFile effectContainerFile { get; set; }
        private Xv2CoreLib.EEPK.AssetType ConstrainedAssetType { get; set; }
        private bool Constrained = false;
        public List<Asset> SelectedAssets { get; set; }
        public bool MultiSelectMode { get; set; }
        public Xv2CoreLib.EEPK.AssetType SelectedAssetType { get; private set; }
        public Asset SelectedAsset
        {
            get
            {
                if (SelectedAssets == null) return null;
                if (SelectedAssets.Count == 0) return null;
                return SelectedAssets[0];
            }
        }

        //Search parameters
        private string _searchParameter = null;
        public string SearchParameter
        {
            get
            {
                return this._searchParameter;
            }
            set
            {
                if (value != this._searchParameter)
                {
                    this._searchParameter = value;
                    NotifyPropertyChanged("SearchParameter");
                }
            }
        }

        //Search function is unfinished. TODO later.
        public ObservableCollection<Asset> MergedAssetList = null;
        public ListCollectionView ViewMergedAsserList { get; set; }

        public AssetSelector(EffectContainerFile _effectFile, bool multiSelect, bool _constrained = false, Xv2CoreLib.EEPK.AssetType _constrainedAssetType = Xv2CoreLib.EEPK.AssetType.EMO, EepkEditor parent = null, Asset initialSelection = null)
        {
            MultiSelectMode = multiSelect;
            effectContainerFile = _effectFile;
            ConstrainedAssetType = _constrainedAssetType;
            Constrained = _constrained;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = this;
            InitializeTabs(initialSelection);
            InitializeSearchTab();
            if (MultiSelectMode)
            {
                emoGrid.SelectionMode = DataGridSelectionMode.Extended;
                pbindGrid.SelectionMode = DataGridSelectionMode.Extended;
                tbindGrid.SelectionMode = DataGridSelectionMode.Extended;
                cbindGrid.SelectionMode = DataGridSelectionMode.Extended;
                lightEmaGrid.SelectionMode = DataGridSelectionMode.Extended;
                searchGrid.SelectionMode = DataGridSelectionMode.Extended;
            }
            else
            {
                emoGrid.SelectionMode = DataGridSelectionMode.Single;
                pbindGrid.SelectionMode = DataGridSelectionMode.Single;
                tbindGrid.SelectionMode = DataGridSelectionMode.Single;
                cbindGrid.SelectionMode = DataGridSelectionMode.Single;
                lightEmaGrid.SelectionMode = DataGridSelectionMode.Single;
                searchGrid.SelectionMode = DataGridSelectionMode.Single;
                multiSelectTip.Visibility = Visibility.Hidden;
            }


        }

        private void InitializeTabs(Asset initialSel)
        {
            if (Constrained)
            {
                emoTab.IsEnabled = false;
                pbindTab.IsEnabled = false;
                tbindTab.IsEnabled = false;
                lightEmaTab.IsEnabled = false;
                cbindTab.IsEnabled = false;

                switch (ConstrainedAssetType)
                {
                    case Xv2CoreLib.EEPK.AssetType.EMO:
                        emoTab.IsEnabled = true;
                        emoTab.IsSelected = true;
                        break;
                    case Xv2CoreLib.EEPK.AssetType.PBIND:
                        pbindTab.IsEnabled = true;
                        pbindTab.IsSelected = true;
                        break;
                    case Xv2CoreLib.EEPK.AssetType.TBIND:
                        tbindTab.IsEnabled = true;
                        tbindTab.IsSelected = true;
                        break;
                    case Xv2CoreLib.EEPK.AssetType.LIGHT:
                        lightEmaTab.IsEnabled = true;
                        lightEmaTab.IsSelected = true;
                        break;
                    case Xv2CoreLib.EEPK.AssetType.CBIND:
                        cbindTab.IsEnabled = true;
                        cbindTab.IsSelected = true;
                        break;
                }
            }

            if(initialSel != null)
            {

                switch (ConstrainedAssetType)
                {
                    case Xv2CoreLib.EEPK.AssetType.EMO:
                        emoTab.IsSelected = true;
                        emoGrid.SelectedItem = initialSel;
                        emoGrid.ScrollIntoView(initialSel);
                        break;
                    case Xv2CoreLib.EEPK.AssetType.PBIND:
                        pbindTab.IsSelected = true;
                        pbindGrid.SelectedItem = initialSel;
                        pbindGrid.ScrollIntoView(initialSel);
                        break;
                    case Xv2CoreLib.EEPK.AssetType.TBIND:
                        tbindTab.IsSelected = true;
                        tbindGrid.SelectedItem = initialSel;
                        tbindGrid.ScrollIntoView(initialSel);
                        break;
                    case Xv2CoreLib.EEPK.AssetType.LIGHT:
                        lightEmaTab.IsSelected = true;
                        lightEmaGrid.SelectedItem = initialSel;
                        lightEmaGrid.ScrollIntoView(initialSel);
                        break;
                    case Xv2CoreLib.EEPK.AssetType.CBIND:
                        cbindTab.IsSelected = true;
                        cbindGrid.SelectedItem = initialSel;
                        cbindGrid.ScrollIntoView(initialSel);
                        break;
                }
                
            }

        }

        private void InitializeSearchTab()
        {
            MergedAssetList = new ObservableCollection<Asset>();

            if (!Constrained)
            {
                foreach (var asset in effectContainerFile.Emo.Assets)
                {
                    MergedAssetList.Add(asset);
                }

                foreach (var asset in effectContainerFile.Pbind.Assets)
                {
                    MergedAssetList.Add(asset);
                }

                foreach (var asset in effectContainerFile.Tbind.Assets)
                {
                    MergedAssetList.Add(asset);
                }

                foreach (var asset in effectContainerFile.LightEma.Assets)
                {
                    MergedAssetList.Add(asset);
                }

                foreach (var asset in effectContainerFile.Cbind.Assets)
                {
                    MergedAssetList.Add(asset);
                }

            }
            else
            {
                if(ConstrainedAssetType == AssetType.EMO)
                {
                    foreach (var asset in effectContainerFile.Emo.Assets)
                    {
                        MergedAssetList.Add(asset);
                    }
                }
                else if (ConstrainedAssetType == AssetType.PBIND)
                {
                    foreach (var asset in effectContainerFile.Pbind.Assets)
                    {
                        MergedAssetList.Add(asset);
                    }
                }
                else if (ConstrainedAssetType == AssetType.TBIND)
                {
                    foreach (var asset in effectContainerFile.Tbind.Assets)
                    {
                        MergedAssetList.Add(asset);
                    }
                }
                else if (ConstrainedAssetType == AssetType.LIGHT)
                {
                    foreach (var asset in effectContainerFile.LightEma.Assets)
                    {
                        MergedAssetList.Add(asset);
                    }
                }
                else if (ConstrainedAssetType == AssetType.CBIND)
                {
                    foreach (var asset in effectContainerFile.Cbind.Assets)
                    {
                        MergedAssetList.Add(asset);
                    }
                }
            }

            ViewMergedAsserList = new ListCollectionView(MergedAssetList);
            ViewMergedAsserList.Filter = new Predicate<object>(AssetFilterCheck);
        }

        public bool AssetFilterCheck(object asset)
        {
            if (String.IsNullOrWhiteSpace(SearchParameter)) return true;
            var _asset = asset as Asset;

            if (_asset != null)
            {
                if (_asset.FileNamesPreviewWithExtension.ToLower().Contains(SearchParameter.ToLower())) return true;
            }

            return false;
        }

        public void UpdateAssetFilter()
        {
            if (ViewMergedAsserList == null)
                ViewMergedAsserList = new ListCollectionView(MergedAssetList);

            ViewMergedAsserList.Filter = new Predicate<object>(AssetFilterCheck);
            NotifyPropertyChanged("ViewMergedAsserList");
        }


        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DataGrid currentGrid = null;
            bool searchMode = false;

            switch (tabControl.SelectedIndex)
            {
                case 0://EMO
                    currentGrid = emoGrid;
                    SelectedAssetType = Xv2CoreLib.EEPK.AssetType.EMO;
                    break;
                case 1://PBIND
                    currentGrid = pbindGrid;
                    SelectedAssetType = Xv2CoreLib.EEPK.AssetType.PBIND;
                    break;
                case 2://TBIND
                    currentGrid = tbindGrid;
                    SelectedAssetType = Xv2CoreLib.EEPK.AssetType.TBIND;
                    break;
                case 3://LIGHT.EMA
                    currentGrid = lightEmaGrid;
                    SelectedAssetType = Xv2CoreLib.EEPK.AssetType.LIGHT;
                    break;
                case 4://CBIND
                    currentGrid = cbindGrid;
                    SelectedAssetType = Xv2CoreLib.EEPK.AssetType.CBIND;
                    break;
                case 5://Search
                    currentGrid = searchGrid;
                    searchMode = true;
                    break;
                default:
                    MessageBox.Show("Unknown tab selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
            }
            
            if(currentGrid.SelectedItems.Count < 1)
            {
                MessageBox.Show("No asset is selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = currentGrid.SelectedItems.Cast<Asset>().ToList();

            if(item != null)
            {
                if (searchMode)
                {
                    AssetType prevAssetType = AssetType.EMO;
                    bool first = true;

                    foreach(var asset in item)
                    {
                        if (!first && asset.assetType != prevAssetType)
                        {
                            MessageBox.Show("All selected assets must be of the same type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            first = false;
                        }
                        prevAssetType = asset.assetType;
                    }

                    SelectedAssetType = prevAssetType;
                }

                SelectedAssets = item;
                Close();
            }
            else
            {
                MessageBox.Show("No asset is selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void Button_Search_Click(object sender, RoutedEventArgs e)
        {
            UpdateAssetFilter();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                UpdateAssetFilter();
        }
    }
}
