using System;
using System.Linq;
using System.Collections.Generic;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.BCM;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAS;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.Xenoverse2;

namespace Xv2CoreLib
{
    [Serializable]
    public class Xv2MoveFiles
    {
        //Base
        public Xv2File<BDM_File> BdmFile { get; set; } = null;
        public AsyncObservableCollection<Xv2File<EAN_File>> EanFile { get; set; } = new AsyncObservableCollection<Xv2File<EAN_File>>();
        public AsyncObservableCollection<Xv2File<EAN_File>> CamEanFile { get; set; } = new AsyncObservableCollection<Xv2File<EAN_File>>();
        public AsyncObservableCollection<Xv2File<ACB_Wrapper>> SeAcbFile { get; set; } = new AsyncObservableCollection<Xv2File<ACB_Wrapper>>();
        public AsyncObservableCollection<Xv2File<ACB_Wrapper>> VoxAcbFile { get; set; } = new AsyncObservableCollection<Xv2File<ACB_Wrapper>>();

        //Skill
        public Xv2File<BSA_File> BsaFile { get; set; } = null;
        public Xv2File<BDM_File> ShotBdmFile { get; set; } = null;
        public Xv2File<BAS_File> BasFile { get; set; } = null;

        //BCM
        public Xv2File<BCM_File> BcmFile { get; set; } = null;
        public Xv2File<BCM_File> AfterBcmFile { get; set; } = null;

        //BAC
        public AsyncObservableCollection<Xv2File<BAC_File>> BacFiles { get; set; } = new AsyncObservableCollection<Xv2File<BAC_File>>();
        public Xv2File<BAC_File> BacFile
        {
            get => BacFiles.Count > 0 ? BacFiles[0] : null;
            set
            {
                if (BacFiles.Count > 0)
                    BacFiles[0] = value;
                else
                    BacFiles.Add(value);
            }
        }
        public Xv2File<BAC_File> AfterBacFile
        {
            get => BacFiles.Count > 1 ? BacFiles[1] : null;
            set
            {
                if (BacFiles.Count > 1)
                    BacFiles[1] = value;
                else if (BacFiles.Count == 1)
                    BacFiles.Add(value);
                else
                    throw new Exception("Cannot set AfterBacFile without a Base BacFile existing!");
            }
        }

        //EEPK
        public AsyncObservableCollection<Xv2File<EffectContainerFile>> EepkFiles { get; set; } = new AsyncObservableCollection<Xv2File<EffectContainerFile>>();
        public Xv2File<EffectContainerFile> EepkFile
        {
            get => (EepkFiles.Count > 0) ? EepkFiles[0] : null;
            set
            {
                if (EepkFiles.Count == 0)
                    EepkFiles.Add(value);
                else
                    EepkFiles[0] = value;
            }
        }

        
        //Paths (read-only, always re-calcate paths when saving. These are the paths as they were when the skill/character was first loaded.)
        public string BacPath = string.Empty;
        public string BcmPath = string.Empty;
        public string BdmPath = string.Empty;
        public List<string> EanPaths = new List<string>();
        public List<string> CamPaths = new List<string>();
        public string EepkPath = string.Empty;
        public string SeAcbPath = string.Empty;
        public List<string> VoxAcbPath = new List<string>();
        public string BsaPath = string.Empty;
        public string ShotBdmPath = string.Empty;
        public string BasPath = string.Empty;
        public string AfterBacPath = string.Empty;
        public string AfterBcmPath = string.Empty;


        public Xv2MoveFiles()
        {

        }


        #region AddEntry
        public int[] AddBdmEntry(IList<BDM_Entry> entries, bool shotBdm)
        {
            int[] indexes = new int[entries.Count];

            for (int i = 0; i < entries.Count; i++)
                indexes[i] = AddBdmEntry(entries[i], shotBdm);

            return indexes;
        }

        private int AddBdmEntry(BDM_Entry entry, bool shotBdm)
        {
            int idx = (shotBdm) ? ShotBdmFile.File.NextID() : BdmFile.File.NextID();
            var newCopy = entry.Copy();
            newCopy.ID = idx;

            if (shotBdm)
                ShotBdmFile.File.AddEntry(idx, newCopy);
            else
                BdmFile.File.AddEntry(idx, newCopy);

            return idx;
        }

        #endregion

        #region Add
        public void AddCamEanFile(EAN_File file, string chara, string path, bool borrowed = false, bool isDefault = false, MoveType moveType = 0)
        {
            int index = CamEanFile.IndexOf(CamEanFile.FirstOrDefault(x => x.CharaCode == chara));
            if (index != -1)
            {
                CamEanFile[index] = new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.CAM_EAN, 0, isDefault, moveType);
                CamEanFile[index].File.IsCharaUnique = !string.IsNullOrWhiteSpace(chara);
            }
            else
            {
                CamEanFile.Add(new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.CAM_EAN, 0, isDefault, moveType));
                CamEanFile[CamEanFile.Count - 1].File.IsCharaUnique = !string.IsNullOrWhiteSpace(chara);
            }
        }

        public void AddEanFile(EAN_File file, string chara, string path, bool borrowed = false, bool isDefault = false, MoveType moveType = 0)
        {
            int index = EanFile.IndexOf(EanFile.FirstOrDefault(x => x.CharaCode == chara));
            if (index != -1)
            {
                EanFile[index] = new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.EAN, 0, isDefault, moveType);
                EanFile[index].File.IsCharaUnique = !string.IsNullOrWhiteSpace(chara);
            }
            else
            {
                EanFile.Add(new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.EAN, 0, isDefault, moveType));
                EanFile[EanFile.Count - 1].File.IsCharaUnique = !string.IsNullOrWhiteSpace(chara);
            }
        }

        public void AddVoxAcbFile(ACB_Wrapper file, string chara, bool isEnglish, string path, bool borrowed = false, bool isDefault = false, MoveType moveType = 0)
        {
            int index = VoxAcbFile.IndexOf(VoxAcbFile.FirstOrDefault(x => x.CharaCode == chara && x.IsEnglish == isEnglish));
            if (index != -1)
                VoxAcbFile[index] = new Xv2File<ACB_Wrapper>(file, path, borrowed, chara, isEnglish, Xenoverse2.MoveFileTypes.VOX_ACB, 0, isDefault, moveType);
            else
                VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(file, path, borrowed, chara, isEnglish, Xenoverse2.MoveFileTypes.VOX_ACB, 0, isDefault, moveType));
        }

        public void AddSeAcbFile(ACB_Wrapper file, int costume, string path, bool borrowed = false, bool isDefault = true, MoveType moveType = 0)
        {
            var entry = new Xv2File<ACB_Wrapper>(file, path, borrowed, null, false, Xenoverse2.MoveFileTypes.SE_ACB, 0, isDefault, moveType);
            entry.Costumes.Clear();
            entry.Costumes.Add(costume);
            SeAcbFile.Add(entry);
        }

        #endregion

        #region Get
        public EAN_File GetCamEanFile(string chara, bool charaUnique)
        {
            if (charaUnique)
            {
                var file = CamEanFile.FirstOrDefault(x => x.CharaCode == chara);
                return (file != null) ? file.File : GetDefaultCamEanFile();
            }
            else
            {
                return GetDefaultOrFirstCamEanFile();
            }
        }

        public EAN_File GetEanFile(string chara, bool charaUnique)
        {
            if (charaUnique)
            {
                var file = EanFile.FirstOrDefault(x => x.CharaCode == chara);
                return (file != null) ? file.File : GetDefaultEanFile();
            }
            else
            {
                return GetDefaultOrFirstEanFile();
            }
        }

        public EAN_File GetFaceEanFile()
        {
            return EanFile.FirstOrDefault(x => x.FileType == Xenoverse2.MoveFileTypes.FCE_EAN)?.File;
        }

        public EAN_File GetFaceForeheadEanFile()
        {
            return EanFile.FirstOrDefault(x => x.FileType == Xenoverse2.MoveFileTypes.FCE_FOREHEAD_EAN)?.File;
        }

        public EAN_File GetTailEanFile()
        {
            return EanFile.FirstOrDefault(x => x.FileType == Xenoverse2.MoveFileTypes.TAL_EAN)?.File;
        }

        public ACB_Wrapper GetVoxFile(string chara, int costume, bool isEnglish)
        {
            var file = VoxAcbFile.FirstOrDefault(x => x.CharaCode == chara && x.IsEnglish == isEnglish && x.HasCostume(costume));

            if (file != null) return file.File;
            return VoxAcbFile.FirstOrDefault(x => x.CharaCode == chara && x.IsEnglish == isEnglish && x.IsDefault)?.File;
        }

        public ACB_Wrapper GetVoxFile(int costume, bool isEnglish)
        {
            var file = VoxAcbFile.FirstOrDefault(x => x.IsEnglish == isEnglish && x.HasCostume(costume));

            if (file != null) return file.File;
            return VoxAcbFile.FirstOrDefault(x => x.IsEnglish == isEnglish && x.IsDefault)?.File;
        }

        public ACB_Wrapper GetSeFile(int costume = -1)
        {
            if (SeAcbFile.Count > 0 && costume == -1)
                return SeAcbFile[0].File;

            var file = SeAcbFile.FirstOrDefault(x => x.HasCostume(costume))?.File;

            if (file != null) return file;
            return SeAcbFile.FirstOrDefault(x => x.IsDefault)?.File;
        }

        private EAN_File GetDefaultEanFile()
        {
            foreach (var ean in EanFile)
                if (string.IsNullOrWhiteSpace(ean.CharaCode)) return ean.File;
            return null;
        }

        private EAN_File GetDefaultCamEanFile()
        {
            foreach (var ean in CamEanFile)
                if (string.IsNullOrWhiteSpace(ean.CharaCode)) return ean.File;
            return null;
        }

        public EAN_File GetDefaultOrFirstEanFile()
        {
            var ean = GetDefaultEanFile();
            if (ean != null) return ean;
            if (EanFile.Count == 0) return null;
            return EanFile[0].File;
        }

        public EAN_File GetDefaultOrFirstCamEanFile()
        {
            var ean = GetDefaultCamEanFile();
            if (ean != null) return ean;
            if (CamEanFile.Count == 0) return null;
            return CamEanFile[0].File;
        }

        #endregion

        public void UpdateTypeStrings()
        {
            UpdateTypeString(BacFile, Xenoverse2.MoveFileTypes.BAC);
            UpdateTypeString(BcmFile, Xenoverse2.MoveFileTypes.BCM);
            UpdateTypeString(BdmFile, Xenoverse2.MoveFileTypes.BDM);
            UpdateTypeString(ShotBdmFile, Xenoverse2.MoveFileTypes.SHOT_BDM);
            UpdateTypeString(BsaFile, Xenoverse2.MoveFileTypes.BSA);
            UpdateTypeString(BasFile, Xenoverse2.MoveFileTypes.BAS);
            UpdateTypeString(AfterBacFile, Xenoverse2.MoveFileTypes.AFTER_BAC);
            UpdateTypeString(AfterBcmFile, Xenoverse2.MoveFileTypes.AFTER_BCM);

            foreach(var file in EepkFiles)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.EEPK);

            foreach (var file in VoxAcbFile)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.VOX_ACB);

            foreach (var file in SeAcbFile)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.SE_ACB);

            foreach (var file in EanFile)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.EAN);

            foreach (var file in CamEanFile)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.CAM_EAN);
        }

        private void UpdateTypeString<T>(Xv2File<T> file, Xenoverse2.MoveFileTypes typeString) where T : class
        {
            if (file != null)
                file.FileType = typeString;
        }

        public List<IUndoRedo> UnborrowFile(object xv2FileObject)
        {
            if (BacFile == xv2FileObject) return BacFile.UnborrowFile();
            if (BcmFile == xv2FileObject) return BcmFile.UnborrowFile();
            if (BsaFile == xv2FileObject) return BsaFile.UnborrowFile();
            if (ShotBdmFile == xv2FileObject) return ShotBdmFile.UnborrowFile();
            if (BdmFile == xv2FileObject) return BdmFile.UnborrowFile();
            if (BasFile == xv2FileObject) return BasFile.UnborrowFile();
            if (AfterBacFile == xv2FileObject) return AfterBacFile.UnborrowFile();
            if (AfterBcmFile == xv2FileObject) return AfterBcmFile.UnborrowFile();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EepkFiles.Contains(xv2FileObject))
            {
                foreach (var file in EepkFiles)
                {
                    undos.AddRange(file.UnborrowFile());
                }
            }

            if (EanFile.Contains(xv2FileObject))
            {
                foreach (var file in EanFile)
                {
                    undos.AddRange(file.UnborrowFile());
                }
            }

            if (CamEanFile.Contains(xv2FileObject))
            {
                foreach (var file in CamEanFile)
                {
                    undos.AddRange(file.UnborrowFile());
                }
            }

            if (VoxAcbFile.Contains(xv2FileObject))
            {
                foreach (var file in VoxAcbFile)
                {
                    undos.AddRange(file.UnborrowFile());
                }
            }

            if (SeAcbFile.Contains(xv2FileObject))
            {
                foreach (var file in SeAcbFile)
                {
                    undos.AddRange(file.UnborrowFile());
                }
            }

            return undos;
        }

        public List<IUndoRedo> ReplaceFile(object xv2FileObject, string path)
        {
            if (BacFile == xv2FileObject) return BacFile.ReplaceFile(path);
            if (BcmFile == xv2FileObject) return BcmFile.ReplaceFile(path);
            if (BsaFile == xv2FileObject) return BsaFile.ReplaceFile(path);
            if (ShotBdmFile == xv2FileObject) return ShotBdmFile.ReplaceFile(path);
            if (BdmFile == xv2FileObject) return BdmFile.ReplaceFile(path);
            if (BasFile == xv2FileObject) return BasFile.ReplaceFile(path);
            if (AfterBacFile == xv2FileObject) return AfterBacFile.ReplaceFile(path);
            if (AfterBcmFile == xv2FileObject) return AfterBcmFile.ReplaceFile(path);

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EepkFiles.Contains(xv2FileObject))
            {
                foreach (var file in EepkFiles)
                {
                    undos.AddRange(file.ReplaceFile(path));
                }
            }

            if (EanFile.Contains(xv2FileObject))
            {
                foreach (var file in EanFile)
                {
                    undos.AddRange(file.ReplaceFile(path));
                }
            }

            if (CamEanFile.Contains(xv2FileObject))
            {
                foreach (var file in CamEanFile)
                {
                    undos.AddRange(file.ReplaceFile(path));
                }
            }

            if (VoxAcbFile.Contains(xv2FileObject))
            {
                foreach (var file in VoxAcbFile)
                {
                    undos.AddRange(file.ReplaceFile(path));
                }
            }

            if (SeAcbFile.Contains(xv2FileObject))
            {
                foreach (var file in SeAcbFile)
                {
                    undos.AddRange(file.ReplaceFile(path));
                }
            }

            return undos;
        }

        public bool CanRemoveFile(object file)
        {
            return (bool)file.GetType().GetMethod("IsOptionalFile")?.Invoke(file, null);
        }

        public List<IUndoRedo> RemoveFile(object xv2FileObject)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (EanFile.Contains(xv2FileObject))
            {
                undos.Add(new UndoableListRemove<Xv2File<EAN_File>>(EanFile, (Xv2File<EAN_File>)xv2FileObject));
                EanFile.Remove((Xv2File<EAN_File>)xv2FileObject);
            }

            if (CamEanFile.Contains(xv2FileObject))
            {
                undos.Add(new UndoableListRemove<Xv2File<EAN_File>>(CamEanFile, (Xv2File<EAN_File>)xv2FileObject));
                CamEanFile.Remove((Xv2File<EAN_File>)xv2FileObject);
            }

            if (VoxAcbFile.Contains(xv2FileObject))
            {
                undos.Add(new UndoableListRemove<Xv2File<ACB_Wrapper>>(VoxAcbFile, (Xv2File<ACB_Wrapper>)xv2FileObject));
                VoxAcbFile.Remove((Xv2File<ACB_Wrapper>)xv2FileObject);
            }

            if (AfterBacFile == xv2FileObject)
            {
                undos.Add(new UndoableProperty<Xv2MoveFiles>(nameof(AfterBacFile), this, AfterBacFile, null));
                AfterBacFile = null;
            }

            if (AfterBcmFile == xv2FileObject)
            {
                undos.Add(new UndoableProperty<Xv2MoveFiles>(nameof(AfterBcmFile), this, AfterBcmFile, null));
                AfterBcmFile = null;
            }

            return undos;
        }

    }

}
