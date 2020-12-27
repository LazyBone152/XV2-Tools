using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;

namespace AudioCueEditor.Settings
{
    public enum AppTheme
    {
        Light,
        Dark,
        WindowsDefault
    }

    [YAXSerializeAs("Settings")]
    public class AppSettings : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public bool ValidGameDir
        {
            get
            {
                return (File.Exists(String.Format("{0}/bin/DBXV2.exe", GameDirectory)));
            }
        }

        #region Values
        private string _gameDir = null;
        private AppTheme _currentTheme = AppTheme.WindowsDefault;
        #endregion
        public string GameDirectory
        {
            get
            {
                return this._gameDir;
            }
            set
            {
                if (value != this._gameDir)
                {
                    this._gameDir = value;
                    NotifyPropertyChanged(nameof(GameDirectory));
                }
            }
        }

        public bool UseLightTheme
        {
            get
            {
                return _currentTheme == AppTheme.Light;
            }
            set
            {
                if (value)
                    _currentTheme = AppTheme.Light;

                NotifyPropertyChanged(nameof(UseLightTheme));
            }
        }
        public bool UseDarkTheme
        {
            get
            {
                return _currentTheme == AppTheme.Dark;
            }
            set
            {
                if (value)
                    _currentTheme = AppTheme.Dark;

                NotifyPropertyChanged(nameof(UseDarkTheme));
            }
        }
        public bool UseWindowsTheme
        {
            get
            {
                return _currentTheme == AppTheme.WindowsDefault;
            }
            set
            {
                if (value)
                    _currentTheme = AppTheme.WindowsDefault;

                NotifyPropertyChanged(nameof(UseWindowsTheme));
            }
        }



        public static AppSettings LoadSettings()
        {
            try
            {
                //Try to load the settings
                YAXSerializer serializer = new YAXSerializer(typeof(AppSettings), YAXSerializationOptions.DontSerializeNullObjects);
                var settings = (AppSettings)serializer.DeserializeFromFile(GeneralInfo.SETTINGS_PATH);
                settings.InitSettings();
                return settings;
            }
            catch
            {
                //If it fails, create a new instance and save it to disk.
                var newSettings = new AppSettings()
                {
                };

                newSettings.InitSettings();
                newSettings.SaveSettings();

                return newSettings;
            }

        }

        public void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(GeneralInfo.SETTINGS_PATH)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(GeneralInfo.SETTINGS_PATH));
                }

                YAXSerializer serializer = new YAXSerializer(typeof(AppSettings));
                serializer.SerializeToFile(this, GeneralInfo.SETTINGS_PATH);
            }
            catch
            {
                MessageBox.Show("Failed to save settings.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitSettings()
        {
            if (string.IsNullOrWhiteSpace(GameDirectory) || !ValidGameDir)
            {
                GameDirectory = FindGameDirectory();
            }

        }

        private string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            foreach (var letter in alphabet)
            {
                string _path1 = String.Format(@"{0}:{1}Program Files (x86){1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);
                string _path2 = String.Format(@"{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);
                string _path3 = String.Format(@"{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);

                if (File.Exists(_path1))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path1));
                }
                else if (File.Exists(_path2))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path2));
                }
                else if (File.Exists(_path3))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path3));
                }
            }

            return null;
        }

        public AppTheme GetCurrentTheme()
        {
            return _currentTheme;
        }
    }
}
