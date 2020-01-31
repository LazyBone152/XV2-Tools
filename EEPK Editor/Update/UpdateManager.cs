using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace EEPK_Organiser.Update
{
    public struct UpdateInfo
    {
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        public string Version { get; set; }
        [YAXAttributeFor("DownloadPath")]
        [YAXSerializeAs("url")]
        public string DownloadUrl { get; set; }
        [YAXAttributeFor("Changelog")]
        [YAXSerializeAs("str")]
        public string Changelog { get; set; }

        public Version GetVersion()
        {
            return new Version(Version);
        }
        
    }

    public class UpdateManager
    {
        private const string updateServer = "https://dl.dropboxusercontent.com/s/9lrrj6841hdzl2a/updateinfo.xml?dl=0";

        public bool UpdateCheckComplete { get; private set; } = false;
        public bool UpdateAvailable { get; private set; }
        public bool GotResponseFromServer { get; private set; }
        public UpdateInfo updateInfo { get; private set; }

        public UpdateManager()
        {
            
        }

        public void InitUpdateCheck()
        {
            updateInfo = DownloadFile();
            if (GotResponseFromServer)
            {
                VersionCheck();
            }
            UpdateCheckComplete = true;
        }

        private UpdateInfo DownloadFile()
        {
            //Get the temp file path and ensure the directory exists
            string localPath = Path.GetFullPath(String.Format("{0}/{1}/updateinfo.xml", Path.GetTempPath(), GeneralInfo.AppName));
            if (!Directory.Exists(Path.GetDirectoryName(localPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            }

            //Download file
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(updateServer, localPath);
                }
            }
            catch
            {
                GotResponseFromServer = false;
                return new UpdateInfo();
            }
            

            //Load the xml
            try
            {
                YAXSerializer serializer = new YAXSerializer(typeof(UpdateInfo), YAXSerializationOptions.DontSerializeNullObjects);
                var xml = (UpdateInfo)serializer.DeserializeFromFile(localPath);
                GotResponseFromServer = true;

                //Delete the xml
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }

                //Return the file
                return xml;
            }
            catch
            {
                GotResponseFromServer = false;
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
                return new UpdateInfo();
            }
            

            
        }

        private void VersionCheck()
        {
            Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Version updateVersion = updateInfo.GetVersion();

            if(updateVersion > currentVersion)
            {
                UpdateAvailable = true;
            }
            else
            {
                UpdateAvailable = false;
            }
        }

    }
}
