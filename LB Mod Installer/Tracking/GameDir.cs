using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;

namespace LB_Mod_Installer.Settings
{
    public class GameDirXml : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        public string GameDirectory { get; set; } 

        public static GameDirXml LoadSettings()
        {
            try
            {
                //Try to load the settings
                YAXSerializer serializer = new YAXSerializer(typeof(GameDirXml), YAXSerializationOptions.DontSerializeNullObjects);
                return (GameDirXml)serializer.DeserializeFromFile(GeneralInfo.SettingsPath);
            }
            catch
            {
                //If it fails, create a new instance and save it to disk.
                var newSettings = new GameDirXml()
                {
                    GameDirectory = null
                };

                newSettings.SaveSettings();

                return newSettings;
            }

        }

        public void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(GeneralInfo.SettingsPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(GeneralInfo.SettingsPath));
                }

                YAXSerializer serializer = new YAXSerializer(typeof(GameDirXml));
                serializer.SerializeToFile(this, GeneralInfo.SettingsPath);
            }
            catch
            {

            }
        }

    }
}
