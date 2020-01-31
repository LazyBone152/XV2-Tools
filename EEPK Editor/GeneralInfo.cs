using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEPK_Organiser.Settings;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser
{
    public static class GeneralInfo
    {
        public static string SETTINGS_PATH = GetAbsolutePathRelativeToExe("eepk_tool/settings.xml");
        public static string NAMELIST_DIR_PATH = GetAbsolutePathRelativeToExe("eepk_tool/namelist");
        public static string ERROR_LOG_PATH = GetAbsolutePathRelativeToExe("eepk_tool/error_log.txt");
        public static Settings.AppSettings AppSettings = null;
        public static readonly string AppName = String.Format("EEPK Organiser ({0})", CurrentVersionString);
        public static string CurrentVersionString
        {
            get
            {
                string[] split = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                if (split[2] == "0" && split[3] == "0")
                {
                    return String.Format("{0}.{1}", split[0], split[1]);
                }
                else if (split[3] == "0")
                {
                    return String.Format("{0}.{1}{2}", split[0], split[1], split[2]);
                }
                else
                {
                    return String.Format("{0}.{1}{2}{3}", split[0], split[1], split[2], split[3]);
                }
            }
        }
        public static Version CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        //Settings
        public static bool UseExistingAssetBasedOnName
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.AssetReuse_NameMatch;
            }
        }
        public static bool UseExistingTexturesBasedOnName
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.TextureReuse_NameMatch;
            }
        }
        public static bool LoadTextures
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.LoadTextures;
            }
        }
        public static bool FileCleanUp_Ignore
        {
            get
            {
                if (AppSettings == null) return false;
                return AppSettings.FileCleanUp_Ignore;
            }
        }
        public static bool FileCleanUp_Prompt
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.FileCleanUp_Prompt;
            }
        }
        public static bool FileCleanUp_Delete
        {
            get
            {
                if (AppSettings == null) return false;
                return AppSettings.FileCleanUp_Delete;
            }
        }
        public static bool CheckForUpdatesOnStartUp
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.UpdateNotifications;
            }
        }
        public static bool AutoContainerRename
        {
            get
            {
                if (AppSettings == null) return true;
                return AppSettings.AutoContainerRename;
            }
        }
        public static int CacheFileLimit
        {
            get
            {
                if (AppSettings == null) return 3;
                return AppSettings.FileCacheLimit;
            }
        }


        public static string GetAbsolutePathRelativeToExe(string relativePath)
        {
            return String.Format("{0}/{1}", Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), relativePath);
        }
        
        public static void UpdateEepkToolInterlop()
        {
            EepkToolInterlop.TextureImportMatchNames = UseExistingTexturesBasedOnName;
            EepkToolInterlop.LoadTextures = LoadTextures;
            EepkToolInterlop.AutoRenameContainers = AutoContainerRename;
            EepkToolInterlop.AssetReuseMatchName = UseExistingAssetBasedOnName;
        }
    }
}
