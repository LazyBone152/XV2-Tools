using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xv2CoreLib.ACB;
using Xv2CoreLib.AMK;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BAI;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BCS;
using Xv2CoreLib.BDM;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CSO;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.ERS;
using Xv2CoreLib.ESK;
using Xv2CoreLib.PSC;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib
{
    public class Xv2Character
    {
        public bool IsCaC { get { return CmsEntry?.ID >= 100 && CmsEntry?.ID <= 108; } }

        public string[] Name = new string[(int)Xenoverse2.Language.NumLanguages];

        public AsyncObservableCollection<Xv2CharaCostume> Costumes { get; set; } = new AsyncObservableCollection<Xv2CharaCostume>();
        public CMS_Entry CmsEntry { get; set; }
        public List<CSO_Entry> CsoEntry { get; set; }
        public ERS_MainTableEntry ErsEntry { get; set; }
        public Xv2File<BAI_File> BaiFile { get; set; }
        public List<Xv2File<AMK_File>> AmkFile { get; set; }

        //BCS:
        public Xv2File<BCS_File> BcsFile { get; set; }
        public AsyncObservableCollection<Xv2PartSetFile> PartSetFiles { get; set; } = new AsyncObservableCollection<Xv2PartSetFile>();

        //Moveset:
        public Xv2MoveFiles MovesetFiles { get; set; }
        public Xv2File<ESK_File> EskFile { get; set; }


        public void SaveFiles()
        {
            if (MovesetFiles.BacFile?.File?.IsNull() == false)
                MovesetFiles.BacFile.File.Save(MovesetFiles.BacFile.Path);

            if (MovesetFiles.BcmFile?.File?.IsNull() == false)
                MovesetFiles.BcmFile.File.Save(MovesetFiles.BcmFile.Path);

            if (MovesetFiles.BdmFile?.File?.IsNull() == false)
                MovesetFiles.BdmFile.File.Save(MovesetFiles.BdmFile.Path);

            if (BaiFile?.File?.IsNull() == false)
                BaiFile.File.Save(BaiFile.Path);

            foreach (var ean in MovesetFiles.EanFile)
            {
                ean.File.Save(ean.Path);
            }

            if (MovesetFiles.CamEanFile[0]?.File?.IsNull() == false)
                MovesetFiles.CamEanFile[0].File.Save(MovesetFiles.CamEanFile[0].Path);

            if (MovesetFiles.EepkFile?.File?.IsNull() == false)
            {
                MovesetFiles.EepkFile.File.ChangeFilePath(MovesetFiles.EepkFile.Path);
                MovesetFiles.EepkFile.File.Save();
            }

            foreach (var vox in MovesetFiles.VoxAcbFile)
            {
                if (vox.File?.IsNull() == false)
                    vox.File.AcbFile.Save(vox.Path);
            }

            foreach (var se in MovesetFiles.SeAcbFile)
            {
                if (se.File?.IsNull() == false)
                    se.File.AcbFile.Save(se.Path);
            }

            if(EskFile?.File != null)
            {
                EskFile.File.Save(EskFile.Path);
            }
        }

        public void CalculateFilePaths()
        {
            string charaDir = $"chara/{CmsEntry.ShortName}";

            if (MovesetFiles.BacFile?.Borrowed == false)
            {
                MovesetFiles.BacFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bac", charaDir, CmsEntry.ShortName));
                CmsEntry.BacPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BcmFile?.Borrowed == false)
            {
                MovesetFiles.BcmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bcm", charaDir, CmsEntry.ShortName));
                CmsEntry.BcmPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BdmFile?.Borrowed == false)
            {
                MovesetFiles.BdmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bdm", charaDir, CmsEntry.ShortName));
                CmsEntry.BdmPath = CmsEntry.ShortName;
            }

            if (!MovesetFiles.CamEanFile[0]?.Borrowed == false)
            {
                MovesetFiles.CamEanFile[0].Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.cam.ean", charaDir, CmsEntry.ShortName));
                CmsEntry.CamEanPath = CmsEntry.ShortName;
            }

            if (BaiFile?.Borrowed == false)
            {
                BaiFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.bai", charaDir, CmsEntry.ShortName));
                CmsEntry.BaiPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.EepkFile?.Borrowed == false)
            {
                MovesetFiles.EepkFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("vfx/chara/{0}/{0}.eepk", CmsEntry.ShortName));
            }

            for (int i = 0; i < AmkFile.Count; i++)
            {
                //Number the AMK file if there are more than one

                if (!AmkFile[i].Borrowed)
                {
                    AmkFile[i].Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}{2}.amk", charaDir, CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
            }

            for (int i = 0; i < MovesetFiles.SeAcbFile.Count; i++)
            {
                //Number the ACB file if there are more than one

                if (!MovesetFiles.SeAcbFile[i].Borrowed)
                {
                    MovesetFiles.SeAcbFile[i].Path = FileManager.Instance.GetAbsolutePath(String.Format("sound/SE/Battle/Chara/CAR_BTL_{0}{1}_SE.acb", CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
            }

            //VOX Eng
            int uniqueEngVoxCount = 0;
            foreach (var vox in MovesetFiles.VoxAcbFile.Where(x => x.IsEnglish && !x.Borrowed))
            {
                vox.Path = FileManager.Instance.GetAbsolutePath(String.Format("sound/VOX/Battle/chara/en/CAR_BTL_{0}{1}_VOX.acb", CmsEntry.ShortName, (uniqueEngVoxCount > 0) ? uniqueEngVoxCount.ToString() : string.Empty));

                uniqueEngVoxCount++;
            }

            //VOX Jpn
            int uniqueJpnVoxCount = 0;
            foreach (var vox in MovesetFiles.VoxAcbFile.Where(x => !x.IsEnglish && !x.Borrowed))
            {
                vox.Path = FileManager.Instance.GetAbsolutePath(String.Format("sound/VOX/Battle/chara/CAR_BTL_{0}{1}_VOX.acb", CmsEntry.ShortName, (uniqueJpnVoxCount > 0) ? uniqueJpnVoxCount.ToString() : string.Empty));

                uniqueJpnVoxCount++;
            }

            //EANs
            var mainEan = MovesetFiles.EanFile.FirstOrDefault(x => x.IsDefault && x.FileType == Xenoverse2.MoveFileTypes.EAN);
            var fceEan = MovesetFiles.EanFile.FirstOrDefault(x => x.FileType == Xenoverse2.MoveFileTypes.FCE_EAN);

            if (mainEan?.Borrowed == false)
            {
                mainEan.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.ean", charaDir, CmsEntry.ShortName));
                CmsEntry.EanPath = CmsEntry.ShortName;
            }

            if (fceEan?.Borrowed == false)
            {
                fceEan.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.fce.ean", charaDir, CmsEntry.ShortName));
                CmsEntry.FceEanPath = CmsEntry.ShortName;
            }

            //ESK
            if (EskFile?.Borrowed == false)
            {
                EskFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_000.esk", charaDir, CmsEntry.ShortName));
            }
        }

        public void CreateDefaultFiles()
        {
            if (MovesetFiles == null) throw new NullReferenceException("Xv2Skill.Xv2Character: MovesetFiles was null.");

            if (MovesetFiles.BacFile == null)
                MovesetFiles.BacFile = new Xv2File<BAC_File>(BAC_File.DefaultBacFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BAC);

            if (MovesetFiles.BcmFile == null)
                MovesetFiles.BcmFile = new Xv2File<BCM_File>(BCM_File.DefaultBcmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BCM);

            if (MovesetFiles.BdmFile == null)
                MovesetFiles.BdmFile = new Xv2File<BDM_File>(BDM_File.DefaultBdmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BDM);

            if (MovesetFiles.EepkFile == null)
                MovesetFiles.EepkFile = new Xv2File<EffectContainerFile>(EffectContainerFile.New(), null, false, null, false, Xenoverse2.MoveFileTypes.EEPK);

            if (BaiFile == null)
                BaiFile = new Xv2File<BAI_File>(new BAI_File(), null, false, null, false, Xenoverse2.MoveFileTypes.BAI);

            if (MovesetFiles.EanFile.All(x => !x.IsDefault) && Xenoverse2.Instance.CmnEan != null)
                MovesetFiles.EanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultFile(Xenoverse2.Instance.CmnEan.Skeleton), null, false, null, false, Xenoverse2.MoveFileTypes.EAN));

            if (MovesetFiles.CamEanFile.All(x => !x.IsDefault))
                MovesetFiles.CamEanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultCamFile(), null, false, null, false, Xenoverse2.MoveFileTypes.CAM_EAN));

            if (MovesetFiles.VoxAcbFile.All(x => !x.IsDefault && x.IsEnglish))
                MovesetFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, true, Xenoverse2.MoveFileTypes.VOX_ACB));

            if (MovesetFiles.VoxAcbFile.All(x => !x.IsDefault && !x.IsEnglish))
                MovesetFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.VOX_ACB));

            if (MovesetFiles.SeAcbFile.All(x => !x.IsDefault))
                MovesetFiles.SeAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.SE_ACB));

            if (AmkFile.Any(x => !x.IsDefault))
                AmkFile.Add(new Xv2File<AMK_File>(new AMK_File(), null, false, null, false, Xenoverse2.MoveFileTypes.AMK));
        }

        //PartSet loading
        public void LoadPartSets()
        {
            foreach(var partSet in BcsFile.File.PartSets)
            {
                LoadPart(partSet.FaceBase, PartType.FaceBase);
                LoadPart(partSet.FaceEar, PartType.FaceEar);
                LoadPart(partSet.FaceEye, PartType.FaceEye);
                LoadPart(partSet.FaceForehead, PartType.FaceForehead);
                LoadPart(partSet.FaceNose, PartType.FaceNose);
                LoadPart(partSet.Hair, PartType.Hair);
                LoadPart(partSet.Bust, PartType.Bust);
                LoadPart(partSet.Pants, PartType.Pants);
                LoadPart(partSet.Boots, PartType.Boots);
                LoadPart(partSet.Rist, PartType.Rist);
            }

        }

        private void LoadPart(Part part, PartType type)
        {
            if (part == null) return;

            string emd = part.GetModelPath(type);
            string emb = part.GetEmbPath(type);
            string emm = part.GetEmmPath(type);
            string dyt = part.GetDytPath(type);
            string ean = part.GetEanPath();

            LoadPartSetFile(emd, part.CharaCode, type, null);
            LoadPartSetFile(emb, part.CharaCode, type, null);
            LoadPartSetFile(emm, part.CharaCode, type, null);
            LoadPartSetFile(dyt, part.CharaCode, type, null);

            if(!string.IsNullOrWhiteSpace(ean))
                LoadPartSetFile(ean, part.CharaCode, type, null);

            if (part.PhysicsParts != null)
            {
                foreach (var physicsPart in part.PhysicsParts)
                {
                    string physicsEmd = physicsPart.GetModelPath();
                    string physicsEsk = physicsPart.GetEskPath();
                    string physicsEmb = physicsPart.GetEmbPath();
                    string physicsEmm = physicsPart.GetEmmPath();
                    string physicsDyt = physicsPart.GetDytPath();
                    string physicsEan = physicsPart.GetEanPath();
                    string physicsScd = physicsPart.GetScdPath();

                    LoadPartSetFile(physicsEmd, physicsPart.CharaCode, type, null);
                    LoadPartSetFile(physicsEmb, physicsPart.CharaCode, type, emb);
                    LoadPartSetFile(physicsEmm, physicsPart.CharaCode, type, emm);
                    LoadPartSetFile(physicsDyt, physicsPart.CharaCode, type, dyt);
                    LoadPartSetFile(physicsEan, physicsPart.CharaCode, type, null);
                    LoadPartSetFile(physicsScd, physicsPart.CharaCode, type, null);
                    LoadPartSetFile(physicsEsk, physicsPart.CharaCode, type, null);
                }
            }
        }

        private void LoadPartSetFile(string path, string charaCode, PartType type, string altPath = null)
        {
            if (charaCode != CmsEntry.ShortName) return; //Dont load files belonging to another character

            string name = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(path)) return; //Nothing to load
            if (PartSetFiles.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                //File loaded already. We just need to add the part type to the existing instance.
                Xv2PartSetFile file = PartSetFiles.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                file.PartTypes = file.PartTypes.SetFlag(Part.GetPartTypeFlags(type));

                return;
            }

            path = Utils.SanitizePath(path);

            //Special case: EMB, EMM and DYT for physics parts can use the parents files if none are found
            if (!FileManager.Instance.fileIO.FileExists(path))
            {
                path = altPath;
                name = Path.GetFileName(altPath);
            }

            //Exit if no file is found
            if (!FileManager.Instance.fileIO.FileExists(path))
                return;

            Xv2PartSetFile charaFile = new Xv2PartSetFile(name, this);
            charaFile.PartTypes = charaFile.PartTypes.SetFlag(Part.GetPartTypeFlags(type));

            PartSetFiles.Add(charaFile);
        }

        public object GetPartSetFile(string name)
        {
            var file = PartSetFiles.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if(file != null)
            {
                if (!file.IsLoaded)
                    file.Load();

                return file.File;
            }

            return null;
        }

        //PartSet Editing
        public string GetUnusedCharaFileName(string path)
        {
            //IF the name is unused, then just return it
            if (PartSetFiles.FirstOrDefault(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase)) == null) return path;

            //Else, add a number suffix at the end
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int suffix = 1;

            while(PartSetFiles.Any(x => x.Name.Equals($"{name}_{suffix}{ext}", StringComparison.OrdinalIgnoreCase)))
            {
                suffix++;
            }

            return $"{name}_{suffix}{ext}";
        }

        public bool CheckCharaFilePathIsReserved(string path)
        {
            if (path.Equals($"{CmsEntry?.ShortName}_000.esk", StringComparison.OrdinalIgnoreCase)) return true;
            if (path.Equals($"{CmsEntry?.ShortName}.ean", StringComparison.OrdinalIgnoreCase)) return true;
            if (path.Equals($"{CmsEntry?.ShortName}.cam.ean", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        public IUndoRedo AddCharaFile(string absolutePath, bool forceLoad)
        {
            string relativePath = Path.GetFileName(absolutePath);

            Xv2PartSetFile file = new Xv2PartSetFile(relativePath, this, absolutePath);

            if (file.FileType == Xv2PartSetFile.Type.Unknown)
                throw new InvalidDataException(string.Format("AddCharaFile: File type of \"{0}\" is not supported.\n\nOnly EMD, EMB, DYT.EMB, EMM, EAN and SCD files can be added.", relativePath));

            if (forceLoad)
                file.Load();

            PartSetFiles.Add(file);

            return new UndoableListAdd<Xv2PartSetFile>(PartSetFiles, file, "Add Chara File");
        }

        //Costumes
        /// <summary>
        /// Adds a costume entry if it doesn't already exist.
        /// </summary>
        public List<IUndoRedo> AddCostume(int costume)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (Costumes.Any(x => x.CostumeId == costume))
                return undos;

            Xv2CharaCostume newCostume = new Xv2CharaCostume(costume);
            Xv2CharaCostume defaultCostume = Costumes.FirstOrDefault(x => x.CostumeId == 0);

            //Copy values over
            newCostume.CsoSkills = (defaultCostume.CsoSkills != null) ? defaultCostume.CsoSkills.Copy() : string.Empty;
            newCostume.PscEnabled = newCostume.PscEnabled;
            newCostume.PscEntry = (defaultCostume.PscEntry != null) ? defaultCostume.PscEntry.Copy() : null;

            Costumes.Add(newCostume);
            undos.Add(new UndoableListAdd<Xv2CharaCostume>(Costumes, newCostume));

            return undos;
        }
    }

    [Serializable]
    public class Xv2CharaCostume : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public int CostumeId { get; set; }

        //PSC
        public bool PscEnabled { get; set; }
        public PSC_SpecEntry PscEntry { get; set; } = new PSC_SpecEntry(); //needs to be implemented... currently not loaded

        //CSO
        public string CsoSkills { get; set; }

        public Xv2CharaCostume(int id)
        {
            CostumeId = id;
        }

        public static Xv2CharaCostume GetAndAddCostume(IList<Xv2CharaCostume> costumes, int id)
        {
            var existing = costumes.FirstOrDefault(x => x.CostumeId == id);

            if (existing == null)
            {
                existing = new Xv2CharaCostume(id);
                costumes.Add(existing);
            }

            return existing;
        }

        public bool HasCsoData(Xv2Character chara)
        {
            if (!string.IsNullOrWhiteSpace(CsoSkills))
                return true;

            if (Xv2File<ACB_Wrapper>.HasCostume(chara.MovesetFiles.SeAcbFile, CostumeId))
                return true;

            if (Xv2File<ACB_Wrapper>.HasCostume(chara.MovesetFiles.VoxAcbFile, CostumeId))
                return true;

            if (Xv2File<AMK_File>.HasCostume(chara.AmkFile, CostumeId))
                return true;

            return false;
        }

        public static string GetCsoSkillOrDefault(IList<Xv2CharaCostume> costumes, int costume)
        {
            if (costumes.Any(x => x.CostumeId == costume))
            {
                return costumes.FirstOrDefault(x => x.CostumeId == costume).CsoSkills;
            }

            if (costumes.Any(x => x.CostumeId == 0))
            {
                return costumes.FirstOrDefault(x => x.CostumeId == 0).CsoSkills;
            }

            return string.Empty;
        }

    }

    [Serializable]
    public class Xv2PartSetFile : INotifyPropertyChanged
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

        public enum Type
        {
            Unknown = 0,
            //Main types
            EMD,
            EMB,
            DYT_EMB,
            EMM,
            EAN,

            //For Physics Objects
            SCD,
            SCD_ESK
        }

        //Name and ownership
        /// <summary>
        /// The complete name of the file, including the extension.
        /// </summary>
        public string Name { get; set; }
        public string NameNoExt => Path.GetFileNameWithoutExtension(Name);
        private string RelativePath => $"chara/{Owner.CmsEntry.ShortName}/{Name}";
        [NonSerialized]
        public Xv2Character Owner;

        //Type:
        public Type FileType { get; private set; }
        public PartTypeFlags PartTypes { get; set; }

        //Data:
        public bool IsLoaded { get; private set; }
        public object File { get; private set; }
        //Used for unsupported file types like SCD that have no parser. Otherwise, its null
        public byte[] Bytes { get; private set; }

        //Constructors:
        /// <summary>
        /// Initalize a Xv2PartSetFile from a file that exists in the game. The file will not be loaded upon creation, just when requested.
        /// </summary>
        /// <param name="name">Name of the file, including extension. Relative to owner.</param>
        /// <param name="owner">The owner of this file.</param>
        public Xv2PartSetFile(string name, Xv2Character owner)
        {
            Name = name;
            Owner = owner;
            FileType = GetFileType(name);
        }

        /// <summary>
        /// Manually load a file directly.
        /// </summary>
        /// <param name="name">Name of the file, including extension. Relative to owner.</param>
        /// <param name="owner">The owner of this file.</param>
        /// <param name="filePathToLoad">Absolute path to file. This will be loaded immediately upon creation.</param>
        public Xv2PartSetFile(string name, Xv2Character owner, string filePathToLoad)
        {
            Name = name;
            Owner = owner;
            FileType = GetFileType(name);
            LoadManual(filePathToLoad);
        }

        public void Load(bool allowReload = true)
        {
            if (IsLoaded && !allowReload) return;

            switch (FileType)
            {
                case Type.EMD:
                case Type.EMB:
                case Type.DYT_EMB:
                case Type.EMM:
                case Type.EAN:
                case Type.SCD_ESK:
                    File = FileManager.Instance.GetParsedFileFromGame(RelativePath);
                    break;
                case Type.SCD:
                    Bytes = FileManager.Instance.GetBytesFromGame(RelativePath);
                    break;
                default:
                    return;
            }

            IsLoaded = true;
            NotifyPropertyChanged(nameof(IsLoaded));
        }

        private void LoadManual(string path)
        {
            switch (FileType)
            {
                case Type.EMD:
                    File = EMD_File.Load(path);
                    break;
                case Type.EMB:
                case Type.DYT_EMB:
                    File = EMB_File.LoadEmb(path);
                    break;
                case Type.EMM:
                    File = EMM_File.LoadEmm(path);
                    break;
                case Type.EAN:
                    File = EAN_File.Load(path);
                    break;
                case Type.SCD_ESK:
                    File = ESK_File.Load(path);
                    break;
                case Type.SCD:
                    Bytes = System.IO.File.ReadAllBytes(path);
                    break;
                default:
                    throw new ArgumentException($"Xv2PartSetFile.LoadManual: File type not supported here ({FileType}).");
            }

            IsLoaded = true;
            NotifyPropertyChanged(nameof(IsLoaded));
        }

        public void Save()
        {
            if (IsLoaded)
            {
                switch (FileType)
                {
                    case Type.EMD:
                    case Type.EMB:
                    case Type.DYT_EMB:
                    case Type.EMM:
                    case Type.EAN:
                    case Type.SCD_ESK:
                        FileManager.Instance.SaveFileToGame(RelativePath, File);
                        break;
                    case Type.SCD:
                        System.IO.File.WriteAllBytes(FileManager.Instance.fileIO.PathInGameDir(RelativePath), Bytes);
                        break;
                }
            }
        }

        private static Type GetFileType(string path)
        {
            path = path.ToLower();

            if (path.Contains(".emd")) return Type.EMD;
            if (path.Contains(".dyt.emb")) return Type.DYT_EMB;
            if (path.Contains(".emb")) return Type.EMB;
            if (path.Contains(".emm")) return Type.EMM;
            if (path.Contains(".ean")) return Type.EAN;
            if (path.Contains(".scd")) return Type.SCD;
            if (path.Contains(".esk")) return Type.SCD_ESK;

            return Type.Unknown;
        }
    
        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(Name));
            NotifyPropertyChanged(nameof(NameNoExt));
            NotifyPropertyChanged(nameof(IsLoaded));
        }
    
        /// <summary>
        /// Creates a shallow copy of the object, but leaves Owner null.
        /// </summary>
        public Xv2PartSetFile SoftCopy()
        {
            return new Xv2PartSetFile(Name, null)
            {
                File = File,
                Bytes = Bytes,
                IsLoaded = IsLoaded,
                PartTypes = PartTypes
            };
        }
        
        /// <summary>
        /// Creates a hard copy of the object, but leaves Owner null.
        /// </summary>
        public Xv2PartSetFile HardCopy()
        {
            return new Xv2PartSetFile(Name, null)
            {
                File = File != null ? File.Copy() : null,
                Bytes = Bytes != null ? Bytes.Copy() : null,
                IsLoaded = IsLoaded,
                PartTypes = PartTypes
            };
        }


    }

}
