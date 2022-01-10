using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.MSG;
using Xv2CoreLib.ERS;
using System.Collections.ObjectModel;
using System.IO;
using Xv2CoreLib;

namespace EEPK_Organiser
{
    public class LoadFromGameHelper
    {
        private const string ERS_PATH = "vfx/vfx_spec.ers";
        private const string CUS_PATH = "system/custom_skill.cus";
        private const string CMS_PATH = "system/char_model_spec.cms";
        private const string CHARACTER_MSG_PATH = "msg/proper_noun_character_name_en.msg";

        public ObservableCollection<GameEntity> characters { get; set; }
        public ObservableCollection<GameEntity> superSkills { get; set; }
        public ObservableCollection<GameEntity> ultimateSkills { get; set; }
        public ObservableCollection<GameEntity> evasiveSkills { get; set; }
        public ObservableCollection<GameEntity> blastSkills { get; set; }
        public ObservableCollection<GameEntity> awokenSkills { get; set; }
        public ObservableCollection<GameEntity> cmn { get; set; }
        public ObservableCollection<GameEntity> demo { get; set; }

        public LoadFromGameHelper()
        {
            CUS_File cusFile = (CUS_File)FileManager.Instance.GetParsedFileFromGame(CUS_PATH);
            CMS_File cmsFile = (CMS_File)FileManager.Instance.GetParsedFileFromGame(CMS_PATH);
            ERS_File ersFile = (ERS_File)FileManager.Instance.GetParsedFileFromGame(ERS_PATH);

            characters = LoadCharacterNames(cmsFile, ersFile);
            superSkills = LoadSkillNames(CUS_File.SkillType.Super, cusFile, cmsFile);
            ultimateSkills = LoadSkillNames(CUS_File.SkillType.Ultimate, cusFile, cmsFile);
            evasiveSkills = LoadSkillNames(CUS_File.SkillType.Evasive, cusFile, cmsFile);
            blastSkills = LoadSkillNames(CUS_File.SkillType.Blast, cusFile, cmsFile);
            awokenSkills = LoadSkillNames(CUS_File.SkillType.Awoken, cusFile, cmsFile);
            cmn = LoadCmnNames(ersFile);
            demo = LoadDemoNames(ersFile);
        }

        private ObservableCollection<GameEntity> LoadCharacterNames(CMS_File cmsFile, ERS_File ersFile)
        {
            ObservableCollection<GameEntity> entities = new ObservableCollection<GameEntity>();

            MSG_File characterMsgFile = (MSG_File)FileManager.Instance.GetParsedFileFromGame(CHARACTER_MSG_PATH);

            foreach(var ersEntry in ersFile.GetSubentryList(2))
            {
                CMS_Entry cmsEntry = cmsFile.GetEntry(ersEntry.Index);
                string name = null;
                string eepkPath = string.Format("vfx/{0}", ersEntry.FILE_PATH);

                if(cmsEntry != null)
                {
                    name = characterMsgFile.GetCharacterName(cmsEntry.Str_04);


                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = String.Format("??? ({0} / {1})", cmsEntry.Index, cmsEntry.Str_04);
                    }
                }
                

                entities.Add(new GameEntity()
                {
                    Name = name,
                    EepkPath = eepkPath
                });
            }

            return entities;
        }
        
        private ObservableCollection<GameEntity> LoadSkillNames(CUS_File.SkillType skillType, CUS_File cusFile, CMS_File cmsFile)
        {
            ObservableCollection<GameEntity> entities = new ObservableCollection<GameEntity>();
            
            MSG_File nameMsgFile = (skillType != CUS_File.SkillType.Blast) ? (MSG_File)FileManager.Instance.GetParsedFileFromGame(string.Format("{0}en.msg", cusFile.GetNameMsgPath(skillType))) : null;
            string skillDir;

            List<Skill> skills;

            switch (skillType)
            {
                case CUS_File.SkillType.Super:
                    skills = cusFile.SuperSkills;
                    skillDir = "skill/SPA";
                    break;
                case CUS_File.SkillType.Ultimate:
                    skills = cusFile.UltimateSkills;
                    skillDir = "skill/ULT";
                    break;
                case CUS_File.SkillType.Evasive:
                    skills = cusFile.EvasiveSkills;
                    skillDir = "skill/ESC";
                    break;
                case CUS_File.SkillType.Blast:
                    skills = cusFile.BlastSkills;
                    skillDir = "skill/BLT";
                    break;
                case CUS_File.SkillType.Awoken:
                    skills = cusFile.AwokenSkills;
                    skillDir = "skill/MET";
                    break;
                default:
                    throw new InvalidDataException("LoadSkillNames: unknown skillType = " + skillType);
            }
            
            foreach(var skill in skills)
            {
                if (skill.FilesLoadedFlags1.HasFlag(Skill.FilesLoadedFlags.Eepk))
                {
                    string eepkPath;

                    if(skill.EepkPath == "NULL")
                    {
                        //Get skill folder and files name
                        int skillID2 = skill.ID2;
                        int cmsId = (int)Math.Floor(skillID2 / 10f);
                        string charaShortName = cmsFile.GetEntry(cmsId.ToString()).Str_04;

                        //If chara ID belongs to a CAC, the skill is tagged as CMN instead.
                        if (cmsId >= 100 && cmsId < 109)
                        {
                            charaShortName = "CMN";
                        }

                        string folderName = String.Format("{0}_{1}_{2}", skillID2.ToString("D3"), charaShortName, skill.ShortName);
                        eepkPath = String.Format("{0}/{1}/{1}.eepk", skillDir, folderName);
                    }
                    else
                    {
                        //This skill uses another skills EEPK, so we dont have to calculate its folder name
                        eepkPath = String.Format("skill/{0}/{1}.eepk", skill.EepkPath, Path.GetFileName(skill.EepkPath));
                    }

                    //Get skill name
                    string name = null;

                    if (skillType != CUS_File.SkillType.Blast)
                    {
                        name = nameMsgFile.GetSkillName(skill.ID2, skillType);
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = string.Format("??? ({0} / {1})", skill.ID2, skill.ShortName);
                    }

                    entities.Add(new GameEntity()
                    {
                        Name = name,
                        EepkPath = eepkPath
                    });
                }
            }
            return entities;
        }

        private ObservableCollection<GameEntity> LoadCmnNames(ERS_File ersFile)
        {
            ObservableCollection<GameEntity> entities = new ObservableCollection<GameEntity>();

            foreach(var entry in ersFile.GetSubentryList(0))
            {
                entities.Add(new GameEntity()
                {
                    Name = Path.GetFileNameWithoutExtension(entry.FILE_PATH),
                    EepkPath = String.Format("vfx/{0}", entry.FILE_PATH)
                });
            }

            return entities;
        }

        private ObservableCollection<GameEntity> LoadDemoNames(ERS_File ersFile)
        {
            ObservableCollection<GameEntity> entities = new ObservableCollection<GameEntity>();

            foreach (var entry in ersFile.GetSubentryList(4))
            {
                entities.Add(new GameEntity()
                {
                    Name = Path.GetFileNameWithoutExtension(entry.FILE_PATH),
                    EepkPath = String.Format("vfx/{0}", entry.FILE_PATH)
                });
            }

            return entities;
        }


    }

    public class GameEntity
    {
        public string Name { get; set; }
        public string EepkPath { get; set; }
        public string ToolTip
        {
            get
            {
                return Path.GetFileName(EepkPath);
            }
        }
    }
}
