using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.BSA
{
    public enum EepkType
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

    public enum AcbType
    {
        Common_SE = 0,
        Chara_SE = 1,
        Skill_SE = 3
        //Chara_VOX = 2 and Skill_VOX = 4?
    }

    public enum Switch
    {
        On = 0,
        Off = 1
    }

    public enum ProjectileProtectionOperation : ushort
    {
        EnableOrReplace = 0,
        DisableWithoutSignal = 1
    }

    public enum SpatialEffectGeometryMode : ushort
    {
        Default = 0,
        DistanceRelative = 1,
        FullVector = 2
    }


    [YAXSerializeAs("BSA")]
    [Serializable]
    public class BSA_File : ISorting, IIsNull
    {
        [YAXAttributeForClass]
        public Int64 I_08 = 0;
        [YAXAttributeForClass]
        public Int16 I_16 = 0;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Entry")]
        public List<BSA_Entry> BSA_Entries { get; set; } = new List<BSA_Entry>();

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static BSA_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetBsaFile();
        }

        public static BSA_File Load(string path)
        {
            return new Parser(path, false).GetBsaFile();
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }

        public void SortEntries()
        {
            BSA_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        /// <summary>
        /// Adds the entry at the requested ID. Duplicate IDs are rejected because the deserializer sizes the
        /// main entry pointer table by unique ID but writes bodies by list position, so a duplicate throws
        /// from deep inside the writer on save.
        /// </summary>
        public void AddEntry(int id, BSA_Entry entry)
        {
            if (BSA_Entries.Any(existing => existing.SortID == id))
                throw new ArgumentException($"A BSA entry with the ID {id} already exists.", nameof(id));

            entry.SortID = id;
            BSA_Entries.Add(entry);
        }

        public int AddEntry(BSA_Entry entry)
        {
            entry.SortID = GetFreeId();
            BSA_Entries.Add(entry);
            return entry.SortID;
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public bool IsNull()
        {
            return (BSA_Entries.Count == 0);
        }

        private int GetFreeId()
        {
            int id = 0;
            while (BSA_Entries.Any(c => c.SortID == id) && id < int.MaxValue)
                id++;
            return id;
        }

        #region IBsaTypesMethods
        public void InitializeIBsaTypes()
        {
            foreach (var bsaEntry in BSA_Entries)
            {
                bsaEntry.InitializeIBsaTypes();
            }
        }

        public void SaveIBsaTypes()
        {
            foreach (var bsaEntry in BSA_Entries)
            {
                bsaEntry.SaveIBsaTypes();
            }
        }
        #endregion

        public void ChangeNeutralSkillId(ushort newId)
        {
            foreach (BSA_Entry entry in BSA_Entries)
            {
                if (entry.SubEntries?.CollisionEntries != null)
                {
                    foreach (var collision in entry.SubEntries.CollisionEntries)
                    {
                        if (collision.SkillID == 0xBACA)
                            collision.SkillID = newId;
                    }
                }

                if(entry.Type6 != null)
                {
                    foreach(var effect in entry.Type6)
                    {
                        if (effect.SkillID == 0xBACA)
                            effect.SkillID = newId;
                    }
                }

                if (entry.Type12 != null)
                {
                    foreach (var type12 in entry.Type12)
                    {
                        if (type12.SkillID == 0xBACA)
                            type12.SkillID = newId;
                    }
                }
            }
        }
    }

    [YAXSerializeAs("BSA_Entry")]
    [Serializable]
    public class BSA_Entry : IUserDefinedName, IInstallable, INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region NonSerialized
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } set { Index = value.ToString(); } }

        private string _userDefinedName;
        [YAXDontSerialize]
        public string UserDefinedName
        {
            get => _userDefinedName;
            set
            {
                if (_userDefinedName == value) return;
                _userDefinedName = value;
                NotifyPropertyChanged(nameof(UserDefinedName));
                NotifyPropertyChanged(nameof(HasUserDefinedName));
            }
        }
        [YAXDontSerialize]
        public bool HasUserDefinedName => !string.IsNullOrWhiteSpace(UserDefinedName);
        #endregion

        private string _index = "0";
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index //int32
        {
            get => _index;
            set
            {
                if (_index == value) return;
                _index = value;
                NotifyPropertyChanged(nameof(Index));
                NotifyPropertyChanged(nameof(SortID));
            }
        }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("ImpactPropeties")]
        [YAXSerializeAs("a")]
        public byte I_16_a { get; set; }
        [YAXAttributeFor("ImpactPropeties")]
        [YAXSerializeAs("b")]
        public byte I_16_b { get; set; }
        [YAXAttributeFor("I_17")]
        [YAXSerializeAs("value")]
        public byte I_17 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public int I_18 { get; set; }
        private ushort _i22;
        [YAXAttributeFor("Lifetime")]
        [YAXSerializeAs("value")]
        public ushort I_22
        {
            get => _i22;
            set
            {
                if (_i22 == value) return;
                _i22 = value;
                NotifyPropertyChanged(nameof(I_22));
            }
        }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("Expires")]
        public ushort Expires { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactProjectile")]
        public ushort ImpactProjectile { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactEnemy")]
        public ushort ImpactEnemy { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactGround")]
        public ushort ImpactGround { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_40 { get; set; } = new int[3]; // size 3

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AfterEffects")]
        [BindingSubList]
        public BSA_SubEntries SubEntries { get; set; } = new BSA_SubEntries();

        //Types
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BsaEntryPassing")]
        [BindingSubList]
        public List<BSA_Type0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Movement")]
        [BindingSubList]
        public List<BSA_Type1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ProjectileTimelineRemap")]
        [BindingSubList]
        public List<BSA_Type2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        [BindingSubList]
        public List<BSA_Type3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Deflection")]
        [BindingSubList]
        public List<BSA_Type4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        [BindingSubList]
        public List<BSA_Type6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        [BindingSubList]
        public List<BSA_Type7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type8")]
        [BindingSubList]
        public List<BSA_Type8> Type8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type10")]
        [BindingSubList]
        public List<BSA_Type10> Type10 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SendProjectileSignal")]
        [BindingSubList]
        public List<BSA_Type12> Type12 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ProjectileProtection")]
        [BindingSubList]
        public List<BSA_Type13> Type13 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EffectPlacement")]
        [BindingSubList]
        public List<BSA_Type14> Type14 { get; set; }
        #region IBsaTypes
        [YAXDontSerialize]
        public AsyncObservableCollection<IBsaType> IBsaTypes { get; set; }

        public void InitializeIBsaTypes()
        {
            InitBsaLists();

            IBsaTypes = AsyncObservableCollection<IBsaType>.Create();

            foreach (var bsaEntry in Type0)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type1)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type2)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type3)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type4)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type6)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type7)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type8)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type10)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type12)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type13)
                IBsaTypes.Add(bsaEntry);
            foreach (var bsaEntry in Type14)
                IBsaTypes.Add(bsaEntry);

        }

        public void SaveIBsaTypes()
        {
            ClearBsaLists();

            foreach (var bsaEntry in IBsaTypes)
            {
                if (bsaEntry is BSA_Type0 type)
                {
                    Type0.Add(type);
                }
                else if (bsaEntry is BSA_Type1 type1)
                {
                    Type1.Add(type1);
                }
                else if (bsaEntry is BSA_Type2 type2)
                {
                    Type2.Add(type2);
                }
                else if (bsaEntry is BSA_Type3 type3)
                {
                    Type3.Add(type3);
                }
                else if (bsaEntry is BSA_Type4 type4)
                {
                    Type4.Add(type4);
                }
                else if (bsaEntry is BSA_Type6 type6)
                {
                    Type6.Add(type6);
                }
                else if (bsaEntry is BSA_Type7 type7)
                {
                    Type7.Add(type7);
                }
                else if (bsaEntry is BSA_Type8 type8)
                {
                    Type8.Add(type8);
                }
                else if (bsaEntry is BSA_Type10 type10)
                {
                    Type10.Add(type10);
                }
                else if (bsaEntry is BSA_Type12 type12)
                {
                    Type12.Add(type12);
                }
                else if (bsaEntry is BSA_Type13 type13)
                {
                    Type13.Add(type13);
                }
                else if (bsaEntry is BSA_Type14 type14)
                {
                    Type14.Add(type14);
                }
            }
        }

        /// <summary>
        /// Inserts the type at the end of its own TypeID block, so the in-memory order always matches the
        /// order the file will be written in. The BSA format stores one header per type, so cross-type order
        /// cannot round-trip.
        /// </summary>
        public IUndoRedo AddIBsaType(IBsaType type)
        {
            if (IBsaTypes == null)
                InitializeIBsaTypes();

            int insertIdx = IBsaTypes.TakeWhile(x => x.TypeID <= type.TypeID).Count();

            IBsaTypes.Insert(insertIdx, type);
            return new UndoableListInsert<IBsaType>(IBsaTypes, insertIdx, type, "BSA Subtype Add");
        }

        private void InitBsaLists()
        {
            if (Type0 == null)
                Type0 = new List<BSA_Type0>();
            if (Type1 == null)
                Type1 = new List<BSA_Type1>();
            if (Type2 == null)
                Type2 = new List<BSA_Type2>();
            if (Type3 == null)
                Type3 = new List<BSA_Type3>();
            if (Type4 == null)
                Type4 = new List<BSA_Type4>();
            if (Type6 == null)
                Type6 = new List<BSA_Type6>();
            if (Type7 == null)
                Type7 = new List<BSA_Type7>();
            if (Type8 == null)
                Type8 = new List<BSA_Type8>();
            if (Type10 == null)
                Type10 = new List<BSA_Type10>();
            if (Type12 == null)
                Type12 = new List<BSA_Type12>();
            if (Type13 == null)
                Type13 = new List<BSA_Type13>();
            if (Type14 == null)
                Type14 = new List<BSA_Type14>();
        }

        private void ClearBsaLists()
        {
            InitBsaLists();

            Type0.Clear();
            Type1.Clear();
            Type2.Clear();
            Type3.Clear();
            Type4.Clear();
            Type6.Clear();
            Type7.Clear();
            Type8.Clear();
            Type10.Clear();
            Type12.Clear();
            Type13.Clear();
            Type14.Clear();
        }

        #endregion

    }

    [YAXSerializeAs("AfterEffects")]
    [Serializable]
    public class BSA_SubEntries
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CollisionEffect")]
        [BindingSubList]
        public List<BSA_Collision> CollisionEntries { get; set; } = new List<BSA_Collision>();
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CollisionSound")]
        [BindingSubList]
        public List<BSA_Expiration> ExpirationEntries { get; set; } = new List<BSA_Expiration>();
    }

    [YAXSerializeAs("CollisionEffect")]
    [YAXAltAliases("Collision")]
    [BindingSubClass]
    [Serializable]
    public class BSA_Collision : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private EepkType _eepkType;
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkType EepkType //int16
        {
            get => _eepkType;
            set
            {
                if (_eepkType == value) return;
                _eepkType = value;
                NotifyPropertyChanged(nameof(EepkType));
            }
        }

        private ushort _skillID;
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public ushort SkillID
        {
            get => _skillID;
            set
            {
                if (_skillID == value) return;
                _skillID = value;
                NotifyPropertyChanged(nameof(SkillID));
            }
        }

        private ushort _effectID;
        [YAXAttributeFor("Effect_ID")]
        [YAXSerializeAs("value")]
        public ushort EffectID
        {
            get => _effectID;
            set
            {
                if (_effectID == value) return;
                _effectID = value;
                NotifyPropertyChanged(nameof(EffectID));
            }
        }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }

        public static List<BSA_Collision> ChangeSkillId(List<BSA_Collision> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].EepkType)
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

    }

    [YAXSerializeAs("CollisionSound")]
    [YAXAltAliases("Expiration")]
    [Serializable]
    public class BSA_Expiration : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private AcbType _i00;
        [YAXAltAliases("I_00/value")]
        [YAXAttributeFor("ACB_Type")]
        [YAXSerializeAs("value")]
        public AcbType I_00
        {
            get => _i00;
            set
            {
                if (_i00 == value) return;
                _i00 = value;
                NotifyPropertyChanged(nameof(I_00));
            }
        }

        private ushort _i02;
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02
        {
            get => _i02;
            set
            {
                if (_i02 == value) return;
                _i02 = value;
                NotifyPropertyChanged(nameof(I_02));
            }
        }

        private ushort _i04;
        [YAXAltAliases("I_04/value")]
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public ushort I_04
        {
            get => _i04;
            set
            {
                if (_i04 == value) return;
                _i04 = value;
                NotifyPropertyChanged(nameof(I_04));
            }
        }

        private ushort _i06;
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06
        {
            get => _i06;
            set
            {
                if (_i06 == value) return;
                _i06 = value;
                NotifyPropertyChanged(nameof(I_06));
            }
        }
    }

    //Types
    [YAXSerializeAs("BsaEntryPassing")]
    [BindingSubClass]
    [Serializable]
    public class BSA_Type0 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 0;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Main_Condition")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("BSA_Entry")]
        [YAXSerializeAs("ID")]
        public ushort BSA_EntryID { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
        [YAXAttributeFor("Bac_Condition")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }

    }

    [YAXSerializeAs("Movement")]
    [Serializable]
    public class BSA_Type1 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 1;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("Motion_Flags")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_00 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Falloff Strength")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_44 { get; set; }
    }

    [YAXSerializeAs("ProjectileTimelineRemap")]
    [YAXAltAliases("BSA_Type2")]
    [Serializable]
    public class BSA_Type2 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 2;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAltAliases("I_02/value")]
        [YAXAttributeFor("Output_Start_Frame")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAltAliases("I_04/value")]
        [YAXAttributeFor("Output_End_Frame")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
    }

    [YAXSerializeAs("Hitbox")]
    [BindingSubClass]
    [Serializable]
    public class BSA_Type3 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 3;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("a")]
        public byte I_06_a { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("b")]
        public byte I_06_b { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("c")]
        public byte I_06_c { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("d")]
        public byte I_06_d { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Hitbox_Scale")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Hit_Amount")]
        [YAXSerializeAs("value")]
        public UInt16 I_48 { get; set; }
        [YAXAttributeFor("Hitbox_Lifetime")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public UInt16 I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public UInt16 I_54 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public UInt16 I_56 { get; set; }
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("FirstHit")]
        public ushort FirstHit { get; set; }
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("MultipleHits")]
        public ushort MultipleHits { get; set; }
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("LastHit")]
        public ushort LastHit { get; set; }

    }

    [YAXSerializeAs("Deflection")]
    [Serializable]
    public class BSA_Type4 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 4;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }

        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
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

        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
    }

    [YAXSerializeAs("Effect")]
    [BindingSubClass]
    [Serializable]
    public class BSA_Type6 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 6;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkType EepkType { get; set; } //Int16
        [YAXAttributeFor("Skill ID")]
        [YAXSerializeAs("value")]
        public ushort SkillID { get; set; }
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("ID")]
        public ushort EffectID { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("Switch")]
        public Switch I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }


        public bool IsSkillEepk()
        {
            switch (EepkType)
            {
                case EepkType.SuperSkill:
                case EepkType.UltimateSkill:
                case EepkType.EvasiveSkill:
                case EepkType.AwokenSkill:
                case EepkType.KiBlastSkill:
                    return true;
            }
            return false;
        }

        public static List<BSA_Type6> ChangeSkillId(List<BSA_Type6> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].EepkType)
                {
                    case EepkType.SuperSkill:
                    case EepkType.UltimateSkill:
                    case EepkType.EvasiveSkill:
                    case EepkType.AwokenSkill:
                    case EepkType.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }

    }

    [YAXSerializeAs("Sound")]
    [Serializable]
    public class BSA_Type7 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 7;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("ACB_File")]
        [YAXSerializeAs("value")]
        public AcbType AcbType { get; set; } //int16
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Cue ID")]
        [YAXSerializeAs("value")]
        public ushort CueId { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
    }

    [YAXSerializeAs("BSA_Type8")]
    [Serializable]
    public class BSA_Type8 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 8;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
    }

    [YAXSerializeAs("BSA_Type10")]
    [Serializable]
    public class BSA_Type10 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 10;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }

    }


    [YAXSerializeAs("SendProjectileSignal")]
    [YAXAltAliases("BSA_Type12")]
    [Serializable]
    public class BSA_Type12 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 12;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAltAliases("F_00/value")]
        [YAXAttributeFor("Signal_Value")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAltAliases("EepkType/value")]
        [YAXAttributeFor("Skill_Type")]
        [YAXSerializeAs("value")]
        public EepkType EepkType { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public int SkillID { get; set; }
        [YAXAltAliases("I_12/value")]
        [YAXAttributeFor("Delivery_Mode")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAltAliases("F_16/value")]
        [YAXAttributeFor("Pause_Recipient_Timeline")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
    }

    [YAXSerializeAs("ProjectileProtection")]
    [YAXAltAliases("BSA_Type13")]
    [Serializable]
    public class BSA_Type13 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 13;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAltAliases("I_00/value")]
        [YAXAttributeFor("Protection")]
        [YAXSerializeAs("State")]
        public ProjectileProtectionOperation I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("Max_Hitbox_Power")]
        [YAXSerializeAs("value")]
        public float F_04 { get; set; }
        [YAXAltAliases("F_08/value")]
        [YAXAttributeFor("Protect_Selectors_0_3")]
        [YAXSerializeAs("value")]
        public float F_08 { get; set; }
        [YAXAltAliases("I_12/value:int")]
        [YAXAttributeFor("Protect_Additional_Selectors")]
        [YAXSerializeAs("value")]
        public float I_12 { get; set; }
        [YAXAltAliases("F_16/value")]
        [YAXAttributeFor("Entry_Passing_Signal")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAltAliases("I_20/value:int")]
        [YAXAttributeFor("Mark_Protected_Hit")]
        [YAXSerializeAs("value")]
        public float I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAltAliases("I_26/value")]
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }


    }

    [YAXSerializeAs("EffectPlacement")]
    [YAXAltAliases("BSA_Type14")]
    [Serializable]
    public class BSA_Type14 : BSA_TypeBase
    {
        [YAXDontSerialize]
        public override int TypeID => 14;

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public override ushort StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public override ushort Duration { get; set; }
        [YAXAltAliases("I_00/value")]
        [YAXAttributeFor("Placement_Mode")]
        [YAXSerializeAs("value")]
        public SpatialEffectGeometryMode I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAltAliases("F_04/value:float")]
        [YAXAttributeFor("Placement_Flags")]
        [YAXSerializeAs("value")]
        public uint F_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public uint I_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public uint I_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public uint I_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public uint I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public uint I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public uint I_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }
        [YAXDontSerialize]
        public uint I_48 { get; set; }

        [YAXAltAliases("I_48/value")]
        [YAXAttributeFor("Legacy_I_48")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public uint? LegacyI48
        {
            // Read the packed legacy field without writing it back to XML.
            set
            {
                if (value.HasValue)
                    I_48 = value.Value;
            }
        }

        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public EepkType EepkType
        {
            get => (EepkType)(I_48 & 0xFFFFu);
            set => I_48 = (I_48 & 0xFFFF0000u) | (uint)value;
        }

        [YAXAttributeFor("Transform")]
        [YAXSerializeAs("Selector")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public ushort TransformSelector
        {
            get => (ushort)(I_48 >> 16);
            set => I_48 = (I_48 & 0x0000FFFFu) | ((uint)value << 16);
        }

        [YAXAltAliases("F_52/value:float")]
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public uint F_52 { get; set; }
        [YAXAltAliases("I_56/value")]
        [YAXAttributeFor("Effect_ID")]
        [YAXSerializeAs("value")]
        public uint I_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public uint I_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public uint I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public uint I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public uint I_80 { get; set; }
        [YAXAltAliases("I_84/value")]
        [YAXAttributeFor("Effect_Placement_Flags")]
        [YAXSerializeAs("value")]
        public uint I_84 { get; set; }
    }

    public static class BsaTypeNames
    {
        public static string GetBaseName(IBsaType type)
        {
            switch (type)
            {
                case BSA_Type0 _:
                    return "Entry Passing";
                case BSA_Type1 _:
                    return "Movement";
                case BSA_Type2 _:
                    return "Projectile Timeline Remap";
                case BSA_Type3 _:
                    return "Hitbox";
                case BSA_Type4 _:
                    return "Deflection";
                case BSA_Type6 _:
                    return "Effect";
                case BSA_Type7 _:
                    return "Sound";
                case BSA_Type8 _:
                    return "Screen Effect";
                case BSA_Type10 _:
                    return "Unknown 10";
                case BSA_Type12 _:
                    return "Send Projectile Signal";
                case BSA_Type13 _:
                    return "Projectile Protection";
                case BSA_Type14 _:
                    return "Effect Placement";
                default:
                    return type?.GetType().Name ?? string.Empty;
            }
        }

        public static string GetName(IBsaType type)
        {
            switch (type)
            {
                case BSA_Type0 type0:
                    return $"Entry Passing ({type0.BSA_EntryID}, 0x{type0.I_02:X}, {type0.F_08:0.###})";
                case BSA_Type1 _:
                    return "Movement";
                case BSA_Type2 type2:
                    return $"Projectile Timeline Remap ({type2.I_00}, {type2.I_02}, {type2.I_04})";
                case BSA_Type3 _:
                    return "Hitbox";
                case BSA_Type4 type4:
                    return $"Deflection ({type4.I_00}, {type4.I_04})";
                case BSA_Type6 type6:
                    return $"Effect ({type6.EepkType}, {type6.SkillID}, {type6.EffectID}, {type6.I_08})";
                case BSA_Type7 type7:
                    return $"Sound ({type7.AcbType}, {type7.CueId})";
                case BSA_Type8 type8:
                    return $"Screen Effect ({type8.I_00}, {type8.I_02})";
                case BSA_Type10 type10:
                    return $"Unknown 10 ({type10.I_00}, {type10.I_04}, {type10.I_06})";
                case BSA_Type12 type12:
                    return $"Send Projectile Signal ({type12.EepkType}, {type12.SkillID}, {type12.I_12})";
                case BSA_Type13 type13:
                    return $"Projectile Protection ({GetProtectionStateName(type13.I_00)}, {type13.F_04:0.###}, {type13.I_12:0.###})";
                case BSA_Type14 type14:
                    return $"Effect Placement ({type14.EepkType}, {type14.F_52}, {type14.I_56})";
                default:
                    return type?.GetType().Name ?? string.Empty;
            }
        }

        private static string GetProtectionStateName(ProjectileProtectionOperation state)
        {
            switch (state)
            {
                case ProjectileProtectionOperation.EnableOrReplace:
                    return "On";
                case ProjectileProtectionOperation.DisableWithoutSignal:
                    return "Off";
                default:
                    return $"Unknown ({(ushort)state})";
            }
        }
    }

    [Serializable]
    public abstract class BSA_TypeBase : IBsaType
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract ushort StartTime { get; set; }
        public abstract ushort Duration { get; set; }

        [YAXDontSerialize]
        public abstract int TypeID { get; }

        [YAXDontSerialize]
        public virtual string Type => BsaTypeNames.GetName(this);

        [YAXDontSerialize]
        public virtual string TypeName => BsaTypeNames.GetBaseName(this);

        public void RefreshType()
        {
            NotifyPropertyChanged(nameof(Type));
            NotifyPropertyChanged(nameof(TypeName));
            NotifyPropertyChanged(nameof(StartTime));
            NotifyPropertyChanged(nameof(Duration));
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface IBsaType : INotifyPropertyChanged
    {
        ushort StartTime { get; set; }
        ushort Duration { get; set; }
        int TypeID { get; }
        string Type { get; }
        string TypeName { get; }
        void RefreshType();
    }
}
