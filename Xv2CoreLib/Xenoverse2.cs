using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.Resource;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.MSG;
using Xv2CoreLib.BCS;
using System.IO;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.CSO;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ERS;
using Xv2CoreLib.IDB;
using Xv2CoreLib.EffectContainer;
using System.Collections.ObjectModel;
using Xv2CoreLib.PUP;
using Xv2CoreLib.BCM;
using Xv2CoreLib.ACB_NEW;
using Xv2CoreLib.BAI;
using Xv2CoreLib.AMK;
using Xv2CoreLib.BAS;
using Xv2CoreLib.ESK;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource.UndoRedo;
using System.ComponentModel;
using static Xv2CoreLib.CUS.CUS_File;
using Xv2CoreLib.PSC;

namespace Xv2CoreLib
{
    public sealed class Xenoverse2
    {
        public enum Language
        {
            English, 
            Spanish1,
            Spanish2,
            French,
            German,
            Italian,
            Portuguese,
            Polish,
            Russian,
            Chinese1,
            Chinese2,
            Korean,

            NumLanguages
        }

        public enum MoveFileTypes
        {
            BAC,
            BCM,
            BSA,
            BDM,
            SHOT_BDM,
            EEPK,
            SE_ACB,
            VOX_ACB,
            EAN,
            CAM_EAN,
            BAS,
            AFTER_BAC,
            AFTER_BCM,
            BAI,
            AMK,
            FCE_EAN,
            TAL_EAN //CMN
        }

        public static readonly string[] LanguageSuffix = new string[12] { "en.msg", "es.msg", "ca.msg", "fr.msg", "de.msg", "it.msg", "pt.msg", "pl.msg", "ru.msg", "tw.msg", "zh.msg", "kr.msg" };
        public static readonly string[] LanguageSuffixNoExt = new string[12] { "en", "es", "ca", "fr", "de", "it", "pt", "pl", "ru", "tw", "zh", "kr" };

        //Singleton
        private static Lazy<Xenoverse2> instance = new Lazy<Xenoverse2>(() => new Xenoverse2());
        public static Xenoverse2 Instance => instance.Value;
        public static string GameDir { get; set; }

        //File Paths
        public const string ERS_PATH = "vfx/vfx_spec.ers";
        public const string CUS_PATH = "system/custom_skill.cus";
        public const string CMS_PATH = "system/char_model_spec.cms";
        public const string PUP_PATH = "system/powerup_parameter.pup";
        public const string CSO_PATH = "system/chara_sound.cso";
        public const string PSC_PATH = "system/parameter_spec_char.psc";
        public const string SKILL_IDB_PATH = "system/item/skill_item.idb";
        public const string TALISMAN_IDB_PATH = "system/item/talisman_item.idb";
        public const string CMN_BAC_PATH = "chara/CMN/CMN.bac";
        public const string CMN_EAN_PATH = "chara/CMN/CMN.ean";
        public const string CMN_CAM_EAN_PATH = "chara/CMN/CMN.cam.ean";
        public const string CMN_BDM_PATH = "chara/CMN/CMN.bdm";
        public const string CMN_BSA_PATH = "skill/CMN/CMN.bsa";
        public const string CMN_SHOT_BDM_PATH = "skill/CMN/CMN_PLAYER.shot.bdm";
        public const string CMN_SE_ACB_PATH = "sound/SE/Battle/Common/CAR_BTL_CMN.acb";
        public const string CMN_EEPK_PATH = "vfx/cmn/BTL_CMN.eepk";
        public const string CMN_TAL_EAN = "chara/CMN/CMN.tal.ean";

        public const string CHARACTER_MSG_PATH = "msg/proper_noun_character_name_";
        public const string SUPER_SKILL_MSG_PATH = "msg/proper_noun_skill_spa_name_";
        public const string ULT_SKILL_MSG_PATH = "msg/proper_noun_skill_ult_name_";
        public const string AWOKEN_SKILL_MSG_PATH = "msg/proper_noun_skill_met_name_";
        public const string EVASIVE_SKILL_MSG_PATH = "msg/proper_noun_skill_esc_name_";
        public const string SUPER_SKILL_DESC_MSG_PATH = "msg/proper_noun_skill_spa_info_";
        public const string ULT_SKILL_DESC_MSG_PATH = "msg/proper_noun_skill_ult_info_";
        public const string AWOKEN_SKILL_DESC_MSG_PATH = "msg/proper_noun_skill_met_info_";
        public const string EVASIVE_SKILL_DESC_MSG_PATH = "msg/proper_noun_skill_esc_info_";
        public const string BTLHUD_MSG_PATH = "msg/quest_btlhud_";

        //Load bools
        public bool loadCmn = false;
        public bool loadSkills = true;
        public bool loadCharacters = true;

        //System Files
        private CUS_File cusFile = null;
        private CMS_File cmsFile = null;
        private ERS_File ersFile = null;
        private IDB_File skillIdbFile = null;
        private PUP_File pupFile = null;
        private CSO_File csoFile = null;
        private PSC_File pscFile = null;

        //Cmn Files
        public EAN_File CmnEan = null;
        public EAN_File CmnCamEan = null;
        public BAC_File CmnBac = null;
        public BDM_File CmnBdm = null;

        //Msg Files
        private MSG_File[] charaNameMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] spaSkillNameMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] ultSkillNameMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] evaSkillNameMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] metSkillNameMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] spaSkillDescMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] ultSkillDescMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] evaSkillDescMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] metSkillDescMsgFile = new MSG_File[(int)Language.NumLanguages];
        private MSG_File[] btlHudMsgFile = new MSG_File[(int)Language.NumLanguages];

        //Misc variables
        private Xv2FileIO fileIO;
        public Language PreferedLanguage = Language.English;
        public bool IsInitialized = false;

        //File Watcher
        private FileWatcher fileWatcher = new FileWatcher();

        //Events
        /// <summary>
        /// Raised when a file is not found and an exception is not thrown.
        /// </summary>
        public static event Xv2FileNotFoundEventHandler FileNotFoundEvent;


        #region Initialization
        private Xenoverse2()
        {
        }

        public void Init()
        {
            try
            {
                fileWatcher.ClearAll();
                IsInitialized = false;
                LoadFileIO();

                if (loadCharacters)
                    InitCharacters();

                if (loadSkills)
                    InitSkills();

                if (loadCmn)
                    InitCmn();
            }
            finally
            {
                IsInitialized = true;
            }
        }

        private void LoadFileIO()
        {
            if (ShouldLoadFileIO())
            {
                if (!File.Exists(string.Format("{0}/bin/DBXV2.exe", GameDir)))
                    GameDir = FindGameDirectory();

                if (File.Exists(string.Format("{0}/bin/DBXV2.exe", GameDir)))
                {
                    fileIO = new Xv2FileIO(GameDir, false, new string[] { "data_d4_5_xv1.cpk", "data_d6_dlc.cpk", "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk" });
                }
                else
                {
                    throw new FileNotFoundException("Xenoverse2.Init: GameDirectory was not set or is not valid.");
                }
            }
        }

        private void InitCharacters()
        {
            if(fileWatcher.WasFileModified(fileIO.PathInGameDir(CMS_PATH)) || cmsFile == null)
            {
                cmsFile = (CMS_File)GetParsedFileFromGame(CMS_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMS_PATH));
            }

            if(fileWatcher.WasFileModified(fileIO.PathInGameDir(ERS_PATH)) || ersFile == null)
            {
                ersFile = (ERS_File)GetParsedFileFromGame(ERS_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(ERS_PATH));
            }

            if(fileWatcher.WasFileModified(fileIO.PathInGameDir(CSO_PATH)) || csoFile == null)
            {
                csoFile = (CSO_File)GetParsedFileFromGame(CSO_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CSO_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(PSC_PATH)) || pscFile == null)
            {
                pscFile = (PSC_File)GetParsedFileFromGame(PSC_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(PSC_PATH));
            }

            LoadMsgFiles(ref charaNameMsgFile, CHARACTER_MSG_PATH);

        }

        private void InitSkills()
        {
            if(fileWatcher.WasFileModified(fileIO.PathInGameDir(CUS_PATH)) || cusFile == null)
            {
                cusFile = (CUS_File)GetParsedFileFromGame(CUS_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CUS_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(CMS_PATH)) || cmsFile == null)
            {
                cmsFile = (CMS_File)GetParsedFileFromGame(CMS_PATH); 
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMS_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(SKILL_IDB_PATH)) || skillIdbFile == null)
            {
                skillIdbFile = (IDB_File)GetParsedFileFromGame(SKILL_IDB_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(SKILL_IDB_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(PUP_PATH)) || pupFile == null)
            {
                pupFile = (PUP_File)GetParsedFileFromGame(PUP_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(PUP_PATH));
            }

            LoadMsgFiles(ref spaSkillNameMsgFile, SUPER_SKILL_MSG_PATH);
            LoadMsgFiles(ref spaSkillDescMsgFile, SUPER_SKILL_DESC_MSG_PATH);
            LoadMsgFiles(ref ultSkillNameMsgFile, ULT_SKILL_MSG_PATH);
            LoadMsgFiles(ref ultSkillDescMsgFile, ULT_SKILL_DESC_MSG_PATH);
            LoadMsgFiles(ref evaSkillNameMsgFile, EVASIVE_SKILL_MSG_PATH);
            LoadMsgFiles(ref evaSkillDescMsgFile, EVASIVE_SKILL_DESC_MSG_PATH);
            LoadMsgFiles(ref metSkillNameMsgFile, AWOKEN_SKILL_MSG_PATH);
            LoadMsgFiles(ref metSkillDescMsgFile, AWOKEN_SKILL_DESC_MSG_PATH);
            LoadMsgFiles(ref btlHudMsgFile, BTLHUD_MSG_PATH);
        }

        private void InitCmn()
        {
            if(fileWatcher.WasFileModified(fileIO.PathInGameDir(CMN_BAC_PATH)) || CmnBac == null)
            {
                CmnBac = (BAC_File)GetParsedFileFromGame(CMN_BAC_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMN_BAC_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(CMN_BDM_PATH)) || CmnBdm == null)
            {
                CmnBdm = (BDM_File)GetParsedFileFromGame(CMN_BDM_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMN_BDM_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(CMN_EAN_PATH)) || CmnEan == null)
            {
                CmnEan = (EAN_File)GetParsedFileFromGame(CMN_EAN_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMN_EAN_PATH));
            }

            if (fileWatcher.WasFileModified(fileIO.PathInGameDir(CMN_CAM_EAN_PATH)) || CmnCamEan == null)
            {
                CmnCamEan = (EAN_File)GetParsedFileFromGame(CMN_CAM_EAN_PATH);
                fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(CMN_CAM_EAN_PATH));
            }

        }

        private void LoadMsgFiles(ref MSG_File[] msgFiles, string path)
        {
            if (msgFiles == null)
                msgFiles = new MSG_File[(int)Language.NumLanguages];

            for (int i = 0; i < (int)Language.NumLanguages; i++)
            {
                string msgPath = path + LanguageSuffix[i];

                if (fileWatcher.WasFileModified(fileIO.PathInGameDir(msgPath)) || msgFiles[i] == null)
                {
                    msgFiles[i] = (MSG_File)GetParsedFileFromGame(msgPath);
                    fileWatcher.FileLoadedOrSaved(fileIO.PathInGameDir(msgPath));
                }
            }

            MSG_File.SynchronizeMsgFiles(msgFiles);
        }

        /// <summary>
        /// Reload skill related files if needed (such as if a file was modified by an external program)
        /// </summary>
        public void RefreshSkills()
        {
            loadSkills = true;
            InitSkills();
        }

        /// <summary>
        /// Reload character related files if needed (such as if a file was modified by an external program)
        /// </summary>
        public void RefreshCharacters()
        {
            loadCharacters = true;
            InitCharacters();
        }

        private bool ShouldLoadFileIO()
        {
            if (fileIO == null) return true;
            return fileIO.GameDir != GameDir;
        }
        #endregion

        #region Skill
        public Xv2Skill GetSkill(CUS_File.SkillType skillType, int id1, bool loadFiles = true)
        {
            if (!loadSkills) throw new InvalidOperationException("Xenoverse2.GetSkill: Cannot get skill as skills have not been loaded.");

            var cusEntry = GetSkillCusEntry(skillType, id1);
            if (cusEntry == null) throw new InvalidOperationException($"Xenoverse2.GetSkill: Skill was not found in the system (ID: {id1}, Type: {skillType}).");
            
            var skill = new Xv2Skill()
            {
                skillType = skillType,
                BtlHud = GetAwokenStageNames(cusEntry.ID2, cusEntry.NumTransformations),
                CusEntry = cusEntry,
                Description = GetSkillDescs(skillType, cusEntry.ID2),
                Name = GetSkillNames(skillType, cusEntry.ID2),
                IdbEntry = GetSkillIdbEntry(skillType, cusEntry.ID2),
                PupEntries = new ObservableCollection<PUP_Entry>(pupFile.GetSequence(cusEntry.PUP, cusEntry.NumTransformations)),
                Files = GetSkillFiles(cusEntry, skillType, loadFiles)
            };

            skill.CreateDefaultFiles();

            return skill;
        }

        public Xv2MoveFiles GetSkillFiles(Skill cusEntry, CUS_File.SkillType skillType, bool loadSkillFiles)
        {
            string skillDir = GetSkillDir(skillType);
            string folderName = GetSkillFolderName(cusEntry);
            Xv2MoveFiles moveFiles = new Xv2MoveFiles();
            
            //BAC
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.BAC))
            {
                moveFiles.BacPath = String.Format("{0}/{1}/{1}.bac", skillDir, folderName);

                if(loadSkillFiles)
                    moveFiles.BacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(moveFiles.BacPath), fileIO.PathInGameDir(moveFiles.BacPath), false, null, false, MoveFileTypes.BAC);
            }

            //BCM
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.BCM))
            {
                moveFiles.BcmPath = String.Format("{0}/{1}/{1}_PLAYER.bcm", skillDir, folderName);

                if(loadSkillFiles)
                    moveFiles.BcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(moveFiles.BcmPath), fileIO.PathInGameDir(moveFiles.BcmPath), false, null, false, MoveFileTypes.BCM);
            }

            //BDM
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.Bdm))
            {
                moveFiles.BdmPath = String.Format("{0}/{1}/{1}_PLAYER.bdm", skillDir, folderName);

                if(loadSkillFiles)
                    moveFiles.BdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(moveFiles.BdmPath), fileIO.PathInGameDir(moveFiles.BdmPath), false, null, false, MoveFileTypes.BDM); 
            }

            //BSA + shot.BDM
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.BsaAndShotBdm))
            {
                moveFiles.ShotBdmPath = String.Format("{0}/{1}/{1}_PLAYER.shot.bdm", skillDir, folderName);
                moveFiles.BsaPath = String.Format("{0}/{1}/{1}.bsa", skillDir, folderName);

                if (loadSkillFiles)
                {
                    moveFiles.ShotBdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(moveFiles.ShotBdmPath), fileIO.PathInGameDir(moveFiles.ShotBdmPath), false, null, false, MoveFileTypes.SHOT_BDM);
                    moveFiles.BsaFile = new Xv2File<BSA_File>((BSA_File)GetParsedFileFromGame(moveFiles.BsaPath), fileIO.PathInGameDir(moveFiles.BsaPath), false, null, false, MoveFileTypes.BSA);
                }
            }

            //BAS
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.Bas))
            {
                moveFiles.BasPath = String.Format("{0}/{1}/{1}.bas", skillDir, folderName);

                if(loadSkillFiles)
                    moveFiles.BasFile = new Xv2File<BAS_File>((BAS_File)GetParsedFileFromGame(moveFiles.BasPath), fileIO.PathInGameDir(moveFiles.BasPath), false, null, false, MoveFileTypes.BAS);
            }

            //EEPK
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.Eepk))
            {
                if (!cusEntry.HasEepkPath)
                {
                    moveFiles.EepkPath = String.Format("{0}/{1}/{1}.eepk", skillDir, folderName);

                    if(loadSkillFiles)
                        moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(moveFiles.EepkPath), fileIO.PathInGameDir(moveFiles.EepkPath), false, null, false, MoveFileTypes.EEPK);
                }
                else
                {
                    //This skill uses another skills EEPK, so we dont have to calculate its folder name
                    moveFiles.EepkPath = String.Format("skill/{0}/{1}.eepk", cusEntry.EepkPath, Path.GetFileName(cusEntry.EepkPath));

                    if(loadSkillFiles)
                        moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(moveFiles.EepkPath), fileIO.PathInGameDir(moveFiles.EepkPath), true, null, false, MoveFileTypes.EEPK);
                }
            }

            //SE ACB
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.CharaSE))
            {
                if (!cusEntry.HasSeAcbPath)
                {
                    moveFiles.SeAcbPath = string.Format(@"sound/SE/Battle/Skill/CAR_BTL_{2}{1}_{0}_SE.acb", cusEntry.ShortName, cusEntry.ID2.ToString("D3"), GetAcbSkillTypeLetter(skillType));

                    if (loadSkillFiles)
                        moveFiles.AddSeAcbFile((ACB_Wrapper)GetParsedFileFromGame(moveFiles.SeAcbPath), -1, fileIO.PathInGameDir(moveFiles.SeAcbPath));
                }
                else
                {
                    moveFiles.SeAcbPath = string.Format(@"sound/SE/Battle/Skill/{0}.acb", cusEntry.SePath);

                    if(loadSkillFiles)
                        moveFiles.AddSeAcbFile((ACB_Wrapper)GetParsedFileFromGame(moveFiles.SeAcbPath), -1, fileIO.PathInGameDir(moveFiles.SeAcbPath), true);
                }
            }

            //VOX ACB
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.CharaVOX))
            {
                //Japanese
                string[] files = fileIO.GetFilesInDirectory("sound/VOX/Battle/Skill", "acb");
                string name = (!cusEntry.HasVoxAcbPath) ? string.Format(@"CAR_BTL_{2}{1}_{0}_", cusEntry.ShortName, cusEntry.ID2.ToString("D3"), GetAcbSkillTypeLetter(skillType)) : cusEntry.VoxPath;
                
                foreach(var file in files.Where(f => f.Contains(name) && f.Contains("_VOX.acb")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = split[(split.Length - 2 > 0) ? split.Length - 2 : 0];
                    moveFiles.VoxAcbPath.Add(file);

                    if(loadSkillFiles)
                        moveFiles.AddVoxAcbFile((ACB_Wrapper)GetParsedFileFromGame(file), charaSuffix, false, fileIO.PathInGameDir(file), cusEntry.HasVoxAcbPath, false);
                }

                //English
                files = fileIO.GetFilesInDirectory("sound/VOX/Battle/Skill/en", "acb");

                foreach (var file in files.Where(f => f.Contains(name) && f.Contains("_VOX.acb")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = split[(split.Length - 2 > 0) ? split.Length - 2 : 0];
                    moveFiles.VoxAcbPath.Add(file);

                    if(loadSkillFiles)
                        moveFiles.AddVoxAcbFile((ACB_Wrapper)GetParsedFileFromGame(file), charaSuffix, true, fileIO.PathInGameDir(file), cusEntry.HasVoxAcbPath, false);
                }
            }

            //EAN
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.EAN))
            {
                string name = (!cusEntry.HasEanPath) ? string.Format("{0}/{1}/{1}.ean", skillDir, folderName) : Utils.ResolveRelativePath("skill/" + cusEntry.EanPath + ".ean");
                name = Utils.SanitizePath(name);
                string[] files = fileIO.GetFilesInDirectory(Path.GetDirectoryName(name), ".ean");

                foreach (var file in files.Where(f => f.Contains(Path.GetFileNameWithoutExtension(name)) && f.Contains(".ean") && !f.Contains(".cam")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = (split.Length == 4) ? split[3].Split('.')[0] : null;
                    moveFiles.EanPaths.Add(file);

                    if(loadSkillFiles)
                        moveFiles.AddEanFile((EAN_File)GetParsedFileFromGame(file), charaSuffix, fileIO.PathInGameDir(file), cusEntry.HasEanPath, string.IsNullOrWhiteSpace(charaSuffix));
                }

                //Create default EAN if none was loaded (duplicate chara-unique one)
                if (!moveFiles.EanFile.Any(x => x.IsDefault) && loadSkillFiles)
                    moveFiles.AddEanFile(moveFiles.EanFile[0].File.Copy(), null, name);
            }

            //CAM
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.CamEan))
            {
                string nameWithoutExt = (!cusEntry.HasCamEanPath) ? string.Format("{0}/{1}/{1}", skillDir, folderName) : Utils.ResolveRelativePath("skill/" + cusEntry.CamEanPath);
                nameWithoutExt = Utils.SanitizePath(nameWithoutExt);
                string name = nameWithoutExt + ".cam.ean";
                string[] files = fileIO.GetFilesInDirectory(Path.GetDirectoryName(nameWithoutExt), ".ean");

                foreach (var file in files.Where(f => f.Contains(nameWithoutExt) && f.Contains("cam.ean")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = (split.Length == 4) ? split[3].Split('.')[0] : null;
                    moveFiles.CamPaths.Add(file);

                    if(loadSkillFiles)
                        moveFiles.AddCamEanFile((EAN_File)GetParsedFileFromGame(file), charaSuffix, fileIO.PathInGameDir(file), cusEntry.HasCamEanPath, string.IsNullOrWhiteSpace(charaSuffix));
                }

                //Create default CAM.EAN if none was loaded (duplicate chara-unique one)
                if (!moveFiles.CamEanFile.Any(x => x.IsDefault) && loadSkillFiles)
                    moveFiles.AddCamEanFile(moveFiles.CamEanFile[0].File.Copy(), null, name);
            }
            
            //AFTER BAC
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.AfterBac))
            {
                if (!cusEntry.HasAfterBacPath)
                {
                    moveFiles.AfterBacPath = String.Format("{0}/{1}/{1}_AFTER.bac", skillDir, folderName);

                    if(loadSkillFiles)
                        moveFiles.AfterBacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(moveFiles.AfterBacPath), fileIO.PathInGameDir(moveFiles.AfterBacPath), false, null, false, MoveFileTypes.BAC);
                }
                else
                {
                    moveFiles.AfterBacPath = String.Format("skill/{0}/{1}.bac", cusEntry.AfterBacPath, Path.GetFileName(cusEntry.AfterBacPath));

                    if(loadSkillFiles)
                        moveFiles.AfterBacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(moveFiles.AfterBacPath), fileIO.PathInGameDir(moveFiles.AfterBacPath), true, null, false, MoveFileTypes.BAC);
                }
            }

            //AFTER BCM
            if (cusEntry.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.AfterBcm))
            {
                if (!cusEntry.HasAfterBacPath)
                {
                    moveFiles.AfterBcmPath = String.Format("{0}/{1}/{1}_AFTER_PLAYER.bcm", skillDir, folderName);

                    if(loadSkillFiles)
                        moveFiles.AfterBcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(moveFiles.AfterBcmPath), fileIO.PathInGameDir(moveFiles.AfterBcmPath), false, null, false, MoveFileTypes.BCM);
                }
                else
                {
                    moveFiles.AfterBcmPath = String.Format("skill/{0}/{1}.bcm", cusEntry.AfterBcmPath, Path.GetFileName(cusEntry.AfterBcmPath));

                    if(loadSkillFiles)
                        moveFiles.AfterBcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(moveFiles.AfterBcmPath), fileIO.PathInGameDir(moveFiles.AfterBcmPath), true, null, false, MoveFileTypes.BCM);
                }
            }



            return moveFiles;
        }

        public Skill GetSkillCusEntry(CUS_File.SkillType skillType, int id1)
        {
            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    return cusFile.SuperSkills.FirstOrDefault(s => s.ID1 == id1);
                case CUS_File.SkillType.Ultimate:
                    return cusFile.UltimateSkills.FirstOrDefault(s => s.ID1 == id1);
                case CUS_File.SkillType.Evasive:
                    return cusFile.EvasiveSkills.FirstOrDefault(s => s.ID1 == id1);
                case CUS_File.SkillType.Blast:
                    return cusFile.BlastSkills.FirstOrDefault(s => s.ID1 == id1);
                case CUS_File.SkillType.Awoken:
                    return cusFile.AwokenSkills.FirstOrDefault(s => s.ID1 == id1);
                default:
                    throw new InvalidDataException("GetSkill: unknown skilltype = " + skillType);
            }
        }

        public IDB_Entry GetSkillIdbEntry(CUS_File.SkillType skillType, int id2)
        {
            return skillIdbFile.Entries.FirstOrDefault(i => i.ID == id2 && i.Type == (IDB_Type)skillType);
        }
        
        public List<Xv2Item> GetSkillList(CUS_File.SkillType skillType)
        {
            List<Xv2Item> items = new List<Xv2Item>();

            foreach(var skill in cusFile.GetSkills(skillType))
            {
                if(skillType == CUS_File.SkillType.Blast)
                {
                    items.Add(new Xv2Item(skill.ID1, skill.ShortName));
                }
                else
                {
                    items.Add(new Xv2Item(skill.ID1, GetSkillName(skillType, skill.ID2, PreferedLanguage)));
                }
            }

            return items;
        }

        //Helpers
        public string GetSkillDir(CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    return "skill/SPA";
                case CUS_File.SkillType.Ultimate:
                    return "skill/ULT";
                case CUS_File.SkillType.Evasive:
                    return "skill/ESC";
                case CUS_File.SkillType.Blast:
                    return "skill/BLT";
                case CUS_File.SkillType.Awoken:
                    return "skill/MET";
                default:
                    return null;
            }
        }

        public string GetSkillFolderName(Skill cusEntry)
        {
            int cmsId = (int)Math.Floor(cusEntry.ID2 / 10f);
            string charaShortName = cmsFile.GetEntry(cmsId.ToString()).Str_04;

            //If chara ID belongs to a CAC, the skill is tagged as CMN instead.
            if (cmsId >= 100 && cmsId < 109)
                charaShortName = "CMN";

            return String.Format("{0}_{1}_{2}", cusEntry.ID2.ToString("D3"), charaShortName, cusEntry.ShortName);
        }

        public string GetSkillName(CUS_File.SkillType skillType, int id2, Language language)
        {
            MSG_File[] msgFiles = GetSkillNameMsgFile(skillType);
            return msgFiles[(int)language].GetSkillName(id2, skillType);
        }

        public string[] GetSkillDescs(CUS_File.SkillType skillType, int id2)
        {
            if (skillType == CUS_File.SkillType.Blast) return new string[(int)Language.NumLanguages];
            string[] descs = new string[(int)Language.NumLanguages];
            MSG_File[] msgFiles = GetSkillDescMsgFile(skillType);

            for (int i = 0; i < (int)Language.NumLanguages; i++)
            {
                descs[i] = msgFiles[i].GetSkillDesc(id2, skillType);
            }

            return descs;
        }

        public string[] GetSkillNames(CUS_File.SkillType skillType, int id2)
        {
            string[] names = new string[(int)Language.NumLanguages];

            if(skillType == CUS_File.SkillType.Blast)
            {
                for(int i = 0; i < names.Length; i++)
                {
                    names[i] = cusFile.BlastSkills.FirstOrDefault(x => x.ID2 == id2).ShortName;
                }

                return names;
            }

            MSG_File[] msgFiles = GetSkillNameMsgFile(skillType);

            for (int i = 0; i < (int)Language.NumLanguages; i++)
            {
                names[i] = msgFiles[i].GetSkillName(id2, skillType);
            }

            return names;
        }

        public ObservableCollection<string[]> GetAwokenStageNames(int id2, int numStages)
        {
            if (numStages > 3) numStages = 1;
            ObservableCollection<string[]> stages = new ObservableCollection<string[]>();

            for(int a = 0; a < numStages; a++)
            {
                string[] names = new string[(int)Language.NumLanguages];

                for (int i = 0; i < (int)Language.NumLanguages; i++)
                {
                    names[i] = btlHudMsgFile[i].GetAwokenStageName(id2, a);
                }

                stages.Add(names);
            }

            return stages;
        }

        private MSG_File[] GetSkillNameMsgFile(CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    return spaSkillNameMsgFile;
                case CUS_File.SkillType.Ultimate:
                    return ultSkillNameMsgFile;
                case CUS_File.SkillType.Evasive:
                    return evaSkillNameMsgFile;
                case CUS_File.SkillType.Awoken:
                    return metSkillNameMsgFile;
                default:
                    throw new InvalidDataException("Unknown skilltype = " + skillType);
            }
        }

        private MSG_File[] GetSkillDescMsgFile(CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    return spaSkillDescMsgFile;
                case CUS_File.SkillType.Ultimate:
                    return ultSkillDescMsgFile;
                case CUS_File.SkillType.Evasive:
                    return evaSkillDescMsgFile;
                case CUS_File.SkillType.Awoken:
                    return metSkillDescMsgFile;
                default:
                    throw new InvalidDataException("Unknown skilltype = " + skillType);
            }
        }

        public string GetAcbSkillTypeLetter(CUS_File.SkillType skillType)
        {
            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    return "S";
                case CUS_File.SkillType.Ultimate:
                    return "U";
                case CUS_File.SkillType.Evasive:
                    return "E";
                case CUS_File.SkillType.Blast:
                    return "B";
                case CUS_File.SkillType.Awoken:
                    return "M";
            }

            return null;
        }


        #endregion

        #region Character
        public Xv2Character GetCharacter(int cmsId, bool loadFiles = true)
        {
            if (!loadCharacters) throw new InvalidOperationException("Xenoverse2.GetCharacter: Cannot get character as characters have not been loaded.");

            var cmsEntry = cmsFile.CMS_Entries.FirstOrDefault(x => x.ID == cmsId);
            if (cmsEntry == null) throw new InvalidOperationException($"Xenoverse2.GetCharacter: Character was not found in the system (ID: {cmsId}).");
            var names = GetCharacterName(cmsEntry.ShortName);
            var csoEntries = csoFile.CsoEntries.Where(x => x.CharaID == cmsId).ToList();
            var ersEntry = ersFile.GetEntry(2, cmsId);

            List<string> loadedFiles = new List<string>();

            if (loadFiles)
            {
                //Load bcs
                string bcsPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bcs", cmsEntry.ShortName, cmsEntry.BcsPath));
                BCS_File bcsFile = (BCS_File)GetParsedFileFromGame(bcsPath);

                //Load bai file
                string baiPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bai", cmsEntry.ShortName, cmsEntry.BaiPath));
                BAI_File baiFile = (BAI_File)GetParsedFileFromGame(baiPath);

                //Costumes
                List<Xv2File<AMK_File>> amkFiles = new List<Xv2File<AMK_File>>();
                ObservableCollection<Xv2CharaCostume> costumes = new ObservableCollection<Xv2CharaCostume>();

                foreach(var csoEntry in csoEntries)
                {
                    Xv2CharaCostume costume = Xv2CharaCostume.GetAndAddCostume(costumes, (int)csoEntry.Costume);

                    costume.CsoSkills = csoEntry.SkillCharaCode;

                    //AMK
                    string amkPath = Utils.ResolveRelativePath(string.Format("chara/{0}.amk", csoEntry.AmkPath));

                    if (!string.IsNullOrWhiteSpace(csoEntry.AmkPath) && !loadedFiles.Contains(amkPath))
                    {
                        AMK_File amkFile = (AMK_File)GetParsedFileFromGame(amkPath);
                        amkFiles.Add(new Xv2File<AMK_File>(amkFile, GetAbsolutePath(amkPath), !Utils.CompareSplitString(csoEntry.AmkPath, '/', 0, cmsEntry.ShortName), null, false, 0, csoEntry.Costume.ToString()));
                        loadedFiles.Add(amkPath);
                    }
                    else
                    {
                        Xv2File<AMK_File>.AddCostume(amkFiles, GetAbsolutePath(amkPath), (int)csoEntry.Costume, false);
                    }

                    //ACBs loaded in GetCharacterMoveFiles()
                }


                Xv2Character chara = new Xv2Character()
                {
                    CmsEntry = cmsEntry,
                    CsoEntry = csoEntries,
                    ErsEntry = ersEntry,
                    Name = names,
                    BcsFile = new Xv2File<BCS_File>(bcsFile, fileIO.PathInGameDir(bcsPath), false, null, false),
                    AmkFile = amkFiles,
                    BaiFile = new Xv2File<BAI_File>(baiFile, fileIO.PathInGameDir(baiPath), !cmsEntry.IsSelfReference(cmsEntry.BaiPath)),
                    MovesetFiles = GetCharacterMoveFiles(cmsEntry, ersEntry, csoEntries, loadFiles),
                    Costumes = costumes
                };

                chara.CreateDefaultFiles();

                return chara;
            }
            else
            {
                return new Xv2Character()
                {
                    CmsEntry = cmsEntry,
                    CsoEntry = csoEntries,
                    ErsEntry = ersEntry,
                    Name = names,
                    MovesetFiles = GetCharacterMoveFiles(cmsEntry, ersEntry, csoEntries, loadFiles)
                };
            }
        }

        public List<Xv2Item> GetCharacterList()
        {
            if (!loadCharacters) throw new InvalidOperationException("Xenoverse2.GetCharacterList: characters are not loaded.");
            List<Xv2Item> items = new List<Xv2Item>();

            foreach (var character in cmsFile.CMS_Entries)
            {
                items.Add(new Xv2Item(character.ID, charaNameMsgFile[(int)PreferedLanguage].GetCharacterName(character.ShortName)));
            }

            return items;
        }

        public List<Xv2Item> GetPartSetList(int cmsId)
        {
            var cmsEntry = cmsFile.CMS_Entries.FirstOrDefault(x => x.ID == cmsId);
            if (cmsEntry == null) throw new InvalidOperationException($"Xenoverse2.GetPartSetList: Character was not found in the system (ID: {cmsId}).");

            string bcsPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bcs", cmsEntry.ShortName, cmsEntry.BcsPath));
            BCS_File bcsFile = (BCS_File)GetParsedFileFromGame(bcsPath);

            List<Xv2Item> items = new List<Xv2Item>();

            foreach (var partSet in bcsFile.PartSets)
                items.Add(new Xv2Item(partSet.ID, partSet.ID.ToString()));

            return items;
        }

        public BCS_File GetBcsFile(int cmsId)
        {
            var cmsEntry = cmsFile.CMS_Entries.FirstOrDefault(x => x.ID == cmsId);
            if (cmsEntry == null) throw new InvalidOperationException($"Xenoverse2.GetBcsFile: Character was not found in the system (ID: {cmsId}).");

            string bcsPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bcs", cmsEntry.ShortName, cmsEntry.BcsPath));
            return (BCS_File)GetParsedFileFromGame(bcsPath);
        }

        public string[] GetCharacterName(string shortName)
        {
            string[] names = new string[(int)Language.NumLanguages];

            for(int i = 0; i < names.Length; i++)
            {
                names[i] = charaNameMsgFile[i].GetCharacterName(shortName);
                if (string.IsNullOrWhiteSpace(names[i])) names[i] = string.Format("Unknown Character - {0}", shortName);
            }

            return names;
        }
        
        private Xv2MoveFiles GetCharacterMoveFiles(CMS_Entry cmsEntry, ERS_MainTableEntry ersEntry, IList<CSO_Entry> csoEntries, bool loadFiles)
        {
            List<string> loadedFiles = new List<string>();
            Xv2MoveFiles moveFiles = new Xv2MoveFiles();

            //Clear defaults out
            moveFiles.EanFile.Clear();
            moveFiles.CamEanFile.Clear();

            //BAC
            moveFiles.BacPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bac", cmsEntry.ShortName, cmsEntry.BacPath));

            if(loadFiles)
                moveFiles.BacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(moveFiles.BacPath), fileIO.PathInGameDir(moveFiles.BacPath), !cmsEntry.IsSelfReference(cmsEntry.BacPath), null, false, MoveFileTypes.BAC);

            //BCM
            moveFiles.BcmPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bcm", cmsEntry.ShortName, cmsEntry.BcmPath));

            if(loadFiles)
                moveFiles.BcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(moveFiles.BcmPath), fileIO.PathInGameDir(moveFiles.BcmPath), !cmsEntry.IsSelfReference(cmsEntry.BcmPath), null, false, MoveFileTypes.BCM);

            //EAN
            string eanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.ean", cmsEntry.ShortName, cmsEntry.EanPath));
            moveFiles.EanPaths.Add(eanPath);
            moveFiles.EanFile.Clear();

            if(loadFiles)
                moveFiles.EanFile.Add(new Xv2File<EAN_File>((EAN_File)GetParsedFileFromGame(eanPath), fileIO.PathInGameDir(eanPath), !cmsEntry.IsSelfReference(cmsEntry.EanPath), null, false, MoveFileTypes.EAN));

            //CAM
            string camEanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.cam.ean", cmsEntry.ShortName, cmsEntry.CamEanPath));
            moveFiles.CamPaths.Add(camEanPath);
            moveFiles.CamEanFile.Clear();

            if(loadFiles)
                moveFiles.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)GetParsedFileFromGame(camEanPath), fileIO.PathInGameDir(camEanPath), !cmsEntry.IsSelfReference(cmsEntry.CamEanPath), null, false, MoveFileTypes.CAM_EAN));

            //BDM
            if(cmsEntry.BdmPath != "NULL")
            {
                moveFiles.BdmPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bdm", cmsEntry.ShortName, cmsEntry.BdmPath));

                if(loadFiles)
                    moveFiles.BdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(moveFiles.BdmPath), fileIO.PathInGameDir(moveFiles.BdmPath), !cmsEntry.IsSelfReference(cmsEntry.BdmPath), null, false, MoveFileTypes.BDM);

            }
            //EEPK
            if (ersEntry != null)
            {
                bool borrowed = !Utils.CompareSplitString(ersEntry.FILE_PATH, '/', 1, cmsEntry.ShortName);
                moveFiles.EepkPath = string.Format("vfx/{0}", ersEntry.FILE_PATH);

                if(loadFiles)
                    moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(moveFiles.EepkPath), fileIO.PathInGameDir(moveFiles.EepkPath), borrowed, null, false, MoveFileTypes.EEPK);
            }

            //ACBs
            if(csoEntries.Count > 0)
            {

                foreach (var csoEntry in csoEntries)
                {
                    moveFiles.SeAcbPath = $"sound/SE/Battle/Chara/{csoEntry.SePath}.acb";
                    string acbPath = $"sound/VOX/Battle/Chara/{csoEntry.VoxPath}.acb";
                    string acbEngPath = $"sound/VOX/Battle/Chara/en/{csoEntry.VoxPath}.acb";

                    //SE
                    if (csoEntry.HasSePath && !loadedFiles.Contains(moveFiles.SeAcbPath))
                    {
                        bool borrowed = !Utils.CompareSplitString(csoEntry.SePath, '_', 2, cmsEntry.ShortName);

                        if(loadFiles)
                            moveFiles.SeAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(moveFiles.SeAcbPath), fileIO.PathInGameDir(moveFiles.SeAcbPath), borrowed, null, false, MoveFileTypes.SE_ACB, csoEntry.Costume.ToString()));

                        loadedFiles.Add(moveFiles.SeAcbPath);
                    }
                    else
                    {
                        Xv2File<ACB_Wrapper>.AddCostume(moveFiles.SeAcbFile, GetAbsolutePath(moveFiles.SeAcbPath), (int)csoEntry.Costume, false);
                    }

                    //VOX, Jap
                    if (csoEntry.HasVoxPath && !loadedFiles.Contains(acbPath))
                    {
                        bool borrowed = !Utils.CompareSplitString(csoEntry.VoxPath, '_', 2, cmsEntry.ShortName);

                        moveFiles.VoxAcbPath.Add(acbPath);

                        if (loadFiles)
                            moveFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(acbPath), fileIO.PathInGameDir(acbPath), borrowed, null, false, MoveFileTypes.VOX_ACB, csoEntry.Costume.ToString(), csoEntry.Costume == 0));

                        loadedFiles.Add(acbPath);
                    }
                    else
                    {
                        Xv2File<ACB_Wrapper>.AddCostume(moveFiles.VoxAcbFile, GetAbsolutePath(acbPath), (int)csoEntry.Costume, false);
                    }

                    //VOX, Eng
                    if (csoEntry.HasVoxPath && !loadedFiles.Contains(acbEngPath))
                    {
                        bool borrowed = !Utils.CompareSplitString(csoEntry.VoxPath, '_', 2, cmsEntry.ShortName);
                        moveFiles.VoxAcbPath.Add(acbEngPath);

                        if (loadFiles)
                            moveFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(acbEngPath), fileIO.PathInGameDir(acbEngPath), borrowed, null, true, MoveFileTypes.VOX_ACB, csoEntry.Costume.ToString(), csoEntry.Costume == 0));

                        loadedFiles.Add(acbEngPath);
                    }
                    else
                    {
                        Xv2File<ACB_Wrapper>.AddCostume(moveFiles.VoxAcbFile, GetAbsolutePath(acbEngPath), (int)csoEntry.Costume, true);
                    }
                }
            }

            //Load fce ean
            string fceEanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.fce.ean", cmsEntry.ShortName, cmsEntry.FceEanPath));
            moveFiles.EanPaths.Add(fceEanPath);
            moveFiles.EanFile.Add(new Xv2File<EAN_File>((EAN_File)GetParsedFileFromGame(fceEanPath), fileIO.PathInGameDir(fceEanPath), !cmsEntry.IsSelfReference(cmsEntry.FceEanPath), null, false, MoveFileTypes.FCE_EAN));

            return moveFiles;
        }

        #endregion

        #region Save/Install
        public void SaveSkills()
        {
            if (!loadSkills) throw new InvalidOperationException("Xenoverse2.SaveSkills: loadSkills was null, operation not valid.");

            SaveFileToGame(CUS_PATH, cusFile);
            SaveFileToGame(SKILL_IDB_PATH, skillIdbFile);
            SaveFileToGame(PUP_PATH, pupFile);

            SaveMsgFilesToGame(SUPER_SKILL_MSG_PATH, spaSkillNameMsgFile);
            SaveMsgFilesToGame(SUPER_SKILL_DESC_MSG_PATH, spaSkillDescMsgFile);

            SaveMsgFilesToGame(ULT_SKILL_MSG_PATH, ultSkillNameMsgFile);
            SaveMsgFilesToGame(ULT_SKILL_DESC_MSG_PATH, ultSkillDescMsgFile);

            SaveMsgFilesToGame(EVASIVE_SKILL_MSG_PATH, evaSkillNameMsgFile);
            SaveMsgFilesToGame(EVASIVE_SKILL_DESC_MSG_PATH, evaSkillDescMsgFile);

            SaveMsgFilesToGame(AWOKEN_SKILL_MSG_PATH, metSkillNameMsgFile);
            SaveMsgFilesToGame(AWOKEN_SKILL_DESC_MSG_PATH, metSkillDescMsgFile);
            SaveMsgFilesToGame(BTLHUD_MSG_PATH, btlHudMsgFile);


        }

        /// <summary>
        /// Save a Xv2Skill instance to the game.
        /// </summary>
        public void SaveSkill(Xv2Skill skill)
        {
            //Refresh files
            RefreshSkills();

            //Cus Entry
            skill.CusEntry.ID1 = (ushort)CUS_File.ConvertToID1(skill.CusEntry.ID2, skill.skillType);
            skill.CusEntry.PUP = (skill.PupEntries?.Count > 0) ? InstallPupEntries(skill.PupEntries, skill.CusEntry.ID1) : ushort.MaxValue;
            InstallSkillCusEntry(skill.CusEntry, skill.skillType);

            //IDB Entry
            skill.IdbEntry.ID = skill.CusEntry.ID2;
            skill.IdbEntry.Type = (IDB_Type)skill.skillType;
            InstallSkillIdbEntry(skill.IdbEntry);

            //Skill files
            skill.CalculateSkillFilePaths();
            skill.UpdateCusFlags();
            skill.SaveMoveFiles();

            //Name and Desc
            InstallSkillName(skill.Name, skill.CusEntry.ID2, skill.skillType);
            InstallSkillDesc(skill.Description, skill.CusEntry.ID2, skill.skillType);

            if (skill.skillType == CUS_File.SkillType.Awoken && skill.BtlHud != null)
                InstallSkillAwokenName(skill.BtlHud, skill.CusEntry.ID2);

            //Save files
            SaveSkills();
        }

        public void SaveCharacters()
        {
            SaveFileToGame(CMS_PATH, cmsFile);
            SaveFileToGame(ERS_PATH, ersFile);
            SaveFileToGame(CSO_PATH, csoFile);
            SaveFileToGame(PSC_PATH, pscFile);
            SaveMsgFilesToGame(CHARACTER_MSG_PATH, charaNameMsgFile);
        }

        public void SaveCharacter(Xv2Character chara, bool isMoveset)
        {
            //Refresh files
            RefreshCharacters();

            //Character files
            chara.CalculateFilePaths();
            InstallErsCharacterEntry(chara.CmsEntry.ShortName, chara.CmsEntry.ID, !chara.MovesetFiles.EepkFile.File.IsNull(), chara.MovesetFiles.EepkFile.Borrowed);
            InstallCmsEntry(chara.CmsEntry);

            //Costumes
            List<CSO_Entry> csoEntries = new List<CSO_Entry>();

            foreach (var costume in chara.Costumes)
            {
                Xv2File<ACB_Wrapper> se = chara.MovesetFiles.SeAcbFile.FirstOrDefault(x => x.HasCostume(costume.CostumeId));
                Xv2File<ACB_Wrapper> vox = chara.MovesetFiles.VoxAcbFile.FirstOrDefault(x => x.HasCostume(costume.CostumeId));
                Xv2File<AMK_File> amk = chara.AmkFile.FirstOrDefault(x => x.HasCostume(costume.CostumeId));

                //CSO Entry
                CSO_Entry csoEntry = new CSO_Entry();

                csoEntry.CharaID = chara.CmsEntry.ID;
                csoEntry.Costume = (uint)costume.CostumeId;
                csoEntry.SePath = (se != null) ? Path.GetFileNameWithoutExtension(se.Path) : null;
                csoEntry.VoxPath = (vox != null) ? Path.GetFileNameWithoutExtension(vox.Path) : null;
                csoEntry.AmkPath = (amk != null) ? string.Format("{0}/{1}", Path.GetFileName(Path.GetDirectoryName(amk.Path)), Path.GetFileNameWithoutExtension(amk.Path)) : null;
                csoEntry.SkillCharaCode = costume.CsoSkills;

                csoEntries.Add(csoEntry);
            }

            if (!isMoveset)
            {
                //Name
                InstallCharacterName(chara.Name, chara.CmsEntry.ShortName);
            }

            //Install CSO Entries
            csoFile.CsoEntries.RemoveAll(x => x.CharaID == chara.CmsEntry.ID);
            csoFile.CsoEntries.AddRange(csoEntries);

            //Save
            chara.SaveFiles();
            SaveCharacters();
        }

        //Skill
        public ushort InstallPupEntries(IList<PUP_Entry> entries, int skillId1 = -1) 
        {
            if (entries?.Count == 0) return ushort.MaxValue;

            //First check if sequence already exists, and return the ID if it does
            int id = pupFile.CheckForSequence(entries);

            //Check if old IDs are used by any other skill, and reuse them if not (If prev check failed)
            if(entries[0].ID != -1 && skillId1 != -1 && id == -1)
            {
                if(!cusFile.IsPupEntryUsed(entries[0].ID, entries.Count, skillId1))
                {
                    id = entries[0].ID;
                }
            }

            if(id == -1)
            {
                id = pupFile.GetNewPupId(entries.Count);
            }

            if (id != -1)
            {
                PUP_File.SetPupId(entries, id);
                pupFile.PupEntries.AddRange(entries);
                return (ushort)id;
            }
            else
            {
                throw new InvalidOperationException("Xenoverse2:InstallPupEntries: pupId was -1, could not assign a new ID.");
            }

        }

        public void InstallSkillCusEntry(Skill cusEntry, CUS_File.SkillType skillType)
        {
            IList<Skill> skills = cusFile.GetSkills(skillType);

            int existingIdx = skills.IndexOf(skills.FirstOrDefault(x => x.ID2 == cusEntry.ID2));

            if(existingIdx != -1)
            {
                skills[existingIdx] = cusEntry;
            }
            else
            {
                skills.Add(cusEntry);
            }
        }

        public void InstallSkillIdbEntry(IDB_Entry idbEntry)
        {
            var existingIdx = skillIdbFile.Entries.IndexOf(skillIdbFile.Entries.FirstOrDefault(x => x.ID == idbEntry.ID && x.Type == idbEntry.Type));

            if(existingIdx != -1)
            {
                skillIdbFile.Entries[existingIdx] = idbEntry;
            }
            else
            {
                skillIdbFile.Entries.Add(idbEntry);
            }
        }

        public void InstallSkillName(string[] names, int id2, SkillType skillType)
        {
            if (skillType == SkillType.Blast) return;
            if (names.Length != (int)Language.NumLanguages) throw new InvalidDataException($"Xenoverse2.InstallSkillName: Invalid number of language entries.");

            MSG_File[] msgFiles = GetSkillNameMsgFile(skillType);

            for(int i = 0; i < msgFiles.Length; i++)
            {
                msgFiles[i].SetSkillName(names[i], id2, skillType);
            }
        }

        public void InstallSkillDesc(string[] desc, int id2, SkillType skillType)
        {
            if (skillType == SkillType.Blast) return;
            if (desc.Length != (int)Language.NumLanguages) throw new InvalidDataException($"Xenoverse2.InstallSkillDesc: Invalid number of language entries.");

            MSG_File[] msgFiles = GetSkillDescMsgFile(skillType);

            for (int i = 0; i < msgFiles.Length; i++)
            {
                msgFiles[i].SetSkillDesc(desc[i], id2, skillType);
            }
        }

        public void InstallSkillAwokenName(ObservableCollection<string[]> names, int id2)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Length != (int)Language.NumLanguages) throw new InvalidDataException($"Xenoverse2.InstallSkillAwokenName: Invalid number of language entries.");

                for (int a = 0; a < btlHudMsgFile.Length; a++)
                {
                    btlHudMsgFile[a].SetAwokenStageName(names[i][a], id2, i);
                }
            }
        }
        
        //Moveset/Character
        public void InstallErsCharacterEntry(string charaShortName, int id, bool hasEepk, bool borrowed)
        {
            var ersEntries = ersFile.GetSubentryList(2);

            var entry = ersEntries.FirstOrDefault(x => x.ID == id);
            string path = $"chara/{charaShortName}/{charaShortName}.eepk";

            if (hasEepk && !borrowed)
            {
                if (entry != null)
                {
                    entry.FILE_PATH = path;
                }
                else
                {
                    ersEntries.Add(new ERS_MainTableEntry() { FILE_PATH = path, ID = id });
                }
            }
            else if(!hasEepk && entry != null)
            {
                //Remove entry
                ersEntries.Remove(entry);
            }
        }

        public void InstallCmsEntry(CMS_Entry cmsEntry)
        {
            var existingIdx = cmsFile.CMS_Entries.IndexOf(cmsFile.CMS_Entries.FirstOrDefault(x => x.ID == cmsEntry.ID));

            if (existingIdx != -1)
            {
                cmsFile.CMS_Entries[existingIdx] = cmsEntry;
            }
            else
            {
                cmsFile.CMS_Entries.Add(cmsEntry);
            }
        }

        public void InstallCharacterName(string[] names, string shortName)
        {
            if (names.Length != (int)Language.NumLanguages) throw new InvalidDataException($"Xenoverse2.InstallCharacterName: Invalid number of language entries.");

            for (int i = 0; i < charaNameMsgFile.Length; i++)
            {
                charaNameMsgFile[i].SetCharacterName(names[i], shortName);
            }
        }

        #endregion


        public static string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            foreach(var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Games{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Games{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            return string.Empty;
        }

        public object GetParsedFileFromGame(string path, bool onlyFromCpk = false, bool raiseEx = true)
        {
            if (onlyFromCpk)
            {
                if (!fileIO.FileExistsInCpk(path))
                {
                    if (!raiseEx)
                        return null;
                    else
                        throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the cpks.", path));
                }
            }
            else if (!fileIO.FileExists(path))
            {
                if (!raiseEx)
                    return null;
                else
                    throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the game directory or cpks.", path));
            }

            switch (Path.GetExtension(path))
            {
                case ".bac":
                    return BAC_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".bcm":
                    return BCM_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".bcs":
                    return BCS_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".bdm":
                    return BDM_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx), true);
                case ".bsa":
                    return BSA_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".cms":
                    return CMS_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".cso":
                    return CSO_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".cus":
                    return CUS_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".ean":
                    return EAN_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx), true);
                case ".ers":
                    return ERS_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".idb":
                    return IDB_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".pup":
                    return PUP_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".bas":
                    return BAS_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".bai":
                    return BAI_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".amk":
                    return AMK_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".esk":
                    return ESK_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".emd":
                    return EMD_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".msg":
                    return MSG_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".psc":
                    return PSC_File.Load(GetBytesFromGameWrapper(path, onlyFromCpk, raiseEx));
                case ".eepk":
                    return EffectContainerFile.Load(path, fileIO, onlyFromCpk);
                case ".acb":
                    return new ACB_Wrapper(ACB_File.Load(GetBytesFromGameWrapper(path), fileIO.GetFileFromGame(string.Format("{0}/{1}.awb", Path.GetFileNameWithoutExtension(path), Path.GetDirectoryName(path)), false)));
                default:
                    throw new InvalidDataException(String.Format("GetParsedFileFromGame: The filetype of \"{0}\" is not supported.", path));
            }
        }

        public byte[] GetBytesFromGame(string path, bool onlyFromCpk = false, bool raiseEx = false)
        {
            if (fileIO == null && raiseEx) throw new NullReferenceException("Xenoverse2.GetBytesFromGame: fileIO is null.");
            if (fileIO == null) return null;
            return GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk);
        }

        private byte[] GetBytesFromGameWrapper(string path, bool onlyFromCpk = false, bool raiseEx = false)
        {
            var bytes = fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk);
            if (bytes == null) FileNotFoundEvent.Invoke(this, new Xv2FileNotFoundEventArgs(path));
            return bytes;
        }
    
        public Xv2FileIO GetFileIO()
        {
            return fileIO;
        }

        private void SaveFileToGame(string path, object file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GetAbsolutePath(path)));

            byte[] bytes = GetBytesFromParsedFile(path, file);
            File.WriteAllBytes(GetAbsolutePath(path), bytes);
            fileWatcher.FileLoadedOrSaved(path);
        }

        private void SaveMsgFilesToGame(string path, IList<MSG_File> files)
        {
            for(int i = 0; i < files.Count; i++)
            {
                SaveFileToGame(path + LanguageSuffix[i], files[i]);
            }
        }

        public static byte[] GetBytesFromParsedFile(string path, object data)
        {
            switch (Path.GetExtension(path))
            {
                case ".bac":
                    return ((BAC_File)data).SaveToBytes();
                case ".bcs":
                    return ((BCS_File)data).SaveToBytes();
                case ".bdm":
                    return ((BDM_File)data).SaveToBytes();
                case ".bsa":
                    return ((BSA_File)data).SaveToBytes();
                case ".cms":
                    return ((CMS_File)data).SaveToBytes();
                case ".cso":
                    return ((CSO_File)data).SaveToBytes();
                case ".cus":
                    return ((CUS_File)data).SaveToBytes();
                case ".ean":
                    return ((EAN_File)data).SaveToBytes();
                case ".ers":
                    return ((ERS_File)data).SaveToBytes();
                case ".idb":
                    return ((IDB_File)data).SaveToBytes();
                case ".msg":
                    return ((MSG_File)data).SaveToBytes();
                case ".pup":
                    return ((PUP_File)data).SaveToBytes();
                case ".psc":
                    return ((PSC_File)data).SaveToBytes();
                default:
                    throw new InvalidDataException(String.Format("Xenoverse2.GetBytesFromParsedFile: The filetype of \"{0}\" is not supported.", path));
            }
        }

        public string GetAbsolutePath(string relativePath)
        {
            return (fileIO != null) ? fileIO.PathInGameDir(relativePath) : relativePath;
        }


    }

    public class Xv2Item
    {
        public int ID { get; private set; }
        public string Name { get; private set; }

        public Xv2Item(int id, string name)
        {
            ID = id;
            Name = name;
        }
    }

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
            if (Files.BacFile?.File?.IsNull() == false)
                Files.BacFile.File.Save(Files.BacFile.Path);

            if (Files.BcmFile?.File?.IsNull() == false)
                Files.BcmFile.File.Save(Files.BcmFile.Path);

            if (Files.BsaFile?.File?.IsNull() == false)
                Files.BsaFile.File.Save(Files.BsaFile.Path);

            if (Files.BdmFile?.File?.IsNull() == false)
                Files.BdmFile.File.Save(Files.BdmFile.Path);

            if (Files.ShotBdmFile?.File?.IsNull() == false)
                Files.ShotBdmFile.File.Save(Files.ShotBdmFile.Path);

            if (Files.BasFile?.File?.IsNull() == false)
                Files.BasFile.File.Save(Files.BasFile.Path);

            if(Files.SeAcbFile.Count > 0)
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

            if (Files.AfterBacFile?.File?.IsNull() == false)
                Files.AfterBacFile.File.Save(Files.AfterBacFile.Path);

            if (Files.AfterBcmFile?.File?.IsNull() == false)
                Files.AfterBcmFile.File.Save(Files.AfterBcmFile.Path);
        }

        public void CalculateSkillFilePaths()
        {
            string skillDir = Xenoverse2.Instance.GetSkillDir(skillType);
            string folderName = Xenoverse2.Instance.GetSkillFolderName(CusEntry);

            if (!Files.BacFile.Borrowed)
                Files.BacFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bac", skillDir, folderName));

            if (!Files.BcmFile.Borrowed)
                Files.BcmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.bcm", skillDir, folderName));

            if (!Files.BdmFile.Borrowed)
                Files.BdmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.bdm", skillDir, folderName));

            if (!Files.ShotBdmFile.Borrowed)
                Files.ShotBdmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_PLAYER.shot.bdm", skillDir, folderName));

            if (!Files.BsaFile.Borrowed)
                Files.BsaFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bsa", skillDir, folderName));

            if (!Files.BasFile.Borrowed)
                Files.BasFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.bas", skillDir, folderName));

            if (!Files.EepkFile.Borrowed)
                Files.EepkFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}.eepk", skillDir, folderName));

            if (!Files.SeAcbFile[0].Borrowed)
                Files.SeAcbFile[0].Path = Xenoverse2.Instance.GetAbsolutePath(string.Format(@"sound/SE/Battle/Skill/CAR_BTL_{2}{1}_{0}_SE.acb", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType)));
            
            foreach (var vox in Files.VoxAcbFile)
            {
                if (!vox.Borrowed)
                {
                    if (vox.IsEnglish)
                    {
                        vox.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format(@"sound/VOX/Battle/Skill/en/CAR_BTL_{2}{1}_{0}_{3}_VOX", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType), vox.CharaCode));
                    }
                    else
                    {
                        //Japanese
                        vox.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format(@"sound/VOX/Battle/Skill/CAR_BTL_{2}{1}_{0}_{3}_VOX", CusEntry.ShortName, CusEntry.ID2.ToString("D3"), Xenoverse2.Instance.GetAcbSkillTypeLetter(skillType), vox.CharaCode));
                    }
                }
            }

            foreach (var ean in Files.EanFile)
            {
                if (!ean.Borrowed)
                {
                    if (ean.IsDefault)
                    {
                        ean.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}.ean", skillDir, folderName));
                    }
                    else
                    {
                        //Chara specific eans
                        ean.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}_{2}.ean", skillDir, folderName, ean.CharaCode));
                    }
                }
            }

            foreach (var ean in Files.CamEanFile)
            {
                if (!ean.Borrowed)
                {
                    if (ean.IsDefault)
                    {
                        ean.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}.cam.ean", skillDir, folderName));
                    }
                    else
                    {
                        //Chara specific eans
                        ean.Path = Xenoverse2.Instance.GetAbsolutePath(string.Format("{0}/{1}/{1}_{2}.cam.ean", skillDir, folderName, ean.CharaCode));
                    }
                }
            }

            //These files, at least as of the current code, can be null... so a check is required
            if (Files.AfterBacFile?.Borrowed == false)
                Files.AfterBacFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_AFTER.bac", skillDir, folderName));

            if (Files.AfterBcmFile?.Borrowed == false)
                Files.AfterBcmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}/{1}_AFTER_PLAYER.bcm", skillDir, folderName));

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
                Files.SeAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.SE_ACB, "0"));

            if (Files.AfterBacFile == null)
                Files.AfterBacFile = new Xv2File<BAC_File>(BAC_File.DefaultBacFile(), null, false, null, false, Xenoverse2.MoveFileTypes.AFTER_BAC);

            if (Files.AfterBcmFile == null)
                Files.AfterBcmFile = new Xv2File<BCM_File>(BCM_File.DefaultBcmFile(), null, false, null, false, Xenoverse2.MoveFileTypes.AFTER_BCM);
        }
    }

    public class Xv2Character 
    {
        public string[] Name = new string[(int)Xenoverse2.Language.NumLanguages];

        public ObservableCollection<Xv2CharaCostume> Costumes { get; set; } = new ObservableCollection<Xv2CharaCostume>();
        public CMS_Entry CmsEntry = null;
        public List<CSO_Entry> CsoEntry = null;
        public ERS_MainTableEntry ErsEntry = null;
        public Xv2File<BCS_File> BcsFile = null;
        public Xv2File<BAI_File> BaiFile = null;
        public List<Xv2File<AMK_File>> AmkFile = null;

        public Xv2MoveFiles MovesetFiles = null;

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

            foreach(var ean in MovesetFiles.EanFile)
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

            foreach(var vox in MovesetFiles.VoxAcbFile)
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
                MovesetFiles.BacFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bac", skillDir, CmsEntry.ShortName));
                CmsEntry.BacPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BcmFile?.Borrowed == false)
            {
                MovesetFiles.BcmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bcm", skillDir, CmsEntry.ShortName));
                CmsEntry.BcmPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.BdmFile?.Borrowed == false)
            {
                MovesetFiles.BdmFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}_PLAYER.bdm", skillDir, CmsEntry.ShortName));
                CmsEntry.BdmPath = CmsEntry.ShortName;
            }

            if (!MovesetFiles.CamEanFile[0]?.Borrowed == false)
            {
                MovesetFiles.CamEanFile[0].Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}.cam.ean", skillDir, CmsEntry.ShortName));
                CmsEntry.CamEanPath = CmsEntry.ShortName;
            }

            if (BaiFile?.Borrowed == false)
            {
                BaiFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}.bai", skillDir, CmsEntry.ShortName));
                CmsEntry.BaiPath = CmsEntry.ShortName;
            }

            if (MovesetFiles.EepkFile?.Borrowed == false)
            {
                MovesetFiles.EepkFile.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("vfx/chara/{0}/{0}.eepk", CmsEntry.ShortName));
            }

            for (int i = 0; i < AmkFile.Count; i++)
            {
                //Number the AMK file if there are more than one

                if (!AmkFile[i].Borrowed)
                {
                    AmkFile[i].Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}{2}.amk", skillDir, CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
            }

            for (int i = 0; i < MovesetFiles.SeAcbFile.Count; i++)
            {
                //Number the ACB file if there are more than one

                if (!MovesetFiles.SeAcbFile[i].Borrowed)
                {
                    MovesetFiles.SeAcbFile[i].Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("sound/SE/Battle/Chara/CAR_BTL_{0}{1}_SE.acb", CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
            }

            for (int i = 0; i < MovesetFiles.VoxAcbFile.Count; i++)
            {
                //Number the ACB file if there are more than one

                if (!MovesetFiles.VoxAcbFile[i].Borrowed && MovesetFiles.VoxAcbFile[i].IsEnglish)
                {
                    MovesetFiles.VoxAcbFile[i].Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("sound/VOX/Battle/chara/en/CAR_BTL_{0}{1}_VOX.acb", CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
                else if (!MovesetFiles.VoxAcbFile[i].Borrowed && !MovesetFiles.VoxAcbFile[i].IsEnglish)
                {
                    MovesetFiles.VoxAcbFile[i].Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("sound/VOX/Battle/chara/CAR_BTL_{0}{1}_VOX.acb", CmsEntry.ShortName, (i > 0) ? i.ToString() : string.Empty));
                }
            }

            //EANs
            var mainEan = MovesetFiles.EanFile.FirstOrDefault(x => x.IsDefault && x.FileType == Xenoverse2.MoveFileTypes.EAN);
            var fceEan = MovesetFiles.EanFile.FirstOrDefault(x => x.FileType == Xenoverse2.MoveFileTypes.FCE_EAN);

            if (mainEan?.Borrowed == false)
            {
                mainEan.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}.ean", skillDir, CmsEntry.ShortName));
                CmsEntry.EanPath = CmsEntry.ShortName;
            }

            if (fceEan?.Borrowed == false)
            {
                fceEan.Path = Xenoverse2.Instance.GetAbsolutePath(String.Format("{0}/{1}.fce.ean", skillDir, CmsEntry.ShortName));
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

            if (MovesetFiles.EanFile.Any(x => !x.IsDefault) && Xenoverse2.Instance.CmnEan != null)
                MovesetFiles.EanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultFile(Xenoverse2.Instance.CmnEan.Skeleton), null, false, null, false, Xenoverse2.MoveFileTypes.EAN));

            if (MovesetFiles.CamEanFile.Any(x => !x.IsDefault))
                MovesetFiles.CamEanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultCamFile(), null, false, null, false, Xenoverse2.MoveFileTypes.CAM_EAN));

            if (MovesetFiles.VoxAcbFile.Any(x => !x.IsDefault && x.IsEnglish))
                MovesetFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, true, Xenoverse2.MoveFileTypes.VOX_ACB, "0"));

            if (MovesetFiles.VoxAcbFile.Any(x => !x.IsDefault && !x.IsEnglish))
                MovesetFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.VOX_ACB, "0"));

            if (MovesetFiles.SeAcbFile.Any(x => !x.IsDefault))
                MovesetFiles.SeAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, false, Xenoverse2.MoveFileTypes.SE_ACB, "0"));

            if (AmkFile.Any(x => !x.IsDefault))
                AmkFile.Add(new Xv2File<AMK_File>(new AMK_File(), null, false, null, false, Xenoverse2.MoveFileTypes.AMK, "0"));
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

        public Xv2CharaCostume (int id)
        {
            CostumeId = id;
        }

        public static Xv2CharaCostume GetAndAddCostume(IList<Xv2CharaCostume> costumes, int id)
        {
            var existing = costumes.FirstOrDefault(x => x.CostumeId == id);

            if(existing == null)
            {
                existing = new Xv2CharaCostume(id);
                costumes.Add(existing);
            }

            return existing;
        }
    }

    [Serializable]
    public class Xv2MoveFiles
    {
        //Generic
        public Xv2File<BAC_File> BacFile { get; set; } = null;
        public Xv2File<BCM_File> BcmFile { get; set; } = null;
        public Xv2File<BDM_File> BdmFile { get; set; } = null;
        public AsyncObservableCollection<Xv2File<EAN_File>> EanFile { get; set; } = new AsyncObservableCollection<Xv2File<EAN_File>>();
        public AsyncObservableCollection<Xv2File<EAN_File>> CamEanFile { get; set; } = new AsyncObservableCollection<Xv2File<EAN_File>>();
        public Xv2File<EffectContainerFile> EepkFile { get; set; } = null;
        public AsyncObservableCollection<Xv2File<ACB_Wrapper>> SeAcbFile { get; set; } = new AsyncObservableCollection<Xv2File<ACB_Wrapper>>();
        public AsyncObservableCollection<Xv2File<ACB_Wrapper>> VoxAcbFile { get; set; } = new AsyncObservableCollection<Xv2File<ACB_Wrapper>>();

        //Skill
        public Xv2File<BSA_File> BsaFile { get; set; } = null;
        public Xv2File<BDM_File> ShotBdmFile { get; set; } = null;
        public Xv2File<BAS_File> BasFile { get; set; } = null;

        //After / Awoken specific
        public Xv2File<BAC_File> AfterBacFile { get; set; } = null;
        public Xv2File<BCM_File> AfterBcmFile { get; set; } = null;

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

            for(int i = 0; i < entries.Count; i++)
                indexes[i] = AddBdmEntry(entries[i], shotBdm);

            return indexes;
        }

        private int AddBdmEntry(BDM_Entry entry, bool shotBdm)
        {
            int idx = (shotBdm) ? ShotBdmFile.File.NextID() : BdmFile.File.NextID();
            var newCopy = entry.Copy();
            newCopy.ID = idx;

            if(shotBdm)
                ShotBdmFile.File.AddEntry(idx, newCopy);
            else
                BdmFile.File.AddEntry(idx, newCopy);

            return idx;
        }
        
        #endregion

        #region Add
        public void AddCamEanFile(EAN_File file, string chara, string path, bool borrowed = false, bool isDefault = false)
        {
            int index = CamEanFile.IndexOf(CamEanFile.FirstOrDefault(x => x.CharaCode == chara));
            if (index != -1)
                CamEanFile[index] = new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.CAM_EAN, null, isDefault);
            else
                CamEanFile.Add(new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.CAM_EAN, null, isDefault));
        }

        public void AddEanFile(EAN_File file, string chara, string path, bool borrowed = false, bool isDefault = false)
        {
            int index = EanFile.IndexOf(EanFile.FirstOrDefault(x => x.CharaCode == chara));
            if (index != -1)
                EanFile[index] = new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.EAN, null, isDefault);
            else
                EanFile.Add(new Xv2File<EAN_File>(file, path, borrowed, chara, false, Xenoverse2.MoveFileTypes.EAN, null, isDefault));
        }
        
        public void AddVoxAcbFile(ACB_Wrapper file, string chara, bool isEnglish, string path, bool borrowed = false, bool isDefault = false)
        {
            int index = VoxAcbFile.IndexOf(VoxAcbFile.FirstOrDefault(x => x.CharaCode == chara && x.IsEnglish == isEnglish));
            if (index != -1)
                VoxAcbFile[index] = new Xv2File<ACB_Wrapper>(file, path, borrowed, chara, isEnglish, Xenoverse2.MoveFileTypes.VOX_ACB, null, isDefault);
            else
                VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(file, path, borrowed, chara, isEnglish, Xenoverse2.MoveFileTypes.VOX_ACB, null, isDefault));
        }

        public void AddSeAcbFile(ACB_Wrapper file, int costume, string path, bool borrowed = false)
        {
            var entry = new Xv2File<ACB_Wrapper>(file, path, borrowed, null, false, Xenoverse2.MoveFileTypes.SE_ACB);
            entry.CostumesString = costume.ToString();
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
            UpdateTypeString(EepkFile, Xenoverse2.MoveFileTypes.EEPK);
            UpdateTypeString(BasFile, Xenoverse2.MoveFileTypes.BAS);
            UpdateTypeString(AfterBacFile, Xenoverse2.MoveFileTypes.AFTER_BAC);
            UpdateTypeString(AfterBcmFile, Xenoverse2.MoveFileTypes.AFTER_BCM);

            foreach(var file in VoxAcbFile)
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
            if (EepkFile == xv2FileObject) return EepkFile.UnborrowFile();

            List<IUndoRedo> undos = new List<IUndoRedo>();

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
            if (EepkFile == xv2FileObject) return EepkFile.ReplaceFile(path);

            List<IUndoRedo> undos = new List<IUndoRedo>();

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

            if(AfterBacFile == xv2FileObject)
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

    [Serializable]
    public class Xv2File<T> : INotifyPropertyChanged where T : class
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

        //UI Properties
        public Xenoverse2.MoveFileTypes FileType { get; set; }
        public string BorrowString { get { return (Borrowed) ? "Yes" : "No"; } }
        public string PathString { get { return (Borrowed) ? Path : "Calculated on save"; } }
        public string EanDisplayName
        {
            get
            {
                switch (FileType)
                {
                    case Xenoverse2.MoveFileTypes.TAL_EAN:
                        return "Tail";
                    case Xenoverse2.MoveFileTypes.FCE_EAN:
                        return "Face";
                    case Xenoverse2.MoveFileTypes.EAN:
                    case Xenoverse2.MoveFileTypes.CAM_EAN:
                    case Xenoverse2.MoveFileTypes.VOX_ACB:
                        return (IsDefault) ? "Main" : CharaCode;
                    default:
                        return "invalid";
                }
            }
        }
        
        //Data
        public T File { get; set; } = null;
        /// <summary>
        /// Absolute path to the file. This will be re-calculated when saving except when it is "Borrowed" or was loaded manually.
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// If true, then this file belongs to another source. In this case, it will always be saved back to its original source (overwritting it) unless specified otherwise.
        /// </summary>
        public bool Borrowed { get; set; } = false;

        #region Args
        //Arguments
        private string _charaCode = string.Empty;
        private bool _isEnglish = false;
        private string _costumes = string.Empty;

        /// <summary>
        /// Used to specify character code for VOX ACB and EAN files.
        /// </summary>
        public string CharaCode
        {
            get
            {
                return _charaCode;
            }
            set
            {
                if(_charaCode != value)
                {
                    _charaCode = value;
                    NotifyPropertyChanged(nameof(CharaCode));
                    NotifyPropertyChanged(nameof(EanDisplayName));
                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
            }
        }
        /// <summary>
        /// Applicable to ACB files only. Determines if the ACB file is for English or Japanese.
        /// </summary>
        public bool IsEnglish
        {
            get { return _isEnglish; }
            set
            {
                if(value != _isEnglish)
                {
                    _isEnglish = value;
                    NotifyPropertyChanged(nameof(IsEnglish));
                }
            }
        }
        /// <summary>
        /// Used to specify what costumes this file is for (se, vox, amk). Accepts multiple int values (greater than 0) seperated by "," or a single value of "0".
        /// </summary>
        public string CostumesString
        {
            get
            {
                return _costumes;
            }
            set
            {
                if (_costumes != value)
                {
                    _costumes = value;
                    NotifyPropertyChanged(nameof(CostumesString));
                    NotifyPropertyChanged(nameof(UndoableCostumes));
                }
            }
        }
        /// <summary>
        /// Is this file a default file? (Default files are always on a move and cannot be deleted).
        /// </summary>
        public bool IsDefault { get; private set; }

        //Helpers
        //public bool IsDefault { get { return string.IsNullOrWhiteSpace(CharaCode) || CostumesString == "0"; } }
        public bool IsNotDefault { get { return !IsDefault; } }

        //Undo/Redo
        public string UndoableCharaCode
        {
            get
            {
                return CharaCode;
            }
            set
            {
                if(CharaCode != value && !string.IsNullOrEmpty(value))
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<T>>(nameof(CharaCode), this, CharaCode, value, "CharaCode"));
                    CharaCode = value;

                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
                else
                {
                    //Value cannot be set to null/empty
                    CharaCode = _charaCode;
                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
            }
        }
        public bool UndoableIsEnglish
        {
            get
            {
                return IsEnglish;
            }
            set
            {
                if (IsEnglish != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<T>>(nameof(IsEnglish), this, IsEnglish, value, "IsEnlgish"));
                    IsEnglish = value;

                    NotifyPropertyChanged(nameof(UndoableIsEnglish));
                }
            }
        }
        public string UndoableCostumes
        {
            get
            {
                return CostumesString;
            }
            set
            {
                if (CostumesString != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<T>>(nameof(CostumesString), this, CostumesString, value, "Costumes"));
                    CostumesString = value;

                    NotifyPropertyChanged(nameof(UndoableCostumes));
                }
            }
        }

        #endregion
        public Xv2File(T file, string path, bool borrowed, string charaCode = null, bool isEnglish = false, Xenoverse2.MoveFileTypes fileType = 0, string costumes = null, bool isDefault = true)
        {
            File = file;
            Path = path;
            Borrowed = borrowed;
            CharaCode = charaCode;
            IsEnglish = isEnglish;
            CostumesString = costumes;
            FileType = fileType;
            IsDefault = isDefault;
        }

        public List<IUndoRedo> ReplaceFile(string path)
        {
            object oldFile = File;
            object newFile = null;

            if(File is BAC_File && System.IO.Path.GetExtension(path) == ".bac")
            {
                newFile = BAC_File.Load(path);
                ((BAC_File)newFile).InitializeIBacTypes();
            }
            else if (File is BDM_File && System.IO.Path.GetExtension(path) == ".bdm")
            {
                newFile = BDM_File.Load(path);
            }
            else if (File is BSA_File && System.IO.Path.GetExtension(path) == ".bsa")
            {
                newFile = BSA_File.Load(path);
            }
            else if (File is BAS_File && System.IO.Path.GetExtension(path) == ".bas")
            {
                newFile = BAS_File.Load(path);
            }
            else if (File is ACB_Wrapper && System.IO.Path.GetExtension(path) == ".acb")
            {
                newFile = new ACB_Wrapper(ACB_File.Load(path));
            }
            else if (File is EEPK_File && System.IO.Path.GetExtension(path) == ".eepk")
            {
                newFile = EffectContainerFile.Load(path);
            }
            else if (File is EAN_File && System.IO.Path.GetExtension(path) == ".ean")
            {
                newFile = EAN_File.Load(path, true);
            }
            else
            {
                throw new InvalidDataException("ReplaceFile: File type mismatch!");
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(File), this, oldFile, newFile));
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Borrowed), this, Borrowed, false));
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Path), this, Path, string.Empty));
            undos.Add(new UndoActionDelegate(this, nameof(RefreshProperties), true));

            File = (T)newFile;
            Borrowed = false;
            Path = string.Empty;

            RefreshProperties();
            return undos;
        }

        public List<IUndoRedo> UnborrowFile()
        {
            //this may be buggy with recursive files... todo check

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (Borrowed)
            {
                Borrowed = false;
                T copiedFile = File.Copy();

                undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Borrowed), this, true, false));
                undos.Add(new UndoableProperty<Xv2File<T>>(nameof(File), this, File, copiedFile));
                
                if(copiedFile is ACB_Wrapper acb)
                    acb.Refresh();
                
                File = copiedFile;

                undos.Add(new UndoActionDelegate(this, nameof(RefreshProperties), true));
                RefreshProperties();
            }


            return undos;
        }

        public bool IsOptionalFile()
        {
            //Optional files = VOX ACBs and EAN/CAM.EANs that are chara-unique. Also AFTER_BAC and AFTER_BCM.
            if (FileType == Xenoverse2.MoveFileTypes.AFTER_BAC || FileType == Xenoverse2.MoveFileTypes.AFTER_BCM) return true;
            if (FileType == Xenoverse2.MoveFileTypes.VOX_ACB && !IsDefault) return true;
            if ((FileType == Xenoverse2.MoveFileTypes.EAN || FileType == Xenoverse2.MoveFileTypes.CAM_EAN) && !IsDefault) return true;
            return false;
        }

        private void RefreshProperties()
        {
            NotifyPropertyChanged(nameof(FileType));
            NotifyPropertyChanged(nameof(BorrowString));
            NotifyPropertyChanged(nameof(PathString));
            NotifyPropertyChanged(nameof(CharaCode));
            NotifyPropertyChanged(nameof(IsEnglish));
            NotifyPropertyChanged(nameof(EanDisplayName));
            NotifyPropertyChanged(nameof(IsEnglish));
            NotifyPropertyChanged(nameof(File));
        }
    
        /// <summary>
        /// Gets a read-only List of all costumes specified in CostumesString.
        /// </summary>
        /// <returns></returns>
        public List<int> GetCostumes()
        {
            string[] values = CostumesString.Trim().Split(',');
            List<int> intValues = new List<int>();

            foreach(var value in values)
            {
                int intValue;
                if(!int.TryParse(value, out intValue))
                    throw new InvalidDataException($"Xv2File.GetArg2Values: value of \"{value}\" could not be parsed.");

                intValues.Add(intValue);
            }

            return intValues;
        }
        
        public bool HasCostume(int costume)
        {
            //return (GetArg2Values().Contains(costume) || Arg2 == "0");
            return (GetCostumes().Contains(costume));
        }

        public void AddCostume(int costume)
        {
            CostumesString += (string.IsNullOrWhiteSpace(CostumesString)) ? costume.ToString() : $", {costume}";
        }

        /// <summary>
        /// Add a costume to an existing loaded file. Intended for use with CSO files (AMK, SE, VOX)
        /// </summary>
        /// <returns></returns>
        public static bool AddCostume(IList<Xv2File<T>> files, string absPath, int costume, bool isEnglish)
        {
            var file = files.FirstOrDefault(x => Utils.ComparePaths(absPath, x.Path) && x.IsEnglish == isEnglish);

            if(file != null)
            {
                file.AddCostume(costume);
                return true;
            }

            return false;
        }
    }

    public class FileWatcher
    {
        private List<LastFileWriteTime> files = new List<LastFileWriteTime>();


        public void FileLoadedOrSaved(string path)
        {
            var entry = files.FirstOrDefault(x => x.FilePath == path);

            if(entry == null)
            {
                entry = new LastFileWriteTime(path);
                files.Add(entry);
                return;
            }

            entry.SetCurrentTime();
        }

        public bool WasFileModified(string path)
        {
            var entry = files.FirstOrDefault(x => x.FilePath == path);
            //if (entry == null) throw new NullReferenceException($"FileWatcher: no LastFileWriteTime entry for file: \"{path}\" was found.");
            if (entry == null) return true;

            return entry.HasBeenModified();
        }

        public void ClearAll()
        {
            files.Clear();
        }
    }

    public class LastFileWriteTime
    {
        public string FilePath { get; set; } //absolute
        /// <summary>
        /// Last time the file was loaded/saved by this application. 
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        public LastFileWriteTime(string filePath)
        {
            FilePath = filePath;
            LastWriteTime = File.GetLastWriteTimeUtc(filePath);
        }

        public void SetCurrentTime()
        {
            LastWriteTime = DateTime.UtcNow;
        }

        public bool HasBeenModified()
        {
            return (File.GetLastWriteTimeUtc(FilePath) > LastWriteTime);
        }
    }

    #region Events
    public delegate void Xv2FileNotFoundEventHandler(object source, Xv2FileNotFoundEventArgs e);
    
    public class Xv2FileNotFoundEventArgs : EventArgs
    {
        private string EventInfo;
        public Xv2FileNotFoundEventArgs(string file)
        {
            EventInfo = file;
        }
        public string GetInfo()
        {
            return EventInfo;
        }
    }
    #endregion
}
