using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using IniParser;
using LB_Mod_Installer.Installer;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.Eternity;
using Xv2CoreLib.Resource;

namespace LB_Mod_Installer.Binding
{
    public class X2MHelper
    {

        public enum X2MType
        {
            NOT_FOUND,
            REPLACER,
            NEW_CHARACTER,
            NEW_SKILL,
            NEW_COSTUME,
            NEW_STAGE,
            NEW_QUEST
        }

        private string XV2INS_INSTALLED_MODS
        {
            get
            {
                //return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/XV2INS/Installed";
                return $"{GeneralInfo.GameDataFolder}/InstallData"; //New X2M installed mod location
            }
        }
        private const string X2M_XML = "x2m.xml";

        private Install install;

        public X2MHelper(Install install)
        {
            this.install = install;
        }

        public bool IsModInstalled(string guid)
        {
            return File.Exists($"{XV2INS_INSTALLED_MODS}/{guid.ToLower()}.x2d");
        }

        private XDocument GetInstalledModXml(string guid)
        {
            if (IsModInstalled(guid))
            {
                using (ZipReader x2m = new ZipReader(ZipFile.Open($"{XV2INS_INSTALLED_MODS}/{guid.ToLower()}.x2d", ZipArchiveMode.Read)))
                {
                    return x2m.GetXmlDocumentFromArchive(X2M_XML);
                }
            }

            return null;
        }

        private X2MType GetX2MType(XDocument xml)
        {
            XAttribute atr = xml.Root.Attribute("type");

            if (atr != null)
            {
                return (X2MType)Enum.Parse(typeof(X2MType), atr.Value);
            }

            return X2MType.NOT_FOUND;
        }

        #region Stage
        public int GetStageId(string guid, bool returnSsid)
        {
            string code = guid;

            if (guid.Length > 5)
            {
                //Is guid. Find X2M to get the stage code.
                XDocument xml = GetInstalledModXml(guid);
                if (xml == null) return BindingManager.NullTokenInt;

                X2MType type = GetX2MType(xml);

                if (type == X2MType.NEW_STAGE)
                {
                    bool found = false;
                    foreach (var attr in xml.Root.Element("Stage").Descendants("CODE").Attributes())
                    {
                        if (attr.Name == "value")
                        {
                            code = attr.Value;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return BindingManager.NullTokenInt;
                }
                else
                {
                    return BindingManager.NullTokenInt;
                }
            }

            StageDefFile stageDefs = (StageDefFile)install.GetParsedFile<StageDefFile>(StageDefFile.PATH);
            StageDef stageDef = stageDefs.Stages.FirstOrDefault(x => x.CODE.Equals(guid, StringComparison.OrdinalIgnoreCase));

            if (stageDef != null)
            {
                if (stageDef.SSID == ushort.MaxValue && returnSsid)
                    return BindingManager.NullTokenInt;

                return returnSsid ? stageDef.SSID : (int)stageDef.Index;
            }

            return BindingManager.NullTokenInt;
        }

        #endregion

        #region Character
        public string GetX2MCharaCode(string guid)
        {
            XDocument xml = GetInstalledModXml(guid);

            if (xml != null)
            {
                X2MType type = GetX2MType(xml);

                if (type == X2MType.NEW_CHARACTER)
                {
                    foreach (var attr in xml.Root.Descendants("ENTRY_NAME").Attributes())
                    {
                        if (attr.Name == "value")
                            return attr.Value;
                    }
                }
            }

            return BindingManager.NullTokenStr;
        }

        public int GetX2MCharaID(string guid)
        {
            XDocument xml = GetInstalledModXml(guid);

            if (xml != null)
            {
                string code = GetX2MCharaCode(guid);

                if (code != BindingManager.NullTokenStr)
                {
                    CMS_File cms = (CMS_File)install.GetParsedFile<CMS_File>(BindingManager.CMS_PATH, false, true);

                    CMS_Entry entry = cms.CMS_Entries.FirstOrDefault(x => x.ShortName == code);

                    return (entry != null) ? entry.ID : BindingManager.NullTokenInt;
                }
            }

            return BindingManager.NullTokenInt;
        }
        #endregion

        #region Skill
        public int GetX2MSkillID1(string guid, CUS_File.SkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (int)ret[0];
            }

            return BindingManager.NullTokenInt;
        }

        public int GetX2MSkillID2(string guid, CUS_File.SkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (int)ret[1];
            }

            return BindingManager.NullTokenInt;
        }

        public string GetX2MSkillShortName(string guid, CUS_File.SkillType skillType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {
                return (string)ret[2];
            }

            return null;
        }

        public string GetX2MSkillPath(string guid, CUS_File.SkillType skillType, SkillFileType skillFileType)
        {
            object[] ret = FindX2MSkill(guid, skillType);

            if (ret != null)
            {

                switch (skillFileType)
                {
                    case SkillFileType.BAC:
                        return string.Format(@"skill/{0}/{1}/{1}.bac", (string)ret[3], (string)ret[4]);
                    case SkillFileType.BSA:
                        return string.Format(@"skill/{0}/{1}/{1}.bsa", (string)ret[3], (string)ret[4]);
                    case SkillFileType.BDM:
                        return string.Format(@"skill/{0}/{1}/{1}_PLAYER.bdm", (string)ret[3], (string)ret[4]);
                    case SkillFileType.ShotBDM:
                        return string.Format(@"skill/{0}/{1}/{1}_PLAYER.shot.bdm", (string)ret[3], (string)ret[4]);

                }

            }

            return null;
        }

        private object[] FindX2MSkill(string guid, CUS_File.SkillType skillType)
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
                    case CUS_File.SkillType.Super:
                        directory = Path.GetFullPath(String.Format("{0}/skill/SPA", GeneralInfo.GameDataFolder));
                        break;
                    case CUS_File.SkillType.Ultimate:
                        directory = Path.GetFullPath(String.Format("{0}/skill/ULT", GeneralInfo.GameDataFolder));
                        break;
                    case CUS_File.SkillType.Evasive:
                        directory = Path.GetFullPath(String.Format("{0}/skill/ESC", GeneralInfo.GameDataFolder));
                        break;
                    case CUS_File.SkillType.Blast:
                        directory = Path.GetFullPath(String.Format("{0}/skill/BLT", GeneralInfo.GameDataFolder));
                        break;
                    case CUS_File.SkillType.Awoken:
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
                                case CUS_File.SkillType.Super:
                                    ID1 = ID2;
                                    SkillType = "SPA";
                                    break;
                                case CUS_File.SkillType.Ultimate:
                                    ID1 = ID2 + 5000;
                                    SkillType = "ULT";
                                    break;
                                case CUS_File.SkillType.Evasive:
                                    ID1 = ID2 + 10000;
                                    SkillType = "ESC";
                                    break;
                                case CUS_File.SkillType.Blast:
                                    ID1 = ID2 + 20000;
                                    SkillType = "BLT";
                                    break;
                                case CUS_File.SkillType.Awoken:
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
                    return new object[5] { ID1, ID2, ShortName, SkillType, FolderName };
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

        #endregion
    }
}
