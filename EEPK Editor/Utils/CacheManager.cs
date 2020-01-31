using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser.Utils
{
    public class CacheManager
    {
        public ObservableCollection<CachedFile> CachedFiles { get; set; } = new ObservableCollection<CachedFile>();

        public CacheManager()
        {

        }

        
        public void CacheFile(string path, EffectContainerFile file, string source)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            path = path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/").ToLower();

            //If file is already cached, then remove it
            if (IsFileCached(path))
            {
                if (!RemoveCachedFile(path))
                {
                    throw new Exception(String.Format("CacheFile: Cannot remove existing cached file instance for \"{0}\".", path));
                }
            }

            if (GeneralInfo.CacheFileLimit == 0)
            {
                return; //caching is disabled
            }
            else if(GeneralInfo.CacheFileLimit > 0)
            {
                if (CachedFiles.Count >= GeneralInfo.CacheFileLimit)
                {
                    CachedFiles.RemoveAt(0);
                    
                    //CachedFiles.RemoveRange(0, CachedFiles.Count - GeneralInfo.CacheFileLimit + 1);
                }

                CachedFiles.Add(new CachedFile()
                {
                    Path = path,
                    effectContainerFile = file,
                    Name = name,
                    Source = source
                });
            }
        }

        public bool IsFileCached(string path)
        {
            path = path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/").ToLower();

            foreach (var file in CachedFiles)
            {
                if (file.Path == path) return true;
            }
            return false;
        }

        public EffectContainerFile GetCachedFile(string path)
        {
            path = path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/").ToLower();

            foreach (var file in CachedFiles)
            {
                if (file.Path == path) return file.effectContainerFile;
            }
            return null;
        }

        public bool RemoveCachedFile(string path)
        {
            path = path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/").ToLower();

            for (int i = 0; i < CachedFiles.Count; i++)
            {
                if (CachedFiles[i].Path == path)
                {
                    CachedFiles.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }

    public class CachedFile
    {
        public string Source { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public EffectContainerFile effectContainerFile { get; set; }

        public string DisplayName
        {
            get
            {
                return string.Format("_[{0}] {1}", Source, Name);
            }
        }
    }
}
