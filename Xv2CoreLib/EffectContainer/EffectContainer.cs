using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMO;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EffectContainer
{
    public enum SaveFormat
    {
        EEPK,
        VfxPackage
    }

    [Serializable]
    public class EffectContainerFile : INotifyPropertyChanged, IIsNull
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public static readonly List<string> SupportedFormats = new List<string>()
        {
            ".emp",
            ".etr",
            ".ecf",
            ".emo",
            ".ema",
            ".emm",
            ".emb",
            ".obj",
            ".mat",
            ".light"
        };
        public const string ZipExtension = ".vfxpackage";

        //Format
        private SaveFormat _saveFormat = SaveFormat.EEPK;
        public SaveFormat saveFormat
        {
            get => _saveFormat;
            set
            {
                if (value != _saveFormat)
                {
                    _saveFormat = value;
                    NotifyPropertyChanged(nameof(Name));
                    NotifyPropertyChanged(nameof(DisplayName));
                    NotifyPropertyChanged(nameof(saveFormat));
                }
            }
        }
        public VfxPackageExtension VfxPackageExtension { get; set; } = new VfxPackageExtension();

        //Name/Directory 
        private string _name = null;
        private string _directory = null;
        public bool NameListApplied = false;

        /// <summary>
        /// The name of the EEPK file, minus the extension.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged(nameof(Name));
                    NotifyPropertyChanged(nameof(DisplayName));
                }
            }
        }
        /// <summary>
        /// The directory the EEPK is saved at. If in ZIP save mode, then this is the path to the ZIP file, minus the extension.
        /// </summary>
        public string Directory
        {
            get => _directory;
            set
            {
                if (value != _directory)
                {
                    _directory = value;
                    NotifyPropertyChanged(nameof(Directory));
                    NotifyPropertyChanged(nameof(DisplayName));
                    NotifyPropertyChanged(nameof(CanSave));
                }
            }
        }
        public string DisplayName
        {
            get
            {
                string hasDir = (String.IsNullOrWhiteSpace(Directory)) ? null : "/";
                if (Path.IsPathRooted(Directory))
                {
                    return (saveFormat == SaveFormat.EEPK) ? String.Format("_{0}{1}{2}.eepk", Directory, hasDir, Name) : String.Format("_{0}{1}", Directory, ZipExtension);
                }
                else
                {
                    return (saveFormat == SaveFormat.EEPK) ? String.Format("<_{0}{1}{2}.eepk>", Directory, hasDir, Name) : String.Format("<_{0}{1}>", Directory, ZipExtension);
                }
            }
        }
        public bool CanSave => Path.IsPathRooted(Directory);
        public string FullFilePath
        {
            get
            {
                if (saveFormat == SaveFormat.VfxPackage)
                {
                    return string.Format("{0}{1}", Directory, ZipExtension);
                }
                else
                {
                    return string.Format("{0}/{1}.eepk", Directory, Name);
                }
            }
        }

        private VersionEnum _versionValue = 0;
        public VersionEnum Version
        {
            get
            {
                return this._versionValue;
            }
            set
            {
                if (value != this._versionValue)
                {
                    this._versionValue = value;
                    NotifyPropertyChanged(nameof(Version));
                }
            }
        }

        //For tracking files that were loaded with this EEPK (disabled for ZIP format)
        public List<string> LoadedExternalFiles = new List<string>();

        //For tracking files that were loaded with this EEPK, but weren't accounted for when saving last (which would happen if they were deleted, for example)
        public List<string> LoadedExternalFilesNotSaved = new List<string>();

        //File IO
        [NonSerialized]
        private Xv2FileIO xv2FileIO = null;
        private bool OnlyLoadFromCpk = false;
        [NonSerialized]
        private ZipReader zipReader = null;
        [NonSerialized]
        private ZipWriter zipWriter = null;

        //Asset containers
        public AssetContainerTool Pbind { get; set; }
        public AssetContainerTool Tbind { get; set; }
        public AssetContainerTool Cbind { get; set; }
        public AssetContainerTool Emo { get; set; }
        public AssetContainerTool LightEma { get; set; }

        //Effects
        public AsyncObservableCollection<Effect> Effects { get; set; }

        #region UiProperties
        //Filters
        [NonSerialized]
        private string _effectSearchFilter = null;
        public string EffectSearchFilter
        {
            get => _effectSearchFilter;
            set
            {
                if (value != _effectSearchFilter)
                {
                    _effectSearchFilter = value;
                    NotifyPropertyChanged(nameof(EffectSearchFilter));
                }
            }
        }

        [NonSerialized]
        private ListCollectionView _viewEffects = null;
        public ListCollectionView ViewEffects
        {
            get
            {
                if (_viewEffects != null)
                {
                    return _viewEffects;
                }
                _viewEffects = new ListCollectionView(Effects.Binding);
                _viewEffects.Filter = new Predicate<object>(EffectFilterCheck);
                return _viewEffects;
            }
            set
            {
                if (value != _viewEffects)
                {
                    _viewEffects = value;
                    NotifyPropertyChanged(nameof(ViewEffects));
                }
            }
        }

        public bool EffectFilterCheck(object effect)
        {
            if (String.IsNullOrWhiteSpace(EffectSearchFilter)) return true;
            var _effect = effect as Effect;
            string flattenedSearchParam = EffectSearchFilter.ToLower();

            if (_effect != null)
            {
                ushort ret = 0;
                if (ushort.TryParse(EffectSearchFilter, out ret))
                {
                    //Search is for a number, so look for the effect ID
                    if (_effect.IndexNum.ToString().Contains(EffectSearchFilter)) return true;
                }

                //Search is for either a string or number but is not an Effect ID, so look for EffectPart asset names and Effect Name/Description (from namelist)
                if (!String.IsNullOrWhiteSpace(_effect.NameList) && _effect.NameList.ToLower().Contains(flattenedSearchParam))
                {
                    return true;
                }

                if (_effect.EffectParts == null) return false;

                foreach (var effectPart in _effect.EffectParts)
                {
                    if (effectPart.AssetRefDetails.ToLower().Contains(flattenedSearchParam)) return true;
                }

            }

            return false;
        }

        public void NewEffectFilter(string newFilter)
        {
            EffectSearchFilter = newFilter;
        }

        public void UpdateEffectFilter()
        {
            if (_viewEffects == null)
                _viewEffects = new ListCollectionView(Effects.Binding);

            _viewEffects.CommitEdit();
            _viewEffects.SortDescriptions.Add(new SortDescription(nameof(Effect.IndexNum), ListSortDirection.Ascending));
            _viewEffects.Filter = new Predicate<object>(EffectFilterCheck);
            NotifyPropertyChanged(nameof(ViewEffects));
        }

        public void UpdateAllFilters()
        {
            UpdateEffectFilter();
            Pbind.UpdateAssetFilter();
            Tbind.UpdateAssetFilter();
            Cbind.UpdateAssetFilter();
            LightEma.UpdateAssetFilter();
            Emo.UpdateAssetFilter();
        }
        #endregion

        #region UndoableFunctions
        /// <summary>
        /// Add effects as an undoable operation.
        /// </summary>
        /// <param name="wasForPaste">Is this a part of a copy-paste operation? (Used for the undo description)</param>
        public void UndoableAddEffects(IList<Effect> effects, bool wasForPaste = false)
        {
            List<IUndoRedo> undos = AddEffects(effects);
            undos.Add(new UndoActionDelegate(this, nameof(UpdateEffectFilter), true));
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, (wasForPaste) ? "Paste Effects" : "Add Effects"));
        }

        public void UndoableAddAsset(Asset assetToAdd, AssetType type)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            AddAsset(assetToAdd, type, undos);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Add Asset"));
        }

        #endregion

        #region AddFunctions
        public Asset AssetExists(Asset asset, AssetType type)
        {
            switch (type)
            {
                case AssetType.EMO:
                    return Emo.AssetExists(asset);
                case AssetType.PBIND:
                    return Pbind.AssetExists(asset);
                case AssetType.TBIND:
                    return Tbind.AssetExists(asset);
                case AssetType.LIGHT:
                    return LightEma.AssetExists(asset);
                case AssetType.CBIND:
                    return Cbind.AssetExists(asset);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Add a asset. Will return an identical asset if found.
        /// </summary>
        public Asset AddAsset(Asset assetToAdd, AssetType type)
        {
            return AddAsset(assetToAdd, type, new List<IUndoRedo>());
        }

        public Asset AddAsset(Asset assetToAdd, AssetType type, List<IUndoRedo> undos)
        {
            Asset existingAsset = AssetExists(assetToAdd, type);
            if (existingAsset == null)
            {
                //Asset doesn't exist in the current eppk, so we need to add it.

                switch (type)
                {
                    case AssetType.EMO:
                        Emo.AddAsset(assetToAdd, undos);
                        break;
                    case AssetType.PBIND:
                        Pbind.AddAsset(assetToAdd, undos);
                        break;
                    case AssetType.TBIND:
                        Tbind.AddAsset(assetToAdd, undos);
                        break;
                    case AssetType.LIGHT:
                        LightEma.AddAsset(assetToAdd, undos);
                        break;
                    case AssetType.CBIND:
                        Cbind.AddAsset(assetToAdd, undos);
                        break;
                }

                existingAsset = assetToAdd;
            }

            //In the future this method may be expanded to do more advanced comparisons instead of a simple object check 
            //(which would eliminate duplicates, like what is done for textures/materials) but for now we will go for this
            //basic implementation.
            return existingAsset;
        }

        public List<IUndoRedo> AddEffect(Effect effect, bool allowNullAssets = false, bool addUndoDelegate = false)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Add the assets used
            foreach (EffectPart effectPart in effect.EffectParts)
            {
                if (effectPart.AssetRef == null && allowNullAssets)
                {
                    //Null and its allowed, so OK
                }
                if (effectPart.AssetRef != null)
                {
                    effectPart.AssetRef = AddAsset(effectPart.AssetRef, effectPart.AssetType, undos);
                }
                else if (!allowNullAssets)
                {
                    throw new NullReferenceException(String.Format("AddEffect: Effect {0} contains an EffectPart with a null asset reference. Cannot continue.", effect.IndexNum));
                }
            }

            //Add the effect
            Effects.Add(effect);
            Effects.Sort((x, y) => x.IndexNum - y.IndexNum);
            undos.Add(new UndoableListAdd<Effect>(Effects, effect));

            if (addUndoDelegate)
            {
                undos.Add(new UndoActionDelegate(this, nameof(UpdateEffectFilter), true));
                UpdateEffectFilter();
            }

            return undos;
        }

        public List<IUndoRedo> AddEffects(IList<Effect> effects, bool allowNullAssets = false)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var effect in effects)
            {
                undos.AddRange(AddEffect(effect, allowNullAssets));
            }

            return undos;
        }
        #endregion

        #region LoadSaveFunctions
        public static EffectContainerFile New()
        {
            var newFile = new EffectContainerFile()
            {
                Name = "NewEepk",
                Directory = String.Empty
            };

            newFile.Pbind = newFile.GetDefaultContainer(AssetType.PBIND);
            newFile.Tbind = newFile.GetDefaultContainer(AssetType.TBIND);
            newFile.Cbind = newFile.GetDefaultContainer(AssetType.CBIND);
            newFile.Emo = newFile.GetDefaultContainer(AssetType.EMO);
            newFile.LightEma = newFile.GetDefaultContainer(AssetType.LIGHT);
            newFile.Effects = AsyncObservableCollection<Effect>.Create();
            newFile.Version = VersionEnum.DBXV2;

            return newFile;
        }

        //Load
        /// <summary>
        /// Load an eepk + assets from disk.
        /// </summary>
        /// <param name="path">An absolute path to the eepk.</param>
        /// <returns></returns>
        public static EffectContainerFile Load(string path)
        {
            return Load(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), null, false, null, SaveFormat.EEPK);
        }

        /// <summary>
        /// Load an eepk + assets from the game.
        /// </summary>
        /// <param name="path">A relative path from the game data folder.</param>
        /// <param name="_fileIO">The Xv2FileIO object to load from.</param>
        /// <returns></returns>
        public static EffectContainerFile Load(string path, Resource.Xv2FileIO _fileIO, bool onlyFromCpk)
        {
            return Load(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), _fileIO, onlyFromCpk, null, SaveFormat.EEPK);
        }

        /// <summary>
        /// Load an eepk + assets from a ZipArchive.
        /// </summary>
        /// <param name="path">An absolute path to the VFX2 zip file.</param>
        /// <param name="zipReader">The ZipReader that contains the EEPK and asset files.</param>
        /// <returns></returns>
        public static EffectContainerFile Load(string path, Resource.ZipReader zipReader)
        {
            string eepkPath = zipReader.GetPathWithExtension(".eepk");
            if (eepkPath == null) throw new InvalidDataException("EffectContainerFile.Load(string path, ZipReader zipReader): The vfx2 file does not contain a .eepk file.");
            string dir = string.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

            return Load(dir, Path.GetFileNameWithoutExtension(eepkPath), null, false, zipReader, SaveFormat.VfxPackage);
        }

        /// <summary>
        /// Load an eepk + assets. (Direct load, from game or from zip... depending on parameters)
        /// </summary>
        /// <param name="path">An absolute path to the eepk or a relative path from the game data folder (used with _fileIO).</param>
        /// <param name="_fileIO">Pass this in if loading from the game. Leave null if loading a eepk directly.</param>
        /// <returns></returns>
        private static EffectContainerFile Load(string dir, string name, Resource.Xv2FileIO _fileIO = null, bool onlyFromCpk = false, Resource.ZipReader _zipReader = null, SaveFormat _saveFormat = SaveFormat.EEPK)
        {

            //Path shouldn't include directory if its loading from a zip file
            string eepkPath = (_zipReader == null) ? string.Format("{0}/{1}.eepk", dir, name) : name + ".eepk";

            //Files can be loaded directly, from game (cpk/data folder, using Xv2FileIO), or from a Zip file (using ZipReader)
            EEPK_File eepkFile = EEPK_File.LoadEepk(GetFile(eepkPath, _fileIO, onlyFromCpk, _zipReader));
            EffectContainerFile effectContainerFile = CreateEffectContainerFile(eepkFile);
            effectContainerFile.Name = name;
            effectContainerFile.Directory = dir;
            effectContainerFile.xv2FileIO = _fileIO;
            effectContainerFile.OnlyLoadFromCpk = onlyFromCpk;
            effectContainerFile.zipReader = _zipReader;
            effectContainerFile.saveFormat = _saveFormat;

            //Load the container files. If they dont exist, we will create a dummy entry.
            //PBIND
            if (effectContainerFile.Pbind != null)
            {
                effectContainerFile.Pbind = effectContainerFile.LoadAssetContainer(effectContainerFile.Pbind, AssetType.PBIND);
            }
            else
            {
                effectContainerFile.Pbind = effectContainerFile.GetDefaultContainer(AssetType.PBIND);
            }

            //TBIND
            if (effectContainerFile.Tbind != null)
            {
                effectContainerFile.Tbind = effectContainerFile.LoadAssetContainer(effectContainerFile.Tbind, AssetType.TBIND);
            }
            else
            {
                effectContainerFile.Tbind = effectContainerFile.GetDefaultContainer(AssetType.TBIND);
            }

            //CBIND
            if (effectContainerFile.Cbind != null)
            {
                effectContainerFile.Cbind = effectContainerFile.LoadAssetContainer(effectContainerFile.Cbind, AssetType.CBIND);
            }
            else
            {
                effectContainerFile.Cbind = effectContainerFile.GetDefaultContainer(AssetType.CBIND);
            }

            //EMO
            if (effectContainerFile.Emo != null)
            {
                effectContainerFile.Emo = effectContainerFile.LoadAssetContainer(effectContainerFile.Emo, AssetType.EMO);
            }
            else
            {
                effectContainerFile.Emo = effectContainerFile.GetDefaultContainer(AssetType.EMO);
            }

            //LIGHT.EMA
            if (effectContainerFile.LightEma != null)
            {
                effectContainerFile.LightEma = effectContainerFile.LoadAssetContainer(effectContainerFile.LightEma, AssetType.LIGHT);
            }
            else
            {
                effectContainerFile.LightEma = effectContainerFile.GetDefaultContainer(AssetType.LIGHT);
            }

            //If SDBH, then we need to convert the container into the global EMM/EMB format that XV2 uses.
            if ((effectContainerFile.Version == VersionEnum.SDBH || effectContainerFile.Version == (VersionEnum)1))
            {
                effectContainerFile.LoadSDBHPBINDContainer();
            }
            else
            {
                effectContainerFile.PbindLinkTextureAndMaterial();
            }

            effectContainerFile.TbindLinkTextureAndMaterial();
            effectContainerFile.EffectLinkAssets();

            //Validate the texture containers
            effectContainerFile.ValidateTextureContainers(AssetType.PBIND);
            effectContainerFile.ValidateTextureContainers(AssetType.TBIND);

            //Load VfxPackageExtension
            if (_saveFormat == SaveFormat.VfxPackage)
            {
                effectContainerFile.VfxPackageExtension = VfxPackageExtension.Load(_zipReader, effectContainerFile);
            }

            return effectContainerFile;
        }

        public static EffectContainerFile LoadVfxPackage(Stream stream, string path)
        {
            using (ZipReader reader = new ZipReader(new ZipArchive(stream, ZipArchiveMode.Read)))
            {
                var vfxFile = Load(path, reader);
                vfxFile.zipReader = null;
                return vfxFile;
            }
        }

        public static EffectContainerFile LoadVfxPackage(string path)
        {
            using (ZipReader reader = new ZipReader(ZipFile.Open(path, ZipArchiveMode.Read)))
            {
                var vfxFile = Load(path, reader);
                vfxFile.zipReader = null;
                return vfxFile;
            }
        }

        private AssetContainerTool LoadAssetContainer(AssetContainerTool container, AssetType type)
        {
            //Load the EMBs and EMMs

            //SPECIAL NOTE FOR SDBH EEPKS: Global PBIND EMM/EMBs are optional. If path is NULL, then it will use a default name 
            //(eepk name + _PIC.emb/_MTL.emm), and if that don't exist, then it simply wont load a global emm/emb.
            //In addition to this, each EMP can have its own EMM and EMB file (emp name + .emm/.emb).

            switch (type)
            {
                case AssetType.PBIND:
                case AssetType.TBIND:
                    if (!container.LooseFiles)
                    {
                        container.File1_Ref = EMB_File.LoadEmb(LoadExternalFile(string.Format("{0}/{1}", Directory, container.File1_Name)));
                    }

                    if ((Version == VersionEnum.SDBH || Version == (VersionEnum)1) && type == AssetType.PBIND)
                    {
                        //SDBH handles container names differently.
                        //Sometimes the actual names for the material emm and texture emb are not declared, and so we must calculate them.
                        //And secondly, global EMM/EMBs are optional
                        //DBXV2 does NOT support this

                        string emm = CalculateSDBHPBINDContainerName_EMM(container.File2_Name);
                        string emb = CalculateSDBHPBINDContainerName_EMB(container.File3_Name);

                        if (!string.IsNullOrWhiteSpace(emm))
                        {
                            container.File2_Name = Path.GetFileName(emm);
                            container.File2_Ref = EMM_File.LoadEmm(LoadExternalFile(emm));
                        }
                        else
                        {
                            container.File2_Name = string.Format("{0}_MTL.emm", Name);
                            container.File2_Ref = EMM_File.DefaultEmmFile();
                        }

                        if (!string.IsNullOrWhiteSpace(emb))
                        {
                            container.File3_Name = Path.GetFileName(emb);
                            container.File3_Ref = EMB_File.LoadEmb(LoadExternalFile(emb));
                        }
                        else
                        {
                            container.File3_Name = string.Format("{0}_PIC.emb", Name);
                            container.File3_Ref = EMB_File.DefaultEmbFile(true);
                        }

                    }
                    else
                    {
                        //Regular XV2 EEPK
                        container.File2_Ref = EMM_File.LoadEmm(LoadExternalFile(string.Format("{0}/{1}", Directory, container.File2_Name)));
                        container.File3_Ref = EMB_File.LoadEmb(LoadExternalFile(string.Format("{0}/{1}", Directory, container.File3_Name)));
                    }
                    break;
                case AssetType.CBIND:
                case AssetType.LIGHT:
                    if (!container.LooseFiles)
                    {
                        container.File1_Ref = EMB_File.LoadEmb(LoadExternalFile(string.Format("{0}/{1}", Directory, container.File1_Name)));
                    }
                    break;
                case AssetType.EMO:
                    break;
                default:
                    throw new InvalidOperationException(string.Format("LoadAssetContainer: Unrecognized asset type: {0}", type));
            }

            //Load the actual assets and link them (if needed)
            for (int i = 0; i < container.Assets.Count; i++)
            {
                EMM_File emmFile = null;
                EMA_File matEmaFile = null;

                foreach (var file in container.Assets[i].Files)
                {
                    if (file.FullFileName != "NULL")
                    {
                        //Get the file bytes
                        byte[] fileBytes = null;

                        if (container.LooseFiles || type == AssetType.EMO)
                        {
                            fileBytes = LoadExternalFile(string.Format("{0}/{1}", Directory, file.FullFileName));
                        }
                        else
                        {
                            var entry = container.File1_Ref.GetEntry(i);
                            if (entry == null) throw new FileNotFoundException(string.Format("Could not find file \"{0}\" in \"{1}\".\n\nThis is possibly caused by a corrupted eepk file.", file.FullFileName, container.File1_Name));

                            fileBytes = entry.Data;
                        }


                        switch (file.Extension)
                        {
                            case ".emp":
                                file.EmpFile = EMP_File.Load(fileBytes, EepkToolInterlop.FullDecompile);
                                file.fileType = EffectFile.FileType.EMP;
                                break;
                            case ".etr":
                                file.EtrFile = ETR_File.Load(fileBytes);
                                file.fileType = EffectFile.FileType.ETR;
                                break;
                            case ".ecf":
                                file.EcfFile = ECF_File.Load(fileBytes);
                                file.fileType = EffectFile.FileType.ECF;
                                break;
                            case ".emb":
                                file.EmbFile = EMB_File.LoadEmb(fileBytes);
                                file.fileType = EffectFile.FileType.EMB;
                                break;
                            case ".emm":
                                file.EmmFile = EMM_File.LoadEmm(fileBytes);
                                file.fileType = EffectFile.FileType.EMM;
                                emmFile = file.EmmFile;
                                break;
                            case ".mat.ema":
                            case ".light.ema":
                            case ".obj.ema":
                            case ".ema":
                            ema:
                                if (EepkToolInterlop.FullDecompile)
                                {
                                    file.EmaFile = EMA_File.Load(fileBytes);

                                    if (file.EmaFile.EmaType == EmaType.mat)
                                        matEmaFile = file.EmaFile;
                                }
                                else
                                {
                                    file.Bytes = fileBytes;
                                }

                                file.fileType = EffectFile.FileType.EMA;
                                break;
                            case ".emo":
                                if (EepkToolInterlop.FullDecompile)
                                {
                                    file.EmoFile = EMO_File.Load(fileBytes);
                                }
                                else
                                {
                                    file.Bytes = fileBytes;
                                }

                                file.fileType = EffectFile.FileType.EMO;
                                break;
                            default:
                                if (file.Extension.Contains(".ema")) goto ema;
                                file.Bytes = fileBytes;
                                file.fileType = EffectFile.FileType.Other;
                                break;
                        }

                    }

                }

                //MAT.EMA material name linkage
                if(matEmaFile != null && emmFile != null)
                {
                    matEmaFile.FixMaterialNames(emmFile);
                }
            }

            //Regenerate asset names (to solve any name conflict user-errors)
            if (type == AssetType.PBIND || type == AssetType.TBIND)
            {
                container.File2_Ref.ValidateNames();
                container.File3_Ref.ValidateNames();
            }
            container.ValidateAssetNames();

            return container;
        }

        /// <summary>
        /// Load a file either from disk or from the game. 
        /// </summary>
        /// <param name="path">If fileIO is null, then this a abosolute path, otherwise it is relative to the game data folder.</param>
        /// <returns></returns>
        private static byte[] GetFile(string path, Resource.Xv2FileIO fileIO, bool onlyFromCpk, Resource.ZipReader zipReader)
        {
            byte[] bytes = null;

            try
            {
                if (fileIO == null && zipReader == null)
                {
                    bytes = File.ReadAllBytes(path);
                }
                else if (zipReader != null)
                {
                    bytes = zipReader.GetFileFromArchive(Path.GetFileName(path));
                }
                else
                {
                    if (onlyFromCpk)
                    {
                        bytes = fileIO.GetFileFromCpk(path);
                    }
                    else
                    {
                        bytes = fileIO.GetFileFromGame(path);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new FileLoadException(String.Format("An error occured while loading the file \"{0}\".", ex.Message), ex);
            }

            if (bytes == null)
            {
                throw new FileNotFoundException(string.Format("The file \"{0}\" could not be found.", path));
            }

            return bytes;
        }

        //Save
        public bool SaveVfxPackage()
        {
            if (saveFormat != SaveFormat.VfxPackage) throw new InvalidOperationException("SaveVfxPackage: SaveFormat is not set to VfxPackage.");

            bool result;
            string vfxPackagePath = string.Format("{0}{1}", Directory, ZipExtension);
            string tempVfxPackagePath = vfxPackagePath + "_temp";

            if (File.Exists(tempVfxPackagePath))
                File.Delete(tempVfxPackagePath);

            using (ZipWriter writer = new ZipWriter(ZipFile.Open(tempVfxPackagePath, ZipArchiveMode.Update)))
            {
                zipWriter = writer;
                result = Save();
                VfxPackageExtension.Save(writer, this);
                zipWriter = null;
            }

            //Overwrite original vfxPackage with the newly created one
            if (File.Exists(vfxPackagePath))
                File.Delete(vfxPackagePath);

            File.Move(tempVfxPackagePath, vfxPackagePath);

            return result;
        }

        public bool Save()
        {
            //Convert all loaded dds textures back into a byte array
            Pbind.File3_Ref.SaveDdsImages();
            Tbind.File3_Ref.SaveDdsImages();
            SaveEmoDdsFiles();
            ValidateContainerFileNames();

            RemoveNullEffectParts();
            InitExternalLoadedFileNotSavedList();
            SetVersion();

            PbindSetTextureAndMaterialIndex();
            TbindSetTextureAndMaterialIndex();
            EffectSetAssetIndex();
            FinalizeMainEmb();

            //Save eepk
            EEPK_File eepkFile = CreateEepk();
            SaveFile(eepkFile.SaveToBytes(), string.Format("{0}/{1}.eepk", Directory, Name));

            //Save containers
            //If a container has no assets, it will be ignored. The binary eepk also omits it.
            if (Pbind.Assets.Count > 0)
            {
                if (!Pbind.LooseFiles)
                {
                    ExternalFileSaved(string.Format("{0}/{1}", Directory, Pbind.File1_Name));
                    SaveFile(Pbind.File1_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Pbind.File1_Name));
                }
                ExternalFileSaved(string.Format("{0}/{1}", Directory, Pbind.File2_Name));
                ExternalFileSaved(string.Format("{0}/{1}", Directory, Pbind.File3_Name));
                SaveFile(Pbind.File2_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Pbind.File2_Name));
                SaveFile(Pbind.File3_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Pbind.File3_Name));

                //Lose files
                if (Pbind.LooseFiles)
                {
                    foreach (Asset asset in Pbind.Assets)
                    {
                        ExternalFileSaved(String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EmpFile.SaveToBytes(), string.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }

            }

            if (Tbind.Assets.Count > 0)
            {
                if (!Tbind.LooseFiles)
                {
                    ExternalFileSaved(string.Format("{0}/{1}", Directory, Tbind.File1_Name));
                    SaveFile(Tbind.File1_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Tbind.File1_Name));
                }
                ExternalFileSaved(string.Format("{0}/{1}", Directory, Tbind.File2_Name));
                ExternalFileSaved(string.Format("{0}/{1}", Directory, Tbind.File3_Name));
                SaveFile(Tbind.File2_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Tbind.File2_Name));
                SaveFile(Tbind.File3_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Tbind.File3_Name));

                //Lose files
                if (Tbind.LooseFiles)
                {
                    foreach (Asset asset in Tbind.Assets)
                    {
                        ExternalFileSaved(string.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EtrFile.Write(), string.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }
            }

            if (Cbind.Assets.Count > 0)
            {
                if (!Cbind.LooseFiles)
                {
                    ExternalFileSaved(string.Format("{0}/{1}", Directory, Cbind.File1_Name));
                    SaveFile(Cbind.File1_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, Cbind.File1_Name));
                }

                //Lose files
                if (Cbind.LooseFiles)
                {
                    foreach (Asset asset in Cbind.Assets)
                    {
                        ExternalFileSaved(string.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EcfFile.SaveToBytes(), string.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }
            }

            if (Emo.Assets.Count > 0)
            {
                if (Emo.LooseFiles)
                {
                    foreach (Asset asset in Emo.Assets)
                    {
                        foreach (var file in asset.Files)
                        {
                            if (file.HasValidData() && file.FullFileName != "NULL")
                            {
                                ExternalFileSaved(string.Format("{0}/{1}", Directory, file.FullFileName));

                                switch (file.fileType)
                                {
                                    case EffectFile.FileType.EMM:
                                        SaveFile(file.EmmFile.SaveToBytes(), string.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    case EffectFile.FileType.EMB:
                                        SaveFile(file.EmbFile.SaveToBytes(), string.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    case EffectFile.FileType.EMA:
                                        if (!EepkToolInterlop.FullDecompile) goto default;

                                        //Link material animation nodes to the actual materials
                                        if(file.EmaFile.EmaType == EmaType.mat)
                                        {
                                            EffectFile _emmFile = asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMM);

                                            if(_emmFile?.EmmFile != null)
                                                file.EmaFile.CreateMaterialSkeleton(_emmFile.EmmFile);
                                        }

                                        SaveFile(file.EmaFile.Write(), string.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    case EffectFile.FileType.EMO:
                                        if (!EepkToolInterlop.FullDecompile) goto default;
                                        SaveFile(file.EmoFile.Write(), string.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    default:
                                        SaveFile(file.Bytes, string.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("EMO AssetContainer must be set to LooseFiles mode.");
                }
            }

            if (LightEma.Assets.Count > 0)
            {
                if (LightEma.LooseFiles)
                {
                    foreach (Asset asset in LightEma.Assets)
                    {
                        foreach (EffectFile file in asset.Files)
                        {
                            if (file.fileType == EffectFile.FileType.EMA && file.FullFileName != "NULL")
                            {
                                ExternalFileSaved(string.Format("{0}/{1}", Directory, file.FullFileName));
                                SaveFile(file.EmaFile != null ? file.EmaFile.Write() : file.Bytes, string.Format("{0}/{1}", Directory, file.FullFileName));
                            }
                        }
                    }
                }
                else if (!LightEma.LooseFiles)
                {
                    ExternalFileSaved(string.Format("{0}/{1}", Directory, LightEma.File1_Name));
                    SaveFile(LightEma.File1_Ref.SaveToBytes(), string.Format("{0}/{1}", Directory, LightEma.File1_Name));
                }
            }

            return true;
        }

        private void FinalizeMainEmb()
        {
            if (!Pbind.LooseFiles)
            {
                Pbind.File1_Ref = EMB_File.DefaultEmbFile(false);
                Pbind.File1_Ref.UseFileNames = true;

                foreach (var asset in Pbind.Assets)
                {
                    byte[] bytes = asset.Files[0].EmpFile.SaveToBytes();
                    Pbind.File1_Ref.Entry.Add(new EmbEntry()
                    {
                        Data = bytes,
                        Name = asset.Files[0].FullFileName
                    });
                }
            }

            if (!Tbind.LooseFiles)
            {
                Tbind.File1_Ref = EMB_File.DefaultEmbFile(false);
                Tbind.File1_Ref.UseFileNames = true;

                foreach (var asset in Tbind.Assets)
                {
                    byte[] bytes = asset.Files[0].EtrFile.Write();
                    Tbind.File1_Ref.Entry.Add(new EmbEntry()
                    {
                        Data = bytes,
                        Name = asset.Files[0].FullFileName
                    });
                }
            }

            if (!Cbind.LooseFiles)
            {
                Cbind.File1_Ref = EMB_File.DefaultEmbFile(false);
                Cbind.File1_Ref.UseFileNames = true;

                foreach (Asset asset in Cbind.Assets)
                {
                    byte[] bytes = asset.Files[0].EcfFile.SaveToBytes();
                    Cbind.File1_Ref.Entry.Add(new EmbEntry()
                    {
                        Data = bytes,
                        Name = asset.Files[0].FullFileName
                    });
                }
            }

            if (!LightEma.LooseFiles)
            {
                LightEma.File1_Ref = EMB_File.DefaultEmbFile(false);
                LightEma.File1_Ref.UseFileNames = true;

                foreach (var asset in LightEma.Assets)
                {
                    if (asset.Files[0].EmaFile != null)
                    {
                        LightEma.File1_Ref.Entry.Add(new EmbEntry()
                        {
                            Data = asset.Files[0].EmaFile.Write(),
                            Name = asset.Files[0].FullFileName
                        });
                    }
                    else
                    {
                        byte[] bytes = asset.Files[0].Bytes;
                        LightEma.File1_Ref.Entry.Add(new EmbEntry()
                        {
                            Data = bytes,
                            Name = asset.Files[0].FullFileName
                        });
                    }
                }
            }


        }

        private void ValidateContainerFileNames()
        {
            //Apparantly long paths (128 characters and above) cannot be read by the game (or patcher?), at least for EEPKs. If a container file has such a long path the game will just hang as it cant find the file.
            //So... best to ensure we dont rename our containers if the final path is gonna be too long for the game to even load
            bool allowContainerRename = ($"{Directory}/{Name}.xxxxx.xxx".Length >= 128 ? false : true) && EepkToolInterlop.AutoRenameContainers;

            //Ensure that all containers have a valid name
            //PBIND
            if (Pbind.File2_Name == "NULL" || allowContainerRename)
                Pbind.File2_Name = String.Format("{0}.ptcl.emm", GetDefaultEepkName());

            if (Pbind.File3_Name == "NULL" || allowContainerRename)
                Pbind.File3_Name = String.Format("{0}.ptcl.emb", GetDefaultEepkName());

            if (Pbind.File1_Name == "NULL" && !Pbind.LooseFiles)
            {
                Pbind.File1_Name = String.Format("{0}.pbind.emb", GetDefaultEepkName());
            }
            else if (!Pbind.LooseFiles && allowContainerRename)
            {
                Pbind.File1_Name = String.Format("{0}.pbind.emb", GetDefaultEepkName());
            }
            else if (Pbind.LooseFiles)
            {
                Pbind.File1_Name = "NULL";
            }

            //TBIND
            if (Tbind.File2_Name == "NULL" || allowContainerRename)
                Tbind.File2_Name = String.Format("{0}.trc.emm", GetDefaultEepkName());

            if (Tbind.File3_Name == "NULL" || allowContainerRename)
                Tbind.File3_Name = String.Format("{0}.trc.emb", GetDefaultEepkName());

            if (Tbind.File1_Name == "NULL" && !Tbind.LooseFiles)
            {
                Tbind.File1_Name = String.Format("{0}.tbind.emb", GetDefaultEepkName());
            }
            else if (!Tbind.LooseFiles && allowContainerRename)
            {
                Tbind.File1_Name = String.Format("{0}.tbind.emb", GetDefaultEepkName());
            }
            else if (Tbind.LooseFiles)
            {
                Tbind.File1_Name = "NULL";
            }

            //CBIND
            if (Cbind.File1_Name == "NULL" && !Cbind.LooseFiles)
            {
                Cbind.File1_Name = String.Format("{0}.cbind.emb", GetDefaultEepkName());
            }
            else if (!Cbind.LooseFiles && allowContainerRename)
            {
                Cbind.File1_Name = String.Format("{0}.cbind.emb", GetDefaultEepkName());
            }
            else if (Cbind.LooseFiles)
            {
                Cbind.File1_Name = "NULL";
            }

            //LIGHT
            if (LightEma.File1_Name == "NULL" && !LightEma.LooseFiles)
            {
                LightEma.File1_Name = String.Format("{0}.light.emb", GetDefaultEepkName());
            }
            else if (!LightEma.LooseFiles && allowContainerRename)
            {
                LightEma.File1_Name = String.Format("{0}.light.emb", GetDefaultEepkName());
            }
            else if (LightEma.LooseFiles)
            {
                LightEma.File1_Name = "NULL";
            }

        }

        private void SetVersion()
        {
            //Set Version on all applicable files
            //Currently this is only for (DBXV2 > SDBH differences)

            try
            {
                foreach (var asset in Pbind.Assets)
                {
                    if (asset.Files.Count > 0)
                    {
                        if (asset.Files[0].EmpFile != null)
                        {
                            asset.Files[0].EmpFile.Version = Version;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetVersion: {0}", ex.Message));
            }
        }

        public void ChangeFilePath(string fullPath)
        {
            Directory = Path.GetDirectoryName(fullPath);
            Name = (Path.GetExtension(fullPath) == ".eepk") ? Path.GetFileNameWithoutExtension(fullPath) : fullPath;
        }
        #endregion

        #region ExternalAssetFiles
        //Loading
        private byte[] LoadExternalFile(string path, bool log = true)
        {
            if (log && saveFormat != SaveFormat.VfxPackage)
                LoadedExternalFiles.Add(path);

            return GetFile(path, xv2FileIO, OnlyLoadFromCpk, zipReader);
        }

        private void ExternalFileSaved(string path)
        {
            if (saveFormat == SaveFormat.VfxPackage) return;

            LoadedExternalFilesNotSaved.Remove(path);
            LoadedExternalFiles.Add(path);
        }

        private void InitExternalLoadedFileNotSavedList()
        {
            LoadedExternalFilesNotSaved.Clear();

            foreach (var str in LoadedExternalFiles)
            {
                LoadedExternalFilesNotSaved.Add(str);
            }

            LoadedExternalFiles.Clear();
        }

        private bool DoesFileExist(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }

            if (xv2FileIO != null)
            {
                return xv2FileIO.FileExists(path);
            }

            return false;
        }

        //Saving
        private void SaveFile(byte[] bytes, string path)
        {
            if (saveFormat == SaveFormat.EEPK)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);
            }
            else if (saveFormat == SaveFormat.VfxPackage)
            {
                zipWriter.AddFile(Path.GetFileName(path), bytes);
            }
        }
        #endregion

        #region LinkingFunctions
        private void PbindLinkTextureAndMaterial()
        {
            if (Pbind != null)
            {
                int index = 0;
                foreach (var empAsset in Pbind.Assets)
                {
                    //Validation
                    if (empAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find PBIND asset at index {0}.", index));
                    }
                    if (empAsset.Files[0].EmpFile == null)
                    {
                        throw new InvalidDataException(String.Format("The EMP file at index {0} was null (name: {1}).", index, empAsset.Files[0].FullFileName));
                    }

                    EMP_File empFile = empAsset.Files[0].EmpFile;

                    //Materials
                    PbindLinkTextureAndMaterial_Recursive(empFile.ParticleNodes);

                    //Textures
                    foreach (EMP_TextureSamplerDef empTexture in empFile.Textures)
                    {
                        if (empTexture.EmbIndex >= byte.MinValue && empTexture.EmbIndex < sbyte.MaxValue + 1)
                        {
                            empTexture.TextureRef = Pbind.File3_Ref.GetEntry(empTexture.EmbIndex);
                        }
                        else if (empTexture.EmbIndex != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("PbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\nptcl.emb can only have a maximum of 128 textures.", empTexture.EmbIndex));
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void PbindLinkTextureAndMaterial_Recursive(IList<ParticleNode> particleNodes)
        {
            foreach (ParticleNode empEntry in particleNodes)
            {
                if (empEntry.NodeType == ParticleNodeType.Emission)
                {
                    empEntry.EmissionNode.Texture.MaterialRef = (empEntry.EmissionNode.Texture.MaterialID != ushort.MaxValue) ? Pbind.File2_Ref.GetEntry(empEntry.EmissionNode.Texture.MaterialID) : null;
                }

                if (empEntry.ChildParticleNodes != null)
                {
                    PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleNodes);
                }
            }
        }

        private void TbindLinkTextureAndMaterial()
        {
            if (Tbind != null)
            {
                int index = 0;
                foreach (var etrAsset in Tbind.Assets)
                {
                    //Validation
                    if (etrAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find TBIND asset at index {0}.", index));
                    }
                    if (etrAsset.Files[0].EtrFile == null)
                    {
                        throw new InvalidDataException(String.Format("The ETR file at index {0} was null (name: {1}).", index, etrAsset.Files[0].FullFileName));
                    }

                    ETR_File etrFile = etrAsset.Files[0].EtrFile;

                    //Materials
                    foreach (var etrEntry in etrFile.Nodes)
                    {
                        etrEntry.MaterialRef = Tbind.File2_Ref.GetEntry(etrEntry.MaterialID);
                    }

                    //Textures
                    foreach (var etrTexture in etrFile.Textures)
                    {
                        if (etrTexture.EmbIndex >= byte.MinValue && etrTexture.EmbIndex < sbyte.MaxValue + 1)
                        {
                            etrTexture.TextureRef = Tbind.File3_Ref.GetEntry(etrTexture.EmbIndex);
                        }
                        else if (etrTexture.EmbIndex != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("TbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\trc.emb can only have a maximum of 128 textures.", etrTexture.EmbIndex));
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void PbindSetTextureAndMaterialIndex()
        {
            if (Pbind != null)
            {
                int index = 0;
                foreach (var empAsset in Pbind.Assets)
                {
                    //Validation
                    if (empAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find PBIND asset at index {0}.", index));
                    }
                    if (empAsset.Files[0].EmpFile == null)
                    {
                        throw new InvalidDataException(String.Format("The EMP file at index {0} was null (name: {1}).", index, empAsset.Files[0].FullFileName));
                    }

                    EMP_File empFile = empAsset.Files[0].EmpFile;

                    //Materials
                    PbindSetTextureAndMaterialIndex_Recursive(empFile.ParticleNodes);

                    //Textures
                    foreach (var empTexture in empFile.Textures)
                    {
                        if (empTexture.TextureRef != null)
                        {
                            int textureIdx = Pbind.File3_Ref.Entry.IndexOf(empTexture.TextureRef);

                            if (textureIdx == -1)
                            {
                                //A texture is assigned, but it wasn't found in the texture container.
                                throw new InvalidOperationException("PbindSetTextureAndMaterialIndex: texture not found.");
                            }

                            empTexture.EmbIndex = (byte)textureIdx;
                        }
                        else
                        {
                            //No assigned texture. 
                            empTexture.EmbIndex = byte.MaxValue;
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void PbindSetTextureAndMaterialIndex_Recursive(IList<ParticleNode> childrenParticleEffects)
        {
            foreach (ParticleNode empEntry in childrenParticleEffects)
            {
                if (empEntry.NodeType == ParticleNodeType.Emission)
                {
                    int matIdx = Pbind.File2_Ref.Materials.IndexOf(empEntry.EmissionNode.Texture.MaterialRef);

                    if (matIdx == -1 && empEntry.EmissionNode.Texture.MaterialRef != null)
                    {
                        throw new InvalidOperationException("PbindSetTextureAndMaterialIndex_Recursive: material not found.");
                    }

                    empEntry.EmissionNode.Texture.MaterialID = (matIdx != -1) ? (ushort)matIdx : ushort.MaxValue;
                }

                if (empEntry.ChildParticleNodes != null)
                {
                    PbindSetTextureAndMaterialIndex_Recursive(empEntry.ChildParticleNodes);
                }
            }
        }

        private void TbindSetTextureAndMaterialIndex()
        {
            if (Tbind != null)
            {
                int index = 0;
                foreach (var etrAsset in Tbind.Assets)
                {
                    //Validation
                    if (etrAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find TBIND asset at index {0}.", index));
                    }
                    if (etrAsset.Files[0].EtrFile == null)
                    {
                        throw new InvalidDataException(String.Format("The ETR file at index {0} was null (name: {1}).", index, etrAsset.Files[0].FullFileName));
                    }

                    ETR_File etrFile = etrAsset.Files[0].EtrFile;

                    //Materials
                    foreach (var etrEntry in etrFile.Nodes)
                    {
                        int matIdx = Tbind.File2_Ref.Materials.IndexOf(etrEntry.MaterialRef);

                        if (matIdx == -1)
                        {
                            throw new InvalidOperationException("TbindSetTextureAndMaterialIndex: material not found.");
                        }

                        etrEntry.MaterialID = (ushort)matIdx;
                    }

                    //Textures
                    foreach (var etrTexture in etrFile.Textures)
                    {
                        if (etrTexture.TextureRef != null)
                        {
                            int textureIdx = Tbind.File3_Ref.Entry.IndexOf(etrTexture.TextureRef);

                            if (textureIdx == -1)
                            {
                                throw new InvalidOperationException("TbindSetTextureAndMaterialIndex: texture not found.");
                            }

                            etrTexture.EmbIndex = (byte)textureIdx;
                        }
                        else
                        {
                            etrTexture.EmbIndex = byte.MaxValue;
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        //Effect link/sync functions
        /// <summary>
        /// Sets the AssetRef on all EffectParts that corrosponds to the asset index.
        /// </summary>
        private void EffectLinkAssets()
        {
            foreach (var effect in Effects)
            {
                foreach (var effectPart in effect.EffectParts)
                {
                    try
                    {
                        switch (effectPart.AssetType)
                        {
                            case AssetType.EMO:
                                effectPart.AssetRef = Emo.Assets[effectPart.AssetIndex];
                                break;
                            case AssetType.PBIND:
                                effectPart.AssetRef = Pbind.Assets[effectPart.AssetIndex];
                                break;
                            case AssetType.TBIND:
                                effectPart.AssetRef = Tbind.Assets[effectPart.AssetIndex];
                                break;
                            case AssetType.CBIND:
                                effectPart.AssetRef = Cbind.Assets[effectPart.AssetIndex];
                                break;
                            case AssetType.LIGHT:
                                effectPart.AssetRef = LightEma.Assets[effectPart.AssetIndex];
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("An error occured while linking effect {0} with the assets.\n\nDetails: {1}", effect.IndexNum, ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Sets the AssetIndex on all EffectParts that corrosponds to the AssetRef
        /// </summary>
        private void EffectSetAssetIndex()
        {
            //Set/update the assetIndex of all effectParts by getting the current index of the set reference object

            foreach (var effect in Effects)
            {
                foreach (var effectPart in effect.EffectParts)
                {
                    if (effectPart.AssetRef == null)
                    {
                        throw new InvalidOperationException(String.Format("EffectSetAssetIndex: effectPart.AssetRef was null, method cannot proceed in this state."));
                    }

                    switch (effectPart.AssetType)
                    {
                        case AssetType.EMO:
                            effectPart.AssetIndex = (ushort)Emo.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.PBIND:
                            effectPart.AssetIndex = (ushort)Pbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.TBIND:
                            effectPart.AssetIndex = (ushort)Tbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.CBIND:
                            effectPart.AssetIndex = (ushort)Cbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.LIGHT:
                            effectPart.AssetIndex = (ushort)LightEma.Assets.IndexOf(effectPart.AssetRef);
                            break;
                    }

                    if (effectPart.AssetIndex == ushort.MaxValue)
                    {
                        throw new InvalidOperationException(String.Format("EffectSetAssetIndex: index returned -1, method is unable to proceed in this state."));
                    }
                }
            }

        }

        #endregion

        #region ConversionFunctions
        public static EffectContainerFile CreateEffectContainerFile(EEPK_File eepkFile)
        {
            EffectContainerFile effectContainer = new EffectContainerFile();
            effectContainer.Version = (VersionEnum)eepkFile.Version;
            effectContainer.Effects = new AsyncObservableCollection<Effect>(eepkFile.Effects);

            foreach (var container in eepkFile.Assets)
            {
                AssetContainerTool assetContainer = new AssetContainerTool();

                assetContainer.LooseFiles = (container.FILES[0] == "NULL") ? true : false;
                assetContainer.AssetSpawnLimit = container.AssetSpawnLimit;
                assetContainer.I_04 = container.I_04;
                assetContainer.I_05 = container.I_05;
                assetContainer.I_06 = container.I_06;
                assetContainer.I_07 = container.I_07;
                assetContainer.AssetListLimit = container.AssetListLimit;
                assetContainer.I_12 = container.I_12;
                assetContainer.ContainerAssetType = container.I_16;

                assetContainer.File1_Name = container.FILES[0];
                assetContainer.File2_Name = container.FILES[1];
                assetContainer.File3_Name = container.FILES[2];
                assetContainer.Assets = new AsyncObservableCollection<Asset>();

                foreach (var assets in container.AssetEntries)
                {
                    AsyncObservableCollection<EffectFile> files = new AsyncObservableCollection<EffectFile>();

                    foreach (Asset_File filename in assets.FILES)
                    {
                        //IF the file is NULL, then we wont add it
                        //When recreating the EEPK file we will add the nessecary NULL entries once again (up to 5)
                        if (filename.Path != "NULL")
                        {
                            //At this point we wont be loading the assets, just setting the name and type
                            EffectFile.FileType type;

                            switch (EffectFile.GetExtension(filename.Path))
                            {
                                case ".emp":
                                    type = EffectFile.FileType.EMP;
                                    break;
                                case ".ecf":
                                    type = EffectFile.FileType.ECF;
                                    break;
                                case ".etr":
                                    type = EffectFile.FileType.ETR;
                                    break;
                                case ".emb":
                                    type = EffectFile.FileType.EMB;
                                    break;
                                case ".emm":
                                    type = EffectFile.FileType.EMM;
                                    break;
                                case ".light.ema":
                                    type = EffectFile.FileType.EMA;
                                    break;
                                case ".emo":
                                    type = EffectFile.FileType.EMO;
                                    break;
                                default:
                                    type = EffectFile.FileType.Other;
                                    break;
                            }

                            EffectFile effectFile = new EffectFile()
                            {
                                fileType = type
                            };
                            effectFile.SetName(filename.Path);

                            files.Add(effectFile);
                        }
                    }

                    assetContainer.Assets.Add(new Asset()
                    {
                        I_00 = assets.I_00,
                        Files = files,
                        assetType = container.I_16
                    });
                }

                switch (container.I_16)
                {
                    case AssetType.PBIND:
                        effectContainer.Pbind = assetContainer;
                        break;
                    case AssetType.TBIND:
                        effectContainer.Tbind = assetContainer;
                        break;
                    case AssetType.CBIND:
                        effectContainer.Cbind = assetContainer;
                        break;
                    case AssetType.EMO:
                        effectContainer.Emo = assetContainer;
                        break;
                    case AssetType.LIGHT:
                        effectContainer.LightEma = assetContainer;
                        break;
                    default:
                        throw new InvalidDataException(String.Format("Unknown container type: {0}", container.I_16));
                }
            }

            return effectContainer;
        }

        public static EEPK_File CreateEepk(EffectContainerFile effectContainer)
        {
            EEPK_File eepkFile = new EEPK_File();
            eepkFile.Version = (int)effectContainer.Version;
            eepkFile.Assets = new List<AssetContainer>();
            eepkFile.Effects = effectContainer.Effects.ToList();

            if (effectContainer.Emo.Assets.Count > 0)
            {
                eepkFile.Assets.Add(CreateEepkAssetContainer(effectContainer.Emo, AssetType.EMO));
            }

            if (effectContainer.Pbind.Assets.Count > 0)
            {
                eepkFile.Assets.Add(CreateEepkAssetContainer(effectContainer.Pbind, AssetType.PBIND));
            }

            if (effectContainer.Tbind.Assets.Count > 0)
            {
                eepkFile.Assets.Add(CreateEepkAssetContainer(effectContainer.Tbind, AssetType.TBIND));
            }

            if (effectContainer.LightEma.Assets.Count > 0)
            {
                eepkFile.Assets.Add(CreateEepkAssetContainer(effectContainer.LightEma, AssetType.LIGHT));
            }

            if (effectContainer.Cbind.Assets.Count > 0)
            {
                eepkFile.Assets.Add(CreateEepkAssetContainer(effectContainer.Cbind, AssetType.CBIND));
            }

            return eepkFile;
        }

        private static AssetContainer CreateEepkAssetContainer(AssetContainerTool assetContainer, AssetType type)
        {
            AssetContainer newAssetContainer = new AssetContainer();
            newAssetContainer.AssetSpawnLimit = assetContainer.AssetSpawnLimit;
            newAssetContainer.I_04 = assetContainer.I_04;
            newAssetContainer.I_05 = assetContainer.I_05;
            newAssetContainer.I_06 = assetContainer.I_06;
            newAssetContainer.I_07 = assetContainer.I_07;
            newAssetContainer.AssetListLimit = assetContainer.AssetListLimit;
            newAssetContainer.I_12 = assetContainer.I_12;
            newAssetContainer.I_16 = type;

            if (assetContainer.LooseFiles)
            {
                assetContainer.File1_Name = "NULL";
            }

            newAssetContainer.FILES = new string[3] { assetContainer.File1_Name, assetContainer.File2_Name, assetContainer.File3_Name };

            if (assetContainer.Assets != null)
            {
                newAssetContainer.AssetEntries = new List<Asset_Entry>();

                foreach (var asset in assetContainer.Assets)
                {
                    List<Asset_File> files = new List<Asset_File>();

                    for (int i = 0; i < asset.Files.Count; i++)
                    {
                        if (asset.Files[i].FullFileName != "NULL")
                            files.Add(new Asset_File() { Path = asset.Files[i].FullFileName });
                    }

                    newAssetContainer.AssetEntries.Add(new Asset_Entry()
                    {
                        I_00 = asset.I_00,
                        FILES = files
                    });
                }
            }

            return newAssetContainer;
        }

        public EEPK_File CreateEepk()
        {
            return CreateEepk(this);
        }
        #endregion

        #region Helpers
        //Misc
        private AssetContainerTool GetAssetContainer(AssetType type)
        {
            switch (type)
            {
                case AssetType.CBIND:
                    return Cbind;
                case AssetType.TBIND:
                    return Tbind;
                case AssetType.PBIND:
                    return Pbind;
                case AssetType.EMO:
                    return Emo;
                case AssetType.LIGHT:
                    return LightEma;
                default:
                    throw new InvalidOperationException(String.Format("GetAssetContainer: Unrecognized AssetType: {0}", type));
            }

        }

        private AssetContainerTool GetDefaultContainer(AssetType type)
        {
            switch (type)
            {
                case AssetType.PBIND:
                    return new AssetContainerTool()
                    {
                        LooseFiles = false,
                        File1_Ref = EMB_File.DefaultEmbFile(false),
                        File2_Ref = EMM_File.DefaultEmmFile(),
                        File3_Ref = EMB_File.DefaultEmbFile(true),
                        File1_Name = String.Format("{0}.pbind.emb", GetDefaultEepkName()),
                        File2_Name = String.Format("{0}.ptcl.emm", GetDefaultEepkName()),
                        File3_Name = String.Format("{0}.ptcl.emb", GetDefaultEepkName()),
                        Assets = AsyncObservableCollection<Asset>.Create(),
                        ContainerAssetType = AssetType.PBIND
                    };
                case AssetType.TBIND:
                    return new AssetContainerTool()
                    {
                        LooseFiles = false,
                        File1_Ref = EMB_File.DefaultEmbFile(false),
                        File2_Ref = EMM_File.DefaultEmmFile(),
                        File3_Ref = EMB_File.DefaultEmbFile(true),
                        File1_Name = String.Format("{0}.tbind.emb", GetDefaultEepkName()),
                        File2_Name = String.Format("{0}.trc.emm", GetDefaultEepkName()),
                        File3_Name = String.Format("{0}.trc.emb", GetDefaultEepkName()),
                        Assets = AsyncObservableCollection<Asset>.Create(),
                        ContainerAssetType = AssetType.TBIND
                    };
                case AssetType.CBIND:
                    return new AssetContainerTool()
                    {
                        LooseFiles = false,
                        File1_Ref = EMB_File.DefaultEmbFile(false),
                        File1_Name = String.Format("{0}.cbind.emb", GetDefaultEepkName()),
                        File2_Name = "NULL",
                        File3_Name = "NULL",
                        Assets = AsyncObservableCollection<Asset>.Create(),
                        ContainerAssetType = AssetType.CBIND
                    };
                case AssetType.EMO:
                    return new AssetContainerTool()
                    {
                        LooseFiles = true,
                        File1_Name = "NULL",
                        File2_Name = "NULL",
                        File3_Name = "NULL",
                        Assets = AsyncObservableCollection<Asset>.Create(),
                        ContainerAssetType = AssetType.EMO
                    };
                case AssetType.LIGHT:
                    return new AssetContainerTool()
                    {
                        LooseFiles = true,
                        File1_Name = "NULL",
                        File2_Name = "NULL",
                        File3_Name = "NULL",
                        Assets = AsyncObservableCollection<Asset>.Create(),
                        ContainerAssetType = AssetType.LIGHT
                    };
                default:
                    throw new InvalidOperationException(String.Format("GetDefaultContainer: Unrecognized AssetType: {0}", type));

            }
        }

        public void RefreshAssetCounts()
        {
            Pbind.RefreshAssetCount();
            Tbind.RefreshAssetCount();
            Cbind.RefreshAssetCount();
            Emo.RefreshAssetCount();
            LightEma.RefreshAssetCount();
        }

        private void ValidateTextureContainers(AssetType type)
        {
            AssetContainerTool container = GetAssetContainer(type);

            if (container.File3_Ref.Entry.Count > EMB_File.MAX_EFFECT_TEXTURES)
            {
                RemoveUnusedTextures(type);

                if (container.File3_Ref.Entry.Count > EMB_File.MAX_EFFECT_TEXTURES)
                {
                    throw new IndexOutOfRangeException(string.Format("ValidateTextureContainers: The {2} texture container has too many textures ({0}, maximum allowed is {1}).", container.File3_Ref.Entry.Count, EMB_File.MAX_EFFECT_TEXTURES, type));
                }
            }
        }

        private string GetDefaultEepkName()
        {
            if (Name == "NNNN_CCC_SSSS")
            {
                //The eepk belongs to a skill in x2m format, so we need to find another name. (If it's called NNNN_CCC_SSSS, it will be renamed)

                if (Pbind != null)
                {
                    if (Pbind.File2_Name != "NULL")
                    {
                        return Pbind.File2_Name.Split('.')[0];
                    }
                }

                if (Tbind != null)
                {
                    if (Tbind.File2_Name != "NULL")
                    {
                        return Tbind.File2_Name.Split('.')[0];
                    }
                }


                if (Cbind != null)
                {
                    if (Cbind.File1_Name != "NULL")
                    {
                        return Cbind.File1_Name.Split('.')[0];
                    }
                }

                //No containers exist that we can get a name from, so we must resort to a default one
                return "default_name";

            }
            else
            {
                return Name;
            }
        }

        //Editor
        /// <summary>
        /// Changes the asset reference on all Effect entries.
        /// </summary>
        /// <param name="oldAsset"></param>
        /// <param name="newAsset"></param>
        public void AssetRefRefactor(Asset oldAsset, Asset newAsset, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var effect in Effects)
            {
            start:
                for (int i = 0; i < effect.EffectParts.Count; i++)
                {
                    if (effect.EffectParts[i].AssetRef == oldAsset)
                    {
                        //If newAsset is null, we delete the effectPart (for Asset Delete)
                        //Else we update AssetRef with newAsset (for Asset Replace)
                        if (newAsset == null)
                        {
                            undos.Add(new UndoableListRemove<EffectPart>(effect.EffectParts, effect.EffectParts[i]));
                            effect.EffectParts.RemoveAt(i);
                            goto start;
                        }
                        else
                        {
                            undos.Add(new UndoableProperty<EffectPart>(nameof(EffectPart.AssetRef), effect.EffectParts[i], effect.EffectParts[i].AssetRef, newAsset));
                            effect.EffectParts[i].AssetRef = newAsset;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list containing all effect IDs that use the specified asset.
        /// </summary>
        /// <returns></returns>
        public List<int> AssetUsedBy(Asset asset)
        {
            List<int> effects = new List<int>();

            foreach (var effect in Effects)
            {
                foreach (var part in effect.EffectParts)
                {
                    if (part.AssetRef == asset)
                    {
                        if (effects.IndexOf(effect.IndexNum) == -1)
                        {
                            effects.Add(effect.IndexNum);
                        }
                    }
                }
            }

            return effects;
        }

        public bool EffectIdUsedByOtherEffects(ushort id, Effect effect)
        {
            foreach (var _effect in Effects)
            {
                if (_effect != effect && _effect.IndexNum == id) return true;
            }

            return false;
        }

        public void AssetRefDetailsRefresh(Asset asset)
        {
            foreach (var effect in Effects)
            {
                effect.AssetRefDetailsRefresh(asset);
            }
        }

        public void SaveDds()
        {
            Pbind.File3_Ref.SaveDdsImages();
            Tbind.File3_Ref.SaveDdsImages();

            SaveEmoDdsFiles();
        }

        private void SaveEmoDdsFiles()
        {
            foreach (var emo in Emo.Assets)
            {
                foreach (var file in emo.Files)
                {
                    if (file.fileType == EffectFile.FileType.EMB)
                    {
                        file.EmbFile.SaveDdsImages();
                    }
                }
            }
        }

        public bool IsAssetUsed(Asset asset)
        {
            if (asset == null) return false;

            foreach (var effect in Effects)
            {
                foreach (var effectPart in effect.EffectParts)
                {
                    if (effectPart.AssetRef == asset) return true;
                }
            }
            return false;
        }


        //Effect
        public Effect GetEffect(int id)
        {
            return Effects.FirstOrDefault(x => x.IndexNum == id);
        }

        private void RemoveNullEffectParts()
        {
            //Removes all EffectParts that have a null AssetRef (e.g. someone added a new EffectPart and never assigned an asset to it)
            foreach (var effect in Effects)
            {
                effect.RemoveNulls();
            }
        }

        public ushort GetUnusedEffectId(ushort min = 0)
        {
            while (IsEffectIdUsed(min))
            {
                min++;
            }

            return min;
        }

        public bool IsEffectIdUsed(ushort id)
        {
            foreach (var effect in Effects)
            {
                if (effect.IndexNum == id) return true;
            }
            return false;
        }

        public Effect GetEffectAssociatedWithEffectPart(EffectPart effectPart)
        {
            if (effectPart == null) return null;

            foreach (var effect in Effects)
            {
                foreach (var _effectPart in effect.EffectParts)
                {
                    if (_effectPart == effectPart) return effect;
                }
            }

            return null;
        }

        public Effect GetEffectAssociatedWithEffectParts(List<EffectPart> effectParts, Effect firstEffect)
        {
            if (effectParts == null) return null;
            if (effectParts.Count == 0) return null;

            if (firstEffect != null)
            {
                foreach (var effectPart in firstEffect.EffectParts)
                {
                    if (effectPart == effectParts[0]) return firstEffect;
                }
            }

            return GetEffectAssociatedWithEffectPart(effectParts[0]);
        }

        private bool IsAssetIndexUsed(AssetType type, int index)
        {
            foreach (var effect in Effects)
            {
                foreach (var effectPart in effect.EffectParts)
                {
                    if (effectPart.AssetType == type && effectPart.AssetIndex == index) return true;
                }
            }

            return false;
        }
        #endregion

        #region SDBHSupport
        private string CalculateSDBHPBINDContainerName_EMM(string emm)
        {
            if (emm != "NULL" && File.Exists(string.Format("{0}/{1}", Directory, emm)))
            {
                return string.Format("{0}/{1}", Directory, emm);
            }
            else if (emm != "NULL")
            {
                throw new FileNotFoundException(String.Format("Could not find the declared PBIND EMM file at \"{0}\".", string.Format("{0}/{1}", Directory, emm)));
            }

            if (DoesFileExist(string.Format("{0}/{1}_MTL.emm", Directory, Name)))
            {
                return string.Format("{0}/{1}_MTL.emm", Directory, Name);
            }

            return null;
        }

        private string CalculateSDBHPBINDContainerName_EMB(string emb)
        {
            if (emb != "NULL" && File.Exists(string.Format("{0}/{1}", Directory, emb)))
            {
                return string.Format("{0}/{1}", Directory, emb);
            }
            else if (emb != "NULL")
            {
                throw new FileNotFoundException(String.Format("Could not find the declared PBIND EMB file at \"{0}\".", string.Format("{0}/{1}", Directory, emb)));
            }

            if (DoesFileExist(string.Format("{0}/{1}_PIC.emb", Directory, Name)))
            {
                return string.Format("{0}/{1}_PIC.emb", Directory, Name);
            }

            return null;
        }

        private void LoadSDBHPBINDContainer()
        {
            SDBH_PbindLinkTextureAndMaterial();

            foreach (var emp in Pbind.Assets)
            {
                Pbind.AddPbindDependencies(emp.Files[0].EmpFile);
            }
        }

        private void SDBH_PbindLinkTextureAndMaterial()
        {
            if (Pbind != null)
            {
                int index = 0;
                foreach (Asset empAsset in Pbind.Assets)
                {
                    //Validation
                    if (empAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find PBIND asset at index {0}.", index));
                    }
                    if (empAsset.Files[0].EmpFile == null)
                    {
                        throw new InvalidDataException(String.Format("The EMP file at index {0} was null (name: {1}).", index, empAsset.Files[0].FullFileName));
                    }

                    EMP_File empFile = empAsset.Files[0].EmpFile;

                    /*
                     //This is weird
                    string emmPath = String.Format("{0}/{1}.emm", Directory, Path.GetFileName(empAsset.Files[0].FileName));
                    string embPath = String.Format("{0}/{1}.emb", Directory, Path.GetFileName(empAsset.Files[0].FileName));
                    EMM_File emmFile = (DoesFileExist(emmPath)) ? EMM_File.LoadEmm(LoadExternalFile(emmPath, false)) : Pbind.File2_Ref;
                    EMB_File embFile = (DoesFileExist(emmPath)) ? EMB_File.LoadEmb(LoadExternalFile(embPath, false)) : Pbind.File3_Ref;
                    */

                    EMM_File emmFile = Pbind.File2_Ref;
                    EMB_File embFile = Pbind.File3_Ref;

                    //Materials
                    SDBH_PbindLinkTextureAndMaterial_Recursive(empFile.ParticleNodes, emmFile);

                    //Textures
                    foreach (var empTexture in empFile.Textures)
                    {
                        if (empTexture.EmbIndex >= byte.MinValue && empTexture.EmbIndex < sbyte.MaxValue + 1)
                        {
                            empTexture.TextureRef = embFile.GetEntry(empTexture.EmbIndex);
                        }
                        else if (empTexture.EmbIndex != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("SDBH_PbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\nptcl.emb can only have a maximum of 128 textures.", empTexture.EmbIndex));
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void SDBH_PbindLinkTextureAndMaterial_Recursive(AsyncObservableCollection<ParticleNode> particleNodes, EMM_File emmFile)
        {
            foreach (ParticleNode empEntry in particleNodes)
            {
                if (empEntry.NodeType == ParticleNodeType.Emission)
                {
                    empEntry.EmissionNode.Texture.MaterialRef = (empEntry.EmissionNode.Texture.MaterialID != ushort.MaxValue) ? emmFile.GetEntry(empEntry.EmissionNode.Texture.MaterialID) : null;
                }

                if (empEntry.ChildParticleNodes != null)
                {
                    SDBH_PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleNodes, emmFile);
                }
            }
        }
        #endregion

        #region Operations
        public int[] RemoveAllUnusedOrDuplicates(List<IUndoRedo> undos)
        {
            int total;
            int emp = 0;
            int etr = 0;
            int ecf = 0;
            int emo = 0;
            int light = 0;
            int textures = 0;
            int materials = 0;
            int empTextures = 0;

            emp += RemoveUnusedAssets(AssetType.PBIND, undos);
            etr += RemoveUnusedAssets(AssetType.TBIND, undos);
            ecf += RemoveUnusedAssets(AssetType.CBIND, undos);
            emo += RemoveUnusedAssets(AssetType.EMO, undos);
            light += RemoveUnusedAssets(AssetType.LIGHT, undos);

            int[] pbindRet = Pbind.CleanAllUnusedAndDuplicates(undos);
            int[] tbindRet = Tbind.CleanAllUnusedAndDuplicates(undos);

            empTextures += pbindRet[0];
            textures += pbindRet[1];
            textures += tbindRet[1];
            materials += pbindRet[2];
            materials += tbindRet[2];
            total = emp + etr + ecf + emo + light + empTextures + textures + materials;

            return new int[9] { emp, etr, ecf, emo, light, empTextures, textures, materials, total };
        }

        /// <summary>
        /// Removes all assets that are not used by an Effect.
        /// </summary>
        /// <param name="type">PBIND or TBIND.</param>
        /// <returns>The amount of textures that were removed.</returns>
        public int RemoveUnusedAssets(AssetType type, List<IUndoRedo> undos)
        {
            int removed = 0;
            AssetContainerTool container = GetAssetContainer(type);

            for (int i = container.Assets.Count - 1; i >= 0; i--)
            {
                if (!IsAssetUsed(container.Assets[i]))
                {
                    undos.Add(new UndoableListRemove<Asset>(container.Assets, container.Assets[i]));
                    container.Assets.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Renives all textures that are unused by Particle Effects or ETR Effects (depending on type)
        /// </summary>
        /// <param name="type">PBIND or TBIND.</param>
        /// <returns>The amount of textures that were removed.</returns>
        public int RemoveUnusedTextures(AssetType type, List<IUndoRedo> undos = null)
        {
            if (type != AssetType.PBIND && type != AssetType.TBIND) throw new InvalidOperationException(String.Format("RemoveUnusedTextures: Method was called with type parameter = {0}, which is invalid (expecting either PBIND or TBIND).", type));
            if (undos == null) undos = new List<IUndoRedo>();

            AssetContainerTool container = GetAssetContainer(type);

            if (type == AssetType.PBIND)
                _ = container.RemoveUnusedEmpTextures();

            return container.RemoveUnusedTextures(undos);
        }

        /// <summary>
        /// Merges all identical textures into a single instance.
        /// </summary>
        /// <param name="type">PBIND or TBIND.</param>
        /// <returns>The amount of merged textures.</returns>
        public int MergeDuplicateTextures(AssetType type, List<IUndoRedo> undos = null)
        {
            if (type != AssetType.PBIND && type != AssetType.TBIND) throw new InvalidOperationException(String.Format("MergeDuplicateTextures: Method was called with type parameter = {0}, which is invalid (expecting either PBIND or TBIND).", type));
            if (undos == null) undos = new List<IUndoRedo>();

            AssetContainerTool container = GetAssetContainer(type);
            return container.MergeDuplicateTextures(undos);
        }

        /// <summary>
        /// Remove all materials that are unused by Particle Effects or ETR Effects.
        /// </summary>
        /// <param name="type">PBIND or TBIND</param>
        /// <returns>The amount of materials that were removed.</returns>
        public int RemoveUnusedMaterials(AssetType type, List<IUndoRedo> undos = null)
        {
            if (type != AssetType.PBIND && type != AssetType.TBIND) throw new InvalidOperationException(String.Format("RemoveUnusedMaterials: Method was called with type parameter = {0}, which is invalid (expecting either PBIND or TBIND).", type));

            AssetContainerTool container = GetAssetContainer(type);

            return container.RemoveUnusedMaterials(undos);
        }

        public void RemoveAsset(Asset asset, AssetType type, List<IUndoRedo> undos = null)
        {
            AssetContainerTool container = GetAssetContainer(type);

            if (undos != null && container.Assets.Contains(asset))
                undos.Add(new UndoableListRemove<Asset>(container.Assets, asset));

            container.Assets.Remove(asset);
        }


        //SUPER TEXTURES:
        /// <summary>
        /// Merges all PBIND textures into larger Super Textures, optimizing the amount of textures used.
        /// </summary>
        /// <returns>[0] = the number of textures merged, [1] = the number of super textures. </returns>
        public int[] MergeAllTexturesIntoSuperTextures_PBIND()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var embEntries = new List<EmbEntry>(Pbind.File3_Ref.Entry);

            //Remove all repeating textures
            embEntries.RemoveAll(x => EMP_TextureSamplerDef.IsRepeatingTexture(x, Pbind));

            //All textures, ordered by their highest dimension (lowest first)
            List<WriteableBitmap> textures = EmbEntry.GetBitmaps(embEntries);
            textures = textures.OrderBy(x => Math.Max(x.Width, x.Height)).ToList();

            //Remove any texture that is used by a EMP with SpeedScroll values or is Mirrored (cant merge these)
            textures.RemoveAll(x => Pbind.SuperTexture_IsTextureUsedByUnallowedType(x));

            //Remove larger than 2k textures
            textures.RemoveAll(x => Math.Max(x.Width, x.Height) > 2048);

            //Remove all textures that aren't a power of 2, as the merging algo doesn't like that at all.
            textures.RemoveAll(x => !MathHelpers.IsPowerOfTwo(x.PixelWidth) || !MathHelpers.IsPowerOfTwo(x.PixelHeight));

            int superCount = 0;
            int totalMerged = textures.Count;

            while (textures.Count > 1)
            {
                var mergeList = SelectTexturesForMerge(textures);
                var embList = Pbind.File3_Ref.GetAllEmbEntriesByBitmap(mergeList);
                Pbind.MergeIntoSuperTexture_PBIND(embList, undos);
                superCount++;
            }

            if (textures.Count == 1)
                totalMerged--;

            UndoManager.Instance.AddCompositeUndo(undos, "Super Texture Merge");

            return new int[2] { totalMerged, superCount };
        }

        private List<WriteableBitmap> SelectTexturesForMerge(List<WriteableBitmap> bitmaps)
        {
            List<WriteableBitmap> toMerge = new List<WriteableBitmap>();
            toMerge.Add(bitmaps[0]);

            for (int i = 1; i < bitmaps.Count; i++)
            {
                toMerge.Add(bitmaps[i]);

                if (EmbEntry.SelectTextureSize(EmbEntry.HighestDimension(toMerge), toMerge.Count) == -1)
                {
                    toMerge.RemoveAt(toMerge.Count - 1);
                    break;
                }
            }

            //Remove the bitmaps we are going to merge from the main list
            bitmaps.RemoveAll(x => toMerge.Contains(x));

            return toMerge;
        }

        #endregion

        #region Install
        /// <summary>
        /// Remove the specified effect. Any assets will also be removed if they are no longer in use.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveEffect(int id)
        {
            if (!Effects.Any(e => e.IndexNum == id)) return;

            Effect toRemove = Effects.First(e => e.IndexNum == id);

            if (toRemove != null)
            {
                Effects.Remove(toRemove);

                foreach (var asset in toRemove.EffectParts)
                {
                    //If asset isn't used by other effects, then remove it
                    if (!IsAssetUsed(asset.AssetRef) && asset.AssetRef != null)
                    {
                        RemoveAsset(asset.AssetRef, asset.AssetType);
                    }
                }
            }
        }

        public void InstallEffects(IList<Effect> effects)
        {
            ProcessVfxExtensions(effects);

            //Remove effects (clears out potential duplicate and unneeded data)
            foreach (var effect in effects)
            {
                RemoveEffect(effect.IndexNum);
            }

            //Remove unused dependencies (same reason)
            RemoveUnusedTextures(AssetType.PBIND);
            RemoveUnusedMaterials(AssetType.PBIND);
            RemoveUnusedTextures(AssetType.TBIND);
            RemoveUnusedMaterials(AssetType.TBIND);

            //Merge identical textures
            MergeDuplicateTextures(AssetType.PBIND);
            MergeDuplicateTextures(AssetType.TBIND);

            //Add effects
            foreach (var effect in effects)
            {
                AddEffect(effect);
            }
        }

        public void UninstallEffects(List<string> ids, EffectContainerFile originalEepk)
        {
            //Uninstall effects
            foreach (var id in ids)
            {
                Effect originalEffect = originalEepk != null ? originalEepk.Effects.FirstOrDefault(e => e.Index == id) : null;
                Effect effect = Effects.FirstOrDefault(e => e.Index == id);

                if (effect != null)
                {
                    //An effect with a matching ID exists and so we will uninstall it.
                    UninstallEffect(effect, originalEffect);
                }
            }

            //Clean up
            RemoveUnusedTextures(AssetType.PBIND);
            RemoveUnusedMaterials(AssetType.PBIND);
            RemoveUnusedTextures(AssetType.TBIND);
            RemoveUnusedMaterials(AssetType.TBIND);

        }

        /// <summary>
        /// Uninstall the specified effect, and optionally install another effect to take its place.
        /// </summary>
        /// <param name="effect">The effect to uninstall.</param>
        /// <param name="original">The original effect loaded from CPK that will be reinstalled.</param>
        public void UninstallEffect(Effect effect, Effect original = null)
        {
            if (effect != null)
            {
                RemoveEffect(effect.IndexNum);

                if (original != null)
                {
                    AddEffect(original);
                }
            }
        }

        private void ProcessVfxExtensions(IList<Effect> effects)
        {
            foreach (var effect in effects)
            {
                if (effect.ExtendedEffectData != null)
                {
                    //AutoID
                    if (effect.ExtendedEffectData.AutoIdEnabled)
                    {
                        effect.ExtendedEffectData.EffectID = AssignAutoId(effects);
                    }

                    //CopyFrom
                    if (effect.ExtendedEffectData.CopyFrom != -1)
                    {
                        //First try to find effect in VfxPackage
                        Effect copyEffect = effects.FirstOrDefault(x => x.IndexNum == effect.ExtendedEffectData.CopyFrom);

                        //No effect found in VfxPackage, now check EEPK
                        if (copyEffect == null)
                        {
                            copyEffect = Effects.FirstOrDefault(x => x.IndexNum == effect.ExtendedEffectData.CopyFrom);

                            if (copyEffect == null)
                                throw new Exception($"ProcessVfxExtensions: CopyFrom cannot be completed. No Effect ID of {effect.ExtendedEffectData.CopyFrom} was found in either the VfxPackage or EEPK.");

                        }

                        effect.EffectParts.AddRange(copyEffect.EffectParts);
                    }
                }
            }
        }

        private int AssignAutoId(IList<Effect> effectsToInstall)
        {
            int id = 2000;

            while (IsEffectIdUsed(id, effectsToInstall))
            {
                id++;

                if (id > ushort.MaxValue)
                {
                    throw new Exception("AssignAutoId: Cannot assign a ID within the EEPK file. There are no suitable IDs.");
                }
            }

            return id;
        }

        private bool IsEffectIdUsed(int id, IList<Effect> effectsToInstall)
        {
            if (Effects.Any(x => x.IndexNum == id)) return true;
            if (effectsToInstall.Any(x => x.IndexNum == id && x.ExtendedEffectData?.AutoIdEnabled == false)) return true;

            return false;
        }

        public int CreateAwokenOverlayEntry(byte[] texture)
        {
            Effect bluerintEffect = Effects.FirstOrDefault(x => x.SortID == 20000);

            if (bluerintEffect == null)
                throw new Exception("CreateStageSelectorEntry: StageSelector Blueprint effect at ID 20000 not found.");

            Effect newEffect = bluerintEffect.Copy();
            newEffect.IndexNum = GetUnusedEffectId(20001);
            Effects.Add(newEffect);

            Asset asset = newEffect.EffectParts[0].AssetRef.Clone();
            asset.InstanceID = Guid.NewGuid();
            asset.Files[0].SetName(Pbind.GetUnusedName(asset.Files[0].FullFileName));

            EmbEntry embEntry = new EmbEntry();
            embEntry.Name = "StageSelector.png";
            embEntry.Data = texture;

            if (Pbind.File3_Ref.Entry.Count == EMB_File.MAX_EFFECT_TEXTURES)
            {
                MergeAllTexturesIntoSuperTextures_PBIND();

                if (Pbind.File3_Ref.Entry.Count == EMB_File.MAX_EFFECT_TEXTURES)
                {
                    throw new Exception("CreateStageSelectorEntry: Not enough space for the StageSelector textures.");
                }
            }

            asset.Files[0].EmpFile.Textures[0].TextureRef = embEntry;
            Pbind.AddAsset(asset);

            newEffect.EffectParts[0].AssetRef = asset;

            return newEffect.IndexNum;

        }
        #endregion

        public bool IsNull()
        {
            return (Effects.Count == 0);
        }
    }

    [Serializable]
    public class AssetContainerTool : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public AssetType ContainerAssetType { get; internal set; }

        private bool _looseFilesValue = false;
        public bool LooseFiles  //true = File1_Name is == NULL, meaning the assets are not stored in a emb (EMO will always be true)
        {
            get
            {
                return this._looseFilesValue;
            }
            set
            {
                if (value != this._looseFilesValue)
                {
                    this._looseFilesValue = value;
                    NotifyPropertyChanged(nameof(LooseFiles));
                    NotifyPropertyChanged(nameof(UndoableLooseFiles));
                }
            }
        }
        public bool UndoableLooseFiles
        {
            get
            {
                return LooseFiles;
            }
            set
            {
                if (LooseFiles != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<AssetContainerTool>(nameof(LooseFiles), this, LooseFiles, value, "Loose Files"));
                    LooseFiles = value;
                }

            }
        }


        public int AssetSpawnLimit { get; set; } = 128;
        public byte I_04 { get; set; } = 255;
        public byte I_05 { get; set; } = 255;
        public byte I_06 { get; set; } = 255;
        public byte I_07 { get; set; } = 255;
        public int AssetListLimit { get; set; } = -1;
        public int I_12 { get; set; } = -1;

        public string File1_Name { get; set; } //Main container emb (pbind, tbind, cbind, light)
        public string File2_Name { get; set; } //Material .emm
        public string File3_Name { get; set; } //Texture emb (trc, ptcl)

        //References to the emb/emm files (for pbind, tbind and cbind only)
        public EMB_File File1_Ref { get; set; }
        public EMM_File File2_Ref { get; set; }
        public EMB_File File3_Ref { get; set; }

        private AsyncObservableCollection<Asset> _assetsValue = null;
        public AsyncObservableCollection<Asset> Assets
        {
            get
            {
                return this._assetsValue;
            }
            set
            {
                if (value != this._assetsValue)
                {
                    this._assetsValue = value;
                    NotifyPropertyChanged(nameof(Assets));
                }
            }
        }

        #region UiProperties
        //Count
        public string AssetCount
        {
            get
            {
                return string.Format("{0}/--", Assets.Count);
            }
        }

        //View
        private string _assetSearchFilter = null;
        public string AssetSearchFilter
        {
            get
            {
                return this._assetSearchFilter;
            }

            set
            {
                if (value != this._assetSearchFilter)
                {
                    this._assetSearchFilter = value;
                    NotifyPropertyChanged("AssetSearchFilter");
                }
            }
        }

        [NonSerialized]
        private ListCollectionView _viewAssets = null;
        public ListCollectionView ViewAssets
        {
            get
            {
                if (_viewAssets != null)
                {
                    return _viewAssets;
                }
                _viewAssets = new ListCollectionView(Assets.Binding);
                _viewAssets.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Asset.assetType)));
                _viewAssets.Filter = new Predicate<object>(AssetFilterCheck);
                return _viewAssets;
            }
            set
            {
                if (value != _viewAssets)
                {
                    _viewAssets = value;
                    NotifyPropertyChanged("ViewAssets");
                }
            }
        }


        //Filter methods
        public bool AssetFilterCheck(object skill)
        {
            if (String.IsNullOrWhiteSpace(AssetSearchFilter)) return true;
            var _asset = skill as Asset;
            string flattenedSearchParam = AssetSearchFilter.ToLower();

            if (_asset != null)
            {
                foreach (var file in _asset.Files)
                {
                    string fullName = file.FileName + file.Extension;
                    if (fullName.ToLower().Contains(flattenedSearchParam)) return true;
                }
            }

            return false;
        }

        public void NewAssetFilter(string newFilter)
        {
            AssetSearchFilter = newFilter;
        }

        public void UpdateAssetFilter()
        {
            if (_viewAssets == null)
                _viewAssets = new ListCollectionView(Assets.Binding);

            _viewAssets.Filter = new Predicate<object>(AssetFilterCheck);
            NotifyPropertyChanged("ViewAssets");
        }


        //Count Method
        public void RefreshAssetCount()
        {
            NotifyPropertyChanged(nameof(AssetCount));
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Get an asset based on the fileName of the first file.
        /// </summary>
        /// <param name="fileName">The name of the first file, minus the extension.</param>
        /// <returns></returns>
        public Asset GetAsset(string fileName)
        {
            foreach (var asset in Assets)
            {
                if (asset.Files.Count > 0)
                {
                    if (asset.Files[0].FileName == fileName) return asset;
                }
            }

            return null;
        }

        public string GetUnusedName(string name)
        {
            string nameWithoutExtension = EffectFile.GetFileNameWithoutExtension(name);
            string extension = EffectFile.GetExtension(name);
            string newName = name;
            int num = 1;

            while (NameUsed(newName))
            {
                newName = String.Format("{0}_{1}{2}", nameWithoutExtension, num, extension);
                num++;
            }

            return newName;
        }

        public bool NameUsed(string name)
        {
            if (name == "NULL") return true;

            foreach (var asset in Assets)
            {
                foreach (var file in asset.Files)
                {
                    if (file.FullFileName == name) return true;
                }
            }

            return false;
        }

        public Asset GetAssetByFileInstance(EffectFile file)
        {
            foreach (var asset in Assets)
            {
                foreach (var _file in asset.Files)
                {
                    if (_file == file)
                    {
                        return asset;
                    }
                }
            }

            return null;
        }

        public Asset AssetExists(Asset asset)
        {
            //For Installer use. Reuses an asset if it has the same name.
            if (EepkToolInterlop.AssetReuseMatchName)
            {
                foreach (var _asset in Assets)
                {
                    if (_asset.HasSameFileNames(asset))
                    {
                        return _asset;
                    }
                }

            }

            foreach (var _asset in Assets)
            {
                if (_asset.InstanceID == asset.InstanceID) return _asset;
            }

            return null;
        }

        public List<string> TextureUsedBy(EmbEntry embEntry)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("TextureUsedBy: AssetType is not PBIND or TBIND, cannot continue.");

            List<string> textureUsedBy = new List<string>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var texture in asset.Files[0].EmpFile.Textures)
                    {
                        if (texture.TextureRef == embEntry)
                        {
                            if (!textureUsedBy.Contains(asset.FileNamesPreviewWithExtension))
                            {
                                textureUsedBy.Add(asset.FileNamesPreviewWithExtension);
                            }
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.Textures)
                    {
                        if (texture.TextureRef == embEntry)
                        {
                            if (!textureUsedBy.Contains(asset.FileNamesPreviewWithExtension))
                            {
                                textureUsedBy.Add(asset.FileNamesPreviewWithExtension);
                            }
                        }
                    }
                }
            }

            return textureUsedBy;
        }

        public List<string> MaterialUsedBy(EmmMaterial material)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("MaterialUsedBy: AssetType is not PBIND or TBIND, cannot continue.");

            List<string> materialUsedBy = new List<string>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    MaterialUsedBy_Recursive(asset.Files[0].EmpFile.ParticleNodes, material, materialUsedBy, asset.FileNamesPreviewWithExtension);
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.Nodes)
                    {
                        if (texture.MaterialRef == material)
                        {
                            if (!materialUsedBy.Contains(asset.FileNamesPreviewWithExtension))
                            {
                                materialUsedBy.Add(asset.FileNamesPreviewWithExtension);
                            }
                        }
                    }
                }
            }

            return materialUsedBy;
        }

        private void MaterialUsedBy_Recursive(IList<ParticleNode> childParticleEffects, EmmMaterial material, List<string> textureUsedBy, string fileName)
        {
            foreach (var particle in childParticleEffects)
            {
                if (particle.NodeType == ParticleNodeType.Emission)
                {
                    if (particle.EmissionNode.Texture.MaterialRef == material)
                    {
                        if (!textureUsedBy.Contains(fileName))
                        {
                            textureUsedBy.Add(fileName);
                        }
                    }
                }

                if (particle.ChildParticleNodes != null)
                {
                    MaterialUsedBy_Recursive(particle.ChildParticleNodes, material, textureUsedBy, fileName);
                }
            }
        }

        public bool IsTextureUsed(EmbEntry embEntry)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("IsTextureUsed: AssetType is not PBIND or TBIND, cannot continue.");

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var texture in asset.Files[0].EmpFile.Textures)
                    {
                        if (texture.TextureRef == embEntry)
                        {
                            return true;
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.Textures)
                    {
                        if (texture.TextureRef == embEntry)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool IsMaterialUsed(EmmMaterial material)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("IsMaterialUsed: AssetType is not PBIND or TBIND, cannot continue.");

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    if (IsMaterialUsed_Recursive(asset.Files[0].EmpFile.ParticleNodes, material))
                        return true;
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var mat in asset.Files[0].EtrFile.Nodes)
                    {
                        if (mat.MaterialRef == material)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsMaterialUsed_Recursive(IList<ParticleNode> childParticleEffects, EmmMaterial material)
        {
            foreach (var mat in childParticleEffects)
            {
                if (mat.EmissionNode.Texture.MaterialRef == material)
                {
                    return true;
                }

                if (mat.ChildParticleNodes != null)
                {
                    if (IsMaterialUsed_Recursive(mat.ChildParticleNodes, material))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public EmmMaterial GetMaterialAssociatedWithParameters(List<Parameter> parameters, EmmMaterial firstMaterial)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("GetMaterialAssociatedWithParameters: AssetType is not PBIND or TBIND, cannot continue.");

            if (parameters == null) return null;
            if (parameters.Count == 0) return null;

            if (firstMaterial != null)
            {
                foreach (var param in firstMaterial.Parameters)
                {
                    if (param == parameters[0]) return firstMaterial;
                }
            }

            foreach (var material in File2_Ref.Materials)
            {
                foreach (var param in material.Parameters)
                {
                    if (param == parameters[0]) return material;
                }
            }

            return null;
        }

        public List<EMP_TextureSamplerDef> GetAllTextureDefinitions(EmbEntry embEntry)
        {
            List<EMP_TextureSamplerDef> textures = new List<EMP_TextureSamplerDef>();

            foreach (var asset in Assets)
            {
                if (asset.assetType == AssetType.PBIND && asset.Files.Count == 1)
                {
                    foreach (var textureDef in asset.Files[0].EmpFile.Textures)
                    {
                        if (textureDef.TextureRef == embEntry)
                            textures.Add(textureDef);
                    }
                }
            }

            return textures;
        }

        public bool SuperTexture_IsTextureUsedByUnallowedType(WriteableBitmap bitmap)
        {
            foreach (var asset in Assets)
            {
                if (asset.assetType == AssetType.PBIND)
                {
                    foreach (var textureDef in asset.Files[0].EmpFile.Textures)
                    {
                        if (textureDef.TextureRef?.Texture == bitmap)
                        {
                            if (textureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Speed
                                || textureDef.RepetitionU == EMP_TextureSamplerDef.TextureRepitition.Mirror || textureDef.RepetitionV == EMP_TextureSamplerDef.TextureRepitition.Mirror
                                || textureDef.RepetitionU == EMP_TextureSamplerDef.TextureRepitition.Clamp || textureDef.RepetitionV == EMP_TextureSamplerDef.TextureRepitition.Clamp)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
        #endregion

        #region AddAssetFunctions
        public void AddAsset(Asset asset, List<IUndoRedo> undos)
        {
            if (Assets.IndexOf(asset) != -1)
            {
                throw new InvalidOperationException(string.Format("Tried to add a duplicate asset in {0}.", ContainerAssetType));
            }

            //Regenerate names (to prevent duplicates)
            foreach (var file in asset.Files)
            {
                if (file.FullFileName != "NULL")
                {
                    string originalName = file.FullFileName;
                    file.SetName(GetUnusedName(file.FullFileName));

                    if (file.FullFileName != originalName)
                        undos.Add(new UndoableProperty<EffectFile>("FullFileName", file, originalName, file.FullFileName));
                }
            }

            //Handle textures/materials if pbind/tbind
            if (ContainerAssetType == AssetType.PBIND)
            {
                AddPbindDependencies(asset.Files[0].EmpFile, undos);
            }
            else if (ContainerAssetType == AssetType.TBIND)
            {
                AddTbindDependencies(asset.Files[0].EtrFile, undos);
            }

            //Add asset
            Assets.Add(asset);
            undos.Add(new UndoableListAdd<Asset>(Assets, asset));
        }

        public void AddAsset(Asset asset)
        {
            var undos = new List<IUndoRedo>();
            AddAsset(asset, undos);
        }

        /// <summary>
        /// Add a single empFile to Assets.
        /// </summary>
        public Asset AddAsset(EMP_File empFile, string name)
        {
            if (Path.GetExtension(name) != ".emp")
            {
                name += ".emp";
            }

            var asset = Asset.Create(empFile, name, EffectFile.FileType.EMP, AssetType.PBIND);
            AddAsset(asset);

            return asset;
        }

        /// <summary>
        /// Add a single etrFile to Assets.
        /// </summary>
        public Asset AddAsset(ETR_File etrFile, string name)
        {
            if (Path.GetExtension(name) != ".etr")
            {
                name += ".etr";
            }

            var asset = Asset.Create(etrFile, name, EffectFile.FileType.ETR, AssetType.TBIND);
            AddAsset(asset);

            return asset;
        }

        /// <summary>
        /// Add a single ecfFile to Assets.
        /// </summary>
        public Asset AddAsset(ECF_File ecfFile, string name)
        {
            if (Path.GetExtension(name) != ".ecf")
            {
                name += ".ecf";
            }
            Asset asset = Asset.Create(ecfFile, name, EffectFile.FileType.ECF, AssetType.CBIND);
            AddAsset(asset);

            return asset;
        }

        /// <summary>
        /// Add a single lightEmaFile to Assets.
        /// </summary>
        public Asset AddAsset(EMA_File lightEmaFile, string name)
        {
            if (Path.GetExtension(name) != ".ema")
            {
                name += "light.ema";
            }

            var asset = Asset.Create(lightEmaFile, name, EffectFile.FileType.EMA, AssetType.LIGHT);
            AddAsset(asset);

            return asset;
        }

        //Dependency Adding
        public void AddPbindDependencies(EMP_File empFile, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            if (empFile == null)
            {
                throw new InvalidOperationException("AddPbindDependencies(EMP_File empFile): empFile was null.");
            }

            foreach (EMP_TextureSamplerDef texture in empFile.Textures)
            {
                if (texture.TextureRef != null)
                {
                    EmbEntry entryWithSameName = File3_Ref.GetEntry(texture.TextureRef.Name);
                    var ret = File3_Ref.Compare(texture.TextureRef);

                    if (ret != null)
                    {
                        //An identical texture was found, so use that instead
                        texture.TextureRef = ret;
                    }
                    else if (entryWithSameName != null && EepkToolInterlop.TextureImportMatchNames)
                    {
                        //Use this texture
                        texture.TextureRef = entryWithSameName;
                    }
                    else
                    {
                        //No identical texture was found, so add it
                        if (File3_Ref.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
                        {
                            throw new InvalidOperationException("AddPbindDependencies: The maximum allowed amount of textures has been reached. Cannot add anymore.");
                        }
                        texture.TextureRef.Name = File3_Ref.GetUnusedName(texture.TextureRef.Name); //Regenerate the name
                        File3_Ref.Entry.Add(texture.TextureRef);
                        undos?.Add(new UndoableListAdd<EmbEntry>(File3_Ref.Entry, texture.TextureRef));
                    }
                }
            }

            //Particle effects (recursive)
            AddPbindDependencies_Recursive(empFile.ParticleNodes, undos);

        }

        private void AddPbindDependencies_Recursive(IList<ParticleNode> childrenParticleEffects, List<IUndoRedo> undos)
        {
            foreach (ParticleNode particleEffect in childrenParticleEffects)
            {
                if (particleEffect.NodeType == ParticleNodeType.Emission)
                {
                    if (particleEffect.EmissionNode.Texture.MaterialRef != null)
                    {
                        EmmMaterial ret = File2_Ref.Compare(particleEffect.EmissionNode.Texture.MaterialRef);

                        if (ret == null)
                        {
                            //No identical material was found, so add it
                            particleEffect.EmissionNode.Texture.MaterialRef.Name = File2_Ref.GetUnusedName(particleEffect.EmissionNode.Texture.MaterialRef.Name); //Regenerate the name
                            File2_Ref.Materials.Add(particleEffect.EmissionNode.Texture.MaterialRef);
                            undos?.Add(new UndoableListAdd<EmmMaterial>(File2_Ref.Materials, particleEffect.EmissionNode.Texture.MaterialRef));
                        }
                        else
                        {
                            //An identical material was found, so use that instead
                            particleEffect.EmissionNode.Texture.MaterialRef = ret;
                        }
                    }
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    AddPbindDependencies_Recursive(particleEffect.ChildParticleNodes, undos);
                }
            }
        }

        public void AddTbindDependencies(ETR_File etrFile, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();
            if (etrFile == null)
            {
                throw new InvalidOperationException("AddTbindDependencies(ETR_File etrFile): etrFile was null.");
            }

            //Textures
            foreach (var texture in etrFile.Textures)
            {
                if (texture.TextureRef != null)
                {
                    var entryWithSameName = File3_Ref.GetEntry(texture.TextureRef.Name);

                    var ret = File3_Ref.Compare(texture.TextureRef);

                    if (ret != null)
                    {
                        //An identical texture was found, so use that instead
                        texture.TextureRef = ret;
                    }
                    else if (entryWithSameName != null && EepkToolInterlop.TextureImportMatchNames)
                    {
                        //Use this texture
                        texture.TextureRef = entryWithSameName;
                    }
                    else
                    {
                        //No identical texture was found, so add it
                        if (File3_Ref.Entry.Count >= EMB_File.MAX_EFFECT_TEXTURES)
                        {
                            throw new InvalidOperationException("AddTbindDependencies: The maximum allowed amount of textures has been reached. Cannot add anymore.");
                        }
                        texture.TextureRef.Name = File3_Ref.GetUnusedName(texture.TextureRef.Name); //Regenerate the name
                        File3_Ref.Entry.Add(texture.TextureRef);
                        undos?.Add(new UndoableListAdd<EmbEntry>(File3_Ref.Entry, texture.TextureRef));
                    }
                }
            }

            //Main entries
            foreach (ETR_Node entry in etrFile.Nodes)
            {
                var ret = File2_Ref.Compare(entry.MaterialRef);

                if (ret == null && entry.MaterialRef != null)
                {
                    //No identical material was found, so add it
                    entry.MaterialRef.Name = File2_Ref.GetUnusedName(entry.MaterialRef.Name); //Regenerate the name
                    File2_Ref.Materials.Add(entry.MaterialRef);
                    undos?.Add(new UndoableListAdd<EmmMaterial>(File2_Ref.Materials, entry.MaterialRef));
                }
                else
                {
                    //An identical material was found, so use that instead
                    entry.MaterialRef = ret;
                }
            }
        }

        public void AddTbindDependencies(ETR_Node node, ETR_File etrFile, List<IUndoRedo> undos)
        {
            //Import Materials
            if (node.MaterialRef != null)
            {
                if (!File2_Ref.Materials.Contains(node.MaterialRef))
                {
                    EmmMaterial result = File2_Ref.Compare(node.MaterialRef, true);

                    if (result == null)
                    {
                        //Material didn't exist so we have to add it
                        node.MaterialRef.Name = File2_Ref.GetUnusedName(node.MaterialRef.Name);
                        undos.Add(new UndoableListAdd<EmmMaterial>(File2_Ref.Materials, node.MaterialRef));
                        File2_Ref.Materials.Add(node.MaterialRef);
                    }
                    else if (result != node.MaterialRef)
                    {
                        //A identical material already existed but it was a different instance.
                        //Change the referenced material to this.
                        node.MaterialRef = result;
                    }
                }
            }

            //Import Textures
            ImportTextures(node.TextureEntryRef, etrFile.Textures, undos);
        }

        public void AddPbindDependencies(ParticleNode node, EMP_File empFile, List<IUndoRedo> undos)
        {
            //Import Materials
            if (node.EmissionNode.Texture.MaterialRef != null)
            {
                if (!File2_Ref.Materials.Contains(node.EmissionNode.Texture.MaterialRef))
                {
                    EmmMaterial result = File2_Ref.Compare(node.EmissionNode.Texture.MaterialRef, true);

                    if (result == null)
                    {
                        //Material didn't exist so we have to add it
                        node.EmissionNode.Texture.MaterialRef.Name = File2_Ref.GetUnusedName(node.EmissionNode.Texture.MaterialRef.Name);
                        undos.Add(new UndoableListAdd<EmmMaterial>(File2_Ref.Materials, node.EmissionNode.Texture.MaterialRef));
                        File2_Ref.Materials.Add(node.EmissionNode.Texture.MaterialRef);
                    }
                    else if (result != node.EmissionNode.Texture.MaterialRef)
                    {
                        //A identical material already existed but it was a different instance.
                        //Change the referenced material to this.
                        node.EmissionNode.Texture.MaterialRef = result;
                    }
                }
            }

            //Import Textures
            ImportTextures(node.EmissionNode.Texture.TextureEntryRef, empFile.Textures, undos);

            //Recursively call this method again if this node has children
            if (node.ChildParticleNodes != null)
            {
                foreach (ParticleNode child in node.ChildParticleNodes)
                {
                    AddPbindDependencies(child, empFile, undos);
                }
            }
        }

        /// <summary>
        /// Import textures from an EMP or ETR. Internal function.
        /// </summary>
        private void ImportTextures(IList<TextureEntry_Ref> texturesToImport, IList<EMP_TextureSamplerDef> destination, List<IUndoRedo> undos)
        {
            foreach (TextureEntry_Ref texture in texturesToImport.Where(x => x.TextureRef != null))
            {
                EMP_TextureSamplerDef newTex = destination.FirstOrDefault(x => x.Compare(texture.TextureRef));

                if (newTex == null)
                {
                    //Create new texture entry:
                    //Clone texture def - needed to break link with any instances in other EMP files
                    newTex = texture.TextureRef.Clone();
                    undos.Add(new UndoablePropertyGeneric(nameof(texture.TextureRef), texture, texture.TextureRef, newTex));
                    texture.TextureRef = newTex;

                    undos.Add(new UndoableListAdd<EmbEntry>(File3_Ref.Entry, texture.TextureRef.TextureRef));
                    texture.TextureRef.TextureRef = File3_Ref.Add(texture.TextureRef.TextureRef);

                    //Add the texture
                    undos.Add(new UndoableListAdd<EMP_TextureSamplerDef>(destination, texture.TextureRef));
                    destination.Add(texture.TextureRef);
                }
                else
                {
                    //An identical texture was found, so set that as the ref
                    texture.TextureRef = newTex;
                }
            }
        }
        #endregion

        #region Editor
        public void DeleteTexture(EmbEntry embEntry, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("DeleteTexture: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos != null && File3_Ref.Entry.Contains(embEntry))
                undos.Add(new UndoableListRemove<EmbEntry>(File3_Ref.Entry, embEntry, File3_Ref.Entry.IndexOf(embEntry)));

            File3_Ref.Entry.Remove(embEntry);
        }

        public void DeleteMaterial(EmmMaterial material, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("DeleteMaterial: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos != null && File2_Ref.Materials.Contains(material))
                undos.Add(new UndoableListRemove<EmmMaterial>(File2_Ref.Materials, material, File2_Ref.Materials.IndexOf(material)));

            File2_Ref.Materials.Remove(material);
        }

        //Operations
        /// <summary>
        /// Merges all identical materials into a single instance.
        /// </summary>
        /// <returns>The amount of merged materials.</returns>
        public int MergeDuplicateMaterials(List<IUndoRedo> undos = null)
        {
            AssetContainerTool container = this;

            int duplicateCount = 0;

        restart:
            foreach (var material1 in container.File2_Ref.Materials)
            {
                List<EmmMaterial> Duplicates = new List<EmmMaterial>();

                foreach (var material2 in container.File2_Ref.Materials)
                {
                    if (material1 != material2 && material1.Compare(material2))
                    {
                        //Textures are the same, but the EmbEntry instances are different, thus its a duplicate
                        duplicateCount++;
                        Duplicates.Add(material2);
                    }
                }

                //Redirect the duplicates
                if (Duplicates.Count > 0)
                {
                    foreach (var duplicate in Duplicates)
                    {
                        container.RefactorMaterialRef(duplicate, material1, undos);

                        //Delete the duplicate
                        undos?.Add(new UndoableListRemove<EmmMaterial>(container.File2_Ref.Materials, duplicate));
                        container.File2_Ref.Materials.Remove(duplicate);
                    }
                    goto restart;
                }

            }

            return duplicateCount;
        }

        /// <summary>
        /// Remove all materials that are unused by Particle Effects or ETR Effects.
        /// </summary>
        /// <returns>The amount of materials that were removed.</returns>
        public int RemoveUnusedMaterials(List<IUndoRedo> undos = null)
        {
            int removed = 0;
            AssetContainerTool container = this;

            for (int i = container.File2_Ref.Materials.Count - 1; i >= 0; i--)
            {
                if (!container.IsMaterialUsed(container.File2_Ref.Materials[i]))
                {
                    undos?.Add(new UndoableListRemove<EmmMaterial>(container.File2_Ref.Materials, container.File2_Ref.Materials[i]));
                    container.File2_Ref.Materials.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        public int MergeDuplicateTextures(List<IUndoRedo> undos = null)
        {
            int duplicateCount = 0;

        restart:
            foreach (var texture1 in File3_Ref.Entry)
            {
                List<EmbEntry> Duplicates = new List<EmbEntry>();

                foreach (var texture2 in File3_Ref.Entry)
                {
                    if (texture1 != texture2 && texture1.Compare(texture2, true))
                    {
                        //Textures are the same, but the EmbEntry instances are different, thus its a duplicate
                        duplicateCount++;
                        Duplicates.Add(texture2);
                    }
                }

                //Redirect the duplicates
                if (Duplicates.Count > 0)
                {
                    foreach (var duplicate in Duplicates)
                    {
                        RefactorTextureRef(duplicate, texture1, undos);

                        //Delete the duplicate
                        undos?.Add(new UndoableListRemove<EmbEntry>(File3_Ref.Entry, duplicate));
                        File3_Ref.Entry.Remove(duplicate);
                    }
                    goto restart;
                }

            }

            return duplicateCount;
        }

        public int RemoveUnusedTextures(List<IUndoRedo> undos = null)
        {
            int removed = 0;
            for (int i = File3_Ref.Entry.Count - 1; i >= 0; i--)
            {
                if (!IsTextureUsed(File3_Ref.Entry[i]))
                {
                    undos?.Add(new UndoableListRemove<EmbEntry>(File3_Ref.Entry, File3_Ref.Entry[i]));
                    File3_Ref.Entry.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        public void MergeIntoSuperTexture_PBIND(List<EmbEntry> embEntries, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND) throw new InvalidOperationException("MergeIntoSuperTexture_PBIND: SuperTexture feature is only available for PBIND container type.");
            if (undos == null) undos = new List<IUndoRedo>();

            List<WriteableBitmap> bitmaps = EmbEntry.GetBitmaps(embEntries);
            double maxDimension = EmbEntry.HighestDimension(bitmaps);
            int textureSize = (int)EmbEntry.SelectTextureSize(maxDimension, bitmaps.Count);

            WriteableBitmap superTexture = new WriteableBitmap(textureSize, textureSize, 96, 96, PixelFormats.Bgra32, null);
            EmbEntry newEmbEntry = new EmbEntry();
            newEmbEntry.ImageFormat = CSharpImageLibrary.ImageEngineFormat.DDS_DXT5;
            newEmbEntry.Texture = superTexture;

            for (int i = 0; i < bitmaps.Count; i++)
            {
                if (bitmaps[i] == null)
                    throw new NullReferenceException("Bitmap was null.");

                double position = maxDimension * i / textureSize;
                int row = (int)position;
                position -= row;
                int x = (int)(position * textureSize);
                int y = (int)maxDimension * row;

                Rect sourceRect = new Rect(0, 0, bitmaps[i].PixelWidth, bitmaps[i].PixelHeight);
                Rect destRect = new Rect(x, y, bitmaps[i].PixelWidth, bitmaps[i].PixelHeight);

                superTexture.Blit(destRect, bitmaps[i], sourceRect);

                //Update EMP Texture cordinates
                List<EMP_TextureSamplerDef> textureDefs = GetAllTextureDefinitions(embEntries[i]);

                double a = textureSize / maxDimension;

                foreach (EMP_TextureSamplerDef textureDef in textureDefs)
                {
                    undos.Add(new UndoableProperty<EMP_TextureSamplerDef>(nameof(EMP_TextureSamplerDef.TextureRef), textureDef, textureDef.TextureRef, newEmbEntry));
                    textureDef.TextureRef = newEmbEntry;

                    //If SpriteSheet or Static and not TextureRepetition == Mirror
                    if ((textureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet || textureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.Static) &&
                        textureDef.RepetitionU != EMP_TextureSamplerDef.TextureRepitition.Mirror && textureDef.RepetitionV != EMP_TextureSamplerDef.TextureRepitition.Mirror)
                    {

                        //If no keyframe exists, then create a default one. (Some EMPs are like this, for some weird reason????)
                        if (textureDef.ScrollState.Keyframes.Count == 0)
                        {
                            EMP_ScrollKeyframe newKeyframe = new EMP_ScrollKeyframe();
                            newKeyframe.ScaleU = 1f;
                            newKeyframe.ScaleV = 1f;
                            textureDef.ScrollState.Keyframes.Add(newKeyframe);
                            undos.Add(new UndoableListAdd<EMP_ScrollKeyframe>(textureDef.ScrollState.Keyframes, newKeyframe));

                            if (textureDef.ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                            {
                                textureDef.ScrollState.ScrollType = EMP_ScrollState.ScrollTypeEnum.Static;
                                undos.Add(new UndoableProperty<EMP_TextureSamplerDef>(nameof(EMP_ScrollState.ScrollType), textureDef, EMP_ScrollState.ScrollTypeEnum.SpriteSheet, EMP_ScrollState.ScrollTypeEnum.Static));
                            }
                        }

                        foreach (EMP_ScrollKeyframe keyframe in textureDef.ScrollState.Keyframes)
                        {
                            float newScaleX = (float)(keyframe.ScaleU / a * (bitmaps[i].Width / maxDimension));
                            float newScaleY = (float)(keyframe.ScaleV / a * (bitmaps[i].Height / maxDimension));
                            float newScrollX = (float)((keyframe.ScrollU / a * (bitmaps[i].Width / maxDimension)) + position);
                            float newScrollY = (float)((keyframe.ScrollV / a * (bitmaps[i].Height / maxDimension)) + (row / a));

                            undos.Add(new UndoableProperty<EMP_ScrollKeyframe>(nameof(EMP_ScrollKeyframe.ScrollU), keyframe, keyframe.ScrollU, newScrollX));
                            undos.Add(new UndoableProperty<EMP_ScrollKeyframe>(nameof(EMP_ScrollKeyframe.ScrollV), keyframe, keyframe.ScrollV, newScrollY));
                            undos.Add(new UndoableProperty<EMP_ScrollKeyframe>(nameof(EMP_ScrollKeyframe.ScaleU), keyframe, keyframe.ScaleU, newScaleX));
                            undos.Add(new UndoableProperty<EMP_ScrollKeyframe>(nameof(EMP_ScrollKeyframe.ScaleV), keyframe, keyframe.ScaleV, newScaleY));

                            keyframe.ScrollU = newScrollX;
                            keyframe.ScrollV = newScrollY;
                            keyframe.ScaleU = newScaleX;
                            keyframe.ScaleV = newScaleY;
                        }
                    }
                }
            }

            //EmbEntry settings:
            newEmbEntry.SaveDds(false);
#if DEBUG
            newEmbEntry.LoadDds();

            if (newEmbEntry.Texture == null)
                throw new NullReferenceException("DdsImage couldn't be reloaded after merge.");
#endif


            newEmbEntry.Name = (newEmbEntry.Texture != null) ? $"SuperTexture ({newEmbEntry.Texture.GetHashCode()}).dds" : $"SuperTexture ({newEmbEntry.Data.GetHashCode()}).dds";

            //Delete all previous textures
            foreach (var entry in embEntries)
            {
                undos.Add(new UndoableListRemove<EmbEntry>(File3_Ref.Entry, entry));
                File3_Ref.Entry.Remove(entry);
            }

            //Add new texture
            undos.Add(new UndoableListAdd<EmbEntry>(File3_Ref.Entry, newEmbEntry));
            File3_Ref.Entry.Add(newEmbEntry);

        }

        public int[] CleanAllUnusedAndDuplicates(List<IUndoRedo> undos = null)
        {
            //ret = textures, materials
            int empTextures = 0;
            int textures = 0;
            int materials = 0;

            if (ContainerAssetType == AssetType.PBIND)
                empTextures += RemoveUnusedEmpTextures(undos);

            materials += MergeDuplicateMaterials(undos);
            textures += MergeDuplicateTextures(undos);
            materials += RemoveUnusedMaterials(undos);
            textures += RemoveUnusedTextures(undos);

            return new int[] { empTextures, textures, materials };
        }

        public int RemoveUnusedEmpTextures(List<IUndoRedo> undos = null)
        {
            int total = 0;

            foreach (Asset asset in Assets)
            {
                if (asset.Files.Count == 0) continue;
                if (asset.Files[0].EmpFile == null) continue;

                EMP_File empFile = asset.Files[0].EmpFile;

                for (int i = empFile.Textures.Count - 1; i >= 0; i--)
                {
                    if (empFile.GetNodesThatUseTexture(empFile.Textures[i]).Count == 0)
                    {
                        if (undos != null)
                        {
                            undos.Add(new UndoableListRemove<EMP_TextureSamplerDef>(empFile.Textures, empFile.Textures[i]));
                        }

                        total++;
                        empFile.Textures.RemoveAt(i);
                    }
                }
            }

            return total;
        }

        //Refactoring
        public void RefactorTextureRef(EmbEntry oldRef, EmbEntry newRef, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("RefactorTextureRef: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var texture in asset.Files[0].EmpFile.Textures)
                    {
                        if (texture.TextureRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<EMP_TextureSamplerDef>(nameof(EMP_TextureSamplerDef.TextureRef), texture, oldRef, newRef));
                            texture.TextureRef = newRef;
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.Textures)
                    {
                        if (texture.TextureRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<EMP_TextureSamplerDef>(nameof(EMP_TextureSamplerDef.TextureRef), texture, oldRef, newRef));
                            texture.TextureRef = newRef;
                        }
                    }
                }
            }
        }

        public void RefactorMaterialRef(EmmMaterial oldRef, EmmMaterial newRef, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("RefactorMaterialRef: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    RefactorMaterialRef_Recursive(asset.Files[0].EmpFile.ParticleNodes, oldRef, newRef, undos);
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var etrEntry in asset.Files[0].EtrFile.Nodes)
                    {
                        if (etrEntry.MaterialRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<ETR_Node>(nameof(ETR_Node.MaterialRef), etrEntry, oldRef, newRef));
                            etrEntry.MaterialRef = newRef;
                        }
                    }
                }
            }
        }

        private void RefactorMaterialRef_Recursive(IList<ParticleNode> childParticleEffects, EmmMaterial oldRef, EmmMaterial newRef, List<IUndoRedo> undos)
        {
            foreach (ParticleNode particleEffect in childParticleEffects)
            {
                if (particleEffect.NodeType == ParticleNodeType.Emission)
                {
                    if (particleEffect.EmissionNode.Texture.MaterialRef == oldRef)
                    {
                        undos.Add(new UndoableProperty<ParticleTexture>(nameof(particleEffect.EmissionNode.Texture.MaterialRef), particleEffect.EmissionNode.Texture, oldRef, newRef));
                        particleEffect.EmissionNode.Texture.MaterialRef = newRef;
                    }
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    RefactorMaterialRef_Recursive(particleEffect.ChildParticleNodes, oldRef, newRef, undos);
                }
            }

        }
        #endregion

        //Validation
        public void ValidateAssetNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < Assets.Count; i++)
            {
                for (int a = 0; a < Assets[i].Files.Count; a++)
                {
                    if (names.Contains(Assets[i].Files[a].FullFileName))
                    {
                        //Name was used previously
                        Assets[i].Files[a].SetName(GetUnusedName(Assets[i].Files[a].FullFileName));
                    }
                    else
                    {
                        //Name is unused
                        names.Add(Assets[i].Files[a].FullFileName);
                    }
                }
            }
        }

    }

    [Serializable]
    public class Asset : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //So we can identify assets accross multiple instances (mainly for copy/paste)
        public Guid InstanceID = Guid.NewGuid();


        public AssetType assetType { get; set; }

        public string FileNamesPreview
        {
            get
            {
                return Files.Count > 0 ? Files[0].FileName : "[No Files]";
            }
        }
        public string FileNamesPreviewWithExtension
        {
            get
            {
                return Files.Count > 0 ? Files[0].FullFileName : "[No Files]";
            }
        }
        public string FileNamePreviewWithAssetType
        {
            get
            {
                return Files.Count > 0 ? String.Format("[{1}] {0}", Files[0].FileName, assetType) : $"[{assetType}] [No Files]";
            }
        }


        private short _I_00_value = 0;
        public short I_00  //Still have no idea what this is
        {
            get
            {
                return this._I_00_value;
            }
            set
            {
                if (value != this._I_00_value)
                {
                    this._I_00_value = value;
                    NotifyPropertyChanged(nameof(I_00));
                }
            }
        }

        private AsyncObservableCollection<EffectFile> _filesValue = new AsyncObservableCollection<EffectFile>();
        public AsyncObservableCollection<EffectFile> Files
        {
            get
            {
                return this._filesValue;
            }
            set
            {
                if (value != this._filesValue)
                {
                    this._filesValue = value;
                    NotifyPropertyChanged(nameof(Files));
                    NotifyPropertyChanged(nameof(FileNamesPreview));
                }
            }
        }


        public bool Compare(Asset asset, AssetType type)
        {
            if (type == AssetType.TBIND || type == AssetType.PBIND || type == AssetType.CBIND)
            {
                //No comparison for these types, yet...
                return false;
            }
            else
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    if (Files[i].FullFileName != "NULL")
                    {
                        if (!Utils.CompareArray(Files[i].Bytes, asset.Files[i].Bytes) || Files[i].FullFileName != asset.Files[i].FullFileName)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void AddFile(object data, string name, EffectFile.FileType type, List<IUndoRedo> undos = null)
        {
            if (Files.Count == 5)
            {
                throw new InvalidOperationException("Cannot add file because the maximum allowed amount of 5 is already reached.");
            }

            EffectFile newEffectFile = null;

            switch (type)
            {
                case EffectFile.FileType.EMP:
                    newEffectFile = new EffectFile()
                    {
                        EmpFile = data as EMP_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.ETR:
                    newEffectFile = new EffectFile()
                    {
                        EtrFile = data as ETR_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.ECF:
                    newEffectFile = new EffectFile()
                    {
                        EcfFile = data as ECF_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.EMM:
                    newEffectFile = new EffectFile()
                    {
                        EmmFile = data as EMM_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.EMB:
                    newEffectFile = new EffectFile()
                    {
                        EmbFile = data as EMB_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.EMA:
                    newEffectFile = new EffectFile()
                    {
                        EmaFile = data as EMA_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                case EffectFile.FileType.EMO:
                    newEffectFile = new EffectFile()
                    {
                        EmoFile = data as EMO_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
                default:
                    if (data as byte[] == null)
                    {
                        throw new InvalidDataException(String.Format("EffectFile.AddFile: tried add undefined file type ({0}), but bytes was null.", type));
                    }
                    newEffectFile = new EffectFile()
                    {
                        Bytes = data as byte[],
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    };
                    break;
            }

            Files.Add(newEffectFile);

            if (undos != null)
            {
                undos.Add(new UndoableListAdd<EffectFile>(Files, newEffectFile));
            }

            RefreshNamePreview();
        }

        public void RemoveFile(EffectFile file, List<IUndoRedo> undos = null)
        {
            if (Files.Count == 1)
            {
                throw new InvalidOperationException("Cannot remove the last file.");
            }

            Files.Remove(file);

            if (undos != null)
                undos.Add(new UndoableListRemove<EffectFile>(Files, file));

            NotifyPropertyChanged(nameof(FileNamesPreview));
        }

        public void RefreshNamePreview()
        {
            NotifyPropertyChanged(nameof(FileNamesPreview));
            NotifyPropertyChanged(nameof(FileNamesPreviewWithExtension));
            NotifyPropertyChanged(nameof(FileNamePreviewWithAssetType));
        }

        /// <summary>
        /// EMP/ETR: DO NOT USE WITHOUT FIRST SETTING THE TEXTURE/MATERIAL INDEXES!!!!
        /// Other asset types are fine.
        /// </summary>
        /// <returns></returns>
        public Asset Clone()
        {
            Asset newAsset = new Asset();
            newAsset.I_00 = I_00;
            newAsset.Files = new AsyncObservableCollection<EffectFile>();

            foreach (var file in Files)
            {
                EffectFile newFile = new EffectFile();
                newFile.OriginalFileName = file.OriginalFileName;
                newFile.FileName = file.FileName;
                newFile.Extension = file.Extension;
                newFile.fileType = file.fileType;

                if (newFile.fileType == EffectFile.FileType.EMO && EepkToolInterlop.FullDecompile)
                {
                    newFile.EmoFile = file.EmoFile.Copy();
                }
                else if (newFile.fileType == EffectFile.FileType.ECF)
                {
                    newFile.EcfFile = file.EcfFile.Copy();
                }
                else if (newFile.fileType == EffectFile.FileType.EMP)
                {
                    newFile.EmpFile = file.EmpFile.Copy();
                    EffectFile.CopyEmpRef(file.EmpFile, newFile.EmpFile);
                }
                else if (newFile.fileType == EffectFile.FileType.ETR)
                {
                    newFile.EtrFile = file.EtrFile.Copy();
                    EffectFile.CopyEtrRef(file.EtrFile, newFile.EtrFile);
                }
                else if (newFile.fileType == EffectFile.FileType.EMB)
                {
                    newFile.EmbFile = file.EmbFile.Copy();
                }
                else if (newFile.fileType == EffectFile.FileType.EMM)
                {
                    newFile.EmmFile = file.EmmFile.Copy();
                }
                else if (newFile.fileType == EffectFile.FileType.EMA && EepkToolInterlop.FullDecompile)
                {
                    newFile.EmaFile = file.EmaFile.Copy();
                }
                else if(file.Bytes != null)
                {
                    newFile.Bytes = file.Bytes.Copy();
                }

                newAsset.Files.Add(newFile);

            }
            return newAsset;
        }

        /// <summary>
        /// Create a new Asset with no files.
        /// </summary>
        public static Asset Create(AssetType assetType)
        {
            Asset asset = new Asset();
            asset.assetType = assetType;

            return asset;
        }

        /// <summary>
        /// Create a new Asset out of a single file.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Asset Create(object data, string name, EffectFile.FileType fileType, AssetType assetType)
        {
            Asset asset = new Asset();
            asset.assetType = assetType;
            asset.AddFile(data, name, fileType);
            return asset;
        }

        public bool HasSameFileNames(Asset asset)
        {
            if (Files.Count == asset.Files.Count)
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    if (Files[i].FullFileName != asset.Files[i].FullFileName)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors;

            switch (assetType)
            {
                case AssetType.PBIND:
                    colors = Files[0].EmpFile.GetUsedColors();
                    break;
                case AssetType.TBIND:
                    colors = Files[0].EtrFile.GetUsedColors();
                    break;
                case AssetType.CBIND:
                    colors = Files[0].EcfFile.GetUsedColors();
                    break;
                case AssetType.LIGHT:
                    colors = Files[0].EmaFile.GetUsedColors();
                    break;
                case AssetType.EMO:
                    colors = new List<RgbColor>();

                    foreach (var file in Files)
                    {
                        switch (file.Extension)
                        {
                            case ".emb":
                                colors.AddRange(file.EmbFile.GetUsedColors());
                                break;
                            case ".emm":
                                colors.AddRange(file.EmmFile.GetUsedColors());
                                break;
                        }
                    }

                    break;
                default:
                    throw new InvalidOperationException(string.Format("Asset.GetUsedColors: Not supported for {0}.", assetType));
            }

            return colors;
        }

    }

    [Serializable]
    public class EffectFile : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public enum FileType
        {
            EMP,
            ETR,
            ECF,
            EMB,
            EMM,
            EMA,
            EMO,
            Other
        }

        public FileType fileType { get; set; }
        public string FullFileName
        {
            get
            {
                return FileName + Extension;
            }
        }
        public string EmoFullFileNamePreview //UI Preview
        {
            get
            {
                return $"------> [FILE] {FullFileName}";
            }
        }

        private string _fileName = null;
        public string FileName
        {
            get
            {
                return this._fileName;
            }
            set
            {
                if (value != this._fileName)
                {
                    this._fileName = value;
                    NotifyPropertyChanged(nameof(FileName));
                    NotifyPropertyChanged(nameof(FullFileName));
                    NotifyPropertyChanged(nameof(EmoFullFileNamePreview));
                }
            }
        }

        private string _extension = null;
        public string Extension
        {
            get
            {
                return this._extension;
            }
            set
            {
                if (value != this._extension)
                {
                    this._extension = value;
                    NotifyPropertyChanged("FileName");
                    NotifyPropertyChanged("FullFileName");
                }
            }
        }

        public string OriginalFileName { get; set; } //Original name before importing, which we save to show on the UI as a helper.

        //File data
        public EMP_File EmpFile { get; set; } = null;
        public ETR_File EtrFile { get; set; } = null;
        public ECF_File EcfFile { get; set; } = null;
        public EMB_File EmbFile { get; set; } = null;
        public EMM_File EmmFile { get; set; } = null;
        public EMA_File EmaFile { get; set; } = null;
        public EMO_File EmoFile { get; set; } = null;
        public byte[] Bytes { get; set; } = null;

        public void SetName(string name)
        {
            Extension = GetExtension(name);
            FileName = GetFileNameWithoutExtension(name);

            if (OriginalFileName == null)
            {
                OriginalFileName = name;
            }
        }

        //Gets full recursive extension out of the pre-defined support formats.
        public static string GetExtension(string fileName)
        {
            if (EffectContainerFile.SupportedFormats.Contains(Path.GetExtension(fileName)))
            {
                string ext1 = Path.GetExtension(fileName);

                if (EffectContainerFile.SupportedFormats.Contains(Path.GetExtension(Path.GetFileNameWithoutExtension(fileName))))
                {
                    string ext2 = Path.GetExtension(Path.GetFileNameWithoutExtension(fileName));

                    return ext2 + ext1;
                }
                else
                {
                    return ext1;
                }
            }

            return null;
        }

        public static string GetFileNameWithoutExtension(string fileName)
        {
            if (EffectContainerFile.SupportedFormats.Contains(Path.GetExtension(fileName)))
            {

                if (EffectContainerFile.SupportedFormats.Contains(Path.GetExtension(Path.GetFileNameWithoutExtension(fileName))))
                {
                    return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
                }
                else
                {
                    return Path.GetFileNameWithoutExtension(fileName);
                }
            }

            return null;
        }

        public static FileType GetFileType(string fileName)
        {
            switch (GetExtension(fileName))
            {
                case ".emo":
                    return FileType.EMO;
                case ".emb":
                    return FileType.EMB;
                case ".emm":
                    return FileType.EMM;
                case ".emp":
                    return FileType.EMP;
                case ".etr":
                    return FileType.ETR;
                case ".ecf":
                    return FileType.ECF;
                case ".obj.ema":
                case ".mat.ema":
                case ".light.ema":
                case ".ema":
                    return FileType.EMA;
                default:
                    throw new InvalidDataException(String.Format("GetFileType: Unrecognized asset file type = {0}.", GetExtension(fileName)));
            }
        }

        //Copy ref
        /// <summary>
        /// Copy all material and texture references from oldFile to newFile. The files must have an equal amount of entries for it to be successful.
        /// </summary>
        public static void CopyEmpRef(EMP_File oldFile, EMP_File newFile)
        {
            if (oldFile.ParticleNodes.Count != newFile.ParticleNodes.Count)
                throw new InvalidDataException("CopyEmpRef: oldFile and newFile ParticleNode count is out of sync.");
            if (oldFile.Textures.Count != newFile.Textures.Count)
                throw new InvalidDataException("CopyEmpRef: oldFile and newFile Texture count is out of sync.");

            //Copy particle effect material references
            CopyEmpRef_Recursive(oldFile.ParticleNodes, newFile.ParticleNodes);

            //Copy texture EmbEntry references
            for (int i = 0; i < oldFile.Textures.Count; i++)
            {
                newFile.Textures[i].TextureRef = oldFile.Textures[i].TextureRef;
            }
        }

        private static void CopyEmpRef_Recursive(AsyncObservableCollection<ParticleNode> oldFile, AsyncObservableCollection<ParticleNode> newFile)
        {
            if (oldFile.Count != newFile.Count)
                throw new InvalidDataException("CopyEmpRef_Recursive: oldFile and newFile ParticleNode count is out of sync.");

            for (int i = 0; i < oldFile.Count; i++)
            {
                if (oldFile[i].NodeType == ParticleNodeType.Emission)
                {
                    newFile[i].EmissionNode.Texture.MaterialRef = oldFile[i].EmissionNode.Texture.MaterialRef;
                }

                if (oldFile[i].ChildParticleNodes != null)
                {
                    CopyEmpRef_Recursive(oldFile[i].ChildParticleNodes, newFile[i].ChildParticleNodes);
                }
            }
        }

        /// <summary>
        /// Copy all material and texture references from oldFile to newFile. The files must have an equal amount of entries for it to be successful.
        /// </summary>
        public static void CopyEtrRef(ETR_File oldFile, ETR_File newFile)
        {
            if (oldFile.Nodes.Count != newFile.Nodes.Count)
                throw new InvalidDataException("CopyEtrRef: oldFile and newFile Entry count is out of sync.");
            if (oldFile.Textures.Count != newFile.Textures.Count)
                throw new InvalidDataException("CopyEtrRef: oldFile and newFile Texture count is out of sync.");

            //Copy trace effect material references
            for (int i = 0; i < oldFile.Nodes.Count; i++)
            {
                newFile.Nodes[i].MaterialRef = oldFile.Nodes[i].MaterialRef;
            }

            //Copy texture EmbEntry references
            for (int i = 0; i < oldFile.Textures.Count; i++)
            {
                newFile.Textures[i].TextureRef = oldFile.Textures[i].TextureRef;
            }
        }

        public bool HasValidData()
        {
            switch (fileType)
            {
                case FileType.EMM:
                    if (EmmFile == null) return false;
                    break;
                case FileType.EMB:
                    if (EmbFile == null) return false;
                    break;
                case FileType.EMP:
                    if (EmpFile == null) return false;
                    break;
                case FileType.ETR:
                    if (EtrFile == null) return false;
                    break;
                case FileType.ECF:
                    if (EcfFile == null) return false;
                    break;
                case FileType.EMA:
                    if (!EepkToolInterlop.FullDecompile) goto default;
                    if (EmaFile == null) return false;
                    break;
                case FileType.EMO:
                    if (!EepkToolInterlop.FullDecompile) goto default;
                    if (EmoFile == null) return false;
                    break;
                default:
                    if (Bytes == null) return false;
                    break;
            }

            return true;
        }

        public byte[] GetBytes()
        {
            switch (fileType)
            {
                case FileType.ECF:
                    return EcfFile.SaveToBytes();
                case FileType.EMA:
                    return EepkToolInterlop.FullDecompile ? EmaFile.Write() : Bytes;
                case FileType.EMB:
                    return EmbFile.SaveToBytes();
                case FileType.EMM:
                    return EmmFile.SaveToBytes();
                case FileType.EMP:
                    return EmpFile.SaveToBytes();
                case FileType.ETR:
                    return EtrFile.Write();
                case FileType.EMO:
                    return EepkToolInterlop.FullDecompile ? EmoFile.Write() : Bytes;
                case FileType.Other:
                    return Bytes;
                default:
                    throw new Exception(string.Format("EffectFile.GetBytes(): Unknown fileType = {0}", fileType));
            }
        }
    }

}
