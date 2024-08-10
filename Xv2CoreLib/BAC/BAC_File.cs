using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;
using Xv2CoreLib.Resource;
using static Xv2CoreLib.BAC.BAC_Entry;
using System.Windows.Media;
using Xv2CoreLib.Resource.App;

#if UndoRedo
using Xv2CoreLib.Resource.UndoRedo;
#endif

namespace Xv2CoreLib.BAC
{
    public interface IBacType : ITimeLineItem
    {
        int TypeID { get; }

        ushort StartTime { get; set; } //0
        ushort Duration { get; set; } //2
        ushort I_04 { get; set; } //4
        ushort Flags { get; set; } //6

        int TimesActivated { get; set; }

        void RefreshType();
    }

    [YAXSerializeAs("BAC")]
    [Serializable]
    public class BAC_File : ISorting, IIsNull
    {
        public const int BAC_TYPE_COUNT = 32;

        public static Dictionary<int, string> BacTypeNames { get; private set; } = new Dictionary<int, string>()
        {
            {0, "Animation" }, {1, "Hitbox"}, {2, "Movement"}, {3, "Invulnerability"}, {4, "Time Scale"}, {5, "Tracking" },
            {6, "Charge Control" }, {7, "BCM Callback"}, {8, "Effect"}, {9, "Projectile" }, {10, "Camera" }, {11, "Sound" },
            {12, "Targeting Assistance" }, {13, "BCS Part Visibility" }, {14, "Bone Modification" }, {15, "Functions" },
            {16, "Post Effect" }, {17, "Throw Handler" }, {18, "Physics Object" }, {19, "Aura" }, {20, "Homing Movement" },
            {21, "Eye Movement" }, {22, "BAC_Type22" }, {23, "Transparency Effect" }, {24, "Dual Skill Handler"}, {25, "Extended Charge Control"},
            {26, "Extended Camera Control" }, {27, "Effect Property Control" }, {28, "BAC_Type28"}, {29, "BAC_Type29"}, {30, "BAC_Type30"}, {31, "BAC_Type31"}
        };

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0)]
        public int DefaultEmptyEndIndex { get; set; }

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
        public AsyncObservableCollection<BAC_Entry> BacEntries { get; set; } = new AsyncObservableCollection<BAC_Entry>();


        #region LoadSave
        public static BAC_File DefaultBacFile()
        {
            return new BAC_File();
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

        #endregion

        #region AddRemoveGet
        //Add
        /// <summary>
        /// Adds a BAC Entry and returns the assigned index.
        /// </summary>
        /// <param name="entry">The BAC Entry to add.</param>
        /// <param name="id">The index to add the BAC entry at. If this is -1, then one will be automatically assigned.</param>
        /// <returns></returns>
        public int AddEntry(BAC_Entry entry, int id = -1)
        {
            int idx = BacEntries.IndexOf(BacEntries.FirstOrDefault(x => x.SortID == id));

            if (idx != -1)
            {
                BacEntries[idx] = entry;
                BacEntries[idx].SortID = id;
                return id;
            }

            if (id == -1)
                id = GetFreeId();

            entry.SortID = id;
            BacEntries.Add(entry);
            return id;
        }

        public BAC_Entry AddNewEntry()
        {
            BAC_Entry newEntry = new BAC_Entry();
            int id = AddEntry(newEntry);
            return BacEntries.FirstOrDefault(x => x.SortID == id);
        }


        //Remove
        public void RemoveEntry(BAC_Entry entryToRemove)
        {
            BacEntries.Remove(entryToRemove);
        }


        //Get
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

        #endregion

        #region IBacTypesMethods
        public void InitializeIBacTypes()
        {
            foreach (var bacEntry in BacEntries)
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

        #region Helpers
        public void SortEntries()
        {
            if (BacEntries != null)
                BacEntries = Sorting.SortEntries(BacEntries);
            //BacEntries.Sort((x, y) => x.SortID - y.SortID);
        }

        public void RegenerateIndexes()
        {
            for (int i = 0; i < BacEntries.Count; i++)
            {
                BacEntries[i].Index = i.ToString();
            }
        }

        public bool IsNull()
        {
            foreach (var entry in BacEntries)
                if (!entry.IsBacEntryEmpty()) return false;
            return true;
        }

        public int GetFreeId()
        {
            int id = 0;
            while (BacEntries.Any(c => c.SortID == id) && id < int.MaxValue)
                id++;
            return id;
        }

        /// <summary>
        /// Checks all flag and unknown integer values and reports if any previously unused values now have values. (Works on IBacTypes only, so call after those are initialized)
        /// </summary>
        /// <returns>null if check was successfull, else the name of the value in question.</returns>
        public string ValidateValues()
        {
            foreach (var entry in BacEntries)
            {
                foreach (var type in entry.IBacTypes)
                {
                    if (type is BAC_Type5 type5)
                    {
                        var flag = type5.TrackingFlags.RemoveFlag(BAC_Type5.TrackingFlagsEnum.TrackForwardAndBackwards | BAC_Type5.TrackingFlagsEnum.Unk1 | BAC_Type5.TrackingFlagsEnum.Unk9 | BAC_Type5.TrackingFlagsEnum.Unk10 | BAC_Type5.TrackingFlagsEnum.Unk12 | BAC_Type5.TrackingFlagsEnum.Unk13 | BAC_Type5.TrackingFlagsEnum.Unk14);

                        if ((ushort)flag != 0)
                            return $"TrackingFlags ({HexConverter.GetHexString((ushort)flag)})";
                    }
                    else if (type is BAC_Type7 type7)
                    {
                        var flag = type7.LinkFlags.RemoveFlag(BAC_Type7.BcmCallbackFlagsEnum.KNOWN_MASK);

                        if ((uint)flag != 0)
                            return $"BcmCallback LinkFlags ({HexConverter.GetHexString((uint)flag)})";
                    }
                    else if (type is BAC_Type8 type8)
                    {
                        var flag = type8.EffectFlags.RemoveFlag(BAC_Type8.EffectFlagsEnum.Loop | BAC_Type8.EffectFlagsEnum.Unk2 | BAC_Type8.EffectFlagsEnum.Unk6 | BAC_Type8.EffectFlagsEnum.Off | BAC_Type8.EffectFlagsEnum.SpawnOnTarget | BAC_Type8.EffectFlagsEnum.UserOnly | BAC_Type8.EffectFlagsEnum.Unk8);

                        if ((uint)flag != 0)
                            return $"EffectFlags ({HexConverter.GetHexString((uint)flag)})";

                        if (type8.UseSkillId != BAC_Type8.UseSkillIdEnum.True && type8.UseSkillId != BAC_Type8.UseSkillIdEnum.False)
                            return $"Effect: UseSkillId is neither True or False ({HexConverter.GetHexString((ushort)type8.UseSkillId)})";
                    }
                    else if (type is BAC_Type9 type9)
                    {
                        if (type9.I_47 != 0)
                            return $"Projectile I_47 ({HexConverter.GetHexString(type9.I_47)}) (likely flags)";

                        if (type9.I_56 != 0)
                            return $"Projectile I_56 ({HexConverter.GetHexString(type9.I_56)})";

                        if (type9.I_60 != 0)
                            return $"Projectile I_60 ({HexConverter.GetHexString(type9.I_60)})";

                        if (type9.CanUseCmnBsa != BAC_Type9.CanUseCmnBsaEnum.True && type9.CanUseCmnBsa != BAC_Type9.CanUseCmnBsaEnum.False)
                            return $"Projectile: CanUseCmnBsa is neither True or False ({HexConverter.GetHexString((ushort)type9.CanUseCmnBsa)})";

                        var flag = type9.BsaFlags.RemoveFlag(BAC_Type9.BsaFlagsEnum.TerminatePreviousProjectile | BAC_Type9.BsaFlagsEnum.Unk2 | BAC_Type9.BsaFlagsEnum.Unk3 | BAC_Type9.BsaFlagsEnum.Unk4 | BAC_Type9.BsaFlagsEnum.BcmCondition | BAC_Type9.BsaFlagsEnum.DuplicateForAllOpponents |
                            BAC_Type9.BsaFlagsEnum.Loop | BAC_Type9.BsaFlagsEnum.MarkRandomID | BAC_Type9.BsaFlagsEnum.MarkUniqueID | BAC_Type9.BsaFlagsEnum.Unk10 | BAC_Type9.BsaFlagsEnum.Unk11 | BAC_Type9.BsaFlagsEnum.Unk14);

                        if ((ushort)flag != 0)
                            return $"BsaFlags ({HexConverter.GetHexString((ushort)flag)})";

                    }
                    else if (type is BAC_Type11 type11)
                    {
                        if (type11.I_14 != 0)
                            return $"Sound I_14 ({HexConverter.GetHexString(type11.I_14)})";
                    }
                    else if (type is BAC_Type13 type13)
                    {
                        if (type13.Visibility != BcsPartVisibilitySwitch.On && type13.Visibility != BcsPartVisibilitySwitch.Off)
                            return $"BcsPartVisibility Visibility ({HexConverter.GetHexString((ushort)type13.Visibility)})";
                    }
                    else if (type is BAC_Type16 type16)
                    {
                        if (type16.I_10 != 0)
                            return $"ScreenEffect I_10 ({HexConverter.GetHexString(type16.I_10)})";

                        if (type16.I_16 != 0)
                            return $"ScreenEffect I_16 ({HexConverter.GetHexString(type16.I_16)})";

                        var flag = type16.ScreenEffectFlags.RemoveFlag(BAC_Type16.ScreenEffectFlagsEnum.DisableEffect | BAC_Type16.ScreenEffectFlagsEnum.Unk1 | BAC_Type16.ScreenEffectFlagsEnum.Unk4 | BAC_Type16.ScreenEffectFlagsEnum.Unk5);

                        if ((ushort)flag != 0)
                            return $"ScreenEffectFlags ({HexConverter.GetHexString((ushort)flag)})";
                    }
                    else if (type is BAC_Type19 type19)
                    {
                        var flag = type19.AuraFlags.RemoveFlag(BAC_Type19.AuraFlagsEnum.DisableAura | BAC_Type19.AuraFlagsEnum.Unk2 | BAC_Type19.AuraFlagsEnum.Unk3 | BAC_Type19.AuraFlagsEnum.Unk4);

                        if ((ushort)flag != 0)
                            return $"AuraFlags ({HexConverter.GetHexString((ushort)flag)})";
                    }
                    else if (type is BAC_Type20 type20)
                    {
                        var flag = type20.HomingFlags.RemoveFlag(BAC_Type20.HomingFlagsEnum.KNOWN_MASK);

                        if ((ushort)flag != 0)
                            return $"HomingFlags ({HexConverter.GetHexString((ushort)flag)})";
                    }
                    else if (type is BAC_Type23 type23)
                    {
                        var flag = type23.TransparencyFlags.RemoveFlag(BAC_Type23.TransparencyFlagsEnum.Activate | BAC_Type23.TransparencyFlagsEnum.Unk1 | BAC_Type23.TransparencyFlagsEnum.Unk3 | BAC_Type23.TransparencyFlagsEnum.Unk4);

                        if ((ushort)flag != 0)
                            return $"TransparencyFlags ({HexConverter.GetHexString((ushort)flag)})";
                    }

                }
            }

            return null;
        }

        public void ChangeNeutralSkillId(ushort newId)
        {
            foreach (var entry in BacEntries)
            {
                if (entry.Type8 != null)
                {
                    foreach (var effect in entry.Type8)
                    {
                        if (effect.SkillID == 0xBACA)
                            effect.SkillID = newId;
                    }
                }
                if (entry.Type9 != null)
                {
                    foreach (var projectile in entry.Type9)
                    {
                        if (projectile.SkillID == 0xBACA)
                            projectile.SkillID = newId;
                    }
                }

                if (entry.Type15 != null)
                {
                    foreach (var function in entry.Type15)
                    {
                        if (function.Param1 == 0xBACA || function.Param1 == 0xBACABACA)
                            function.Param1 = newId;

                        if (function.Param2 == 0xBACA || function.Param2 == 0xBACABACA)
                            function.Param2 = newId;

                        if (function.Param3 == 0xBACA || function.Param3 == 0xBACABACA)
                            function.Param3 = newId;

                        if (function.Param4 == 0xBACA || function.Param4 == 0xBACABACA)
                            function.Param4 = newId;

                        if (function.Param5 == 0xBACA || function.Param5 == 0xBACABACA)
                            function.Param5 = newId;
                    }
                }
            }
        }
        
        public static string GetBacTypeNameWithTypeID(int type)
        {
            if(BacTypeNames.TryGetValue(type, out string name))
            {
                return $"[{type}] {name}";
            }

            return null;
        }
        #endregion

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

        public const int MAX_ENTRIES_CHARACTER = 1000;
        public const int MAX_ENTRIES_SKILL = 100;

        [Flags]
        public enum Flags : uint
        {
            unk1 = 0x1,
            unk2 = 0x2,
            unk3 = 0x4,
            unk4 = 0x8,
            unk5 = 0x10,
            unk6 = 0x20,
            unk7 = 0x40,
            unk8 = 0x80,
            unk9 = 0x100,
            unk10 = 0x200,
            unk11 = 0x400,
            unk12 = 0x800,
            unk13 = 0x1000,
            unk14 = 0x2000,
            unk15 = 0x4000,
            unk16 = 0x8000,
            unk17 = 0x10000,
            unk18 = 0x20000,
            unk19 = 0x40000,
            unk20 = 0x80000,
            unk21 = 0x100000,
            unk22 = 0x200000,
            unk23 = 0x400000,
            unk24 = 0x800000,
            unk25 = 0x1000000,
            unk26 = 0x2000000,
            unk27 = 0x4000000,
            unk28 = 0x8000000,
            unk29 = 0x10000000,
            unk30 = 0x20000000,
            unk31 = 0x40000000,
            Empty = 0x80000000
        }

        #region NonSerialized
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } set { Index = value.ToString(); NotifyPropertyChanged(nameof(Index)); } }
        [YAXDontSerialize]
        public string FlagStr => HexConverter.GetHexString((uint)Flag);
        [YAXDontSerialize]
        public string MovesetBacEntryName
        {
            get
            {
                string name;
                if (ValuesDictionary.BAC.MovesetBacEntry.TryGetValue(SortID, out name))
                    return name;

                return null;
            }
        }

        private Flags _flag = 0;

        public bool NewEntry = false;
        #endregion


        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } = "0"; //int32
        [YAXAttributeForClass]
        public Flags Flag
        {
            get => _flag;
            set
            {
                _flag = value;
                NotifyPropertyChanged(nameof(Flag));
                NotifyPropertyChanged(nameof(FlagStr));
            }
        }

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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BoneModification")]
        public List<BAC_Type14> Type14 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Functions")]
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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PhysicsObject")]
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
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DualSkillHandler")]
        public List<BAC_Type24> Type24 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ExtendedChainAttack")]
        public List<BAC_Type25> Type25 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ExtendedCameraControl")]
        public List<BAC_Type26> Type26 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EffectPropertyControl")]
        public List<BAC_Type27> Type27 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacType28")]
        public List<BAC_Type28> Type28 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacType29")]
        public List<BAC_Type29> Type29 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacType30")]
        public List<BAC_Type30> Type30 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacType31")]
        public List<BAC_Type31> Type31 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("Types")]
        [YAXAttributeFor("HasDummy")]
        public List<int> TypeDummy { get; set; }

        [YAXDontSerialize]
        public AsyncObservableCollection<IBacType> IBacTypes { get; set; } = new AsyncObservableCollection<IBacType>();

        #region TimeLine
        public void SortTimeLineLayers()
        {
            for(int i = 0; i < BAC_File.BAC_TYPE_COUNT; i++) 
            {
                SortTimeLineLayers(i);
            }
        }

        public void SortTimeLineLayer(IBacType entry)
        {
            entry.Layer = 0;

            while (IsTimingCollision(entry, IBacTypes))
            {
                entry.Layer++;
            }
        }

        private void SortTimeLineLayers(int type)
        {
            if(type == 0)
            {
                int lastLayer = 0;

                //Full Body Animations
                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1))
                {
                    if(bacType is BAC_Type0 anim)
                    {
                        if (BAC_Type0.IsFullBodyAnimation(anim.EanType))
                        {
                            bacType.Layer = 0;

                            while (IsTimingCollision(bacType, IBacTypes))
                            {
                                bacType.Layer++;

                                if (bacType.Layer > lastLayer)
                                    lastLayer = bacType.Layer;
                            }
                        }
                    }
                }

                //Face Animations
                int faceStart = ++lastLayer;

                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1))
                {
                    if (bacType is BAC_Type0 anim)
                    {
                        if (BAC_Type0.IsFaceAnimation(anim.EanType))
                        {
                            bacType.Layer = faceStart;

                            while (IsTimingCollision(bacType, IBacTypes))
                            {
                                bacType.Layer++;

                                if (bacType.Layer > lastLayer)
                                    lastLayer = bacType.Layer;
                            }
                        }
                    }
                }

                //Everything else
                lastLayer++;
                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1))
                {
                    if (bacType is BAC_TypeBase typeBase)
                    {
                        bacType.Layer = lastLayer;

                        while (IsTimingCollision(bacType, IBacTypes))
                        {
                            bacType.Layer++;
                        }
                    }
                }
            }
            else
            {
                //We will add types to layers in batches based on the flags value. This is so that entries marked as "CAC" or "Roster" will tend to have their own layers, assuming there are a lot of entries.
                //In the event where they can all fit on one layer, they will all be placed there.
                int lastLayer = 0;

                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1 && x.Flags == 0))
                {
                    bacType.Layer = 0;

                    while (IsTimingCollision(bacType, IBacTypes))
                    {
                        bacType.Layer++;

                        if (bacType.Layer > lastLayer)
                            lastLayer = bacType.Layer;
                    }
                }

                //Now we place FLAGS 1 and 2 entries
                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1 && x.Flags == 1))
                {
                    bacType.Layer = lastLayer;

                    while (IsTimingCollision(bacType, IBacTypes))
                    {
                        bacType.Layer++;
                    }
                }

                foreach (IBacType bacType in IBacTypes.Where(x => x.TypeID == type && x.Layer == -1 && x.Flags == 2))
                {
                    bacType.Layer = lastLayer;

                    while (IsTimingCollision(bacType, IBacTypes))
                    {
                        bacType.Layer++;
                    }
                }
            }
        }

        public bool DoesLayerExist(int layer, int layerGroup)
        {
            foreach(var type in IBacTypes.Where(x => x.LayerGroup == layerGroup))
            {
                if (type.Layer == layer) return true;
            }

            return false;
        }

        public int AssignNewLayer(int layerGroup)
        {
            int layer = 0;

            while (DoesLayerExist(layer, layerGroup))
                layer++;

            return layer;
        }

        public static bool IsTimingCollision(IBacType item, IList<IBacType> items)
        {
            foreach (ITimeLineItem current in items.Where(x => x.Layer == item.Layer && x.LayerGroup == item.LayerGroup && x != item))
            {
                int currentEndTime = current.TimeLine_StartTime + current.TimeLine_Duration;
                int endTime = item.TimeLine_StartTime + item.TimeLine_Duration;

                //StartTime is within another entry
                if (item.TimeLine_StartTime >= current.TimeLine_StartTime && item.TimeLine_StartTime < currentEndTime)
                    return true;

                //StartTime is before another entry, but it ends within another
                if (item.TimeLine_StartTime < current.TimeLine_StartTime && endTime >= current.TimeLine_StartTime)
                    return true;
            }

            return false;
        }
        
        public static bool IsTimingCollision(int startTime, int duration, int layer, int layerGroup, IList<IBacType> items, IBacType exclusion = null)
        {
            foreach (ITimeLineItem current in items.Where(x => x.Layer == layer && x.LayerGroup == layerGroup))
            {
                if(current == exclusion) continue;
                int currentEndTime = current.TimeLine_StartTime + current.TimeLine_Duration;
                int endTime = startTime + duration;

                //StartTime is within another entry
                if (startTime >= current.TimeLine_StartTime && startTime < currentEndTime)
                    return true;

                //StartTime is before another entry, but it ends within another
                if (startTime < current.TimeLine_StartTime && endTime > current.TimeLine_StartTime)
                    return true;
            }

            return false;
        }

        public int GetTimeLineLength()
        {
            int endFrame = 0;

            foreach(IBacType type in IBacTypes)
            {
                if(type.StartTime + type.Duration > endFrame)
                {
                    endFrame = type.StartTime + type.Duration;
                }
            }

            return endFrame;
        }
        #endregion

        #region IBacTypeMethods
        public void InitializeIBacTypes()
        {
            InitBacLists();

            IBacTypes = new AsyncObservableCollection<IBacType>();

            foreach (var bacEntry in Type0.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type1.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type2.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type3.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type4.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type5.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type6.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type7.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type8.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type9.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type10.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type11.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type12.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type13.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type14.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type15.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type16.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type17.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type18.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type19.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type20.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type21.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type22.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type23.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type24.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type25.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type26.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type27.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type28.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type29.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type30.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);

            foreach (var bacEntry in Type31.OrderBy(x => x.StartTime))
                IBacTypes.Add(bacEntry);
        }

        public void SaveIBacTypes()
        {
            ClearBacLists();

            foreach (var bacEntry in IBacTypes)
            {
                if (bacEntry is BAC_Type0 type)
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
                else if (bacEntry is BAC_Type28 type28)
                {
                    Type28.Add(type28);
                }
                else if (bacEntry is BAC_Type29 type29)
                {
                    Type29.Add(type29);
                }
                else if (bacEntry is BAC_Type30 type30)
                {
                    Type30.Add(type30);
                }
                else if (bacEntry is BAC_Type31 type31)
                {
                    Type31.Add(type31);
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
            if (Type28 == null)
                Type28 = new List<BAC_Type28>();
            if (Type29 == null)
                Type29 = new List<BAC_Type29>();
            if (Type30 == null)
                Type30 = new List<BAC_Type30>();
            if (Type31 == null)
                Type31 = new List<BAC_Type31>();
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
            Type28.Clear();
            Type29.Clear();
            Type30.Clear();
            Type31.Clear();
        }

        /// <summary>
        /// Add a new instance of the specified IBacType as an undoable operation.
        /// </summary>
        /// <param name="bacType"></param>
        /// <returns></returns>
        public IBacType UndoableAddIBacType(int bacType, int layer = -1, int startTime = 0)
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
                case 28:
                    iBacType = new BAC_Type28();
                    break;
                case 29:
                    iBacType = new BAC_Type29();
                    break;
                case 30:
                    iBacType = new BAC_Type30();
                    break;
                case 31:
                    iBacType = new BAC_Type31();
                    break;
                default:
                    throw new InvalidOperationException($"UndoableAddIBacType: Invalid bacType {bacType}!");
            }

            iBacType.StartTime = (ushort)startTime;
            iBacType.Layer = layer;

            var undo = AddEntry(iBacType);
            UndoManager.Instance.AddUndo(undo);

            return iBacType;
        }

        /// <summary>
        /// Remove the specified iBacType instances as an undoable operation.
        /// </summary>
        /// <param name="iBacType"></param>
        public void UndoableRemoveIBacType(IList<IBacType> iBacTypes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var iBacType in iBacTypes)
            {
                undos.Add(new UndoableListRemove<IBacType>(IBacTypes, iBacType));
                IBacTypes.Remove(iBacType);
            }
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "BacType Delete"));
        }

        public IUndoRedo AddEntry(IBacType bacType)
        {
            int insertIdx = IBacTypes.IndexOf(IBacTypes.FirstOrDefault(x => x.TypeID > bacType.TypeID));

            if (insertIdx == -1)
            {
                //Add
                IBacTypes.Add(bacType);
                return new UndoableListAdd<IBacType>(IBacTypes, bacType, $"New BacType {bacType}");
            }
            else
            {
                //Insert
                IBacTypes.Insert(insertIdx, bacType);
                return new UndoableListInsert<IBacType>(IBacTypes, insertIdx, bacType, $"New BacType {bacType}");
            }
        }

        public void RefreshIBacTypes()
        {
            NotifyPropertyChanged(nameof(IBacTypes));
        }
        
        public void ResetTimesActivated()
        {
            foreach(var type in IBacTypes)
            {
                type.TimesActivated = 0;
            }
        }
        #endregion

        public bool IsBacEntryEmpty()
        {
            if (Type0?.Count != 0 || Type1?.Count != 0 || Type2?.Count != 0 || Type3?.Count != 0 || Type4?.Count != 0 || Type5?.Count != 0 || Type6?.Count != 0 ||
                Type7?.Count != 0 || Type8?.Count != 0 || Type9?.Count != 0 || Type10?.Count != 0 || Type11?.Count != 0 || Type12?.Count != 0 || Type13?.Count != 0 ||
                Type14?.Count != 0 || Type15?.Count != 0 || Type16?.Count != 0 || Type17?.Count != 0 || Type18?.Count != 0 || Type19?.Count != 0 || Type20?.Count != 0 ||
                Type21?.Count != 0 || Type22?.Count != 0 || Type23?.Count != 0 || Type24?.Count != 0 || Type25?.Count != 0 || Type26?.Count != 0 || Type27?.Count != 0 || 
                Type28?.Count != 0 || Type29?.Count != 0 || Type30?.Count != 0 | Type31?.Count != 0)
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
                Type28 = Type28,
                Type29 = Type29,
                Type30 = Type30,
                TypeDummy = TypeDummy
            };
        }

        public static BAC_Entry Empty(int id = 0)
        {
            return new BAC_Entry()
            {
                Flag = Flags.Empty,
                SortID = id
            };
        }

        public void UpdateEntryName()
        {
            NotifyPropertyChanged(nameof(MovesetBacEntryName));
        }
    }

    [Serializable]
    public class BAC_TypeBase : IBacType
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region TimeLineItem
        private bool _isSelected = false;
        [YAXDontSerialize]
        public bool TimeLine_IsSelected
        {
            get => _isSelected;
            set
            {
                if(_isSelected != value)
                {
                    _isSelected = value;
                    NotifyPropertyChanged(nameof(TimeLine_IsSelected));
                    NotifyPropertyChanged(nameof(TimeLineMainBrush));
                    NotifyPropertyChanged(nameof(TimeLineTextBrush));
                }
            }
        }

        [YAXDontSerialize]
        public int TimeLine_StartTime
        {
            get => _startTime;
            set => _startTime = (ushort)value;
        }
        [YAXDontSerialize]
        public int TimeLine_Duration
        {
            get => _duration;
            set => _duration = (ushort)value;
        }
        [YAXDontSerialize]
        public int Layer { get; set; } = -1;
        [YAXDontSerialize]
        public int LayerGroup => TypeID;

        [YAXDontSerialize]
        public string DisplayName => $"{Type}";

        [YAXDontSerialize]
        public Brush TimeLineMainBrush => GetMainBrush();
        [YAXDontSerialize]
        public Brush TimeLineTextBrush => GetTextBrush();
        [YAXDontSerialize]
        public Brush TimeLineBorderBrush => GetBorderBrush();

        private Brush GetMainBrush()
        {
            if (_isSelected && SettingsManager.settings.UseDarkTheme) return Brushes.White;
            if (_isSelected && SettingsManager.settings.UseLightTheme) return Brushes.Black;

            switch (TypeID)
            {
                case 2: //Animation and Movement Group
                case 14:
                case 18:
                case 20:
                case 21:
                    return Brushes.Blue;
                case 0: //Animation. Brush will change slightly based on type of animation.
                    //BAC_Type0.EanTypeEnum ean = (BAC_Type0.EanTypeEnum)GetValue(0);

                    //if (BAC_Type0.IsFullBodyAnimation(ean))
                    //    return Brushes.Aqua;

                    //if (BAC_Type0.IsFaceAnimation(ean))
                    //    return Brushes.Blue;

                    return Brushes.Blue;
                case 8: //Effect Group
                case 16:
                case 19:
                case 23:
                case 27:
                    return Brushes.Green;
                case 1: //Attack Group
                case 9:
                    return Brushes.Red;
                case 6: //Flow Control Group
                case 7:
                case 17:
                case 24:
                case 25:
                    return Brushes.Yellow;
                case 10:
                case 26:
                    return Brushes.MediumPurple;
                case 11: //Sound
                    return Brushes.HotPink;
                case 3:
                case 4:
                case 5:
                case 12:
                case 13:
                    return Brushes.DarkGray;
                case 15: //Functions. This is dependent on the actual selected function.
                    int function = GetValue(0);

                    //Flow Control functions
                    if (function == 0 || function == 0x22 || function == 0x10 || function == 0x1d || function == 0x25 || function == 0x26 || function == 0x3d)
                        goto case 7;

                    //Attack functions
                    if (function == 0x2)
                        goto case 1;

                    goto case 3;
                default:
                    return Brushes.LightGray;
            }
        }
        
        private Brush GetTextBrush()
        {
            if (_isSelected && SettingsManager.settings.UseDarkTheme) return Brushes.Black;
            if (_isSelected && SettingsManager.settings.UseLightTheme) return Brushes.White;

            switch (TypeID)
            {
                case 6: //Flow Control Group
                case 7:
                case 17:
                case 24:
                case 25:
                    return Brushes.Black;
                case 15:
                    int function = GetValue(0);

                    //Flow Control functions
                    if (function == 0 || function == 0x22 || function == 0x10 || function == 0x1d || function == 0x25 || function == 0x26 || function == 0x3d)
                        goto case 7;

                    goto default;
                default:
                    return Brushes.White;
            }
        }
        
        private Brush GetBorderBrush()
        {
            switch (Flags)
            {
                case 1: //CAC
                    return Brushes.Purple;
                case 2: //Roster
                    return Brushes.White;
                default: //Both CAC and Roster, and whatever 5 is
                    return Brushes.DarkGray;
            }
        }

        public void UpdateSourceValues(ushort newStartTime, ushort newDuration, List<IUndoRedo> undos = null)
        {
            if (undos != null)
            {
                undos.Add(new UndoablePropertyGeneric(nameof(StartTime), this, StartTime, newStartTime));
                undos.Add(new UndoablePropertyGeneric(nameof(Duration), this, Duration, newDuration));
            }

            StartTime = newStartTime;
            Duration = newDuration;
        }
        #endregion

        [YAXDontSerialize]
        public virtual string Type => "";
        [YAXDontSerialize]
        public virtual int TypeID => -1;
        [YAXDontSerialize]
        public int TimesActivated { get; set; }

        //Values:
        private ushort _startTime = 0;
        private ushort _duration = 1;
        private ushort _flags = 0;

        //Props:
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    NotifyPropertyChanged(nameof(StartTime));
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort Duration
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
                    NotifyPropertyChanged(nameof(Duration));
                }
            }
        }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Flags")]
        public ushort Flags
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
                    NotifyPropertyChanged(nameof(Flags));
                    NotifyPropertyChanged(nameof(TimeLineBorderBrush));
                }
            }
        }

        public void RefreshType()
        {
            NotifyPropertyChanged(nameof(Type));
            NotifyPropertyChanged(nameof(DisplayName));
        }
        
        public virtual int GetValue(int i)
        {
            return 0;
        }
    }

    [YAXSerializeAs("Animation")]
    [Serializable]
    public class BAC_Type0 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"Animation ({EanType}, {EanIndex})";
        [YAXDontSerialize]
        public override int TypeID => 0;

        public enum EanTypeEnum : ushort
        {
            Common = 0,
            MCM_DBA = 2,
            Character = 5,
            CommonTail = 9,
            FaceBase = 10,
            FaceForehead = 11,
            Skill = 65534,

            //Legacy options (installer compat):
            FaceA = 10,
            FaceB = 11,
        }

        [Flags]
        public enum AnimationFlags : ushort
        {
            MoveWithAxis_X = 0x1,
            MoveWithAxis_Y = 0x2,
            MoveWithAxis_Z = 0x4,
            Unk4 = 0x8,
            UseRootMotion = 0x10,
            Unk6 = 0x20,
            ForceYRootMotion = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            ContinueFromLast = 0x200,
            IgnoreRootMotionX = 0x400,
            IgnoreRootMotionY = 0x800,
            IgnoreRootMotionZ = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Rotate180Degrees = 0x8000,

            //Old options kept for installer compatibility
            FullRootMotion = 0x40, //"Force Y Motion"
            Unk10 = 0x200, //"Continue From Last"
            Unk11 = 0x400, //"Ignore Root Motion X"
            Unk12 = 0x800, //"Ignore Root Motion Y"
            Unk13 = 0x1000, //"Ignore Root Motion Z"
            Unk16 = 0x8000, //"Rotate 180 degrees"

        }

        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("File")]
        public EanTypeEnum EanType { get; set; }
        [YAXAttributeFor("EanIndex")]
        [YAXSerializeAs("value")]
        public ushort EanIndex { get; set; }  //ushort
        [YAXAttributeFor("AnimationFlags")]
        [YAXSerializeAs("values")]
        public AnimationFlags AnimFlags { get; set; } //ushort
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("StartFrame")]
        [YAXSerializeAs("value")]
        public ushort StartFrame { get; set; }
        [YAXAttributeFor("EndFrame")]
        [YAXSerializeAs("value")]
        public ushort EndFrame { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("LoopStartFrame")]
        [YAXSerializeAs("value")]
        public ushort LoopStartFrame { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("TimeScale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Amount")]
        public float TimeScale { get; set; } = 1f;
        [YAXAttributeFor("StartBlendWeight")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float BlendWeight { get; set; } = 1f;
        [YAXAttributeFor("BlendWeightFrameStep")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float BlendWeightFrameStep { get; set; } = 0f;

        //Cached values
        public int CachedActualDuration = 0;

        public static List<BAC_Type0> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type0> Type0 = new List<BAC_Type0>();

            for (int i = 0; i < count; i++)
            {
                Type0.Add(new BAC_Type0()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    EanType = (EanTypeEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                    EanIndex = BitConverter.ToUInt16(rawBytes, offset + 10),
                    AnimFlags = (AnimationFlags)BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    StartFrame = BitConverter.ToUInt16(rawBytes, offset + 16),
                    EndFrame = BitConverter.ToUInt16(rawBytes, offset + 18),
                    LoopStartFrame = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    TimeScale = BitConverter.ToSingle(rawBytes, offset + 24),
                    BlendWeight = BitConverter.ToSingle(rawBytes, offset + 28),
                    BlendWeightFrameStep = BitConverter.ToSingle(rawBytes, offset + 32)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.EanType));
                bytes.AddRange(BitConverter.GetBytes(type.EanIndex));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.AnimFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.StartFrame));
                bytes.AddRange(BitConverter.GetBytes(type.EndFrame));
                bytes.AddRange(BitConverter.GetBytes(type.LoopStartFrame));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
                bytes.AddRange(BitConverter.GetBytes(type.TimeScale));
                bytes.AddRange(BitConverter.GetBytes(type.BlendWeight));
                bytes.AddRange(BitConverter.GetBytes(type.BlendWeightFrameStep));
            }

            return bytes;
        }

        public static List<BAC_Type0> Clone(List<BAC_Type0> types)
        {
            if (types == null) return null;
            List<BAC_Type0> newTypes = new List<BAC_Type0>();

            foreach (var entry in types)
            {
                newTypes.Add(entry.Clone());
            }

            return newTypes;
        }

        public BAC_Type0 Clone()
        {
            return new BAC_Type0()
            {
                TimeScale = TimeScale,
                BlendWeight = BlendWeight,
                BlendWeightFrameStep = BlendWeightFrameStep,
                StartTime = StartTime,
                Duration = Duration,
                I_04 = I_04,
                Flags = Flags,
                EanType = EanType,
                EanIndex = EanIndex,
                AnimFlags = AnimFlags,
                I_14 = I_14,
                StartFrame = StartFrame,
                EndFrame = EndFrame,
                LoopStartFrame = LoopStartFrame,
                I_22 = I_22
            };

        }

        public override int GetValue(int i)
        {
            return (int)EanType;
        }

        public static bool IsFullBodyAnimation(EanTypeEnum ean)
        {
            return ean == EanTypeEnum.Common || ean == EanTypeEnum.Character || ean == EanTypeEnum.Skill || ean == EanTypeEnum.MCM_DBA;
        }

        public static bool IsFaceAnimation(EanTypeEnum ean)
        {
            return ean == EanTypeEnum.FaceForehead || ean == EanTypeEnum.FaceBase;
        }

        //XenoKit
        public static int CalculateNumOfBlendingFrames(IList<IBacType> types, int frame)
        {
            int blendingFrames = 0;

            for (int currentFrame = 0; currentFrame <= frame; currentFrame++)
            {
                if (blendingFrames > 0)
                    blendingFrames--;

                foreach (var type in types.Where(x => x.GetType() == typeof(BAC_Type0) && x.StartTime == currentFrame))
                {
                    if (type is BAC_Type0 anim)
                    {
                        if (anim.EanIndex != ushort.MaxValue && anim.BlendWeight < 1f && anim.BlendWeightFrameStep > 0f)
                        {
                            float blendFactor = anim.BlendWeight * 100f;
                            float blendFactorStep = anim.BlendWeightFrameStep * 100f;
                            float maxBlendWeight = 1f * 100f;

                            int blendAmount = (int)((maxBlendWeight - blendFactor) / blendFactorStep);

                            if (blendAmount > blendingFrames)
                                blendingFrames = blendAmount;
                        }
                    }
                }
            }

            //Always return atleast 1
            return (blendingFrames > 0) ? blendingFrames : 1;
        }
    }

    [YAXSerializeAs("Hitbox")]
    [Serializable]
    public class BAC_Type1 : BAC_TypeBase, IBacBone, IBacTypeMatrix
    {
        [YAXDontSerialize]
        public override string Type => $"Hitbox ({bdmFile})";
        [YAXDontSerialize]
        public override int TypeID => 1;

        public enum BdmType : byte
        {
            Common = 0,
            Character = 1,
            Skill = 2
        }

        public enum BoundingBoxTypeEnum : byte
        {
            Uniform = 0,
            MinMax = 1,
            Unk2 = 2,
            Unk3 = 3,
            Unk4 = 4
        }

        [Flags]
        public enum HitboxFlagsEnum : ushort
        {
            Unk1 = 0x1,
            Unk2 = 0x2,
            Unk3 = 0x4,
            Unk4 = 0x8,

            //If no spawn source flag: use User
            SpawnSource_Unk1 = 0x10,
            SpawnSource_Unk2 = 0x20,
            SpawnSource_User = 0x40,
            SpawnSource_Target = 0x80,

            //If no damage flag: use Health
            DamageType_Unk1 = 0x100, //No Impact
            DamageType_None = 0x200,
            DamageType_Ki = 0x400,
            DamageType_Unk4 = 0x800, //No Impact

            //If no impact flag: use Continuous
            ImpactType_Unk1 = 0x1000, //No Impact
            ImpactType_Single = 0x2000,
            ImpactType_Continuous = 0x4000,
            ImpactType_Unk4 = 0x8000 //No Impact
        }


        [YAXAttributeFor("BDM")]
        [YAXSerializeAs("File")]
        public BdmType bdmFile { get; set; }
        [YAXAttributeFor("BDM_Entry")]
        [YAXSerializeAs("ID")]
        public ushort BdmEntryID { get; set; } //ushort
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("A")]
        public BoundingBoxTypeEnum BoundingBoxType
        {
            get => _boundingBoxType;
            set
            {
                if (value != _boundingBoxType)
                {
                    _boundingBoxType = value;
                    NotifyPropertyChanged(nameof(BoundingBoxType));
                }
            }
        }
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("B")]
        public byte I_18_b { get; set; } //always 0
        [YAXAttributeFor("Flag_18")]
        [YAXSerializeAs("D")]
        public byte I_18_d { get; set; } //always 0
        [YAXAttributeFor("HitboxFlags")]
        [YAXSerializeAs("value")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public HitboxFlagsEnum HitboxFlags { get; set; }

        //HitboxFlags as a hex value. Required for reading older XML files where the value was stored this way.
        [YAXAttributeFor("Hitbox_Flags")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string XML_HitboxFlags
        {
            set
            {
                if(value != null)
                {
                    ushort val = (ushort)HexConverter.ToInt16(value);
                    HitboxFlags = (HitboxFlagsEnum)val;
                }
            }
        }

        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("value")]
        public ushort Damage { get; set; }
        [YAXAttributeFor("Damage_When_Blocked")]
        [YAXSerializeAs("value")]
        public ushort DamageWhenBlocked { get; set; }
        [YAXAttributeFor("Stamina_Taken_When_Blocked")]
        [YAXSerializeAs("value")]
        public ushort StaminaTakenWhenBlocked { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink
        {
            get => _boneLinks;
            set
            {
                if (value != _boneLinks)
                {
                    _boneLinks = value;
                    NotifyPropertyChanged(nameof(BoneLink));
                }
            }
        }
        [YAXAttributeFor("F_24")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float PositionX
        {
            get => _posX;
            set
            {
                if (value != _posX)
                {
                    _posX = value;
                    NotifyPropertyChanged(nameof(PositionX));
                }
            }
        }

        //The XML names are wrong, but updating them would break old XMLs and cause installer trouble. Best to leave them as is.
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float PositionY
        {
            get => _posY;
            set
            {
                if (value != _posY)
                {
                    _posY = value;
                    NotifyPropertyChanged(nameof(PositionY));
                }
            }
        }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float PositionZ
        {
            get => _posZ;
            set
            {
                if (value != _posZ)
                {
                    _posZ = value;
                    NotifyPropertyChanged(nameof(PositionZ));
                }
            }
        }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float Size
        {
            get => _size;
            set
            {
                if (value != _size)
                {
                    _size = value;
                    NotifyPropertyChanged(nameof(Size));
                }
            }
        }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float MinX
        {
            get => _minX;
            set
            {
                if (value != _minX)
                {
                    _minX = value;
                    NotifyPropertyChanged(nameof(MinX));
                }
            }
        }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float MinY
        {
            get => _minY;
            set
            {
                if (value != _minY)
                {
                    _minY = value;
                    NotifyPropertyChanged(nameof(MinY));
                }
            }
        }
        [YAXAttributeFor("Scale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float MinZ
        {
            get => _minZ;
            set
            {
                if (value != _minZ)
                {
                    _minZ = value;
                    NotifyPropertyChanged(nameof(MinZ));
                }
            }
        }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float MaxX
        {
            get => _maxX;
            set
            {
                if (value != _maxX)
                {
                    _maxX = value;
                    NotifyPropertyChanged(nameof(MaxX));
                }
            }
        }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float MaxY
        {
            get => _maxY;
            set
            {
                if (value != _maxY)
                {
                    _maxY = value;
                    NotifyPropertyChanged(nameof(MaxY));
                }
            }
        }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float MaxZ
        {
            get => _maxZ;
            set
            {
                if (value != _maxZ)
                {
                    _maxZ = value;
                    NotifyPropertyChanged(nameof(_maxZ));
                }
            }
        }

        private BoneLinks _boneLinks = 0;
        private BoundingBoxTypeEnum _boundingBoxType = 0;
        private float _posX = 0f;
        private float _posY = 0f;
        private float _posZ = 0f;
        private float _size = 0f;
        private float _minX = 0f;
        private float _minY = 0f;
        private float _minZ = 0f;
        private float _maxX = 0f;
        private float _maxY = 0f;
        private float _maxZ = 0f;

        #region IBacMatrix
        //Placeholders

        [YAXDontSerialize]
        public float RotationX { get; set; }
        [YAXDontSerialize]
        public float RotationY { get; set; }
        [YAXDontSerialize]
        public float RotationZ { get; set; }
        #endregion

        public static List<BAC_Type1> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type1> Type1 = new List<BAC_Type1>();

            for (int i = 0; i < count; i++)
            {
                Type1.Add(new BAC_Type1()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    BdmEntryID = BitConverter.ToUInt16(rawBytes, offset + 8),
                    HitboxFlags = (HitboxFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 10),
                    Damage = BitConverter.ToUInt16(rawBytes, offset + 12),
                    DamageWhenBlocked = BitConverter.ToUInt16(rawBytes, offset + 14),
                    StaminaTakenWhenBlocked = BitConverter.ToUInt16(rawBytes, offset + 16),
                    BoundingBoxType = (BoundingBoxTypeEnum)Int4Converter.ToInt4(rawBytes[offset + 18])[0],
                    I_18_b = Int4Converter.ToInt4(rawBytes[offset + 18])[1],
                    bdmFile = (BdmType)Int4Converter.ToInt4(rawBytes[offset + 19])[0],
                    I_18_d = Int4Converter.ToInt4(rawBytes[offset + 19])[1],
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 22),
                    PositionX = BitConverter.ToSingle(rawBytes, offset + 24),
                    PositionY = BitConverter.ToSingle(rawBytes, offset + 28),
                    PositionZ = BitConverter.ToSingle(rawBytes, offset + 32),
                    Size = BitConverter.ToSingle(rawBytes, offset + 36),
                    MinX = BitConverter.ToSingle(rawBytes, offset + 40),
                    MinY = BitConverter.ToSingle(rawBytes, offset + 44),
                    MinZ = BitConverter.ToSingle(rawBytes, offset + 48),
                    MaxX = BitConverter.ToSingle(rawBytes, offset + 52),
                    MaxY = BitConverter.ToSingle(rawBytes, offset + 56),
                    MaxZ = BitConverter.ToSingle(rawBytes, offset + 60),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.HitboxFlags));
                bytes.AddRange(BitConverter.GetBytes(type.Damage));
                bytes.AddRange(BitConverter.GetBytes(type.DamageWhenBlocked));
                bytes.AddRange(BitConverter.GetBytes(type.StaminaTakenWhenBlocked));
                bytes.Add(Int4Converter.GetByte((byte)type.BoundingBoxType, type.I_18_b, "Hitbox > Flag_18 > A", "Hitbox > Flag_18 > B"));
                bytes.Add(Int4Converter.GetByte((byte)type.bdmFile, type.I_18_d, "Hitbox > BDM File", "Hitbox > Flag_18 > D"));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes(type.PositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ));
                bytes.AddRange(BitConverter.GetBytes(type.Size));
                bytes.AddRange(BitConverter.GetBytes(type.MinX));
                bytes.AddRange(BitConverter.GetBytes(type.MinY));
                bytes.AddRange(BitConverter.GetBytes(type.MinZ));
                bytes.AddRange(BitConverter.GetBytes(type.MaxX));
                bytes.AddRange(BitConverter.GetBytes(type.MaxY));
                bytes.AddRange(BitConverter.GetBytes(type.MaxZ));
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
                PositionY = PositionY,
                PositionZ = PositionZ,
                StartTime = StartTime,
                Duration = Duration,
                I_04 = I_04,
                Flags = Flags,
                BdmEntryID = BdmEntryID,
                HitboxFlags = HitboxFlags,
                Damage = Damage,
                DamageWhenBlocked = DamageWhenBlocked,
                StaminaTakenWhenBlocked = StaminaTakenWhenBlocked,
                I_20 = I_20,
                BoundingBoxType = BoundingBoxType,
                I_18_b = I_18_b,
                bdmFile = bdmFile,
                I_18_d = I_18_d,
                Size = Size,
                MinX = MinX,
                MinY = MinY,
                MinZ = MinZ,
                MaxX = MaxX,
                MaxY = MaxY,
                MaxZ = MaxZ
            };

        }

        public HitboxFlagsEnum GetImpactType()
        {
            int value = ((ushort)HitboxFlags & 0xF000);
            return value != 0 ? (HitboxFlagsEnum)value : HitboxFlagsEnum.ImpactType_Continuous;
        }

        public HitboxFlagsEnum GetSpawnSource()
        {
            int value = ((ushort)HitboxFlags & 0x00F0);
            return value != 0 ? (HitboxFlagsEnum)value : HitboxFlagsEnum.SpawnSource_User;
        }

        public HitboxFlagsEnum GetDamageType()
        {
            int value = ((ushort)HitboxFlags & 0x0F00);
            return (HitboxFlagsEnum)value;
            //return value != 0 ? (HitboxFlagsEnum)value : 0;
        }
    }

    [YAXSerializeAs("Movement")]
    [Serializable]
    public class BAC_Type2 : BAC_TypeBase, IBacTypeMatrix
    {
        [YAXDontSerialize]
        public override string Type => $"Movement";
        [YAXDontSerialize]
        public override int TypeID => 2;


        [YAXAttributeFor("Movement_Type")]
        [YAXSerializeAs("Flags")]
        [YAXHexValue]
        public ushort MovementFlags { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //always 0
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float DirectionX { get; set; }
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float DirectionY { get; set; }
        [YAXAttributeFor("Direction")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float DirectionZ { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float DragX { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float DragY { get; set; }
        [YAXAttributeFor("Drag")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float DragZ { get; set; }

        #region IBacMatrix
        [YAXDontSerialize]
        public float PositionX
        {
            get => DirectionX;
            set => DirectionX = value;
        }
        [YAXDontSerialize]
        public float PositionY
        {
            get => DirectionY;
            set => DirectionY = value;
        }
        [YAXDontSerialize]
        public float PositionZ
        {
            get => DirectionZ;
            set => DirectionZ = value;
        }

        [YAXDontSerialize]
        public float RotationX { get; set; }
        [YAXDontSerialize]
        public float RotationY { get; set; }
        [YAXDontSerialize]
        public float RotationZ { get; set; }
        #endregion

        public static List<BAC_Type2> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type2> Type2 = new List<BAC_Type2>();

            for (int i = 0; i < count; i++)
            {
                Type2.Add(new BAC_Type2()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    MovementFlags = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    DirectionX = BitConverter.ToSingle(rawBytes, offset + 12),
                    DirectionY = BitConverter.ToSingle(rawBytes, offset + 16),
                    DirectionZ = BitConverter.ToSingle(rawBytes, offset + 20),
                    DragX = BitConverter.ToSingle(rawBytes, offset + 24),
                    DragY = BitConverter.ToSingle(rawBytes, offset + 28),
                    DragZ = BitConverter.ToSingle(rawBytes, offset + 32)
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
                bytes.AddRange(BitConverter.GetBytes(type.MovementFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.DirectionX));
                bytes.AddRange(BitConverter.GetBytes(type.DirectionY));
                bytes.AddRange(BitConverter.GetBytes(type.DirectionZ));
                bytes.AddRange(BitConverter.GetBytes(type.DragX));
                bytes.AddRange(BitConverter.GetBytes(type.DragY));
                bytes.AddRange(BitConverter.GetBytes(type.DragZ));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("Invulnerability")]
    [Serializable]
    public class BAC_Type3 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"Invulnerability";
        [YAXDontSerialize]
        public override int TypeID => 3;


        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public ushort InvulnerabilityType { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }

        public static List<BAC_Type3> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type3> Type3 = new List<BAC_Type3>();

            for (int i = 0; i < count; i++)
            {
                Type3.Add(new BAC_Type3()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    InvulnerabilityType = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10)
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
                bytes.AddRange(BitConverter.GetBytes(type.InvulnerabilityType));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("TimeScale")]
    [Serializable]
    public class BAC_Type4 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"TimeScale ({TimeScale})";
        [YAXDontSerialize]
        public override int TypeID => 4;


        [YAXAttributeFor("TimeScale")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Amount")]
        public float TimeScale { get; set; } = 1f;

        public static List<BAC_Type4> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type4> Type4 = new List<BAC_Type4>();

            for (int i = 0; i < count; i++)
            {
                Type4.Add(new BAC_Type4()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    TimeScale = BitConverter.ToSingle(rawBytes, offset + 8)
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
                bytes.AddRange(BitConverter.GetBytes(type.TimeScale));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Tracking")]
    [Serializable]
    public class BAC_Type5 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "Tracking";
        [YAXDontSerialize]
        public override int TypeID => 5;

        [Flags]
        public enum TrackingFlagsEnum : ushort
        {
            Unk1 = 0x1,
            Unk2 = 0x2,//unused
            Unk3 = 0x4,//unused
            Unk4 = 0x8,//unused
            Unk5 = 0x10,//unused
            Unk6 = 0x20,//unused
            Unk7 = 0x40,//unused
            Unk8 = 0x80,//unused
            Unk9 = 0x100,
            Unk10 = 0x200,
            TrackForwardAndBackwards = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000, //unused
            Unk16 = 0x8000 //unused
        }

        [YAXAttributeFor("Tracking")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float Tracking { get; set; }
        [YAXAttributeFor("TrackingFlags")]
        [YAXSerializeAs("value")]
        public TrackingFlagsEnum TrackingFlags { get; set; } //looks like bit flags
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; } //always 0

        public static List<BAC_Type5> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type5> Type5 = new List<BAC_Type5>();

            for (int i = 0; i < count; i++)
            {
                Type5.Add(new BAC_Type5()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    Tracking = BitConverter.ToSingle(rawBytes, offset + 8),
                    TrackingFlags = (TrackingFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14)
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
                bytes.AddRange(BitConverter.GetBytes(type.Tracking));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.TrackingFlags));
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
        public override string Type => "ChargeControl";
        [YAXDontSerialize]
        public override int TypeID => 6;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Charge_Time")]
        [YAXSerializeAs("value")]
        public ushort ChargeTime { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; } //always 0

        public static List<BAC_Type6> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type6> Type6 = new List<BAC_Type6>();

            for (int i = 0; i < count; i++)
            {
                Type6.Add(new BAC_Type6()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    ChargeTime = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14)
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
                bytes.AddRange(BitConverter.GetBytes(type.ChargeTime));
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
        public override string Type => "BcmCallback";
        [YAXDontSerialize]
        public override int TypeID => 7;

        [Flags]
        public enum BcmCallbackFlagsEnum : uint
        {
            Attacks = 0x1,
            Movement = 0x2,
            DisableKiBlastLink = 0x4,
            Unk4 = 0x8,
            Counters = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            BackHits = 0x80,
            Combos = 0x100,
            Supers = 0x200,
            UltimatesAndEvasives = 0x400,
            ZVanish = 0x800,
            KiBlasts = 0x1000,
            Jump = 0x2000,
            Guard = 0x4000,
            FlyingAndStepDash = 0x8000,
            Unk17 = 0x10000,
            Unk18 = 0x20000,
            Unk19 = 0x40000,
            Unk20 = 0x80000,
            KNOWN_MASK = 0xfffff
            //0x80000 and onwards are never used (included 0x80000 in the enum just to even it out)
        }

        [YAXAttributeFor("LinkFlags")]
        [YAXSerializeAs("value")]
        public BcmCallbackFlagsEnum LinkFlags { get; set; }

        public static List<BAC_Type7> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type7> Type7 = new List<BAC_Type7>();

            for (int i = 0; i < count; i++)
            {
                Type7.Add(new BAC_Type7()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    LinkFlags = (BcmCallbackFlagsEnum)BitConverter.ToUInt32(rawBytes, offset + 8)
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
                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((uint)type.LinkFlags));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Effect")]
    [Serializable]
    public class BAC_Type8 : BAC_TypeBase, IBacTypeMatrix, IBacBone
    {
        [YAXDontSerialize]
        public override string Type => CalculateTypeString();
        [YAXDontSerialize]
        public override int TypeID => 8;


        public enum EepkTypeEnum : ushort
        {
            Common = 0,
            StageBG = 1,
            Character = 2,
            AwokenSkill = 3, //AnySkill
            SuperSkill = 5,
            UltimateSkill = 6,
            EvasiveSkill = 7,
            KiBlastSkill = 9,
            Stage = 11,
            NEW_AwokenSkill = 12
        }

        [Flags]
        public enum EffectFlagsEnum : uint
        {
            //These are the only values used by the game
            Default = 0,
            Off = 0x1,
            Unk2 = 0x2,
            SpawnOnTarget = 0x4,
            Loop = 0x8,
            UserOnly = 0x10,
            Unk6 = 0x20,
            Unk8 = 0x80
        }

        public enum UseSkillIdEnum : ushort
        {
            True = 0,
            False = 65535
        }


        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkTypeEnum EepkType { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public ushort SkillID { get; set; } //ushort
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("ID")]
        public int EffectID { get; set; }
        [YAXAttributeFor("EffectFlags")]
        [YAXSerializeAs("flags")]
        public EffectFlagsEnum EffectFlags { get; set; } //uint32
        [YAXAttributeFor("Bone_Link")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; }
        [YAXAttributeFor("Use_Skill_ID")]
        [YAXSerializeAs("value")]
        public UseSkillIdEnum UseSkillId { get; set; } //Only possible values are 65535 (False) and 0 (True). When false, SkillID is always 65535, and when true it is never 65535
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#########")]
        public float PositionX { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#########")]
        public float PositionY { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#########")]
        public float PositionZ { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#########")]
        public float RotationX { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#########")]
        public float RotationY { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#########")]
        public float RotationZ { get; set; }

        public static List<BAC_Type8> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type8> Type8 = new List<BAC_Type8>();

            for (int i = 0; i < count; i++)
            {
                Type8.Add(new BAC_Type8()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    EepkType = (EepkTypeEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                    BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 10),
                    SkillID = BitConverter.ToUInt16(rawBytes, offset + 12),
                    UseSkillId = (UseSkillIdEnum)BitConverter.ToUInt16(rawBytes, offset + 14),
                    EffectID = BitConverter.ToInt32(rawBytes, offset + 16),
                    PositionX = BitConverter.ToSingle(rawBytes, offset + 20),
                    PositionY = BitConverter.ToSingle(rawBytes, offset + 24),
                    PositionZ = BitConverter.ToSingle(rawBytes, offset + 28),
                    RotationX = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 32)),
                    RotationY = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 36)),
                    RotationZ = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 40)),
                    EffectFlags = (EffectFlagsEnum)BitConverter.ToUInt32(rawBytes, offset + 44)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.EepkType));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes(type.SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.UseSkillId));
                bytes.AddRange(BitConverter.GetBytes(type.EffectID));
                bytes.AddRange(BitConverter.GetBytes(type.PositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationX)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationY)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationZ)));
                bytes.AddRange(BitConverter.GetBytes((uint)type.EffectFlags));
            }

            return bytes;
        }

        public static List<BAC_Type8> ChangeSkillId(List<BAC_Type8> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].EepkType)
                {
                    case EepkTypeEnum.AwokenSkill:
                    case EepkTypeEnum.SuperSkill:
                    case EepkTypeEnum.UltimateSkill:
                    case EepkTypeEnum.EvasiveSkill:
                    case EepkTypeEnum.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }

        public bool IsSkillEepk()
        {
            if (EepkType == EepkTypeEnum.SuperSkill || EepkType == EepkTypeEnum.UltimateSkill || EepkType == EepkTypeEnum.EvasiveSkill || EepkType == EepkTypeEnum.AwokenSkill || EepkType == EepkTypeEnum.KiBlastSkill)
            {
                return true;
            }
            return false;
        }

        public string CalculateTypeString()
        {
            string enable = EffectFlags.HasFlag(EffectFlagsEnum.Off) ? "Disable" : "Enable";
            return $"Effect ({EepkType}, {EffectID}, {enable})";
        }
    }

    [YAXSerializeAs("Projectile")]
    [Serializable]
    public class BAC_Type9 : BAC_TypeBase, IBacTypeMatrix, IBacBone
    {
        [YAXDontSerialize]
        public override string Type => $"Projectile ({BsaType})";
        [YAXDontSerialize]
        public override int TypeID => 9;

        public enum BsaTypeEnum : byte
        {
            Common = 0,
            AwokenSkill = 3, //AnySkill
            SuperSkill = 5,
            UltimateSkill = 6,
            EvasiveSkill = 7,
            KiBlastSkill = 9,
            NEW_AwokenSkill = 12
        }

        [Flags]
        public enum BsaFlagsEnum : ushort
        {
            TerminatePreviousProjectile = 0x1,
            Unk2 = 0x2,
            Unk3 = 0x4,
            AllowLoop = 0x8,
            Unk5 = 0x10, //unused
            Unk6 = 0x20, //unused
            Unk7 = 0x40, //unused
            Unk8 = 0x80,  //unused
            BcmCondition = 0x100,
            Unk10 = 0x200,
            Unk11 = 0x400,
            Loop = 0x800,
            DuplicateForAllOpponents = 0x1000,
            Unk14 = 0x2000,
            MarkRandomID = 0x4000,
            MarkUniqueID = 0x8000,

            Unk4 = 0x8,
        }


        public enum CanUseCmnBsaEnum : ushort
        {
            True = 65535,
            False = 0
        }

        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Type")]
        public BsaTypeEnum BsaType { get; set; }
        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Skill_ID")]
        public ushort SkillID { get; set; }
        [YAXAttributeFor("Can_Use_Cmn_Bsa")]
        [YAXSerializeAs("value")]
        public CanUseCmnBsaEnum CanUseCmnBsa { get; set; }
        [YAXAttributeFor("BSA")]
        [YAXSerializeAs("Entry ID")]
        public int EntryID { get; set; }
        [YAXAttributeFor("Bone")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; }
        [YAXAttributeFor("SpawnSource")]
        [YAXSerializeAs("value")]
        public byte SpawnSource { get; set; }
        [YAXAttributeFor("SpawnOrientation")]
        [YAXSerializeAs("value")]
        public byte SpawnOrientation { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float PositionX { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float PositionY { get; set; }
        [YAXAttributeFor("Position")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float PositionZ { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("X")]
        public float RotationX { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Y")]
        public float RotationY { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("Z")]
        public float RotationZ { get; set; }
        [YAXAttributeFor("BsaFlags")]
        [YAXSerializeAs("value")]
        public BsaFlagsEnum BsaFlags { get; set; } //uint16
        [YAXAttributeFor("I_47")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_47 { get; set; } //Always 0 (probably part of the previous flag values, but its not used by the game)
        [YAXAttributeFor("Projectile_Health")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float ProjectileHealth { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int UniqueID { get; set; } //0,1,2,3,4
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; } //always 0
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; } //always 0


        public bool IsSkillBsa()
        {
            if (BsaType == BsaTypeEnum.SuperSkill || BsaType == BsaTypeEnum.UltimateSkill || BsaType == BsaTypeEnum.EvasiveSkill || BsaType == BsaTypeEnum.AwokenSkill || BsaType == BsaTypeEnum.KiBlastSkill)
            {
                return true;
            }

            return false;
        }

        public static List<BAC_Type9> Read(byte[] rawBytes, int offset, int count)
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
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    SkillID = BitConverter.ToUInt16(rawBytes, offset + 8),
                    CanUseCmnBsa = (CanUseCmnBsaEnum)BitConverter.ToUInt16(rawBytes, offset + 10),
                    EntryID = BitConverter.ToInt32(rawBytes, offset + 12),
                    BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 16),
                    SpawnSource = rawBytes[offset + 18],
                    SpawnOrientation = rawBytes[offset + 19],
                    PositionX = BitConverter.ToSingle(rawBytes, offset + 20),
                    PositionY = BitConverter.ToSingle(rawBytes, offset + 24),
                    PositionZ = BitConverter.ToSingle(rawBytes, offset + 28),
                    RotationX = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 32)),
                    RotationY = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 36)),
                    RotationZ = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, offset + 40)),
                    BsaType = (BsaTypeEnum)rawBytes[offset + 44],
                    BsaFlags = (BsaFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 45),
                    I_47 = rawBytes[offset + 47],
                    ProjectileHealth = _F_48,
                    UniqueID = _I_52,
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
                bytes.AddRange(BitConverter.GetBytes(type.SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.CanUseCmnBsa));
                bytes.AddRange(BitConverter.GetBytes(type.EntryID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.Add(type.SpawnSource);
                bytes.Add(type.SpawnOrientation);
                bytes.AddRange(BitConverter.GetBytes(type.PositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationX)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationY)));
                bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(type.RotationZ)));
                bytes.Add((byte)type.BsaType);
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BsaFlags));
                bytes.Add(type.I_47);
                bytes.AddRange(BitConverter.GetBytes(type.ProjectileHealth));
                bytes.AddRange(BitConverter.GetBytes(type.UniqueID));
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
                switch (types[i].BsaType)
                {
                    case BsaTypeEnum.AwokenSkill:
                    case BsaTypeEnum.SuperSkill:
                    case BsaTypeEnum.UltimateSkill:
                    case BsaTypeEnum.EvasiveSkill:
                    case BsaTypeEnum.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }

    }

    [YAXSerializeAs("Camera")]
    [Serializable]
    public class BAC_Type10 : BAC_TypeBase, IBacBone
    {
        [YAXDontSerialize]
        public override string Type => $"Camera ({EanType}, {EanIndex})";
        [YAXDontSerialize]
        public override int TypeID => 10;


        public enum EanTypeEnum : ushort
        {
            Target = 0,
            Common = 3,
            Character = 4,
            Skill = 5,
            MCM = 9
        }

        [Flags]
        public enum CameraFlags2 : byte
        {
            Unk9 = 0x1,
            Unk10 = 0x2,
            Unk11 = 0x4,
            Unk12 = 0x8,
            Unk13 = 0x10,
            Unk14 = 0x20,
            Unk15 = 0x40,
            Unk16 = 0x80
        }

        [YAXAttributeFor("EanType")]
        [YAXSerializeAs("value")]
        public EanTypeEnum EanType { get; set; } = EanTypeEnum.Common;
        [YAXAttributeFor("BoneToFocusOn")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; }
        [YAXAttributeFor("EanIndex")]
        [YAXSerializeAs("value")]
        public ushort EanIndex { get; set; } //ushort
        [YAXAttributeFor("StartFrame")]
        [YAXSerializeAs("value")]
        public ushort StartFrame { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("GlobalModiferDuration")]
        [YAXSerializeAs("value")]
        public ushort GlobalModiferDuration { get; set; }
        [YAXAttributeFor("EnableTransformModifers")]
        [YAXSerializeAs("value")]
        public bool EnableTransformModifiers { get; set; } //I_74_7
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float PositionX { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float PositionY { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float PositionZ { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float RotationX { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float RotationY { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float RotationZ { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("XZ")]
        [YAXFormat("0.0###########")]
        public float DisplacementXZ { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("ZY")]
        [YAXFormat("0.0###########")]
        public float DisplacementZY { get; set; }
        [YAXAttributeFor("FieldOfView")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float FieldOfView { get; set; }

        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("X")]
        public ushort PositionX_Duration { get; set; }
        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("Y")]
        public ushort PositionY_Duration { get; set; }
        [YAXAttributeFor("PositionDuration")]
        [YAXSerializeAs("Z")]
        public ushort PositionZ_Duration { get; set; }

        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("X")]
        public ushort RotationX_Duration { get; set; }
        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("Y")]
        public ushort RotationY_Duration { get; set; }
        [YAXAttributeFor("RotationDuration")]
        [YAXSerializeAs("Z")]
        public ushort RotationZ_Duration { get; set; }

        [YAXAttributeFor("DisplacementDuration")]
        [YAXSerializeAs("XZ")]
        public ushort DisplacementXZ_Duration { get; set; }
        [YAXAttributeFor("DisplacementDuration")]
        [YAXSerializeAs("ZY")]
        public ushort DisplacementZY_Duration { get; set; }

        [YAXAttributeFor("FieldOfViewDuration")]
        [YAXSerializeAs("value")]
        public ushort FieldOfView_Duration { get; set; }

        [YAXAttributeFor("EnableCameraForAllPlayers")]
        [YAXSerializeAs("value")]
        public bool EnableCameraForAllPlayers { get; set; } //I_74_0
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk1")]
        public bool I_74_1 { get; set; } //I_74_1
        [YAXAttributeFor("FocusOnTarget")]
        [YAXSerializeAs("value")]
        public bool FocusOnTarget { get; set; } //I_74_2
        [YAXAttributeFor("UseCharacterSpecificCameraEan")]
        [YAXSerializeAs("value")]
        public bool UseCharacterSpecificCameraEan { get; set; } //I_74_3
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk4")]
        public bool I_74_4 { get; set; } //I_74_4
        [YAXAttributeFor("Flag_74")]
        [YAXSerializeAs("Unk5")]
        public bool I_74_5 { get; set; } //I_74_5
        [YAXAttributeFor("DontOverrideActiveCameras")]
        [YAXSerializeAs("value")]
        public bool DontOverrideActiveCameras { get; set; } //I_74_6


        [YAXAttributeFor("ExtraFlags")]
        [YAXSerializeAs("value")]
        public CameraFlags2 cameraFlags2 { get; set; } //Int8

        public static List<BAC_Type10> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type10> Type10 = new List<BAC_Type10>();

            for (int i = 0; i < count; i++)
            {
                BitArray I_74 = new BitArray(new byte[1] { rawBytes[offset + 74] });

                Type10.Add(new BAC_Type10()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    EanType = (EanTypeEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                    BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 10),
                    EanIndex = BitConverter.ToUInt16(rawBytes, offset + 12),
                    StartFrame = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    GlobalModiferDuration = BitConverter.ToUInt16(rawBytes, offset + 18),
                    PositionZ = BitConverter.ToSingle(rawBytes, offset + 20),
                    DisplacementXZ = BitConverter.ToSingle(rawBytes, offset + 24),
                    DisplacementZY = BitConverter.ToSingle(rawBytes, offset + 28),
                    RotationX = BitConverter.ToSingle(rawBytes, offset + 36),
                    RotationY = BitConverter.ToSingle(rawBytes, offset + 32),
                    PositionX = BitConverter.ToSingle(rawBytes, offset + 40),
                    PositionY = BitConverter.ToSingle(rawBytes, offset + 44),
                    FieldOfView = BitConverter.ToSingle(rawBytes, offset + 48),
                    RotationZ = BitConverter.ToSingle(rawBytes, offset + 52),
                    PositionZ_Duration = BitConverter.ToUInt16(rawBytes, offset + 56),
                    DisplacementXZ_Duration = BitConverter.ToUInt16(rawBytes, offset + 58),
                    DisplacementZY_Duration = BitConverter.ToUInt16(rawBytes, offset + 60),
                    RotationX_Duration = BitConverter.ToUInt16(rawBytes, offset + 64),
                    RotationY_Duration = BitConverter.ToUInt16(rawBytes, offset + 62),
                    PositionX_Duration = BitConverter.ToUInt16(rawBytes, offset + 66),
                    PositionY_Duration = BitConverter.ToUInt16(rawBytes, offset + 68),
                    FieldOfView_Duration = BitConverter.ToUInt16(rawBytes, offset + 70),
                    RotationZ_Duration = BitConverter.ToUInt16(rawBytes, offset + 72),
                    EnableCameraForAllPlayers = I_74[0],
                    I_74_1 = I_74[1],
                    FocusOnTarget = I_74[2],
                    UseCharacterSpecificCameraEan = I_74[3],
                    I_74_4 = I_74[4],
                    I_74_5 = I_74[5],
                    DontOverrideActiveCameras = I_74[6],
                    EnableTransformModifiers = I_74[7],
                    cameraFlags2 = (CameraFlags2)rawBytes[offset + 75]
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
                BitArray I_74 = new BitArray(new bool[8] { type.EnableCameraForAllPlayers, type.I_74_1, type.FocusOnTarget, type.UseCharacterSpecificCameraEan, type.I_74_4, type.I_74_5, type.DontOverrideActiveCameras, type.EnableTransformModifiers, });

                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.EanType));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes(type.EanIndex));
                bytes.AddRange(BitConverter.GetBytes(type.StartFrame));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.GlobalModiferDuration));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementXZ));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementZY));
                bytes.AddRange(BitConverter.GetBytes(type.RotationY));
                bytes.AddRange(BitConverter.GetBytes(type.RotationX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY));
                bytes.AddRange(BitConverter.GetBytes(type.FieldOfView));
                bytes.AddRange(BitConverter.GetBytes(type.RotationZ));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementXZ_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementZY_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.RotationY_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.RotationX_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.PositionX_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.FieldOfView_Duration));
                bytes.AddRange(BitConverter.GetBytes(type.RotationZ_Duration));
                bytes.Add(Utils.ConvertToByte(I_74));
                bytes.Add((byte)type.cameraFlags2);
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Sound")]
    [Serializable]
    public class BAC_Type11 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"Sound ({AcbType}, {CueId})";
        [YAXDontSerialize]
        public override int TypeID => 11;

        #region NonSerialized
        [YAXDontSerialize]
        public ushort AcbTypeNumeric { get { return (ushort)AcbType; } set { AcbType = (AcbType)value; } }
        #endregion

        [YAXAttributeFor("ACB")]
        [YAXSerializeAs("File")]
        public AcbType AcbType { get; set; }
        [YAXAttributeFor("SoundFlags")]
        [YAXSerializeAs("value")]
        public SoundFlags SoundFlags { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public ushort CueId { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; } //always 0


        public static List<BAC_Type11> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type11> Type11 = new List<BAC_Type11>();

            for (int i = 0; i < count; i++)
            {
                Type11.Add(new BAC_Type11()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    AcbType = (AcbType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    SoundFlags = (SoundFlags)BitConverter.ToUInt16(rawBytes, offset + 10),
                    CueId = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.AcbType));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.SoundFlags));
                bytes.AddRange(BitConverter.GetBytes(type.CueId));
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
        public override string Type => $"TargetingAssistance ({Axis})";
        [YAXDontSerialize]
        public override int TypeID => 12;

        [YAXAttributeFor("Axis")]
        [YAXSerializeAs("value")]
        public TargetingAxis Axis { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //always 0

        public static List<BAC_Type12> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type12> Type12 = new List<BAC_Type12>();

            for (int i = 0; i < count; i++)
            {
                Type12.Add(new BAC_Type12()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    Axis = (TargetingAxis)BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.Axis));
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
        public override string Type => $"BcsPartSetInvisibility ({Part}, {Visibility})";
        [YAXDontSerialize]
        public override int TypeID => 13;

        [YAXAttributeFor("Part")]
        [YAXSerializeAs("value")]
        public BcsPartId Part { get; set; } //uint16
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public BcsPartVisibilitySwitch Visibility { get; set; } //uint16

        public static List<BAC_Type13> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type13> Type13 = new List<BAC_Type13>();

            for (int i = 0; i < count; i++)
            {
                Type13.Add(new BAC_Type13()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    Part = (BcsPartId)BitConverter.ToInt16(rawBytes, offset + 8),
                    Visibility = (BcsPartVisibilitySwitch)BitConverter.ToInt16(rawBytes, offset + 10)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.Part));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.Visibility));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("BoneModification")]
    [Serializable]
    public class BAC_Type14 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "BoneModification";
        [YAXDontSerialize]
        public override int TypeID => 14;

        [Flags]
        public enum AnimationModFlags : ushort
        {
            Head_HorVert = 0x1,
            Spine_Vert = 0x2,
            Spine_Hor = 0x4,
            Unk4 = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            Unk10 = 0x200,
            Unk11 = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Unk16 = 0x8000
        }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public AnimationModFlags ModificationFlags { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //always 0

        public static List<BAC_Type14> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type14> Type14 = new List<BAC_Type14>();

            for (int i = 0; i < count; i++)
            {
                Type14.Add(new BAC_Type14()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    ModificationFlags = (AnimationModFlags)BitConverter.ToUInt16(rawBytes, offset + 8),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.ModificationFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("Functions")]
    [Serializable]
    public class BAC_Type15 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type
        {
            get
            {
                string ret;
                if (!ValuesDictionary.BAC.BacFunctionNames.TryGetValue(FunctionType, out ret))
                    ret = FunctionType.ToString();

                return $"Functions ({ret})";
            }
        }
        [YAXDontSerialize]
        public override int TypeID => 15;


        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Type")]
        public int FunctionType { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Param1")]
        [YAXFormat("0.0########")]
        public float Param1 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Param2")]
        [YAXFormat("0.0########")]
        public float Param2 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Param3")]
        [YAXFormat("0.0########")]
        public float Param3 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Param4")]
        [YAXFormat("0.0########")]
        public float Param4 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Param5")]
        [YAXFormat("0.0########")]
        public float Param5 { get; set; }

        public static List<BAC_Type15> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type15> Type15 = new List<BAC_Type15>();

            for (int i = 0; i < count; i++)
            {
                Type15.Add(new BAC_Type15()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    FunctionType = BitConverter.ToInt32(rawBytes, offset + 8),
                    Param1 = BitConverter.ToSingle(rawBytes, offset + 12),
                    Param2 = BitConverter.ToSingle(rawBytes, offset + 16),
                    Param3 = BitConverter.ToSingle(rawBytes, offset + 20),
                    Param4 = BitConverter.ToSingle(rawBytes, offset + 24),
                    Param5 = BitConverter.ToSingle(rawBytes, offset + 28)
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
                bytes.AddRange(BitConverter.GetBytes(type.FunctionType));
                bytes.AddRange(BitConverter.GetBytes(type.Param1));
                bytes.AddRange(BitConverter.GetBytes(type.Param2));
                bytes.AddRange(BitConverter.GetBytes(type.Param3));
                bytes.AddRange(BitConverter.GetBytes(type.Param4));
                bytes.AddRange(BitConverter.GetBytes(type.Param5));
            }

            return bytes;
        }

        public override int GetValue(int i)
        {
            return FunctionType;
        }
    }

    [YAXSerializeAs("ScreenEffect")]
    [Serializable]
    public class BAC_Type16 : BAC_TypeBase, IBacBone, IBacTypeMatrix
    {
        [YAXDontSerialize]
        public override string Type
        {
            get
            {
                string ret;
                if (!ValuesDictionary.BAC.ScreenEffectIds.TryGetValue(BpeIndex, out ret))
                    ret = BpeIndex.ToString();

                return $"PostEffect ({ret})";
            }
        }
        [YAXDontSerialize]
        public override int TypeID => 16;

        [Flags]
        public enum ScreenEffectFlagsEnum : ushort
        {
            Unk1 = 0x1,
            DisableEffect = 0x2,
            Unk3 = 0x4,
            AllowLoop = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            Unk10 = 0x200,
            Unk11 = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Unk16 = 0x8000,

            Unk4 = 0x8,
        }

        [YAXAttributeFor("BPE_Index")]
        [YAXSerializeAs("value")]
        public ushort BpeIndex { get; set; } //ushort
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //always 0
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ScreenEffectFlagsEnum ScreenEffectFlags { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; } //always 0
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float PositionX { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float PositionY { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float PositionZ { get; set; }

        #region IBacMatrix
        //Placeholder
        [YAXDontSerialize]
        public float RotationX { get; set; }
        [YAXDontSerialize]
        public float RotationY { get; set; }
        [YAXDontSerialize]
        public float RotationZ { get; set; }
        #endregion

        public static List<BAC_Type16> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type16> Type16 = new List<BAC_Type16>();

            for (int i = 0; i < count; i++)
            {
                Type16.Add(new BAC_Type16()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    BpeIndex = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 12),
                    ScreenEffectFlags = (ScreenEffectFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    PositionX = BitConverter.ToSingle(rawBytes, offset + 20),
                    PositionY = BitConverter.ToSingle(rawBytes, offset + 24),
                    PositionZ = BitConverter.ToSingle(rawBytes, offset + 28),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.ScreenEffectFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.PositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PositionY));
                bytes.AddRange(BitConverter.GetBytes(type.PositionZ));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("ThrowHandler")]
    [Serializable]
    public class BAC_Type17 : BAC_TypeBase, IBacBone
    {
        [YAXDontSerialize]
        public override string Type => "ThrowHandler";
        [YAXDontSerialize]
        public override int TypeID => 17;

        [Flags]
        public enum ThrowHandlerFlagsEnum : ushort
        {
            FixedDir_BoneEnabled = 0x1,
            FixedDir_BoneDisabled = 0x2,
            FreeDir_BoneEnabled = 0x4,
            FreeDir_BoneDisabled = 0x8,
            Unk5 = 0x10,
            BacJump_AfterDuration = 0x20,
            Unk7 = 0x40,
            BacJump_ReachGround = 0x80,
            MoveVictimToUser = 0x100,
            MoveVictimToUser_RelativeDir = 0x200,
            Unk11 = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Unk16 = 0x8000
        }


        [YAXAttributeFor("ThrowHandlerFlags")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ThrowHandlerFlagsEnum ThrowHandlerFlags { get; set; } //uint16
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //0 or 1
        [YAXAttributeFor("UserBone")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; }
        [YAXAttributeFor("VictimBone")]
        [YAXSerializeAs("value")]
        public BoneLinks VictimBone { get; set; }
        [YAXAttributeFor("BAC_Entry_ID")]
        [YAXSerializeAs("value")]
        public ushort BacEntryId { get; set; } //ushort
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; } //0, 1, 2

        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("X")]
        public float DisplacementX { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Y")]
        public float DisplacementY { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Z")]
        public float DisplacementZ { get; set; }


        public static List<BAC_Type17> Read(byte[] rawBytes, int offset, int count, bool isSmall)
        {
            List<BAC_Type17> Type17 = new List<BAC_Type17>();

            for (int i = 0; i < count; i++)
            {
                if (!isSmall)
                {
                    Type17.Add(new BAC_Type17()
                    {
                        StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                        Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                        ThrowHandlerFlags = (ThrowHandlerFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 12),
                        VictimBone = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 14),
                        BacEntryId = BitConverter.ToUInt16(rawBytes, offset + 16),
                        I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                        DisplacementX = BitConverter.ToSingle(rawBytes, offset + 20),
                        DisplacementY = BitConverter.ToSingle(rawBytes, offset + 24),
                        DisplacementZ = BitConverter.ToSingle(rawBytes, offset + 28),
                    });
                    offset += 32;
                }
                else
                {
                    Type17.Add(new BAC_Type17()
                    {
                        StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                        Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                        Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                        ThrowHandlerFlags = (ThrowHandlerFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        BoneLink = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 12),
                        VictimBone = (BoneLinks)BitConverter.ToUInt16(rawBytes, offset + 14),
                        BacEntryId = BitConverter.ToUInt16(rawBytes, offset + 16),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.ThrowHandlerFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.VictimBone));
                bytes.AddRange(BitConverter.GetBytes(type.BacEntryId));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));

                //Displacement values are now always written
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementX));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementY));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementZ));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("PhysicsObject")]
    [Serializable]
    public class BAC_Type18 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"PhysicsPartControl ({Function})";
        [YAXDontSerialize]
        public override int TypeID => 18;

        public enum FunctionType : ushort
        {
            SimulatePhysics = 0,
            PlayScdAnimation = 1,
            UnkFunction_3 = 2
        }

        [YAXAttributeFor("FunctionType")]
        [YAXSerializeAs("value")]
        public FunctionType Function { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("EAN_Index")]
        [YAXSerializeAs("value")]
        public ushort EanIndex { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        public float F_16 { get; set; }
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
        public int I_28 { get; set; } //always 0

        public static List<BAC_Type18> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type18> Type18 = new List<BAC_Type18>();

            for (int i = 0; i < count; i++)
            {
                Type18.Add(new BAC_Type18()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    Function = (FunctionType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    EanIndex = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.Function));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.EanIndex));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.F_16));
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
        public override string Type => $"Aura ({AuraType})";
        [YAXDontSerialize]
        public override int TypeID => 19;

        [Flags]
        public enum AuraFlagsEnum : ushort
        {
            //Only 0x1 and 0x8 used in game files
            DisableAura = 0x1,
            Unk2 = 0x2,
            Unk3 = 0x4,
            AllowLoop = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            Unk10 = 0x200,
            Unk11 = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Unk16 = 0x8000,

            Unk4 = 0x8,
        }

        [YAXAttributeFor("Aura")]
        [YAXSerializeAs("Type")]
        public AuraType AuraType { get; set; } //uint16
        [YAXAttributeFor("Aura")]
        [YAXSerializeAs("Flags")]
        public AuraFlagsEnum AuraFlags { get; set; } // uint16
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; } //always 0

        public static List<BAC_Type19> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type19> Type19 = new List<BAC_Type19>();

            for (int i = 0; i < count; i++)
            {
                Type19.Add(new BAC_Type19()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    AuraType = (AuraType)BitConverter.ToInt16(rawBytes, offset + 8),
                    AuraFlags = (AuraFlagsEnum)BitConverter.ToInt16(rawBytes, offset + 10),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.AuraType));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.AuraFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("HomingMovement")]
    [Serializable]
    public class BAC_Type20 : BAC_TypeBase, IBacBone
    {
        [YAXDontSerialize]
        public override string Type => $"HomingMovement ({HomingMovementType})";
        [YAXDontSerialize]
        public override int TypeID => 20;

        public enum HomingType : ushort
        {
            //Only 0,1,2,3 used in the game files
            HorizontalArc = 0,
            StraightLine = 1,
            UpDownLeftRight = 2,
            Unk3
        }

        [Flags]
        public enum HomingFlagsEnum : ushort
        {
            EnableAutoTracking = 0x1,
            Left = 0x1,
            UseFloatSpeedModifier = 0x2,
            Unk3 = 0x4,
            UseBones = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40, //unused
            Unk8 = 0x80, //unused
            Unk9 = 0x100, //unused
            Unk10 = 0x200, //unused
            Unk11 = 0x400, //unused
            Unk12 = 0x800, //unused
            Unk13 = 0x1000, //unused
            Unk14 = 0x2000, //unused
            Unk15 = 0x4000, //unused
            Unk16 = 0x8000, //unused

            KNOWN_MASK = 0x3f
        }

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public HomingType HomingMovementType { get; set; }
        [YAXAttributeFor("HomingFlags")]
        [YAXSerializeAs("value")]
        public HomingFlagsEnum HomingFlags { get; set; }
        [YAXAttributeFor("SpeedModifier")]
        [YAXSerializeAs("value")]
        public float SpeedModifier { get; set; } // Either UInt or Float, depending on HomingArcFlags. We can store the int version as a float for simplicity since the int version will never be too large.
        [YAXAttributeFor("FrameThreshold")]
        [YAXSerializeAs("value")]
        public int FrameThreshold { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float DisplacementX { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float DisplacementY { get; set; }
        [YAXAttributeFor("Displacement")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float DisplacementZ { get; set; }
        [YAXAttributeFor("UserBone")]
        [YAXSerializeAs("value")]
        public BoneLinks BoneLink { get; set; } //Inverted
        [YAXAttributeFor("TargetBone")]
        [YAXSerializeAs("value")]
        public BoneLinks TargetBone { get; set; } //This one works normally
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; } //always 0
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; } //always 0

        public static List<BAC_Type20> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type20> Type20 = new List<BAC_Type20>();

            for (int i = 0; i < count; i++)
            {
                HomingFlagsEnum horizontalArctype = (HomingFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 10);
                float speed;

                if (horizontalArctype.HasFlag(HomingFlagsEnum.UseFloatSpeedModifier))
                {
                    speed = BitConverter.ToSingle(rawBytes, offset + 12);
                }
                else
                {
                    speed = BitConverter.ToUInt32(rawBytes, offset + 12);
                }

                Type20.Add(new BAC_Type20()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    HomingMovementType = (HomingType)BitConverter.ToUInt16(rawBytes, offset + 8),
                    HomingFlags = (HomingFlagsEnum)horizontalArctype,
                    SpeedModifier = speed,
                    FrameThreshold = BitConverter.ToInt32(rawBytes, offset + 16),
                    DisplacementX = BitConverter.ToSingle(rawBytes, offset + 20),
                    DisplacementY = BitConverter.ToSingle(rawBytes, offset + 24),
                    DisplacementZ = BitConverter.ToSingle(rawBytes, offset + 28),
                    BoneLink = (BoneLinks)BitConverter.ToInt16(rawBytes, offset + 32),
                    TargetBone = (BoneLinks)BitConverter.ToInt16(rawBytes, offset + 36),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.HomingMovementType));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.HomingFlags));

                if (type.HomingFlags.HasFlag(HomingFlagsEnum.UseFloatSpeedModifier))
                {
                    bytes.AddRange(BitConverter.GetBytes(type.SpeedModifier));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes((uint)type.SpeedModifier));
                }

                bytes.AddRange(BitConverter.GetBytes(type.FrameThreshold));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementX));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementY));
                bytes.AddRange(BitConverter.GetBytes(type.DisplacementZ));
                bytes.AddRange(BitConverter.GetBytes((int)type.BoneLink));
                bytes.AddRange(BitConverter.GetBytes((int)type.TargetBone));
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
        public override string Type => $"EyeMovement ({EyeDirectionNext})";
        [YAXDontSerialize]
        public override int TypeID => 21;

        public enum EyeDirection : ushort
        {
            LeftUp = 0x0,
            Up = 0x1,
            RightUp = 0x2,
            Left = 0x3,
            None = 0x4,
            Right = 0x5,
            LeftDown = 0x6,
            Down = 0x7,
            RightDown = 0x8
        }

        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; } //always 0
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //0,1
        [YAXAttributeFor("DirectionPrev")]
        [YAXSerializeAs("value")]
        public EyeDirection EyeDirectionPrev { get; set; } //0 - 8
        [YAXAttributeFor("DirectionNext")]
        [YAXSerializeAs("value")]
        public EyeDirection EyeDirectionNext { get; set; } //0 - 8
        [YAXAttributeFor("EyeRotationFrames")]
        [YAXSerializeAs("value")]
        public int EyeRotationFrames { get; set; }
        [YAXAttributeFor("EyeMovementDuration")]
        [YAXSerializeAs("value")]
        public int EyeMovementDuration { get; set; } = 100;
        [YAXAttributeFor("LeftEyeRotationPercent")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float LeftEyeRotationPercent { get; set; } = 1f;
        [YAXAttributeFor("RightEyeRotationPercent")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float RightEyeRotationPercent { get; set; } = 1f;

        public static List<BAC_Type21> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type21> Type21 = new List<BAC_Type21>();

            for (int i = 0; i < count; i++)
            {
                Type21.Add(new BAC_Type21()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    EyeDirectionPrev = (EyeDirection)BitConverter.ToUInt16(rawBytes, offset + 12),
                    EyeDirectionNext = (EyeDirection)BitConverter.ToUInt16(rawBytes, offset + 14),
                    EyeRotationFrames = BitConverter.ToInt32(rawBytes, offset + 16),
                    EyeMovementDuration = BitConverter.ToInt32(rawBytes, offset + 20),
                    LeftEyeRotationPercent = BitConverter.ToSingle(rawBytes, offset + 24),
                    RightEyeRotationPercent = BitConverter.ToSingle(rawBytes, offset + 28)
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.EyeDirectionPrev));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.EyeDirectionNext));
                bytes.AddRange(BitConverter.GetBytes(type.EyeRotationFrames));
                bytes.AddRange(BitConverter.GetBytes(type.EyeMovementDuration));
                bytes.AddRange(BitConverter.GetBytes(type.LeftEyeRotationPercent));
                bytes.AddRange(BitConverter.GetBytes(type.RightEyeRotationPercent));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("BAC_Type22")]
    [Serializable]
    public class BAC_Type22 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "BAC_Type22";
        [YAXDontSerialize]
        public override int TypeID => 22;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; } //always 0
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; } //always 1
        [YAXAttributeFor("F_12")]
        [YAXFormat("0.0########")]
        [YAXSerializeAs("value")]
        public float F_12 { get; set; } //1, 10
        [YAXAttributeFor("STR_16")]
        [YAXSerializeAs("value")]
        public string STR_16 { get; set; } //HYPERSHOT_00, HYPERSHOT_return

        public static List<BAC_Type22> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type22> Type22 = new List<BAC_Type22>();

            for (int i = 0; i < count; i++)
            {
                Type22.Add(new BAC_Type22()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    STR_16 = StringEx.GetString(rawBytes, offset + 16, false, StringEx.EncodingType.ASCII, 32)
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
        public override string Type => "TransparencyEffect";
        [YAXDontSerialize]
        public override int TypeID => 23;

        [Flags]
        public enum TransparencyFlagsEnum : byte
        {
            //Only 1,2 and 3 used in game files
            Unk1 = 0x1,
            Activate = 0x2,
            Unk3 = 0x4,
            Unk4 = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            Unk8 = 0x80
        }

        [YAXAttributeFor("Transparency_Flags")]
        [YAXSerializeAs("value")]
        public TransparencyFlagsEnum TransparencyFlags { get; set; } //int8

        [YAXAttributeFor("VerticalGapWidth")]
        [YAXSerializeAs("value")]
        public byte VerticalGapWidth { get; set; }
        [YAXAttributeFor("HorizontalGapHeight")]
        [YAXSerializeAs("value")]
        public byte HorizontalGapHeight { get; set; }
        [YAXAttributeFor("VisiblePixelWidth")]
        [YAXSerializeAs("value")]
        public byte VisiblePixelWidth { get; set; }

        [YAXAttributeFor("Dilution")]
        [YAXSerializeAs("value")]
        public ushort Dilution { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public byte I_14 { get; set; } //0, 1
        [YAXAttributeFor("I_15")]
        [YAXSerializeAs("value")]
        public byte I_15 { get; set; } //always 1

        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; } //always 0

        //Color: thse values only ever range from 0 -> 1.
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0########")]
        public float Tint_R { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0########")]
        public float Tint_G { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0########")]
        public float Tint_B { get; set; }
        [YAXAttributeFor("Tint")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0########")]
        public float Tint_A { get; set; }

        //Possibily another set of RGBA values. These values can go beyond 1, however.
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_48 { get; set; } //Always either 0 or 1

        [YAXAttributeFor("F_52")]
        [YAXFormat("0.0########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("value")]
        public float[] F_52 { get; set; } = new float[3]; //size 3 (values always 0)

        public static List<BAC_Type23> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type23> Type23 = new List<BAC_Type23>();

            for (int i = 0; i < count; i++)
            {
                Type23.Add(new BAC_Type23()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    TransparencyFlags = (TransparencyFlagsEnum)rawBytes[offset + 8],
                    VerticalGapWidth = rawBytes[offset + 9],
                    HorizontalGapHeight = rawBytes[offset + 10],
                    VisiblePixelWidth = rawBytes[offset + 11],
                    Dilution = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = rawBytes[offset + 14],
                    I_15 = rawBytes[offset + 15],
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    Tint_R = BitConverter.ToSingle(rawBytes, offset + 20),
                    Tint_G = BitConverter.ToSingle(rawBytes, offset + 24),
                    Tint_B = BitConverter.ToSingle(rawBytes, offset + 28),
                    Tint_A = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                    F_52 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 52, 3)
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
                if (type.F_52.Length != 3) throw new InvalidDataException("BACType23.Write: F_52 array size is incorrect. Should have 3 entries only.");

                bytes.AddRange(BitConverter.GetBytes(type.StartTime));
                bytes.AddRange(BitConverter.GetBytes(type.Duration));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.Flags));
                bytes.Add((byte)type.TransparencyFlags);
                bytes.Add(type.VerticalGapWidth);
                bytes.Add(type.HorizontalGapHeight);
                bytes.Add(type.VisiblePixelWidth);
                bytes.AddRange(BitConverter.GetBytes(type.Dilution)); //12
                bytes.Add(type.I_14); //14
                bytes.Add(type.I_15); //15
                bytes.AddRange(BitConverter.GetBytes(type.I_16)); //16
                bytes.AddRange(BitConverter.GetBytes(type.Tint_R)); //20
                bytes.AddRange(BitConverter.GetBytes(type.Tint_G)); //24
                bytes.AddRange(BitConverter.GetBytes(type.Tint_B)); //28
                bytes.AddRange(BitConverter.GetBytes(type.Tint_A)); //32
                bytes.AddRange(BitConverter.GetBytes(type.F_36)); //36
                bytes.AddRange(BitConverter.GetBytes(type.F_40)); //40
                bytes.AddRange(BitConverter.GetBytes(type.F_44)); //44
                bytes.AddRange(BitConverter.GetBytes(type.F_48)); //48
                bytes.AddRange(BitConverter_Ex.GetBytes(type.F_52)); //52
            }

            if (bytes.Count != types.Count * 64) throw new InvalidDataException("BACType23.Write: Invalid size!");

            return bytes;
        }
    }

    [YAXSerializeAs("DualSkillHandler")]
    [Serializable]
    public class BAC_Type24 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "DualSkillHandler";
        [YAXDontSerialize]
        public override int TypeID => 24;

        [Flags]
        public enum DualSkillHandlerFlagsEnum : ushort
        {
            Unk1 = 0x1,
            Unk2 = 0x2,
            Unk3 = 0x4,
            Unk4 = 0x8,
            Unk5 = 0x10,
            Unk6 = 0x20,
            Unk7 = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            Unk10 = 0x200,
            Unk11 = 0x400,
            Unk12 = 0x800,
            Unk13 = 0x1000,
            Unk14 = 0x2000,
            Unk15 = 0x4000,
            Unk16 = 0x8000
        }

        [YAXAttributeFor("DualSkillFlags")]
        [YAXSerializeAs("value")]
        public DualSkillHandlerFlagsEnum DualSkillFlags { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }//0,1,2,3

        //Initiator
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }//0,1,4,0x12
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }//0,1
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; } //0,1,4,0xd,0x12
        [YAXAttributeFor("InitiatorPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float InitiatorPositionX { get; set; }
        [YAXAttributeFor("InitiatorPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float InitiatorPositionY { get; set; }
        [YAXAttributeFor("InitiatorPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float InitiatorPositionZ { get; set; }

        //Partner
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public ushort I_32 { get; set; }//0,1,4,0x12
        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public ushort I_34 { get; set; }//0,3
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; } //0,1,4,0xd,0x12
        [YAXAttributeFor("PartnerPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float PartnerPositionX { get; set; }
        [YAXAttributeFor("PartnerPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float PartnerPositionY { get; set; }
        [YAXAttributeFor("PartnerPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float PartnerPositionZ { get; set; }


        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; } //0, 0x16, 0x17, 0x18 and 0xffff
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }//0,1,2

        public static List<BAC_Type24> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type24> Type24 = new List<BAC_Type24>();

            for (int i = 0; i < count; i++)
            {
                Type24.Add(new BAC_Type24()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    DualSkillFlags = (DualSkillHandlerFlagsEnum)BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    InitiatorPositionX = BitConverter.ToSingle(rawBytes, offset + 20),
                    InitiatorPositionY = BitConverter.ToSingle(rawBytes, offset + 24),
                    InitiatorPositionZ = BitConverter.ToSingle(rawBytes, offset + 28),
                    I_32 = BitConverter.ToUInt16(rawBytes, offset + 32),
                    I_34 = BitConverter.ToUInt16(rawBytes, offset + 34),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    PartnerPositionX = BitConverter.ToSingle(rawBytes, offset + 40),
                    PartnerPositionY = BitConverter.ToSingle(rawBytes, offset + 44),
                    PartnerPositionZ = BitConverter.ToSingle(rawBytes, offset + 48),
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
                bytes.AddRange(BitConverter.GetBytes((ushort)type.DualSkillFlags));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.InitiatorPositionX));
                bytes.AddRange(BitConverter.GetBytes(type.InitiatorPositionY));
                bytes.AddRange(BitConverter.GetBytes(type.InitiatorPositionZ));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_34));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.PartnerPositionX));
                bytes.AddRange(BitConverter.GetBytes(type.PartnerPositionY));
                bytes.AddRange(BitConverter.GetBytes(type.PartnerPositionZ));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_54));
            }

            return bytes;
        }

    }

    [YAXSerializeAs("ExtendedChargeControl")]
    [Serializable]
    public class BAC_Type25 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "Extended Charge";
        [YAXDontSerialize]
        public override int TypeID => 25;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Charge_Time")]
        [YAXSerializeAs("value")]
        public int ChargeTime { get; set; }

        public static List<BAC_Type25> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type25> Type25 = new List<BAC_Type25>();

            for (int i = 0; i < count; i++)
            {
                Type25.Add(new BAC_Type25()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    ChargeTime = BitConverter.ToInt32(rawBytes, offset + 12)
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
                bytes.AddRange(BitConverter.GetBytes(type.ChargeTime));
            }

            return bytes;
        }
    }

    [YAXSerializeAs("ExtendedCameraControl")]
    [Serializable]
    public class BAC_Type26 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => "Extended Camera";
        [YAXDontSerialize]
        public override int TypeID => 26;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }

        //All values below are 0 in all (6) files, as of 1.17
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

        public static List<BAC_Type26> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type26> Type26 = new List<BAC_Type26>();

            for (int i = 0; i < count; i++)
            {
                Type26.Add(new BAC_Type26()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
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
                bytes.AddRange(BitConverter.GetBytes(type.F_16));
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
        public override string Type => $"EffectPropertyControl ({SkillType})";
        [YAXDontSerialize]
        public override int TypeID => 27;


        [YAXAttributeFor("SkillID")]
        [YAXSerializeAs("value")]
        public ushort SkillID { get; set; } //uint16
        [YAXAttributeFor("SkillType")]
        [YAXSerializeAs("value")]
        public BAC_Type8.EepkTypeEnum SkillType { get; set; }
        [YAXAttributeFor("EffectID")]
        [YAXSerializeAs("value")]
        public ushort EffectID { get; set; }
        [YAXAttributeFor("FunctionDuration")]
        [YAXSerializeAs("value")]
        public ushort FunctionDuration { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort Function { get; set; }//only ever 0x1d (flag?)
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_18 { get; set; } //always 0
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_20 { get; set; } //always 0
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_22 { get; set; } //always 0

        public static List<BAC_Type27> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type27> Type26 = new List<BAC_Type27>();

            for (int i = 0; i < count; i++)
            {
                Type26.Add(new BAC_Type27()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    SkillID = BitConverter.ToUInt16(rawBytes, offset + 8),
                    SkillType = (BAC_Type8.EepkTypeEnum)BitConverter.ToUInt16(rawBytes, offset + 10),
                    EffectID = BitConverter.ToUInt16(rawBytes, offset + 12),
                    FunctionDuration = BitConverter.ToUInt16(rawBytes, offset + 14),
                    Function = BitConverter.ToUInt16(rawBytes, offset + 16),
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
                bytes.AddRange(BitConverter.GetBytes(type.SkillID));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.SkillType));
                bytes.AddRange(BitConverter.GetBytes(type.EffectID));
                bytes.AddRange(BitConverter.GetBytes(type.FunctionDuration));
                bytes.AddRange(BitConverter.GetBytes(type.Function));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
            }

            if (bytes.Count != 24 * types.Count) throw new InvalidDataException("BacType27 invalid size.");

            return bytes;
        }
    }

    [YAXSerializeAs("BacType28")]
    [Serializable]
    public class BAC_Type28 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"BacType28 ({I_08})";
        [YAXDontSerialize]
        public override int TypeID => 28;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
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

        public static List<BAC_Type28> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type28> Type28 = new List<BAC_Type28>();

            for (int i = 0; i < count; i++)
            {
                Type28.Add(new BAC_Type28()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),

                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),


                }); ;

                offset += 36;
            }

            return Type28;
        }

        public static List<byte> Write(List<BAC_Type28> types)
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
                bytes.AddRange(BitConverter.GetBytes(type.F_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_24));
                bytes.AddRange(BitConverter.GetBytes(type.I_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));

            }

            if (bytes.Count != 36 * types.Count) throw new InvalidDataException("BacType28 invalid size.");

            return bytes;
        }
    }

    [YAXSerializeAs("BAC_Type29")]
    [Serializable]
    public class BAC_Type29 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"BAC_Type29";
        [YAXDontSerialize]
        public override int TypeID => 29;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }


        public static List<BAC_Type29> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type29> Type29 = new List<BAC_Type29>();

            for (int i = 0; i < count; i++)
            {
                Type29.Add(new BAC_Type29()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToUInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                    F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56)


                });

                offset += 60;
            }

            return Type29;
        }

        public static List<byte> Write(List<BAC_Type29> types)
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
                bytes.AddRange(BitConverter.GetBytes(type.F_16));
                bytes.AddRange(BitConverter.GetBytes(type.F_20));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.F_32));
                bytes.AddRange(BitConverter.GetBytes(type.F_36));
                bytes.AddRange(BitConverter.GetBytes(type.F_40));
                bytes.AddRange(BitConverter.GetBytes(type.F_44));
                bytes.AddRange(BitConverter.GetBytes(type.I_48));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_56));

            }

            if (bytes.Count != 60 * types.Count) throw new InvalidDataException($"BacType29 invalid size. {bytes.Count }");

            return bytes;
        }
    }

    [YAXSerializeAs("BAC_Type30")]
    [Serializable]
    public class BAC_Type30 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"BAC_Type30";
        [YAXDontSerialize]
        public override int TypeID => 30;


        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
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


        public static List<BAC_Type30> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type30> Type30 = new List<BAC_Type30>();

            for (int i = 0; i < count; i++)
            {
                Type30.Add(new BAC_Type30()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),


                });

                offset += 48;
            }

            return Type30;
        }

        public static List<byte> Write(List<BAC_Type30> types)
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
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_24));
                bytes.AddRange(BitConverter.GetBytes(type.I_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.I_40));
                bytes.AddRange(BitConverter.GetBytes(type.I_44));

            }

            if (bytes.Count != 48 * types.Count) throw new InvalidDataException($"BacType30 invalid size. {bytes.Count }");

            return bytes;
        }
    }


    [YAXSerializeAs("BAC_Type31")]
    [Serializable]
    public class BAC_Type31 : BAC_TypeBase
    {
        [YAXDontSerialize]
        public override string Type => $"BAC_Type31";
        [YAXDontSerialize]
        public override int TypeID => 31;


        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("SkillID")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
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
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }


        public static List<BAC_Type31> Read(byte[] rawBytes, int offset, int count)
        {
            List<BAC_Type31> type = new List<BAC_Type31>();

            for (int i = 0; i < count; i++)
            {
                type.Add(new BAC_Type31()
                {
                    StartTime = BitConverter.ToUInt16(rawBytes, offset + 0),
                    Duration = BitConverter.ToUInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    Flags = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18),
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                    F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, offset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    I_52 = BitConverter.ToInt32(rawBytes, offset + 52),
                    I_56 = BitConverter.ToInt32(rawBytes, offset + 56),
                    I_60 = BitConverter.ToInt32(rawBytes, offset + 60)
                });

                offset += 64;
            }

            return type;
        }

        public static List<byte> Write(List<BAC_Type31> types)
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
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
                bytes.AddRange(BitConverter.GetBytes(type.I_20));
                bytes.AddRange(BitConverter.GetBytes(type.I_22));
                bytes.AddRange(BitConverter.GetBytes(type.F_24));
                bytes.AddRange(BitConverter.GetBytes(type.F_28));
                bytes.AddRange(BitConverter.GetBytes(type.I_32));
                bytes.AddRange(BitConverter.GetBytes(type.I_36));
                bytes.AddRange(BitConverter.GetBytes(type.I_40));
                bytes.AddRange(BitConverter.GetBytes(type.I_44));
                bytes.AddRange(BitConverter.GetBytes(type.I_48));
                bytes.AddRange(BitConverter.GetBytes(type.I_52));
                bytes.AddRange(BitConverter.GetBytes(type.I_56));
                bytes.AddRange(BitConverter.GetBytes(type.I_60));

            }

            if (bytes.Count != 64 * types.Count) throw new InvalidDataException($"BacType31 invalid size. {bytes.Count}");

            return bytes;
        }
    }


    #region Enums
    public enum AcbType : ushort
    {
        Common_SE = 0,
        Character_SE = 2,
        Character_VOX = 3,
        Skill_SE = 10,
        Skill_VOX = 11
    }

    [Flags]
    public enum SoundFlags : ushort
    {
        Unk1 = 0x1,
        Unk2 = 0x2,
        Unk3 = 0x4,
        StopWhenParentEnds = 0x8,
        Unk5 = 0x10,
        Unk6 = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80,
        Unk9 = 0x100,
        Unk10 = 0x200,
        Unk11 = 0x400,
        Unk12 = 0x800,
        Unk13 = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        Unk16 = 0x8000,
    }

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

    public enum BcsPartVisibilitySwitch : ushort
    {
        On = 0,
        Off = 1
    }

    public enum TargetingAxis : ushort
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    public enum BoneLinks : ushort
    {
        b_C_Base = 0x0,
        b_C_Chest = 0x1,
        b_C_Head = 0x2,
        b_C_Neck1 = 0x3,
        b_C_Pelvis = 0x4,
        b_C_Spine1 = 0x5,
        b_C_Spine2 = 0x6,
        b_R_Hand = 0x7,
        b_L_Hand = 0x8,
        b_R_Elbow = 0x9,
        b_L_Elbow = 0xA,
        b_R_Shoulder = 0xB,
        b_L_Shoulder = 0xC,
        b_R_Foot = 0xD,
        b_L_Foot = 0xE,
        b_R_Leg1 = 0xF,
        b_L_Leg1 = 0x10,
        g_C_Head = 0x11,
        g_C_Pelvis = 0x12,
        g_L_Foot = 0x13,
        g_L_Hand = 0x14,
        g_R_Foot = 0x15,
        g_R_Hand = 0x16,
        g_x_CAM = 0x17,
        g_x_LND = 0x18,

        /*
        Unk_25 = 25,
        Unk_26 = 26,
        Unk_27 = 27,
        Unk_28 = 28,
        Unk_29 = 29,
        Unk_30 = 30,
        Unk_31 = 31,
        Unk_32 = 32,
        Unk_33 = 33,
        Unk_34 = 34,
        */
        //Old options kept for installer compatibility
        b_R_Arm1 = 0x9,
        b_L_Arm1 = 0xA,
    }
    #endregion

    public interface IBacTypeMatrix
    {
        float PositionX { get; set; }
        float PositionY { get; set; }
        float PositionZ { get; set; }
        float RotationX { get; set; }
        float RotationY { get; set; }
        float RotationZ { get; set; }
    }

    public interface IBacBone
    {
        BoneLinks BoneLink { get; set; }
    }
}
