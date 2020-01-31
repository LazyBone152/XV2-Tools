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

        private object[] FindX2MSkill(string guid, IdBindingManager.CusSkillType skillType)
        {
            try
            {
                //Init
                bool foundSkill = false;
                int ID1 = -1;
                int ID2 = -1;
                string ShortName = String.Empty;

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

                            //Getting IDs
                            ID2 = int.Parse(name.Split('_')[0]);
                            ShortName = name.Split('_')[2];

                            switch (skillType)
                            {
                                case IdBindingManager.CusSkillType.Super:
                                    ID1 = ID2;
                                    break;
                                case IdBindingManager.CusSkillType.Ultimate:
                                    ID1 = ID2 + 5000;
                                    break;
                                case IdBindingManager.CusSkillType.Evasive:
                                    ID1 = ID2 + 10000;
                                    break;
                                case IdBindingManager.CusSkillType.Blast:
                                    ID1 = ID2 + 20000;
                                    break;
                                case IdBindingManager.CusSkillType.Awoken:
                                    ID1 = ID2 + 25000;
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
                    return new object[3] { ID1, ID2, ShortName };
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
