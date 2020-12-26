using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for ProgressBarFileLoad.xaml
    /// </summary>
    public partial class ProgressBarAssetImport : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public AssetContainerTool container { get; set; }
        public List<Asset> SelectedAssets { get; set; }
        public string ProgressUpdate { get; set; }
        public int renameCount = 0;
        public int alreadyExistCount = 0;
        public int addedCount = 0;
        public AssetType type { get; set; }
        public Exception exception = null;

        public ProgressBarAssetImport(AssetContainerTool _mainContainerFile, List<Asset> _selectedAssets, AssetType _type, Window parent)
        {
            container = _mainContainerFile;
            SelectedAssets = _selectedAssets;
            type = _type;
            InitializeComponent();
            Owner = parent;
            InitProgressBar();
            DataContext = this;
        }

        private void InitProgressBar()
        {
            progressBar.Maximum = SelectedAssets.Count;
            progressBar.Value = 0;
            ProgressUpdate = String.Format("Importing assets: 0 of {0}.", SelectedAssets.Count);
            NotifyPropertyChanged("ProgressUpdate");
        }

        private void Load()
        {
            foreach (var newAsset in SelectedAssets)
            {
                Asset existingAsset = container.AssetExists(newAsset);
                if (existingAsset == null)
                {
                    //First, regenerate the names
                    foreach (var newFile in newAsset.Files)
                    {
                        newFile.SetName(container.GetUnusedName(newFile.FullFileName));

                        if (newFile.FullFileName != newFile.OriginalFileName)
                        {
                            renameCount++;
                        }

                        try
                        {
                            Dispatcher.Invoke((() =>
                            {
                                switch (type)
                                {
                                    case AssetType.PBIND:
                                        container.AddPbindDependencies(newFile.EmpFile);
                                        break;
                                    case AssetType.TBIND:
                                        container.AddTbindDependencies(newFile.EtrFile);
                                        break;
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            //There was an overflow of textures
                            //Since we haven't added the new asset to the main list yet we dont need to revert anything.
                            throw new InvalidOperationException(String.Format("{0}\n\nThe asset was not imported.", ex.Message));
                        }
                    }

                    newAsset.RefreshNamePreview();

                    Dispatcher.Invoke((() =>
                    {
                        container.AddAsset(newAsset);
                        progressBar.Value++;
                        ProgressUpdate = String.Format("Importing assets: {0} of {1}.", progressBar.Value, SelectedAssets.Count);
                        NotifyPropertyChanged("ProgressUpdate");
                    }));
                    addedCount++;
                }
                else
                {
                    alreadyExistCount++;
                }
            }

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LoadFileAsync();
        }

        private async Task LoadFileAsync()
        {
            try
            {
                await Task.Run(new Action(Load));
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Close();
            }
        }
    }
}
