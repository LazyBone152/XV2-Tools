using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP;
using Xv2CoreLib.ETR;
using Xv2CoreLib.ECF;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Xv2CoreLib.EMA;
using System.IO.Compression;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EffectContainer
{
    public enum SaveFormat
    {
        Binary,
        ZIP
    }

    [Serializable]
    public class EffectContainerFile : INotifyPropertyChanged, IIsNull
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
        private SaveFormat _saveFormat = SaveFormat.Binary;
        public SaveFormat saveFormat
        {
            get
            {
                return this._saveFormat;
            }
            set
            {
                if (value != this._saveFormat)
                {
                    this._saveFormat = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayName");
                    NotifyPropertyChanged("saveFormat");
                }
            }
        }

        //Name/Directory 
        private string _name = null;
        private string _directory = null;

        /// <summary>
        /// The name of the EEPK file, minus the extension.
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        /// <summary>
        /// The directory the EEPK is saved at. If in ZIP save mode, then this is the path to the ZIP file, minus the extension.
        /// </summary>
        public string Directory
        {
            get
            {
                return this._directory;
            }
            set
            {
                if (value != this._directory)
                {
                    this._directory = value;
                    NotifyPropertyChanged("Directory");
                    NotifyPropertyChanged("DisplayName");
                    NotifyPropertyChanged("CanSave");
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
                    return (saveFormat == SaveFormat.Binary) ? String.Format("_{0}{1}{2}.eepk", Directory, hasDir, Name) : String.Format("_{0}{1}", Directory, ZipExtension);
                }
                else
                {
                    return (saveFormat == SaveFormat.Binary) ? String.Format("<_{0}{1}{2}.eepk>", Directory, hasDir, Name) : String.Format("<_{0}{1}>", Directory, ZipExtension);
                }
            }
        }
        public bool CanSave
        {
            get
            {
                return Path.IsPathRooted(Directory);
            }
        }
        public string FullFilePath
        {
            get
            {
                if(saveFormat == SaveFormat.ZIP)
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
                    NotifyPropertyChanged("Version");
                }
            }
        }

        //For tracking files that were loaded with this EEPK (disabled for ZIP format)
        public List<string> LoadedExternalFiles = new List<string>();

        //For tracking files that were loaded with this EEPK, but weren't accounted for when saving last (which would happen if they were deleted, for example)
        public List<string> LoadedExternalFilesNotSaved = new List<string>();

        //File IO
        [NonSerialized]
        private Resource.Xv2FileIO xv2FileIO = null;
        private bool OnlyLoadFromCpk = false;
        [NonSerialized]
        private Resource.ZipReader zipReader = null;
        [NonSerialized]
        private Resource.ZipWriter zipWriter = null;

        //Asset containers
        public AssetContainerTool Pbind { get; set; }
        public AssetContainerTool Tbind { get; set; }
        public AssetContainerTool Cbind { get; set; }
        public AssetContainerTool Emo { get; set; }
        public AssetContainerTool LightEma { get; set; }

        //Effects
        public ObservableCollection<Effect> Effects { get; set; }

        #region UiProperties
        //SelectedEffect
        [NonSerialized]
        private Effect _selectedEffect = null;
        public Effect SelectedEffect
        {
            get
            {
                return this._selectedEffect;
            }
            set
            {
                if (value != this._selectedEffect)
                {
                    this._selectedEffect = value;
                    NotifyPropertyChanged("SelectedEffect");
                }
            }
        }

        //Filters
        [NonSerialized]
        private string _effectSearchFilter = null;
        public string EffectSearchFilter
        {
            get
            {
                return this._effectSearchFilter;
            }
            set
            {
                if (value != this._effectSearchFilter)
                {
                    this._effectSearchFilter = value;
                    NotifyPropertyChanged("EffectSearchFilter");
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
                _viewEffects = new ListCollectionView(Effects);
                _viewEffects.Filter = new Predicate<object>(EffectFilterCheck);
                return _viewEffects;
            }
            set
            {
                if (value != _viewEffects)
                {
                    _viewEffects = value;
                    NotifyPropertyChanged("ViewEffects");
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

                foreach(var effectPart in _effect.EffectParts)
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
                _viewEffects = new ListCollectionView(Effects);

            _viewEffects.Filter = new Predicate<object>(EffectFilterCheck);
            NotifyPropertyChanged("ViewEffects");
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
            var undos = AddEffects(effects);
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

        public List<IUndoRedo> AddEffect(Effect effect, bool allowNullAssets = false)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Add the assets used
            foreach(var effectPart in effect.EffectParts)
            {
                if (effectPart.AssetRef == null && allowNullAssets)
                {
                    //Null and its allowed, so OK
                }
                if (effectPart.AssetRef != null)
                {
                    effectPart.AssetRef = AddAsset(effectPart.AssetRef, effectPart.I_02, undos);
                }
                else if (!allowNullAssets)
                {
                    throw new NullReferenceException(String.Format("AddEffect: Effect {0} contains an EffectPart with a null asset reference. Cannot continue.", effect.IndexNum));
                }
            }

            //Add the effect
            Effects.Add(effect);
            undos.Add(new UndoableListAdd<Effect>(Effects, effect));

            return undos;
        }

        public List<IUndoRedo> AddEffects(IList<Effect> effects, bool allowNullAssets = false)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var effect in effects)
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
            newFile.Effects = new ObservableCollection<Effect>();
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
            return Load(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), null, false, null, SaveFormat.Binary);
        }

        /// <summary>
        /// Load an eepk + assets from the game.
        /// </summary>
        /// <param name="path">A relative path from the game data folder.</param>
        /// <param name="_fileIO">The Xv2FileIO object to load from.</param>
        /// <returns></returns>
        public static EffectContainerFile Load(string path, Resource.Xv2FileIO _fileIO, bool onlyFromCpk)
        {
            return Load(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), _fileIO, onlyFromCpk, null, SaveFormat.Binary);
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

            return Load(dir, Path.GetFileNameWithoutExtension(eepkPath), null, false, zipReader, SaveFormat.ZIP);
        }

        /// <summary>
        /// Load an eepk + assets. (Direct load, from game or from zip... depending on parameters)
        /// </summary>
        /// <param name="path">An absolute path to the eepk or a relative path from the game data folder (used with _fileIO).</param>
        /// <param name="_fileIO">Pass this in if loading from the game. Leave null if loading a eepk directly.</param>
        /// <returns></returns>
        private static EffectContainerFile Load(string dir, string name, Resource.Xv2FileIO _fileIO = null, bool onlyFromCpk = false, Resource.ZipReader _zipReader = null, SaveFormat _saveFormat = SaveFormat.Binary)
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
            if((effectContainerFile.Version == VersionEnum.SDBH || effectContainerFile.Version == (VersionEnum)1))
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

            return effectContainerFile;
        }
        
        public static EffectContainerFile LoadVfx2(Stream stream, string path)
        {
            using (Resource.ZipReader reader = new Resource.ZipReader(new ZipArchive(stream, ZipArchiveMode.Read)))
            {
                var vfxFile = Load(path, reader);
                vfxFile.zipReader = null;
                return vfxFile;
            }
        }

        public static EffectContainerFile LoadVfx2(string path)
        {
            using (Resource.ZipReader reader = new Resource.ZipReader(ZipFile.Open(path, ZipArchiveMode.Read)))
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
                        container.File1_Ref = EMB_File.LoadEmb(LoadExternalFile(String.Format("{0}/{1}", Directory, container.File1_Name)));
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
                        container.File2_Ref = EMM_File.LoadEmm(LoadExternalFile(String.Format("{0}/{1}", Directory, container.File2_Name)));
                        container.File3_Ref = EMB_File.LoadEmb(LoadExternalFile(String.Format("{0}/{1}", Directory, container.File3_Name)));
                    }
                    break;
                case AssetType.CBIND:
                case AssetType.LIGHT:
                    if (!container.LooseFiles)
                    {
                        container.File1_Ref = EMB_File.LoadEmb(LoadExternalFile(String.Format("{0}/{1}", Directory, container.File1_Name)));
                    }
                    break;
                case AssetType.EMO:
                    break;
                default:
                    throw new InvalidOperationException(String.Format("LoadAssetContainer: Unrecognized asset type: {0}", type));
            }

            //Load the actual assets and link them (if needed)
            for (int i = 0; i < container.Assets.Count; i++)
            {
                foreach (var file in container.Assets[i].Files)
                {
                    if (file.FullFileName != "NULL")
                    {
                        //Get the file bytes
                        List<byte> fileBytes = null;
                        byte[] _fileBytes = null;
                        if (container.LooseFiles || type == AssetType.EMO)
                        {
                            _fileBytes = LoadExternalFile(String.Format("{0}/{1}", Directory, file.FullFileName));
                            fileBytes = _fileBytes.ToList();
                        }
                        else
                        {
                            var entry = container.File1_Ref.GetEntry(i);
                            if (entry == null) throw new FileNotFoundException(string.Format("Could not find file \"{0}\" in \"{1}\".\n\nThis is possibly caused by a corrupted eepk file.", file.FullFileName, container.File1_Name));

                            fileBytes = entry.Data;
                            _fileBytes = fileBytes.ToArray();
                        }


                        switch (file.Extension)
                        {
                            case ".emp":
                                file.EmpFile = EMP_File.Load(fileBytes, ParserMode.Tool);
                                file.fileType = EffectFile.FileType.EMP;
                                break;
                            case ".etr":
                                file.EtrFile = ETR_File.Load(_fileBytes);
                                file.fileType = EffectFile.FileType.ETR;
                                break;
                            case ".ecf":
                                file.EcfFile = ECF_File.Load(fileBytes);
                                file.fileType = EffectFile.FileType.ECF;
                                break;
                            case ".emb":
                                file.EmbFile = EMB_File.LoadEmb(fileBytes.ToArray());
                                file.fileType = EffectFile.FileType.EMB;
                                break;
                            case ".emm":
                                file.EmmFile = EMM_File.LoadEmm(fileBytes.ToArray());
                                file.fileType = EffectFile.FileType.EMM;
                                break;
                            case ".light.ema":
                            case ".ema":
                                file.EmaFile = EMA_File.Load(fileBytes.ToArray());
                                file.fileType = EffectFile.FileType.EMA;
                                break;
                            default:
                                file.Bytes = _fileBytes;
                                file.fileType = EffectFile.FileType.Other;
                                break;
                        }
                    }

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
        public bool SaveVfx2()
        {
            if (saveFormat != SaveFormat.ZIP) throw new InvalidOperationException("SaveVfx2: saveFormat is not set to VFX2.");

            bool result;

            using (Resource.ZipWriter writer = new Resource.ZipWriter(ZipFile.Open(string.Format("{0}{1}", Directory, ZipExtension), ZipArchiveMode.Update)))
            {
                zipWriter = writer;
                result = Save();
                zipWriter = null;
            }

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
            SaveFile(eepkFile.SaveToBytes(), String.Format("{0}/{1}.eepk", Directory, Name));

            //Save containers
            //If a container has no assets, it will be ignored. The binary eepk also omits it.
            if(Pbind.Assets.Count > 0)
            {
                if (!Pbind.LooseFiles)
                {
                    ExternalFileSaved(String.Format("{0}/{1}", Directory, Pbind.File1_Name));
                    SaveFile(Pbind.File1_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Pbind.File1_Name));
                }
                ExternalFileSaved(String.Format("{0}/{1}", Directory, Pbind.File2_Name));
                ExternalFileSaved(String.Format("{0}/{1}", Directory, Pbind.File3_Name));
                SaveFile(Pbind.File2_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Pbind.File2_Name));
                SaveFile(Pbind.File3_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Pbind.File3_Name));

                //Lose files
                if (Pbind.LooseFiles)
                {
                    foreach(var asset in Pbind.Assets)
                    {
                        ExternalFileSaved(String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EmpFile.SaveToBytes(ParserMode.Tool), String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }

            }

            if (Tbind.Assets.Count > 0)
            {
                if (!Tbind.LooseFiles)
                {
                    ExternalFileSaved(String.Format("{0}/{1}", Directory, Tbind.File1_Name));
                    SaveFile(Tbind.File1_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Tbind.File1_Name));
                }
                ExternalFileSaved(String.Format("{0}/{1}", Directory, Tbind.File2_Name));
                ExternalFileSaved(String.Format("{0}/{1}", Directory, Tbind.File3_Name));
                SaveFile(Tbind.File2_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Tbind.File2_Name));
                SaveFile(Tbind.File3_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Tbind.File3_Name));
                
                //Lose files
                if (Tbind.LooseFiles)
                {
                    foreach (var asset in Tbind.Assets)
                    {
                        ExternalFileSaved(String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EtrFile.Save(false), String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }
            }

            if (Cbind.Assets.Count > 0)
            {
                if (!Cbind.LooseFiles)
                {
                    ExternalFileSaved(String.Format("{0}/{1}", Directory, Cbind.File1_Name));
                    SaveFile(Cbind.File1_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, Cbind.File1_Name));
                }

                //Lose files
                if (Cbind.LooseFiles)
                {
                    foreach (var asset in Cbind.Assets)
                    {
                        ExternalFileSaved(String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                        SaveFile(asset.Files[0].EcfFile.SaveToBytes(), String.Format("{0}/{1}", Directory, asset.Files[0].FullFileName));
                    }
                }
            }

            if(Emo.Assets.Count > 0)
            {
                if (Emo.LooseFiles)
                {
                    foreach(var asset in Emo.Assets)
                    {
                        foreach(var file in asset.Files)
                        {
                            if(file.HasValidData() && file.FullFileName != "NULL")
                            {
                                ExternalFileSaved(String.Format("{0}/{1}", Directory, file.FullFileName));

                                switch (file.fileType)
                                {
                                    case EffectFile.FileType.EMM:
                                        SaveFile(file.EmmFile.SaveToBytes(), String.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    case EffectFile.FileType.EMB:
                                        SaveFile(file.EmbFile.SaveToBytes(), String.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    case EffectFile.FileType.EMA:
                                        SaveFile(file.EmaFile.Write(), String.Format("{0}/{1}", Directory, file.FullFileName));
                                        break;
                                    default:
                                        SaveFile(file.Bytes, String.Format("{0}/{1}", Directory, file.FullFileName));
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
                    foreach (var asset in LightEma.Assets)
                    {
                        foreach (var file in asset.Files)
                        {
                            if(file.fileType == EffectFile.FileType.EMA)
                            {
                                ExternalFileSaved(String.Format("{0}/{1}", Directory, file.FullFileName));
                                SaveFile(file.EmaFile.Write(), String.Format("{0}/{1}", Directory, file.FullFileName));
                            }
                            else if (file.Bytes != null && file.FullFileName != "NULL")
                            {
                                ExternalFileSaved(String.Format("{0}/{1}", Directory, file.FullFileName));
                                SaveFile(file.Bytes, String.Format("{0}/{1}", Directory, file.FullFileName));
                            }
                        }
                    }
                }
                else if(!LightEma.LooseFiles)
                {
                    ExternalFileSaved(String.Format("{0}/{1}", Directory, LightEma.File1_Name));
                    SaveFile(LightEma.File1_Ref.SaveToBytes(), String.Format("{0}/{1}", Directory, LightEma.File1_Name));
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

                foreach(var asset in Pbind.Assets)
                {
                    byte[] bytes = asset.Files[0].EmpFile.SaveToBytes(ParserMode.Tool);
                    Pbind.File1_Ref.Entry.Add(new EMB_CLASS.EmbEntry()
                    {
                        Data = bytes.ToList(),
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
                    byte[] bytes = asset.Files[0].EtrFile.Save(false);
                    Tbind.File1_Ref.Entry.Add(new EMB_CLASS.EmbEntry()
                    {
                        Data = bytes.ToList(),
                        Name = asset.Files[0].FullFileName
                    });
                }
            }

            if (!Cbind.LooseFiles)
            {
                Cbind.File1_Ref = EMB_File.DefaultEmbFile(false);
                Cbind.File1_Ref.UseFileNames = true;

                foreach (var asset in Cbind.Assets)
                {
                    byte[] bytes = asset.Files[0].EcfFile.SaveToBytes();
                    Cbind.File1_Ref.Entry.Add(new EMB_CLASS.EmbEntry()
                    {
                        Data = bytes.ToList(),
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
                    if(asset.Files[0].fileType == EffectFile.FileType.EMA)
                    {
                        LightEma.File1_Ref.Entry.Add(new EMB_CLASS.EmbEntry()
                        {
                            Data = asset.Files[0].EmaFile.Write().ToList(),
                            Name = asset.Files[0].FullFileName
                        });
                    }
                    else
                    {
                        byte[] bytes = asset.Files[0].Bytes;
                        LightEma.File1_Ref.Entry.Add(new EMB_CLASS.EmbEntry()
                        {
                            Data = bytes.ToList(),
                            Name = asset.Files[0].FullFileName
                        });
                    }
                }
            }


        }

        private void ValidateContainerFileNames()
        {
            //Ensure that all containers have a valid name
            //PBIND
            if (Pbind.File2_Name == "NULL" || EepkToolInterlop.AutoRenameContainers)
                Pbind.File2_Name = String.Format("{0}.ptcl.emm", GetDefaultEepkName());

            if (Pbind.File3_Name == "NULL" || EepkToolInterlop.AutoRenameContainers)
                Pbind.File3_Name = String.Format("{0}.ptcl.emb", GetDefaultEepkName());

            if(Pbind.File1_Name == "NULL" && !Pbind.LooseFiles)
            {
                Pbind.File1_Name = String.Format("{0}.pbind.emb", GetDefaultEepkName());
            }
            else if (!Pbind.LooseFiles && EepkToolInterlop.AutoRenameContainers)
            {
                Pbind.File1_Name = String.Format("{0}.pbind.emb", GetDefaultEepkName());
            }
            else if (Pbind.LooseFiles)
            {
                Pbind.File1_Name = "NULL";
            }

            //TBIND
            if (Tbind.File2_Name == "NULL" || EepkToolInterlop.AutoRenameContainers)
                Tbind.File2_Name = String.Format("{0}.trc.emm", GetDefaultEepkName());

            if (Tbind.File3_Name == "NULL" || EepkToolInterlop.AutoRenameContainers)
                Tbind.File3_Name = String.Format("{0}.trc.emb", GetDefaultEepkName());

            if (Tbind.File1_Name == "NULL" && !Tbind.LooseFiles)
            {
                Tbind.File1_Name = String.Format("{0}.tbind.emb", GetDefaultEepkName());
            }
            else if (!Tbind.LooseFiles && EepkToolInterlop.AutoRenameContainers)
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
            else if (!Cbind.LooseFiles && EepkToolInterlop.AutoRenameContainers)
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
            else if (!LightEma.LooseFiles && EepkToolInterlop.AutoRenameContainers)
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
        
        #endregion

        #region ExternalAssetFiles
        //Loading
        private byte[] LoadExternalFile(string path, bool log = true)
        {
            if(log && saveFormat != SaveFormat.ZIP)
                LoadedExternalFiles.Add(path);

            return GetFile(path, xv2FileIO, OnlyLoadFromCpk, zipReader);
        }

        private void ExternalFileSaved(string path)
        {
            if (saveFormat == SaveFormat.ZIP) return;

            LoadedExternalFilesNotSaved.Remove(path);
            LoadedExternalFiles.Add(path);
        }

        private void InitExternalLoadedFileNotSavedList()
        {
            LoadedExternalFilesNotSaved.Clear();

            foreach(var str in LoadedExternalFiles)
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

            if(xv2FileIO != null)
            {
                return xv2FileIO.FileExists(path);
            }

            return false;
        }

        //Saving
        private void SaveFile(byte[] bytes, string path)
        {
            if(saveFormat == SaveFormat.Binary)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);
            }
            else if (saveFormat == SaveFormat.ZIP)
            {
                zipWriter.AddFile(Path.GetFileName(path), bytes);
            }
        }
        #endregion

        #region LinkingFunctions
        private void PbindLinkTextureAndMaterial()
        {
            if(Pbind != null)
            {
                int index = 0;
                foreach(var empAsset in Pbind.Assets)
                {
                    //Validation
                    if(empAsset.Files.Count < 1)
                    {
                        throw new InvalidDataException(String.Format("Cant find PBIND asset at index {0}.", index));
                    }
                    if (empAsset.Files[0].EmpFile == null)
                    {
                        throw new InvalidDataException(String.Format("The EMP file at index {0} was null (name: {1}).", index, empAsset.Files[0].FullFileName));
                    }

                    EMP_File empFile = empAsset.Files[0].EmpFile;

                    //Materials
                    foreach(var empEntry in empFile.ParticleEffects)
                    {
                        if(empEntry.Type_Texture != null)
                        {
                            empEntry.Type_Texture.MaterialRef = (empEntry.Type_Texture.I_16 != ushort.MaxValue) ? Pbind.File2_Ref.GetEntry(empEntry.Type_Texture.I_16) : null;
                        }

                        if(empEntry.ChildParticleEffects != null)
                        {
                            PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleEffects);
                        }

                    }

                    //Textures
                    foreach (var empTexture in empFile.Textures)
                    {
                        if(empTexture.I_01 >= byte.MinValue && empTexture.I_01 < sbyte.MaxValue + 1)
                        {
                            empTexture.TextureRef = Pbind.File3_Ref.GetEntry(empTexture.I_01);
                        }
                        else if(empTexture.I_01 != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("PbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\nptcl.emb can only have a maximum of 128 textures.", empTexture.I_01));
                        }
                    }
                    
                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void PbindLinkTextureAndMaterial_Recursive(ObservableCollection<ParticleEffect> childrenParticleEffects)
        {
            foreach (var empEntry in childrenParticleEffects)
            {
                if (empEntry.Type_Texture != null)
                {
                    empEntry.Type_Texture.MaterialRef = (empEntry.Type_Texture.I_16 != ushort.MaxValue) ? Pbind.File2_Ref.GetEntry(empEntry.Type_Texture.I_16) : null;
                }

                if (empEntry.ChildParticleEffects != null)
                {
                    PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleEffects);
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
                    foreach (var etrEntry in etrFile.ETR_Entries)
                    {
                        etrEntry.MaterialRef = Tbind.File2_Ref.GetEntry(etrEntry.I_108);
                    }

                    //Textures
                    foreach (var etrTexture in etrFile.ETR_TextureEntries)
                    {
                        if (etrTexture.I_01 >= byte.MinValue && etrTexture.I_01 < sbyte.MaxValue + 1)
                        {
                            etrTexture.TextureRef = Tbind.File3_Ref.GetEntry(etrTexture.I_01);
                        }
                        else if (etrTexture.I_01 != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("TbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\trc.emb can only have a maximum of 128 textures.", etrTexture.I_01));
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
                    foreach (var empEntry in empFile.ParticleEffects)
                    {
                        if (empEntry.Type_Texture != null)
                        {
                            int matIdx = Pbind.File2_Ref.Materials.IndexOf(empEntry.Type_Texture.MaterialRef);

                            if(matIdx == -1 && empEntry.Type_Texture.MaterialRef != null)
                            {
                                throw new InvalidOperationException("PbindSetTextureAndMaterialIndex: material not found.");
                            }

                            empEntry.Type_Texture.I_16 = (matIdx != -1) ? (ushort)matIdx : ushort.MaxValue;
                        }

                        if (empEntry.ChildParticleEffects != null)
                        {
                            PbindSetTextureAndMaterialIndex_Recursive(empEntry.ChildParticleEffects);
                        }

                    }

                    //Textures
                    foreach (var empTexture in empFile.Textures)
                    {
                        if(empTexture.TextureRef != null)
                        {
                            int textureIdx = Pbind.File3_Ref.Entry.IndexOf(empTexture.TextureRef);

                            if (textureIdx == -1)
                            {
                                //A texture is assigned, but it wasn't found in the texture container.
                                throw new InvalidOperationException("PbindSetTextureAndMaterialIndex: texture not found.");
                            }

                            empTexture.I_01 = (byte)textureIdx;
                        }
                        else
                        {
                            //No assigned texture. 
                            empTexture.I_01 = byte.MaxValue;
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void PbindSetTextureAndMaterialIndex_Recursive(ObservableCollection<ParticleEffect> childrenParticleEffects)
        {
            foreach (var empEntry in childrenParticleEffects)
            {
                if (empEntry.Type_Texture != null)
                {
                    int matIdx = Pbind.File2_Ref.Materials.IndexOf(empEntry.Type_Texture.MaterialRef);

                    if (matIdx == -1 && empEntry.Type_Texture.MaterialRef != null)
                    {
                        throw new InvalidOperationException("PbindSetTextureAndMaterialIndex_Recursive: material not found.");
                    }

                    empEntry.Type_Texture.I_16 = (matIdx != -1) ? (ushort)matIdx : ushort.MaxValue;
                }

                if (empEntry.ChildParticleEffects != null)
                {
                    PbindSetTextureAndMaterialIndex_Recursive(empEntry.ChildParticleEffects);
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
                    foreach (var etrEntry in etrFile.ETR_Entries)
                    {
                        int matIdx = Tbind.File2_Ref.Materials.IndexOf(etrEntry.MaterialRef);

                        if (matIdx == -1)
                        {
                            throw new InvalidOperationException("TbindSetTextureAndMaterialIndex: material not found.");
                        }

                        etrEntry.I_108 = (ushort)matIdx;
                    }

                    //Textures
                    foreach (var etrTexture in etrFile.ETR_TextureEntries)
                    {
                        if(etrTexture.TextureRef != null)
                        {
                            int textureIdx = Tbind.File3_Ref.Entry.IndexOf(etrTexture.TextureRef);

                            if (textureIdx == -1)
                            {
                                throw new InvalidOperationException("TbindSetTextureAndMaterialIndex: texture not found.");
                            }

                            etrTexture.I_01 = (byte)textureIdx;
                        }
                        else
                        {
                            etrTexture.I_01 = byte.MaxValue;
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
                        switch (effectPart.I_02)
                        {
                            case AssetType.EMO:
                                effectPart.AssetRef = Emo.Assets[effectPart.I_00];
                                break;
                            case AssetType.PBIND:
                                effectPart.AssetRef = Pbind.Assets[effectPart.I_00];
                                break;
                            case AssetType.TBIND:
                                effectPart.AssetRef = Tbind.Assets[effectPart.I_00];
                                break;
                            case AssetType.CBIND:
                                effectPart.AssetRef = Cbind.Assets[effectPart.I_00];
                                break;
                            case AssetType.LIGHT:
                                effectPart.AssetRef = LightEma.Assets[effectPart.I_00];
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

                    switch (effectPart.I_02)
                    {
                        case AssetType.EMO:
                            effectPart.I_00 = (ushort)Emo.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.PBIND:
                            effectPart.I_00 = (ushort)Pbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.TBIND:
                            effectPart.I_00 = (ushort)Tbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.CBIND:
                            effectPart.I_00 = (ushort)Cbind.Assets.IndexOf(effectPart.AssetRef);
                            break;
                        case AssetType.LIGHT:
                            effectPart.I_00 = (ushort)LightEma.Assets.IndexOf(effectPart.AssetRef);
                            break;
                    }

                    if (effectPart.I_00 == ushort.MaxValue)
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
            effectContainer.Version = (VersionEnum)eepkFile.I_08;
            effectContainer.Effects = new ObservableCollection<Effect>(eepkFile.Effects);

            foreach(var container in eepkFile.Assets)
            {
                AssetContainerTool assetContainer = new AssetContainerTool();

                assetContainer.LooseFiles = (container.FILES[0] == "NULL") ? true : false;
                assetContainer.I_00 = HexConverter.ToInt32(container.I_00);
                assetContainer.I_04 = HexConverter.ToInt8(container.I_04);
                assetContainer.I_05 = HexConverter.ToInt8(container.I_05);
                assetContainer.I_06 = HexConverter.ToInt8(container.I_06);
                assetContainer.I_07 = HexConverter.ToInt8(container.I_07);
                assetContainer.I_08 = HexConverter.ToInt32(container.I_08);
                assetContainer.I_12 = HexConverter.ToInt32(container.I_12);
                assetContainer.ContainerAssetType = container.I_16;
                
                assetContainer.File1_Name = container.FILES[0];
                assetContainer.File2_Name = container.FILES[1];
                assetContainer.File3_Name = container.FILES[2];
                assetContainer.Assets = new ObservableCollection<Asset>();

                foreach(var assets in container.AssetEntries)
                {
                    ObservableCollection<EffectFile> files = new ObservableCollection<EffectFile>();

                    foreach(Asset_File filename in assets.FILES)
                    {
                        //IF the file is NULL, then we wont add it
                        //When recreating the EEPK file we will add the nessecary NULL entries once again (up to 5)
                        if(filename.Path != "NULL")
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
            eepkFile.I_08 = (int)effectContainer.Version;
            eepkFile.Assets = new List<AssetContainer>();
            eepkFile.Effects = effectContainer.Effects.ToList();

            if(effectContainer.Emo.Assets.Count > 0)
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
            newAssetContainer.I_00 = HexConverter.GetHexString(assetContainer.I_00);
            newAssetContainer.I_04 = HexConverter.GetHexString(assetContainer.I_04);
            newAssetContainer.I_05 = HexConverter.GetHexString(assetContainer.I_05);
            newAssetContainer.I_06 = HexConverter.GetHexString(assetContainer.I_06);
            newAssetContainer.I_07 = HexConverter.GetHexString(assetContainer.I_07);
            newAssetContainer.I_08 = HexConverter.GetHexString(assetContainer.I_08);
            newAssetContainer.I_12 = HexConverter.GetHexString(assetContainer.I_12);
            newAssetContainer.I_16 = type;

            if (assetContainer.LooseFiles)
            {
                assetContainer.File1_Name = "NULL";
            }

            newAssetContainer.FILES = new string[3] { assetContainer.File1_Name, assetContainer.File2_Name , assetContainer.File3_Name };

            if(assetContainer.Assets != null)
            {
                newAssetContainer.AssetEntries = new List<Asset_Entry>();

                foreach(var asset in assetContainer.Assets)
                {
                    List<Asset_File> files = new List<Asset_File>();

                    for(int i = 0; i < asset.Files.Count; i++)
                    {
                        if(asset.Files[i].FullFileName != "NULL")
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
                        Assets = new ObservableCollection<Asset>(),
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
                        Assets = new ObservableCollection<Asset>(),
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
                        Assets = new ObservableCollection<Asset>(),
                        ContainerAssetType = AssetType.CBIND
                    };
                case AssetType.EMO:
                    return new AssetContainerTool()
                    {
                        LooseFiles = true,
                        File1_Name = "NULL",
                        File2_Name = "NULL",
                        File3_Name = "NULL",
                        Assets = new ObservableCollection<Asset>(),
                        ContainerAssetType = AssetType.EMO
                    };
                case AssetType.LIGHT:
                    return new AssetContainerTool()
                    {
                        LooseFiles = true,
                        File1_Name = "NULL",
                        File2_Name = "NULL",
                        File3_Name = "NULL",
                        Assets = new ObservableCollection<Asset>(),
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

            if(container.File3_Ref.Entry.Count > EMB_File.MAX_EFFECT_TEXTURES)
            {
                RemoveUnusedTextures(type);

                if(container.File3_Ref.Entry.Count > EMB_File.MAX_EFFECT_TEXTURES)
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
        private void RemoveNullEffectParts()
        {
            //Removes all EffectParts that have a null AssetRef (e.g. someone added a new EffectPart and never assigned an asset to it)
            foreach(var effect in Effects)
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
            foreach(var effect in Effects)
            {
                if (effect.IndexNum == id) return true;
            }
            return false;
        }

        public Effect GetEffectAssociatedWithEffectPart(EffectPart effectPart)
        {
            if (effectPart == null) return null;

            foreach(var effect in Effects)
            {
                foreach(var _effectPart in effect.EffectParts)
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
            
            if(firstEffect != null)
            {
                foreach(var effectPart in firstEffect.EffectParts)
                {
                    if (effectPart == effectParts[0]) return firstEffect;
                }
            }

            return GetEffectAssociatedWithEffectPart(effectParts[0]);
        }
        
        /// <summary>
        /// Returns the SelectedEffectParts for the first SelectedEffect, if it exists.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<EffectPart> GetSelectedEffectParts()
        {
            if(SelectedEffect != null)
            {
                return SelectedEffect.SelectedEffectParts;
            }

            return null;
        }
        
        private bool IsAssetIndexUsed(AssetType type, int index)
        {
            foreach(var effect in Effects)
            {
                foreach(var effectPart in effect.EffectParts)
                {
                    if (effectPart.I_02 == type && effectPart.I_00 == index) return true;
                }
            }

            return false;
        }
        #endregion
        
        #region SDBHSupport
        private string CalculateSDBHPBINDContainerName_EMM(string emm)
        {
            if(emm != "NULL" && File.Exists(string.Format("{0}/{1}", Directory, emm)))
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

            foreach(var emp in Pbind.Assets)
            {
                Pbind.AddPbindDependencies(emp.Files[0].EmpFile);
            }
        }

        private void SDBH_PbindLinkTextureAndMaterial()
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

                    string emmPath = String.Format("{0}/{1}.emm", Directory, Path.GetFileName(empAsset.Files[0].FileName));
                    string embPath = String.Format("{0}/{1}.emb", Directory, Path.GetFileName(empAsset.Files[0].FileName));
                    EMM_File emmFile = (DoesFileExist(emmPath)) ? EMM_File.LoadEmm(LoadExternalFile(emmPath, false)) : Pbind.File2_Ref;
                    EMB_File embFile = (DoesFileExist(emmPath)) ? EMB_File.LoadEmb(LoadExternalFile(embPath, false)) : Pbind.File3_Ref;

                    //Materials
                    foreach (var empEntry in empFile.ParticleEffects)
                    {
                        if (empEntry.Type_Texture != null)
                        {
                            empEntry.Type_Texture.MaterialRef = (empEntry.Type_Texture.I_16 != ushort.MaxValue) ? emmFile.GetEntry(empEntry.Type_Texture.I_16) : null;
                        }

                        if (empEntry.ChildParticleEffects != null)
                        {
                            SDBH_PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleEffects, emmFile);
                        }

                    }

                    //Textures
                    foreach (var empTexture in empFile.Textures)
                    {
                        if (empTexture.I_01 >= byte.MinValue && empTexture.I_01 < sbyte.MaxValue + 1)
                        {
                            empTexture.TextureRef = embFile.GetEntry(empTexture.I_01);
                        }
                        else if (empTexture.I_01 != byte.MaxValue)
                        {
                            throw new IndexOutOfRangeException(String.Format("SDBH_PbindLinkTextureAndMaterial: EMB_Index is out of range ({0}).\n\nptcl.emb can only have a maximum of 128 textures.", empTexture.I_01));
                        }
                    }

                    index++; //This is for debugging/error messages, no other purpose
                }
            }
        }

        private void SDBH_PbindLinkTextureAndMaterial_Recursive(ObservableCollection<ParticleEffect> childrenParticleEffects, EMM_File emmFile)
        {
            foreach (var empEntry in childrenParticleEffects)
            {
                if (empEntry.Type_Texture != null)
                {
                    empEntry.Type_Texture.MaterialRef = (empEntry.Type_Texture.I_16 != ushort.MaxValue) ? emmFile.GetEntry(empEntry.Type_Texture.I_16) : null;
                }

                if (empEntry.ChildParticleEffects != null)
                {
                    SDBH_PbindLinkTextureAndMaterial_Recursive(empEntry.ChildParticleEffects, emmFile);
                }
            }
        }
        #endregion

        #region Operations
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

            int removed = 0;
            AssetContainerTool container = GetAssetContainer(type);

            for (int i = container.File3_Ref.Entry.Count - 1; i >= 0; i--)
            {
                if (!container.IsTextureUsed(container.File3_Ref.Entry[i]))
                {
                    undos.Add(new UndoableListRemove<EmbEntry>(container.File3_Ref.Entry, container.File3_Ref.Entry[i]));
                    container.File3_Ref.Entry.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
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

            int duplicateCount = 0;

            restart:
            foreach (var texture1 in container.File3_Ref.Entry)
            {
                List<EmbEntry> Duplicates = new List<EmbEntry>();

                foreach (var texture2 in container.File3_Ref.Entry)
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
                        container.RefactorTextureRef(duplicate, texture1, undos);

                        //Delete the duplicate
                        undos.Add(new UndoableListRemove<EmbEntry>(container.File3_Ref.Entry, duplicate));
                        container.File3_Ref.Entry.Remove(duplicate);
                    }
                    goto restart;
                }

            }

            return duplicateCount;
        }

        /// <summary>
        /// Remove all materials that are unused by Particle Effects or ETR Effects.
        /// </summary>
        /// <param name="type">PBIND or TBIND</param>
        /// <returns>The amount of materials that were removed.</returns>
        public int RemoveUnusedMaterials(AssetType type, List<IUndoRedo> undos = null)
        {
            if (type != AssetType.PBIND && type != AssetType.TBIND) throw new InvalidOperationException(String.Format("RemoveUnusedMaterials: Method was called with type parameter = {0}, which is invalid (expecting either PBIND or TBIND).", type));
            if (undos == null) undos = new List<IUndoRedo>();

            int removed = 0;
            AssetContainerTool container = GetAssetContainer(type);

            for (int i = container.File2_Ref.Materials.Count - 1; i >= 0; i--)
            {
                if (!container.IsMaterialUsed(container.File2_Ref.Materials[i]))
                {
                    undos.Add(new UndoableListRemove<Material>(container.File2_Ref.Materials, container.File2_Ref.Materials[i]));
                    container.File2_Ref.Materials.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Merges all identical materials into a single instance.
        /// </summary>
        /// <param name="type">PBIND or TBIND.</param>
        /// <returns>The amount of merged materials.</returns>
        public int MergeDuplicateMaterials(AssetType type, List<IUndoRedo> undos = null)
        {
            if (type != AssetType.PBIND && type != AssetType.TBIND) throw new InvalidOperationException(String.Format("MergeDuplicateMaterials: Method was called with type parameter = {0}, which is invalid (expecting either PBIND or TBIND).", type));
            if (undos == null) undos = new List<IUndoRedo>();

            AssetContainerTool container = GetAssetContainer(type);

            int duplicateCount = 0;

            restart:
            foreach (var material1 in container.File2_Ref.Materials)
            {
                List<Material> Duplicates = new List<Material>();

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
                        undos.Add(new UndoableListRemove<Material>(container.File2_Ref.Materials, duplicate));
                        container.File2_Ref.Materials.Remove(duplicate);
                    }
                    goto restart;
                }

            }


            return duplicateCount;
        }

        public void RemoveAsset(Asset asset, AssetType type, List<IUndoRedo> undos = null)
        {
            AssetContainerTool container = GetAssetContainer(type);

            if (undos != null && container.Assets.Contains(asset))
                undos.Add(new UndoableListRemove<Asset>(container.Assets, asset));

            container.Assets.Remove(asset);
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

            if(toRemove != null)
            {
                Effects.Remove(toRemove);

                foreach(var asset in toRemove.EffectParts)
                {
                    //If asset isn't used by other effects, then remove it
                    if (!IsAssetUsed(asset.AssetRef) && asset.AssetRef != null)
                    {
                        RemoveAsset(asset.AssetRef, asset.I_02);
                    }
                }
            }
        }

        public void InstallEffects(ObservableCollection<Effect> effects)
        {
            //Remove effects (clears out potential duplicate and unneeded data)
            foreach(var effect in effects)
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
            foreach(var id in ids)
            {
                Effect originalEffect = originalEepk.Effects.FirstOrDefault(e => e.Index == id);
                Effect effect = Effects.FirstOrDefault(e => e.Index == id);

                if(effect != null)
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

                if(original != null)
                {
                    AddEffect(original);
                }
            }
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

        internal AssetType ContainerAssetType { get; set; }

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
                if(LooseFiles != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<AssetContainerTool>(nameof(LooseFiles), this, LooseFiles, value, "Loose Files"));
                    LooseFiles = value;
                }

            }
        }

        //EEPK parameters
        //Will crash if set wrong
        private int _I_00_value = 128;
        private byte _I_04_value = 255;
        private byte _I_05_value = 255;
        private byte _I_06_value = 255;
        private byte _I_07_value = 255;
        private int _I_08_value = -1;
        private int _I_12_value = -1;

        public int I_00
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
                    NotifyPropertyChanged("I_00");
                }
            }
        }
        public byte I_04
        {
            get
            {
                return this._I_04_value;
            }
            set
            {
                if (value != this._I_04_value)
                {
                    this._I_04_value = value;
                    NotifyPropertyChanged("I_04");
                }
            }
        }
        public byte I_05
        {
            get
            {
                return this._I_05_value;
            }
            set
            {
                if (value != this._I_05_value)
                {
                    this._I_05_value = value;
                    NotifyPropertyChanged("I_05");
                }
            }
        }
        public byte I_06
        {
            get
            {
                return this._I_06_value;
            }
            set
            {
                if (value != this._I_06_value)
                {
                    this._I_06_value = value;
                    NotifyPropertyChanged("I_06");
                }
            }
        }
        public byte I_07
        {
            get
            {
                return this._I_07_value;
            }
            set
            {
                if (value != this._I_07_value)
                {
                    this._I_07_value = value;
                    NotifyPropertyChanged("I_07");
                }
            }
        }
        public int I_08
        {
            get
            {
                return this._I_08_value;
            }
            set
            {
                if (value != this._I_08_value)
                {
                    this._I_08_value = value;
                    NotifyPropertyChanged("I_08");
                }
            }
        }
        public int I_12
        {
            get
            {
                return this._I_12_value;
            }
            set
            {
                if (value != this._I_12_value)
                {
                    this._I_12_value = value;
                    NotifyPropertyChanged("I_12");
                }
            }
        }
        
        public string File1_Name { get; set; } //Main container emb (pbind, tbind, cbind, light)
        public string File2_Name { get; set; } //Material .emm
        public string File3_Name { get; set; } //Texture emb (trc, ptcl)

        //References to the emb/emm files (for pbind, tbind and cbind only)
        public EMB_File File1_Ref = null;
        public EMM_File File2_Ref = null;
        public EMB_File File3_Ref = null;

        private ObservableCollection<Asset> _assetsValue = null;
        public ObservableCollection<Asset> Assets
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
                    NotifyPropertyChanged("Assets");
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
                _viewAssets = new ListCollectionView(Assets);
                _viewAssets.GroupDescriptions.Add(new PropertyGroupDescription("SubType"));
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
                foreach(var file in _asset.Files)
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
            if(_viewAssets == null)
                _viewAssets = new ListCollectionView(Assets);
            
            _viewAssets.Filter = new Predicate<object>(AssetFilterCheck);
            NotifyPropertyChanged("ViewAssets");
        }

        
        //Count Method
        public void RefreshAssetCount()
        {
            NotifyPropertyChanged("AssetCount");
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
            foreach(var asset in Assets)
            {
                if(asset.Files.Count > 0)
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

            foreach(var asset in Assets)
            {
                foreach(var file in asset.Files)
                {
                    if (file.FullFileName == name) return true;
                }
            }

            return false;
        }
        
        public Asset GetAssetByFileInstance(EffectFile file)
        {
            foreach(var asset in Assets)
            {
                foreach(var _file in asset.Files)
                {
                    if(_file == file)
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

            foreach(var _asset in Assets)
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
                    foreach (var texture in asset.Files[0].EtrFile.ETR_TextureEntries)
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

        public List<string> MaterialUsedBy(Material material)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("MaterialUsedBy: AssetType is not PBIND or TBIND, cannot continue.");

            List<string> materialUsedBy = new List<string>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var particle in asset.Files[0].EmpFile.ParticleEffects)
                    {
                        if (particle.Type_Texture != null)
                        {
                            if (particle.Type_Texture.MaterialRef == material)
                            {
                                if (!materialUsedBy.Contains(asset.FileNamesPreviewWithExtension))
                                {
                                    materialUsedBy.Add(asset.FileNamesPreviewWithExtension);
                                }
                            }
                        }

                        if (particle.ChildParticleEffects != null)
                        {
                            MaterialUsedBy_Recursive(particle.ChildParticleEffects, material, materialUsedBy, asset.FileNamesPreviewWithExtension);
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.ETR_Entries)
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

        private void MaterialUsedBy_Recursive(ObservableCollection<ParticleEffect> childParticleEffects, Material material, List<string> textureUsedBy, string fileName)
        {
            foreach (var particle in childParticleEffects)
            {
                if (particle.Type_Texture != null)
                {
                    if (particle.Type_Texture.MaterialRef == material)
                    {
                        if (!textureUsedBy.Contains(fileName))
                        {
                            textureUsedBy.Add(fileName);
                        }
                    }
                }

                if (particle.ChildParticleEffects != null)
                {
                    MaterialUsedBy_Recursive(particle.ChildParticleEffects, material, textureUsedBy, fileName);
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
                    foreach (var texture in asset.Files[0].EtrFile.ETR_TextureEntries)
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

        public bool IsMaterialUsed(Material material)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("IsMaterialUsed: AssetType is not PBIND or TBIND, cannot continue.");

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var mat in asset.Files[0].EmpFile.ParticleEffects)
                    {
                        if (mat.Type_Texture != null)
                        {
                            if (mat.Type_Texture.MaterialRef == material)
                            {
                                return true;
                            }
                        }
                        if (mat.ChildParticleEffects != null)
                        {
                            if (IsMaterialUsed_Recursive(mat.ChildParticleEffects, material))
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var mat in asset.Files[0].EtrFile.ETR_Entries)
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

        private bool IsMaterialUsed_Recursive(ObservableCollection<ParticleEffect> childParticleEffects, Material material)
        {
            foreach (var mat in childParticleEffects)
            {
                if (mat.Type_Texture != null)
                {
                    if (mat.Type_Texture.MaterialRef == material)
                    {
                        return true;
                    }
                }

                if (mat.ChildParticleEffects != null)
                {
                    if (IsMaterialUsed_Recursive(mat.ChildParticleEffects, material))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Material GetMaterialAssociatedWithParameters(List<Parameter> parameters, Material firstMaterial)
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

                    if(file.FullFileName != originalName)
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
            if(Path.GetExtension(name) != ".emp")
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
            var asset = Asset.Create(ecfFile, name, EffectFile.FileType.ECF, AssetType.CBIND);
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

            foreach (var texture in empFile.Textures)
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
            AddPbindDependencies_Recursive(empFile.ParticleEffects, undos);

        }

        private void AddPbindDependencies_Recursive(ObservableCollection<ParticleEffect> childrenParticleEffects, List<IUndoRedo> undos)
        {
            foreach (var particleEffect in childrenParticleEffects)
            {
                if (particleEffect.Type_Texture != null)
                {
                    if (particleEffect.Type_Texture.MaterialRef != null)
                    {
                        var ret = File2_Ref.Compare(particleEffect.Type_Texture.MaterialRef);

                        if (ret == null)
                        {
                            //No identical material was found, so add it
                            particleEffect.Type_Texture.MaterialRef.Str_00 = File2_Ref.GetUnusedName(particleEffect.Type_Texture.MaterialRef.Str_00); //Regenerate the name
                            File2_Ref.Materials.Add(particleEffect.Type_Texture.MaterialRef);
                            undos?.Add(new UndoableListAdd<Material>(File2_Ref.Materials, particleEffect.Type_Texture.MaterialRef));
                        }
                        else
                        {
                            //An identical material was found, so use that instead
                            particleEffect.Type_Texture.MaterialRef = ret;
                        }
                    }
                }

                if (particleEffect.ChildParticleEffects != null)
                {
                    AddPbindDependencies_Recursive(particleEffect.ChildParticleEffects, undos);
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
            foreach (var texture in etrFile.ETR_TextureEntries)
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
            foreach (var entry in etrFile.ETR_Entries)
            {
                var ret = File2_Ref.Compare(entry.MaterialRef);

                if (ret == null)
                {
                    //No identical material was found, so add it
                    entry.MaterialRef.Str_00 = File2_Ref.GetUnusedName(entry.MaterialRef.Str_00); //Regenerate the name
                    File2_Ref.Materials.Add(entry.MaterialRef);
                    undos?.Add(new UndoableListAdd<Material>(File2_Ref.Materials, entry.MaterialRef));
                }
                else
                {
                    //An identical material was found, so use that instead
                    entry.MaterialRef = ret;
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
                undos.Add(new UndoableListRemove<EmbEntry>(File3_Ref.Entry, embEntry));

            File3_Ref.Entry.Remove(embEntry);
        }

        public void DeleteMaterial(Material material, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("DeleteMaterial: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos != null && File2_Ref.Materials.Contains(material))
                undos.Add(new UndoableListRemove<Material>(File2_Ref.Materials, material));

            File2_Ref.Materials.Remove(material);
        }
        
        //Refactoring
        public void RefactorTextureRef(EMB_CLASS.EmbEntry oldRef, EMB_CLASS.EmbEntry newRef, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("RefactorTextureRef: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var asset in Assets)
            {
                if(ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var texture in asset.Files[0].EmpFile.Textures)
                    {
                        if (texture.TextureRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<EMP_TextureDefinition>(nameof(EMP_TextureDefinition.TextureRef), texture, oldRef, newRef));
                            texture.TextureRef = newRef;
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var texture in asset.Files[0].EtrFile.ETR_TextureEntries)
                    {
                        if (texture.TextureRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<ETR_TextureEntry>(nameof(ETR_TextureEntry.TextureRef), texture, oldRef, newRef));
                            texture.TextureRef = newRef;
                        }
                    }
                }
            }
        }
        
        public void RefactorMaterialRef(Material oldRef, Material newRef, List<IUndoRedo> undos = null)
        {
            if (ContainerAssetType != AssetType.PBIND && ContainerAssetType != AssetType.TBIND)
                throw new InvalidOperationException("RefactorMaterialRef: AssetType is not PBIND or TBIND, cannot continue.");

            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var asset in Assets)
            {
                if (ContainerAssetType == AssetType.PBIND)
                {
                    foreach (var particleEffect in asset.Files[0].EmpFile.ParticleEffects)
                    {
                        if(particleEffect.Type_Texture != null)
                        {
                            if (particleEffect.Type_Texture.MaterialRef == oldRef)
                            {
                                undos.Add(new UndoableProperty<TexturePart>(nameof(particleEffect.Type_Texture.MaterialRef), particleEffect.Type_Texture, oldRef, newRef));
                                particleEffect.Type_Texture.MaterialRef = newRef;
                            }
                        }

                        if(particleEffect.ChildParticleEffects != null)
                        {
                            RefactorMaterialRef_Recursive(particleEffect.ChildParticleEffects, oldRef, newRef, undos);
                        }
                    }
                }
                else if (ContainerAssetType == AssetType.TBIND)
                {
                    foreach (var etrEntry in asset.Files[0].EtrFile.ETR_Entries)
                    {
                        if (etrEntry.MaterialRef == oldRef)
                        {
                            undos.Add(new UndoableProperty<ETR_MainEntry>(nameof(ETR_MainEntry.MaterialRef), etrEntry, oldRef, newRef));
                            etrEntry.MaterialRef = newRef;
                        }
                    }
                }
            }
        }

        private void RefactorMaterialRef_Recursive(ObservableCollection<ParticleEffect> childParticleEffects, Material oldRef, Material newRef, List<IUndoRedo> undos)
        {
            foreach (var particleEffect in childParticleEffects)
            {
                if (particleEffect.Type_Texture != null)
                {
                    if (particleEffect.Type_Texture.MaterialRef == oldRef)
                    {
                        undos.Add(new UndoableProperty<TexturePart>(nameof(particleEffect.Type_Texture.MaterialRef), particleEffect.Type_Texture, oldRef, newRef));
                        particleEffect.Type_Texture.MaterialRef = newRef;
                    }
                }

                if (particleEffect.ChildParticleEffects != null)
                {
                    RefactorMaterialRef_Recursive(particleEffect.ChildParticleEffects, oldRef, newRef, undos);
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
                for(int a = 0; a <Assets[i].Files.Count; a++)
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
                return Files[0].FileName;
            }
        }
        public string FileNamesPreviewWithExtension
        {
            get
            {
                return Files[0].FullFileName;
            }
        }
        public string FileNamePreviewWithAssetType
        {
            get
            {
                return String.Format("[{1}] {0}", Files[0].FileName, assetType);
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
                    NotifyPropertyChanged("I_00");
                }
            }
        }

        private ObservableCollection<EffectFile> _filesValue = null;
        public ObservableCollection<EffectFile> Files
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
                    NotifyPropertyChanged("Files");
                    NotifyPropertyChanged("FileNamesPreview");
                }
            }
        }


        public bool Compare(Asset asset, AssetType type)
        {
            if(type == AssetType.TBIND || type == AssetType.PBIND || type == AssetType.CBIND)
            {
                //No comparison for these types, yet...
                return false;
            }
            else
            {
                for(int i = 0; i < Files.Count; i++)
                {
                    if(Files[i].FullFileName != "NULL")
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

        public void AddFile(object data, string name, EffectFile.FileType type)
        {
            if(Files.Count == 5)
            {
                throw new InvalidOperationException("Cannot add file because the maximum allowed amount of 5 is already reached.");
            }


            switch (type)
            {
                case EffectFile.FileType.EMP:
                    Files.Add(new EffectFile()
                    {
                        EmpFile = data as EMP_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                case EffectFile.FileType.ETR:
                    Files.Add(new EffectFile()
                    {
                        EtrFile = data as ETR_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                case EffectFile.FileType.ECF:
                    Files.Add(new EffectFile()
                    {
                        EcfFile = data as ECF_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                case EffectFile.FileType.EMM:
                    Files.Add(new EffectFile()
                    {
                        EmmFile = data as EMM_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                case EffectFile.FileType.EMB:
                    Files.Add(new EffectFile()
                    {
                        EmbFile = data as EMB_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                case EffectFile.FileType.EMA:
                    Files.Add(new EffectFile()
                    {
                        EmaFile = data as EMA_File,
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
                default:
                    if(data as byte[] == null)
                    {
                        throw new InvalidDataException(String.Format("EffectFile.AddFile: tried add undefined file type ({0}), but bytes was null.", type));
                    }
                    Files.Add(new EffectFile()
                    {
                        Bytes = data as byte[],
                        FileName = name,
                        fileType = type,
                        OriginalFileName = name
                    });
                    break;
            }
            
            NotifyPropertyChanged("FileNamesPreview");
        }

        public void RemoveFile(EffectFile file)
        {
            if(Files.Count == 1)
            {
                throw new InvalidOperationException("Cannot remove the last file.");
            }

            Files.Remove(file);
            NotifyPropertyChanged("FileNamesPreview");
        }

        public void RefreshNamePreview()
        {
            NotifyPropertyChanged("FileNamesPreview");
            NotifyPropertyChanged("FileNamesPreviewWithExtension");
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
            newAsset.Files = new ObservableCollection<EffectFile>();

            foreach(var file in Files)
            {
                EffectFile newFile = new EffectFile();
                newFile.OriginalFileName = file.OriginalFileName;
                newFile.FileName = file.FileName;
                newFile.Extension = file.Extension;
                newFile.fileType = file.fileType;

                if(newFile.fileType == EffectFile.FileType.Other)
                {
                    newFile.Bytes = file.Bytes.Copy();
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
                else if (newFile.fileType == EffectFile.FileType.EMA)
                {
                    newFile.EmaFile = file.EmaFile.Copy();
                }

                newAsset.Files.Add(newFile);

            }
            return newAsset;
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
            asset.Files = new ObservableCollection<EffectFile>();
            asset.assetType = assetType;
            asset.AddFile(data, name, fileType);
            return asset;
        }

        public bool HasSameFileNames(Asset asset)
        {
            if(Files.Count == asset.Files.Count)
            {
                for(int i = 0; i < Files.Count; i++)
                {
                    if(Files[i].FullFileName != asset.Files[i].FullFileName)
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

                    foreach(var file in Files)
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
        public EMP_File EmpFile = null;
        public ETR_File EtrFile = null;
        public ECF_File EcfFile = null;
        public EMB_File EmbFile = null;
        public EMM_File EmmFile = null;
        public EMA_File EmaFile = null;
        public byte[] Bytes = null;

        public void SetName(string name)
        {
            Extension = GetExtension(name);
            FileName = GetFileNameWithoutExtension(name);

            if(OriginalFileName == null)
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
                    return FileType.Other;
                case ".emb":
                    return FileType.EMB;
                case ".emm":
                    return FileType.EMM;
                case ".mat.ema":
                    return FileType.Other;
                case ".obj.ema":
                    return FileType.Other;
                case ".emp":
                    return FileType.EMP;
                case ".etr":
                    return FileType.ETR;
                case ".ecf":
                    return FileType.ECF;
                case ".light.ema":
                case ".ema"://Some .light.ema are named incorrectly, like "_light.ema", so we must also load .ema files
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
            if (oldFile.ParticleEffects.Count != newFile.ParticleEffects.Count)
                throw new InvalidDataException("CopyEmpRef: oldFile and newFile ParticleEffect count is out of sync.");
            if (oldFile.Textures.Count != newFile.Textures.Count)
                throw new InvalidDataException("CopyEmpRef: oldFile and newFile Texture count is out of sync.");

            //Copy particle effect material references
            for (int i = 0; i < oldFile.ParticleEffects.Count; i++)
            {
                if(oldFile.ParticleEffects[i].Type_Texture != null)
                {
                    newFile.ParticleEffects[i].Type_Texture.MaterialRef = oldFile.ParticleEffects[i].Type_Texture.MaterialRef;
                }

                if(oldFile.ParticleEffects[i].ChildParticleEffects != null)
                {
                    CopyEmpRef_Recursive(oldFile.ParticleEffects[i].ChildParticleEffects, newFile.ParticleEffects[i].ChildParticleEffects);
                }
            }

            //Copy texture EmbEntry references
            for(int i = 0; i <oldFile.Textures.Count; i++)
            {
                newFile.Textures[i].TextureRef = oldFile.Textures[i].TextureRef;
            }
        }

        private static void CopyEmpRef_Recursive(ObservableCollection<ParticleEffect> oldFile, ObservableCollection<ParticleEffect> newFile)
        {
            if (oldFile.Count != newFile.Count)
                throw new InvalidDataException("CopyEmpRef_Recursive: oldFile and newFile ParticleEffect count is out of sync.");

            for (int i = 0; i < oldFile.Count; i++)
            {
                if (oldFile[i].Type_Texture != null)
                {
                    newFile[i].Type_Texture.MaterialRef = oldFile[i].Type_Texture.MaterialRef;
                }

                if (oldFile[i].ChildParticleEffects != null)
                {
                    CopyEmpRef_Recursive(oldFile[i].ChildParticleEffects, newFile[i].ChildParticleEffects);
                }
            }
        }

        /// <summary>
        /// Copy all material and texture references from oldFile to newFile. The files must have an equal amount of entries for it to be successful.
        /// </summary>
        public static void CopyEtrRef(ETR_File oldFile, ETR_File newFile)
        {
            if (oldFile.ETR_Entries.Count != newFile.ETR_Entries.Count)
                throw new InvalidDataException("CopyEtrRef: oldFile and newFile Entry count is out of sync.");
            if (oldFile.ETR_TextureEntries.Count != newFile.ETR_TextureEntries.Count)
                throw new InvalidDataException("CopyEtrRef: oldFile and newFile Texture count is out of sync.");

            //Copy particle effect material references
            for (int i = 0; i < oldFile.ETR_Entries.Count; i++)
            {
                newFile.ETR_Entries[i].MaterialRef = oldFile.ETR_Entries[i].MaterialRef;
            }

            //Copy texture EmbEntry references
            for (int i = 0; i < oldFile.ETR_TextureEntries.Count; i++)
            {
                newFile.ETR_TextureEntries[i].TextureRef = oldFile.ETR_TextureEntries[i].TextureRef;
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
                    if (EmaFile == null) return false;
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
                    return EmaFile.Write();
                case FileType.EMB:
                    return EmbFile.SaveToBytes();
                case FileType.EMM:
                    return EmmFile.SaveToBytes();
                case FileType.EMP:
                    return EmpFile.SaveToBytes(ParserMode.Tool);
                case FileType.ETR:
                    return EtrFile.Save(false);
                case FileType.Other:
                    return Bytes;
                default:
                    throw new Exception(string.Format("EffectFile.GetBytes(): Unknown fileType = {0}", fileType));
            }
        }
    }




}
