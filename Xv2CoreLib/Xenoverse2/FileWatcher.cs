using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xv2CoreLib
{

    public class FileWatcher
    {
        private List<LastFileWriteTime> files = new List<LastFileWriteTime>();
        
        public void FileLoadedOrSaved(string path)
        {
            lock (files)
            {
                var entry = GetFile(path);

                if (entry == null)
                {
                    entry = new LastFileWriteTime(path);
                    files.Add(entry);
                    return;
                }
                entry.SetCurrentTime();
            }

        }

        public bool WasFileModified(string path)
        {
            lock (files)
            {
                var entry = GetFile(path);
                //if (entry == null) throw new NullReferenceException($"FileWatcher: no LastFileWriteTime entry for file: \"{path}\" was found.");
                if (entry == null) return true;

                return entry.HasBeenModified();
            }
        }

        private LastFileWriteTime GetFile(string path)
        {
            for(int i = 0; i < files.Count; i++)
            {
                if (files[i].FilePath == path) return files[i];
            }

            return null;
        }

        public void ClearAll()
        {
            lock (files)
            {
                files.Clear();
            }
        }
    }

    public class LastFileWriteTime
    {
        public string FilePath { get; set; } //absolute
        /// <summary>
        /// Last time the file was loaded/saved by this application. 
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        public LastFileWriteTime(string filePath)
        {
            FilePath = filePath;
            LastWriteTime = File.GetLastWriteTimeUtc(filePath);
        }

        public void SetCurrentTime()
        {
            LastWriteTime = DateTime.UtcNow;
        }

        public bool HasBeenModified()
        {
            return File.GetLastWriteTimeUtc(FilePath) > LastWriteTime;
        }
    }
}
