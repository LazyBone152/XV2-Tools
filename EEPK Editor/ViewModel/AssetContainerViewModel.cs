using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.ViewModel
{
    public class AssetContainerViewModel : ObservableObject, IDisposable
    {
        private readonly AssetContainerTool AssetContainer;

        public int AssetSpawnLimit
        {
            get => AssetContainer.AssetSpawnLimit;
            set
            {
                if(AssetContainer.AssetSpawnLimit != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.AssetSpawnLimit), AssetContainer, AssetContainer.AssetSpawnLimit, value, $"{AssetContainer.ContainerAssetType} Spawn Limit"));
                    AssetContainer.AssetSpawnLimit = value;
                }
            }
        }
        public byte I_04
        {
            get => AssetContainer.I_04;
            set
            {
                if (AssetContainer.I_04 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.I_04), AssetContainer, AssetContainer.I_04, value, $"{AssetContainer.ContainerAssetType} I_04"));
                    AssetContainer.I_04 = value;
                }
            }
        }
        public byte I_05
        {
            get => AssetContainer.I_05;
            set
            {
                if (AssetContainer.I_05 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.I_05), AssetContainer, AssetContainer.I_05, value, $"{AssetContainer.ContainerAssetType} I_05"));
                    AssetContainer.I_05 = value;
                }
            }
        }
        public byte I_06
        {
            get => AssetContainer.I_06;
            set
            {
                if (AssetContainer.I_06 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.I_06), AssetContainer, AssetContainer.I_06, value, $"{AssetContainer.ContainerAssetType} I_06"));
                    AssetContainer.I_06 = value;
                }
            }
        }
        public byte I_07
        {
            get => AssetContainer.I_07;
            set
            {
                if (AssetContainer.I_07 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.I_07), AssetContainer, AssetContainer.I_07, value, $"{AssetContainer.ContainerAssetType} I_07"));
                    AssetContainer.I_07 = value;
                }
            }
        }
        public int AssetListLimit
        {
            get => AssetContainer.AssetListLimit;
            set
            {
                if (AssetContainer.AssetListLimit != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.AssetListLimit), AssetContainer, AssetContainer.AssetListLimit, value, $"{AssetContainer.ContainerAssetType} List Limit"));
                    AssetContainer.AssetListLimit = value;
                }
            }
        }
        public int I_12
        {
            get => AssetContainer.I_12;
            set
            {
                if (AssetContainer.I_12 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.I_12), AssetContainer, AssetContainer.I_12, value, $"{AssetContainer.ContainerAssetType} I_12"));
                    AssetContainer.I_12 = value;
                }
            }
        }

        public AssetContainerViewModel(AssetContainerTool assetContainer)
        {
            AssetContainer = assetContainer;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => AssetSpawnLimit);
            RaisePropertyChanged(() => I_04);
            RaisePropertyChanged(() => I_05);
            RaisePropertyChanged(() => I_06);
            RaisePropertyChanged(() => I_07);
            RaisePropertyChanged(() => AssetListLimit);
            RaisePropertyChanged(() => I_12);
        }

    }
}