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
            AFTER_BCM
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
            cmsFile = (CMS_File)GetParsedFileFromGame(CMS_PATH);
            ersFile = (ERS_File)GetParsedFileFromGame(ERS_PATH);
            csoFile = (CSO_File)GetParsedFileFromGame(CSO_PATH);
            LoadMsgFiles(ref charaNameMsgFile, CHARACTER_MSG_PATH);
        }

        private void InitSkills()
        {
            cusFile = (CUS_File)GetParsedFileFromGame(CUS_PATH);
            cmsFile = (CMS_File)GetParsedFileFromGame(CMS_PATH); //Duplication with InitCharacters, but it is needed
            skillIdbFile = (IDB_File)GetParsedFileFromGame(SKILL_IDB_PATH);
            pupFile = (PUP_File)GetParsedFileFromGame(PUP_PATH);
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
            CmnBac = (BAC_File)GetParsedFileFromGame(CMN_BAC_PATH);
            CmnBdm = (BDM_File)GetParsedFileFromGame(CMN_BDM_PATH);
            CmnEan = (EAN_File)GetParsedFileFromGame(CMN_EAN_PATH);
            CmnCamEan = (EAN_File)GetParsedFileFromGame(CMN_CAM_EAN_PATH);
        }

        private void LoadMsgFiles(ref MSG_File[] msgFiles, string path)
        {
            if (msgFiles == null)
                msgFiles = new MSG_File[(int)Language.NumLanguages];

            for (int i = 0; i < (int)Language.NumLanguages; i++)
                msgFiles[i] = (MSG_File)GetParsedFileFromGame(path + LanguageSuffix[i]);

            MSG_File.SynchronizeMsgFiles(msgFiles);
        }

        public void RefreshSkills()
        {
            loadSkills = true;
            InitSkills();
        }

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
        public Xv2Skill GetSkill(CUS_File.SkillType skillType, int id1)
        {
            if (!loadSkills) throw new InvalidOperationException("Xenoverse2.GetSkill: Cannot get skill as skills have not been loaded.");

            var cusEntry = GetSkillCusEntry(skillType, id1);
            if (cusEntry == null) throw new InvalidOperationException($"Xenoverse2.GetSkill: Skill was not found in the system (ID: {id1}, Type: {skillType}).");
            
            return new Xv2Skill()
            {
                BtlHud = GetAwokenStageNames(cusEntry.ID2, cusEntry.NumTransformations),
                CusEntry = cusEntry,
                Description = GetSkillDescs(skillType, cusEntry.ID2),
                Name = GetSkillNames(skillType, cusEntry.ID2),
                IdbEntry = GetSkillIdbEntry(skillType, cusEntry.ID2),
                PupEntries = new ObservableCollection<PUP_Entry>(pupFile.GetSequence(cusEntry.PUP, cusEntry.NumTransformations)),
                SkillFiles = GetSkillFiles(cusEntry, skillType)
            };
        }

        public Xv2MoveFiles GetSkillFiles(Skill cusEntry, CUS_File.SkillType skillType)
        {
            string skillDir = GetSkillDir(skillType);
            string folderName = GetSkillFolderName(cusEntry);
            Xv2MoveFiles moveFiles = new Xv2MoveFiles(String.Format("{0}/{1}", skillDir, folderName), folderName, (CmnEan != null) ? CmnEan.Skeleton : null, false);
            

            //BAC
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.BAC))
            {
                string path = String.Format("{0}/{1}/{1}.bac", skillDir, folderName);
                moveFiles.BacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BAC);
            }

            //BCM
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.BCM))
            {
                string path = String.Format("{0}/{1}/{1}_PLAYER.bcm", skillDir, folderName);
                moveFiles.BcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BCM);
            }

            //BDM
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.Bdm))
            {
                string path = String.Format("{0}/{1}/{1}_PLAYER.bdm", skillDir, folderName);
                moveFiles.BdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BDM); 
            }

            //BSA + shot.BDM
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.BsaAndShotBdm))
            {
                string shotBdmPath = String.Format("{0}/{1}/{1}_PLAYER.shot.bdm", skillDir, folderName);
                moveFiles.ShotBdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(shotBdmPath), fileIO.PathInGameDir(shotBdmPath), false, null, null, MoveFileTypes.SHOT_BDM);
                string bsaPath = String.Format("{0}/{1}/{1}.bsa", skillDir, folderName);
                moveFiles.BsaFile = new Xv2File<BSA_File>((BSA_File)GetParsedFileFromGame(bsaPath), fileIO.PathInGameDir(bsaPath), false, null, null, MoveFileTypes.BSA);
            }

            //BAS
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.Bas))
            {
                string path = String.Format("{0}/{1}/{1}.bas", skillDir, folderName);
                moveFiles.BasFile = new Xv2File<BAS_File>((BAS_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BAS);
            }

            //EEPK
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.Eepk))
            {
                if (!cusEntry.HasEepkPath)
                {
                    string path = String.Format("{0}/{1}/{1}.eepk", skillDir, folderName);
                    moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.EEPK);
                }
                else
                {
                    //This skill uses another skills EEPK, so we dont have to calculate its folder name
                    string path = String.Format("skill/{0}/{1}.eepk", cusEntry.Str_28, Path.GetFileName(cusEntry.Str_28));
                    moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), true, null, null, MoveFileTypes.EEPK);
                }
            }

            //SE ACB
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.CharaSE))
            {
                if (!cusEntry.HasSeAcbPath)
                {
                    string path = string.Format(@"sound/SE/Battle/Skill/CAR_BTL_{2}{1}_{0}_SE.acb", cusEntry.ShortName, cusEntry.ID2.ToString("D3"), GetAcbSkillTypeLetter(skillType));
                    moveFiles.SeAcbFile = new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.SE_ACB);
                }
                else
                {
                    string path = string.Format(@"sound/SE/Battle/Skill/{0}.acb", cusEntry.SeAcbPath);
                    moveFiles.SeAcbFile = new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), true, null, null, MoveFileTypes.SE_ACB);
                }
            }

            //VOX ACB
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.CharaVOX))
            {
                moveFiles.VoxAcbFile.Clear();

                //Japanese
                string[] files = fileIO.GetFilesInDirectory("sound/VOX/Battle/Skill", "acb");
                string name = (!cusEntry.HasVoxAcbPath) ? string.Format(@"CAR_BTL_{2}{1}_{0}_", cusEntry.ShortName, cusEntry.ID2.ToString("D3"), GetAcbSkillTypeLetter(skillType)) : cusEntry.VoxAcbPath;
                
                foreach(var file in files.Where(f => f.Contains(name) && f.Contains("_VOX.acb")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = split[(split.Length - 2 > 0) ? split.Length - 2 : 0];

                    moveFiles.AddVoxAcbFile((ACB_Wrapper)GetParsedFileFromGame(file), charaSuffix, null, fileIO.PathInGameDir(file));
                }

                //English
                files = fileIO.GetFilesInDirectory("sound/VOX/Battle/Skill/en", "acb");

                foreach (var file in files.Where(f => f.Contains(name) && f.Contains("_VOX.acb")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = split[(split.Length - 2 > 0) ? split.Length - 2 : 0];
                    
                    moveFiles.AddVoxAcbFile((ACB_Wrapper)GetParsedFileFromGame(file), charaSuffix, "en", fileIO.PathInGameDir(file));
                }
            }

            //EAN
            if (cusEntry.FilesLoadedFlags2.HasFlag(Skill.Type.EAN))
            {
                moveFiles.EanFile.Clear();

                string name = (!cusEntry.HasEanPath) ? string.Format("{0}/{1}/{1}.ean", skillDir, folderName) : Utils.ResolveRelativePath("skill/" + cusEntry.EanPath + ".ean");
                name = Utils.SanitizePath(name);
                string[] files = fileIO.GetFilesInDirectory(Path.GetDirectoryName(name), ".ean");

                foreach (var file in files.Where(f => f.Contains(Path.GetFileNameWithoutExtension(name)) && f.Contains(".ean") && !f.Contains(".cam")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = (split.Length == 4) ? split[3].Split('.')[0] : null;
                    moveFiles.AddEanFile((EAN_File)GetParsedFileFromGame(file), charaSuffix, fileIO.PathInGameDir(file));
                }

                //Create default EAN if none was loaded (duplicate chara-unique one)
                if (!moveFiles.EanFile.Any(x => x.IsDefault))
                    moveFiles.AddEanFile(moveFiles.EanFile[0].File.Copy(), null, name);
            }

            //CAM
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.CamEan))
            {
                moveFiles.CamEanFile.Clear();

                string nameWithoutExt = (!cusEntry.HasCamEanPath) ? string.Format("{0}/{1}/{1}", skillDir, folderName) : Utils.ResolveRelativePath("skill/" + cusEntry.CamEanPath);
                nameWithoutExt = Utils.SanitizePath(nameWithoutExt);
                string name = nameWithoutExt + ".cam.ean";
                string[] files = fileIO.GetFilesInDirectory(Path.GetDirectoryName(nameWithoutExt), ".ean");

                foreach (var file in files.Where(f => f.Contains(nameWithoutExt) && f.Contains("cam.ean")))
                {
                    string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                    string charaSuffix = (split.Length == 4) ? split[3].Split('.')[0] : null;
                    moveFiles.AddCamEanFile((EAN_File)GetParsedFileFromGame(file), charaSuffix, fileIO.PathInGameDir(file));
                }

                //Create default CAM.EAN if none was loaded (duplicate chara-unique one)
                if (!moveFiles.CamEanFile.Any(x => x.IsDefault))
                    moveFiles.AddCamEanFile(moveFiles.CamEanFile[0].File.Copy(), null, name);
            }
            
            //AFTER BAC
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.AfterBac))
            {
                if (!cusEntry.HasAfterBacPath)
                {
                    string path = String.Format("{0}/{1}/{1}__AFTER.bac", skillDir, folderName);
                    moveFiles.AfterBacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BAC);
                }
                else
                {
                    string path = String.Format("skill/{0}/{1}.bac", cusEntry.AfterBacPath, Path.GetFileName(cusEntry.AfterBacPath));
                    moveFiles.AfterBacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), true, null, null, MoveFileTypes.BAC);
                }
            }

            //AFTER BCM
            if (cusEntry.I_14.HasFlag(Skill.FilesLoadedFlags.AfterBcm))
            {
                if (!cusEntry.HasAfterBacPath)
                {
                    string path = String.Format("{0}/{1}/{1}__AFTER_PLAYER.bcm", skillDir, folderName);
                    moveFiles.AfterBcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), false, null, null, MoveFileTypes.BCM);
                }
                else
                {
                    string path = String.Format("skill/{0}/{1}.bcm", cusEntry.AfterBcmPath, Path.GetFileName(cusEntry.AfterBcmPath));
                    moveFiles.AfterBcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(path), fileIO.PathInGameDir(path), true, null, null, MoveFileTypes.BCM);
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
        private string GetSkillDir(CUS_File.SkillType skillType)
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

        private string GetSkillFolderName(Skill cusEntry)
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

        private string GetAcbSkillTypeLetter(CUS_File.SkillType skillType)
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
        public Xv2Character GetCharacter(int cmsId)
        {
            if (!loadCharacters) throw new InvalidOperationException("Xenoverse2.GetCharacter: Cannot get character as characters have not been loaded.");

            var cmsEntry = cmsFile.CMS_Entries.FirstOrDefault(x => x.ID == cmsId);
            if(cmsEntry == null) throw new InvalidOperationException($"Xenoverse2.GetCharacter: Character was not found in the system (ID: {cmsId}).");
            var names = GetCharacterName(cmsEntry.ShortName);
            var csoEntry = csoFile.CsoEntries.FirstOrDefault(x => x.CharaID == cmsId);
            var ersEntry = ersFile.GetEntry(2, cmsId);

            //Load bcs
            string bcsPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bcs", cmsEntry.ShortName, cmsEntry.BcsPath));
            BCS_File bcsFile = (BCS_File)GetParsedFileFromGame(bcsPath);

            //Load amk
            AMK_File amkFile = null;
            string amkPath = string.Empty;
            if(csoEntry != null)
            {
                if(csoEntry.AmkPath != "NULL" && !string.IsNullOrWhiteSpace(csoEntry.AmkPath))
                {
                    amkPath = Utils.ResolveRelativePath(string.Format("chara/{0}.amk", csoEntry.AmkPath));
                    amkFile = (AMK_File)GetParsedFileFromGame(amkPath);
                }
            }

            //Load fce ean
            string fceEanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.fce.ean", cmsEntry.ShortName, cmsEntry.FceEanPath));
            EAN_File fceEan = (EAN_File)GetParsedFileFromGame(fceEanPath);

            //Load bai file
            string baiPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.bai", cmsEntry.ShortName, cmsEntry.BaiPath));
            BAI_File baiFile = (BAI_File)GetParsedFileFromGame(baiPath);
            
            return new Xv2Character()
            {
                CmsEntry = cmsEntry,
                CsoEntry = csoEntry,
                ErsEntry = ersEntry,
                Name = names,
                BcsFile = new Xv2File<BCS_File>(bcsFile, fileIO.PathInGameDir(bcsPath), false, null, null),
                AmkFile = (amkFile != null) ? new Xv2File<AMK_File>(amkFile, fileIO.PathInGameDir(amkPath), false) : null,
                FceEanFile = new Xv2File<EAN_File>(fceEan, fileIO.PathInGameDir(fceEanPath), cmsEntry.IsSelfReference(cmsEntry.FceEanPath)),
                BaiFile = new Xv2File<BAI_File>(baiFile, fileIO.PathInGameDir(baiPath), cmsEntry.IsSelfReference(cmsEntry.BaiPath)),
                MovesetFiles = GetCharacterMoveFiles(cmsEntry, ersEntry, csoEntry)
            };
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
        
        private Xv2MoveFiles GetCharacterMoveFiles(CMS_Entry cmsEntry, ERS_MainTableEntry ersEntry, CSO_Entry csoEntry)
        {
            Xv2MoveFiles moveFiles = new Xv2MoveFiles(string.Format("chara/{0}", cmsEntry.ShortName), cmsEntry.ShortName, (CmnEan != null) ? CmnEan.Skeleton : null, false);

            //Clear defaults out
            moveFiles.EanFile.Clear();
            moveFiles.CamEanFile.Clear();

            //BAC
            string bacPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bac", cmsEntry.ShortName, cmsEntry.BacPath));
            moveFiles.BacFile = new Xv2File<BAC_File>((BAC_File)GetParsedFileFromGame(bacPath), fileIO.PathInGameDir(bacPath), cmsEntry.IsSelfReference(cmsEntry.BacPath), null, null, MoveFileTypes.BAC);

            //BCM
            string bcmPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bcm", cmsEntry.ShortName, cmsEntry.BcmPath));
            moveFiles.BcmFile = new Xv2File<BCM_File>((BCM_File)GetParsedFileFromGame(bcmPath), fileIO.PathInGameDir(bcmPath), cmsEntry.IsSelfReference(cmsEntry.BcmPath), null, null, MoveFileTypes.BCM);

            //EAN
            string eanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.ean", cmsEntry.ShortName, cmsEntry.EanPath));
            moveFiles.EanFile.Clear();
            moveFiles.EanFile.Add(new Xv2File<EAN_File>((EAN_File)GetParsedFileFromGame(eanPath), fileIO.PathInGameDir(eanPath), cmsEntry.IsSelfReference(cmsEntry.EanPath), null, null, MoveFileTypes.EAN));

            //CAM
            string camEanPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.cam.ean", cmsEntry.ShortName, cmsEntry.CamEanPath));
            moveFiles.CamEanFile.Clear();
            moveFiles.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)GetParsedFileFromGame(camEanPath), fileIO.PathInGameDir(camEanPath), cmsEntry.IsSelfReference(cmsEntry.CamEanPath), null, null, MoveFileTypes.CAM_EAN));

            //BDM
            if(cmsEntry.BdmPath != "NULL")
            {
                string bdmPath = Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_PLAYER.bdm", cmsEntry.ShortName, cmsEntry.BdmPath));
                moveFiles.BdmFile = new Xv2File<BDM_File>((BDM_File)GetParsedFileFromGame(bdmPath), fileIO.PathInGameDir(bdmPath), cmsEntry.IsSelfReference(cmsEntry.BdmPath), null, null, MoveFileTypes.BDM);

            }
            //EEPK
            if (ersEntry != null)
            {
                bool borrowed = (ersEntry.FILE_PATH != string.Format("chara/{0}/{0}.eepk", cmsEntry.ShortName));
                string eepkPath = string.Format("vfx/{0}", ersEntry.FILE_PATH);
                moveFiles.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)GetParsedFileFromGame(eepkPath), fileIO.PathInGameDir(eepkPath), borrowed, null, null, MoveFileTypes.EEPK);
            }

            //ACBs
            if(csoEntry != null)
            {
                //SE
                if (csoEntry.HasSePath)
                {
                    string acbPath = $"sound/SE/Battle/Chara/{csoEntry.SePath}.acb";
                    moveFiles.SeAcbFile = new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(acbPath), fileIO.PathInGameDir(acbPath), false, null, null, MoveFileTypes.SE_ACB);
                }

                moveFiles.VoxAcbFile.Clear();

                //VOX, Jap
                if (csoEntry.HasVoxPath)
                {
                    moveFiles.VoxAcbFile.Clear();

                    string acbPath = $"sound/VOX/Battle/Chara/{csoEntry.VoxPath}.acb";
                    moveFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(acbPath), fileIO.PathInGameDir(acbPath), false, null, null, MoveFileTypes.VOX_ACB));
                }

                //VOX, Eng
                if (csoEntry.HasVoxPath)
                {
                    string acbPath = $"sound/VOX/Battle/Chara/en/{csoEntry.VoxPath}.acb";
                    moveFiles.VoxAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)GetParsedFileFromGame(acbPath), fileIO.PathInGameDir(acbPath), false, null, "en", MoveFileTypes.VOX_ACB));
                }
            }


            return moveFiles;
        }

        #endregion

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
                    return BAC_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".bcm":
                    return BCM_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".bcs":
                    return BCS_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".bdm":
                    return BDM_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk), true);
                case ".bsa":
                    return BSA_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".cms":
                    return CMS_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".cso":
                    return CSO_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".cus":
                    return CUS_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".ean":
                    return EAN_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".ers":
                    return ERS_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".idb":
                    return IDB_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".pup":
                    return PUP_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".bas":
                    return BAS_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".bai":
                    return BAI_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".amk":
                    return AMK_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".esk":
                    return ESK_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".emd":
                    return EMD_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
                case ".msg":
                    return MSG_File.Load(GetBytesFromGameWrapper(path, raiseEx, onlyFromCpk));
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

        public string GetAbsolutePath(string relativePath)
        {
            return (fileIO != null) ? fileIO.PathInGameDir(relativePath) : relativePath;
        }

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


        private byte[] GetBytesFromGameWrapper(string path, bool onlyFromCpk = false, bool raiseEx = false)
        {
            var bytes = fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk);
            if (bytes == null) FileNotFoundEvent.Invoke(this, new Xv2FileNotFoundEventArgs(path));
            return bytes;
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
        public Skill CusEntry = null;
        public IDB_Entry IdbEntry = null;
        public ObservableCollection<PUP_Entry> PupEntries = new ObservableCollection<PUP_Entry>();
        public string[] Name = new string[(int)Xenoverse2.Language.NumLanguages];
        public string[] Description = new string[(int)Xenoverse2.Language.NumLanguages];
        public ObservableCollection<string[]> BtlHud = new ObservableCollection<string[]>();

        public Xv2MoveFiles SkillFiles = null;
    }

    public class Xv2Character 
    {
        public string[] Name = new string[(int)Xenoverse2.Language.NumLanguages];

        public CMS_Entry CmsEntry = null;
        public CSO_Entry CsoEntry = null;
        public ERS_MainTableEntry ErsEntry = null;
        public Xv2File<BCS_File> BcsFile = null;
        public Xv2File<BAI_File> BaiFile = null;
        public Xv2File<AMK_File> AmkFile = null;
        public Xv2File<EAN_File> FceEanFile = null;

        public Xv2MoveFiles MovesetFiles = null;

        //EMDs, ESKs loaded seperately
    }

    [Serializable]
    public class Xv2MoveFiles
    {
        //Generic
        public Xv2File<BAC_File> BacFile { get; set; } = null;
        public Xv2File<BCM_File> BcmFile { get; set; } = null;
        public Xv2File<BDM_File> BdmFile { get; set; } = null;
        public ObservableCollection<Xv2File<EAN_File>> EanFile { get; set; } = new ObservableCollection<Xv2File<EAN_File>>();
        public ObservableCollection<Xv2File<EAN_File>> CamEanFile { get; set; } = new ObservableCollection<Xv2File<EAN_File>>();
        public Xv2File<EffectContainerFile> EepkFile { get; set; } = null;
        public Xv2File<ACB_Wrapper> SeAcbFile { get; set; } = null;
        public ObservableCollection<Xv2File<ACB_Wrapper>> VoxAcbFile { get; set; } = new ObservableCollection<Xv2File<ACB_Wrapper>>();

        //Skill
        public Xv2File<BSA_File> BsaFile { get; set; } = null;
        public Xv2File<BDM_File> ShotBdmFile { get; set; } = null;
        public Xv2File<BAS_File> BasFile { get; set; } = null;

        //After / Awoken specific
        public Xv2File<BAC_File> AfterBacFile { get; set; } = null;
        public Xv2File<BCM_File> AfterBcmFile { get; set; } = null;
        
        public Xv2MoveFiles()
        {
            BacFile = new Xv2File<BAC_File>(new BAC_File(), null, false, null, null, Xenoverse2.MoveFileTypes.BAC);
            BcmFile = new Xv2File<BCM_File>(new BCM_File(), null, false, null, null, Xenoverse2.MoveFileTypes.BCM);
            BdmFile = new Xv2File<BDM_File>(new BDM_File(), null, false, null, null, Xenoverse2.MoveFileTypes.BDM);
            BsaFile = new Xv2File<BSA_File>(new BSA_File(), null, false, null, null, Xenoverse2.MoveFileTypes.BSA);
            ShotBdmFile = new Xv2File<BDM_File>(new BDM_File(), null, false, null, null, Xenoverse2.MoveFileTypes.SHOT_BDM);
            EepkFile = new Xv2File<EffectContainerFile>(EffectContainerFile.New(), null, false, null, null, Xenoverse2.MoveFileTypes.EEPK);
            SeAcbFile = new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), null, false, null, null, Xenoverse2.MoveFileTypes.SE_ACB);
            
        }

        public Xv2MoveFiles(string dir, string eanName, EAN.ESK_Skeleton eanSkeleton, bool generateDefaultVoxAcb)
        {
            //Generate default files
            //Paths will be auto-generated when saving TO game. Manual saves will use paths set here.

            BacFile = new Xv2File<BAC_File>(new BAC_File(), string.Format("{0}/{1}.bac", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.BAC);
            BcmFile = new Xv2File<BCM_File>(new BCM_File(), string.Format("{0}/{1}.bcm", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.BCM);
            BdmFile = new Xv2File<BDM_File>(new BDM_File(), string.Format("{0}/{1}.bdm", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.BDM);
            BsaFile = new Xv2File<BSA_File>(new BSA_File(), string.Format("{0}/{1}.bsa", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.BSA);
            ShotBdmFile = new Xv2File<BDM_File>(new BDM_File(), string.Format("{0}/{1}.shot.bdm", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.SHOT_BDM);
            EepkFile = new Xv2File<EffectContainerFile>(EffectContainerFile.New(), string.Format("{0}/{1}.eepk", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.EEPK);
            SeAcbFile = new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), string.Format("{0}/{1}_SE.acb", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.SE_ACB);
            CamEanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultCamFile(), string.Format("{0}/{1}.cam.ean", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.CAM_EAN));
            
            //Default VOX ACBs are used only for movesets/characters. 
            if(generateDefaultVoxAcb)
                VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), string.Format("{0}/{1}_VOX.acb", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.VOX_ACB));

            if (eanSkeleton != null)
                EanFile.Add(new Xv2File<EAN_File>(EAN_File.DefaultFile(eanSkeleton), string.Format("{0}/{1}.ean", dir, eanName), false, null, null, Xenoverse2.MoveFileTypes.EAN));
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
        public void AddCamEanFile(EAN_File file, string chara, string path)
        {
            int index = CamEanFile.IndexOf(CamEanFile.FirstOrDefault(x => x.Arg0 == chara));
            if (index != -1)
                CamEanFile[index] = new Xv2File<EAN_File>(file, path, false, chara, null, Xenoverse2.MoveFileTypes.CAM_EAN);
            else
                CamEanFile.Add(new Xv2File<EAN_File>(file, path, false, chara, null, Xenoverse2.MoveFileTypes.CAM_EAN));
        }

        public void AddEanFile(EAN_File file, string chara, string path)
        {
            int index = EanFile.IndexOf(EanFile.FirstOrDefault(x => x.Arg0 == chara));
            if (index != -1)
                EanFile[index] = new Xv2File<EAN_File>(file, path, false, chara, null, Xenoverse2.MoveFileTypes.EAN);
            else
                EanFile.Add(new Xv2File<EAN_File>(file, path, false, chara, null, Xenoverse2.MoveFileTypes.EAN));
        }
        
        public void AddVoxAcbFile(ACB_Wrapper file, string chara, string language, string path)
        {
            int index = VoxAcbFile.IndexOf(VoxAcbFile.FirstOrDefault(x => x.Arg0 == chara && x.Arg1 == language));
            if (index != -1)
                VoxAcbFile[index] = new Xv2File<ACB_Wrapper>(file, path, false, chara, language, Xenoverse2.MoveFileTypes.VOX_ACB);
            else
                VoxAcbFile.Add(new Xv2File<ACB_Wrapper>(file, path, false, chara, language, Xenoverse2.MoveFileTypes.VOX_ACB));
        }
        #endregion

        #region Get
        public EAN_File GetCamEanFile(string chara, bool charaUnique)
        {
            if (charaUnique)
            {
                var file = CamEanFile.FirstOrDefault(x => x.Arg0 == chara);
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
                var file = EanFile.FirstOrDefault(x => x.Arg0 == chara);
                return (file != null) ? file.File : GetDefaultEanFile();
            }
            else
            {
                return GetDefaultOrFirstEanFile();
            }
        }

        public ACB_Wrapper GetVoxFile(string chara, string language)
        {
            var file = VoxAcbFile.FirstOrDefault(x => x.Arg0 == chara && x.Arg1 == language);
            return (file != null) ? file.File : null;
        }

        private EAN_File GetDefaultEanFile()
        {
            foreach (var ean in EanFile)
                if (string.IsNullOrWhiteSpace(ean.Arg0)) return ean.File;
            return null;
        }

        private EAN_File GetDefaultCamEanFile()
        {
            foreach (var ean in CamEanFile)
                if (string.IsNullOrWhiteSpace(ean.Arg0)) return ean.File;
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
            UpdateTypeString(SeAcbFile, Xenoverse2.MoveFileTypes.SE_ACB);
            UpdateTypeString(BasFile, Xenoverse2.MoveFileTypes.BAS);
            UpdateTypeString(AfterBacFile, Xenoverse2.MoveFileTypes.AFTER_BAC);
            UpdateTypeString(AfterBcmFile, Xenoverse2.MoveFileTypes.AFTER_BCM);

            foreach(var file in VoxAcbFile)
                UpdateTypeString(file, Xenoverse2.MoveFileTypes.VOX_ACB);

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
            if (SeAcbFile == xv2FileObject) return SeAcbFile.UnborrowFile();
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

            return undos;
        }

        public List<IUndoRedo> ReplaceFile(object xv2FileObject, string path)
        {
            if (BacFile == xv2FileObject) return BacFile.ReplaceFile(path);
            if (BcmFile == xv2FileObject) return BcmFile.ReplaceFile(path);
            if (BsaFile == xv2FileObject) return BsaFile.ReplaceFile(path);
            if (ShotBdmFile == xv2FileObject) return ShotBdmFile.ReplaceFile(path);
            if (BdmFile == xv2FileObject) return BdmFile.ReplaceFile(path);
            if (SeAcbFile == xv2FileObject) return SeAcbFile.ReplaceFile(path);
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

            return undos;
        }
        
        public bool CanRemoveFile(object file)
        {
            return (bool)file.GetType().GetMethod("IsOptionalFile").Invoke(file, null);
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
        public string EanDisplayName { get { return (string.IsNullOrWhiteSpace(Arg0)) ? "Main" : Arg0; } }
        
        public string Arg0_Wrapper
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Arg0)) return "Default";
                return Arg0;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(Arg0))
                    Arg0 = value;
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

        //Arguments
        public string Arg0 { get; set; } = string.Empty; //Used to specify character code for vox acb and ean files, if any
        public string Arg1 { get; set; } = string.Empty; //Used to specify language of vox acb files (en = english, else jap)

        //Helpers
        public bool IsEnglish { get { return (Arg1 == "en"); } set { Arg1 = (value) ? "en" : ""; } }
        public bool IsDefault { get { return string.IsNullOrWhiteSpace(Arg0); } }

        public Xv2File(T file, string path, bool borrowed, string arg0 = null, string arg1 = null, Xenoverse2.MoveFileTypes fileType = 0)
        {
            File = file;
            Path = path;
            Borrowed = borrowed;
            Arg0 = arg0;
            Arg1 = arg1;
            FileType = fileType;
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
                newFile = EAN_File.Load(path);
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
            NotifyPropertyChanged(nameof(Arg0));
            NotifyPropertyChanged(nameof(Arg1));
            NotifyPropertyChanged(nameof(EanDisplayName));
            NotifyPropertyChanged(nameof(IsEnglish));
            NotifyPropertyChanged(nameof(Arg0_Wrapper));
            NotifyPropertyChanged(nameof(File));
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
