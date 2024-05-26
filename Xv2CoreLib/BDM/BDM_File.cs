using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.BDM
{
    //BDM Damage Types(XV1 > XV2) :
    //5 = 5
    //6 = 7 (+1)
    //7 = 9 (+2)
    //8 = 10 (+2)
    //12 = 14 (+2)
    //14 = 16 (+2)
    //15 = 17 (+2)
    //16 = 18 (+2)


    [YAXSerializeAs("BDM")]
    [Serializable]
    public class BDM_File : ISorting, IIsNull
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public BDM_Type BDM_Type { get; set; } = BDM_Type.XV2_0;

        [YAXComment("[XV2 Types only] Each BDM SubEntry is for a different activation condition, all known ones are as follows (by Index):" +
            "\n0 = default struck" +
            "\n1 = struck while using a skill" +
            "\n2 = struck while in floating hit animation" +
            "\n3 = struck in back" +
            "\n4 = struck while in rolling hit animation" +
            "\n5 = struck whilst guarding or just guarding" +
            "\n6 = struck while in standard hit animation" +
            "\n7 = Unknown" +
            "\n8 = struck while in heavy stun animation" +
            "\n9 = struck while lying on ground")]

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BDM_Entry")]
        public List<BDM_Entry> BDM_Entries { get; set; } = new List<BDM_Entry>();

        #region LoadSave
        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            BDM_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static BDM_File Load(byte[] rawBytes, bool convertToType0 = false)
        {
            var bdm = new Parser(rawBytes).bdmFile;

            if (convertToType0 && bdm.BDM_Type == BDM_Type.XV2_1)
            {
                bdm.ConvertToXv2_0();
            }

            return bdm;
        }

        public static BDM_File Load(string path, bool convertToType0 = false)
        {
            var bdm = new Parser(path, false).bdmFile;

            if (convertToType0 && bdm.BDM_Type == BDM_Type.XV2_1)
            {
                bdm.ConvertToXv2_0();
            }

            return bdm;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        #endregion

        #region Helpers
        public void AddEntry(int id, BDM_Entry entry)
        {
            for (int i = 0; i < BDM_Entries.Count; i++)
            {
                if (BDM_Entries[i].ID == id)
                {
                    BDM_Entries[i] = entry;
                    return;
                }
            }

            BDM_Entries.Add(entry);
        }

        public int AddEntry(BDM_Entry entry)
        {
            entry.ID = NextID(0);
            BDM_Entries.Add(entry);
            return entry.ID;
        }

        public int IndexOf(int ID)
        {
            if (BDM_Entries != null)
            {
                for (int i = 0; i < BDM_Entries.Count; i++)
                {
                    if (BDM_Entries[i].ID == ID)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public BDM_Entry GetEntryClone(int id)
        {
            if (BDM_Entries == null) throw new Exception("BDM_Entries was null.");

            foreach (var entry in BDM_Entries)
            {
                if (entry.ID == id) return entry.CloneType0();
            }

            throw new Exception("Could not find the BDM_Entry with ID " + id);
        }

        public int NextID(int minID = 500)
        {
            int id = minID;

            while (IndexOf(id) != -1)
            {
                id++;
            }

            return id;
        }

        public BDM_Entry GetEntry(int id)
        {
            int idx = IndexOf(id);
            if (idx == -1) throw new Exception("Could not find a BDM_Entry the id " + id);
            return BDM_Entries[idx];
        }

        public bool IsNull()
        {
            return (BDM_Entries.Count == 0);
        }

        public static BDM_File DefaultBdmFile()
        {
            return new BDM_File()
            {
                BDM_Type = BDM_Type.XV2_0,
                BDM_Entries = new List<BDM_Entry>()
            };
        }

        public void ChangeNeutralSkillId(ushort newId)
        {
            foreach (var entry in BDM_Entries)
            {
                foreach(var subEntry in entry.Type0Entries)
                {
                    if (subEntry.Effect1_SkillID == 0xBACA)
                        subEntry.Effect1_SkillID = newId;

                    if (subEntry.Effect2_SkillID == 0xBACA)
                        subEntry.Effect2_SkillID = newId;

                    if (subEntry.Effect3_SkillID == 0xBACA)
                        subEntry.Effect3_SkillID = newId;
                }
            }
        }

        #endregion

        #region Convert
        private static ushort DamageTypeXv1ToXv2(ushort damageType)
        {
            int _type = damageType;

            if (damageType == 6) _type += 1;
            if (damageType >= 7) _type += 2;

            return (ushort)_type;
        }

        public void ConvertToXv2()
        {
            if (BDM_Type == BDM_Type.XV1)
                ConvertXv1ToXv2_0();

            if (BDM_Type == BDM_Type.XV2_1)
                ConvertToXv2_0();
        }

        /// <summary>
        /// Converts a TYPE XV2_1 BDM file into a TYPE XV2_0 BDM file.
        /// </summary>
        public void ConvertToXv2_0()
        {
            if (BDM_Type == BDM_Type.XV2_1)
            {
                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach (var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), ID = entry.ID });

                    foreach (var subEntry in entry.Type1Entries)
                    {
                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            DamageType = subEntry.I_00,
                            DamageSecondaryType = subEntry.I_94,
                            DamageAmount = subEntry.I_04,
                            DamageSpecial = subEntry.I_86,
                            AcbType = (AcbType)subEntry.I_12,
                            CueId = subEntry.I_14,
                            Effect1_ID = subEntry.I_16,
                            Effect1_SkillID = (ushort)subEntry.I_18,
                            Effect1_EepkType = (EepkType)subEntry.I_20,
                            I_22 = subEntry.I_22,
                            Effect2_ID = subEntry.I_24,
                            Effect2_SkillID = (ushort)subEntry.I_26,
                            Effect2_EepkType = (EepkType)subEntry.I_28,
                            Effect3_ID = -1,
                            PushbackStrength = subEntry.F_32,
                            PushbackAcceleration = subEntry.F_36,
                            UserStun = subEntry.I_40,
                            VictimStun = subEntry.I_42,
                            KnockbackDuration = subEntry.I_44,
                            KnockbackGroundImpactTime = subEntry.I_48,
                            KnockbackRecoveryAfterImpactTime = subEntry.I_46,
                            KnockbackGravityTime = subEntry.I_70,
                            KnockbackStrengthX = subEntry.F_52,
                            KnockbackStrengthY = subEntry.F_56,
                            KnockbackStrengthZ = subEntry.F_60,
                            KnockbackDragY = subEntry.F_64,
                            VictimInvincibilityTime = subEntry.I_72,
                            AlimentType = subEntry.I_78,
                            CameraShakeType = subEntry.I_96,
                            CameraShakeTime = subEntry.I_98,
                            UserBpeID = subEntry.I_100,
                            VictimBpeID = subEntry.I_102,
                            StaminaBrokenOverrideBdmId = -1,
                            TransformationType = subEntry.I_76,
                            StumbleType = (Stumble)subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }
                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new InvalidOperationException(String.Format("Cannot convert BDM Type {0} into BDM Type XV2_0.", BDM_Type));
            }
        }

        public void ConvertXv1ToXv2_0()
        {
            if (BDM_Type == BDM_Type.XV1)
            {
                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach (var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), ID = entry.ID });

                    foreach (var subEntry in entry.Type1Entries)
                    {
                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            DamageType = (DamageType)DamageTypeXv1ToXv2((ushort)subEntry.I_00),
                            DamageSecondaryType = subEntry.I_94,
                            DamageAmount = subEntry.I_04,
                            DamageSpecial = subEntry.I_86,
                            AcbType = (AcbType)subEntry.I_12,
                            CueId = subEntry.I_14,
                            Effect1_ID = subEntry.I_16,
                            Effect1_SkillID = (ushort)subEntry.I_18,
                            Effect1_EepkType = (EepkType)subEntry.I_20,
                            I_22 = subEntry.I_22,
                            Effect2_ID = subEntry.I_24,
                            Effect2_SkillID = (ushort)subEntry.I_26,
                            Effect2_EepkType = (EepkType)subEntry.I_28,
                            Effect3_ID = -1,
                            PushbackStrength = subEntry.F_32,
                            PushbackAcceleration = subEntry.F_36,
                            UserStun = subEntry.I_40,
                            VictimStun = subEntry.I_42,
                            KnockbackDuration = subEntry.I_44,
                            KnockbackGroundImpactTime = subEntry.I_48,
                            KnockbackRecoveryAfterImpactTime = subEntry.I_46,
                            KnockbackGravityTime = subEntry.I_70,
                            KnockbackStrengthX = subEntry.F_52,
                            KnockbackStrengthY = subEntry.F_56,
                            KnockbackStrengthZ = subEntry.F_60,
                            KnockbackDragY = subEntry.F_64,
                            VictimInvincibilityTime = subEntry.I_72,
                            AlimentType = subEntry.I_78,
                            CameraShakeType = subEntry.I_96,
                            CameraShakeTime = subEntry.I_98,
                            UserBpeID = subEntry.I_100,
                            VictimBpeID = subEntry.I_102,
                            StaminaBrokenOverrideBdmId = -1,
                            TransformationType = subEntry.I_76,
                            StumbleType = (Stumble)subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }

                    //Adding the XV2 specific subentries
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(7));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(8));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[6].Clone(9));
                    newEntries[idx].Type0Entries[6] = newEntries[idx].Type0Entries[0].Clone(6);

                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new InvalidOperationException(String.Format("Cannot convert BDM Type {0} into BDM Type XV2_0.", BDM_Type));
            }
        }

        public void ConvertXv1ToXv2_0(int skillID)
        {
            if (BDM_Type == BDM_Type.XV1)
            {

                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach (var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), ID = entry.ID });

                    foreach (var subEntry in entry.Type1Entries)
                    {
                        int skillID1 = subEntry.I_16;
                        int skillID2 = subEntry.I_24;
                        if (skillID != -1)
                        {
                            if (subEntry.I_20 >= 5 && subEntry.I_20 < 10)
                            {
                                skillID1 = skillID;
                            }
                            if (subEntry.I_28 >= 5 && subEntry.I_28 < 10)
                            {
                                skillID2 = skillID;
                            }
                        }


                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            DamageType = subEntry.I_00,
                            DamageSecondaryType = subEntry.I_94,
                            DamageAmount = subEntry.I_04,
                            DamageSpecial = subEntry.I_86,
                            AcbType = (AcbType)subEntry.I_12,
                            CueId = subEntry.I_14,
                            Effect1_ID = (short)skillID1,
                            Effect1_SkillID = (ushort)subEntry.I_18,
                            Effect1_EepkType = (EepkType)subEntry.I_20,
                            I_22 = subEntry.I_22,
                            Effect2_ID = (short)skillID2,
                            Effect2_SkillID = (ushort)subEntry.I_26,
                            Effect2_EepkType = (EepkType)subEntry.I_28,
                            Effect3_ID = -1,
                            PushbackStrength = subEntry.F_32,
                            PushbackAcceleration = subEntry.F_36,
                            UserStun = subEntry.I_40,
                            VictimStun = subEntry.I_42,
                            KnockbackDuration = subEntry.I_44,
                            KnockbackGroundImpactTime = subEntry.I_48,
                            KnockbackRecoveryAfterImpactTime = subEntry.I_46,
                            KnockbackGravityTime = subEntry.I_70,
                            KnockbackStrengthX = subEntry.F_52,
                            KnockbackStrengthY = subEntry.F_56,
                            KnockbackStrengthZ = subEntry.F_60,
                            KnockbackDragY = subEntry.F_64,
                            VictimInvincibilityTime = subEntry.I_72,
                            AlimentType = subEntry.I_78,
                            CameraShakeType = subEntry.I_96,
                            CameraShakeTime = subEntry.I_98,
                            UserBpeID = subEntry.I_100,
                            VictimBpeID = subEntry.I_102,
                            StaminaBrokenOverrideBdmId = -1,
                            TransformationType = subEntry.I_76,
                            StumbleType = (Stumble)subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }

                    //Adding the XV2 specific subentries
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(7));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(8));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[6].Clone(9));
                    newEntries[idx].Type0Entries[6] = newEntries[idx].Type0Entries[0].Clone(6);

                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new Exception("BDM file was not in Xenoverse 1 format.");
            }
        }

        #endregion
    }

    [Serializable]
    public class BDM_Entry : IInstallable, INotifyPropertyChanged
    {
        #region INotifyPropChanged
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

        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID => ID;
        [YAXDontSerialize]
        public int ID
        {
            get => Utils.TryParseInt(Index);
            set => Index = value.ToString();
        }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Wrapper for I_00

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Type0_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "T0_SubEntry")]
        [BindingSubList]
        public List<Type0SubEntry> Type0Entries { get; set; }

        //Legacy BDMs. These will be automatically converted into the newer XV2 format.
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Type1_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "T1_SubEntry")]
        public List<Type1SubEntry> Type1Entries { get; set; }

        public BDM_Entry CloneType0()
        {
            if (Type0Entries == null) throw new Exception("Cannot clone BDM Entry as it is not Type0.");
            List<Type0SubEntry> subs = new List<Type0SubEntry>();

            foreach (var sub in Type0Entries)
            {
                subs.Add(sub.Clone());
            }

            return new BDM_Entry()
            {
                ID = ID,
                Type0Entries = subs
            };
        }
    }

    [YAXSerializeAs("BDM_SubEntry")]
    [Serializable]
    public class Type0SubEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Type")]
        public DamageType DamageType { get; set; }
        [YAXDontSerialize]
        public SecondaryTypeFlags DamageSecondaryType { get; set; }

        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Secondary_Type")]
        [YAXHexValue]
        public string DamageSecondaryType_XmlBinding
        {
            get => DamageSecondaryType.ToString();
            set
            {
                if (value == null) return;
                SecondaryTypeFlags val;
                if (Enum.TryParse(value, out val))
                {
                    DamageSecondaryType = val;
                }
                else
                {
                    ushort intVal;
                    if (ushort.TryParse(value, out intVal))
                    {
                        DamageSecondaryType = (SecondaryTypeFlags)intVal;
                    }
                    else if (value.Contains("0x"))
                    {
                        DamageSecondaryType = (SecondaryTypeFlags)HexConverter.ToInt16(value);
                    }
                    else
                    {
                        throw new InvalidDataException(string.Format("BDM: Unknown value for \"Secondary_Type\" = \"{0}\"", value));
                    }
                }
            }
        }

        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Amount")]
        public ushort DamageAmount { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Special")]
        public ushort DamageSpecial { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("AcbType")]
        public AcbType AcbType { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("CueID")]
        public short CueId { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("ID")]
        public short Effect1_ID { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_ID")]
        public ushort Effect1_SkillID { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("EepkType")]
        public EepkType Effect1_EepkType { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("ID")]
        public short Effect2_ID { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_ID")]
        public ushort Effect2_SkillID { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("EepkType")]
        public EepkType Effect2_EepkType { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("ID")]
        public short Effect3_ID { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("Skill_ID")]
        public ushort Effect3_SkillID { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("EepkType")]
        public EepkType Effect3_EepkType { get; set; }



        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Strength")]
        [YAXFormat("0.0###########")]
        public float PushbackStrength { get; set; }
        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Acceleration")]
        [YAXFormat("0.0###########")]
        public float PushbackAcceleration { get; set; }
        [YAXAttributeFor("User_Stun")]
        [YAXSerializeAs("value")]
        public ushort UserStun { get; set; }
        [YAXAttributeFor("Victim_Stun")]
        [YAXSerializeAs("value")]
        public ushort VictimStun { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Duration")]
        public ushort KnockbackDuration { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Ground_Impact")]
        public ushort KnockbackGroundImpactTime { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Recovery_After_Impact")]
        public ushort KnockbackRecoveryAfterImpactTime { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Gravity")]
        public ushort KnockbackGravityTime { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float KnockbackStrengthX { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float KnockbackStrengthY { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float KnockbackStrengthZ { get; set; }
        [YAXAttributeFor("Knockback_Drag")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float KnockbackDragY { get; set; }


        [YAXAttributeFor("Victim_Invincibility")]
        [YAXSerializeAs("Time")]
        public short VictimInvincibilityTime { get; set; }
        [YAXAttributeFor("Aliment_Type")]
        [YAXSerializeAs("value")]
        public AlimentFlags AlimentType { get; set; }
        [YAXAttributeFor("Camera_Shake")]
        [YAXSerializeAs("Type")]
        public SByte CameraShakeType { get; set; } //Flag?
        [YAXAttributeFor("Camera_Shake")]
        [YAXSerializeAs("Time")]
        public ushort CameraShakeTime { get; set; }
        [YAXAttributeFor("User Screen Flash Transperancy")]
        [YAXSerializeAs("value")]
        public short UserBpeID { get; set; }
        [YAXAttributeFor("Victim Screen Flash Transparency")]
        [YAXSerializeAs("value")]
        public short VictimBpeID { get; set; }
        [YAXAttributeFor("Stamina Broken BDM ID Override")]
        [YAXSerializeAs("value")]
        public short StaminaBrokenOverrideBdmId { get; set; }
        [YAXAttributeFor("Time Before Z-Vanish Enabled")]
        [YAXSerializeAs("value")]
        public ushort ZVanishEnableTime { get; set; }
        [YAXAttributeFor("User Animation")]
        [YAXSerializeAs("Time")]
        public ushort UserAnimationTIme { get; set; }
        [YAXAttributeFor("Victim Animation")]
        [YAXSerializeAs("Time")]
        public ushort VictimAnimationTime { get; set; }
        [YAXAttributeFor("User Animation")]
        [YAXSerializeAs("Speed")]
        [YAXFormat("0.0###########")]
        public float UserAnimationSpeed { get; set; }
        [YAXAttributeFor("Victim Animation")]
        [YAXSerializeAs("Speed")]
        [YAXFormat("0.0###########")]
        public float VictimAnimationSpeed { get; set; }
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public ushort TransformationType { get; set; }
        [YAXAttributeFor("Stumble")]
        [YAXSerializeAs("value")]
        public Stumble StumbleType { get; set; }


        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public ushort I_30 { get; set; }
        [YAXAttributeFor("I_38")]
        [YAXSerializeAs("value")]
        public ushort I_38 { get; set; }
        [YAXAttributeFor("I_58")]
        [YAXSerializeAs("value")]
        public ushort I_58 { get; set; }


        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public ushort I_76 { get; set; }
        [YAXAttributeFor("I_82")]
        [YAXSerializeAs("value")]
        public ushort I_82 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_88 { get; set; } //Size 3
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_96 { get; set; } //size 2

        public Type0SubEntry Clone(int index = -1)
        {
            if (index == -1)
            {
                index = Index;
            }

            return new Type0SubEntry()
            {
                Index = index,
                DamageType = DamageType,
                I_02 = I_02,
                DamageAmount = DamageAmount,
                I_06 = I_06,
                StumbleType = (Stumble)StumbleType,
                DamageSecondaryType = DamageSecondaryType,
                CameraShakeType = CameraShakeType,
                AcbType = AcbType,
                CueId = CueId,
                Effect1_ID = Effect1_ID,
                Effect1_SkillID = Effect1_SkillID,
                Effect1_EepkType = Effect1_EepkType,
                I_22 = I_22,
                Effect2_ID = Effect2_ID,
                Effect2_SkillID = Effect2_SkillID,
                Effect2_EepkType = Effect2_EepkType,
                I_30 = I_30,
                VictimStun = VictimStun,
                I_76 = I_76,
                KnockbackGravityTime = KnockbackGravityTime,
                VictimInvincibilityTime = VictimInvincibilityTime,
                AlimentType = AlimentType,
                I_88 = I_88,
                DamageSpecial = DamageSpecial,
                I_96 = I_96,
                F_08 = F_08,
                KnockbackStrengthX = KnockbackStrengthX,
                KnockbackStrengthY = KnockbackStrengthY,
                CameraShakeTime = CameraShakeTime,
                UserBpeID = UserBpeID,
                VictimBpeID = VictimBpeID,
                StaminaBrokenOverrideBdmId = StaminaBrokenOverrideBdmId,
                ZVanishEnableTime = ZVanishEnableTime,
                UserAnimationTIme = UserAnimationTIme,
                VictimAnimationTime = VictimAnimationTime,
                Effect3_ID = Effect3_ID,
                Effect3_SkillID = Effect3_SkillID,
                Effect3_EepkType = Effect3_EepkType,
                I_38 = I_38,
                KnockbackDuration = KnockbackDuration,
                KnockbackRecoveryAfterImpactTime = KnockbackRecoveryAfterImpactTime,
                KnockbackGroundImpactTime = KnockbackGroundImpactTime,
                I_58 = I_58,
                I_82 = I_82,
                TransformationType = TransformationType,
                UserAnimationSpeed = UserAnimationSpeed,
                VictimAnimationSpeed = VictimAnimationSpeed,
                PushbackStrength = PushbackStrength,
                PushbackAcceleration = PushbackAcceleration,
                UserStun = UserStun,
                KnockbackStrengthZ = KnockbackStrengthZ,
                KnockbackDragY = KnockbackDragY
            };
        }

    }


    [Serializable]
    public class Type1SubEntry
    {
        //Older XV1-style BDM entry

        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Type")]
        public DamageType I_00 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Secondary_Type")]
        public SecondaryTypeFlags I_94 { get; set; } //uint16
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Amount")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Special")]
        public ushort I_86 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("Type")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("CueID")]
        public short I_14 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("ID")]
        public short I_16 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_ID")]
        public short I_18 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_Type")]
        public short I_20 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("ID")]
        public short I_24 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_ID")]
        public short I_26 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_Type")]
        public short I_28 { get; set; }


        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Strength")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Acceleration")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("User_Stun")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("Victim_Stun")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Duration")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Ground_Impact")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Recovery_After_Impact")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Gravity")]
        public ushort I_70 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Knockback_Drag")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Victim_Invincibility")]
        [YAXSerializeAs("Time")]
        public short I_72 { get; set; }
        [YAXAttributeFor("Aliment_Type")]
        [YAXSerializeAs("value")]
        public AlimentFlags I_78 { get; set; }
        [YAXAttributeFor("Camera_Shake_Type")]
        [YAXSerializeAs("value")]
        public SByte I_96 { get; set; } //Flag?
        [YAXAttributeFor("Camera_Shake_Time")]
        [YAXSerializeAs("value")]
        public ushort I_98 { get; set; }
        [YAXAttributeFor("User Screen Flash Transperancy")]
        [YAXSerializeAs("value")]
        public short I_100 { get; set; }
        [YAXAttributeFor("Victim Screen Flash Transparency")]
        [YAXSerializeAs("value")]
        public short I_102 { get; set; }
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public ushort I_76 { get; set; }
        [YAXAttributeFor("Stumble")]
        [YAXSerializeAs("value")]
        public ushort I_92 { get; set; }


        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public UInt16 I_22 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public UInt16 I_30 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public UInt16 I_68 { get; set; }
        [YAXAttributeFor("I_74")]
        [YAXSerializeAs("value")]
        public ushort I_74 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_80 { get; set; } //Size 3
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_88 { get; set; } //size 2
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }

        public Type1SubEntry Clone(int index = -1)
        {
            if (index == -1)
            {
                index = Index;
            }

            return new Type1SubEntry()
            {
                Index = index,
                I_00 = I_00,
                I_02 = I_02,
                I_04 = I_04,
                I_06 = I_06,
                I_100 = I_100,
                I_102 = I_102,
                I_104 = I_104,
                I_12 = I_12,
                I_14 = I_14,
                I_16 = I_16,
                I_18 = I_18,
                I_20 = I_20,
                I_22 = I_22,
                I_24 = I_24,
                I_26 = I_26,
                I_28 = I_28,
                I_30 = I_30,
                I_40 = I_40,
                I_42 = I_42,
                I_44 = I_44,
                I_46 = I_46,
                I_48 = I_48,
                I_50 = I_50,
                I_68 = I_68,
                I_70 = I_70,
                I_72 = I_72,
                I_74 = I_74,
                I_76 = I_76,
                I_78 = I_78,
                I_80 = I_80,
                I_86 = I_86,
                I_88 = I_88,
                I_92 = I_92,
                I_94 = I_94,
                I_96 = I_96,
                I_98 = I_98,
                F_08 = F_08,
                F_32 = F_32,
                F_36 = F_36,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60,
                F_64 = F_64
            };
        }
    }


    public enum BDM_Type
    {
        XV2_0, //Just auto-convert all BDMs to this
        XV2_1,
        XV1
    }

    public enum AcbType : ushort
    {
        Common = 0,
        Character_SE = 2,
        Character_VOX = 3,
        Skill_SE = 10,
        Skill_VOX = 11
    }

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

    public enum DamageType : ushort
    {
        None = 0,
        Block = 1,
        GuardBreak = 2,
        Standard = 3,
        Heavy = 4,
        Knockback = 5,
        Knockback1 = 6,
        Knockback2 = 7,
        Knockback3 = 8,
        Knockback4 = 9,
        Grab = 10,
        HoldStomach = 11,
        HoldEyes = 12,
        Knockback5 = 13,
        Electric = 14,
        Dazed = 15,
        Paralysis = 16,
        Freeze = 17,
        Wildcard = 18,
        //19 is never used in the game
        HeavyStaminaBreak = 20,
        LightStaminaBreak = 21,
        GiantKiBlastPush = 22,
        Brainwash = 23,
        GiantKiBlastReturn = 24,
        Knockback6 = 25,
        Knockback7 = 26,
        Knockback8 = 27,
        Knockback9 = 28,
        SlowOpponent = 29,
        Brainwash2 = 30,
        TimeStop = 31
    }

    public enum HitboxState
    {
        Default = 0,
        Skill = 1,
        PrimaryKnockback = 2,
        Back = 3,
        GroundImpact = 4,
        Guarding = 5,
        Stumble = 6,
        Unknown = 7,
        FloatingKnockback = 8,
        KnockedDown = 9
    }

    [Flags]
    public enum SecondaryTypeFlags : ushort
    {
        RestoreHealth = 0x1,
        Unk2 = 0x2,
        Unk3 = 0x4,
        Unk4 = 0x8,
        Unk5 = 0x10,
        Unk6 = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80,
        DisableEvasiveUsage = 0x100,
        Unk10 = 0x200,
        BypassTimeStopDamage = 0x400,
        BypassSuperArmor = 0x800,
        FaceOpponentAlways = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        Unk16 = 0x8000
    }

    [Flags]
    public enum AlimentFlags : ushort
    {
        Unk1 = 0x1,
        HP_DEF = 0x2,
        SPD = 0x4,
        Target = 0x8,
        SealAwokenSkill = 0x10,
        Unk6 = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80
    }

    [Flags]
    public enum Stumble : ushort
    {
        StumbleSet1 = 0x1,
        StumbleSet2 = 0x2,
        StumbleSet3 = 0x4,
        StumbleSet4 = 0x8,
        StumbleSet5 = 0x10,
        StumbleSet6 = 0x20,
        AllStumbleSets = 0x40,
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
}
