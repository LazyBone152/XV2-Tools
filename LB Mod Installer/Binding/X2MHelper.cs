using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IniParser;

namespace LB_Mod_Installer.Binding
{
    public class X2MHelper
    {
        public enum SkillType
        {
            Super,
            Ultimate,
            Evasive,
            Blast,
            Awoken
        }
        
        public X2MHelper()
        {
        }

        public int GetX2MSkillID1(string guid, IdBindingManager.CusSkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (int)ret[0];
            }

            return IdBindingManager.NullTokenInt;
        }

        public int GetX2MSkillID2(string guid, IdBindingManager.CusSkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (int)ret[1];
            }

            return IdBindingManager.NullTokenInt;
        }
        
        public string GetX2MSkillShortName(string guid, IdBindingManager.CusSkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (string)ret[2];
            }

            return null;
        }

        public string GetX2MSkillPath(string guid, IdBindingManager.CusSkillType skillType, IdBindingManager.SkillFileType skillFileType)
        {
            //added 2 extra params to the returned object
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {

                switch (skillFileType)
                {
                    case IdBindingManager.SkillFileType.BAC:
                        return string.Format(@"skill/{0}/{1}/{1}.bac", (string)ret[3], (string)ret[4]);
                    case IdBindingManager.SkillFileType.BDM:
                        return string.Format(@"skill/{0}/{1}/{1}.bdm", (string)ret[3], (string)ret[4]);
                    case IdBindingManager.SkillFileType.ShotBDM:
                        return string.Format(@"skill/{0}/{1}/{1}.shot.bdm", (string)ret[3], (string)ret[4]);

                }
                
            }

            return null;
        }
        private object[] FindX2MSkill(string guid, IdBindingManager.CusSkillType skillType)
        {
            try
            {
                //Init
                bool foundSkill = false;
                int ID1 = -1;
                int ID2 = -1;
                string ShortName = String.Empty;

                //for GetX2MSkillPath
                string FolderName = String.Empty;
                string SkillType = String.Empty;

                //Determine the directory to look in
                string directory = String.Empty;

                switch (skillType)
                {
                    case IdBindingManager.CusSkillType.Super:
                        directory = Path.GetFullPath(String.Format("{0}/skill/SPA", GeneralInfo.GameDataFolder));
                        break;
                    case IdBindingManager.CusSkillType.Ultimate:
                        directory = Path.GetFullPath(String.Format("{0}/skill/ULT", GeneralInfo.GameDataFolder));
                        break;
                    case IdBindingManager.CusSkillType.Evasive:
                        directory = Path.GetFullPath(String.Format("{0}/skill/ESC", GeneralInfo.GameDataFolder));
                        break;
                    case IdBindingManager.CusSkillType.Blast:
                        directory = Path.GetFullPath(String.Format("{0}/skill/BLT", GeneralInfo.GameDataFolder));
                        break;
                    case IdBindingManager.CusSkillType.Awoken:
                        directory = Path.GetFullPath(String.Format("{0}/skill/MET", GeneralInfo.GameDataFolder));
                        break;
                }

                //Search directory
                string[] directories = Directory.GetDirectories(directory);

                foreach (var d in directories)
                {
                    string iniFilePath = Path.GetFullPath(String.Format("{0}/X2M_SKILL.ini", d));

                    if (File.Exists(iniFilePath))
                    {

                        StreamIniDataParser ini = new StreamIniDataParser();
                        ini.Parser.Configuration.CommentString = "#";
                        var iniData = ini.ReadData(new StreamReader(iniFilePath));
                        string iniGuid = iniData["General"]["GUID"].ToString().Trim('"');
                        
                        if (guid == iniGuid)
                        {
                            //Skill found
                             string name = new DirectoryInfo(d).Name;
                            
                            //Get folder name to return for GetX2MSkillPath
                            FolderName = name;

                            //Getting IDs
                            ID2 = int.Parse(name.Split('_')[0]);
                            ShortName = name.Split('_')[2];

                            switch (skillType)
                            {
                                case IdBindingManager.CusSkillType.Super:
                                    ID1 = ID2;
                                    SkillType = "SPA";
                                    break;
                                case IdBindingManager.CusSkillType.Ultimate:
                                    ID1 = ID2 + 5000;
                                    SkillType = "ULT";
                                    break;
                                case IdBindingManager.CusSkillType.Evasive:
                                    ID1 = ID2 + 10000;
                                    SkillType = "ESC";
                                    break;
                                case IdBindingManager.CusSkillType.Blast:
                                    ID1 = ID2 + 20000;
                                    SkillType = "BLT";
                                    break;
                                case IdBindingManager.CusSkillType.Awoken:
                                    ID1 = ID2 + 25000;
                                    SkillType = "MET";
                                    break;
                            }

                            //Setting bool
                            foundSkill = true;
                            break;
                        }
                    }
                }

                //Return
                if (foundSkill)
                {
                    return new object[5] { ID1, ID2, ShortName , SkillType, FolderName };
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
