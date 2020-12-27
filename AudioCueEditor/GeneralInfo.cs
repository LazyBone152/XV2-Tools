using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioCueEditor
{
    public static class GeneralInfo
    {
        public static string SETTINGS_PATH = GetAbsolutePathRelativeToExe("ace_settings.xml");
        public static string ERROR_LOG_PATH = GetAbsolutePathRelativeToExe("ace_error_log.txt");
        public static Settings.AppSettings AppSettings = null;
        public static readonly string AppName = String.Format("ACE ({0})", CurrentVersionString);
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


        public static string GetAbsolutePathRelativeToExe(string relativePath)
        {
            return String.Format("{0}/{1}", Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), relativePath);
        }

    }
}
