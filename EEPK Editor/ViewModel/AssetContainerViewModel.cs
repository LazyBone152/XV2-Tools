using LB_Common.Mvvm;
using System;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.ViewModel
{
    public class AssetContainerViewModel : AutoObservableObject, IDisposable
    {
        private readonly AssetContainerTool AssetContainer;

        public int PoolSize
        {
            get => AssetContainer.PoolSize;
            set
            {
                if(AssetContainer.PoolSize != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.PoolSize), AssetContainer, AssetContainer.PoolSize, value, $"{AssetContainer.ContainerAssetType} Pool Size"));
                    AssetContainer.PoolSize = value;
                }
            }
        }
        public int ParticleEngineConfig1
        {
            get => AssetContainer.ParticleEngineConfig1;
            set
            {
                if (AssetContainer.ParticleEngineConfig1 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.ParticleEngineConfig1), AssetContainer, AssetContainer.ParticleEngineConfig1, value, $"{AssetContainer.ContainerAssetType} ParticleEngineConfig1"));
                    AssetContainer.ParticleEngineConfig1 = value;
                }
            }
        }
        public int ParticleEngineConfig2
        {
            get => AssetContainer.ParticleEngineConfig2;
            set
            {
                if (AssetContainer.ParticleEngineConfig2 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.ParticleEngineConfig2), AssetContainer, AssetContainer.ParticleEngineConfig2, value, $"{AssetContainer.ContainerAssetType} ParticleEngineConfig2"));
                    AssetContainer.ParticleEngineConfig2 = value;
                }
            }
        }
        public int ParticleEngineConfig3
        {
            get => AssetContainer.ParticleEngineConfig3;
            set
            {
                if (AssetContainer.ParticleEngineConfig3 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.ParticleEngineConfig3), AssetContainer, AssetContainer.ParticleEngineConfig3, value, $"{AssetContainer.ContainerAssetType} ParticleEngineConfig3"));
                    AssetContainer.ParticleEngineConfig3 = value;
                }
            }
        }
        public bool AutoGenerate
        {
            get => AssetContainer.AutoConfiguration;
            set
            {
                if (AssetContainer.AutoConfiguration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(AssetContainer.AutoConfiguration), AssetContainer, AssetContainer.AutoConfiguration, value, $"{AssetContainer.ContainerAssetType} AutoConfiguration"));
                    AssetContainer.AutoConfiguration = value;
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
            RaisePropertyChanged(() => PoolSize);
            RaisePropertyChanged(() => ParticleEngineConfig1);
            RaisePropertyChanged(() => ParticleEngineConfig2);
            RaisePropertyChanged(() => ParticleEngineConfig3);
            RaisePropertyChanged(() => AutoGenerate);
        }

    }
}