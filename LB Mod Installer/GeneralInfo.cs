using LB_Mod_Installer.Installer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YAXLib;

namespace LB_Mod_Installer
{
    public static class GeneralInfo
    {
        public enum SpecialFailStates
        {
            None,
            AutoIdBindingFailed,
            BindingFailed, //Missing mod most likely
            X2MNotFound
        }
        public static string AppName
        {
            get
            {
                if (InstallerXmlInfo != null)
                {
                    return InstallerXmlInfo.InstallerNameWithVersion;
                }
                return "LB Mod Installer";
            }
        }
        public static string TrackerPath
        {
            get
            {
                string name = $"{InstallerXmlInfo.Name.ToLower().Trim()}_{InstallerXmlInfo.Author.ToLower().Trim()}";

                //Replace illegal file name characters with _
                name = name.Replace("/", "_").Replace(@"\", "_").Replace("@", "_").Replace(":", "_").Replace("?", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("*", "_");
                return GetPathInGameDir($"LB Mod Installer/{name}.xml");
            }
        }
        public const string InstallerXml = "InstallerXml.xml";
        public static string SettingsPath
        {
            get
            {
                return string.Format("{0}/LB Mod Installer 3/settings.xml", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }
        }

        public static string GameDataFolder
        {
            get
            {
                return string.Format("{0}/data", GameDir);
            }
        }
        public static string GameCpkFolder
        {
            get
            {
                return string.Format("{0}/cpk", GameDir);
            }
        }
        private static string _gameDir = null;
        public static string GameDir
        {
            get
            {
                return _gameDir;
            }
            set
            {
                _gameDir = value;
                IsGameDirValid();
            }
        }

        public static string[] LanguageSuffix = new string[13] { "en.msg", "es.msg", "ca.msg", "fr.msg", "de.msg", "it.msg", "pt.msg", "pl.msg", "ru.msg", "tw.msg", "zh.msg", "kr.msg", "ja.msg" };
        public static List<string> JungleBlacklist = new List<string>()
        {
            "HUM.bcs",
            "HUF.bcs",
            "MAM.bcs",
            "MAF.bcs",
            "FRI.bcs",
            "NMC.bcs",
            "BTL_AURA.eepk",
            "BTL_AURA.pbind.emb",
            "BTL_AURA.ptcl.emb",
            "BTL_AURA.pbind.emm",
            "BTL_AURA.tbind.emb",
            "BTL_AURA.trc.emb",
            "BTL_AURA.tbind.emm",
            "BTL_AURA.cbind.emb",
            "BTL_CMN.eepk",
            "BTL_CMN.pbind.emb",
            "BTL_CMN.ptcl.emb",
            "BTL_CMN.pbind.emm",
            "BTL_CMN.tbind.emb",
            "BTL_CMN.trc.emb",
            "BTL_CMN.tbind.emm",
            "BTL_CMN.cbind.emb",
            "BTL_KDN.eepk",
            "BTL_KDN.pbind.emb",
            "BTL_KDN.ptcl.emb",
            "BTL_KDN.pbind.emm",
            "BTL_KDN.tbind.emb",
            "BTL_KDN.trc.emb",
            "BTL_KDN.tbind.emm",
            "BTL_KDN.cbind.emb",
            "vfx_spec.ers",
            "aura_setting.aur",
            "CameraLimitValue.cml",
            "char_model_spec.cms",
            "chara_sound.cso",
            "custom_skill.cus",
            "parameter_spec_char.psc",
            "powerup_parameter.pup",
            "QuestSort.qsf",
            "special_event_voice.sev",
            "talisman_item.idb",
            "skill_item.idb",
            "costume_top_item.idb",
            "costume_shoes_item.idb",
            "costume_gloves_item.idb",
            "costume_bottom_item.idb",
            "accessory_item.idb",
            "chara_image.hci",
            "CHARASELE.iggy",
            "CHARA01.emb",
            "cmn.bpe",
            "Common.bev",
            "CMN.ean",
            "CMN.cam.ean",
            "CMN.bac",
            "CMN.bdm",
            "MCM.DBA.ean",
            "MCM.TTL.ean",
            "MCM.TU6.ean",
            "combination_skill.cnc",
            "combination_skill.cns",
            "pre-baked.xml",
            "X2M_COSTUME.xml",
            "X2M_SKILL.ini",
            "lobby_event_manage.tsd",
            "lobby_npc_list.tnl",
            "baq_data.qxd",
            "chq_data.qxd",
            "hlq_data.qxd",
            "leq_data.qxd",
            "osq_data.qxd",
            "prb_data.qxd",
            "rbq_data.qxd",
            "tcq_data.qxd",
            "tfb_data.qxd",
            "tmq_data.qxd",
            "tnb_data.qxd",
            "tpq_data.qxd",
            "ttq_data.qxd",
            "DM_CMN.eepk",
            "DM_CMN.cbind.emb",
            "DM_CMN.pbind.emb",
            "LBY_CMN.eepk",
            "LBY_CMN.pbind.emb",
            "LBY_CMN.ptcl.emb",
            "LBY_CMN.pbind.emm",
            "LBY_CMN.tbind.emb",
            "LBY_CMN.trc.emb",
            "LBY_CMN.tbind.emm",
            "LBY_CMN.cbind.emb",
            "TTL.eepk",
            "TTL.pbind.emb",
            "TTL.ptcl.emb",
            "TTL.pbind.emm",
            "TTL.tbind.emb",
            "TTL.trc.emb",
            "TTL.tbind.emm",
            "TTL.cbind.emb",
            "TTL_LBY.eepk",
            "TTL_LBY.pbind.emb",
            "TTL_LBY.ptcl.emb",
            "TTL_LBY.pbind.emm",
            "TTL_LBY.tbind.emb",
            "TTL_LBY.trc.emb",
            "TTL_LBY.tbind.emm",
            "TTL_LBY.cbind.emb",
            "BTL_CMN2.eepk",
            "BTL_CMN2.pbind.emb",
            "BTL_CMN2.ptcl.emb",
            "BTL_CMN2.pbind.emm",
            "BTL_CMN2.tbind.emb",
            "BTL_CMN2.trc.emb",
            "BTL_CMN2.tbind.emm",
            "BTL_CMN2.cbind.emb"

        };
        public static TrackingXml Tracker { get; set; }
        public static SpecialFailStates SpecialFailState = SpecialFailStates.None;
        public static bool isInstalled = false;

        public static CultureInfo SystemCulture;

        //Safe InstallerXml requests
        public static string CurrentModName
        {
            get
            {
                if (InstallerXmlInfo == null) return null;
                return InstallerXmlInfo.Name;
            }
        }
        public static string CurrentModVersion
        {
            get
            {
                if (InstallerXmlInfo == null) return null;
                return InstallerXmlInfo.VersionFormattedString;
            }
        }

        //Safe Tracker requests
        public static string InstalledModVersion
        {
            get
            {
                if (Tracker == null) return null;
                var mod = Tracker.GetCurrentMod();
                if (mod != null) return mod.VersionFormattedString;
                return null;
            }
        }

        public static InstallerXml InstallerXmlInfo { get; set; }

        public static void LoadTracker()
        {
            if (!IsGameDirValid()) return;

            if (File.Exists(TrackerPath))
            {
                try
                {
                    YAXSerializer serializer = new YAXSerializer(typeof(TrackingXml), YAXSerializationOptions.DontSerializeNullObjects);
                    Tracker = (TrackingXml)serializer.DeserializeFromFile(TrackerPath);
                }
                catch
                {
                    Tracker = new TrackingXml();
                }
            }
            else
            {
                Tracker = new TrackingXml();
            }

            //Initializes the tracker
            Tracker.GetCurrentMod();
        }

        public static void SaveTracker()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TrackerPath));

            //Update version
            Tracker.GetCurrentMod().VersionString = InstallerXmlInfo.VersionString;

            //Saving tracker xml
            YAXSerializer serializer = new YAXSerializer(typeof(TrackingXml));
            serializer.SerializeToFile(Tracker, TrackerPath);
        }

        public static void DeleteTracker()
        {
            try
            {
                //Try deleting the tracker
                File.Delete(TrackerPath);
            }
            catch
            {
                //If it fails, try setting mod to null and saving
                try
                {
                    Tracker.Mod = null;
                    SaveTracker();
                }
                catch
                {

                }
            }
        }

        public static bool IsGameDirValid()
        {
            if (File.Exists(String.Format("{0}/bin/DBXV2.exe", GameDir)))
            {
                //In data folder
                if (!Directory.Exists(String.Format("{0}/data", GameDir)))
                {
                    Directory.CreateDirectory((String.Format("{0}/data", GameDir)));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetPathInGameDir(string relativePath)
        {
            return string.Format("{1}/{0}", relativePath, GameDataFolder);
        }

        public static string GetPathInZipDataDir(string relativePath)
        {
            return string.Format("data/{0}", relativePath);
        }
    }
}
