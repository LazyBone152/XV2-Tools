using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using YAXLib;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EffectContainer
{
    [Serializable]
    public abstract class EffectPackage
    {
        //A ZIP file containing an EEPK and its assets
        public const string EXT = "vfx2";

        public static EffectContainerFile LoadEffectPackage(string path)
        {
            using (ZipReader reader = new ZipReader(ZipFile.Open(path, ZipArchiveMode.Read)))
            {
                string newPath = string.Format("{0}.eepk", Path.GetFileNameWithoutExtension(path));
                return EffectContainerFile.Load(newPath, reader);
            }
        }

        public static void SaveEffectPackage(string path)
        {
            using (ZipWriter writer = new ZipWriter(ZipFile.Open(path, ZipArchiveMode.Update)))
            {

            }
        }

    }

    [YAXSerializeAs("InstallInstructions")]
    [Serializable]
    public class EffectPackageInstallInstructions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public TextureInstallTypes textureInstallTypes { get; set; }

        [YAXDontSerialize]
        public bool textureInstallType_NameMatch
        {
            get
            {
                return (textureInstallTypes == TextureInstallTypes.NameMatch);
            }
            set
            {
                textureInstallTypes = TextureInstallTypes.NameMatch;
                NotifyPropertyChanged("textureInstallType_NameMatch");
                NotifyPropertyChanged("textureInstallType_Identical");
                NotifyPropertyChanged("textureInstallTypes");
            }
        }
        [YAXDontSerialize]
        public bool textureInstallType_Identical
        {
            get
            {
                return (textureInstallTypes == TextureInstallTypes.Identical);
            }
            set
            {
                textureInstallTypes = TextureInstallTypes.Identical;
                NotifyPropertyChanged("textureInstallType_NameMatch");
                NotifyPropertyChanged("textureInstallType_Identical");
                NotifyPropertyChanged("textureInstallTypes");
            }
        }

        public ObservableCollection<EffectInstallInstructions> effectInstallInstructions { get; set; } = new ObservableCollection<EffectInstallInstructions>();
    }

    [Serializable]
    public class EffectInstallInstructions
    {
        public ushort ID { get; set; }
        public EffectInstallTypes EffectInstallType { get; set; }

    }

    public enum EffectInstallTypes
    {
        NameMatch, //Assets with same name will be reused
        AlwaysInstallNew //Assets will always be installed (and given a new name if there's an existing one)
    }

    public enum TextureInstallTypes
    {
        NameMatch, 
        Identical 
    }
}
