using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.CPK;

namespace Xv2CoreLib.Resource
{
    public class Xv2FileIO : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CPK_Reader cpkReader { get; private set; }
        public bool IsInitialized { get; set; }
        public string GameDir { get; set; }
        private bool AsyncInit { get; set; }
        private string[] CustomLoadOrder = null;

        private Exception InitException = null;

        /// <summary>
        /// Xv2FileIO retrieves files from a Xenoverse 2 installation. It will first search the data folder, and then the cpks (based on the cpk load order).
        /// </summary>
        /// <param name="gameDirectory">The path to the Xenoverse 2 directory. This is the root folder that contains the cpk and data subfolders.</param>
        /// <param name="asyncInitialization">If true, the cpks will be initialized on a seperate thread. Use the property IsInitialized to determine whether the initilization has been completed.</param>
        /// <param name="customCpkLoadOrder">A custom cpk load order can be specified here. Include the file names with the extension. If null, the default Xenoverse 2 load order will be used.</param>
        public Xv2FileIO(string gameDirectory, bool asyncInitialization = false, string[] customCpkLoadOrder = null)
        {
            CustomLoadOrder = customCpkLoadOrder;
            AsyncInit = asyncInitialization;
            GameDir = gameDirectory;

            Init();

            if(InitException != null)
            {
                ExceptionDispatchInfo.Capture(InitException).Throw();
            }
        }

        private async Task Init()
        {
            try
            {
                if (AsyncInit)
                {
                    await Task.Run(new Action(EnableCpkReader));
                }
                else
                {
                    EnableCpkReader();
                }

                IsInitialized = true;
                NotifyPropertyChanged("IsInitialized");
            }
            catch (Exception ex)
            {
                InitException = ex;
            }
        }

        /// <summary>
        /// Re-enable the Cpk Reader. (Use the IsInitialized property to determine if the Cpk Reader is initialized)
        /// </summary>
        public void EnableCpkReader()
        {
            InitCpkReader();
        }

        private bool InitCpkReader()
        {
            if (Directory.Exists(string.Format("{0}/cpk", GameDir)))
            {
                cpkReader = new CPK_Reader(string.Format("{0}/cpk", GameDir), false, CustomLoadOrder);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disable the Cpk Reader. This will free up resource usage. (It can be re-enabled again later on with EnableCpkReader)
        /// </summary>
        public void DisableCpkReader()
        {
            IsInitialized = false;
            cpkReader = null;
        }


        /// <summary>
        /// Get a file from the game installation (data/cpk).
        /// </summary>
        /// <param name="path">Relative path to the file inside the data folder.</param>
        /// <param name="raiseException">If the file can't be found, an exception will be raised if this is true. Otherwise null will be returned.</param>
        /// <returns></returns>
        public byte[] GetFileFromGame(string path, bool raiseException = true, bool onlyFromCpk = false)
        {
            if(!onlyFromCpk)
            {
                var bytes = GetFileFromDataFolder(path);
                if (bytes != null) return bytes;
            }
            
            if (cpkReader != null)
            {
                var bytes = GetFileFromCpk(path);
                if (bytes != null) return bytes;
            }

            if (raiseException)
            {
                throw new FileNotFoundException(String.Format("Cannot find the file \"{0}\" in the game directory.", path));
            }

            return null;
        }

        /// <summary>
        /// Get a file from the data folder. Does not search the cpk.
        /// </summary>
        /// <param name="path">Relative path to the file inside the data folder.</param>
        /// <returns></returns>
        public byte[] GetFileFromDataFolder(string path)
        {
            path = Utils.SanitizePath(path);

            string fullPath = String.Format("{0}/data/{1}", GameDir, path);
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            return null;
        }

        /// <summary>
        /// Get a file from the cpks.
        /// </summary>
        /// <param name="path">Relative path to the file inside the data folder</param>
        /// <returns></returns>
        public byte[] GetFileFromCpk(string path)
        {
            string relativePath = String.Format("data/{0}", path);

            {
                var file = cpkReader.GetFile(relativePath);

                if (file != null) return file;
            }
            return null;
        }

        private string GetPath(string path, bool dataPath)
        {
            if (dataPath)
            {
                return PathInGameDir(path);
            }
            else
            {
                return path;
            }
        }

        public string PathInGameDir(string relativePath)
        {
            return String.Format("{0}/data/{1}", GameDir, relativePath);
        }

        /// <summary>
        /// Checks if the specified file exists.
        /// </summary>
        /// <param name="path">Relative path to the file inside the data folder.</param>
        /// <returns></returns>
        public bool FileExists(string path)
        {
            string relativePath = String.Format("data/{0}", path);

            //Check data
            if (FileExistsInGameDataDir(path)) return true;

            //Check CPK
            if (cpkReader != null) return cpkReader.Exists(relativePath);

            return false;
        }

        public bool FileExistsInGameDataDir(string path)
        {

            //Check data
            {
                string fullPath = String.Format("{0}/data/{1}", GameDir, path);
                if (File.Exists(fullPath))
                {
                    return true;
                }
            }

            return false;
        }

        public bool FileExistsInCpk(string path)
        {
            string relativePath = String.Format("data/{0}", path);

            //Check CPK
            if (cpkReader != null) return cpkReader.Exists(relativePath);

            return false;
        }

        /// <summary>
        /// Get files in the specified directly and returns their names (including relative path). Can be partially recursive (loose files only).
        /// </summary>
        public string[] GetFilesInDirectory(string directory, string extension, bool includeSubFolders = false)
        {
            if (!extension.Contains('.'))
                extension = "." + extension;

            //Take in relative path starting at data:
            //Return array of relative paths also starting at data:

            directory = Utils.SanitizePath(directory);

            List<string> files = new List<string>();

            //From game directory (data folder)
            if (Directory.Exists(PathInGameDir(directory)))
                foreach (string file in Directory.GetFiles(PathInGameDir(directory), $"*{extension}", includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    if (Path.GetExtension(file) == extension)
                        files.Add(Utils.GetRelativePath(string.Format("{0}/{1}", directory, Path.GetFileName(file))));

            //From cpks
            string[] cpkFiles = cpkReader.GetFilesInDirectory("data/" + directory);

            foreach(var _file in cpkFiles)
            {
                if (!files.Contains(_file) && Path.GetExtension(_file) == extension)
                {
                    files.Add(Utils.GetRelativePath(_file));
                }
            }

            return files.ToArray();
        }
        
    }
}
