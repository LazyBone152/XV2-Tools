using System;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.MSG;
using Xv2CoreLib.BCS;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.CSO;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ERS;
using Xv2CoreLib.IDB;
using Xv2CoreLib.PUP;
using Xv2CoreLib.BCM;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAI;
using Xv2CoreLib.AMK;
using Xv2CoreLib.BAS;
using Xv2CoreLib.ESK;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.PSC;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.Xenoverse2;
using static Xv2CoreLib.CUS.CUS_File;

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
        public AsyncObservableCollection<Xv2BcsFile> PartSetFiles { get; set; } = new AsyncObservableCollection<Xv2BcsFile>();

        //Moveset:
        public Xv2MoveFiles MovesetFiles { get; set; }

        //EMDs, ESKs loaded seperately
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
        }

        public void CalculateFilePaths()
        {
            string skillDir = $"chara/{CmsEntry.ShortName}";

            if (MovesetFiles.BacFile?.Borrowed == false)
            {
                MovesetFiles.BacFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bac", skillDir, CmsEntry.ShortName));
                CmsEntry.BacPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BcmFile?.Borrowed == false)
            {
                MovesetFiles.BcmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bcm", skillDir, CmsEntry.ShortName));
                CmsEntry.BcmPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BdmFile?.Borrowed == false)
            {
                MovesetFiles.BdmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bdm", skillDir, CmsEntry.ShortName));
                CmsEntry.BdmPath = CmsEntry.ShortName;
            }

            if (!MovesetFiles.CamEanFile[0]?.Borrowed == false)
            {
                MovesetFiles.CamEanFile[0].Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.cam.ean", skillDir, CmsEntry.ShortName));
                CmsEntry.CamEanPath = CmsEntry.ShortName;
            }

            if (BaiFile?.Borrowed == false)
            {
                BaiFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.bai", skillDir, CmsEntry.ShortName));
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
                    AmkFile[i].Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}{2}.amk", skillDir, CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
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
                mainEan.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.ean", skillDir, CmsEntry.ShortName));
                CmsEntry.EanPath = CmsEntry.ShortName;
            }

            if (fceEan?.Borrowed == false)
            {
                fceEan.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}.fce.ean", skillDir, CmsEntry.ShortName));
                CmsEntry.FceEanPath = CmsEntry.ShortName;
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
        /// <summary>
        /// Loads the specified PartSet and stores the files in <see cref="PartSetFiles"/>.
        /// </summary>
        /// <param name="id"PartSet ID></param>
        /// <returns>Signals whether the PartSet was found or not</returns>
        public bool LoadPartSet(int id, bool forceReload = false)
        {
            PartSet partSet = BcsFile.File.PartSets.FirstOrDefault(x => x.ID == id);

            if(partSet != null)
            {
                LoadPart(partSet.FaceBase, PartType.FaceBase, forceReload);
                LoadPart(partSet.FaceEar, PartType.FaceEar, forceReload);
                LoadPart(partSet.FaceEye, PartType.FaceEye, forceReload);
                LoadPart(partSet.FaceForehead, PartType.FaceForehead, forceReload);
                LoadPart(partSet.FaceNose, PartType.FaceNose, forceReload);
                LoadPart(partSet.Hair, PartType.Hair, forceReload);
                LoadPart(partSet.Bust, PartType.Bust, forceReload);
                LoadPart(partSet.Pants, PartType.Pants, forceReload);
                LoadPart(partSet.Boots, PartType.Boots, forceReload);
                LoadPart(partSet.Rist, PartType.Rist, forceReload);
                return true;
            }

            return false;
        }

        private void LoadPart(Part part, PartType type, bool forceReload)
        {
            string emd = part.GetModelPath(type);
            string emb = part.GetEmbPath(type);
            string emm = part.GetEmmPath(type);
            string dyt = part.GetDytPath(type);
            string ean = part.GetEanPath();

            LoadPartSetFile(emd, part.CharaCode, null, forceReload);
            LoadPartSetFile(emb, part.CharaCode, null, forceReload);
            LoadPartSetFile(emm, part.CharaCode, null, forceReload);
            LoadPartSetFile(ean, part.CharaCode, null, forceReload);
            LoadPartSetFile(dyt, part.CharaCode, null, forceReload);

            if(part.Physics_Objects != null)
            {
                foreach (var physicsPart in part.Physics_Objects)
                {
                    string physicsEmd = physicsPart.GetModelPath();
                    string physicsEmb = physicsPart.GetEmbPath();
                    string physicsEmm = physicsPart.GetEmmPath();
                    string physicsDyt = physicsPart.GetDytPath();
                    string physicsEan = physicsPart.GetEanPath();
                    string physicsScd = physicsPart.GetScdPath();

                    LoadPartSetFile(physicsEmd, physicsPart.CharaCode, null, forceReload);
                    LoadPartSetFile(physicsEmb, physicsPart.CharaCode, emb, forceReload);
                    LoadPartSetFile(physicsEmm, physicsPart.CharaCode, emm, forceReload);
                    LoadPartSetFile(physicsDyt, physicsPart.CharaCode, dyt, forceReload);
                    LoadPartSetFile(physicsEan, physicsPart.CharaCode, null, forceReload);
                    LoadPartSetFile(physicsScd, physicsPart.CharaCode, null, forceReload);
                }
            }
        }

        private void LoadPartSetFile(string path, string charaCode, string altPath = null, bool forceReload = false)
        {
            if (string.IsNullOrWhiteSpace(path)) return; //Nothing to load
            if (PartSetFiles.Any(x => x.Name == Path.GetFileName(path) && x.CharacterCode == charaCode) && !forceReload) return; //File already loaded

            path = Utils.SanitizePath(path.ToLower());

            //Special case: EMB, EMM and DYT for physics parts can use the parents files if none are found
            if (!FileManager.Instance.fileIO.FileExists(path))
                path = altPath;

            Xv2BcsFile.Type type = Xv2BcsFile.GetType(path);
            Xv2BcsFile xv2bcsFile = null;

            //Get name and chara folder
            string name = Path.GetFileName(path);

            switch (type)
            {
                case Xv2BcsFile.Type.EMD:
                    xv2bcsFile = new Xv2BcsFile((EMD_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode);
                    break;
                case Xv2BcsFile.Type.EMB:
                    xv2bcsFile = new Xv2BcsFile((EMB_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode, false);
                    break;
                case Xv2BcsFile.Type.DYT_EMB:
                    xv2bcsFile = new Xv2BcsFile((EMB_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode, true);
                    break;
                case Xv2BcsFile.Type.EMM:
                    xv2bcsFile = new Xv2BcsFile((EMM_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode);
                    break;
                case Xv2BcsFile.Type.EAN:
                    xv2bcsFile = new Xv2BcsFile((EAN_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode);
                    break;
                case Xv2BcsFile.Type.SCD_ESK:
                    xv2bcsFile = new Xv2BcsFile((ESK_File)FileManager.Instance.GetParsedFileFromGame(path, forceReload: forceReload), name, charaCode);
                    break;
                case Xv2BcsFile.Type.SCD:
                    xv2bcsFile = new Xv2BcsFile(FileManager.Instance.GetBytesFromGame(path), name, charaCode);
                    break;
            }

            //If file already exists, then we will overwrite it. Normally this will never happen unless forceReload is true.
            int idx = PartSetFiles.IndexOf(PartSetFiles.FirstOrDefault(x => x.Name == Path.GetFileName(path) && x.CharacterCode == charaCode));

            if(idx != -1)
            {
                PartSetFiles[idx] = xv2bcsFile;
            }
            else
            {
                PartSetFiles.Add(xv2bcsFile);
            }
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

    public class Xv2CharaCostume : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
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

    public class Xv2BcsFile
    {
        public enum Type
        {
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
        /// <summary>
        /// The character code of the owner of this file. For a self-owned file, this should be null or <see cref="string.Empty"/>
        /// </summary>
        public string CharacterCode { get; set; } 

        //Type:
        public Type FileType { get; private set; }
        /// <summary>
        /// When true, <see cref="File"/> will contain the parsed file of the type specified in <see cref="FileType"/>. Otherwise, the raw bytes of the file will be stored in <see cref="Bytes"/>
        /// </summary>
        public bool IsParsed { get; private set; }

        //Data:
        public object File { get; private set; }
        public byte[] Bytes { get; private set; }

        //Constructors:
        public Xv2BcsFile(EMB_File file, string name, string charaCode, bool isDyt) : this(file, name, charaCode, isDyt ? Type.DYT_EMB : Type.EMB) { }

        public Xv2BcsFile(EMD_File file, string name, string charaCode) : this(file, name, charaCode, Type.EMD) { }

        public Xv2BcsFile(EMM_File file, string name, string charaCode) : this(file, name, charaCode, Type.EMM) { }

        public Xv2BcsFile(EAN_File file, string name, string charaCode) : this(file, name, charaCode, Type.EAN) { }

        public Xv2BcsFile(ESK_File file, string name, string charaCode) : this(file, name, charaCode, Type.SCD_ESK) { }

        public Xv2BcsFile(byte[] bytes, string name, string charaCode) : this(bytes, name, charaCode, Type.SCD) { }

        public Xv2BcsFile(object file, string name, string charaCode, Type type)
        {
            Name = name;
            CharacterCode = charaCode;
            FileType = type;

            if(file.GetType() == typeof(byte[]))
            {
                Bytes = (byte[])file;
            }
            else
            {
                File = file;
                IsParsed = true;
            }

        }

        public void Save()
        {
            FileManager.Instance.SaveFileToGame($"chara/{CharacterCode}/{Name}", File);
        }

        public static Type GetType(string path)
        {
            path = path.ToLower();

            if (path.Contains(".emd")) return Type.EMD;
            if (path.Contains(".dyt.emb")) return Type.DYT_EMB;
            if (path.Contains(".emb")) return Type.EMB;
            if (path.Contains(".emm")) return Type.EMM;
            if (path.Contains(".ean")) return Type.EAN;
            if (path.Contains(".scd")) return Type.SCD;
            if (path.Contains(".esk")) return Type.SCD_ESK;

            return 0;
        }
    }

}
