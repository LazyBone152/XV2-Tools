using System;
using System.Linq;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAS;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.Xenoverse2;

namespace Xv2CoreLib
{
    [Serializable]
    public class Xv2File<T> : INotifyPropertyChanged where T : class
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region UI Properties
        public string BorrowString { get { return (Borrowed) ? "Yes" : "No"; } }
        public string PathString { get { return (Borrowed) ? Path : "Calculated on save"; } }
        public string DisplayName
        {
            get
            {
                switch (FileType)
                {
                    case Xenoverse2.MoveFileTypes.TAL_EAN:
                        return "Tail"; //Only ever loaded for Common
                    case Xenoverse2.MoveFileTypes.FCE_EAN:
                        return "Face"; //Loaded on characters
                    case Xenoverse2.MoveFileTypes.EAN:
                    case Xenoverse2.MoveFileTypes.CAM_EAN:
                        return (IsDefault) ? "Main" : CharaCode;
                    case Xenoverse2.MoveFileTypes.VOX_ACB:
                    case Xenoverse2.MoveFileTypes.SE_ACB:
                        string lang = (IsEnglish) ? "EN" : "JP";

                        if (CharaCode == "HUM" && FileType == MoveFileTypes.VOX_ACB)
                        {
                            return $"Voice {Costumes[0]} ({lang})";
                        }

                        switch (MoveType)
                        {
                            case Xenoverse2.MoveType.Common:
                                return "Common";
                            case Xenoverse2.MoveType.Character:
                                return (FileType == MoveFileTypes.VOX_ACB) ? $"Costume: {GetCostumesString()} ({lang})" : $"Costume: {GetCostumesString()}";
                            case Xenoverse2.MoveType.Skill:
                                return (FileType == MoveFileTypes.VOX_ACB) ? $"Chara: {CharaCode} ({lang})" : $"SE";
                            default:
                                return "invalid_ACB_Type";
                        }
                    default:
                        return "invalid";
                }
            }
        }

        #endregion

        //Data
        public Xenoverse2.MoveType MoveType { get; set; }
        public Xenoverse2.MoveFileTypes FileType { get; set; }
        public T File { get; set; } = null;
        /// <summary>
        /// Absolute path to the file. This will be re-calculated when saving except when it is "Borrowed" or was loaded manually.
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// If true, then this file belongs to another source. In this case, it will always be saved back to its original source (overwritting it) unless specified otherwise.
        /// </summary>
        public bool Borrowed { get; set; } = false;

        #region Args
        private string _charaCode = string.Empty;
        private bool _isEnglish = false;
        private List<int> _costumes = null;

        /// <summary>
        /// Used to specify character code for VOX ACB and EAN files.
        /// </summary>
        public string CharaCode
        {
            get
            {
                return _charaCode;
            }
            set
            {
                if (_charaCode != value)
                {
                    _charaCode = value;
                    NotifyPropertyChanged(nameof(CharaCode));
                    NotifyPropertyChanged(nameof(DisplayName));
                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
            }
        }
        /// <summary>
        /// Applicable to ACB files only. Determines if the ACB file is for English or Japanese.
        /// </summary>
        public bool IsEnglish
        {
            get { return _isEnglish; }
            set
            {
                if (value != _isEnglish)
                {
                    _isEnglish = value;
                    NotifyPropertyChanged(nameof(IsEnglish));
                }
            }
        }
        /// <summary>
        /// Used to specify what costumes this file is for (se, vox, amk). Accepts multiple int values (greater than 0) or a single value of 0.
        /// </summary>
        public List<int> Costumes
        {
            get
            {
                return _costumes;
            }
            set
            {
                if (_costumes != value)
                {
                    _costumes = value;
                    NotifyPropertyChanged(nameof(Costumes));
                }
            }
        }
        /// <summary>
        /// Is this file a default file? (Default files are always on a move and cannot be deleted.).
        /// </summary>
        public bool IsDefault { get; private set; }

        //Helpers
        public bool IsNotDefault { get { return !IsDefault; } }

        //Undo/Redo
        public string UndoableCharaCode
        {
            get
            {
                return CharaCode;
            }
            set
            {
                if (CharaCode != value && !string.IsNullOrEmpty(value))
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<T>>(nameof(CharaCode), this, CharaCode, value, "CharaCode"));
                    CharaCode = value;

                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
                else
                {
                    //Value cannot be set to null/empty
                    CharaCode = _charaCode;
                    NotifyPropertyChanged(nameof(UndoableCharaCode));
                }
            }
        }
        public bool UndoableIsEnglish
        {
            get
            {
                return IsEnglish;
            }
            set
            {
                if (IsEnglish != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<T>>(nameof(IsEnglish), this, IsEnglish, value, "IsEnlgish"));
                    IsEnglish = value;

                    NotifyPropertyChanged(nameof(UndoableIsEnglish));
                }
            }
        }


        #endregion

        public Xv2File(T file, string path, bool borrowed, string charaCode = null, bool isEnglish = false, Xenoverse2.MoveFileTypes fileType = 0, int costume = 0, bool isDefault = true, Xenoverse2.MoveType moveType = 0)
        {
            File = file;
            Path = path;
            Borrowed = borrowed;
            CharaCode = charaCode;
            IsEnglish = isEnglish;
            FileType = fileType;
            IsDefault = isDefault;
            MoveType = moveType;

            if (fileType == Xenoverse2.MoveFileTypes.SE_ACB || fileType == Xenoverse2.MoveFileTypes.VOX_ACB || fileType == Xenoverse2.MoveFileTypes.AMK)
            {
                Costumes = new List<int>();
                Costumes.Add(costume);
            }
        }

        public Xv2File(T file, List<int> costume = null, Xenoverse2.MoveFileTypes fileType = 0, Xenoverse2.MoveType moveType = 0, bool isEnglish = false, bool isDefault = true)
        {
            File = file;
            IsEnglish = isEnglish;
            FileType = fileType;
            IsDefault = isDefault;
            Costumes = costume;
            MoveType = moveType;

            if (costume == null && (fileType == Xenoverse2.MoveFileTypes.SE_ACB || fileType == Xenoverse2.MoveFileTypes.VOX_ACB || fileType == Xenoverse2.MoveFileTypes.AMK))
            {
                Costumes = new List<int>();
                Costumes.Add(0);
            }
        }
        

        public List<IUndoRedo> ReplaceFile(string path)
        {
            object oldFile = File;
            object newFile = null;

            if (File is BAC_File && System.IO.Path.GetExtension(path) == ".bac")
            {
                newFile = BAC_File.Load(path);
                ((BAC_File)newFile).InitializeIBacTypes();
            }
            else if (File is BDM_File && System.IO.Path.GetExtension(path) == ".bdm")
            {
                newFile = BDM_File.Load(path);
            }
            else if (File is BSA_File && System.IO.Path.GetExtension(path) == ".bsa")
            {
                newFile = BSA_File.Load(path);
            }
            else if (File is BAS_File && System.IO.Path.GetExtension(path) == ".bas")
            {
                newFile = BAS_File.Load(path);
            }
            else if (File is ACB_Wrapper && System.IO.Path.GetExtension(path) == ".acb")
            {
                newFile = new ACB_Wrapper(ACB_File.Load(path));
            }
            else if (File is EEPK_File && System.IO.Path.GetExtension(path) == ".eepk")
            {
                newFile = EffectContainerFile.Load(path);
            }
            else if (File is EAN_File && System.IO.Path.GetExtension(path) == ".ean")
            {
                newFile = EAN_File.Load(path, true);
            }
            else
            {
                throw new InvalidDataException("ReplaceFile: File type mismatch!");
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(File), this, oldFile, newFile));
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Borrowed), this, Borrowed, false));
            undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Path), this, Path, string.Empty));
            undos.Add(new UndoActionDelegate(this, nameof(RefreshProperties), true));

            File = (T)newFile;
            Borrowed = false;
            Path = string.Empty;

            RefreshProperties();
            return undos;
        }

        public List<IUndoRedo> UnborrowFile()
        {
            //this may be buggy with recursive files... todo check

            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (Borrowed)
            {
                Borrowed = false;
                T copiedFile = File.Copy();

                undos.Add(new UndoableProperty<Xv2File<T>>(nameof(Borrowed), this, true, false));
                undos.Add(new UndoableProperty<Xv2File<T>>(nameof(File), this, File, copiedFile));

                if (copiedFile is ACB_Wrapper acb)
                    acb.Refresh();

                File = copiedFile;

                undos.Add(new UndoActionDelegate(this, nameof(RefreshProperties), true));
                RefreshProperties();
            }


            return undos;
        }

        /// <summary>
        /// Creates a new instance of <see cref="File"/>, breaking any shared instances that may exist. <see cref="File"/> will be deep-copied, so all of its values will remain but it will be a new instance.
        /// </summary>
        public List<IUndoRedo> NewInstance()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            object oldFile = File;
            File = File.Copy();

            //Special case: if ACB, references must be fixed after serialization
            if(File is ACB_Wrapper acb && typeof(T) == typeof(ACB_Wrapper))
            {
                acb.AcbFile.SetCommandTableVersion();
                File = new ACB_Wrapper(acb.AcbFile) as T;
            }

            //Since this is now a new instance it wont be treated as borrowed file anymore
            if (Borrowed)
            {
                Borrowed = false;
                undos.Add(new UndoablePropertyGeneric(nameof(Borrowed), this, true, false));
            }

            undos.Add(new UndoablePropertyGeneric(nameof(File), this, oldFile, File));
            return undos;
        }

        #region Costume
        public string GetCostumesString()
        {
            if (Costumes.Count == 1 && Costumes[0] == 0) return "0";

            StringBuilder str = new StringBuilder();

            for (int i = 0; i < Costumes.Count; i++)
            {
                str.Append(Costumes[i]);

                if (Costumes.Count - 1 != i)
                    str.Append(", ");
            }

            return str.ToString();
        }

        public bool HasCostume(int costume)
        {
            return Costumes.Contains(costume);
        }

        public bool HasCostume(List<int> costumes)
        {
            foreach (var costume in costumes)
            {
                if (HasCostume(costume)) return true;
            }

            return false;
        }

        public void AddCostume(int costume)
        {
            if (Costumes == null) Costumes = new List<int>();

            //Dont add extra costumes to the default (0). 
            //0 will automatically be used by all costumes that dont have a specific extra defined.
            if (HasCostume(0)) return;

            if (!Costumes.Contains(costume))
                Costumes.Add(costume);
        }


        /// <summary>
        /// Add a costume to an existing loaded file. Intended for use with CSO files (AMK, SE, VOX)
        /// </summary>
        /// <returns></returns>
        public static bool AddCostume(IList<Xv2File<T>> files, string absPath, int costume, bool isEnglish)
        {
            var file = files.FirstOrDefault(x => Utils.ComparePaths(absPath, x.Path) && x.IsEnglish == isEnglish);

            if (file != null)
            {
                file.AddCostume(costume);
                return true;
            }

            return false;
        }

        public static bool HasCostume(IList<Xv2File<T>> files, List<int> costumes, int excludeIndex = -1)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (excludeIndex != i && files[i].HasCostume(costumes))
                    return true;
            }

            return false;
        }

        public static bool HasCostume(IList<Xv2File<T>> files, int costume, int excludeIndex = -1)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (excludeIndex != i && files[i].HasCostume(costume))
                    return true;
            }

            return false;
        }
        #endregion

        #region Helpers
        public bool IsOptionalFile()
        {
            //Optional files = VOX ACBs and EAN/CAM.EANs that are chara-unique. Also AFTER_BAC and AFTER_BCM.
            if (FileType == Xenoverse2.MoveFileTypes.AFTER_BAC || FileType == Xenoverse2.MoveFileTypes.AFTER_BCM) return true;
            if (FileType == Xenoverse2.MoveFileTypes.VOX_ACB && !IsDefault) return true;
            if ((FileType == Xenoverse2.MoveFileTypes.EAN || FileType == Xenoverse2.MoveFileTypes.CAM_EAN) && !IsDefault) return true;
            return false;
        }

        private void RefreshProperties()
        {
            NotifyPropertyChanged(nameof(FileType));
            NotifyPropertyChanged(nameof(BorrowString));
            NotifyPropertyChanged(nameof(PathString));
            NotifyPropertyChanged(nameof(CharaCode));
            NotifyPropertyChanged(nameof(IsEnglish));
            NotifyPropertyChanged(nameof(DisplayName));
            NotifyPropertyChanged(nameof(IsEnglish));
            NotifyPropertyChanged(nameof(File));
        }

        public static Xv2File<T> GetCostumeFileOrDefault(IList<Xv2File<T>> files, int costume)
        {
            Xv2File<T> file = files.FirstOrDefault(x => x.HasCostume(costume));

            if (file == null)
                return files.FirstOrDefault(x => x.HasCostume(0));

            return file;
        }

        public static bool IsCharaCodeUsed(IList<Xv2File<T>> files, string charaCode, int excludeIndex = -1)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (excludeIndex != i && files[i].CharaCode == charaCode)
                    return true;
            }

            return false;
        }
        
        #endregion
    }

}
