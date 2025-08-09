using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.MSG;
using Xv2CoreLib.BCS;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.CSO;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ERS;
using Xv2CoreLib.IDB;
using Xv2CoreLib.PUP;
using Xv2CoreLib.BCM;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAI;
using Xv2CoreLib.AMK;
using Xv2CoreLib.BAS;
using Xv2CoreLib.ESK;
using Xv2CoreLib.EMD;
using Xv2CoreLib.PSC;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using System.IO;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.AFS2;
using Xv2CoreLib.SPM;
using Xv2CoreLib.FMP;
using Xv2CoreLib.NSK;
using Xv2CoreLib.Eternity;

namespace Xv2CoreLib
{
    public class FileManager
    {
        #region Singleton
        private static Lazy<FileManager> instance = new Lazy<FileManager>(() => new FileManager());
        public static FileManager Instance => instance.Value;

        private FileManager() { }
        #endregion

        public static string GameDir => Instance.fileIO?.GameDir;

        internal FileWatcher fileWatcher { get; private set; } = new FileWatcher();
        public Xv2FileIO fileIO { get; private set; }
        private Dictionary<string, CachedFile> CachedFiles;

        /// <summary>
        /// Use direct references when caching loaded files, preventing them from being removed by the garbage collector. These files can later be freed up by calling <see cref="ClearStrongReferneces"/>.
        /// </summary>
        public bool UseStrongReferences { get; set; }
        /// <summary>
        /// When enabled, the file cache is ignored and files are always reloaded from loose files or CPK, with the cache entry being overwritten.
        /// </summary>
        public bool ForceReloadFiles { get; set; }

        private object _lock = new object();

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
                    fileIO = new Xv2FileIO(GameDir, false, new string[] { "data_d4_5_xv1.cpk", "data_d6_dlc.cpk", "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk" });
                }
                else
                {
                    throw new FileNotFoundException("FileManager.Init: GameDirectory was not set or is not valid.");
                }

                CachedFiles = SettingsManager.Instance.CurrentApp == Application.XenoKit ? new Dictionary<string, CachedFile>(1024) : new Dictionary<string, CachedFile>();
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

        private bool ShouldLoadFileIO()
        {
            if (fileIO == null) return true;
            return fileIO.GameDir != SettingsManager.Instance.Settings.GameDirectory;
        }

        private void CheckInitState()
        {
            if(fileIO == null)
            {
                //If FileManager is accessed without Xenoverse.Init() being called, then it must initialize itself.
                Init();
            }
        }
        #endregion

        #region Load
        public object GetParsedFileFromGame(string path, bool onlyFromCpk = false, bool raiseEx = true, bool ignoreCache = false)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            lock (_lock)
            {
                object file = GetParsedFileFromGameInternal(path, onlyFromCpk, raiseEx, ignoreCache);

                if (SettingsManager.Instance.CurrentApp == Application.XenoKit && file != null)
                {
                    CustomEntryNames.LoadNames(path, file);
                }

                return file;
            }
        }

        private object GetParsedFileFromGameInternal(string path, bool onlyFromCpk = false, bool raiseEx = true, bool ignoreCache = false)
        {
            CheckInitState();

            //Check cache and return an existing file, if allowed (caching doesn't occur for CPK-only loads)
            if (!onlyFromCpk && !ForceReloadFiles && !ignoreCache)
            {
                object cached = GetCachedFile(path);
                if (cached != null) return cached;
            }

            //Handle missing files in CPK only
            if (onlyFromCpk)
            {
                if (!fileIO.FileExistsInCpk(path))
                {
                    if (raiseEx)
                        throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the cpks.", path));
                    else
                        return null;
                }
            }

            //File loading
            object file;

            if (path.Equals(StageDefFile.PATH, StringComparison.OrdinalIgnoreCase))
            {
                byte[] stageDefBytes = GetBytesFromGame(path, false, false);
                file = stageDefBytes != null ? StageDefFile.Load(stageDefBytes) : StageDefFile.DefaultFile;
            }
            else
            {
                //Handle missing files
                if (!fileIO.FileExists(path))
                {
                    if (raiseEx)
                        throw new FileNotFoundException(string.Format("The file \"{0}\" does not exist in the game directory or cpks.", path));
                    else
                        return null;
                }

                switch (Path.GetExtension(path))
                {
                    case ".bac":
                        file = BAC_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".bcm":
                        file = BCM_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".bcs":
                        file = BCS_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".bdm":
                        file = BDM_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx), true);
                        break;
                    case ".bsa":
                        file = BSA_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".cms":
                        file = CMS_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".cso":
                        file = CSO_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".cus":
                        file = CUS_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".ean":
                        file = EAN_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx), true);
                        break;
                    case ".ers":
                        file = ERS_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".idb":
                        file = IDB_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".pup":
                        file = PUP_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".bas":
                        file = BAS_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".bai":
                        file = BAI_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".amk":
                        file = AMK_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".esk":
                        file = ESK_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".emd":
                        file = EMD_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".nsk":
                        file = NSK_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".emb":
                        file = EMB_File.LoadEmb(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".emm":
                        file = EMM_File.LoadEmm(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".msg":
                        file = MSG_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".psc":
                        file = PSC_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".eepk":
                        file = EffectContainerFile.Load(path, fileIO, onlyFromCpk);
                        break;
                    case ".emz":
                        file = EMZ.EMZ_File.LoadData(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".acb":
                        {
                            byte[] awbBytes = fileIO.GetFileFromGame(string.Format("{0}/{1}.awb", Path.GetFileNameWithoutExtension(path), Path.GetDirectoryName(path)), false);
                            AFS2_File awbFile = awbBytes != null ? AFS2_File.LoadFromArray(awbBytes) : null;
                            file = new ACB_Wrapper(ACB_File.Load(GetBytesFromGame(path), awbFile));
                        }
                        break;
                    case ".spm":
                        file = SPM_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    case ".map":
                        file = FMP_File.Load(GetBytesFromGame(path, onlyFromCpk, raiseEx));
                        break;
                    default:
                        throw new InvalidDataException(string.Format("FileManager.GetParsedFileFromGame: The filetype of \"{0}\" is not supported.", path));
                }
            }
            
            if(!onlyFromCpk)
                AddCachedFile(path, file);

            return file;
        }

        public byte[] GetBytesFromGame(string path, bool onlyFromCpk = false, bool raiseEx = false)
        {
            CheckInitState();

            if (fileIO == null && raiseEx) throw new NullReferenceException("FileManager.GetBytesFromGame: fileIO is null.");
            if (fileIO == null) return null;

            var bytes = fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk);
            if (bytes == null) FileNotFoundEvent?.Invoke(this, new Xv2FileNotFoundEventArgs(path));
            return bytes;
        }

        #endregion

        #region Save
        internal void SaveFileToGame(string path, object file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GetAbsolutePath(path)));

            byte[] bytes = FileManager.GetBytesFromParsedFile(path, file);
            File.WriteAllBytes(GetAbsolutePath(path), bytes);
            fileWatcher.FileLoadedOrSaved(path);
        }

        internal void SaveMsgFilesToGame(string path, IList<MSG_File> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                SaveFileToGame(path + Xenoverse2.LanguageSuffix[i], files[i]);
            }
        }

        public static byte[] GetBytesFromParsedFile(string path, object data)
        {
            switch (Path.GetExtension(path))
            {
                case ".bac":
                    return ((BAC_File)data).SaveToBytes();
                case ".bcs":
                    return ((BCS_File)data).SaveToBytes();
                case ".bdm":
                    return ((BDM_File)data).SaveToBytes();
                case ".bsa":
                    return ((BSA_File)data).SaveToBytes();
                case ".cms":
                    return ((CMS_File)data).SaveToBytes();
                case ".cso":
                    return ((CSO_File)data).SaveToBytes();
                case ".cus":
                    return ((CUS_File)data).SaveToBytes();
                case ".ean":
                    return ((EAN_File)data).SaveToBytes();
                case ".ers":
                    return ((ERS_File)data).SaveToBytes();
                case ".idb":
                    return ((IDB_File)data).SaveToBytes(Path.GetFileNameWithoutExtension(path).Equals("skill_item", StringComparison.OrdinalIgnoreCase));
                case ".msg":
                    return ((MSG_File)data).SaveToBytes();
                case ".pup":
                    return ((PUP_File)data).SaveToBytes();
                case ".psc":
                    return ((PSC_File)data).SaveToBytes();
                case ".emb":
                    return ((EMB_File)data).SaveToBytes();
                case ".emd":
                    return ((EMD_File)data).SaveToBytes();
                case ".nsk":
                    return ((NSK_File)data).Write();
                case ".emm":
                    return ((EMM_File)data).SaveToBytes();
                case ".spm":
                    return ((SPM_File)data).Write();
                case ".fmp":
                    return ((FMP_File)data).Write();
                default:
                    throw new InvalidDataException(String.Format("Xenoverse2.GetBytesFromParsedFile: The filetype of \"{0}\" is not supported.", path));
            }
        }

        #endregion
        
        public string GetAbsolutePath(string relativePath)
        {
            CheckInitState();
            return (fileIO != null) ? fileIO.PathInGameDir(relativePath) : relativePath;
        }

        private void RemoveDeadReferences()
        {
            CachedFiles.RemoveAll((k, v) => !v.ObjectReference.IsAlive);
        }

        private object GetCachedFile(string path)
        {
            RemoveDeadReferences();

            if(CachedFiles.TryGetValue(path, out CachedFile file))
            {
                return file.ObjectReference.IsAlive ? file.ObjectReference.Target : null;
            }

            return null;
        }

        private void AddCachedFile(string path, object data)
        {
            path = Utils.SanitizePath(path);
            CachedFile newCachedFile = new CachedFile(path, data, UseStrongReferences);

            if (CachedFiles.TryGetValue(path, out CachedFile file))
            {
                CachedFiles[path] = newCachedFile;
            }
            else
            {
                CachedFiles.Add(path, newCachedFile);
            }
        }
    
        public void ClearStrongReferneces()
        {
            foreach(var file in CachedFiles)
            {
                file.Value?.ClearStrongReference();
            }
        }
    }

    internal class CachedFile
    {
        internal string Path { get; private set; }
        internal WeakReference ObjectReference { get; private set; }
        private object StrongReference { get; set; }

        internal CachedFile(string path, object file, bool strongReference)
        {
            Path = path;
            ObjectReference = new WeakReference(file);

            if (strongReference)
                StrongReference = file;
        }

        internal void ClearStrongReference()
        {
            StrongReference = null;
        }

    }

    public delegate void Xv2FileNotFoundEventHandler(object source, Xv2FileNotFoundEventArgs e);

    public class Xv2FileNotFoundEventArgs : EventArgs
    {
        private string EventInfo;
        public Xv2FileNotFoundEventArgs(string file)
        {
            EventInfo = file;
        }
        public string GetInfo()
        {
            return EventInfo;
        }
    }
}
