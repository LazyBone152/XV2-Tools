using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CriPakTools;
using _cpk = CriPakTools.CPK;

namespace Xv2CoreLib.CPK
{
    /// <summary>
    /// Reads the CRIWARE CPK files for Xenoverse 2 and extracts specified files.
    /// </summary>
    public class CPK_Reader
    {
        public bool ValidCpkDirectory
        {
            get
            {
                //Returns true if atleast one of the CPKs specified in CPK_LOAD_ORDER exists
                foreach(string s in CPK_LOAD_ORDER)
                {
                    if (File.Exists(String.Format("{0}/{1}", CpkDirectory, s))) return true;
                }
                return false;
            }
        }
        private string CpkDirectory { get; set; }
        private bool RaiseExceptionIfCpkNotFound { get; set; }

        //Load order includes the XV1 Legend Patrol cpk (data_d4_5_xv1.cpk), but I dont know the exact load order for that...
        private string[] CPK_LOAD_ORDER = new string[12] { "data_d4_5_xv1.cpk", "data_d6_dlc.cpk", "data_d0_stv.cpk", "movie_d6_dlc.cpk", "movie_p4.cpk", "movie_p2.cpk", "movie.cpk", "movie0.cpk", "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk" }; 
        private List<CriPakTools.CPK> CpkFiles { get; set; }
        private List<BinaryReader> binaryReader { get; set; }
        
        /// <summary>
        /// Reads the CRIWARE CPK files for Xenoverse 2 and extracts specified files.
        /// </summary>
        /// <param name="cpkDirectory">The directory where the CPK files are located.</param>
        /// <param name="raiseExceptionIfCpkNotFound">If true, then an exception will be raised if a cpk file specified in CPK_LOAD_ORDER is not found. If false, that cpk will be skipped.</param>
        /// <param name="_CPK_LOAD_ORDER">Defines the CPKs read and in what order. Optional: Xenoverse 2 CPKs are set by default.</param>
        public CPK_Reader(string cpkDirectory, bool raiseExceptionIfCpkNotFound, string[] _CPK_LOAD_ORDER = null)
        {
            if (_CPK_LOAD_ORDER != null) CPK_LOAD_ORDER = _CPK_LOAD_ORDER;
            CpkDirectory = cpkDirectory;
            RaiseExceptionIfCpkNotFound = raiseExceptionIfCpkNotFound;
            ValidateCpks();
            CpkFileInit();
        }

        private void ValidateCpks()
        {
            if (RaiseExceptionIfCpkNotFound)
            {
                foreach(string s in CPK_LOAD_ORDER)
                {
                    if (!File.Exists(GetFullCpkPath(s)))
                    {
                        throw new FileNotFoundException(String.Format("The following CPK could not be found: {0}", s));
                    }
                }
            }
        }

        private void CpkFileInit()
        {
            if (ValidCpkDirectory)
            {
                CpkFiles = new List<CriPakTools.CPK>();
                binaryReader = new List<BinaryReader>();
                
                foreach (string s in CPK_LOAD_ORDER)
                {
                    if (File.Exists(GetFullCpkPath(s)))
                    {
                        var cpk = new CriPakTools.CPK(new Tools());
                        cpk.ReadCPK(GetFullCpkPath(s)); 
                        CpkFiles.Add(cpk);
                        binaryReader.Add(new BinaryReader(File.OpenRead(GetFullCpkPath(s))));
                    }
                }
            }
        }

        private string GetFullCpkPath(string cpk)
        {
            return String.Format("{0}/{1}", CpkDirectory, cpk);
        }

        /// <summary>
        /// Get the specified file from the CPK files.
        /// </summary>
        /// <param name="fileName">The name of the file, including the path (starting from "data").</param>
        public bool GetFile(string fileName, string outputPath)
        {
            if (!ValidCpkDirectory) throw new Exception("Invalid CPK Directory. Cannot retrieve the file.");

            for(int i = 0; i < CpkFiles.Count; i++)
            { 
                if (GetFileFromCpk(CpkFiles[i], binaryReader[i], fileName, outputPath)) return true;
            }

            return false;
        }

        private bool GetFileFromCpk(CriPakTools.CPK cpk, BinaryReader binaryReader, string file, string outputPath)
        {
            try
            {
                string dirToFind = Utils.SanitizePath(Path.GetDirectoryName(file));
                string nameToFind = Path.GetFileName(file);

                var results = cpk.FileTable.Where(p => (Path.Equals(p.DirName, dirToFind)));

                foreach (var e in results)
                {
                    if (String.Equals(nameToFind, (string)e.FileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        }

                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);
                        string isComp = Encoding.ASCII.GetString(binaryReader.ReadBytes(8)); 
                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);

                        byte[] chunk = binaryReader.ReadBytes(Int32.Parse(e.FileSize.ToString()));
                        if (isComp == "CRILAYLA")
                        {
                            int size = Int32.Parse((e.ExtractSize ?? e.FileSize).ToString());
                            chunk = _cpk.DecompressCRILAYLA(chunk, size);
                        }
                        File.WriteAllBytes(outputPath, chunk);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            
        }

        /// <summary>
        /// Get the specified file from the CPK files and returns it as a byte array.
        /// </summary>
        /// <param name="fileName">The name of the file, including the path (starting from "data").</param>
        public byte[] GetFile(string fileName)
        {
            if (!ValidCpkDirectory) throw new Exception("Invalid CPK Directory. Cannot retrieve the file.");
            
            for (int i = 0; i < CpkFiles.Count; i++)
            {
                var bytes = GetFileFromCpkAsByteArray(CpkFiles[i], binaryReader[i], fileName);

                if (bytes != null) return bytes;
            }

            return null;
        }
        
        private byte[] GetFileFromCpkAsByteArray(CriPakTools.CPK cpk, BinaryReader binaryReader, string file)
        {
            string dirToFind = Utils.SanitizePath(Path.GetDirectoryName(file));
            string nameToFind = Path.GetFileName(file);
            
            try
            {
                var results = cpk.FileTable.Where(p => (Path.Equals(p.DirName, dirToFind)));

                foreach (var e in results)
                {
                    if (String.Equals(nameToFind, (string)e.FileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);
                        string isComp = Encoding.ASCII.GetString(binaryReader.ReadBytes(8));
                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);

                        byte[] chunk = binaryReader.ReadBytes(Int32.Parse(e.FileSize.ToString()));
                        if (isComp == "CRILAYLA")
                        {
                            int size = Int32.Parse((e.ExtractSize ?? e.FileSize).ToString());
                            chunk = _cpk.DecompressCRILAYLA(chunk, size);
                        }
                        return chunk;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }

        }

        /// <summary>
        /// Checks if the specified file exists in any CPK.
        /// </summary>
        /// <param name="fileName">The name of the file, including the path (starting from "data").</param>
        public bool Exists(string fileName)
        {
            if (!ValidCpkDirectory) throw new Exception("Invalid CPK Directory. Cannot retrieve the file.");

            for (int i = 0; i < CpkFiles.Count; i++)
            {
                if (DoesFileExist(CpkFiles[i], binaryReader[i], fileName)) return true;
            }

            return false;
        }
        
        private bool DoesFileExist(CriPakTools.CPK cpk, BinaryReader binaryReader, string file)
        {
            string dirToFind = Utils.SanitizePath(Path.GetDirectoryName(file));
            string nameToFind = Path.GetFileName(file);

            try
            {
                var results = cpk.FileTable.Where(p => (Path.Equals(p.DirName, dirToFind)));

                foreach (var e in results)
                {
                    if (String.Equals(nameToFind, (string)e.FileName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        public string[] GetFilesInDirectory(string directory)
        {
            List<string> files = new List<string>();

            for (int i = 0; i < CpkFiles.Count; i++)
            {
                files.AddRange(GetFilesinDirectoryFromCpk(CpkFiles[i], binaryReader[i], directory));
            }

            return files.ToArray();
        }

        private string[] GetFilesinDirectoryFromCpk(CriPakTools.CPK cpk, BinaryReader binaryReader, string dir)
        {
            string dirToFind = Utils.SanitizePath(dir);
            List<string> files = new List<string>();
            
            var results = cpk.FileTable.Where(p => (Path.Equals(p.DirName, dirToFind)));

            foreach (var e in results)
            {
                files.Add(string.Format("{0}/{1}", e.DirName, e.FileName));
            }

            return files.ToArray();
        }



        #region ExtractAll
        /// <summary>
        /// Extract all CPKs into the same directory. The extraction is done in the CPK load order.
        /// </summary>
        /// <returns></returns>
        public async Task ExtractAll(string outputDir, string fileExtToExtract = null)
        {
            List<object> extracted = new List<object>(); //string

            for (int i = CpkFiles.Count - 1; i >= 0; i--)
            {
                var results = CpkFiles[i].FileTable.Where(p => p.FileType == "FILE" && (string)p.FileName != null);

                foreach (var file in results)
                {
                    if (fileExtToExtract != null && Path.GetExtension((string)file.FileName) != fileExtToExtract)
                        continue;

                    //Only extract if it hasn't been previously extracted
                    if(extracted.IndexOf(file.FileName) == -1)
                    {
                        extracted.Add(file.FileName);

                        //Extract it
                        binaryReader[i].BaseStream.Seek((long)file.FileOffset, SeekOrigin.Begin);
                        string isComp = Encoding.ASCII.GetString(binaryReader[i].ReadBytes(8));
                        binaryReader[i].BaseStream.Seek((long)file.FileOffset, SeekOrigin.Begin);

                        byte[] chunk = binaryReader[i].ReadBytes(Int32.Parse(file.FileSize.ToString()));
                        if (isComp == "CRILAYLA")
                        {
                            int size = Int32.Parse((file.ExtractSize ?? file.FileSize).ToString());
                            chunk = _cpk.DecompressCRILAYLA(chunk, size);
                        }

                        if (!Directory.Exists(string.Format("{0}{1}{3}{1}", outputDir, Path.DirectorySeparatorChar, file.FileName, file.DirName)))
                            Directory.CreateDirectory(string.Format("{0}{1}{3}{1}", outputDir, Path.DirectorySeparatorChar, file.FileName, file.DirName));

                        if(chunk != null)
                            File.WriteAllBytes(string.Format("{0}{1}{3}{1}{2}", outputDir, Path.DirectorySeparatorChar, file.FileName, file.DirName), chunk);
                    }
                }
            }
        }
        #endregion


    }

    public class CPK_MultiThreadedExtractor : INotifyPropertyChanged
    {
        #region INotPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region UI Props
        private int _numExtracted = 0;
        private int _numFiles = 0;
        private int _threadsComplete = 0;
        private int _numThreads = 0;
        public int NumExtracted
        {
            get
            {
                return _numExtracted;
            }
            set
            {
                if(_numExtracted != value)
                {
                    _numExtracted = value;
                    NotifyPropertyChanged(nameof(NumExtracted));
                }
            }
        }
        public int NumFiles
        {
            get
            {
                return _numFiles;
            }
            set
            {
                if (_numFiles != value)
                {
                    _numFiles = value;
                    NotifyPropertyChanged(nameof(NumFiles));
                }
            }
        }
        public int ThreadsComplete
        {
            get
            {
                return _threadsComplete;
            }
            set
            {
                if (_threadsComplete != value)
                {
                    _threadsComplete = value;
                    NotifyPropertyChanged(nameof(ThreadsComplete));
                }
            }
        }
        public int NumThreads
        {
            get
            {
                return _numThreads;
            }
            set
            {
                if (_numThreads != value)
                {
                    _numThreads = value;
                    NotifyPropertyChanged(nameof(NumThreads));
                }
            }
        }
        
        #endregion

        CriPakTools.CPK cpk;
        List<BinaryReader> binaryReaders = new List<BinaryReader>();
        string outputDir;
        
        public bool isFinished = false;

        public CPK_MultiThreadedExtractor(string cpkPath, string outputDir, int numThreads = -1)
        {
            cpk = new CriPakTools.CPK(new Tools());
            cpk.ReadCPK(cpkPath);
            this.outputDir = outputDir;
            this.NumThreads = (numThreads != -1) ? numThreads : Environment.ProcessorCount;
            this.NumFiles = cpk.FileTable.Count;

            for (int i = 0; i < NumThreads; i++)
                binaryReaders.Add(new BinaryReader(File.OpenRead(cpkPath)));
        }

        public void Start()
        {
            Task.Run(() => InternalStart());
        }

        private void InternalStart()
        {
            int splitWorkload = cpk.FileTable.Count / NumThreads;
            List<Task> tasks = new List<Task>();

            for (int a = 0; a < NumThreads; a++) 
            {
                 int workload = splitWorkload;

                if(a == NumThreads - 1)
                {
                    //Recalculate workload for last thread to include ALL remaining files (the initial / will exclude some when FileTable.Count is odd)
                    workload = cpk.FileTable.Count - (splitWorkload * (NumThreads - 1));
                }

                //Console.WriteLine($"Thread {a + 1}: Idx = {splitWorkload * a}, Count = {workload}");

                int temp = a;
                tasks.Add(Task.Run(() => ExtractFiles(splitWorkload * temp, workload, binaryReaders[temp], temp)));
            }

            //Console.Read();

            //Wait for all threads to finish execution
            Task.WaitAll(tasks.ToArray());

            //Error checking
            //if (NumExtracted != NumFiles)
            //    throw new Exception("NumExtracted != NumFiles");

            isFinished = true;
        }

        private async Task ExtractFiles(int tableIdx, int count, BinaryReader binaryReader, int idx)
        {
            Thread.CurrentThread.Name = $"CPK Extract Thread {idx}";

            for(int i = tableIdx; i < tableIdx + count; i++)
            {
                if (cpk.FileTable[i].FileType == "FILE" && (string)cpk.FileTable[i].FileName != null)
                {
                    FileEntry e = cpk.FileTable[i];

                    binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);
                    string isComp = Encoding.ASCII.GetString(binaryReader.ReadBytes(8));
                    binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);

                    byte[] chunk = binaryReader.ReadBytes(Int32.Parse(e.FileSize.ToString()));

                    if (isComp == "CRILAYLA")
                    {
                        int size = Int32.Parse((e.ExtractSize ?? e.FileSize).ToString());
                        chunk = _cpk.DecompressCRILAYLA(chunk, size);
                    }

                    string savePath = $"{outputDir}/{e.DirName}/{e.FileName}";
                    string dir = Path.GetDirectoryName(savePath);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllBytes(savePath, chunk);
                }

                //Interlocked.Increment(ref _numExtracted);
            }

            //Interlocked.Increment(ref _threadsComplete);
        }

    }
}
