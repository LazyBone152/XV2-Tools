using CommunityToolkit.Mvvm.Input;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Misc;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ESK;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using Application = System.Windows.Application;

namespace EEPK_Organiser.View
{
    public partial class EepkEditor : AutoObservableUserControl
    {
        public Asset GetSelectedAsset(AssetType type)
        {
            switch (type)
            {
                case AssetType.PBIND:
                    return pbindView.SelectedAsset;
                case AssetType.TBIND:
                    return tbindView.SelectedAsset;
                case AssetType.CBIND:
                    return cbindView.SelectedAsset;
                case AssetType.EMO:
                    return emoView.SelectedAsset;
                case AssetType.LIGHT:
                    return lightView.SelectedAsset;
            }

            return null;
        }

        #region ImportAsset
        public async Task<List<Asset>> ImportAssetsFromGame(AssetType assetType, EntitySelector.EntityType fromType)
        {
            var effectFile = await LoadEepkFromGame(fromType);

            if (effectFile == null)
                return null;

            switch (assetType)
            {
                case AssetType.EMO:
                    return await AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO, effectFile);
                case AssetType.PBIND:
                    return await AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND, effectFile);
                case AssetType.TBIND:
                    return await AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND, effectFile);
                case AssetType.CBIND:
                    return await AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND, effectFile);
                case AssetType.LIGHT:
                    return await AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT, effectFile);
            }

            return null;
        }

        public async Task<List<Asset>> AssetContainer_ImportAssets(AssetContainerTool container, AssetType type, EffectContainerFile importFile = null)
        {
            importFile ??= await LoadEffectContainerFile();

            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<Asset> importedAssets = new List<Asset>();
            int renameCount = 0;
            int alreadyExistCount = 0;
            int addedCount = 0;

            if (importFile != null)
            {
                AssetSelector assetSelector = new AssetSelector(importFile, true, true, type, this);
                assetSelector.ShowDialog();

                if (assetSelector.SelectedAssets != null)
                {
                    var controller = await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync($"Initializing...", $"", false, new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, DialogTitleFontSize = 15, DialogMessageFontSize = 12 });
                    controller.Minimum = 0;
                    controller.Maximum = assetSelector.SelectedAssets.Count;
                    controller.SetMessage(String.Format("Importing assets: 0 of {0}.", assetSelector.SelectedAssets.Count));

                    try
                    {
                        await Task.Run(() =>
                        {
                            foreach (var newAsset in assetSelector.SelectedAssets)
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
                                            switch (type)
                                            {
                                                case AssetType.PBIND:
                                                    container.AddPbindDependencies(newFile.EmpFile, undos);
                                                    break;
                                                case AssetType.TBIND:
                                                    container.AddTbindDependencies(newFile.EtrFile, undos);
                                                    break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            //There was an overflow of textures
                                            //Since we haven't added the new asset to the main list yet we dont need to revert anything.
                                            throw new InvalidOperationException(String.Format("{0}\n\nThe asset was not imported.", ex.Message));
                                        }
                                    }

                                    newAsset.RefreshNamePreview();

                                    container.AddAsset(newAsset);
                                    importedAssets.Add(newAsset);
                                    undos.Add(new UndoableListAdd<Asset>(container.Assets, newAsset));

                                    controller.SetProgress(addedCount);
                                    controller.SetMessage(String.Format("Importing assets: {0} of {1}.", addedCount, assetSelector.SelectedAssets.Count));

                                    addedCount++;
                                }
                                else
                                {
                                    alreadyExistCount++;
                                }
                            }


                        });


                        await controller.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        await controller.CloseAsync();
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }

                }

                if (alreadyExistCount > 0)
                {
                    MessagePrompt.Show(String.Format("{0} assets were skipped because they already exist in this EEPK.\n\nHint: Use the duplicate function if that is what you want.", alreadyExistCount), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                if (renameCount > 0 && addedCount > 0)
                {
                    MessagePrompt.Show(String.Format("Assets imported.\n\nNote: {0} files were renamed during the add process due to existing file(s) having the same name.", renameCount), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else if (addedCount > 0)
                {
                    MessagePrompt.Show(String.Format("Assets imported."), "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Information);
                }
                else
                {
                    MessagePrompt.Show("Operation aborted.", "Add Asset(s)", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                }

                //Add Undos
                if (addedCount > 0)
                {
                    undos.Add(new UndoActionDelegate(container, nameof(container.UpdateAssetFilter), true));
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Import Asset"));
                }
            }

            return importedAssets;
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private async Task ImportAssetsFromExternalFile(AssetType assetType)
        {
            switch ((AssetType)assetType)
            {
                case AssetType.EMO:
                    await AssetContainer_ImportAssets(effectContainerFile.Emo, AssetType.EMO);
                    break;
                case AssetType.PBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Pbind, AssetType.PBIND);
                    break;
                case AssetType.TBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Tbind, AssetType.TBIND);
                    break;
                case AssetType.CBIND:
                    await AssetContainer_ImportAssets(effectContainerFile.Cbind, AssetType.CBIND);
                    break;
                case AssetType.LIGHT:
                    await AssetContainer_ImportAssets(effectContainerFile.LightEma, AssetType.LIGHT);
                    break;
            }
        }
        #endregion
    }
}
