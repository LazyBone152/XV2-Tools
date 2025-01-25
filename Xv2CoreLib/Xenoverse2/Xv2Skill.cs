using System;
using System.Linq;
using System.Collections.ObjectModel;
using Xv2CoreLib.CUS;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.IDB;
using Xv2CoreLib.PUP;
using Xv2CoreLib.BCM;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAS;
using Xv2CoreLib.EffectContainer;
using static Xv2CoreLib.CUS.CUS_File;
using Xv2CoreLib.ValuesDictionary;

namespace Xv2CoreLib
{
    public class Xv2Skill
    {
        public SkillType skillType = SkillType.Super;
        public Skill CusEntry = null;
        public IDB_Entry IdbEntry = null;
        public ObservableCollection<PUP_Entry> PupEntries = new ObservableCollection<PUP_Entry>();
        public string[] Name = new string[(int)Xenoverse2.Language.NumLanguages];
        public string[] Description = new string[(int)Xenoverse2.Language.NumLanguages];
        public ObservableCollection<string[]> BtlHud = new ObservableCollection<string[]>();


        public Xv2MoveFiles Files = null;

        public void SaveMoveFiles()
        {
            foreach (var bac in Files.BacFiles)
            {
                if (!bac.File.IsNull())
                {
                    bac.File.Save(bac.Path);
                }
                CustomEntryNames.SaveNames(bac.RelativePath, bac.File);
            }

            if (Files.BcmFile?.File?.IsNull() == false)
                Files.BcmFile.File.Save(Files.BcmFile.Path);

            if (Files.BsaFile?.File?.IsNull() == false)
                Files.BsaFile.File.Save(Files.BsaFile.Path);

            if (Files.BsaFile?.File != null)
                CustomEntryNames.SaveNames(Files.BsaFile.RelativePath, Files.BsaFile.File);

            if (Files.BdmFile?.File?.IsNull() == false)
                Files.BdmFile.File.Save(Files.BdmFile.Path);

            if(Files.BdmFile?.File != null)
                CustomEntryNames.SaveNames(Files.BdmFile.RelativePath, Files.BdmFile.File);

            if (Files.ShotBdmFile?.File?.IsNull() == false)
                Files.ShotBdmFile.File.Save(Files.ShotBdmFile.Path);

            if(Files.ShotBdmFile?.File != null)
                CustomEntryNames.SaveNames(Files.ShotBdmFile.RelativePath, Files.ShotBdmFile.File);

            if (Files.BasFile?.File?.IsNull() == false)
                Files.BasFile.File.Save(Files.BasFile.Path);

            if (Files.SeAcbFile.Count > 0)
            {
                //Skills only have one SE file
                if (Files.SeAcbFile[0]?.File?.IsNull() == false)
                    Files.SeAcbFile[0].File.AcbFile.Save(Files.SeAcbFile[0].Path);
            }

            if (Files.EepkFile?.File?.IsNull() == false)
            {
                Files.EepkFile.File.ChangeFilePath(Files.EepkFile.Path);
                Files.EepkFile.File.Save();
            }

            if(Files.EepkFile?.File != null)
                CustomEntryNames.SaveNames(Files.EepkFile.RelativePath, Files.EepkFile.File);

            if (Files.EanFile != null)
            {
                foreach (var ean in Files.EanFile)
                {
                    if (!ean.File.IsNull())
                        ean.File.Save(ean.Path);
                }
            }

            if (Files.CamEanFile != null)
            {
                foreach (var ean in Files.CamEanFile)
                {
                    if (!ean.File.IsNull())
                        ean.File.Save(ean.Path);
                }
            }

            if (Files.VoxAcbFile != null)
            {
                foreach (var vox in Files.VoxAcbFile)
                {
                    if (!vox.File.IsNull())
                        vox.File.AcbFile.Save(vox.Path);
                }
            }

            if (Files.AfterBcmFile?.File?.IsNull() == false)
                Files.AfterBcmFile.File.Save(Files.AfterBcmFile.Path);
        }

        public void CalculateSkillFilePaths()
        {
            string skillDir = Xenoverse2.Instance.GetSkillDir(skillType);
            string folderName = Xenoverse2.Instance.GetSkillFolderName(CusEntry);

            if (!Files.BacFile.Borrowed)
                Files.BacFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bac", skillDir, folderName));

            if (!Files.BcmFile.Borrowed)
                Files.BcmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.bcm", skillDir, folderName));

            if (!Files.BdmFile.Borrowed)
                Files.BdmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.bdm", skillDir, folderName));

            if (!Files.ShotBdmFile.Borrowed)
                Files.ShotBdmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.shot.bdm", skillDir, folderName));

            if (!Files.BsaFile.Borrowed)
                Files.BsaFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bsa", skillDir, folderName));

            if (!Files.BasFile.Borrowed)
                Files.BasFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bas", skillDir, folderName));

            if (!Files.EepkFile.Borrowed)
                Files.EepkFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.eepk", skillDir, folderName));

            if (!Files.SeAcbFile[0].Borrowed)
                Files.SeAcbFile[0].Path = FileManager.Instance.GetAbsolutePath(string.Format(@"sound/SE/Battle/Skill/CAR_BTL_{2}{1}_{0}_SE.acb", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType)));

            foreach (var vox in Files.VoxAcbFile)
            {
                if (!vox.Borrowed)
                {
                    if (vox.IsEnglish)
                    {
                        vox.Path = FileManager.Instance.GetAbsolutePath(string.Format(@"sound/VOX/Battle/Skill/en/CAR_BTL_{2}{1}_{0}_{3}_VOX", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType), vox.CharaCode));
                    }
                    else
                    {
                        //Japanese
                        vox.Path = FileManager.Instance.GetAbsolutePath(string.Format(@"sound/VOX/Battle/Skill/CAR_BTL_{2}{1}_{0}_{3}_VOX", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType), vox.CharaCode));
                    }
                }
            }

            foreach (var ean in Files.EanFile)
            {
                if (!ean.Borrowed)
                {
                    if (ean.IsDefault)
                    {
                        ean.Path = FileManager.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}.ean", skillDir, folderName));
                    }
                    else
                    {
                        //Chara specific eans
                        ean.Path = FileManager.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}_{2}.ean", skillDir, folderName, ean.CharaCode));
                    }
                }
            }

            foreach (var ean in Files.CamEanFile)
            {
                if (!ean.Borrowed)
                {
                    if (ean.IsDefault)
                    {
                        ean.Path = FileManager.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}.cam.ean", skillDir, folderName));
                    }
                    else
                    {
                        //Chara specific eans
                        ean.Path = FileManager.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}_{2}.cam.ean", skillDir, folderName, ean.CharaCode));
                    }
                }
            }

            //These files, at least as of the current code, can be null... so a check is required
            if (Files.AfterBacFile?.Borrowed == false)
                Files.AfterBacFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_AFTER.bac", skillDir, folderName));

            if (Files.AfterBcmFile?.Borrowed == false)
                Files.AfterBcmFile.Path = FileManager.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_AFTER_PLAYER.bcm", skillDir, folderName));

        }

        public void UpdateCusFlags()
        {
            Skill.FilesLoadedFlags flags1 = CusEntry.FilesLoadedFlags1;
            Skill.Type flags2 = CusEntry.FilesLoadedFlags2;

            //Remove flags for all file types
            flags1 &= ~(Skill.FilesLoadedFlags.AfterBac | Skill.FilesLoadedFlags.AfterBcm | Skill.FilesLoadedFlags.CamEan | Skill.FilesLoadedFlags.Bas | Skill.FilesLoadedFlags.Bdm | Skill.FilesLoadedFlags.BsaAndShotBdm | Skill.FilesLoadedFlags.CharaSE | Skill.FilesLoadedFlags.CharaVOX | Skill.FilesLoadedFlags.Eepk | Skill.FilesLoadedFlags.ShotEepk);
            flags2 &= ~(Skill.Type.BAC | Skill.Type.BCM | Skill.Type.EAN);

            //Set flags

            if (!Files.BacFile.File.IsNull())
                flags2 |= Skill.Type.BAC;

            if (!Files.BcmFile.File.IsNull())
                flags2 |= Skill.Type.BCM;

            foreach (var ean in Files.EanFile)
            {
                if (ean.IsDefault && !ean.File.IsNull())
                {
                    flags2 |= Skill.Type.EAN;
                }
                else if (!ean.IsDefault)
                {
                    flags2 |= Skill.Type.EAN;
                    flags1 |= Skill.FilesLoadedFlags.CharaSpecificEan;
                }
            }

            foreach (var ean in Files.CamEanFile)
            {
                if (ean.IsDefault && !ean.File.IsNull())
                {
                    flags1 |= Skill.FilesLoadedFlags.CamEan;
                }
                else if (!ean.IsDefault && !ean.File.IsNull())
                {
                    flags1 |= (Skill.FilesLoadedFlags.CamEan | Skill.FilesLoadedFlags.CharaSpecificCamEan);
                }
            }

            if (Files.AfterBacFile?.File.IsNull() == false)
                flags1 |= Skill.FilesLoadedFlags.AfterBac;

            if (Files.AfterBcmFile?.File.IsNull() == false)
                flags1 |= Skill.FilesLoadedFlags.AfterBcm;

            if (!Files.BasFile.File.IsNull())
                flags1 |= Skill.FilesLoadedFlags.Bas;

            if (!Files.BdmFile.File.IsNull())
                flags1 |= Skill.FilesLoadedFlags.Bdm;

            if (!Files.BsaFile.File.IsNull() || !Files.ShotBdmFile.File.IsNull())
                flags1 |= Skill.FilesLoadedFlags.BsaAndShotBdm;

            if (!Files.SeAcbFile[0].File.IsNull())
                flags1 |= Skill.FilesLoadedFlags.CharaSE;

            if (!Files.EepkFile.File.IsNull())
                flags1 |= Skill.FilesLoadedFlags.Eepk;

            foreach (var vox in Files.VoxAcbFile)
            {
                if (!vox.File.IsNull())
                {
                    flags1 |= Skill.FilesLoadedFlags.CharaVOX;
                    break;
                }
            }

            //Update cus flags
            CusEntry.FilesLoadedFlags1 = flags1;
            CusEntry.FilesLoadedFlags2 = flags2;
        }

        /// <summary>
        /// Create default files if none were loaded.
        /// </summary>
        public void CreateDefaultFiles()
        {
            if (Files == null) throw new NullReferenceException("Xv2Skill.CreateDefaultFiles: Files was null.");

            if (Files.BacFile == null)
                Files.BacFile = new Xv2File<BAC_File>(BAC_File.DefaultBacFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BAC);

            if (Files.BcmFile == null)
                Files.BcmFile = new Xv2File<BCM_File>(BCM_File.DefaultBcmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BCM);

            if (Files.BsaFile == null)
                Files.BsaFile = new Xv2File<BSA_File>(new BSA_File(), null, false, null, false, Xenoverse2.MoveFileTypes.BSA);

            if (Files.BdmFile == null)
                Files.BdmFile = new Xv2File<BDM_File>(BDM_File.DefaultBdmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.BDM);

            if (Files.ShotBdmFile == null)
                Files.ShotBdmFile = new Xv2File<BDM_File>(BDM_File.DefaultBdmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.SHOT_BDM);

            if (Files.EepkFile == null)
                Files.EepkFile = new Xv2File<EffectContainerFile>(EffectContainerFile.New(), null, false, null, false, Xenoverse2.MoveFileTypes.EEPK);

            if (Files.BasFile == null)
                Files.BasFile = new Xv2File<BAS_File>(new BAS_File(), null, false, null, false, Xenoverse2.MoveFileTypes.BAS);

            if (Files.EanFile.FirstOrDefault(x => x.IsDefault) == null && Xenoverse2.Instance.CmnEan != null)
                Files.EanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultFile(Xenoverse2.Instance.CmnEan.Skeleton), null, false, null, false, Xenoverse2.MoveFileTypes.EAN));

            if (Files.CamEanFile.FirstOrDefault(x => x.IsDefault) == null)
                Files.CamEanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultCamFile(), null, false, null, false, Xenoverse2.MoveFileTypes.CAM_EAN));

            if (Files.SeAcbFile.FirstOrDefault(x => x.IsDefault) == null)
                Files.SeAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.SE_ACB));

            if (Files.AfterBacFile == null)
                Files.AfterBacFile = new Xv2File<BAC_File>(BAC_File.DefaultBacFile(), null, false, null, false, Xenoverse2.MoveFileTypes.AFTER_BAC);

            if (Files.AfterBcmFile == null)
                Files.AfterBcmFile = new Xv2File<BCM_File>(BCM_File.DefaultBcmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.AFTER_BCM);
        }
    }
}
