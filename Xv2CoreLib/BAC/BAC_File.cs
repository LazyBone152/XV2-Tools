using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;
using Xv2CoreLib.Resource;

#if UndoRedo
using Xv2CoreLib.Resource.UndoRedo;
#endif

namespace Xv2CoreLib.BAC
{
    public interface IBacType
    {
        short StartTime { get; set; } //0
        short Duration { get; set; } //2
        short I_04 { get; set; } //4
        short Flags { get; set; } //6
    }

    [YAXSerializeAs("BAC")]
    [Serializable]
    public class BAC_File : ISorting, IIsNull
    {
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("values")]
        public int[] I_20 { get; set; } = new int[3]; // size 3
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0#############")]
        public float[] F_32 { get; set; } = new float[12]; // size 12
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("values")]
        public int[] I_80 { get; set; } = new int[4];// size 4

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacEntry")]
        public AsyncObservableCollection<BAC_Entry> BacEntries { get; set; } = AsyncObservableCollection<BAC_Entry>.Create();


        public static BAC_File DefaultBacFile()
        {
            return new BAC_File();
        }

        public void SortEntries()
        {
            if (BacEntries != null)
                BacEntries = Sorting.SortEntries(BacEntries);
                //BacEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SaveBinary(string path)
        {
            new Deserializer(this, path);
        }

        public static BAC_File Load(byte[] bytes)
        {
            return new Parser(bytes).bacFile;
        }

        public static BAC_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        /// <summary>
        /// Adds a BAC Entry and returns the assigned index.
        /// </summary>
        /// <param name="entry">The BAC Entry to add.</param>
        /// <param name="idx">The index to add the BAC entry at. If this is -1, then one will be automatically assigned.</param>
        /// <returns></returns>
        public int AddEntry(BAC_Entry entry, int idx = -1)
        {
            if(idx <= (BacEntries.Count - 1) && idx != -1)
            {
                BacEntries[idx] = entry;
                BacEntries[idx].SortID = idx;
                return idx;
            }

            idx = GetFreeId();
            entry.SortID = idx;
            BacEntries.Add(entry);
            return idx;
        }

        public BAC_Entry AddNewEntry()
        {
            BAC_Entry newEntry = new BAC_Entry();
            int idx = AddEntry(newEntry);
            return BacEntries[idx];
        }
        
        public void RemoveEntry(BAC_Entry entryToRemove)
        {
            BacEntries.Remove(entryToRemove);
        }

        public BAC_Entry GetEntry(int id)
        {
            return BacEntries.FirstOrDefault(x => x.SortID == id);
        }

        public BAC_Entry GetEntry(string id)
        {
            foreach (var entry in BacEntries)
            {
                if (entry.Index == id) return entry;
            }
            return null;

        }

        public void RegenerateIndexes()
        {
            for(int i = 0; i < BacEntries.Count; i++)
            {
                BacEntries[i].Index = i.ToString();
            }
        }

        #region IBacTypesMethods
        public void InitializeIBacTypes()
        {
            foreach(var bacEntry in BacEntries)
            {
                bacEntry.InitializeIBacTypes();
            }
        }

        public void SaveIBacTypes()
        {
            foreach (var bacEntry in BacEntries)
            {
                bacEntry.SaveIBacTypes();
            }
        }
        #endregion

        public bool IsNull()
        {
            foreach (var entry in BacEntries)
                if (!entry.IsBacEntryEmpty()) return false;
            return true;
        }

        private int GetFreeId()
        {
            int id = 0;
            while (BacEntries.Any(c => c.SortID == id) && id < int.MaxValue)
                id++;
            return id;
        }
    }

    [Serializable]
    public class BAC_Entry : IInstallable, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        [Flags]
        public enum Flags : uint
        {
            unk1 = 1,
            unk2 = 2,
            unk3 = 4,
            unk4 = 8,
            unk5 = 16,
            unk6 = 32,
            unk7 = 64,
            unk8 = 128,
            unk9 = 256,
            unk10 = 512,
            unk11 = 1024,
            unk12 = 2048,
            unk13 = 4096,
            unk14 = 8192,
            unk15 = 16384,
            unk16 = 32768,
            unk17 = 65536,
            unk18 = 131072,
            unk19 = 262144,
            unk20 = 524288,
            unk21 = 1048576,
            unk22 = 2097152,
            unk23 = 4194304,
            unk24 = 8388608,
            unk25 = 16777216,
            unk26 = 33554432,
            unk27 = 67108864,
            unk28 = 134217728,
            unk29 = 268435456,
            unk30 = 536870912,
            unk31 = 1073741824,
            CMN = 2147483648
        }

        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } set { Index = value.ToString(); NotifyPropertyChanged("UndoableId"); } }
        [YAXDontSerialize]
        public Flags FlagProp { get { return Flag; } set { Flag = value; NotifyPropertyChanged("UndoableFlag"); } }
        #endregion

        #if UndoRedo
        [YAXDontSerialize]
        public int UndoableId
        {
            get { return SortID; }
            set
            {
                Resource.UndoRedo.UndoManager.Instance.AddUndo(new Resource.UndoRedo.UndoableProperty<BAC_Entry>("SortID", this, SortID, value, "Bac Entry ID"));
                SortID = value;
                NotifyPropertyChanged("UndoableId");
            }
        }
        [YAXDontSerialize]
        public Flags UndoableFlag
        {
            get { return FlagProp; }
            set
            {
                Resource.UndoRedo.UndoManager.Instance.AddUndo(new Resource.UndoRedo.UndoableProperty<BAC_Entry>("FlagProp", this, FlagProp, value, "Bac Entry Flag"));
                FlagProp = value;
                NotifyPropertyChanged("UndoableFlag");
            }
        }
#endif

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } = "0"; //int32
        [YAXAttributeForClass]
        public Flags Flag { get; set; }
        

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        [BindingSubList]
        public List<BAC_Type0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        [BindingSubList]
        public List<BAC_Type1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Movement")]
        public List<BAC_Type2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Invulnerability")]
        public List<BAC_Type3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TimeScale")]
        public List<BAC_Type4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Tracking")]
        public List<BAC_Type5> Type5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ChargeControl")]
        public List<BAC_Type6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BcmCallback")]
        public List<BAC_Type7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        [BindingSubList]
        public List<BAC_Type8> Type8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Projectile")]
        [BindingSubList]
        public List<BAC_Type9> Type9 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Camera")]
        [BindingSubList]
        public List<BAC_Type10> Type10 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        public List<BAC_Type11> Type11 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TargetingAssistance")]
        public List<BAC_Type12> Type12 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BcsPartSetInvisibility")]
        public List<BAC_Type13> Type13 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AnimationModification")]
        public List<BAC_Type14> Type14 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TransformControl")]
        [BindingSubList]
        public List<BAC_Type15> Type15 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ScreenEffect")]
        [BindingSubList]
        public List<BAC_Type16> Type16 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ThrowHandler")]
        public List<BAC_Type17> Type17 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PhysicsObjectControl")]
        public List<BAC_Type18> Type18 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Aura")]
        public List<BAC_Type19> Type19 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "HomingMovement")]
        public List<BAC_Type20> Type20 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EyeMovement")]
        public List<BAC_Type21> Type21 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type22")]
        public List<BAC_Type22> Type22 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TransparencyEffect")]
        public List<BAC_Type23> Type23 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DualSkill")]
        public List<BAC_Type24> Type24 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type25")]
        public List<BAC_Type25> Type25 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type26")]
        public List<BAC_Type26> Type26 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EffectPropertyControl")]
        public List<BAC_Type27> Type27 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("Types")]
        [YAXAttributeFor("HasDummy")]
        public List<int> TypeDummy { get; set; }

        [YAXDontSerialize]
        public AsyncObservableCollection<IBacType> IBacTypes { get; set; } = AsyncObservableCollection<IBacType>.Create();

        #region IBacTypeMethods
        public void InitializeIBacTypes()
        {
            InitBacLists();

            IBacTypes = AsyncObservableCollection<IBacType>.Create();

            foreach (var bacEntry in Type0)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type1)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type2)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type3)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type4)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type5)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type6)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type7)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type8)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type9)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type10)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type11)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type12)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type13)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type14)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type15)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type16)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type17)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type18)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type19)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type20)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type21)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type22)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type23)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type24)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type25)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type26)
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type27)
                IBacTypes.Add(bacEntry);
        }

        public void SaveIBacTypes()
        {
            ClearBacLists();

            foreach(var bacEntry in IBacTypes)
            {
                if(bacEntry is BAC_Type0 type)
                {
                    Type0.Add(type);
                }
                else if (bacEntry is BAC_Type1 type1)
                {
                    Type1.Add(type1);
                }
                else if (bacEntry is BAC_Type2 type2)
                {
                    Type2.Add(type2);
                }
                else if (bacEntry is BAC_Type3 type3)
                {
                    Type3.Add(type3);
                }
                else if (bacEntry is BAC_Type4 type4)
                {
                    Type4.Add(type4);
                }
                else if (bacEntry is BAC_Type5 type5)
                {
                    Type5.Add(type5);
                }
                else if (bacEntry is BAC_Type6 type6)
                {
                    Type6.Add(type6);
                }
                else if (bacEntry is BAC_Type7 type7)
                {
                    Type7.Add(type7);
                }
                else if (bacEntry is BAC_Type8 type8)
                {
                    Type8.Add(type8);
                }
                else if (bacEntry is BAC_Type9 type9)
                {
                    Type9.Add(type9);
                }
                else if (bacEntry is BAC_Type10 type10)
                {
                    Type10.Add(type10);
                }
                else if (bacEntry is BAC_Type11 type11)
                {
                    Type11.Add(type11);
                }
                else if (bacEntry is BAC_Type12 type12)
                {
                    Type12.Add(type12);
                }
                else if (bacEntry is BAC_Type13 type13)
                {
                    Type13.Add(type13);
                }
                else if (bacEntry is BAC_Type14 type14)
                {
                    Type14.Add(type14);
                }
                else if (bacEntry is BAC_Type15 type15)
                {
                    Type15.Add(type15);
                }
                else if (bacEntry is BAC_Type16 type16)
                {
                    Type16.Add(type16);
                }
                else if (bacEntry is BAC_Type17 type17)
                {
                    Type17.Add(type17);
                }
                else if (bacEntry is BAC_Type18 type18)
                {
                    Type18.Add(type18);
                }
                else if (bacEntry is BAC_Type19 type19)
                {
                    Type19.Add(type19);
                }
                else if (bacEntry is BAC_Type20 type20)
                {
                    Type20.Add(type20);
                }
                else if (bacEntry is BAC_Type21 type21)
                {
                    Type21.Add(type21);
                }
                else if (bacEntry is BAC_Type22 type22)
                {
                    Type22.Add(type22);
                }
                else if (bacEntry is BAC_Type23 type23)
                {
                    Type23.Add(type23);
                }
                else if (bacEntry is BAC_Type24 type24)
                {
                    Type24.Add(type24);
                }
                else if (bacEntry is BAC_Type25 type25)
                {
                    Type25.Add(type25);
                }
                else if (bacEntry is BAC_Type26 type26)
                {
                    Type26.Add(type26);
                }
                else if (bacEntry is BAC_Type27 type27)
                {
                    Type27.Add(type27);
                }
            }
        }

        private void InitBacLists()
        {
            if (Type0 == null)
                Type0 = new List<BAC_Type0>();
            if (Type1 == null)
                Type1 = new List<BAC_Type1>();
            if (Type2 == null)
                Type2 = new List<BAC_Type2>();
            if (Type3 == null)
                Type3 = new List<BAC_Type3>();
            if (Type4 == null)
                Type4 = new List<BAC_Type4>();
            if (Type5 == null)
                Type5 = new List<BAC_Type5>();
            if (Type6 == null)
                Type6 = new List<BAC_Type6>();
            if (Type7 == null)
                Type7 = new List<BAC_Type7>();
            if (Type8 == null)
                Type8 = new List<BAC_Type8>();
            if (Type9 == null)
                Type9 = new List<BAC_Type9>();
            if (Type10 == null)
                Type10 = new List<BAC_Type10>();
            if (Type11 == null)
                Type11 = new List<BAC_Type11>();
            if (Type12 == null)
                Type12 = new List<BAC_Type12>();
            if (Type13 == null)
                Type13 = new List<BAC_Type13>();
            if (Type14 == null)
                Type14 = new List<BAC_Type14>();
            if (Type15 == null)
                Type15 = new List<BAC_Type15>();
            if (Type16 == null)
                Type16 = new List<BAC_Type16>();
            if (Type17 == null)
                Type17 = new List<BAC_Type17>();
            if (Type18 == null)
                Type18 = new List<BAC_Type18>();
            if (Type19 == null)
                Type19 = new List<BAC_Type19>();
            if (Type20 == null)
                Type20 = new List<BAC_Type20>();
            if (Type21 == null)
                Type21 = new List<BAC_Type21>();
            if (Type22 == null)
                Type22 = new List<BAC_Type22>();
            if (Type23 == null)
                Type23 = new List<BAC_Type23>();
            if (Type24 == null)
                Type24 = new List<BAC_Type24>();
            if (Type25 == null)
                Type25 = new List<BAC_Type25>();
            if (Type26 == null)
                Type26 = new List<BAC_Type26>();
            if (Type27 == null)
                Type27 = new List<BAC_Type27>();
        }

        private void ClearBacLists()
        {
            InitBacLists();

            Type0.Clear();
            Type1.Clear();
            Type2.Clear();
            Type3.Clear();
            Type4.Clear();
            Type5.Clear();
            Type6.Clear();
            Type7.Clear();
            Type8.Clear();
            Type9.Clear();
            Type10.Clear();
            Type11.Clear();
            Type12.Clear();
            Type13.Clear();
            Type14.Clear();
            Type15.Clear();
            Type16.Clear();
            Type17.Clear();
            Type18.Clear();
            Type19.Clear();
            Type20.Clear();
            Type21.Clear();
            Type22.Clear();
            Type23.Clear();
            Type24.Clear();
            Type25.Clear();
            Type26.Clear();
            Type27.Clear();
        }

        #if UndoRedo
        /// <summary>
        /// Add a new instance of the specified IBacType as an undoable operation.
        /// </summary>
        /// <param name="bacType"></param>
        /// <returns></returns>
        public void UndoableAddIBacType(int bacType)
        {
            IBacType iBacType;

            switch (bacType)
            {
                case 0:
                    iBacType = new BAC_Type0();
                    break;
                case 1:
                    iBacType = new BAC_Type1();
                    break;
                case 2:
                    iBacType = new BAC_Type2();
                    break;
                case 3:
                    iBacType = new BAC_Type3();
                    break;
                case 4:
                    iBacType = new BAC_Type4();
                    break;
                case 5:
                    iBacType = new BAC_Type5();
                    break;
                case 6:
                    iBacType = new BAC_Type6();
                    break;
                case 7:
                    iBacType = new BAC_Type7();
                    break;
                case 8:
                    iBacType = new BAC_Type8();
                    break;
                case 9:
                    iBacType = new BAC_Type9();
                    break;
                case 10:
                    iBacType = new BAC_Type10();
                    break;
                case 11:
                    iBacType = new BAC_Type11();
                    break;
                case 12:
                    iBacType = new BAC_Type12();
                    break;
                case 13:
                    iBacType = new BAC_Type13();
                    break;
                case 14:
                    iBacType = new BAC_Type14();
                    break;
                case 15:
                    iBacType = new BAC_Type15();
                    break;
                case 16:
                    iBacType = new BAC_Type16();
                    break;
                case 17:
                    iBacType = new BAC_Type17();
                    break;
                case 18:
                    iBacType = new BAC_Type18();
                    break;
                case 19:
                    iBacType = new BAC_Type19();
                    break;
                case 20:
                    iBacType = new BAC_Type20();
                    break;
                case 21:
                    iBacType = new BAC_Type21();
                    break;
                case 22:
                    iBacType = new BAC_Type22();
                    break;
                case 23:
                    iBacType = new BAC_Type23();
                    break;
                case 24:
                    iBacType = new BAC_Type24();
                    break;
                case 25:
                    iBacType = new BAC_Type25();
                    break;
                case 26:
                    iBacType = new BAC_Type26();
                    break;
                case 27:
                    iBacType = new BAC_Type27();
                    break;
                default:
                    throw new InvalidOperationException($"UndoableAddIBacType: Invalid bacType {bacType}!");
            }

            IBacTypes.Add(iBacType);
            UndoManager.Instance.AddUndo(new UndoableListAdd<IBacType>(IBacTypes, iBacType, $"New BacType {bacType}"));
        }

        /// <summary>
        /// Remove the specified iBacType instances as an undoable operation.
        /// </summary>
        /// <param name="iBacType"></param>
        public void UndoableRemoveIBacType(IList<IBacType> iBacTypes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var iBacType in iBacTypes)
            {
                undos.Add(new UndoableListRemove<IBacType>(IBacTypes, iBacType));
                IBacTypes.Remove(iBacType);
            }
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "BacType Delete"));
        }
        #endif

        #endregion

        public bool IsBacEntryEmpty()
        {
            if (Type0?.Count != 0 || Type1?.Count != 0 || Type2?.Count != 0 || Type3?.Count != 0 || Type4?.Count != 0 || Type5?.Count != 0 || Type6?.Count != 0 ||
                Type7?.Count != 0 || Type8?.Count != 0 || Type9?.Count != 0 || Type10?.Count != 0 || Type11?.Count != 0 || Type12?.Count != 0 || Type13?.Count != 0 ||
                Type14?.Count != 0 || Type15?.Count != 0 || Type16?.Count != 0 || Type17?.Count != 0 || Type18?.Count != 0 || Type19?.Count != 0 || Type20?.Count != 0 ||
                Type21?.Count != 0 || Type22?.Count != 0 || Type23?.Count != 0 || Type24?.Count != 0 || Type25?.Count != 0 || Type26?.Count != 0 || Type27?.Count != 0)
            {
                return false;
            }
            return true;
        }

        public bool IsIBacEntryEmpty()
        {
            return (IBacTypes?.Count == 0);
        }


        /// <summary>
        /// Creates a partial clone of the Bac Entry. Currently only Type0 and Type1 are cloned, other types are returned by reference.
        /// </summary>
        /// <returns></returns>
        public BAC_Entry Clone()
        {
            return new BAC_Entry()
            {
                Index = Index,
                Flag = Flag,
                Type0 = BAC_Type0.Clone(Type0),
                Type1 = BAC_Type1.Clone(Type1),
                Type2 = Type2,
                Type10 = Type10,
                Type11 = Type11,
                Type12 = Type12,
                Type13 = Type13,
                Type14 = Type14,
                Type15 = Type15,
                Type16 = Type16,
                Type17 = Type17,
                Type18 = Type18,
                Type19 = Type19,
                Type20 = Type20,
                Type21 = Type21,
                Type22 = Type22,
                Type23 = Type23,
                Type24 = Type24,
                Type25 = Type25,
                Type3 = Type3,
                Type4 = Type4,
                Type5 = Type5,
                Type6 = Type6,
                Type7 = Type7,
                Type8 = Type8,
                Type9 = Type9,
                Type26 = Type26,
                Type27 = Type27,
                TypeDummy = TypeDummy
            };
        }

        public static BAC_Entry Empty(int id = 0)
        {
            return new BAC_Entry()
            {
                Flag = Flags.CMN,
                SortID = id
            };
        }
    }

    [Serializable]
    public class BAC_TypeBase : IBacType, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
        

        private short _startTime = 0;
        private short _duration = 0;
        private short _flags = 0;

        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public short StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                if(_startTime != value)
                {
                    _startTime = value;
                    NotifyPropertyChanged("StartTime");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public short Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    NotifyPropertyChanged("Duration");
                }
            }
        }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flags")]
        public short Flags
        {
            get
            {
                return _flags;
            }
            set
            {
                if (_flags != value)
                {
                    _flags = value;
                    NotifyPropertyChanged("Flags");
                }
            }
        }
        
    }

    [YAXSerializeAs("Animation")]
    [Serializable]
    public class BAC_Type0 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type
        {
            get
            {
                return  $"Animation ({I_08})";
            }
        }

        [Serializable]
        public enum EanType : ushort
        {
            Common = 0,
            Character = 5,
            CommonTail = 9,
            FaceA = 10,
            FaceB = 11,
            Skill = 65534
        }

        [Flags]
        [Serializable]
        public enum AnimationFlags : ushort
        {
            MoveWithAxis_X = 1,
            MoveWithAxis_Y = 2,
            MoveWithAxis_Z = 4,
            Unk3 = 8,
            Unk4 = 16,
            Unk5 = 32,
            Unk6 = 64,
            Unk7 = 128,
            Unk8 = 256,
            Unk9 = 512,
            Unk10 = 1024,
            Unk11 = 2048,
            Unk12 = 4096,
            Unk13 = 8192,
            Unk14 = 16384,
            Unk15 = 32768
        }

        #region WrapperProperties
        [YAXDontSerialize]
        public bool AnimFlag_MoveWithXAxis
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.MoveWithAxis_X);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.MoveWithAxis_X, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag_MoveWithYAxis
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.MoveWithAxis_Y);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.MoveWithAxis_Y, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag_MoveWithZAxis
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.MoveWithAxis_Z);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.MoveWithAxis_Z, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag8
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk3);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk3, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag16
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk4);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk4, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag32
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk5);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk5, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag64
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk6);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk6, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag128
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk7);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk7, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag256
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk8);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk8, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag_ContinueAnim
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk9);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk9, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag1024
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk10);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk10, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag2048
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk11);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk11, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag4096
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk12);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk12, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag8192
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk13);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk13, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag16384
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk14);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk14, value);
            }
        }
        [YAXDontSerialize]
        public bool AnimFlag32768
        {
            get
            {
                return I_12.HasFlag(AnimationFlags.Unk15);
            }
            set
            {
                I_12 = I_12.SetFlag(AnimationFlags.Unk15, value);
            }
        }
       
        [YAXDontSerialize]
        public ushort EanTypeProp { get { return (ushort)I_08; } set { I_08 = (EanType)value; } }
        [YAXDontSerialize]
        public ushort EanIndexProp { get { return ushort.Parse(I_10); } set { I_10 = value.ToString(); } }
        #endregion

        private EanType _eanType = EanType.Common;
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("File")]
        public EanType I_08
        {
            get
            {
                return _eanType;
            }
            set
            {
                _eanType = value;
                NotifyPropertyChanged("I_08");
                NotifyPropertyChanged("EanTypeProp");
                NotifyPropertyChanged("Type");
            }
        }
        [YAXAttributeFor("EAN_Index")]
        [YAXSerializeAs("value")]
        public string I_10 { get; set; } = "0"; //ushort
        [YAXAttributeFor("AnimationFlags")]
        [YAXSerializeAs("values")]
        public AnimationFlags I_12 { get; set; } //ushort
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("StartFrame")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("EndFrame")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("LoopStartFrame")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("TimeScale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Amount")]
        public float F_24 { get; set; } = 1f;
        [YAXAttributeFor("StartBlendWeight")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_28 { get; set; } = 1f;
        [YAXAttributeFor("BlendWeightIncreasePerFrame")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_32 { get; set; } = 0f;

        //Propeties
        [YAXDontSerialize]
        public ushort EanIndex { get { return ushort.Parse(I_10); } set { I_10 = value.ToString(); } }

        public static List<BAC_Type0> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type0> Type0 = new List<BAC_Type0>();

            for (int i = 0; i < count; i++)
            {
                Type0.Add(new BAC_Type0()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (EanType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    EanIndex = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = (AnimationFlags)BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32)
                });

                offset += 36;
            }

            return Type0;
        }

        public static List<byte> Write(List<BAC_Type0> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.EanIndex));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
            }

            return bytes;
        }

        public static List<BAC_Type0> Clone(List<BAC_Type0> types)
        {
            if (types == null) return null;
            List<BAC_Type0> newTypes = new List<BAC_Type0>();

            foreach(var entry in types)
            {
                newTypes.Add(entry.Clone());
            }

            return newTypes;
        }

        public BAC_Type0 Clone()
        {
            return new BAC_Type0()
            {
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                StartTime = StartTime,
                Duration = Duration,
                I_04 = I_04,
                Flags = Flags,
                I_08 = I_08,
                I_10 = I_10,
                I_12 = I_12,
                I_14 = I_14,
                I_16 = I_16,
                I_18 = I_18,
                I_20 = I_20,
                I_22 = I_22
            };

        }
        
    }

    [YAXSerializeAs("Hitbox")]
    [Serializable]
    public class BAC_Type1 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Hitbox"; } }

        public enum BdmType : byte
        {
            Common = 0,
            Character = 1,
            Skill = 2
        }
        
        [YAXAttributeFor("BDM")]
        [YAXSerializeAs("File")]
        public BdmType I_18_c { get; set; }
        [YAXAttributeFor("BDM_Entry")]
        [YAXSerializeAs("ID")]
        public string I_08 { get; set; } //ushort
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("A")]
        public byte I_18_a { get; set; }
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("B")]
        public byte I_18_b { get; set; }
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("D")]
        public byte I_18_d { get; set; }
        [YAXAttributeFor("Hitbox_Flags")]
        [YAXSerializeAs("value")]
        public string I_10 { get; set; } //uint16
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Damage_When_Blocked")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("Stamina_Taken_When_Blocked")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("values")]
        public short[] I_20 { get; set; } //size 4
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_60 { get; set; }

        //Properties
        [YAXDontSerialize]
        public ushort BdmEntryID { get { return ushort.Parse(I_08); } set { I_08 = value.ToString(); } }

        public static List<BAC_Type1> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type1> Type1 = new List<BAC_Type1>();

            for (int i = 0; i < count; i++)
            {
                Type1.Add(new BAC_Type1()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    BdmEntryID = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = HexConverter.GetHexString(BitConverter.ToUInt16(rawBytes, offset + 10)),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18_a = Int4Converter.ToInt4(rawBytes[offset + 18])[0],
                    I_18_b = Int4Converter.ToInt4(rawBytes[offset + 18])[1],
                    I_18_c = (BdmType)Int4Converter.ToInt4(rawBytes[offset + 19])[0],
                    I_18_d = Int4Converter.ToInt4(rawBytes[offset + 19])[1],
                    I_20 = BitConverter_Ex.ToInt16Array(rawBytes, offset + 20, 4),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                    F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                });

                offset += 64;
            }

            return Type1;
        }

        public static List<byte> Write(List<BAC_Type1> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.BdmEntryID));
                bytes.AddRange(BitConverter.GetBytes(HexConverter.ToInt16(type.I_10)));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.Add(Int4Converter.GetByte(type.I_18_a, type.I_18_b, "Hitbox > Flag_18 > A", "Hitbox > Flag_18 > B"));
                bytes.Add(Int4Converter.GetByte((byte)type.I_18_c, type.I_18_d, "Hitbox > BDM File", "Hitbox > Flag_18 > D"));
                bytes.AddRange(BitConverter_Ex.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter.GetBytes(type.F_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes(type.F_44));
                bytes.AddRange(BitConverter.GetBytes(type.F_48));
                bytes.AddRange(BitConverter.GetBytes(type.F_52));
                bytes.AddRange(BitConverter.GetBytes(type.F_56));
                bytes.AddRange(BitConverter.GetBytes(type.F_60));
            }

            return bytes;
        }


        public static List<BAC_Type1> Clone(List<BAC_Type1> types)
        {
            if (types == null) return null;
            List<BAC_Type1> newTypes = new List<BAC_Type1>();

            foreach (var entry in types)
            {
                newTypes.Add(entry.Clone());
            }

            return newTypes;
        }

        public BAC_Type1 Clone()
        {
            return new BAC_Type1()
            {
                F_28 = F_28,
                F_32 = F_32,
                StartTime = StartTime,
                Duration = Duration,
                I_04 = I_04,
                Flags = Flags,
                I_08 = I_08,
                I_10 = I_10,
                I_12 = I_12,
                I_14 = I_14,
                I_16 = I_16,
                I_20 = I_20,
                I_18_a = I_18_a,
                I_18_b = I_18_b,
                I_18_c = I_18_c,
                I_18_d = I_18_d,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44,
                F_48 = F_48,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60
            };

        }

    }

    [YAXSerializeAs("Movement")]
    [Serializable]
    public class BAC_Type2 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Movement"; } }

        
        [YAXAttributeFor("Movement_Type")]
        [YAXSerializeAs("Flags")]
        public string I_08 { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public short I_10 { get; set; }
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_32 { get; set; }

        public static List<BAC_Type2> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type2> Type2 = new List<BAC_Type2>();

            for (int i = 0; i < count; i++)
            {
                Type2.Add(new BAC_Type2()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = HexConverter.GetHexString(BitConverter.ToUInt16(rawBytes, offset + 8)),
                    I_10 = BitConverter.ToInt16(rawBytes, offset + 10),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32)
                });

                offset += 36;
            }

            return Type2;
        }

        public static List<byte> Write(List<BAC_Type2> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(HexConverter.ToInt16(type.I_08)));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.F_12));
                bytes.AddRange(BitConverter.GetBytes(type.F_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("Invulnerability")]
    [Serializable]
    public class BAC_Type3 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Invulnerability"; } }

        
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }

        public static List<BAC_Type3> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type3> Type3 = new List<BAC_Type3>();

            for (int i = 0; i < count; i++)
            {
                Type3.Add(new BAC_Type3()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8)
                });

                offset += 12;
            }

            return Type3;
        }

        public static List<byte> Write(List<BAC_Type3> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("TimeScale")]
    [Serializable]
    public class BAC_Type4 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "TimeScale"; } }


        [YAXAttributeFor("TimeScale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Amount")]
        public float F_08 { get; set; } = 1f;

        public static List<BAC_Type4> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type4> Type4 = new List<BAC_Type4>();

            for (int i = 0; i < count; i++)
            {
                Type4.Add(new BAC_Type4()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8)
                });

                offset += 12;
            }

            return Type4;
        }

        public static List<byte> Write(List<BAC_Type4> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.F_08));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Tracking")]
    [Serializable]
    public class BAC_Type5 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Tracking"; } }

        
        [YAXAttributeFor("Tracking")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public short I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public short I_14 { get; set; }

        public static List<BAC_Type5> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type5> Type5 = new List<BAC_Type5>();

            for (int i = 0; i < count; i++)
            {
                Type5.Add(new BAC_Type5()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToInt16(rawBytes, offset + 14)
                });

                offset += 16;
            }

            return Type5;
        }

        public static List<byte> Write(List<BAC_Type5> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.F_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("ChargeControl")]
    [Serializable]
    public class BAC_Type6 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "ChargeControl"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Charge_Time")]
        [YAXSerializeAs("value")]
        public short I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public short I_14 { get; set; }

        public static List<BAC_Type6> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type6> Type6 = new List<BAC_Type6>();

            for (int i = 0; i < count; i++)
            {
                Type6.Add(new BAC_Type6()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToInt16(rawBytes, offset + 14)
                });

                offset += 16;
            }

            return Type6;
        }

        public static List<byte> Write(List<BAC_Type6> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("BcmCallback")]
    [Serializable]
    public class BAC_Type7 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "BcmCallback"; } }

        
        [YAXAttributeFor("Link_Flags")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_08 { get; set; } //uint16
        
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_1")]
        //public bool I_09_0 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_2")]
        //public bool I_09_1 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_3")]
        //public bool I_09_2 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_4")]
        //public bool I_09_3 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_5")]
        //public bool I_09_4 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_6")]
        //public bool I_09_5 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_7")]
        //public bool I_09_6 { get; set; }
        //[YAXAttributeFor("Bac_Cases")]
        //[YAXSerializeAs("Case_8")]
        //public bool I_09_7 { get; set; }

        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Unk0")]
        //public bool I_08_0 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("AllowCancel")]
        //public bool I_08_1 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("PairBcmOnly")]
        //public bool I_08_2 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Backhit")]
        //public bool I_08_3 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Unk4")]
        //public bool I_08_4 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Unk5")]
        //public bool I_08_5 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Unk6")]
        //public bool I_08_6 { get; set; }
        //[YAXAttributeFor("Link_Path2")]
        //[YAXSerializeAs("Unk7")]
        //public bool I_08_7 { get; set; }

        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public short I_10 { get; set; }

        public static List<BAC_Type7> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type7> Type7 = new List<BAC_Type7>();

            for (int i = 0; i < count; i++)
            {
                //BitArray I_08 = new BitArray(new byte[1] { rawBytes[offset + 8] });
                //BitArray I_09 = new BitArray(new byte[1] { rawBytes[offset + 9] });

                Type7.Add(new BAC_Type7()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    //I_09_0 = I_09[0],
                    //I_09_1 = I_09[1],
                    //I_09_2 = I_09[2],
                    //I_09_3 = I_09[3],
                    //I_09_4 = I_09[4],
                    //I_09_5 = I_09[5],
                    //I_09_6 = I_09[6],
                    //I_09_7 = I_09[7],
                    //I_08_0 = I_08[0],
                    //I_08_1 = I_08[1],
                    //I_08_2 = I_08[2],
                    //I_08_3 = I_08[3],
                    //I_08_4 = I_08[4],
                    //I_08_5 = I_08[5],
                    //I_08_6 = I_08[6],
                    //I_08_7 = I_08[7],
                    I_10 = BitConverter.ToInt16(rawBytes, offset + 10)
                });

                offset += 12;
            }

            return Type7;
        }

        public static List<byte> Write(List<BAC_Type7> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                //BitArray I_08 = new BitArray(new bool[8] { type.I_08_0, type.I_08_1, type.I_08_2, type.I_08_3, type.I_08_4, type.I_08_5, type.I_08_6, type.I_08_7, });
                //BitArray I_09 = new BitArray(new bool[8] { type.I_09_0, type.I_09_1, type.I_09_2, type.I_09_3, type.I_09_4, type.I_09_5, type.I_09_6, type.I_09_7 });

                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Effect")]
    [Serializable]
    public class BAC_Type8 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Effect"; } }


        public enum EepkType : ushort
        {
            Common = 0,
            StageBG = 1,
            Character = 2,
            AwokenSkill = 3,
            SuperSkill = 5,
            UltimateSkill = 6,
            EvasiveSkill = 7,
            KiBlastSkill = 9,
            Stage = 11
        }

        public enum UseSkillId : ushort
        {
            True = 0,
            False = 65535
        }
        
        public enum Switch : uint
        {
            On = 0,
            Off = 1
        }

        #region WrapperProps
        [YAXDontSerialize]
        public ushort SkillID { get { return ushort.Parse(I_12); } set { I_12 = value.ToString(); } }
        [YAXDontSerialize]
        public int EffectID { get { return I_16; } set { I_16 = value; } }

        #endregion

        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkType I_08 { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public string I_12 { get; set; } //ushort
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("ID")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("Switch")]
        public Switch I_44 { get; set; } //uint32
        [YAXAttributeFor("Bone_Link")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Use_Skill_ID")]
        [YAXSerializeAs("value")]
        public UseSkillId I_14 { get; set; } //uint16
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_40 { get; set; }
        

        public static List<BAC_Type8> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type8> Type8 = new List<BAC_Type8>();

            for (int i = 0; i < count; i++)
            {
                Type8.Add(new BAC_Type8()
                {


                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (EepkType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    SkillID = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = (UseSkillId)BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    I_44 = (Switch)BitConverter.ToUInt32(rawBytes, offset + 44)
                });

                offset += 48;
            }

            return Type8;
        }

        public static List<byte> Write(List<BAC_Type8> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter.GetBytes(type.F_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes((uint)type.I_44));
            }

            return bytes;
        }

        public static List<BAC_Type8> ChangeSkillId(List<BAC_Type8> types, int skillID)
        {
            if (types == null) return null;

            for(int i = 0; i < types.Count; i++)
            {
                switch (types[i].I_08)
                {
                    case EepkType.AwokenSkill:
                    case EepkType.SuperSkill:
                    case EepkType.UltimateSkill:
                    case EepkType.EvasiveSkill:
                    case EepkType.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }
        
        public bool IsSkillEepk()
        {
            if(I_08 == EepkType.SuperSkill || I_08 == EepkType.UltimateSkill || I_08 == EepkType.EvasiveSkill || I_08 == EepkType.AwokenSkill || I_08 == EepkType.KiBlastSkill)
            {
                return true;
            }
            return false;
        }

    }

    [YAXSerializeAs("Projectile")]
    [Serializable]
    public class BAC_Type9 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Projectile"; } }

        

        public enum BsaType : ushort
        {
            Common = 0,
            AwokenSkill = 3,
            SuperSkill = 5,
            UltimateSkill = 6,
            EvasiveSkill = 7,
            KiBlastSkill = 9
        }
        
        public enum CanUseCmnBsa : ushort
        {
            False = 0,
            True = 65535
        }

        #region WrapperProps
        [YAXDontSerialize]
        public ushort SkillID { get { return ushort.Parse(I_08); } set { I_08 = value.ToString(); } }
        [YAXDontSerialize]
        public int EntryID { get { return int.Parse(I_12); } set { I_12 = value.ToString(); } }

        #endregion

        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Type")]
        public BsaType I_44 { get; set; }
        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Skill_ID")]
        public string I_08 { get; set; } //ushort
        [YAXAttributeFor("Can_Use_Cmn_Bsa")]
        [YAXSerializeAs("value")]
        public CanUseCmnBsa I_10 { get; set; } // uint16
        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Entry ID")]
        public string I_12 { get; set; } //int
        [YAXAttributeFor("Bone")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("SpawnSource")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_18 { get; set; } //uint16
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_40 { get; set; }
        [YAXAttributeFor("I_46")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_46 { get; set; } // uint16
        [YAXAttributeFor("Projectile_Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        
        public bool IsSkillBsa()
        {
            if(I_44 == BsaType.SuperSkill || I_44 == BsaType.UltimateSkill || I_44 == BsaType.EvasiveSkill || I_44 == BsaType.AwokenSkill || I_44 == BsaType.KiBlastSkill)
            {
                return true;
            }

            return false;
        }
        
        public static List<BAC_Type9> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type9> Type9 = new List<BAC_Type9>();

            for (int i = 0; i < count; i++)
            {
                float _F_48 = 0;
                int _I_52 = 0;
                int _I_56 = 0;
                int _I_60 = 0;

                try
                {
                     _F_48 = BitConverter.ToSingle(rawBytes, offset + 48);
                     _I_52 = BitConverter.ToInt32(rawBytes, offset + 52);
                     _I_56 = BitConverter.ToInt32(rawBytes, offset + 56);
                     _I_60 = BitConverter.ToInt32(rawBytes, offset + 60);
                }
                catch
                {
                    //If it fails, then this is an old 48 byte size type. In that case, use default values.
                }

                Type9.Add(new BAC_Type9()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8).ToString(),
                    I_10 = (CanUseCmnBsa)BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12).ToString(),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    I_44 = (BsaType)BitConverter.ToUInt16(rawBytes, offset + 44),
                    I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                    F_48 = _F_48,
                    I_52 = _I_52,
                    I_56 = _I_56,
                    I_60 = _I_60,
                });

                offset += 64;
            }

            return Type9;
        }

        public static List<byte> Write(List<BAC_Type9> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(type.I_08)));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_10));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(type.I_12)));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter.GetBytes(type.F_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_44));
                bytes.AddRange(BitConverter.GetBytes(type.I_46));
                bytes.AddRange(BitConverter.GetBytes(type.F_48));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_56));
                bytes.AddRange(BitConverter.GetBytes(type.I_60));
            }

            return bytes;
        }

        public static List<BAC_Type9> ChangeSkillId(List<BAC_Type9> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].I_44)
                {
                    case BsaType.AwokenSkill:
                    case BsaType.SuperSkill:
                    case BsaType.UltimateSkill:
                    case BsaType.EvasiveSkill:
                    case BsaType.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }

    }

    [YAXSerializeAs("Camera")]
    [Serializable]
    public class BAC_Type10 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Camera"; } }


        public enum EanType : ushort
        {
            Target = 0,
            Common = 3,
            Character = 4,
            Skill = 5
        }

        #region WrapperProps

        [YAXDontSerialize]
        public ushort EanTypeProp { get { return (ushort)I_08; } set { I_08 = (EanType)value; } }
        [YAXDontSerialize]
        public ushort EanIndexProp { get { return ushort.Parse(I_12); } set { I_12 = value.ToString(); } }
        #endregion

        [YAXAttributeFor("EAN_TO_USE")]
        [YAXSerializeAs("value")]
        public EanType I_08 { get; set; } = EanType.Common;
        [YAXAttributeFor("BoneToFocusOn")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("EAN_Index")]
        [YAXSerializeAs("value")]
        public string I_12 { get; set; } = "0"; //ushort
        [YAXAttributeFor("StartFrame")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("GlobalModiferDuration")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("EnableTransformModifers")]
        [YAXSerializeAs("value")]
        public bool I_74_7 { get; set; } //I_74_7
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("XZ")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("ZY")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("FieldOfView")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }

        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("X")]
        public ushort I_66 { get; set; }
        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("Y")]
        public ushort I_68 { get; set; }
        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("Z")]
        public ushort I_56 { get; set; }

        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("X")]
        public ushort I_62 { get; set; }
        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("Y")]
        public ushort I_64 { get; set; }
        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("Z")]
        public ushort I_72 { get; set; }

        [YAXAttributeFor("DisplacementDuration")]
        [YAXSerializeAs("XZ")]
        public ushort I_58 { get; set; }
        [YAXAttributeFor("DisplacementDuration")]
        [YAXSerializeAs("ZY")]
        public ushort I_60 { get; set; }

        [YAXAttributeFor("FieldOfViewDuration")]
        [YAXSerializeAs("value")]
        public ushort I_70 { get; set; }

        [YAXAttributeFor("EnableCameraForAllPlayers")]
        [YAXSerializeAs("value")]
        public bool I_74_0 { get; set; }
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk1")]
        public bool I_74_1 { get; set; }
        [YAXAttributeFor("FocusOnTarget")]
        [YAXSerializeAs("value")]
        public bool I_74_2 { get; set; }
        [YAXAttributeFor("UseCharacterSpecificCameraEan")]
        [YAXSerializeAs("value")]
        public bool I_74_3 { get; set; }
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk4")]
        public bool I_74_4 { get; set; }
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk5")]
        public bool I_74_5 { get; set; }
        [YAXAttributeFor("DontOverrideActiveCameras")]
        [YAXSerializeAs("value")]
        public bool I_74_6 { get; set; }


        [YAXAttributeFor("I_75")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_75 { get; set; } //Int8

        //Propeties
        [YAXDontSerialize]
        public ushort EanIndex { get { return ushort.Parse(I_12); } set { I_12 = value.ToString(); } }

        public static List<BAC_Type10> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type10> Type10 = new List<BAC_Type10>();

            for (int i = 0; i < count; i++)
            {
                BitArray I_74 = new BitArray(new byte[1] { rawBytes[offset + 74] });

                Type10.Add(new BAC_Type10()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (EanType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    EanIndex = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                    I_56 = BitConverter.ToUInt16(rawBytes, offset + 56),
                    I_58 = BitConverter.ToUInt16(rawBytes, offset + 58),
                    I_60 = BitConverter.ToUInt16(rawBytes, offset + 60),
                    I_62 = BitConverter.ToUInt16(rawBytes, offset + 62),
                    I_64 = BitConverter.ToUInt16(rawBytes, offset + 64),
                    I_66 = BitConverter.ToUInt16(rawBytes, offset + 66),
                    I_68 = BitConverter.ToUInt16(rawBytes, offset + 68),
                    I_70 = BitConverter.ToUInt16(rawBytes, offset + 70),
                    I_72 = BitConverter.ToUInt16(rawBytes, offset + 72),
                    I_74_0 = I_74[0],
                    I_74_1 = I_74[1],
                    I_74_2 = I_74[2],
                    I_74_3 = I_74[3],
                    I_74_4 = I_74[4],
                    I_74_5 = I_74[5],
                    I_74_6 = I_74[6],
                    I_74_7 = I_74[7],
                    I_75 = rawBytes[offset + 75],
                });

                offset += 76;
            }

            return Type10;
        }

        public static List<byte> Write(List<BAC_Type10> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                BitArray I_74 = new BitArray(new bool[8] { type.I_74_0, type.I_74_1, type.I_74_2, type.I_74_3, type.I_74_4, type.I_74_5, type.I_74_6, type.I_74_7, });

                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.EanIndex));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter.GetBytes(type.F_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes(type.F_44));
                bytes.AddRange(BitConverter.GetBytes(type.F_48));
                bytes.AddRange(BitConverter.GetBytes(type.F_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_56));
                bytes.AddRange(BitConverter.GetBytes(type.I_58));
                bytes.AddRange(BitConverter.GetBytes(type.I_60));
                bytes.AddRange(BitConverter.GetBytes(type.I_62));
                bytes.AddRange(BitConverter.GetBytes(type.I_64));
                bytes.AddRange(BitConverter.GetBytes(type.I_66));
                bytes.AddRange(BitConverter.GetBytes(type.I_68));
                bytes.AddRange(BitConverter.GetBytes(type.I_70));
                bytes.AddRange(BitConverter.GetBytes(type.I_72));
                bytes.Add(Utils.ConvertToByte(I_74));
                bytes.Add(type.I_75);
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Sound")]
    [Serializable]
    public class BAC_Type11 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Sound"; } }


        public enum AcbType : ushort
        {
            Common_SE = 0,
            Character_SE = 2,
            Character_VOX = 3,
            Skill_SE = 10,
            Skill_VOX = 11
        }

        #region WrapperProps
        [YAXDontSerialize]
        public ushort CueId { get { return I_12; } set { I_12 = value; } }
        [YAXDontSerialize]
        public AcbType acbType { get { return I_08; } set { I_08 = value; } }
        [YAXDontSerialize]
        public ushort AcbTypeNumeric { get { return (ushort)I_08; } set { I_08 = (AcbType)value; } }
        #endregion

        [YAXAttributeFor("ACB")]
        [YAXSerializeAs("File")]
        public AcbType I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public short I_14 { get; set; }

        
        public static List<BAC_Type11> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type11> Type11 = new List<BAC_Type11>();

            for (int i = 0; i < count; i++)
            {
                Type11.Add(new BAC_Type11()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (AcbType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToInt16(rawBytes, offset + 14)
                });

                offset += 16;
            }

            return Type11;
        }

        public static List<byte> Write(List<BAC_Type11> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("TargetingAssistance")]
    [Serializable]
    public class BAC_Type12 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "TargetingAssistance"; } }


        public enum Axis : ushort
        {
            X = 0,
            Y = 1,
            Z = 2
        }
        
        [YAXAttributeFor("Axis")]
        [YAXSerializeAs("value")]
        public Axis I_08 { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public short I_10 { get; set; }

        public static List<BAC_Type12> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type12> Type12 = new List<BAC_Type12>();

            for (int i = 0; i < count; i++)
            {
                Type12.Add(new BAC_Type12()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (Axis)BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToInt16(rawBytes, offset + 10)
                });

                offset += 12;
            }

            return Type12;
        }

        public static List<byte> Write(List<BAC_Type12> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("BcsPartSetInvisibility")]
    [Serializable]
    public class BAC_Type13 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "BcsPartSetInvisibility"; } }


        public enum BcsPartId : ushort
        {
            FaceBase = 0,
            FaceForehead = 1,
            FaceEye = 2,
            FaceNose = 3,
            FaceEar = 4,
            Hair = 5,
            Bust = 6,
            Pants = 7,
            Rists = 8,
            Boots = 9
        }

        public enum Switch : ushort
        {
            On = 0,
            Off = 1
        }
        
        [YAXAttributeFor("Part")]
        [YAXSerializeAs("value")]
        public BcsPartId I_08 { get; set; } //uint16
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public Switch I_10 { get; set; } //uint16
        
        public static List<BAC_Type13> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type13> Type13 = new List<BAC_Type13>();

            for (int i = 0; i < count; i++)
            {
                Type13.Add(new BAC_Type13()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (BcsPartId)BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = (Switch)BitConverter.ToInt16(rawBytes, offset + 10)
                });

                offset += 12;
            }

            return Type13;
        }

        public static List<byte> Write(List<BAC_Type13> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_10));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("AnimationModification")]
    [Serializable]
    public class BAC_Type14 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "AnimationModification"; } }

        
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }

        public static List<BAC_Type14> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type14> Type14 = new List<BAC_Type14>();

            for (int i = 0; i < count; i++)
            {
                Type14.Add(new BAC_Type14()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10)
                });

                offset += 12;
            }

            return Type14;
        }

        public static List<byte> Write(List<BAC_Type14> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("TransformControl")]
    [Serializable]
    public class BAC_Type15 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "TransformControl"; } }

        
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Type")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Parameter")]
        //[YAXFormat("0.0########")]
        public string F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        //[YAXFormat("0.0########")]
        public string F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        //[YAXFormat("0.0########")]
        public string F_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }

        #region ParamProps
        [YAXDontSerialize]
        public float Param1_Float { get { return float.Parse(F_12); } set { F_12 = value.ToString(); } }
        [YAXDontSerialize]
        public float Param2_Float { get { return float.Parse(F_16); } set { F_16 = value.ToString(); } }
        [YAXDontSerialize]
        public float Param3_Float { get { return float.Parse(F_20); } set { F_20 = value.ToString(); } }

        #endregion

        public static List<BAC_Type15> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type15> Type15 = new List<BAC_Type15>();

            for (int i = 0; i < count; i++)
            {
                Type15.Add(new BAC_Type15()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    Param1_Float = BitConverter.ToSingle(rawBytes, offset + 12),
                    Param2_Float = BitConverter.ToSingle(rawBytes, offset + 16),
                    Param3_Float = BitConverter.ToSingle(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28)
                });

                offset += 32;
            }

            return Type15;
        }

        public static List<byte> Write(List<BAC_Type15> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.Param1_Float));
                bytes.AddRange(BitConverter.GetBytes(type.Param2_Float));
                bytes.AddRange(BitConverter.GetBytes(type.Param3_Float));
                bytes.AddRange(BitConverter.GetBytes(type.I_24));
                bytes.AddRange(BitConverter.GetBytes(type.I_28));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("ScreenEffect")]
    [Serializable]
    public class BAC_Type16 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "ScreenEffect"; } }


        [YAXAttributeFor("BPE_Index")]
        [YAXSerializeAs("value")]
        public string I_08 { get; set; } = "0"; //ushort
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }

        //Properties
        [YAXDontSerialize]
        public ushort BpeIndex { get { return ushort.Parse(I_08); } set { I_08 = value.ToString(); } }

        public static List<BAC_Type16> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type16> Type16 = new List<BAC_Type16>();

            for (int i = 0; i < count; i++)
            {
                Type16.Add(new BAC_Type16()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    BpeIndex = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                });

                offset += 32;
            }

            return Type16;
        }

        public static List<byte> Write(List<BAC_Type16> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.BpeIndex));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("ThrowHandler")]
    [Serializable]
    public class BAC_Type17 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "ThrowHandler"; } }

        
        [YAXAttributeFor("TH_FLAGS")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_08 { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Bone_User_Connects_To_Victim_From")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Bone_Victim_Connects_To_User_From")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("BAC_Entry_ID")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; } //ushort
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }

        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("X")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Y")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Z")]
        public float F_28 { get; set; }
        
        //Props
        [YAXDontSerialize]
        public ushort BacEntryId { get { return I_16; } set { I_16 = value; } }

        public static List<BAC_Type17> Read(byte[] rawBytes, List<byte> bytes, int offset, int count, bool isSmall)
        {
            List<BAC_Type17> Type17 = new List<BAC_Type17>();

            for (int i = 0; i < count; i++)
            {
                if (!isSmall)
                {
                    Type17.Add(new BAC_Type17()
                    {
                        StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                        Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                        Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                        I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                        I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                        I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                        F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    });
                    offset += 32;
                }
                else
                {
                    Type17.Add(new BAC_Type17()
                    {
                        StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                        Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                        Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                        I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                        I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                        I_18 = BitConverter.ToUInt16(rawBytes, offset + 18)
                    });
                    offset += 20;
                }



            }

            return Type17;
        }

        public static List<byte> Write(List<BAC_Type17> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));

                //Displacement values are now always written
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("PhysicsObjectControl")]
    [Serializable]
    public class BAC_Type18 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "PhysicsObjectControl"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }

        public static List<BAC_Type18> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type18> Type18 = new List<BAC_Type18>();

            for (int i = 0; i < count; i++)
            {
                Type18.Add(new BAC_Type18()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                });

                offset += 32;
            }

            return Type18;
        }

        public static List<byte> Write(List<BAC_Type18> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.I_28));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Aura")]
    [Serializable]
    public class BAC_Type19 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "Aura"; } }


        public enum AuraType : ushort
        {
            BoostStart = 0,
            BoostLoop = 1,
            BoostEnd = 2,
            KiaiCharge = 3,
            KiryokuMax = 4,
            HenshinStart = 5,
            HenshinEnd = 6
        }

        public enum Switch : ushort
        {
            On = 0,
            Off = 1,
            On_8 = 8,
            Off_9 = 9
        }

        
        [YAXAttributeFor("Aura")]
        [YAXSerializeAs("Type")]
        public AuraType I_08 { get; set; } //uint16
        [YAXAttributeFor("Aura")]
        [YAXSerializeAs("Switch")]
        public Switch I_10 { get; set; } // uint16
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        
        public static List<BAC_Type19> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type19> Type19 = new List<BAC_Type19>();

            for (int i = 0; i < count; i++)
            {
                Type19.Add(new BAC_Type19()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = (AuraType)BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = (Switch)BitConverter.ToInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                });

                offset += 16;
            }

            return Type19;
        }

        public static List<byte> Write(List<BAC_Type19> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("HomingMovement")]
    [Serializable]
    public class BAC_Type20 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "HomingMovement"; } }

        
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("Horizontal_Homing_Arc_Direction")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Speed_Modifier")]
        [YAXSerializeAs("value")]
        public string I_12 { get; set; } = "0"; // Either UInt or Float, depending on I_10 (7 = float, else int)
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("X")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Y")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Z")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }

        public static List<BAC_Type20> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type20> Type20 = new List<BAC_Type20>();

            for (int i = 0; i < count; i++)
            {
                string I_12 = String.Empty;

                if (BitConverter.ToInt16(rawBytes, offset + 10) == 7)
                {
                    I_12 = BitConverter.ToSingle(rawBytes, offset + 12).ToString();
                }
                else
                {
                    I_12 = BitConverter.ToUInt32(rawBytes, offset + 12).ToString();
                }

                Type20.Add(new BAC_Type20()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = I_12,
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                });

                offset += 48;
            }

            return Type20;
        }

        public static List<byte> Write(List<BAC_Type20> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));

                if (type.I_10 == 7)
                {
                    bytes.AddRange(BitConverter.GetBytes(Single.Parse(type.I_12)));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(UInt32.Parse(type.I_12)));
                }

                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.I_40));
                bytes.AddRange(BitConverter.GetBytes(type.I_44));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("EyeMovement")]
    [Serializable]
    public class BAC_Type21 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "EyeMovement"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_28 { get; set; }

        public static List<BAC_Type21> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type21> Type21 = new List<BAC_Type21>();

            for (int i = 0; i < count; i++)
            {
                Type21.Add(new BAC_Type21()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                });

                offset += 32;
            }

            return Type21;
        }

        public static List<byte> Write(List<BAC_Type21> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("BAC_Type22")]
    [Serializable]
    public class BAC_Type22 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "BAC_Type22"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_12 { get; set; }
        [YAXAttributeFor("STR_16")]
        [YAXSerializeAs("value")]
        public string STR_16 { get; set; }

        public static List<BAC_Type22> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type22> Type22 = new List<BAC_Type22>();

            for (int i = 0; i < count; i++)
            {
                Type22.Add(new BAC_Type22()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    STR_16 = Utils.GetString(rawBytes.ToList(), offset + 16, 32)
                });

                offset += 48;
            }

            return Type22;
        }

        public static List<byte> Write(List<BAC_Type22> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                if (type.STR_16.Length > 32)
                {
                    throw new InvalidDataException(String.Format("BAcType22 > STR_16: \"{0}\" exceeds the maximum length of 32!", type.STR_16));
                }

                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.F_12));
                bytes.AddRange(Encoding.ASCII.GetBytes(type.STR_16));

                int remaingSpace = 32 - type.STR_16.Length;
                bytes.AddRange(new byte[remaingSpace]);
            }

            return bytes;
        }
    }

    [YAXSerializeAs("TransparencyEffect")]
    [Serializable]
    public class BAC_Type23 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "TransparencyEffect"; } }

        
        [YAXAttributeFor("Transparency_Flags")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_08 { get; set; } //int16
        [YAXAttributeFor("Transparency_Flags_2")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_10 { get; set; } //int16
        [YAXAttributeFor("Dilution")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0########")]
        public float F_32 { get; set; }


        [YAXAttributeFor("F_36")]
        [YAXFormat("0.0########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("value")]
        public float[] F_36 { get; set; } //size 7

        public static List<BAC_Type23> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type23> Type23 = new List<BAC_Type23>();

            for (int i = 0; i < count; i++)
            {
                Type23.Add(new BAC_Type23()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 36, 7)
                });

                offset += 64;
            }

            return Type23;
        }

        public static List<byte> Write(List<BAC_Type23> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter_Ex.GetBytes(type.F_36));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("DualSkill")]
    [Serializable]
    public class BAC_Type24 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "DualSkill"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("Position_Initiator")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Position_Initiator")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Position_Initiator")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }
        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("Position_Partner")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Position_Partner")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Position_Partner")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }

        public static List<BAC_Type24> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type24> Type24 = new List<BAC_Type24>();

            for (int i = 0; i < count; i++)
            {
                Type24.Add(new BAC_Type24()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                    I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    I_52 = BitConverter.ToUInt16(rawBytes, offset + 52),
                    I_54 = BitConverter.ToUInt16(rawBytes, offset + 54),
                });

                offset += 56;
            }

            return Type24;
        }

        public static List<byte> Write(List<BAC_Type24> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_34));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes(type.F_44));
                bytes.AddRange(BitConverter.GetBytes(type.F_48));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_54));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("BAC_Type25")]
    [Serializable]
    public class BAC_Type25 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "BAC_Type25"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }

        public static List<BAC_Type25> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type25> Type25 = new List<BAC_Type25>();

            for (int i = 0; i < count; i++)
            {
                Type25.Add(new BAC_Type25()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12)
                });

                offset += 16;
            }

            return Type25;
        }

        public static List<byte> Write(List<BAC_Type25> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("BAC_Type26")]
    [Serializable]
    public class BAC_Type26 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "BAC_Type26"; } }

        
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_76 { get; set; }

        public static List<BAC_Type26> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type26> Type26 = new List<BAC_Type26>();

            for (int i = 0; i < count; i++)
            {
                Type26.Add(new BAC_Type26()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60),
                    I_64 = BitConverter.ToInt32(rawBytes, offset + 64),
                    I_68 = BitConverter.ToInt32(rawBytes, offset + 68),
                    I_72 = BitConverter.ToInt32(rawBytes, offset + 72),
                    I_76 = BitConverter.ToInt32(rawBytes, offset + 76)
                });

                offset += 80;
            }

            return Type26;
        }

        public static List<byte> Write(List<BAC_Type26> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_24));
                bytes.AddRange(BitConverter.GetBytes(type.I_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.I_40));
                bytes.AddRange(BitConverter.GetBytes(type.I_44));
                bytes.AddRange(BitConverter.GetBytes(type.I_48));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_56));
                bytes.AddRange(BitConverter.GetBytes(type.I_60));
                bytes.AddRange(BitConverter.GetBytes(type.I_64));
                bytes.AddRange(BitConverter.GetBytes(type.I_68));
                bytes.AddRange(BitConverter.GetBytes(type.I_72));
                bytes.AddRange(BitConverter.GetBytes(type.I_76));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("EffectPropertyControl")]
    [Serializable]
    public class BAC_Type27 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public string Type { get { return "EffectPropertyControl"; } }


        [YAXAttributeFor("SkillID")]
        [YAXSerializeAs("value")]
        public string I_08 { get; set; } //uint16
        [YAXAttributeFor("SkillType")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("EffectID")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("FunctionDuration")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_22 { get; set; }

        public static List<BAC_Type27> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type27> Type26 = new List<BAC_Type27>();

            for (int i = 0; i < count; i++)
            {
                Type26.Add(new BAC_Type27()
                {
                    StartTime = BitConverter.ToInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8).ToString(),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                });

                offset += 24;
            }

            return Type26;
        }

        public static List<byte> Write(List<BAC_Type27> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(type.I_08)));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
            }

            if (bytes.Count != 24 * types.Count) throw new InvalidDataException("BacType27 invalid size.");

            return bytes;
        }
    }



}
