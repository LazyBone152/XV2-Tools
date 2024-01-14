using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.BCM
{
    #region Enums
    [Flags]
    public enum ButtonInput : uint
    {
        Light = 0x1, //Square
        Heavy = 0x2, //Trianlge
        Blast = 0x4, //Circle
        Jump = 0x8, //X
        SkillMenu = 0x10, //R2
        Boost = 0x20, //L2
        Guard = 0x40, //L1
        Unk8 = 0x80,
        SuperSkill1 = 0x100,
        SuperSkill2 = 0x200,
        SuperSkill3 = 0x400,
        SuperSkill4 = 0x800,
        UltimateSkill1 = 0x1000,
        UltimateSkill2 = 0x2000,
        AwokenSkill = 0x4000,
        EvasiveSkill = 0x8000,
        SkillInput = 0x10000,
        SuperMenuPlusSkillInput = 0x20000,
        UltimateMenuPlusSkillInput = 0x40000,
        Unk20 = 0x80000,
        LockOn = 0x100000,
        Descend = 0x200000,
        DragonRadar = 0x400000,
        Jump_2 = 0x800000,
        UltimateMenu = 0x1000000,
        Unk26 = 0x2000000,
        Unk27 = 0x4000000,
        Unk28 = 0x8000000,
        UltimateMenu_2 = 0x10000000,
        Unk30 = 0x20000000,
        Unk31 = 0x40000000,
        Unk32 = 0x80000000
    }

    [Flags]
    public enum DirectionalInput : uint
    {
        Forward = 0x1,
        Backwards = 0x2,
        LeftRelative = 0x4,
        RightRelative = 0x8,
        SingleActivation = 0x10,
        Up = 0x20,
        Down = 0x40,
        Right = 0x80,
        Left = 0x100,
        Unk10 = 0x200,
        Unk11 = 0x400,
        Unk12 = 0x800,
        Unk13 = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        Unk16 = 0x8000,
        Unk17 = 0x10000,
        Unk18 = 0x20000,
        Unk19 = 0x40000,
        Unk20 = 0x80000,
        Unk21 = 0x100000,
        Unk22 = 0x200000,
        Unk23 = 0x400000,
        Unk24 = 0x800000,
        Unk25 = 0x1000000,
        Unk26 = 0x2000000,
        Unk27 = 0x4000000,
        Unk28 = 0x8000000,
        Unk29 = 0x10000000,
        Unk30 = 0x20000000,
        Unk31 = 0x40000000,
        Unk32 = 0x80000000
    }

    [Flags]
    public enum BacCases
    {
        Case1 = 1,
        Case2 = 2,
        Case3 = 4,
        Case4 = 8,
        Case5 = 16,
        Case6 = 32,
        Case7 = 64,
        Case8 = 128,
    }

    [Flags]
    public enum ActivatorState : uint
    {
        Idle = 0x1,
        Attacking = 0x2,
        Boosting = 0x4,
        Guarding = 0x8,
        ReceivingDamage = 0x10,
        Jumping = 0x20,
        NotReceivingDamage = 0x40,
        TargetIsAttacking = 0x80,
        Unk9 = 0x100,
        Unk10 = 0x200,
        Unk11 = 0x400,
        Unk12 = 0x800,
        Unk13 = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        Unk16 = 0x8000,
        Unk17 = 0x10000,
        Unk18 = 0x20000,
        Unk19 = 0x40000,
        Unk20 = 0x80000,
        Unk21 = 0x100000,
        Unk22 = 0x200000,
        Unk23 = 0x400000,
        Unk24 = 0x800000,
        Unk25 = 0x1000000,
        Unk26 = 0x2000000,
        Unk27 = 0x4000000,
        Unk28 = 0x8000000,
        Unk29 = 0x10000000,
        Unk30 = 0x20000000,
        Unk31 = 0x40000000,
        Unk32 = 0x80000000
    }

    [Flags]
    public enum PrimaryActivatorConditions : uint
    {
        Standing = 0x1,
        Floating = 0x2,
        TouchingGround = 0x4,
        OnAttackHit = 0x8,
        AttackBlocked = 0x10,
        CloseToTarget = 0x20,
        FarFromTarget = 0x40,
        InBaseForm = 0x80,
        InTransformedState = 0x100,
        Unk10 = 0x200,
        Unk11 = 0x400,
        Idle = 0x800,
        CounterMelee = 0x1000,
        CounterProjectile = 0x2000,
        KiBelow100 = 0x4000,
        KiAboveZero = 0x8000,
        Unk17 = 0x10000,
        Unk18 = 0x20000,
        Ground = 0x40000,
        Opponent = 0x80000,
        OpponentKnockback = 0x100000,
        Unk22 = 0x200000,
        TargetingOpponent = 0x400000,
        Unk24 = 0x800000,
        ActiveProjectile = 0x1000000,
        StaminaAboveZero = 0x2000000,
        NotNearStageCeiling = 0x4000000,
        NotNearCertainObjects = 0x8000000,
        UsersHealth_OneUse = 0x10000000,
        TargetsHealthLessThan25 = 0x20000000,
        CurrentBacEntryHits = 0x40000000,
        UsersHealth = 0x80000000
    }

    public enum InstallInsetAt
    {
        /// <summary>
        /// Will install new entries at end of file, but keep any overriden entry at its original position. Default value to be used if no InsertAt value is provided.
        /// </summary>
        Default,
        /// <summary>
        /// Will install new entries at start of file, and move any overriden entries there
        /// </summary>
        Start,
        /// <summary>
        /// Will install new entries at end of file, and move any overriden entries there
        /// </summary>
        End
    }

    #endregion

    [YAXSerializeAs("BCM")]
    [Serializable]
    public class BCM_File : IIsNull
    {
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BCMEntry")]
        public List<BCM_Entry> BCMEntries { get; set; } = new List<BCM_Entry>();

        #region LoadSave
        public static BCM_File Load(string path)
        {
            if(Path.GetExtension(path) == ".bcm")
            {
                return new Parser(path, false).GetBcmFile();
            }
            else if(Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".bcm")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BCM_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (BCM_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                return null;
            }
        }

        public static BCM_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).bcmFile;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        #endregion

        #region Install
        public List<string> InstallEntries(IList<BCM_Entry> entries)
        {
            List<string> ids = new List<string>();

            //Update index and goto references on all the BCM entries to avoid conflict with the existing BCM entries. (these aren't actually numbers, so appending to the string is a good enough solution)
            AppendIndex_Recursive(BCMEntries[0].BCMEntries.Count.ToString(), entries);

            //Installed in reverse order to ensure they get installed into the same order as they are declared in the XML, as each successive entry is inserted into index 0
            foreach (BCM_Entry entry in entries.Where(x => x.GetInstallInsertAt() == InstallInsetAt.Start).Reverse())
            {
                ids.Add(InstallEntry(entry).ToString());
            }

            foreach (BCM_Entry entry in entries.Where(x => x.GetInstallInsertAt() != InstallInsetAt.Start))
            {
                ids.Add(InstallEntry(entry).ToString());
            }

            return ids;
        }

        private int InstallEntry(BCM_Entry entry)
        {
            int hash = entry.CalculateInstanceHash();
            InstallInsetAt insertAt = entry.GetInstallInsertAt();

            BCM_Entry existingEntry = BCMEntries[0].BCMEntries.FirstOrDefault(x => x.CalculateInstanceHash() == hash);

            if (existingEntry != null)
            {
                switch (insertAt)
                {
                    case InstallInsetAt.Start:
                        BCMEntries[0].BCMEntries.Remove(existingEntry);
                        BCMEntries[0].BCMEntries.Insert(0, entry);
                        return hash;
                    case InstallInsetAt.End:
                        BCMEntries[0].BCMEntries.Remove(existingEntry);
                        BCMEntries[0].BCMEntries.Add(entry);
                        return hash;
                    default:
                        BCMEntries[0].BCMEntries[BCMEntries[0].BCMEntries.IndexOf(existingEntry)] = entry;
                        return hash;
                }
            }
            else
            {
                switch (insertAt)
                {
                    case InstallInsetAt.Start:
                        BCMEntries[0].BCMEntries.Insert(0, entry);
                        return hash;
                    default:
                        BCMEntries[0].BCMEntries.Add(entry);
                        return hash;
                }
            }
        }

        public void UninstallEntries(List<string> ids, BCM_File cpkFile)
        {
            foreach (string id in ids)
            {
                int hash = int.Parse(id);
                UninstallEntry(hash, cpkFile);
            }
        }

        private void UninstallEntry(int id, BCM_File cpkFile)
        {
            int cpkInsertIdx = -1;
            BCM_Entry cpkEntry = cpkFile?.BCMEntries[0].BCMEntries.FirstOrDefault(x => x.CalculateInstanceHash() == id);
            BCM_Entry entry = BCMEntries[0].BCMEntries.FirstOrDefault(x => x.CalculateInstanceHash() == id);

            //Get index to restore original BCM entry into. This needs to be done because the positioning can change with the InsertAt parameter
            if(cpkEntry != null)
            {
                int idx = cpkFile.BCMEntries[0].BCMEntries.IndexOf(cpkEntry);

                if (idx < cpkFile.BCMEntries[0].BCMEntries.Count - 1)
                {
                    int nextEntryHash = cpkFile.BCMEntries[0].BCMEntries[idx + 1].CalculateInstanceHash();
                    cpkInsertIdx = BCMEntries[0].BCMEntries.IndexOf(BCMEntries[0].BCMEntries.FirstOrDefault(x => x.CalculateInstanceHash() == nextEntryHash));
                }
            }

            if(entry != null)
            {
                if(cpkEntry != null)
                {
                    if(cpkInsertIdx != -1)
                    {
                        BCMEntries[0].BCMEntries.Insert(cpkInsertIdx, cpkEntry);
                        BCMEntries[0].BCMEntries.Remove(entry);
                    }
                    else
                    {
                        BCMEntries[0].BCMEntries.Remove(entry);
                        BCMEntries[0].BCMEntries.Add(cpkEntry);
                    }
                }
                else
                {
                    BCMEntries[0].BCMEntries.Remove(entry);
                }
            }
        }
        #endregion

        public bool IsNull()
        {
            if (BCMEntries?.Count == 0) return true;

            if (BCMEntries.Count == 1)
            {
                if (BCMEntries[0].BCMEntries?.Count == 0) return true;
            }

            return false;

        }

        public static BCM_File DefaultBcmFile()
        {
            return new BCM_File()
            {
                BCMEntries = new List<BCM_Entry>()
            };
        }

        #region DumbShit
        public void AppendIndex(string append)
        {
            if (BCMEntries != null)
                AppendIndex_Recursive(append, BCMEntries);
        }

        private static void AppendIndex_Recursive(string append, IList<BCM_Entry> entries)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    entry.Index += $"_{append}";

                    if(entry.LoopAsChild != null)
                        entry.LoopAsChild += $"_{append}";

                    if (entry.LoopAsSibling != null)
                        entry.LoopAsSibling += $"_{append}";

                    AppendIndex_Recursive(append, entry.BCMEntries);
                }
            }
        }

        public void IncreaseIds(int increaseAmount)
        {
            if (increaseAmount == 0) return;
            if (BCMEntries != null)
                IncreaseIds_Recursive(increaseAmount, BCMEntries);
        }

        private void IncreaseIds_Recursive(int increaseAmount, IList<BCM_Entry> entries)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    entry.Index = (int.Parse(entry.Index) + increaseAmount).ToString();
                    IncreaseIds_Recursive(increaseAmount, entry.BCMEntries);
                }
            }
        }

        public void TestSearch()
        {
            TestSearch_Recursive(BCMEntries);
        }

        private void TestSearch_Recursive(List<BCM_Entry> entries)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry.BCMEntries != null)
                {
                    TestSearch_Recursive(entry.BCMEntries);
                }

                if (entry.ButtonInput.HasFlag(ButtonInput.Boost))
                {
                    Console.WriteLine("This");
                    Console.Read();
                }
            }
        }
        #endregion
    }

    [YAXSerializeAs("BCMEntry")]
    [Serializable]
    public class BCM_Entry
    {
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string InsertAt { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Idx")]
        public string Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Child_GoTo_Idx")]
        [YAXDontSerializeIfNull]
        public string LoopAsChild { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Sibling_GoTo_Idx")]
        [YAXDontSerializeIfNull]
        public string LoopAsSibling { get; set; }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public uint I_00 { get; set; }
        [YAXAttributeFor("DirectionalInput")]
        [YAXSerializeAs("value")]
        public DirectionalInput DirectionalInput { get; set; }
        [YAXAttributeFor("ButtonInput")]
        [YAXSerializeAs("value")]
        public ButtonInput ButtonInput { get; set; }
        [YAXAttributeFor("HoldDownConditions")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint HoldDownConditions { get; set; }
        [YAXAttributeFor("OpponentSizeConditions")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint OpponentSizeConditions { get; set; }
        [YAXAttributeFor("MinimumLoopDuration")]
        [YAXSerializeAs("value")]
        public ushort MinimumLoopDuration { get; set; }
        [YAXAttributeFor("MaximumLoopDuration")]
        [YAXSerializeAs("value")]
        public ushort MaximumLoopDuration { get; set; }
        [YAXAttributeFor("PrimaryActivatorConditions")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public PrimaryActivatorConditions PrimaryActivatorConditions { get; set; }
        [YAXAttributeFor("ActivatorState")]
        [YAXSerializeAs("value")]
        public ActivatorState ActivatorState { get; set; }
        [YAXAttributeFor("BacEntryPrimary")]
        [YAXSerializeAs("value")]
        public short BacEntryPrimary { get; set; } = -1;
        [YAXAttributeFor("BacEntryAirborne")]
        [YAXSerializeAs("value")]
        public short BacEntryAirborne { get; set; } = -1;
        [YAXAttributeFor("BacEntryCharge")]
        [YAXSerializeAs("value")]
        public short BacEntryCharge { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public short I_36 { get; set; }
        [YAXAttributeFor("BacEntryUserConnect")]
        [YAXSerializeAs("value")]
        public short BacEntryUserConnect { get; set; }
        [YAXAttributeFor("BacEntryVictimConnect")]
        [YAXSerializeAs("value")]
        public short BacEntryVictimConnect { get; set; }
        [YAXAttributeFor("BacEntryTargetingOverride")]
        [YAXSerializeAs("value")]
        public ushort BacEntryTargetingOverride { get; set; } = 65535;
        [YAXAttributeFor("RandomFlag")]
        [YAXSerializeAs("value")]
        public ushort RandomFlag { get; set; }
        [YAXAttributeFor("KiCost")]
        [YAXSerializeAs("value")]
        public uint I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public uint I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public uint I_72 { get; set; }


        [YAXAttributeFor("ReceiverLinkID")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint ReceiverLinkID { get; set; }

        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public uint I_80 { get; set; }
        [YAXAttributeFor("StaminaCost")]
        [YAXSerializeAs("value")]
        public uint StaminaCost { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public uint I_88 { get; set; }
        [YAXAttributeFor("KiRequired")]
        [YAXSerializeAs("value")]
        public uint KiRequired { get; set; }
        [YAXAttributeFor("HealthRequired")]
        [YAXFormat("0.0#######")]
        [YAXSerializeAs("value")]
        public float HealthRequired { get; set; }
        [YAXAttributeFor("TransStage")]
        [YAXSerializeAs("value")]
        public short TransStage { get; set; }
        [YAXAttributeFor("CUS_AURA")]
        [YAXSerializeAs("value")]
        public short CusAura { get; set; } = -1;
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_108 { get; set; }


        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BCMEntry")]
        public List<BCM_Entry> BCMEntries { get; set; } = new List<BCM_Entry>();

        public BCM_Entry() { }

        public static BCM_Entry CreateAwokenEntry(ButtonInput buttonInput, int bacIdPrimary, BacCases cases = BacCases.Case3, ActivatorState activatorState = ActivatorState.Attacking | ActivatorState.Idle)
        {
            return new BCM_Entry()
            {
                ButtonInput = buttonInput,
                BacEntryPrimary = (short)bacIdPrimary,
                ReceiverLinkID = (uint)cases,
                ActivatorState = activatorState
            };
        }

        public BCM_Entry Clone()
        {
            return new BCM_Entry()
            {
                Index = Index,
                I_00 = I_00,
                DirectionalInput = DirectionalInput,
                ButtonInput = ButtonInput,
                TransStage = TransStage,
                CusAura = CusAura,
                I_104 = I_104,
                I_108 = I_108,
                HoldDownConditions = HoldDownConditions,
                OpponentSizeConditions = OpponentSizeConditions,
                MinimumLoopDuration = MinimumLoopDuration,
                MaximumLoopDuration = MaximumLoopDuration,
                PrimaryActivatorConditions = PrimaryActivatorConditions,
                ActivatorState = ActivatorState,
                BacEntryPrimary = BacEntryPrimary,
                BacEntryCharge = BacEntryCharge,
                I_36 = I_36,
                BacEntryUserConnect = BacEntryUserConnect,
                BacEntryVictimConnect = BacEntryVictimConnect,
                BacEntryAirborne = BacEntryAirborne,
                BacEntryTargetingOverride = BacEntryTargetingOverride,
                RandomFlag = RandomFlag,
                I_64 = I_64,
                I_68 = I_68,
                I_72 = I_72,
                ReceiverLinkID = ReceiverLinkID,
                I_80 = I_80,
                StaminaCost = StaminaCost,
                I_88 = I_88,
                KiRequired = KiRequired,
                HealthRequired = HealthRequired,
                BCMEntries = BCMEntries
            };
        }

        /// <summary>
        /// Checks to see if two entries are identical (excluding index and parent/children/siblings)
        /// </summary>
        /// <returns></returns>
        public bool Compare(BCM_Entry entry)
        {
            if (I_00 == entry.I_00 && DirectionalInput == entry.DirectionalInput &&
                HoldDownConditions == entry.HoldDownConditions && OpponentSizeConditions == entry.OpponentSizeConditions &&
                MinimumLoopDuration == entry.MinimumLoopDuration && MaximumLoopDuration == entry.MaximumLoopDuration &&
                PrimaryActivatorConditions == entry.PrimaryActivatorConditions && ActivatorState == entry.ActivatorState &&
                BacEntryPrimary == entry.BacEntryPrimary && BacEntryCharge == entry.BacEntryCharge &&
                I_36 == entry.I_36 && BacEntryUserConnect == entry.BacEntryUserConnect &&
                BacEntryVictimConnect == entry.BacEntryVictimConnect && BacEntryAirborne == entry.BacEntryAirborne &&
                BacEntryTargetingOverride == entry.BacEntryTargetingOverride && RandomFlag == entry.RandomFlag &&
                I_64 == entry.I_64 && I_68 == entry.I_68 &&
                I_72 == entry.I_72 && I_80 == entry.I_80 &&
                StaminaCost == entry.StaminaCost && I_88 == entry.I_88 &&
                KiRequired == entry.KiRequired && TransStage == entry.TransStage &&
                CusAura == entry.CusAura && I_104 == entry.I_104 &&
                I_108 == entry.I_108 && ReceiverLinkID == entry.ReceiverLinkID
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int CalculateInstanceHash()
        {
            unchecked
            {
                int hash = I_00.GetHashCode();
                hash = 31 * hash + DirectionalInput.GetHashCode();
                hash = 31 * hash + ButtonInput.GetHashCode();
                hash = 31 * hash + HoldDownConditions.GetHashCode();
                hash = 31 * hash + OpponentSizeConditions.GetHashCode();
                hash = 31 * hash + PrimaryActivatorConditions.GetHashCode();
                hash = 31 * hash + ActivatorState.GetHashCode();
                hash = 31 * hash + I_64.GetHashCode();
                hash = 31 * hash + ReceiverLinkID.GetHashCode();
                hash = 31 * hash + I_80.GetHashCode();
                hash = 31 * hash + StaminaCost.GetHashCode();
                hash = 31 * hash + I_88.GetHashCode();
                hash = 31 * hash + KiRequired.GetHashCode();
                hash = 31 * hash + HealthRequired.GetHashCode();
                hash = 31 * hash + TransStage.GetHashCode();
                hash = 31 * hash + CusAura.GetHashCode();
                hash = 31 * hash + I_104.GetHashCode();
                return 31 * hash + I_108.GetHashCode();
            }
        }
    
        public InstallInsetAt GetInstallInsertAt()
        {
            if (string.IsNullOrWhiteSpace(InsertAt)) return InstallInsetAt.Default;

            switch (InsertAt.ToLower())
            {
                case "start":
                    return InstallInsetAt.Start;
                case "end":
                    return InstallInsetAt.End;
                default:
                    throw new ArgumentException("InsertAt value is invalid. Only Start and End are valid options.");
            }
        }
    }
    

}
