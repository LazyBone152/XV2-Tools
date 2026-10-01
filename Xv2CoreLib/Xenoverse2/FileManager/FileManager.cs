using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using Xv2CoreLib.MSG;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;

namespace Xv2CoreLib
{
    public partial class FileManager
    {
        #region Singleton
        private static Lazy<FileManager> instance = new Lazy<FileManager>(() => new FileManager());
        public static FileManager Instance => instance.Value;

        private FileManager() 
        {
            CleanUpTimer = new Timer();
            CleanUpTimer.Interval = 5.0 * 60.0 * 1000.0; //5 minutes
            CleanUpTimer.Elapsed += CleanUpTimer_Elapsed;
        }

        private void CleanUpTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (CachedFiles != null && CpkCachedFiles != null)
                {
                    RemoveDeadReferences();
                }
            }
        }
        #endregion

        public static string GameDir => Instance.FileIO?.GameDir;

        internal FileWatcher FileWatcher { get; private set; } = new FileWatcher();
        public Xv2FileIO FileIO { get; private set; }
        private readonly Timer CleanUpTimer;


        private readonly object _lock = new object();

        //Events
        /// <summary>
        /// Raised when a file is not found and an exception is not thrown.
        /// </summary>
        public static event Xv2FileNotFoundEventHandler FileNotFoundEvent;

        #region Init

        internal void Init()
        {
            if (ShouldLoadFileIO())
            {
                string GameDir = SettingsManager.Instance.Settings.GameDirectory;

                if (!File.Exists(string.Format("{0}/bin/DBXV2.exe", GameDir)))
                    GameDir = FindGameDirectory();

                if (File.Exists(string.Format("{0}/bin/DBXV2.exe", GameDir)))
                {
                    FileIO = new Xv2FileIO(GameDir, false, new string[] { "data_d4_5_xv1.cpk", "data_d6_dlc.cpk", "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk" });
                }
                else
                {
                    throw new FileNotFoundException("FileManager.Init: GameDirectory was not set or is not valid.");
                }

                bool largerCache = SettingsManager.Instance.CurrentApp == Application.XenoKit || SettingsManager.Instance.CurrentApp == Application.EepkOrganiser;
                CachedFiles = largerCache ? new Dictionary<ValueTuple<string, Type>, CachedFile>(1024) : new Dictionary<ValueTuple<string, Type>, CachedFile>(128);
                CpkCachedFiles = largerCache ? new Dictionary<ValueTuple<string, Type>, CachedFile>(1024) : new Dictionary<ValueTuple<string, Type>, CachedFile>(128);
                StrongReferenceStack = new(StrongReferenceLimit);
            }
        }

        internal static string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}Games{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}Games{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            foreach (var letter in alphabet)
            {
                string path = Path.GetFullPath(string.Format("{0}:{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    return Path.GetFullPath(string.Format("{0}:{1}SteamLibrary{1}steamapps{1}common{1}DB Xenoverse 2", letter, Path.DirectorySeparatorChar));
                }
            }

            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldLoadFileIO()
        {
            return FileIO == null || FileIO.GameDir != SettingsManager.Instance.Settings.GameDirectory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckInitState()
        {
            if(FileIO == null)
            {
                //If FileManager is accessed without Xenoverse.Init() being called, then it must initialize itself.
                Init();
            }
        }
        #endregion

        #region Load
        public T LoadFile<T>(string path, bool onlyFromCpk = false, bool raiseEx = true, bool ignoreCache = false) where T : class
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            lock (_lock)
            {
                T file = LoadFileInternal<T>(path, onlyFromCpk, raiseEx, ignoreCache);

                if (SettingsManager.Instance.CurrentApp == Application.XenoKit && file != null)
                {
                    CustomEntryNames.LoadNames(path, file);
                }

                return file;
            }
        }

        private T LoadFileInternal<T>(string path, bool onlyFromCpk = false, bool raiseEx = true, bool ignoreCache = false) where T : class
        {
            CheckInitState();

            bool useCache = !ignoreCache && (!onlyFromCpk || (onlyFromCpk && UseCpkCache));

            //Check cache and return an existing file, if allowed
            if (!ForceReloadFiles && useCache)
            {
                object cached = GetCachedFile<T>(path, onlyFromCpk);
                if (cached != null) return (T)cached;
            }

            //Handle missing files for CPK (when only loading from CPK)
            if (onlyFromCpk)
            {
                if (!FileIO.FileExistsInCpk(path))
                {
                    if (raiseEx)
                        throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the cpks.", path));
                    else
                        return null;
                }
            }

            object file;
            Type type = typeof(T);

            //Try to find file load method on type and associated [FileLoad] attribute
            var fromBytes = GetFromBytesLoadDelegate(type);
            var fromFileSystem = GetFromFileSystemLoadDelegate(type);

            if(fromBytes == null && fromFileSystem == null)
            {
                throw new InvalidDataException(string.Format("FileManager.LoadFile: The file type of {0} is not supported for loading (path={1}).\n\nNo suitable [FileLoad] attribute was found on type {0}.", type.Name, path));
            }

            FileLoadAttribute fileLoadAttribute = fromBytes?.Item1 ?? fromFileSystem?.Item1;

            //Handle missing files
            if (!FileIO.FileExists(path) && !fileLoadAttribute.AllowMissingFile)
            {
                if (raiseEx)
                    throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the game directory or cpks.", path));
                else
                    return null;
            }

            //Attempt to load the file using the found FileLoad method
            if (fromBytes != null)
            {
                file = fromBytes.Item2(GetBytesFromGame(path, onlyFromCpk, !fromBytes.Item1.AllowMissingFile));
            }
            else if (fromFileSystem != null)
            {
                file = fromFileSystem.Item2(path, FileIO, onlyFromCpk);
            }
            else
            {
                throw new InvalidOperationException("FileManager.LoadFile: Bad method state"); //Should be unreachable - the check earlier in the method should catch this and throw
            }

            if (useCache)
                AddCachedFile<T>(path, file, onlyFromCpk);

            return (T)file;
        }

        /// <summary>
        /// Load a single file from the game as a raw byte array.
        /// </summary>
        /// <param name="path">The path (relative to data folder) to the file.</param>
        /// <param name="onlyFromCpk">Limit file loading to just the CPK files. When true, loose files will be ignored.</param>
        /// <param name="throwOnMissingFile">Indicates whether an exception should be thrown if the file could not be found.</param>
        /// <returns>A byte array containing the requested file or null if not found and no exception was thrown.</returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <remarks>If a file is not found, <see cref="FileNotFoundEvent"/> will be raised if an exception was not thrown.</remarks>
        public byte[] GetBytesFromGame(string path, bool onlyFromCpk = false, bool throwOnMissingFile = false)
        {
            CheckInitState();

            var bytes = FileIO.GetFileFromGame(path, throwOnMissingFile, onlyFromCpk);
            if (bytes == null) FileNotFoundEvent?.Invoke(this, new Xv2FileNotFoundEventArgs(path));
            return bytes;
        }

        public string GetAbsolutePath(string relativePath)
        {
            CheckInitState();
            return FileIO.PathInGameDir(relativePath);
        }
        
        public bool Exists(string path)
        {
            CheckInitState();
            return FileIO.FileExists(path);
        }
        #endregion

        #region Save
        public void SaveFileToGame<T>(string path, T file) where T : class
        {
            string absolutePath = GetAbsolutePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            var toBytesWriter = GetToBytesSaveDelegate(typeof(T));
            var toDriveWriter = GetToDriveSaveDelegate(typeof(T));

            if (toBytesWriter != null)
            {
                byte[] bytes = toBytesWriter.Item2(file);
                File.WriteAllBytes(absolutePath, bytes);
                FileWatcher.FileLoadedOrSaved(path);
            }
            else if (toDriveWriter != null)
            {
                toDriveWriter.Item2(file, absolutePath);
            }
            else
            {
                throw new InvalidDataException(string.Format("FileManager.SaveFileToGame: The file type of {0} is not supported for saving (path={1}).\n\nNo suitable [FileSave] attribute was found on type {0}.", typeof(T).Name, path));
            }
        }

        internal void SaveMsgFilesToGame(string path, IList<MSG_File> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                SaveFileToGame(path + Xenoverse2.LanguageSuffix[i], files[i]);
            }
        }

        #endregion

    }

    public delegate void Xv2FileNotFoundEventHandler(object source, Xv2FileNotFoundEventArgs e);

    public class Xv2FileNotFoundEventArgs : EventArgs
    {
        private readonly string EventInfo;

        public Xv2FileNotFoundEventArgs(string file)
        {
            EventInfo = file;
        }

        public string GetInfo() => EventInfo;
    }

}
